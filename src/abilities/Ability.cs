using Newtonsoft.Json;
using System.Runtime.CompilerServices;


namespace Village.Abilities;

// Event handler for when the abilities of a person change.
public delegate void AbilitiesChanged();

public class AbilityType
{
  // The null ability type with an empty name.
  public static AbilityType NULL = new AbilityType("", null);
  // A static dictionary of all ability types.
  // This is used to look up ability types by name.
  // It does not contain the NULL ability type.
  public static Dictionary<string, AbilityType> abilityTypes { get; private set;} = new Dictionary<string, AbilityType>();


  // Clear the types dictionary.
  public static void Clear()
  {
    abilityTypes.Clear();
  }

  // Find a AbilityType by name.
  public static AbilityType? Find(string name)
  {
    if (abilityTypes.ContainsKey(name)) {
      return abilityTypes[name];
    }
    return null;
  }

  // Loader function to load all ability types from a JSON Dictionary.
  public static void Load(Dictionary<string, Dictionary<string, object>> data)
  {
    // Iterate over the ability types.
    foreach (var ability in data) {
      // Get the ability type name.
      string name = ability.Key;
      // Check whether the ability type has a levels setting.
      if (ability.Value.ContainsKey("levels")) {
        int levels = (int)(long)ability.Value["levels"];
        // Iterate over the levels of the ability type.
        for (int level = 0; level < levels; level++)
        {
          // Create the ability type.
          AbilityType abilityType = new AbilityType(name, level);
          // Add the ability type to the dictionary.
          abilityTypes.Add(abilityType.abilityType, abilityType);
        }
      }
      else {
        // Create the ability type.
        AbilityType abilityType = new AbilityType(name, null);
        // Add the ability type to the dictionary.
        abilityTypes.Add(abilityType.abilityType, abilityType);
      }
    }
  }

  // Loader function to load all ability types from a JSON string.
  public static void LoadString(string json)
  {
    // Load the JSON into a dictionary.
    Dictionary<string, Dictionary<string, object>>? data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);
    if (data == null) {
      throw new Exception("Failed to load ability types from string");
    }
    // Load the dictionary.
    Load(data);
  }

  // Loader from a file.
  public static void LoadFile(string filename)
  {
    // Load the JSON from the file.
    string json = File.ReadAllText(filename);
    // Load the JSON.
    try {
      LoadString(json);
    }
    catch (Exception e) {
      throw new Exception("Failed to load ability types from file: " + filename + "\n" + e.Message);
    }
  }

  // Create a level specific ability name from a base ability name and a level.
  public static string GetAbilityName(string abilityName, int? level)
  {
    return abilityName + (level == null ? "" : "_" + level.ToString());
  }

  // Constructor for a ability type.
  public AbilityType(string abilityType, int? level)
  {
    // Name the ability based on the passed string concatenated with the level.
    this.abilityType = GetAbilityName(abilityType, level);
    // Set the parent type.
    this.parentType = abilityType;
    // Set the sub types by copying the sub types of the lower level ability with
    // the same name and adding the lower level ability to the list.
    this.subTypes = new List<AbilityType>();
    if (level != null) {
      AbilityType? parent = Find(GetAbilityName(abilityType, level - 1));
      if (parent != null) {
        this.subTypes.AddRange(parent.subTypes);
        this.subTypes.Add(parent);
      }
    }
    // Add this ability to the super types of all sub types.
    this.superTypes = new List<AbilityType>();
    foreach (AbilityType subType in this.subTypes) {
      subType.superTypes.Add(this);
    }
  }

  // Allow implicit conversion from string to AbilityType only
  // if the string is already a valid ability type.
  // This allows it to be used by unittests and any code that
  // has already validated the string, but ensures we aren't
  // accidentally creating untracked abilities.
  public static implicit operator AbilityType(string name)
  {
    if (abilityTypes.ContainsKey(name)) {
      return abilityTypes[name];
    }
    throw new Exception("Invalid ability type: " + name);
  }

  // The name of the ability type.
  // This is the string used to refer to the ability in lists.
  public readonly string abilityType;

  // The name of the parent ability type.
  public readonly string parentType;

// The list of lower level abilities that are granted by this ability.
  public readonly List<AbilityType> subTypes;

  // The list of higher level abilities that also grant this ability.
  public readonly List<AbilityType> superTypes;

  public override bool Equals(object? obj)
  {
    // Check equality of the abilityType only.
    if (obj is AbilityType other) {
      return other.abilityType == abilityType;
    }
    return false;
  }
  public override int GetHashCode()
  {
    // Hash is based only off of the abilityType since it is unique.
    return abilityType.GetHashCode();
  }
}

public interface IAbilityContext
{
  public HashSet<AbilityType> Abilities { get; }
  public void GrantAbility(AbilityType ability);
}

public class ConcreteAbilityContext: IAbilityContext
{
  private HashSet<AbilityType> _abilities = new HashSet<AbilityType>();
  public HashSet<AbilityType> Abilities { get { return _abilities; } }
  public ConcreteAbilityContext(HashSet<AbilityType> abilities)
  {
    this._abilities = abilities;
  }
  public void GrantAbility(AbilityType ability)
  {
    _abilities.Add(ability);
  }
  
}