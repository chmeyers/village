
namespace Item
{

  // An Inventory is a collection of items, owned by a person, building, trader, village, etc.
  class Inventory
  {
    // Default quantity for items that don't specify a quantity.
    // Quantities are assumed to be specified in hundredths of a unit.
    const int DEFAULT_QUANTITY = 100;

    public Inventory() { }

    // A lock to control access to the inventory.
    private readonly object _itemsLock = new object();

    // The items in the inventory with their quantities.
    private Dictionary<Item, int> _items = new Dictionary<Item, int>();

    // Craft an item, using the specified inputs. Fails if the input items don't exist in the inventory.
    public bool Craft(ItemType output, Dictionary<Item, int> inputs)
    {
      // First lock and remove the input items from the inventory.
      lock (_itemsLock)
      {
        // First check that the items exist in the inventory.
        if (!_ContainsNoLock(inputs))
        {
          return false;
        }
        // Then remove them.
        foreach (var item in inputs)
        {
          // Ignore the return value since we already checked that the items exist.
          _RemoveNoLock(item.Key, item.Value);
        }
      }

      // Then add the output item to the inventory.
      Add(new Item(output), DEFAULT_QUANTITY);
      return true;
    }

    // Trade items between two inventories.
    // Fails without trading if the items don't exist in the correct inventory.
    public bool Trade(Inventory other, Dictionary<Item, int> myItems, Dictionary<Item, int> theirItems)
    {
      // To avoid the possibility of deadlocks, we avoid locking both inventories at the same time.
      // Instead, we remove myItems from this inventory, transfer theirItems to this inventory,
      // then add myItems to the other inventory. If any of these steps fail, we abort the trade and
      // add the items back to their original inventories.
      lock (_itemsLock)
      {
        // First check that the items exist in the inventory.
        if (!_ContainsNoLock(myItems))
        {
          return false;
        }
        // Then remove them.
        foreach (var item in myItems)
        {
          // Ignore the return value since we already checked that the items exist.
          _RemoveNoLock(item.Key, item.Value);
        }
      }
      if (!other.Transfer(this, theirItems))
      {
        // If the transfer failed, add the items back to this inventory.
        Add(myItems);
        return false;
      }
      other.Add(myItems);
      return true;
    }

    // Transfer items from this inventory to another inventory.
    // Fails without transferring if the items don't exist in this inventory.
    public bool Transfer(Inventory other, Dictionary<Item, int> items)
    {
      // First lock and remove the items from this inventory.
      lock (_itemsLock)
      {
        // First check that the items exist in the inventory.
        if (!_ContainsNoLock(items))
        {
          return false;
        }
        // Then remove them.
        foreach (var item in items)
        {
          // Ignore the return value since we already checked that the items exist.
          _RemoveNoLock(item.Key, item.Value);
        }
      }

      // Then lock and add the items to the other inventory.
      lock (other._itemsLock)
      {
        foreach (var item in items)
        {
          other._AddNoLock(item.Key, item.Value);
        }
      }
      return true;
    }


    // Add an item to the inventory.
    public void Add(Item item, int quantity)
    {
      lock (_itemsLock)
      {
        _AddNoLock(item, quantity);
      }
    }

    // Add items to the inventory.
    public void Add(Dictionary<Item, int> items)
    {
      lock (_itemsLock)
      {
        foreach (var item in items)
        {
          _AddNoLock(item.Key, item.Value);
        }
      }
    }

    // Remove an exact item quantity from the inventory.
    // Items are destroyed.
    public bool Remove(Item item, int quantity)
    {
      lock (_itemsLock)
      {
        return _RemoveNoLock(item, quantity);
      }
    }

    // Internal function to remove items from the inventory with no locking.
    private bool _RemoveNoLock(Item item, int quantity)
    {
      if (_items.ContainsKey(item))
      {
        if (_items[item] > quantity)
        {
          _items[item] -= quantity;
          return true;
        }
        else if (_items[item] == quantity)
        {
          _items.Remove(item);
          return true;
        }
      }
      return false;
    }

    // Internal function to add items to the inventory with no locking.
    private void _AddNoLock(Item item, int quantity)
    {
      if (_items.ContainsKey(item))
      {
        _items[item] += quantity;
      }
      else
      {
        _items.Add(item, quantity);
      }
    }

    // Internal function to check whether a given quantity of an item exists
    // in the inventory with no locking.
    private bool _ContainsNoLock(Item item, int quantity)
    {
      if (_items.ContainsKey(item))
      {
        return _items[item] >= quantity;
      }
      return false;
    }

    // Internal function to check whether a given quantity of all items exists
    // in the inventory with no locking.
    private bool _ContainsNoLock(Dictionary<Item, int> items)
    {
      foreach (var item in items)
      {
        if (!_ContainsNoLock(item.Key, item.Value))
        {
          return false;
        }
      }
      return true;
    }

  }
}