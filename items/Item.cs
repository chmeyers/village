

namespace Item
{
  enum ItemGroup
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
  class ItemType
  {
    // Constructor
    public ItemType(string name, ItemGroup group, ItemType parent, string displayName,
                    string uxAsset, int spoilTime, int lossRate, bool flammable,
                    Dictionary<ItemType, int> scrapItems)
    {
      itemType = name;
      itemGroup = group;
      parentType = parent;
      this.displayName = displayName;
      this.uxAsset = uxAsset;
      this.spoilTime = spoilTime;
      this.lossRate = lossRate;
      this.flammable = flammable;
      this.scrapItems = scrapItems;
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
    public readonly Dictionary<ItemType, int>? scrapItems;

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
  class Item
  {

    // Constructor
    public Item(ItemType type)
    {
      itemType = type;
      originalQuality = 100;
      quality = 100;
      timeUntilSpoilage = type.spoilTime;
      if (timeUntilSpoilage == 0) {
        timeUntilSpoilage = int.MaxValue;
      }
    }

    // The type of this item.
    public ItemType itemType;

    // The unique name of this item, or null if it is not a unique item.
    public string? uniqueName;

    // The original quality of this item.
    public int originalQuality;
    // The quality of this item, in hundredths of a unit.
    public int quality;

    // How many hundredths of a turns until this item spoils, or MAX_INT if it never spoils.
    public int timeUntilSpoilage;
  }
}