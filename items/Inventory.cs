
namespace Item {

  // An Inventory is a collection of items, owned by a person, building, trader, village, etc.
  class Inventory {
    public Inventory() {}

    private List<Item> items;

    // Craft an item, using the specified inputs. Fails if the input items don't exist in the inventory.
    public bool Craft(ItemType output, List<Item> inputs) {
      // Check that all inputs exist in the inventory.
      // TODO(chmeyers): This currently assumes that inputs are single quantity and not duplicated.
      foreach (Item input in inputs) {
        if (!items.Contains(input)) {
          return false;
        }
      }
      // Remove the inputs from the inventory.
      foreach (Item input in inputs) {
        items.Remove(input);
      }
      // Add the output to the inventory.
      items.Add(new Item(output));
      return true;
    }

    // Trade items between two inventories.
    // Fails without trading if the items don't exist in the correct inventory.
    // TODO(chmeyers): Deal with items that have quantities.
    public bool Trade(Inventory other, List<Item> myItems, List<Item> theirItems) {
      // Check that all myItems exist in the inventory.
      foreach (Item myItem in myItems) {
        if (!items.Contains(myItem)) {
          return false;
        }
      }

      // Check that all theirItems exist in the other inventory.
      foreach (Item theirItem in theirItems) {
        if (!other.items.Contains(theirItem)) {
          return false;
        }
      }

      // Remove myItems from the inventory and place them in the other inventory.
      foreach (Item myItem in myItems) {
        items.Remove(myItem);
        other.items.Add(myItem);
      }

      // Remove theirItems from the other inventory and place them in the inventory.
      foreach (Item theirItem in theirItems) {
        other.items.Remove(theirItem);
        items.Add(theirItem);
      }
      return true;
    }

    // Add an item to the inventory.
    public void Add(Item item) {
      items.Add(item);
    }

    // Add items to the inventory.
    public void Add(List<Item> items) {
      items.AddRange(items);
    }

    // Remove an item from the inventory.
    // Items are destroyed.
    public bool Remove(Item item) {
      return items.Remove(item);
    }

  }
}