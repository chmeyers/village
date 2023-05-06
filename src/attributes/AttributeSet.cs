using Village.Abilities;
using Village.Items;

namespace Village.Attributes;

// An AttributeSet contains the set of attributes for a person.
public class AttributeSet : IAbilityCollection
{
  // The attributes.
  public Dictionary<AttributeType, Attribute> attributes { get; private set; } = new Dictionary<AttributeType, Attribute>();
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
  private IInventoryContext? effectTarget;
  private IAbilityContext? context;

  // Constructor for an AttributeSet.
  public AttributeSet(IInventoryContext? effectTarget, IAbilityContext? effectContext)
  {
    this.effectTarget = effectTarget;
    this.context = effectContext;
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
      return attributes.ContainsKey(attributeType);
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
  private void AddNoLock(AttributeType attributeType)
  {
    var attribute = new Attribute(attributeType, effectTarget, context);
    attributes.Add(attributeType, attribute);
    // Chain the event handler.
    attribute.AbilitiesChanged += UpdateAbilities;
    if (attribute.Abilities.Count > 0)
    {
      UpdateAbilities(attribute, attribute.Abilities, null, null);
    }
  }

  // Set the value of a specific attribute.
  // This will add the attribute if it doesn't exist.
  // Returns the new value of the attribute.
  public int SetValue(AttributeType attributeType, int value)
  {
    lock (_lock)
    {
      if (!attributes.ContainsKey(attributeType))
      {
        AddNoLock(attributeType);
      }
      return attributes[attributeType].SetValue(value);
    }
  }

  // Add to the value of a specific attribute.
  // This will add the attribute if it doesn't exist.
  // Returns the new value of the attribute.
  public int AddValue(AttributeType attributeType, int value)
  {
    lock (_lock)
    {
      if (!attributes.ContainsKey(attributeType))
      {
        AddNoLock(attributeType);
      }
      return attributes[attributeType].AddValue(value);
    }
  }

  // Get the value of a specific attribute.
  // This will not add the attribute if it doesn't exist,
  // but will return the initial value for the attribute type.
  public int GetValue(AttributeType attributeType)
  {
    lock (_lock)
    {
      if (!attributes.ContainsKey(attributeType))
      {
        return attributeType.initialValue.GetValue(context);
      }
      return attributes[attributeType].value;
    }
  }
}