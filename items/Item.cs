

namespace Item
{
  // A ItemType describes a general item and stores information
  // related to all items of that type.
  class ItemType
  {

    // Constructor
    public ItemType(string name, string group)
    {
      itemType = name;
      itemGroup = group;
    }

    // The name of the item type.
    // This is the string used to refer to the item in configuration files.
    public static string itemType;

    // Parent type of this item type.
    public static string itemGroup;

    // Localized/Pretty display name of the item type.
    public string displayName;
  }

  // An Item is a specific instance of an ItemType.
  class Item
  {

    // Constructor
    public Item(ItemType type)
    {
      itemType = type;
    }

    // The type of this item.
    public ItemType itemType;

    // The quality of this item.
    public int quality;

    // The quanity of this item.
    public int quanity;

    // The turn this item was made or first bought.
    public int turn_created;
  }
}