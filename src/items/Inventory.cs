using Village.Abilities;

namespace Village.Items;


public interface IInventoryContext
{
  public Inventory inventory { get; }
}

public class InventoryEntry : SortedDictionary<Item, int>
{
  public InventoryEntry() { }
  
  public override string ToString()
  {
    // return the total quantity of items in the entry.
    // for ease of debugging.
    int total = 0;
    foreach (var item in this)
    {
      total += item.Value;
    }
    return total.ToString();
  }
}

// An Inventory is a collection of items, owned by a person, building, trader, village, etc.
public class Inventory : IInventoryContext, IAbilityCollection
{
  // Default quantity for items that don't specify a quantity.
  public const int DEFAULT_QUANTITY = 1;

  public Inventory() { }

  public Inventory inventory => this;

  // A lock to control access to the inventory.
  private readonly object _itemsLock = new object();

  // The items in the inventory with their quantities.
  // Items of the same type are sorted so that the "worst" items are used first.
  public Dictionary<ItemType, InventoryEntry> items { get; private set; } = new Dictionary<ItemType, InventoryEntry>();

  // Event handler for when the abilities of a person change.
  public event AbilitiesChanged? AbilitiesChanged;

  private void _RefreshAbilityProviders() {
    lock (_itemsLock) {
      // Clear the ability providers.
      _abilityProviders.Clear();
      // Loop through the items and add their abilities to the dictionary.
      foreach (var itemType in items.Keys)
      {
        foreach (var ability in itemType.abilities) 
        {
          if (!_abilityProviders.ContainsKey(ability))
          {
            _abilityProviders[ability] = new HashSet<IAbilityProvider>();
          }
          foreach (var item in items[itemType].Keys)
          {
            _abilityProviders[ability].Add(item);
          }
        }
      }
    }
  }
  private Dictionary<AbilityType, HashSet<IAbilityProvider>> _abilityProviders = new Dictionary<AbilityType, HashSet<IAbilityProvider>>();

  // Get a list of abilities granted by the items in the inventory.
  public Dictionary<AbilityType, HashSet<IAbilityProvider>> AbilityProviders { get { return _abilityProviders; } }

  private HashSet<AbilityType> _abilities = new HashSet<AbilityType>();

  public HashSet<AbilityType> Abilities { get { return _abilities; } }

  // Trade items between two inventories.
  // Fails without trading if the items don't exist in the correct inventory.
  public bool Trade(Inventory other, IEnumerable<KeyValuePair<Item, int>> myItems, IEnumerable<KeyValuePair<Item, int>> theirItems)
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
  public bool Transfer(Inventory other, IEnumerable<KeyValuePair<Item, int>> transferItems)
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

  // Return the largest quantity multiplier for the given item types
  // that can be satisfied by a positive integer number of items in the inventory.
  // For correct behavior, child types should be listed before parent types.
  public double GetMaxScale(IEnumerable<KeyValuePair<ItemType, int>> list)
  {
    lock (_itemsLock)
    {
      bool hasParent = false;
      int maxMultiplier = 1;
      // for each item in the list, we have to find the max scale that works,
      // then take the minimum of those.
      double maxScale = double.MaxValue;
      foreach (var itemType in list)
      {
        int multiplier = itemType.Value;
        if (multiplier <= 0)
        {
          // If the multiplier is 0, we don't use this item type.
          continue;
        }
        maxMultiplier = Math.Max(maxMultiplier, multiplier);
        // If this item is a parent type, we need to add the multipliers of
        // all descendant types that are in the list, so that we don't try
        // to count the child type twice.
        if (itemType.Key.childTypes.Count > 0)
        {
          hasParent = true;
          foreach (var other in list)
          {
            if (other.Key.IsDescendentOf(itemType.Key))
            {
              multiplier += other.Value;
            }
          }
        }
        // Get the count of items of this type and divide by the multiplier.
        maxScale = Math.Min(maxScale,_CountNoLock(itemType.Key) / (double)multiplier);
      }
      if (!hasParent)
      {
        // If there are no parent types, we can return the exact answer.
        return maxScale;
      }
      // I strongly suspect that getting the exact answer with parent/child types
      // is NP-hard, probably some version of the knapsack problem. It's made more
      // difficult by the fact that we use integers for inventory quantities, so
      // we can't split an item between a parent need and a child.
      // Multiple parents are worse since we will have counted the child multiple times
      // above for each parent.
      // However, maxScale is a strict upper bound on the answer, so we can binary search
      // to find the exact answer using Contains().

      // First check if the exact answer works. The correct answer will always be
      // a multiple of 1/maxMultiplier, so round down to the nearest multiple.
      maxScale = Math.Floor(maxScale * maxMultiplier) / maxMultiplier;
      if (_ScaledContains(list, maxScale))
      {
        return maxScale;
      }
      // Binary search. Use 1/maxMultiplier as the step size.
      double minScale = 0;
      while (maxScale - minScale > 1.0 / maxMultiplier)
      {
        double midScale = (maxScale + minScale) / 2;
        if (_ScaledContains(list, midScale))
        {
          minScale = midScale;
        }
        else
        {
          maxScale = midScale;
        }
      }
      return minScale;
    }
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
  public void Add(IEnumerable<KeyValuePair<Item, int>> items)
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

  // Get the a specific quantity of items of a given type.
  // For correct behavior, child types should be listed before parent types.
  public Dictionary<Item, int>? Get(IEnumerable<KeyValuePair<ItemType, int>> itemTypes)
  {
    if (itemTypes == null || itemTypes.Count() == 0)
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

  public Dictionary<Item, int>? GetExact(IEnumerable<KeyValuePair<ItemType, int>> itemTypes)
  {
    if (itemTypes == null || itemTypes.Count() == 0)
    {
      return new Dictionary<Item, int>();
    }
    lock (_itemsLock)
    {
      return _GetExactNoLock(itemTypes);
    }
  }

  // Get the a specific quantity of items of a given type.
  public Dictionary<Item, int>? GetExact(ItemType itemType, int quantity = DEFAULT_QUANTITY)
  {
    lock (_itemsLock)
    {
      return _GetExactNoLock(itemType, quantity);
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

  // Count the number of items of a given type in the inventory, including child types.
  public int Count(ItemType itemType)
  {
    lock (_itemsLock)
    {
      return _CountNoLock(itemType);
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
      int quantity = 0;
      return _ContainsNoLock(itemType, ref quantity);
    }
  }

  // Check whether a exact given itemtype exists in the inventory with at least.
  // Does not count child itemtypes.
  public bool ContainsExact(ItemType itemType)
  {
    lock (_itemsLock)
    {
      return _ContainsExactNoLock(itemType, 0);
    }
  }

  // Check whether the given items exists in the inventory with at least the specified quantity.
  // For correct behavior, child types should be listed before parent types.
  public bool Contains(IEnumerable<KeyValuePair<ItemType, int>> items)
  {
    if (items == null || items.Count() == 0)
    {
      return true;
    }
    lock (_itemsLock)
    {
      return _ContainsNoLock(items);
    }
  }

  // Check whether the exact given itemtypes exist in the inventory with at least
  // the specified quantity. Does not count child itemtypes.
  public bool ContainsExact(IEnumerable<KeyValuePair<ItemType, int>> items)
  {
    if (items == null || items.Count() == 0)
    {
      return true;
    }
    lock (_itemsLock)
    {
      return _ContainsExactNoLock(items);
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

  // Bracket operator to get the dictionary of a given ItemType in the inventory.
  // Return only the items of the given type, not child types.
  public InventoryEntry this[ItemType itemType]
  {
    get
    {
      lock (_itemsLock)
      {
        if (items.ContainsKey(itemType))
        {
          return items[itemType];
        }
        return new InventoryEntry();
      }
    }
  }


  // Internal function to count an itemType in the inventory with no locking.
  // Checks both the passed type and all descendant types.
  private int _CountNoLock(ItemType itemType)
  {
    int count = 0;
    // Count the passed itemType, then if it has children, count them too.
    if (items.ContainsKey(itemType))
    {
      foreach (var item in items[itemType])
      {
        count += item.Value;
      }
    }
    // Recurse through the children.
    foreach (var child in itemType.childTypes)
    {
      count += _CountNoLock(child);
    }
    return count;
  }

  private bool _RemoveAll(Item item)
  {
    if(!items[item.itemType].Remove(item))
    {
      return false;
    }
    if (item.itemType.abilities.Count > 0)
    {
      IAbilityCollection.UpdateAbilities(ref _abilityProviders, ref _abilities, null, null, item, item.itemType.abilities, AbilitiesChanged);
    }
    if (items[item.itemType].Count == 0)
    {
      items.Remove(item.itemType);
      if (item.itemType.abilities.Count > 0)
      {
        // If this was the last item with a given ability, remove the entry from the dictionary.
        // and fire the AbilityChanged event.
        IAbilityCollection.UpdateAbilities(ref _abilityProviders, ref _abilities, null, null, item, item.itemType.abilities, AbilitiesChanged);
      }
    }
    return true;
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
        _RemoveAll(item);
        return true;
      }
    }
    return false;
  }

  // Internal function to remove items from the inventory with no locking.
  private bool _RemoveNoLock(IEnumerable<KeyValuePair<Item, int>> items)
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
          IAbilityCollection.UpdateAbilities(ref _abilityProviders, ref _abilities, item, item.itemType.abilities, null, null, AbilitiesChanged);
        }
      }
    }
    else
    {
      items.Add(item.itemType, new InventoryEntry { { item, quantity } });
      if (item.itemType.abilities.Count > 0)
      {
        IAbilityCollection.UpdateAbilities(ref _abilityProviders, ref _abilities, item, item.itemType.abilities, null, null, AbilitiesChanged);
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
  // Checks both the passed type and all descendant types.
  private bool _ContainsNoLock(ItemType itemType, ref int quantity)
  {
    // Count the passed itemType, then if it has children, count them too.
    if (items.ContainsKey(itemType))
    {
      foreach (var item in items[itemType])
      {
        quantity -= item.Value;
        // Note that we only return true if we found at least one item.
        // So even if the passed quantity is zero, we still need to check.
        if (quantity <= 0)
        {
          return true;
        }
      }
    }
    // Recurse through the children.
    foreach (var child in itemType.childTypes)
    {
      if (_ContainsNoLock(child, ref quantity))
      {
        return true;
      }
    }
    return false;
  }


  // Internal function to check whether a given quantity of an itemType exists
  // in the inventory with no locking.
  // Does not check for children types.
  private bool _ContainsExactNoLock(ItemType itemType, int quantity)
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
  private bool _ContainsNoLock(IEnumerable<KeyValuePair<Item, int>> items)
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
  private bool _ContainsNoLock(IEnumerable<KeyValuePair<ItemType, int>> itemTypes)
  {
    // Use the _GetNoLock function as we need to deal with the case that the
    // passed itemTypes contains related child/parent types.
    // This requires tracking what items we've already counted.
    return _GetNoLock(itemTypes) != null;
  }

  private bool _ContainsExactNoLock(IEnumerable<KeyValuePair<ItemType, int>> itemTypes)
  {
    foreach (var itemType in itemTypes)
    {
      if (!_ContainsExactNoLock(itemType.Key, itemType.Value))
      {
        return false;
      }
    }
    return true;
  }

  // Internal function to get the a specific quantity of a dictionary of itemTypes
  // in the inventory with no locking.
  private Dictionary<Item, int>? _GetExactNoLock(IEnumerable<KeyValuePair<ItemType, int>> itemTypes)
  {
    var contents = new Dictionary<Item, int>();
    foreach (var itemType in itemTypes)
    {
      if (!_GetExactNoLock(itemType.Key, itemType.Value, ref contents))
      {
        // Don't return partial contents if we don't have enough.
        return null;
      }
    }
    return contents;
  }


  // Internal function to get the a specific quantity of items of a given type
  // in the inventory with no locking.
  private Dictionary<Item, int>? _GetExactNoLock(ItemType itemType, int quantity)
  {
    var contents = new Dictionary<Item, int>();
    if (_GetExactNoLock(itemType, quantity, ref contents))
    {
      return contents;
    }
    return null;
  }

  private bool _GetExactNoLock(ItemType itemType, int quantity, ref Dictionary<Item, int> contents)
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

  // Get including child types.
  private Dictionary<Item, int>? _GetNoLock(IEnumerable<KeyValuePair<ItemType, int>> itemTypes)
  {
    var contents = new Dictionary<Item, int>();
    foreach (var itemType in itemTypes)
    {
      int quantity = itemType.Value;
      if (!_MergeGet(itemType.Key, ref quantity, ref contents))
      {
        // Don't return partial contents if we don't have enough.
        return null;
      }
    }
    return contents;
  }

  private bool _ScaledContains(IEnumerable<KeyValuePair<ItemType, int>> itemTypes, double scale)
  {
    var contents = new Dictionary<Item, int>();
    foreach (var itemType in itemTypes)
    {
      int quantity = (int)Math.Ceiling(itemType.Value * scale);
      if (!_MergeGet(itemType.Key, ref quantity, ref contents))
      {
        // Don't return partial contents if we don't have enough.
        return false;
      }
    }
    return true;
  }

  private Dictionary<Item, int>? _GetNoLock(ItemType itemType, int quantity)
  {
    var contents = new Dictionary<Item, int>();
    if (_MergeGet(itemType, ref quantity, ref contents))
    {
      return contents;
    }
    return null;
  }

  private bool _GetNoMerge(ItemType itemType, ref int quantity, ref Dictionary<Item, int> contents)
  {
    if (items.ContainsKey(itemType))
    {
      foreach (var item in items[itemType])
      {
        // The item might already be in the contents dictionary if it's a child type,
        // in that case we skip over it unless there are more items of this type than
        // we've already counted.
        // Note that this shouldn't happen if the list of itemtypes was properly sorted,
        // as anything that would hit this should instead be using the _MergeGet function.
        if (contents.ContainsKey(item.Key))
        {
          if (contents[item.Key] < item.Value)
          {
            if (item.Value - contents[item.Key] >= quantity)
            {
              contents[item.Key] += quantity;
              return true;
            }
            else
            {
              quantity -= item.Value - contents[item.Key];
              contents[item.Key] = item.Value;
            }
          }
        }
        else if (item.Value >= quantity)
        {
          contents[item.Key] = quantity;
          return true;
        }
        else
        {
          contents[item.Key] = item.Value;
          quantity -= item.Value;
        }
      }
    }
    return false;
  }

  // Get items in the inventory with no locking by merging the sorted dictionaries
  // of each itemType and maintaining the correct merged ordering.
  private bool _MergeGet(ItemType itemType, ref int quantity, ref Dictionary<Item, int> contents)
  {
    // Get the descendants of the itemType.
    var descendants = itemType.GetAllDescendants();
    // If this is a child type, just use the simple get function.
    if (descendants.Count == 0)
    {
      return _GetNoMerge(itemType, ref quantity, ref contents);
    }
    // Add the itemType itself to the list.
    descendants.Add(itemType);
    // Lookup each of the itemTypes in the inventory.
    var itemTypes = new List<InventoryEntry>();
    var enumerators = new List<InventoryEntry.Enumerator>();
    // We could merge all the dictionaries into a single dictionary but that would
    // require sorting the entire set of items. Instead we pull the items out of
    // the dictionaries in the correct order until we have enough.
    // Store an enumerator for each itemType dictionary.
    foreach (var descendant in descendants)
    {
      if (items.ContainsKey(descendant))
      {
        itemTypes.Add(items[descendant]);
        // For some reason the Enumerator doesn't work if I add it directly to the list.
        // It was always returning null items, even after MoveNext, but this works.
        // The Internet was of no help. 
        var e = itemTypes[itemTypes.Count - 1].GetEnumerator();
        if (e.MoveNext())
        {
          enumerators.Add(e);
        }
      }
    }

    // Loop until we have enough items or we run out of items.
    while (quantity > 0)
    {
      // Find the enumerator with the min item.
      int minIndex = -1;
      Item? minItem = null;
      foreach (var enumerator in enumerators)
      {
        if (minItem == null || enumerator.Current.Key.CompareTo(minItem) < 0)
        {
          minItem = enumerator.Current.Key!;
          minIndex = enumerators.IndexOf(enumerator);
        }
      }
      // If we didn't find a min item, we've exhausted all the enumerators.
      if (minItem == null)
      {
        return false;
      }
      int minItemQuantity = enumerators[minIndex].Current.Value;
      // Add the min item to the contents.
      if (contents.ContainsKey(minItem))
      {
        // We already have some of this item.
        if (contents[minItem] < minItemQuantity)
        {
          if (minItemQuantity - contents[minItem] >= quantity)
          {
            // We have enough of this item.
            contents[minItem] += quantity;
            return true;
          }
          else
          {
            // Take all of the remaining items of this type.
            quantity -= minItemQuantity - contents[minItem];
            contents[minItem] = minItemQuantity;
          }
        }
      }
      else
      {
        // We don't have any of this item.
        if (minItemQuantity >= quantity)
        {
          // We have enough of this item.
          contents[minItem] = quantity;
          return true;
        }
        else
        {
          // Take all of the items of this type.
          quantity -= minItemQuantity;
          contents[minItem] = minItemQuantity;
        }
      }
      // Move the enumerator forward.
      if (!enumerators[minIndex].MoveNext())
      {
        // We've exhausted this enumerator, remove it.
        enumerators.RemoveAt(minIndex);
      }
    }
    return false;
  }
}
