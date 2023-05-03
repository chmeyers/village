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

public interface IHouseholdContext
{
  // Returns which household the context is in.
  Household household { get; }
}

public class Household : IInventoryContext, IHouseholdContext
{
  // Registry of all the households.
  public static HashSet<Household> global_households = new HashSet<Household>();
  // The household that the context is in.
  public Household household => this;
  // Whether this is a player household.
  // Player households can be controlled by the player.
  public bool isPlayerHousehold { get; set; } = false;
  // The inventory of the household.
  public Inventory inventory { get; private set; }
  
  // The buildings owned by the household.
  // TODO(chmeyers): protect with a lock.
  public List<Building> buildings { get; private set; }

  // Event handler for when the abilities of a person change.
  public event AbilitiesChanged? AbilitiesChanged;

  private bool _abilitiesDirty = true;

  // Cache of the abilities granted by the buildings in the household.
  private Dictionary<AbilityType, List<Building>> _buildingAbilities = new Dictionary<AbilityType, List<Building>>();

  public Household(bool isPlayerHousehold = false)
  {
    inventory = new Inventory();
    buildings = new List<Building>();
    this.isPlayerHousehold = isPlayerHousehold;
    // Add the household to the global registry.
    global_households.Add(this);
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
}
