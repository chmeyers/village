using Village.Abilities;

namespace Village.Items;


// An Inventory is a collection of items, owned by a person, building, trader, village, etc.
public class Inventory
{
  // Default quantity for items that don't specify a quantity.
  // Quantities are assumed to be specified in hundredths of a unit.
  public const int DEFAULT_QUANTITY = 100;

  public Inventory() { }

  // A lock to control access to the inventory.
  private readonly object _itemsLock = new object();

  // The items in the inventory with their quantities.
  private Dictionary<ItemType, Dictionary<Item, int>> _items = new Dictionary<ItemType, Dictionary<Item, int>>();

  // Get a list of abilities granted by the items in the inventory.
  public HashSet<AbilityType> GetAbilities()
  {
    HashSet<AbilityType> abilities = new HashSet<AbilityType>();
    lock (_itemsLock)
    {
      foreach (ItemType itemType in _items.Keys)
      {
        abilities.UnionWith(itemType.abilities);
      }
    }
    return abilities;
  }

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

  // Count the number of unique items in the inventory.
  public int Count()
  {
    lock (_itemsLock)
    {
      return _items.Count;
    }
  }

  // Count the total quantity of items in the inventory.
  public int CountAll()
  {
    lock (_itemsLock)
    {
      int count = 0;
      foreach (var itemType in _items)
      {
        foreach (var item in itemType.Value) {
          count += item.Value;
        }
      }
      return count;
    }
  }

  // Check whether a given item exists in the inventory, even if the quantity is zero.
  public bool Contains(Item item)
  {
    lock (_itemsLock)
    {
      return _ContainsNoLock(item, 0);
    }
  }

  // Check whether the given items exists in the inventory with at least the specified quantity.
  public bool Contains(Dictionary<ItemType, int> items)
  {
    lock (_itemsLock)
    {
      return _ContainsNoLock(items);
    }
  }

  // Bracket operator to get the quantity of an item in the inventory.
  public int this[Item item]
  {
    get
    {
      lock (_itemsLock)
      {
        if (_items.ContainsKey(item.itemType) && _items[item.itemType].ContainsKey(item))
        {
          return _items[item.itemType][item];
        }
        return 0;
      }
    }
  }
  

  // Internal function to remove items from the inventory with no locking.
  private bool _RemoveNoLock(Item item, int quantity)
  {
    if (_items.ContainsKey(item.itemType) && _items[item.itemType].ContainsKey(item))
    {
      
      if (_items[item.itemType][item] > quantity)
      {
        _items[item.itemType][item] -= quantity;
        return true;
      }
      else if (_items[item.itemType][item] == quantity)
      {
        _items[item.itemType].Remove(item);
        if (_items[item.itemType].Count == 0)
        {
          _items.Remove(item.itemType);
        }
        return true;
      }
    }
    return false;
  }

  // Internal function to add items to the inventory with no locking.
  private void _AddNoLock(Item item, int quantity)
  {
    if (_items.ContainsKey(item.itemType))
    {
      if (_items[item.itemType].ContainsKey(item))
      {
        _items[item.itemType][item] += quantity;
      }
      else
      {
        _items[item.itemType].Add(item, quantity);
      }
    }
    else
    {
      _items.Add(item.itemType, new Dictionary<Item, int> { { item, quantity } });
    }
  }

  // Internal function to check whether a given quantity of an item exists
  // in the inventory with no locking.
  private bool _ContainsNoLock(Item item, int quantity)
  {
    if (_items.ContainsKey(item.itemType) && _items[item.itemType].ContainsKey(item))
    {
      if (_items[item.itemType][item] >= quantity)
      {
        return true;
      }
    }
    return false;
  }

  // Internal function to check whether a given quantity of an itemType exists
  // in the inventory with no locking.
  private bool _ContainsNoLock(ItemType itemType, int quantity)
  {
    if (_items.ContainsKey(itemType))
    {
      int count = 0;
      foreach (var item in _items[itemType])
      {
        count += item.Value;
      }
      if (count >= quantity)
      {
        return true;
      }
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

  // Internal function to check whether a given quantity of all itemtypes exists
  // in the inventory with no locking.
  private bool _ContainsNoLock(Dictionary<ItemType, int> itemTypes)
  {
    foreach (var itemType in itemTypes)
    {
      if (!_ContainsNoLock(itemType.Key, itemType.Value))
      {
        return false;
      }
    }
    return true;
  }

}