using Newtonsoft.Json;
using Village.Abilities;
using Village.Attributes;

namespace Village.Items;

public enum ItemGroup
{
  // Currency is a special type of item that can be used to buy and sell other items.
  CURRENCY,
  // A resource is a raw material that can be used to craft other items.
  RESOURCE,
  // Seeds including cuttings, bulbs, seedlings. Anything that can be planted.
  SEED,
  // Food or drink.
  FOOD,
  // A tool is an item that can be used to complete job tasks.
  TOOL,
  // A weapon for hunting or combat.
  WEAPON,
  // A piece of ammunition for a weapon.
  AMMO,
  // A piece of armor is an item that can be used to defend against attacks.
  ARMOR,
  // Clothing.
  CLOTHING,
  // Furniture or other decorative items.
  FURNITURE,
  // Household items, including linens and containers.
  HOUSEHOLD,
}

public class CropSettings
{
  // Settings describing how the crop grows. We give realistic defaults here,
  // but these should be overridden by the crop type.

  // The name of the crop attribute for this item type
  public string? cropAttributeName;
  // The crop attribute for this item type
  public AttributeType? cropAttribute;
  public double minSoilQuality = 2.0;
  // Temps are in degrees F.
  public double minPlantingTemp = 40.0;
  public double frostTolerance = 32.0;
  public double heatTolerance = 85.0;
  // Drought Tolerance - 0 == very poor, 1 == very good.
  public double droughtTolerance = 0.5;
  // Development stage days should probably coorespond to intervals in
  // the crop's attribute, but don't have to.
  public int initDays = 20;
  public int devDays = 30;
  public int midDays = 35;
  public int lateDays = 15;
  public int totalDays = 100;
  // Crop water use constants.
  // Water use is at kcInit for the initial days, rises to kcMid over devDays,
  // stays at kcMid for midDays, then drops to kcEnd over lateDays.
  // All values are multipliers of the ET (evapotranspiration) rate.
  public double kcInit = 0.3;
  public double kcMid = 1.05;
  public double kcEnd = 0.5;
  public double currentKC(int day)
  {
    if (day < initDays)
    {
      return kcInit;
    }
    if (day < initDays + devDays)
    {
      return kcInit + (kcMid - kcInit) * (day - initDays) / devDays;
    }
    if (day < initDays + devDays + midDays)
    {
      return kcMid;
    }
    if (day < initDays + devDays + midDays + lateDays)
    {
      return kcMid + (kcEnd - kcMid) * (day - initDays - devDays - midDays) / lateDays;
    }
    return kcEnd;
  }
  // How much yield does each tenth of an acre grow each tick.
  public double perTickYieldGrowth = 0.05;
  public double currentYieldGrowth(int day)
  {
    // TODO(chmeyers): This should be a non-linear function of the crop development stage.
    return perTickYieldGrowth;
  }
  // Recommended planting months in temperate climates.
  // Month zero is the beginning of spring.
  public List<int> temperatePlantingMonths = new List<int> { 1, 2 };
  // Target yield per acre in pounds.
  public double targetYield = 1000.0;
  // Seed needed per acre in pounds.
  public double seedPerAcre = 150.0;
  // Does this crop have harvestable straw?
  public bool hasHarvestableStraw = false;
  // Pounds of straw per pound of crop yield.
  public double strawPerYield = 0.0;
  // Nitrogen used per pound of crop yield.
  public double nitrogenPerYield = 1.0;
  // Phosphorus used per pound of crop yield.
  public double phosphorusPerYield = 0.05;
  // Potassium used per pound of crop yield.
  public double potassiumPerYield = 0.5;
  // Nitrogen per pound of straw.
  public double nitrogenPerStraw = 0.0;
  // Phosphorus per pound of straw.
  public double phosphorusPerStraw = 0.0;
  // Potassium per pound of straw.
  public double potassiumPerStraw = 0.0;
  // Total nitrogen.
  public double totalNitrogen => nitrogenPerYield + nitrogenPerStraw * strawPerYield;
  // Total phosphorus.
  public double totalPhosphorus => phosphorusPerYield + phosphorusPerStraw * strawPerYield;
  // Total potassium.
  public double totalPotassium => potassiumPerYield + potassiumPerStraw * strawPerYield;
  // Is this crop nitrogen fixing?
  // For nitrogen fixing crops, we will assume that a percentage of the nitrogen
  // used by the crop is fixed from the air. It doesn't actually go back into the
  // soil unless the crop is plowed under.
  public bool nitrogenFixing = false;
}

// A ItemType describes a general item and stores information
// related to all items of that type.
public class ItemType
{
  // Default Quality is 100.
  public const int DEFAULT_QUALITY = 100;
  // A static dictionary of all item types.
  // This is used to look up item types by name.
  private static Dictionary<string, ItemType> _itemTypes = new Dictionary<string, ItemType>();

  // Clear the item types dictionary.
  public static void Clear()
  {
    _itemTypes.Clear();
  }
  // Readonly Accessor for the item types dictionary.
  public static IReadOnlyDictionary<string, ItemType> itemTypes
  {
    get { return _itemTypes; }
  }

  // Find a ItemType by name.
  public static ItemType? Find(string name)
  {
    if (_itemTypes.ContainsKey(name))
    {
      return _itemTypes[name];
    }
    return null;
  }

  // Loader function to load all item types from a JSON Dictionary.
  public static void Load(Dictionary<string, Dictionary<string, object>> data)
  {
    // Iterate over the item types.
    foreach (var item in data)
    {
      // Get the item type name.
      string name = item.Key;
      // Get the item type data.
      Dictionary<string, object> itemData = item.Value;
      // Get the item type group.
      ItemGroup group = (ItemGroup)Enum.Parse(typeof(ItemGroup), (string)itemData["group"]);
      // Get the item type parents.
      List<ItemType> parents = new List<ItemType>();
      if (itemData.ContainsKey("parents"))
      {
        // Check that the parents is an array.
        if (!(itemData["parents"] is Newtonsoft.Json.Linq.JArray))
        {
          throw new Exception("Parents must be an array for item type: " + name);
        }
        List<string>? parentList = ((Newtonsoft.Json.Linq.JArray)itemData["parents"]).ToObject<List<string>>();
        if (parentList == null)
        {
          throw new Exception("Parents must not be null for item type: " + name);
        }
        // Check that each parent in the list exists in the ItemType dictionary.
        foreach (var parent in parentList!)
        {
          if (!_itemTypes.ContainsKey(parent))
          {
            throw new Exception("Parent item type not found: " + parent + " for item type: " + name);
          }
          parents.Add(_itemTypes[parent]!);
        }
      }
      // Get the item type spoil time.
      int spoilTime = (int)(long)itemData.GetValueOrDefault("spoilTime", 0L);
      // Get the item type loss rate.
      double lossRate = (double)itemData.GetValueOrDefault("lossRate", 0.0);
      // Get the item type flammability or default to false.
      bool flammable = (bool)itemData.GetValueOrDefault("flammable", false);

      // Get the item type scrap items.
      Dictionary<ItemType, int> scrapItems = new Dictionary<ItemType, int>();
      if (itemData.ContainsKey("scrapItems"))
      {
        // Check that the scrap items are a dictionary.
        if (!(itemData["scrapItems"] is Newtonsoft.Json.Linq.JObject))
        {
          throw new Exception("Scrap items must be a dictionary for item type: " + name);
        }
        var dict = ((Newtonsoft.Json.Linq.JObject)itemData["scrapItems"]).ToObject<Dictionary<string, object>>();
        if (dict == null)
        {
          throw new Exception("Scrap items must not be null for item type: " + name);
        }
        foreach (var scrapItem in dict!)
        {
          // If the scrap item isn't already in the dictionary, throw an error.
          if (!_itemTypes.ContainsKey(scrapItem.Key))
          {
            throw new Exception("Scrap item type not found: " + scrapItem.Key + " for item type: " + name);
          }
          scrapItems.Add(Find(scrapItem.Key)!, (int)(long)scrapItem.Value);
        }
      }
      // Get the list of ability strings from the ability json field.
      HashSet<AbilityType> abilitySet = new HashSet<AbilityType>();
      if (itemData.ContainsKey("abilities"))
      {
        // Check that the abilities are a list.
        if (!(itemData["abilities"] is Newtonsoft.Json.Linq.JArray))
        {
          throw new Exception("Abilities must be a list for item type: " + name);
        }
        List<string>? abilities = ((Newtonsoft.Json.Linq.JArray)itemData["abilities"]).ToObject<List<string>>();
        if (abilities == null)
        {
          throw new Exception("Abilities must not be null for item type: " + name);
        }
        // Check that each ability in the list exists in the AbilityType dictionary.
        foreach (var ability in abilities!)
        {
          if (!AbilityType.abilityTypes.ContainsKey(ability))
          {
            throw new Exception("Ability not found: " + ability + " for item type: " + name);
          }
          abilitySet.Add(AbilityType.abilityTypes[ability]!);
          // Add the subtype abilities to the set of abilities.
          abilitySet.UnionWith(AbilityType.abilityTypes[ability]!.subTypes);
        }
      }

      // Get the craft quality.
      AbilityValue craftQuality = new AbilityValue(DEFAULT_QUALITY);
      if (itemData.ContainsKey("craftQuality"))
      {
        // Load craft quality as an AbilityValue.
        craftQuality = AbilityValue.FromJson((Newtonsoft.Json.Linq.JObject)itemData["craftQuality"]);
      }

      // Get the Crop Settings.
      CropSettings? cropSettings = null;
      if (itemData.ContainsKey("cropAttribute"))
      {
        cropSettings = new CropSettings();
        cropSettings.cropAttributeName = (string)itemData["cropAttribute"];
      }

      // Read the harvest Items.
      Dictionary<string, double> harvestItems = new Dictionary<string, double>();
      if (itemData.ContainsKey("harvestItems"))
      {
        // Check that the harvest items are a dictionary.
        if (!(itemData["harvestItems"] is Newtonsoft.Json.Linq.JObject))
        {
          throw new Exception("Harvest items must be a dictionary for item type: " + name);
        }
        var dict = ((Newtonsoft.Json.Linq.JObject)itemData["harvestItems"]).ToObject<Dictionary<string, object>>();
        if (dict == null)
        {
          throw new Exception("Harvest items must not be null for item type: " + name);
        }
        foreach (var harvestItem in dict!)
        {
          // Allow the value to be a double or a long.
          if (harvestItem.Value is double)
          {
            harvestItems.Add(harvestItem.Key, (double)harvestItem.Value);
          }
          else if (harvestItem.Value is long)
          {
            harvestItems.Add(harvestItem.Key, (double)(long)harvestItem.Value);
          }
        }
      }


      // Create the item type.
      ItemType itemType = new ItemType(name, group, parents, spoilTime, lossRate, flammable, scrapItems, craftQuality, abilitySet, cropSettings, harvestItems);
      // Add the item type to the dictionary.
      _itemTypes.Add(name, itemType);
    }
  }
  // Loader function to load all item types from a JSON string.
  public static void LoadString(string json)
  {
    // Parse the JSON string into a dictionary of item type names and data.
    Dictionary<string, Dictionary<string, object>>? data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);
    if (data == null)
    {
      throw new Exception("Failed to load item types from string");
    }
    Load(data);
  }
  // Loader function to load all item types from a JSON file.
  public static void LoadFile(string filename)
  {
    // Load the JSON file.
    string json = File.ReadAllText(filename);
    LoadString(json);
  }


  // Constructor
  public ItemType(string name, ItemGroup group, List<ItemType>? parents, int spoilTime, double lossRate, bool flammable, Dictionary<ItemType, int>? scrapItems, AbilityValue? craftQuality, HashSet<AbilityType>? abilities, CropSettings? cropSettings, Dictionary<string, double>? harvestItems = null)
  {
    itemType = name;
    itemGroup = group;
    if (parents != null)
    {
      parentTypes = parents;
    }
    this.spoilTime = spoilTime;
    this.lossRate = lossRate;
    this.flammable = flammable;
    // If scrap items is null, set it to an empty dictionary.
    if (scrapItems == null)
    {
      this.scrapItems = new Dictionary<ItemType, int>();
    }
    else
    {
      this.scrapItems = scrapItems;
    }
    this.craftQuality = craftQuality ?? new AbilityValue(DEFAULT_QUALITY);
    // If abilities is null, set it to an empty set.
    if (abilities == null)
    {
      this.abilities = new HashSet<AbilityType>();
    }
    else
    {
      this.abilities = abilities;
    }
    if (cropSettings != null)
    {
      this.cropSettings = cropSettings;
    }
    // If harvest items is null, set it to an empty dictionary.
    this.harvestItems = new Dictionary<ItemType, double>();
    if (harvestItems != null)
    {
      // Look up the keys in the item type dictionary.
      // If the key is not found, throw an error, unless the key is for this item type.
      // in which case we point to ourselves.
      foreach (var harvestItem in harvestItems)
      {
        if (harvestItem.Key == itemType)
        {
          this.harvestItems.Add(this, harvestItem.Value);
        }
        else
        {
          if (!_itemTypes.ContainsKey(harvestItem.Key))
          {
            throw new Exception("Harvest item type not found: " + harvestItem.Key + " for item type: " + name);
          }
          this.harvestItems.Add(_itemTypes[harvestItem.Key]!, harvestItem.Value);
        }
      }
    }
    // Add this item type to the parent's child types.
    foreach (var parent in parentTypes)
    {
      parent.childTypes.Add(this);
    }
  }

  // Initialize is called after all effects and other types have been loaded.
  // This is used to resolve any references between items.
  public void Initialize() {
    // Resolve the crop attribute.
    if (cropSettings != null && cropSettings.cropAttributeName != null)
    {
      cropSettings.cropAttribute = AttributeType.Find(cropSettings.cropAttributeName);
      if (cropSettings.cropAttribute == null)
      {
        throw new Exception("Crop attribute not found: " + cropSettings.cropAttributeName + " for item type: " + itemType);
      }
    }
  }

  public static void InitializeAll()
  {
    foreach (var itemType in _itemTypes.Values)
    {
      itemType.Initialize();
    }
  }

  // The name of the item type.
  // This is the string used to refer to the item in configuration files.
  public readonly string itemType;

  // Top level item group for this item type.
  public readonly ItemGroup itemGroup;

  // The parent types of this item type, or null if this is a root type.
  public readonly List<ItemType> parentTypes = new List<ItemType>();

  // The child types of this item type, or null if this is a leaf type.
  public List<ItemType> childTypes { get; private set; } = new List<ItemType>();

  // The time it takes an item of this type to spoil in hundredths of a turn, or zero if it never spoils.
  public readonly int spoilTime;

  // The default loss rate of an item of this type, or zero if it is not subject to loss.
  // Loss rate is the percentage of the item's quanity that is lost to vermin or other causes
  // each month and may be modified by other factors, such as the building it is stored in.
  public readonly double lossRate;

  // Whether this item is flammable.
  // Nonflammable items will survive a fire, but flammable items will be turned to scrap.
  public readonly bool flammable;

  // What items and quantities this item will turn into if destroyed.
  // For example, a broken tool will turn into scrap metal.
  public readonly Dictionary<ItemType, int> scrapItems;

  // What Craft Quality this item has.
  public readonly AbilityValue craftQuality;

  // Set of abilities this item type provides.
  public readonly HashSet<AbilityType> abilities;

  // What items and quantities this item will turn into when harvested.
  // The quantity is a multiplier for the crop yield attribute.
  public readonly Dictionary<ItemType, double> harvestItems = new Dictionary<ItemType, double>();

  // The crop settings for this item type, or null if it is not a crop.
  public CropSettings? cropSettings = null;

  // Unit weight of this item type in pounds.
  // For aggregate items, the size of the unit may have a specific meaning.
  // For example, all food items are measured such that one unit is enough
  // to feed an average adult for one meal.
  // Weight will affect the cost of shipping.
  public double weight = 0.5;

  // How difficult is the item to ship, beyond what it's weight would indicate.
  // A high bulk value can indicate that the item is fragile or difficult to pack,
  // and force it to not be shipped.
  // For example, unfired pottery is difficult to ship.
  public double bulk = 1.0;

  public override bool Equals(object? obj)
  {
    // Check equality of the itemType only.
    if (obj is ItemType other)
    {
      return itemType == other.itemType;
    }
    return false;
  }
  public override int GetHashCode()
  {
    // Hash is based only off of the itemType since it is unique.
    return itemType.GetHashCode();
  }

  public bool IsDescendentOf(ItemType key)
  {
    // Check if this item type is a descendent of the given item type.
    foreach (var parent in parentTypes)
    {
      if (parent == key)
      {
        return true;
      }
      if (parent.IsDescendentOf(key))
      {
        return true;
      }
    }
    return false;
  }

  public HashSet<ItemType> GetAllDescendants()
  {
    // Get all descendents of this item type.
    HashSet<ItemType> descendents = new HashSet<ItemType>();
    foreach (var child in childTypes)
    {
      descendents.Add(child);
      descendents.UnionWith(child.GetAllDescendants());
    }
    return descendents;
  }
}

// An Item is a specific instance of an ItemType.
public class Item : IComparable<Item>, IAbilityProvider
{

  // Constructor
  public Item(ItemType type)
  {
    itemType = type;
    originalQuality = (int)type.craftQuality.GetBaseValue();
    quality = originalQuality;
    timeUntilSpoilage = (int)type.spoilTime;
    if (timeUntilSpoilage == 0)
    {
      timeUntilSpoilage = int.MaxValue;
    }
    uniqueName = null;
  }

  public Item(ItemType type, IAbilityContext crafter)
  {
    itemType = type;
    originalQuality = (int)type.craftQuality.GetValue(crafter);
    quality = originalQuality;
    timeUntilSpoilage = (int)type.spoilTime;
    if (timeUntilSpoilage == 0)
    {
      timeUntilSpoilage = int.MaxValue;
    }
    uniqueName = null;
  }

  // The type of this item.
  public ItemType itemType;

  // The unique name of this item, or null if it is not a unique item.
  public string? uniqueName;

  // The original quality of this item.
  public int originalQuality;
  // The quality of this item, in hundredths of a unit.
  public int quality;

  // How many tenths of a day until this item spoils, or MAX_INT if it never spoils.
  public int timeUntilSpoilage;

  public HashSet<AbilityType> Abilities => itemType.abilities;

  // Clone this item.
  public Item Clone()
  {
    Item clone = new Item(itemType);
    clone.uniqueName = uniqueName;
    clone.originalQuality = originalQuality;
    clone.quality = quality;
    clone.timeUntilSpoilage = timeUntilSpoilage;
    return clone;
  }

  // Equals function compares the items by value.
  public override bool Equals(object? obj)
  {
    if (obj is Item other)
    {
      return itemType == other.itemType &&
             uniqueName == other.uniqueName &&
             originalQuality == other.originalQuality &&
             quality == other.quality &&
             timeUntilSpoilage == other.timeUntilSpoilage;
    }
    return false;
  }

  // Hash function hashes the item by value.
  public override int GetHashCode()
  {
    return HashCode.Combine(itemType, uniqueName, originalQuality, quality, timeUntilSpoilage);
  }

  // Comparer for items that sorts lowest time to spoil first,
  // then lowest quality first, then lowest original quality first,
  // then by unique name, then by item type.
  public int CompareTo(Item? other)
  {
    // If either item is null, sort the other one first.
    if (this == null)
    {
      if (other == null)
      {
        return 0;
      }
      return 1;
    }
    if (other == null)
    {
      return -1;
    }
    // Sort by time until spoilage.
    int timeCompare = this.timeUntilSpoilage.CompareTo(other.timeUntilSpoilage);
    if (timeCompare != 0)
    {
      return timeCompare;
    }
    // Sort by quality.
    int qualityCompare = this.quality.CompareTo(other.quality);
    if (qualityCompare != 0)
    {
      return qualityCompare;
    }
    // Sort by original quality.
    int originalQualityCompare = this.originalQuality.CompareTo(other.originalQuality);
    if (originalQualityCompare != 0)
    {
      return originalQualityCompare;
    }
    // Sort by unique name, nulls sort first.
    int uniqueNameCompare = 0;
    if (this.uniqueName != null)
    {
      if (other.uniqueName != null)
      {
        uniqueNameCompare = this.uniqueName.CompareTo(other.uniqueName);
      }
      else
      {
        uniqueNameCompare = 1;
      }
    }
    else if (other.uniqueName != null)
    {
      uniqueNameCompare = -1;
    }
    if (uniqueNameCompare != 0)
    {
      return uniqueNameCompare;
    }
    // A parent sorts before it's children.
    if (this.itemType.IsDescendentOf(other.itemType))
    {
      return 1;
    }
    if (other.itemType.IsDescendentOf(this.itemType))
    {
      return -1;
    }
    // Sort by item type.
    return this.itemType.itemType.CompareTo(other.itemType.itemType);
  }
}

