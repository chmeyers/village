// A Building is a structure that can be built by a household.
// Buildings are owned by a household, and can be used by the people in the household.
// Buildings provide abilities to the people in the household, and to the household itself.
using Newtonsoft.Json;
using Village.Abilities;
using Village.Base;
using Village.Tasks;

namespace Village.Buildings;


// A Building Phase is a named set of tasks that must be completed before
// the next phase can be started.
using BuildingPhase = KeyValuePair<string,HashSet<BuildingComponent>>;

public class BuildingType
{
  public const string BUILDING_PHASE_ABILITY_PREFIX = "construction_phase_";
  public const string BUILDING_REPAIR_ABILITY_PREFIX = "repair_";
  // The dictionary of all the building types.
  public static Dictionary<string, BuildingType> buildingTypes { get; private set;} = new Dictionary<string, BuildingType>();

  // Clear the dictionary of building types.
  public static void Clear()
  {
    buildingTypes.Clear();
  }

  // Find a building type by name.
  public static BuildingType? Find(string name)
  {
    if (buildingTypes.ContainsKey(name))
    {
      return buildingTypes[name];
    }
    return null;
  }

  // Constructor from JSON dictionary.
  public BuildingType(string name, Dictionary<string, object> data)
  {
    this.name = name;
    abilities = new HashSet<AbilityType>();
    requirements = new HashSet<AbilityType>();
    phases = new List<BuildingPhase>();
    if (data.ContainsKey("abilities"))
    {
      List<string>? abilityList = ((Newtonsoft.Json.Linq.JArray)data["abilities"]).ToObject<List<string>>();
      foreach (var ability in abilityList!)
      {
        // Check that the ability is valid
        var x = AbilityType.Find((string)ability);
        if (x == null)
        {
          throw new Exception($"Ability {ability} not found for building {name}");
        }

        abilities.Add(x);
        abilities.UnionWith(x.subTypes);
      }
    }
    if (data.ContainsKey("requirements"))
    {
      List<string>? requirementList = ((Newtonsoft.Json.Linq.JArray)data["requirements"]).ToObject<List<string>>();
      foreach (var requirement in requirementList!)
      {
        // Check that the requirement is an valid ability.
        var x = AbilityType.Find((string)requirement);
        if (x == null)
        {
          throw new Exception($"Ability {requirement} not found for building {name}");
        }

        requirements.Add(x);
      }
    }
    if (data.ContainsKey("phases"))
    {
      Dictionary<string, List<string>>? phaseList = ((Newtonsoft.Json.Linq.JObject)data["phases"]).ToObject<Dictionary<string, List<string>>>();
      foreach (var phase in phaseList!)
      {
        var buildingPhaseAbility = AbilityType.Find(BUILDING_PHASE_ABILITY_PREFIX + phase.Key);
        if (buildingPhaseAbility == null)
        {
          throw new Exception($"Ability {BUILDING_PHASE_ABILITY_PREFIX + phase.Key} not found for building {name}");
        }
        // Verify that there are tasks that require the building phase ability.
        if (!WorkTask.tasksByAbility.ContainsKey(buildingPhaseAbility.abilityType))
        {
          throw new Exception($"No tasks found that require ability {BUILDING_PHASE_ABILITY_PREFIX + phase.Key} for building {name}");
        }
        var tasks = WorkTask.tasksByAbility[buildingPhaseAbility.abilityType];
        // Verify that there is at least one WorkTask that provides each component
        // that requires the building phase ability.
        var allComponents = new HashSet<BuildingComponent>();
        var components = new HashSet<BuildingComponent>();
        foreach (var component in phase.Value)
        {
          var buildingComponent = new BuildingComponent((string)component);
          // Check that the component is not already seen.
          // TODO(chmeyers): Allow multiples of the same component?
          if (allComponents.Contains(buildingComponent))
          {
            throw new Exception($"Duplicate component {buildingComponent.name} for building {name}");
          }
          components.Add(buildingComponent);
          allComponents.Add(buildingComponent);
          if (WorkTask.tasksByComponent[buildingComponent.name].Count == 0)
          {
            throw new Exception($"No tasks found that provide component {buildingComponent.name} for building {name} phase {phase.Key}");
          }
        }
        phases.Add(new BuildingPhase(phase.Key, components));
      }
    }
    if (data.ContainsKey("landarea"))
    {
      landArea = (int)data["landarea"];
    }
    if (data.ContainsKey("usesPerYear"))
    {
      usesPerYear = AbilityValue.FromJson(data["usesPerYear"]);
    }
    else {
      usesPerYear = new AbilityValue(Calendar.weeksPerYear);
    }
  }

  // Loader function to load all the building types from a JSON Dictionary.
  public static void Load(Dictionary<string, Dictionary<string, object>> data)
  {
    foreach (var building in data)
    {
      var name = building.Key;
      var buildingType = new BuildingType(name, building.Value);
      buildingTypes.Add(name, buildingType);
    }
  }

  // Loader function to load all the building types from a JSON string.
  public static void LoadString(string json)
  {
    // Load the JSON into a dictionary.
    Dictionary<string, Dictionary<string, object>>? data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);
    if (data == null)
    {
      throw new Exception("Failed to load attribute types from string");
    }
    // Load the dictionary.
    Load(data);
  }

  // Loader from a file.
  public static void LoadFile(string filename)
  {
    // Load the JSON into a dictionary.
    string json = File.ReadAllText(filename);
    LoadString(json);
  }

  public IEnumerable<string> RequiredComponents()
  {
    foreach (var phase in phases)
    {
      foreach (var component in phase.Value)
      {
        yield return component.name;
      }
    }
  }

  // The id of the building type.
  public string name { get; private set; }
  // The abilities that the building type provides once complete.
  public HashSet<AbilityType> abilities { get; private set; }
  // The abilities required to build the building type.
  public HashSet<AbilityType> requirements { get; private set; }
  // The components required to build the building type.
  // A Building is not built until all the components are completed.
  // The components are grouped into phases, and each phase must be
  // completed before the next phase can be started.
  // If the phases are empty, then the building is built immediately.
  public List<BuildingPhase> phases { get; private set; }
  // Land area required to build the building type.
  public int landArea { get; private set; }
  // The number of times per year the building is expected to be used
  // per year. This is used to calculate the expected utility of constructing
  // the building.
  public AbilityValue usesPerYear { get; private set; }
}

// A Building is a structure that can be built by a household.
// Buildings are owned by a household, and can be used by the people in the household.
// Buildings provide abilities to the people in the household, and to the household itself.
public class Building : IAbilityCollection
{
  public const string BUILDING_COMPLETE_PHASE = "building_complete";
  // Constructor.
  public Building(BuildingType buildingType)
  {
    this.buildingType = buildingType;
    this.phase = 0;
    ChangeBuildingAbilities();
  }

  // The building type.
  public BuildingType buildingType { get; private set; }
  // The current phase of the building.
  private int phase;
  // The current phase of the building.
  public string currentPhase {
    get
    {
      if (completed)
      {
        return BUILDING_COMPLETE_PHASE;
      }
      return buildingType.phases[phase].Key;
    }
  }
  // Whether the building is complete.
  public bool completed {
    get
    {
      return (buildingType.phases.Count == phase);
    }
  }
  // The components that have been completed.
  public HashSet<BuildingComponent> completedComponents { get; private set; } = new HashSet<BuildingComponent>();

  public bool broken { get; private set; } = false;
  public bool repairable { get; private set; } = false;

  public void ForceComplete()
  {
    phase = buildingType.phases.Count;
    ChangeBuildingAbilities();
  }

  public virtual event AbilitiesChanged? AbilitiesChanged;

  // Add a component to the building.
  // Returns true if the component was added, false if the component was already added.
  public bool AddComponent(BuildingComponent component)
  {
    lock(_lock)
    {
      Advance();
      if (completed)
      {
        return false;
      }
      if (buildingType.phases[phase].Value.Contains(component))
      {
        completedComponents.Add(component);
        if (completedComponents.IsSupersetOf(buildingType.phases[phase].Value))
        {
          phase++;
          ChangeBuildingAbilities();
        }
        return true;
      }
      return false;
    }
  }
  // Whether the building still needs the given component for the current phase.
  public bool NeedsComponent(BuildingComponent component)
  {
    if (completed)
    {
      return false;
    }
    return buildingType.phases[phase].Value.Contains(component) && !completedComponents.Contains(component);
  }

  public IEnumerable<string> MissingComponents()
  {
    if (completed)
    {
      yield break;
    }
    foreach (var component in buildingType.RequiredComponents())
    {
      if (!completedComponents.Contains(component))
      {
        yield return component;
      }
    }
  }

  // Replace a component in the building.
  // For example, a thatch roof can be replaced with a tile roof.
  public bool ReplaceComponent(BuildingComponent newComponent)
  {
    lock(_lock)
    {
      Advance();
      if (!completedComponents.Contains(newComponent))
      {
        return false;
      }
      // Components with the same name hash the same and are equal.
      // So to replace a component, we just remove and then re-add the new component.
      return completedComponents.Remove(newComponent) && completedComponents.Add(newComponent);
    }
  }
  // The abilities that the building provides.
  // During construction, the building provides the abilities of the current phase.
  // Once the building is complete, the building provides all the abilities of the building type.
  protected Dictionary<AbilityType, HashSet<IAbilityProvider>> _abilityProviders = new Dictionary<AbilityType, HashSet<IAbilityProvider>>();

  public virtual Dictionary<AbilityType, HashSet<IAbilityProvider>> AbilityProviders { get { return _abilityProviders; } }

  protected HashSet<AbilityType> _abilities = new HashSet<AbilityType>();

  public virtual HashSet<AbilityType> Abilities { get { return _abilities; } }

  private void ChangeBuildingAbilities()
  {
    // Save a copy of the existing abilities and ability providers.
    var oldAbilities = new HashSet<AbilityType>(_abilities);
    var oldAbilityProviders = new Dictionary<AbilityType, HashSet<IAbilityProvider>>(_abilityProviders);
    _abilities.Clear();
    _abilityProviders.Clear();
    if (completed && !broken)
    {
      Abilities.UnionWith(buildingType.abilities);
    }
    else if (!completed)
    {
      var phaseAbility = AbilityType.Find(BuildingType.BUILDING_PHASE_ABILITY_PREFIX + buildingType.phases[phase].Key)!;
      var abilities = new HashSet<AbilityType>() { phaseAbility };
      abilities.UnionWith(phaseAbility.subTypes);
      Abilities.UnionWith(abilities);
    }
    if (repairable)
    {
      foreach (var component in completedComponents)
      {
        if (component.currentQuality / component.builtQuality < repairableThreshold)
        {
          var repairAbility = AbilityType.Find(BuildingType.BUILDING_REPAIR_ABILITY_PREFIX + component.builtComponent)!;
          Abilities.Add(repairAbility);
        }
      }
    }
    // The AbilityProvider for all the abilities is the building itself.
    foreach (var ability in Abilities)
    {
      _abilityProviders[ability] = new HashSet<IAbilityProvider>() { this };
    }
    // Notify that the abilities have changed.
    AbilitiesChanged?.Invoke(this, Abilities.Except(oldAbilities), this, oldAbilities.Except(_abilities));
  }

  private long _lastAdvanceTick = 0;
  private object _lock = new object();
  public const double repairableThreshold = 0.2;
  public void Advance()
  {
    lock(_lock)
    {
      bool updateAbilities = false;
      if (Calendar.Ticks == _lastAdvanceTick) return;
      long ticks = Calendar.Ticks - _lastAdvanceTick;
      _lastAdvanceTick = Calendar.Ticks;
      // Degrade each component by the number of ticks since the last advance.
      foreach (var component in completedComponents)
      {
        if (component.currentQuality > 0)
        {
          component.currentQuality -= (int)ticks;
          if (component.currentQuality <= 0)
          {
            component.currentQuality = 0;
            broken = true;
            repairable = true;
            updateAbilities = true;
          }
          else if (component.currentQuality/component.builtQuality < repairableThreshold)
          {
            repairable = true;
            updateAbilities = true;
          }
        }
      }
      if (updateAbilities)
      {
        ChangeBuildingAbilities();
      }
    }
  }

  public override string ToString()
  {
    return buildingType.name;
  }
}