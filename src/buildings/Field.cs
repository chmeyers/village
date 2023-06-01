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

  public Household household { get; private set; }

  private const string fieldAttributeGroup = "field";
  private const double minPlantQuantity = 0.000022; // ~ 1 square foot.
  // TODO(chmeyers): We shouldn't have to specify the type here.
  public Field(BuildingType buildingType, Household household) : base(buildingType)
  {
    this.household = household;
    state = new AttributeSet(this, this, this);
    state.Rescale(size);
    state.SetEffectMultiplier(size);
    state.AbilitiesChanged += UpdateAbilities;
    // Add field attributes to set.
    foreach (var attributeType in AttributeType.groups[fieldAttributeGroup])
    {
      state.Add(attributeType);
    }
    // Add the weather as a scoped set.
    state.AddScopedSet(WeatherAttributes.GetWeather());
    this._lastAdvanceTick = Calendar.Ticks;
  }

  // Advance the current state of the field to the current tick.
  public void Advance()
  {
    lock (_lock)
    {
      if (Calendar.Ticks == _lastAdvanceTick) return;
      _lastAdvanceTick = Calendar.Ticks;
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
    lock (_lock)
    {
      Advance();
      if (itemType.cropSettings == null)
      {
        // Can't plant something that isn't a crop.
        return false;
      }
      if (quantity <= minPlantQuantity) {
        // Refuse to plant tiny areas.
        return false;
      }
      if (quantity + cropCount > size)
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
      cropCount += quantity;
      return true;
    }
  }

  public bool Remove(ItemType itemType, double quantity)
  {
    lock (_lock)
    {
      Advance();
      if (!_crops.ContainsKey(itemType) || _crops[itemType].quantity < quantity)
      {
        // Can't harvest what isn't planted.
        return false;
      }
      cropCount -= quantity;
      if (_crops[itemType].quantity - quantity <= 0)
      {
        // Remove the crop.
        _crops.Remove(itemType);
        return true;
      }
      _crops[itemType].quantity -= quantity;
      return true;
    }
  }

  public void RemoveAll()
  {
    // Remove all the crops.
    lock (_lock)
    {
      _crops.Clear();
      cropCount = 0;
    }
  }

  public double GetCropCanopyUtilization()
  {
    lock (_lock)
    {
      double canopyUtilization = 0;
      foreach (var crop in _crops.Values)
      {
        canopyUtilization += crop.GetCropCanopyUtilization() * crop.GetCropCoverage();
      }
      return canopyUtilization;
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

  public double GetUnscaledValue(ItemType itemType, AttributeType attributeType)
  {
    lock (_lock)
    {
      if (!_crops.ContainsKey(itemType))
      {
        return state.GetUnscaledValue(attributeType);
      }
      return _crops[itemType].state.GetUnscaledValue(attributeType);
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
      field = parent;
      this.state = new AttributeSet(this, field, this);
      this.state.AddScopedSet(field.state);
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

    public double GetCropCoverage()
    {
      return quantity / field.size;
    }

    public double GetCropCanopyUtilization()
    {
      // Estimate what percentage of the field is being fully utilized by the crop,
      // such that the crop is getting the full benefit of the light and nutrients.
      // We assume that the crop starts at zero and goes to 20% during the first
      // initDays, then goes to 100% at the end of the devDays or weedSusceptibleDays,
      // whichever is longer.
      // Get the current day of the crop from the cropAttribute.
      var cropAttribute = itemType.cropSettings!.cropAttribute;
      var cropDay = state.GetUnscaledValue(cropAttribute!);
      var initDays = itemType.cropSettings!.initDays;
      var devDays = itemType.cropSettings!.devDays;
      var weedSusceptibleDays = itemType.cropSettings!.weedSusceptibleDays;
      var fullDays = System.Math.Max(devDays + initDays, weedSusceptibleDays);
      if (cropDay < initDays)
      {
        return cropDay / initDays * 0.2;
      }
      if (cropDay < fullDays)
      {
        return 0.2 + 0.8 * (cropDay - initDays) / (fullDays - initDays);
      }
      return 1.0;
    }

    public ItemType itemType;
    // Whenver the quantity changes we need to rescale the AttributeSet.
    private double _quantity;
    public double quantity
    {
      get
      {
        return _quantity;
      }
      set
      {
        _quantity = value;
        this.state.Rescale(quantity);
        this.state.SetEffectMultiplier(value);
      }
    }
    public AttributeSet state;
    public Field field;

    public Dictionary<AbilityType, HashSet<IAbilityProvider>> AbilityProviders => throw new NotImplementedException();

    // Crop abilities the parent field's abilities.
    // Note that this means that any abilites added by the crop's AttributeSet
    // will be ignored.
    public HashSet<AbilityType> Abilities => field.Abilities;

    public Household household => field.household;

    // Not Currently Used.
    public event AbilitiesChanged? AbilitiesChanged { add { } remove { } }

    public void GrantAbility(AbilityType ability)
    {
      throw new NotImplementedException();
    }

    public double? GetNamedValue(string name)
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

  // Readonly accessor for the crops.
  public IReadOnlyDictionary<ItemType, CropInfo> crops { get { return _crops; } }

  // The current number of crops in the field.
  public double cropCount { get; private set; } = 0;

  // The size of the field
  // In acres, 1 acre = 4 rods by 40 rods = 80 furrows each 1 furlong long.
  // 1 rod = 5.5 yards = 16.5 feet
  // One skilled guy with a good plow and a single ox can plow 1 acre in a day.
  public double size { get; private set; } = 1.0;

  public void Resize(double size)
  {
    lock (_lock)
    {
      Advance();
      this.size = size;
      state.Rescale(size);
      state.SetEffectMultiplier(size);
    }
  }

  // When was the last time the attributes were advanced.
  private long _lastAdvanceTick = 0;

  // AttributeSet to track the current state of the field.
  public AttributeSet state;

  public double? GetNamedValue(string name)
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
    Advance();
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
    Advance();
    return state.AddAttribute(attributeType, value);
  }

  public double AddAttribute(AttributeType attributeType)
  {
    Advance();
    return state.AddAttribute(attributeType);
  }
}