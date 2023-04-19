using Village.Abilities;
using Village.Items;

namespace Village.Attributes;

// An AttributeSet contains the set of attributes for a person.
public class AttributeSet
{
  // The attributes.
  private Dictionary<AttributeType, Attribute> _attributes = new Dictionary<AttributeType, Attribute>();
  // Cache of the abilities given by the attributes.
  private HashSet<AbilityType> attributeAbilities = new HashSet<AbilityType>();
  // Dirty bit for the attribute abilities.
  private bool attributeAbilitiesDirty = true;
  // Event handler for when the abilities of a person change.
  public event AbilitiesChanged? AbilitiesChanged;
  // lock for the attribute abilities.
  private object _lock = new object();

  // The target and context for any effects run.
  // Typically this will be the person the attributes belong to.
  private IInventoryContext? effectTarget;
  private IAbilityContext? effectContext;

  // Constructor for an AttributeSet.
  public AttributeSet(IInventoryContext? effectTarget, IAbilityContext? effectContext)
  {
    this.effectTarget = effectTarget;
    this.effectContext = effectContext;
  }


  // Add an attribute to the set.
  public void Add(AttributeType attributeType)
  {
    lock (_lock)
    {
      AddNoLock(attributeType);
    }
  }

  // Contains the attribute type.
  public bool Contains(AttributeType attributeType)
  {
    lock (_lock)
    {
      return _attributes.ContainsKey(attributeType);
    }
  }

  // Add without the lock.
  private void AddNoLock(AttributeType attributeType)
  {
    var attribute = new Attribute(attributeType, effectTarget, effectContext);
    _attributes.Add(attributeType, attribute);
    if (attribute.GetAbilities().Count > 0)
    {
      attributeAbilitiesDirty = true;
      AbilitiesChanged?.Invoke();
    }
  }

  // Get the set of abilities currently given by the attributes.
  public HashSet<AbilityType> AttributeAbilities()
  {
    lock (_lock)
    {
      if (!attributeAbilitiesDirty)
      {
        return attributeAbilities;
      }
      attributeAbilities.Clear();
      foreach (Attribute attribute in _attributes.Values)
      {
        attributeAbilities.UnionWith(attribute.GetAbilities());
      }
      attributeAbilitiesDirty = false;
      return attributeAbilities;
    }
  }

  // Set the value of a specific attribute.
  // This will add the attribute if it doesn't exist.
  // Returns the new value of the attribute.
  public int SetValue(AttributeType attributeType, int value)
  {
    lock (_lock)
    {
      if (!_attributes.ContainsKey(attributeType))
      {
        AddNoLock(attributeType);
      }
      if (_attributes[attributeType].SetValue(value))
      {
        attributeAbilitiesDirty = true;
        AbilitiesChanged?.Invoke();
      }
      return _attributes[attributeType].value;
    }
  }

  // Add to the value of a specific attribute.
  // This will add the attribute if it doesn't exist.
  // Returns the new value of the attribute.
  public int AddValue(AttributeType attributeType, int value)
  {
    lock(_lock)
    {
      if (!_attributes.ContainsKey(attributeType))
      {
        AddNoLock(attributeType);
      }
      if (_attributes[attributeType].AddValue(value))
      {
        attributeAbilitiesDirty = true;
        AbilitiesChanged?.Invoke();
      }
      return _attributes[attributeType].value;
    }
  }

  // Get the value of a specific attribute.
  // This will not add the attribute if it doesn't exist,
  // but will return the initial value for the attribute type.
  public int GetValue(AttributeType attributeType)
  {
    lock (_lock)
    {
      if (!_attributes.ContainsKey(attributeType))
      {
        return attributeType.initialValue;
      }
      return _attributes[attributeType].value;
    }
  }
}