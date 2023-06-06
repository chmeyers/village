using Village.Abilities;
using Village.Base;
using Village.Items;

namespace Village.Attributes;

public interface IAttributeContext
{
  public double SetAttribute(AttributeType attributeType, double value);

  public double GetAttributeValue(AttributeType attributeType);

  public double AddAttribute(AttributeType attributeType, double value);

  public double AddAttribute(AttributeType attributeType);

  public double Utility(AttributeType attributeType, double delta);
}

// An AttributeSet contains the set of attributes for a person.
public class AttributeSet : IAbilityCollection, IAttributeContext
{
  // The attributes.
  public Dictionary<AttributeType, Attribute> attributes { get; private set; } = new Dictionary<AttributeType, Attribute>();
  // Attributes that may have ongoing effects.
  public List<Attribute> effectAttributes { get; private set; } = new List<Attribute>();
  // Cache of the abilities given by the attributes.
  private Dictionary<AbilityType, HashSet<IAbilityProvider>> _abilityProviders = new Dictionary<AbilityType, HashSet<IAbilityProvider>>();

  public Dictionary<AbilityType, HashSet<IAbilityProvider>> AbilityProviders { get { return _abilityProviders; } }

  private HashSet<AbilityType> _abilities = new HashSet<AbilityType>();

  public HashSet<AbilityType> Abilities { get { return _abilities; } }

  // Event handler for when the abilities of a person change.
  public event AbilitiesChanged? AbilitiesChanged;

  // lock for the attribute abilities.
  private object _lock = new object();

  // The target and context for any effects run.
  // Typically this will be the person the attributes belong to.
  private object? _target;
  private IInventoryContext? _targetContext;
  private IAbilityContext? _abilityContext;

  // Other attribute sets that this attribute inherits from.
  // Name lookups will include these sets.
  private List<AttributeSet> _scopedSets = new List<AttributeSet>();

  private double _scale = 1;
  private double _effectMultiplier = 1;
  // When was the last time the attributes were advanced.
  private long _lastAdvanceTick = 0;

  // Constructor for an AttributeSet.
  public AttributeSet(object? target, IInventoryContext? effectTarget, IAbilityContext? effectContext)
  {
    this._target = target;
    this._targetContext = effectTarget;
    this._abilityContext = effectContext;
    // Note that we can't set the last advance tick to the current calendar tick
    // because the calendar creates an attribute set during it's constructor.
    // Instead it will get set the first time the attributes are advanced.
    this._lastAdvanceTick = 0;
  }

  public void AddScopedSet(AttributeSet set)
  {
    lock (_lock)
    {
      _scopedSets.Add(set);
    }
  }

  // Explicitly add an attribute to the set, overriding any scoped sets.
  public void Add(AttributeType attributeType)
  {
    lock (_lock)
    {
      Advance();
      if (attributes.ContainsKey(attributeType)) return;
      AddNoLock(attributeType);
    }
  }

  // Contains the attribute type.
  public bool Contains(AttributeType attributeType)
  {
    lock (_lock)
    {
      return attributes.ContainsKey(attributeType) || _scopedSets.Any(set => set.Contains(attributeType));
    }
  }

  public void Advance()
  {
    lock (_lock)
    {
      if (Calendar.Ticks == _lastAdvanceTick) return;
      _lastAdvanceTick = Calendar.Ticks;
      foreach (var attribute in effectAttributes)
      {
        // Only advance the attribute if it has ongoing effects or a change per tick.
        attribute.Advance();
      }
    }
  }

  public void Rescale(double scale)
  {
    if (scale <= 0) throw new ArgumentException("Attribute Scale must be positive.");
    lock (_lock)
    {
      Advance();
      _scale = scale;
      foreach (var attribute in attributes.Values)
      {
        attribute.Rescale(scale);
      }
    }
  }

  public void SetEffectMultiplier(double value)
  {
    lock (_lock)
    {
      Advance();
      _effectMultiplier = value;
      foreach (var attribute in effectAttributes)
      {
        attribute.effectMultiplier = value;
      }
    }
  }

  // Update Abilities from the attributes.
  private void UpdateAbilities(IAbilityProvider? addedProvider, IEnumerable<AbilityType>? added, IAbilityProvider? removedProvider, IEnumerable<AbilityType>? removed)
  {
    lock (_lock)
    {
      IAbilityCollection.UpdateAbilities(ref _abilityProviders, ref _abilities, addedProvider, added, removedProvider, removed, AbilitiesChanged);
    }
  }

  // Add without the lock.
  private Attribute AddNoLock(AttributeType attributeType)
  {
    var attribute = new Attribute(attributeType, _target, _targetContext, _abilityContext);
    attribute.Rescale(_scale);
    attribute.effectMultiplier = _effectMultiplier;
    attributes.Add(attributeType, attribute);
    // Chain the event handler.
    attribute.AbilitiesChanged += UpdateAbilities;
    if (attribute.Abilities.Count > 0)
    {
      UpdateAbilities(attribute, attribute.Abilities, null, null);
    }
    if (attributeType.HasOngoingEffects() || attributeType.changePerTick != 0)
    {
      effectAttributes.Add(attribute);
    }
    return attribute;
  }

  private Attribute? _GetScopedAttribute(AttributeType attributeType)
  {
    if (attributes.ContainsKey(attributeType))
    {
      return attributes[attributeType];
    }
    foreach (var scopedSet in _scopedSets)
    {
      var scoped = scopedSet._GetScopedAttribute(attributeType);
      if (scoped != null)
      {
        return scoped;
      }
    }
    return null;
  }

  // Set the value of a specific attribute.
  // This will add the attribute if it doesn't exist.
  // Returns the new value of the attribute.
  public double SetValue(AttributeType attributeType, double value)
  {
    lock (_lock)
    {
      Advance();
      Attribute? attribute = _GetScopedAttribute(attributeType);
      if (attribute == null)
      {
        attribute = AddNoLock(attributeType);
      }
      return attribute.SetValue(value);
    }
  }

  // Add to the value of a specific attribute.
  // This will add the attribute if it doesn't exist.
  // Returns the new value of the attribute.
  public double AddValue(AttributeType attributeType, double value)
  {
    lock (_lock)
    {
      Advance();
      Attribute? attribute = _GetScopedAttribute(attributeType);
      if (attribute == null)
      {
        attribute = AddNoLock(attributeType);
      }
      return attribute.AddValue(value);
    }
  }

  // Get the value of a specific attribute.
  // This will not add the attribute if it doesn't exist,
  // but will return the initial value for the attribute type.
  public double GetValue(AttributeType attributeType)
  {
    lock (_lock)
    {
      Attribute? attribute = _GetScopedAttribute(attributeType);
      if (attribute == null)
      {
        return attributeType.initialValue.GetValue(_abilityContext);
      }
      return attribute.value;
    }
  }

  public double GetUnscaledValue(AttributeType attributeType)
  {
    lock (_lock)
    {
      Attribute? attribute = _GetScopedAttribute(attributeType);
      if (attribute == null)
      {
        return attributeType.initialValue.GetValue(_abilityContext);
      }
      return attribute.GetUnscaled();
    }
  }

  public double? GetNamedValue(string name)
  {
    lock (_lock)
    {
      // Lookup the attribute type.
      var attributeType = AttributeType.Find(name);
      if (attributeType == null)
      {
        return null;
      }
      return GetValue(attributeType);
    }
  }

  // IAttributeContext implementation
  public double SetAttribute(AttributeType attributeType, double value)
  {
    return SetValue(attributeType, value);
  }

  public double GetAttributeValue(AttributeType attributeType)
  {
    return GetValue(attributeType);
  }

  public double AddAttribute(AttributeType attributeType, double value)
  {
    return AddValue(attributeType, value);
  }

  public double AddAttribute(AttributeType attributeType)
  {
    Add(attributeType);
    return GetValue(attributeType);
  }

  public double Utility(AttributeType attributeType, double delta)
  {
    // TODO(chmeyers): Implement this.
    return 0;
  }
}