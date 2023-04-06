using Newtonsoft.Json;
using Village.Abilities;

namespace Village.Items;

public enum ItemGroup
{
  // Currency is a special type of item that can be used to buy and sell other items.
  CURRENCY,
  // A resource is a raw material that can be used to craft other items.
  RESOURCE,
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


// A ItemType describes a general item and stores information
// related to all items of that type.
public class ItemType
{
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
    if (_itemTypes.ContainsKey(name)) {
      return _itemTypes[name];
    }
    return null;
  }

  // Loader function to load all item types from a JSON Dictionary.
  public static void Load(Dictionary<string, Dictionary<string, object>> data)
  {
    // Iterate over the item types.
    foreach (var item in data) {
      // Get the item type name.
      string name = item.Key;
      // Get the item type data.
      Dictionary<string, object> itemData = item.Value;
      // Get the item type group.
      ItemGroup group = (ItemGroup)Enum.Parse(typeof(ItemGroup), (string)itemData["group"]);
      // Get the item type parent.
      ItemType? parent = null;
      if (itemData.ContainsKey("parent")) {
        // If parent isn't already in the dictionary, throw an error.
        if (!_itemTypes.ContainsKey((string)itemData["parent"])) {
          throw new Exception("Parent item type not found: " + (string)itemData["parent"] + " for item type: " + name);
        }
        parent = Find((string)itemData["parent"]);
      }
      // Get the item type display name.
      string displayName = (string)itemData.GetValueOrDefault("displayName", name);
      // Get the item type UX asset.
      string uxAsset = (string)itemData.GetValueOrDefault("uxAsset", "");
      // Get the item type spoil time.
      int spoilTime = (int)(long)itemData.GetValueOrDefault("spoilTime", 0L);
      // Get the item type loss rate.
      int lossRate = (int)(long)itemData.GetValueOrDefault("lossRate", 0L);
      // Get the item type flammability or default to false.
      bool flammable = (bool)itemData.GetValueOrDefault("flammable", false);
      
      // Get the item type scrap items.
      Dictionary<ItemType, int> scrapItems = new Dictionary<ItemType, int>();
      if (itemData.ContainsKey("scrapItems")) {
        // Check that the scrap items are a dictionary.
        if (!(itemData["scrapItems"] is Newtonsoft.Json.Linq.JObject)) {
          throw new Exception("Scrap items must be a dictionary for item type: " + name);
        }
        var dict = ((Newtonsoft.Json.Linq.JObject)itemData["scrapItems"]).ToObject<Dictionary<string, object>>();
        if (dict == null) {
          throw new Exception("Scrap items must not be null for item type: " + name);
        }
        foreach (var scrapItem in dict!) {
          // If the scrap item isn't already in the dictionary, throw an error.
          if (!_itemTypes.ContainsKey(scrapItem.Key)) {
            throw new Exception("Scrap item type not found: " + scrapItem.Key + " for item type: " + name);
          }
          scrapItems.Add(Find(scrapItem.Key)!, (int)(long)scrapItem.Value);
        }
      }
      // Get the list of ability strings from the ability json field.
      HashSet<AbilityType> abilitySet = new HashSet<AbilityType>();
      if (itemData.ContainsKey("abilities")) {
        // Check that the abilities are a list.
        if (!(itemData["abilities"] is Newtonsoft.Json.Linq.JArray)) {
          throw new Exception("Abilities must be a list for item type: " + name);
        }
        List<string>? abilities = ((Newtonsoft.Json.Linq.JArray)itemData["abilities"]).ToObject<List<string>>();
        if (abilities == null) {
          throw new Exception("Abilities must not be null for item type: " + name);
        }
        // Check that each ability in the list exists in the AbilityType dictionary.
        foreach (var ability in abilities!) {
          if (!AbilityType.abilityTypes.ContainsKey(ability)) {
            throw new Exception("Ability not found: " + ability + " for item type: " + name);
          }
          abilitySet.Add(AbilityType.abilityTypes[ability]!);
        }
      }
      

      // Create the item type.
      ItemType itemType = new ItemType(name, group, parent, displayName, uxAsset, spoilTime, lossRate, flammable, scrapItems, abilitySet);
      // Add the item type to the dictionary.
      _itemTypes.Add(name, itemType);
    }
  }
  // Loader function to load all item types from a JSON string.
  public static void LoadString(string json)
  {
    // Parse the JSON string into a dictionary of item type names and data.
    Dictionary<string, Dictionary<string, object>>? data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);
    if (data == null) {
      throw new Exception("Failed to load item types from string");
    }
    Load(data);
  }
  // Loader function to load all item types from a JSON file.
  public static void LoadFile(string filename)
  {
    // Load the JSON file.
    string json = File.ReadAllText(filename);
    try {
      LoadString(json);
    }
    catch (Exception e) {
      throw new Exception("Failed to load item types from file: " + filename + "\n" + e.Message);
    }
  }


  // Constructor
  public ItemType(string name, ItemGroup group, ItemType? parent, string displayName,
                  string uxAsset, int spoilTime, int lossRate, bool flammable,
                  Dictionary<ItemType, int>? scrapItems, HashSet<AbilityType>? abilities)
  {
    itemType = name;
    itemGroup = group;
    parentType = parent;
    this.displayName = displayName;
    this.uxAsset = uxAsset;
    this.spoilTime = spoilTime;
    this.lossRate = lossRate;
    this.flammable = flammable;
    // If scrap items is null, set it to an empty dictionary.
    if (scrapItems == null) {
      this.scrapItems = new Dictionary<ItemType, int>();
    }
    else {
      this.scrapItems = scrapItems;
    }
    // If abilities is null, set it to an empty set.
    if (abilities == null) {
      this.abilities = new HashSet<AbilityType>();
    }
    else {
      this.abilities = abilities;
    }
  }

  // The name of the item type.
  // This is the string used to refer to the item in configuration files.
  public readonly string itemType;

  // Top level item group for this item type.
  public readonly ItemGroup itemGroup;

  // The parent type of this item type, or null if this is a root type.
  public readonly ItemType? parentType;

  // Localized/Pretty display name of the item type.
  public readonly string displayName;

  // Graphic asset used to display this item type.
  public readonly string uxAsset;

  // The time it takes an item of this type to spoil in hundredths of a turn, or zero if it never spoils.
  public readonly int spoilTime;

  // The default loss rate of an item of this type, or zero if it is not subject to loss.
  // Loss rate is the percentage of the item's quanity that is lost to vermin or other causes
  // each turn and may be modified by other factors, such as the building it is stored in.
  public readonly int lossRate;

  // Whether this item is flammable.
  // Nonflammable items will survive a fire, but flammable items will be turned to scrap.
  public readonly bool flammable;

  // What items and quantities this item will turn into if destroyed.
  // For example, a broken tool will turn into scrap metal.
  public readonly Dictionary<ItemType, int> scrapItems;

  // Set of abilities this item type provides.
  public readonly HashSet<AbilityType> abilities;

  public override bool Equals(object? obj)
  {
    // Check equality of the itemType only.
    if (obj is ItemType other) {
      return itemType == other.itemType;
    }
    return false;
  }
  public override int GetHashCode()
  {
    // Hash is based only off of the itemType since it is unique.
    return itemType.GetHashCode();
  }
}

// An Item is a specific instance of an ItemType.
public class Item : IComparable<Item>
{

  // Constructor
  public Item(ItemType type)
  {
    itemType = type;
    originalQuality = 100;
    quality = 100;
    timeUntilSpoilage = (int)type.spoilTime;
    if (timeUntilSpoilage == 0) {
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
    if (obj is Item other) {
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
    if (this == null) {
      if (other == null) {
        return 0;
      }
      return 1;
    }
    if (other == null) {
      return -1;
    }
    // Sort by time until spoilage.
    int timeCompare = this.timeUntilSpoilage.CompareTo(other.timeUntilSpoilage);
    if (timeCompare != 0) {
      return timeCompare;
    }
    // Sort by quality.
    int qualityCompare = this.quality.CompareTo(other.quality);
    if (qualityCompare != 0) {
      return qualityCompare;
    }
    // Sort by original quality.
    int originalQualityCompare = this.originalQuality.CompareTo(other.originalQuality);
    if (originalQualityCompare != 0) {
      return originalQualityCompare;
    }
    // Sort by unique name, nulls sort first.
    int uniqueNameCompare = 0;
    if (this.uniqueName != null) {
      if (other.uniqueName != null) {
        uniqueNameCompare = this.uniqueName.CompareTo(other.uniqueName);
      }
      else {
        uniqueNameCompare = 1;
      }
    }
    else if (other.uniqueName != null) {
      uniqueNameCompare = -1;
    }
    // Sort by item type.
    return this.itemType.itemType.CompareTo(other.itemType.itemType);
  }
}

