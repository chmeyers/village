using Village.Abilities;

namespace Village.Items;


public interface IInventoryContext
{
  public Inventory inventory { get; }
}

// An Inventory is a collection of items, owned by a person, building, trader, village, etc.
public class Inventory : IInventoryContext
{
  // Default quantity for items that don't specify a quantity.
  public const int DEFAULT_QUANTITY = 1;

  public Inventory() { }

  public Inventory inventory => this;

  // A lock to control access to the inventory.
  private readonly object _itemsLock = new object();

  // The items in the inventory with their quantities.
  // Items of the same type are sorted so that the "worst" items are used first.
  public Dictionary<ItemType, SortedDictionary<Item, int>> items { get; private set;} = new Dictionary<ItemType, SortedDictionary<Item, int>>();

  // Event handler for when the abilities of a person change.
  public event AbilitiesChanged? AbilitiesChanged;

  // Get a list of abilities granted by the items in the inventory.
  public Dictionary<AbilityType, List<Item>> ItemAbilities()
  {
    Dictionary<AbilityType, List<Item>> abilities = new Dictionary<AbilityType, List<Item>>();
    lock (_itemsLock)
    {
      foreach (ItemType itemType in items.Keys)
      {
        // Add the itemType's abilities to the list.
        foreach (AbilityType ability in itemType.abilities)
        {
          if (!abilities.ContainsKey(ability))
          {
            abilities[ability] = new List<Item>();
          }
          // Add every item of that type to the list.
          foreach (Item item in items[itemType].Keys)
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
  public bool Trade(Inventory other, IDictionary<Item, int> myItems, IDictionary<Item, int> theirItems)
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
  public bool Transfer(Inventory other, IDictionary<Item, int> transferItems)
  {
    // First lock and remove the items from this inventory.
    lock (_itemsLock)
    {
      // First check that the items exist in the inventory.
      if (!_ContainsNoLock(transferItems))
      {
        return false;
      }
      // Then remove them.
      foreach (var item in transferItems)
      {
        // Ignore the return value since we already checked that the items exist.
        _RemoveNoLock(item.Key, item.Value);
      }
    }

    // Then lock and add the items to the other inventory.
    lock (other._itemsLock)
    {
      foreach (var item in transferItems)
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
  public void Add(IDictionary<Item, int> items)
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
  public bool RemoveItem(Item item, int quantity)
  {
    lock (_itemsLock)
    {
      return _RemoveNoLock(item, quantity);
    }
  }

  // Remove itemTypes from the inventory, selecting the worst matching items first.
  // Items are destroyed.
  public bool Remove(IDictionary<Item, int> items)
  {
    if (items == null || items.Count == 0)
    {
      return true;
    }
    lock (_itemsLock)
    {
      return _RemoveNoLock(items);
    }
  }

  public Dictionary<Item, int>? Get(IDictionary<ItemType, int> itemTypes)
  {
    if (itemTypes == null || itemTypes.Count == 0)
    {
      return new Dictionary<Item, int>();
    }
    lock (_itemsLock)
    {
      return _GetNoLock(itemTypes);
    }
  }

  // Get the a specific quantity of items of a given type.
  public Dictionary<Item, int>? Get(ItemType itemType, int quantity = DEFAULT_QUANTITY)
  {
    lock (_itemsLock)
    {
      return _GetNoLock(itemType, quantity);
    }
  }


  // Count the number of unique items in the inventory.
  public int Count()
  {
    lock (_itemsLock)
    {
      return items.Count;
    }
  }

  // Count the total quantity of items in the inventory.
  public int CountAll()
  {
    lock (_itemsLock)
    {
      int count = 0;
      foreach (var itemType in items)
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
      foreach (var itemType in items)
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

  // Check whether a given item exists in the inventory, even if the quantity is zero.
  public bool Contains(ItemType itemType)
  {
    lock (_itemsLock)
    {
      return _ContainsNoLock(itemType, 0);
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
        if (items.ContainsKey(item.itemType) && items[item.itemType].ContainsKey(item))
        {
          return items[item.itemType][item];
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
        if (items.ContainsKey(itemType))
        {
          return items[itemType];
        }
        return new SortedDictionary<Item, int>();
      }
    }
  }


  // Internal function to remove items from the inventory with no locking.
  private bool _RemoveNoLock(Item item, int quantity)
  {
    if (items.ContainsKey(item.itemType) && items[item.itemType].ContainsKey(item))
    {

      if (items[item.itemType][item] > quantity)
      {
        items[item.itemType][item] -= quantity;
        return true;
      }
      else if (items[item.itemType][item] == quantity)
      {
        items[item.itemType].Remove(item);
        if (items[item.itemType].Count == 0)
        {
          items.Remove(item.itemType);
          if (item.itemType.abilities.Count > 0)
          {
            AbilitiesChanged?.Invoke();
          }
        }
        return true;
      }
    }
    return false;
  }

  // Internal function to remove items from the inventory with no locking.
  private bool _RemoveNoLock(IDictionary<Item, int> items)
  {
    // First ensure that the inventory contains the items.
    if (!_ContainsNoLock(items))
    {
      return false;
    }
    foreach (var item in items)
    {
      if (!_RemoveNoLock(item.Key, item.Value))
      {
        // This should never happen since we already checked that the items exist.
        // If it does, it's likely due to calling this function without locking.
        throw new System.Exception("Inventory.Remove: Failed to remove the required quantity of items with itemType " + item.Key.itemType);
      }
    }
    return true;
  }

  // Internal function to add items to the inventory with no locking.
  private void _AddNoLock(Item item, int quantity)
  {
    if (items.ContainsKey(item.itemType))
    {
      if (items[item.itemType].ContainsKey(item))
      {
        items[item.itemType][item] += quantity;
      }
      else
      {
        items[item.itemType].Add(item, quantity);
        if (item.itemType.abilities.Count > 0)
        {
          // Not a new Ability, but the ItemAbilities lists need to change.
          AbilitiesChanged?.Invoke();
        }
      }
    }
    else
    {
      items.Add(item.itemType, new SortedDictionary<Item, int> { { item, quantity } });
      if (item.itemType.abilities.Count > 0)
      {
        AbilitiesChanged?.Invoke();
      }
    }
  }

  // Internal function to check whether a given quantity of an item exists
  // in the inventory with no locking.
  private bool _ContainsNoLock(Item item, int quantity)
  {
    if (items.ContainsKey(item.itemType) && items[item.itemType].ContainsKey(item))
    {
      if (items[item.itemType][item] >= quantity)
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
    if (items.ContainsKey(itemType))
    {
      int count = 0;
      foreach (var item in items[itemType])
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
  private bool _ContainsNoLock(IDictionary<Item, int> items)
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
  private bool _ContainsNoLock(IDictionary<ItemType, int> itemTypes)
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

  // Internal function to get the a specific quantity of a dictionary of itemTypes
  // in the inventory with no locking.
  private Dictionary<Item, int>? _GetNoLock(IDictionary<ItemType, int> itemTypes)
  {
    var contents = new Dictionary<Item, int>();
    foreach (var itemType in itemTypes)
    {
      if (!_GetNoLock(itemType.Key, itemType.Value, ref contents))
      {
        // Don't return partial contents if we don't have enough.
        return null;
      }
    }
    return contents;
  }


  // Internal function to get the a specific quantity of items of a given type
  // in the inventory with no locking.
  private Dictionary<Item, int>? _GetNoLock(ItemType itemType, int quantity)
  {
    var contents = new Dictionary<Item, int>();
    if (_GetNoLock(itemType, quantity, ref contents))
    {
      return contents;
    }
    return null;
  }

  private bool _GetNoLock(ItemType itemType, int quantity, ref Dictionary<Item, int> contents)
  {
    if (items.ContainsKey(itemType))
    {
      foreach (var item in items[itemType])
      {
        if (item.Value >= quantity)
        {
          contents.Add(item.Key, quantity);
          return true;
        }
        else
        {
          contents.Add(item.Key, item.Value);
          quantity -= item.Value;
        }
      }
    }
    return false;
  }
}
