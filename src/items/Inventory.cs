using Village.Abilities;

namespace Village.Items;


public interface IInventoryContext
{
  public void AddItem(Item item, int quantity);
  public bool RemoveItem(Item item, int quantity);
  Dictionary<AbilityType, List<Item>> ItemAbilities();
}

// An Inventory is a collection of items, owned by a person, building, trader, village, etc.
public class Inventory : IInventoryContext
{
  // Default quantity for items that don't specify a quantity.
  public const int DEFAULT_QUANTITY = 1;

  public Inventory() { }

  // A lock to control access to the inventory.
  private readonly object _itemsLock = new object();

  // The items in the inventory with their quantities.
  // Items of the same type are sorted so that the "worst" items are used first.
  private Dictionary<ItemType, SortedDictionary<Item, int>> _items = new Dictionary<ItemType, SortedDictionary<Item, int>>();

  // Get a list of abilities granted by the items in the inventory.
  public Dictionary<AbilityType, List<Item>> ItemAbilities()
  {
    Dictionary<AbilityType, List<Item>> abilities = new Dictionary<AbilityType, List<Item>>();
    lock (_itemsLock)
    {
      foreach (ItemType itemType in _items.Keys)
      {
        // Add the itemType's abilities to the list.
        foreach (AbilityType ability in itemType.abilities)
        {
          if (!abilities.ContainsKey(ability))
          {
            abilities[ability] = new List<Item>();
          }
          // Add every item of that type to the list.
          foreach (Item item in _items[itemType].Keys)
          {
            abilities[ability].Add(item);
          }
        }
      }
    }
    return abilities;
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
  public void AddItem(Item item, int quantity)
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

  // Add itemTypes to the inventory.
  public void Add(Dictionary<ItemType, int> items)
  {
    lock (_itemsLock)
    {
      foreach (var itemType in items)
      {
        _AddNoLock(itemType.Key, itemType.Value);
      }
    }
  }

  // Remove an exact item quantity from the inventory.
  // Items are destroyed.
  public bool RemoveItem(Item item, int quantity)
  {
    lock (_itemsLock)
    {
      return _RemoveNoLock(item, quantity);
    }
  }

  // Remove itemTypes from the inventory, selecting the worst matching items first.
  // Items are destroyed.
  public bool Remove(Dictionary<ItemType, int> itemTypes)
  {
    if (itemTypes == null || itemTypes.Count == 0)
    {
      return true;
    }
    lock (_itemsLock)
    {
      return _RemoveNoLock(itemTypes);
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
        foreach (var item in itemType.Value)
        {
          count += item.Value;
        }
      }
      return count;
    }
  }

  // Count the number of unique items in the inventory.
  public int CountUnique()
  {
    lock (_itemsLock)
    {
      int count = 0;
      foreach (var itemType in _items)
      {
        count += itemType.Value.Count;
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
    if (items == null || items.Count == 0)
    {
      return true;
    }
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

  // Bracket operator to get the dicationary of a given ItemType in the inventory.
  public SortedDictionary<Item, int> this[ItemType itemType]
  {
    get
    {
      lock (_itemsLock)
      {
        if (_items.ContainsKey(itemType))
        {
          return _items[itemType];
        }
        return new SortedDictionary<Item, int>();
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

  // Internal function to remove itemTypes from the inventory with no locking.
  private bool _RemoveNoLock(Dictionary<ItemType, int> itemTypes)
  {
    // First ensure that the inventory contains the items.
    if (!_ContainsNoLock(itemTypes))
    {
      return false;
    }
    foreach (var itemType in itemTypes)
    {
      // Remove the items in sorted order so that the worst items go first.
      // Keep removing until we have removed the required quantity.
      int quantity = itemType.Value;
      while (quantity > 0 && _items[itemType.Key].Count > 0)
      {
        // Get the first item in the dictionary.
        var item = _items[itemType.Key].First();
        if (item.Value > quantity)
        {
          _items[itemType.Key][item.Key] -= quantity;
          quantity = 0;
          break;
        }
        else
        {
          quantity -= item.Value;
          _items[itemType.Key].Remove(item.Key);
          if (quantity == 0) break;
        }
      }

      if (_items[itemType.Key].Count == 0)
      {
        _items.Remove(itemType.Key);
      }
      // Throw an exception if we didn't remove the required quantity.
      if (quantity > 0)
      {
        // Should never happen, since we checked Contains above.
        throw new System.Exception("Inventory.Remove: Failed to remove the required quantity of items with itemType " + itemType.Key + ".");
      }
    }
    return true;
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
      _items.Add(item.itemType, new SortedDictionary<Item, int> { { item, quantity } });
    }
  }

  private void _AddNoLock(ItemType item, int quantity)
  {
    // Make a new item of the given type.
    var newItem = new Item(item);
    _AddNoLock(newItem, quantity);
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
