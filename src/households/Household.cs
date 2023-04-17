// Households are container for multiple people and buildings.
// They have an inventory which is shared by all the people in the household.
// They also have a list of buildings that they own.
// People in the household have assigned roles, which determine what they do.
// They can typically access the household inventory, but do not gain abilities
// from the items in it. They can also access the buildings in the household.

using Village.Abilities;
using Village.Items;
using Village.Persons;

namespace Village.Households;

public class Household : IInventoryContext
{
  // The inventory of the household.
  public Inventory inventory { get; private set; }
  

  public Household()
  {
    inventory = new Inventory();
  }


  public void AddItem(Item item, int quantity)
  {
    inventory.AddItem(item, quantity);
  }

  public Dictionary<AbilityType, List<Item>> ItemAbilities()
  {
    // Items in a household inventory do not grant abilities, so return an empty dictionary.
    return new Dictionary<AbilityType, List<Item>>();
  }

  public bool RemoveItem(Item item, int quantity)
  {
    return inventory.RemoveItem(item, quantity);
  }

  public void Add(Dictionary<ItemType, int> items)
  {
    inventory.Add(items);
  }

  public bool Remove(Dictionary<ItemType, int> itemTypes)
  {
    return inventory.Remove(itemTypes);
  }
}
