using Village.Abilities;
using Village.Attributes;
using Village.Base;
using Village.Households;
using Village.Items;

namespace Village.Buildings;

// Fields are specialized buildings that produce food.
// As food production is dependent on the conditions while
// the crop is growing, the field must track the current
// state of the crop.
public class Field : Building, IAbilityContext, IInventoryContext, IHouseholdContext, IAttributeContext
{
  public Inventory inventory { get; private set; } = new Inventory();

  public Household household { get; private set;}

  private const string fieldAttributeGroup = "field";
  // TODO(chmeyers): We shouldn't have to specify the type here.
  public Field(BuildingType buildingType, Household household) : base(buildingType)
  {
    this.household = household;
    state = new AttributeSet(this, this, this);
    state.Rescale(_size);
    state.SetEffectMultiplier(_size);
    state.AbilitiesChanged += UpdateAbilities;
    // Add field attributes to set.
    foreach (var attributeType in AttributeType.groups[fieldAttributeGroup])
    {
      state.Add(attributeType);
    }
    // Add the weather as a scoped set.
    state.AddScopedSet(WeatherAttributes.GetWeather());
  }

  // Advance the current state of the field to the current tick.
  public void Advance()
  {
    lock (_lock)
    {
      state.Advance();
      // Advance each crop.
      foreach (var crop in _crops.Values)
      {
        crop.Advance();
      }
    }
  }

  public bool Plant(ItemType itemType, double quantity)
  {
    lock(_lock)
    {
      if (itemType.cropSettings == null)
      {
        // Can't plant something that isn't a crop.
        return false;
      }
      if (quantity + _cropCount > _size)
      {
        // Too many to plant.
        return false;
      }
      if (!_crops.ContainsKey(itemType))
      {
        _crops[itemType] = new CropInfo(itemType, quantity, this);
      }
      else
      {
        _crops[itemType].quantity += quantity;
      }
      _cropCount += quantity;
      return true;
    }
  }

  public bool Harvest(ItemType itemType, double quantity)
  {
    lock(_lock)
    {
      if (!_crops.ContainsKey(itemType) || _crops[itemType].quantity < quantity)
      {
        // Can't harvest what isn't planted.
        return false;
      }
      _crops[itemType].quantity -= quantity;
      _cropCount -= quantity;
      return true;
    }
  }

  public double Count(ItemType itemType)
  {
    lock (_lock)
    {
      if (!_crops.ContainsKey(itemType))
      {
        return 0;
      }
      return _crops[itemType].quantity;
    }
  }

  public double GetValue(ItemType itemType, AttributeType attributeType)
  {
    lock (_lock)
    {
      if (!_crops.ContainsKey(itemType))
      {
        return state.GetValue(attributeType);
      }
      return _crops[itemType].state.GetValue(attributeType);
    }
  }

  public class CropInfo : IAbilityContext, IAttributeContext, IHouseholdContext
  {
    private const string cropAttributeGroup = "crop";
    public CropInfo(ItemType itemType, double quantity, Field parent)
    {
      if (itemType.cropSettings == null)
      {
        throw new System.ArgumentException("ItemType must be a crop.", nameof(itemType));
      }
      this.itemType = itemType;
      this._quantity = quantity;
      // Crop attributes effects use the field's abilities, but point at the Crop and
      // take named attribute values from the crop's AttributeSet, which includes
      // the field's AttributeSet as a scoped set.
      _parent = parent;
      this.state = new AttributeSet(this, _parent, this);
      this.state.AddScopedSet(_parent.state);
      this.state.Rescale(quantity);
      this.state.SetEffectMultiplier(quantity);
      foreach (var attributeType in AttributeType.groups[cropAttributeGroup])
      {
        this.state.Add(attributeType);
      }
      if (itemType.cropSettings != null && itemType.cropSettings!.cropAttribute != null)
      {
        this.state.Add(itemType.cropSettings!.cropAttribute);
      }
    }

    public void Advance()
    {
      // Advance the attributes.
      state.Advance();
    }

    public ItemType itemType;
    // Whenver the quantity changes we need to rescale the AttributeSet.
    private double _quantity;
    public double quantity {
      get {
        return _quantity;
      }
      set {
        _quantity = value;
        this.state.Rescale(quantity);
        this.state.SetEffectMultiplier(value);
      }
    }
    public AttributeSet state;
    private Field _parent;

    public Dictionary<AbilityType, HashSet<IAbilityProvider>> AbilityProviders => throw new NotImplementedException();

    // Crop abilities the parent field's abilities.
    // Note that this means that any abilites added by the crop's AttributeSet
    // will be ignored.
    public HashSet<AbilityType> Abilities => _parent.Abilities;

    public Household household => _parent.household;

    // Not Currently Used.
    public event AbilitiesChanged? AbilitiesChanged { add { } remove { } }

    public void GrantAbility(AbilityType ability)
    {
      throw new NotImplementedException();
    }

    public double GetNamedValue(string name)
    {
      return state.GetNamedValue(name);
    }

    public double SetAttribute(AttributeType attributeType, double value)
    {
      return state.SetAttribute(attributeType, value);
    }

    public double GetAttributeValue(AttributeType attributeType)
    {
      return state.GetAttributeValue(attributeType);
    }

    public double GetUnscaledAttributeValue(AttributeType attributeType)
    {
      return state.GetUnscaledValue(attributeType);
    }

    public double AddAttribute(AttributeType attributeType, double value)
    {
      return state.AddAttribute(attributeType, value);
    }

    public double AddAttribute(AttributeType attributeType)
    {
      return state.AddAttribute(attributeType);
    }
  }

  private Dictionary<ItemType, CropInfo> _crops = new Dictionary<ItemType, CropInfo>();

  // The current number of crops in the field.
  private double _cropCount = 0;

  // The size of the field
  // In tenths of an acre, i.e. 4 rods by 4 rods.
  // 1 acre = 4 rods by 40 rods = 80 furrows each 1 furlong long.
  // 1 rod = 5.5 yards = 16.5 feet
  // One skilled guy with a good plow and a single ox can plow 1 acre in a day.
  private int _size = 10;

  // AttributeSet to track the current state of the field.
  public AttributeSet state;

  public double GetNamedValue(string name)
  {
    return state.GetNamedValue(name);
  }

  // Abilities given to a field will bubble up to every member of the household.
  // They are not granted to the household's other fields.
  // TODO(chmeyers): Do we want a separate set of internal abilities for the field?
  public void GrantAbility(AbilityType ability)
  {
    throw new NotImplementedException();
  }

  // Building abilities are stored in the base class. We store field specific abilities here.
  // Field specific abilities are not propagated to the household.
  protected Dictionary<AbilityType, HashSet<IAbilityProvider>> _fieldAbilityProviders = new Dictionary<AbilityType, HashSet<IAbilityProvider>>();

  public override Dictionary<AbilityType, HashSet<IAbilityProvider>> AbilityProviders { get { return _fieldAbilityProviders; } }

  private HashSet<AbilityType> _fieldAbilities = new HashSet<AbilityType>();

  // Note that we are only returning the field abilities here, so the abilities
  // given by the field as a building to a household are not included.
  public override HashSet<AbilityType> Abilities { get { return _fieldAbilities; } }

  private object _lock = new object();

  private void UpdateAbilities(IAbilityProvider? addedProvider, IEnumerable<AbilityType>? added, IAbilityProvider? removedProvider, IEnumerable<AbilityType>? removed)
  {
    lock (_lock)
    {
      // Note that we don't propagate any abilities here to the household,
      // as we are passing a null event handler.
      IAbilityCollection.UpdateAbilities(ref _fieldAbilityProviders, ref _fieldAbilities, addedProvider, added, removedProvider, removed, null);
    }
  }

  public double SetAttribute(AttributeType attributeType, double value)
  {
    return state.SetAttribute(attributeType, value);
  }

  public double GetAttributeValue(AttributeType attributeType)
  {
    return state.GetAttributeValue(attributeType);
  }

  public double AddAttribute(AttributeType attributeType, double value)
  {
    return state.AddAttribute(attributeType, value);
  }

  public double AddAttribute(AttributeType attributeType)
  {
    return state.AddAttribute(attributeType);
  }
}