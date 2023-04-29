// Households are container for multiple people and buildings.
// They have an inventory which is shared by all the people in the household.
// They also have a list of buildings that they own.
// People in the household have assigned roles, which determine what they do.
// They can typically access the household inventory, but do not gain abilities
// from the items in it. They can also access the buildings in the household.

using Village.Abilities;
using Village.Buildings;
using Village.Items;

namespace Village.Households;

public class Household : IInventoryContext
{
  // The inventory of the household.
  public Inventory inventory { get; private set; }
  
  // The buildings owned by the household.
  // TODO(chmeyers): protect with a lock.
  public List<Building> buildings { get; private set; }

  public int this[Item item] => ((IInventoryContext)inventory)[item];

  // Event handler for when the abilities of a person change.
  public event AbilitiesChanged? AbilitiesChanged;

  private bool _abilitiesDirty = true;

  // Cache of the abilities granted by the buildings in the household.
  private Dictionary<AbilityType, List<Building>> _buildingAbilities = new Dictionary<AbilityType, List<Building>>();

  public Household()
  {
    inventory = new Inventory();
    buildings = new List<Building>();
  }

  public Dictionary<AbilityType, List<Building>> BuildingAbilities()
  {
    if (!_abilitiesDirty)
    {
      return _buildingAbilities;
    }
    // loop through the buildings and add their abilities to the dictionary.
    _buildingAbilities.Clear();
    foreach (var building in buildings)
    {
      foreach (var ability in building.abilities)
      {
        if (!_buildingAbilities.ContainsKey(ability))
        {
          _buildingAbilities[ability] = new List<Building>();
        }
        _buildingAbilities[ability].Add(building);
      }
    }
    _abilitiesDirty = false;
    return _buildingAbilities;
  }

  public void AddBuilding(BuildingType buildingType)
  {
    Building building = new Building(buildingType);
    building.AbilitiesChanged += BuildingAbilitiesChanged;
    buildings.Add(building);
    if (building.abilities.Count > 0)
    {
      BuildingAbilitiesChanged();
    }
  }

  private void BuildingAbilitiesChanged()
  {
    _abilitiesDirty = true;
    AbilitiesChanged?.Invoke();
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

  public void Add(IDictionary<ItemType, int> items)
  {
    inventory.Add(items);
  }

  public bool Remove(IDictionary<ItemType, int> itemTypes)
  {
    return inventory.Remove(itemTypes);
  }
}
