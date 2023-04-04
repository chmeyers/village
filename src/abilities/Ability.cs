using Newtonsoft.Json;
using System.Runtime.CompilerServices;


namespace Village.Abilities;

public class AbilityType
{
  // A static dictionary of all ability types.
  // This is used to look up ability types by name.
  private static Dictionary<string, AbilityType> _abilityTypes = new Dictionary<string, AbilityType>();

  // Clear the types dictionary.
  public static void Clear()
  {
    _abilityTypes.Clear();
  }
  // Readonly Accessor for the ability types dictionary.
  public static IReadOnlyDictionary<string, AbilityType> abilityTypes
  {
    get { return _abilityTypes; }
  }

  // Find a AbilityType by name.
  public static AbilityType? Find(string name)
  {
    if (_abilityTypes.ContainsKey(name)) {
      return _abilityTypes[name];
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
      // Get the levels setting from the Value
      int levels = (int)(long)ability.Value["levels"];
      // Iterate over the levels, starting at the highest level, so
      // that the higher level abilities can be added to the sub types.
      for (int level = levels - 1; level >= 0; level--) {
        // Create the ability type.
        AbilityType abilityType = new AbilityType(name, level);
        // Add the ability type to the dictionary.
        _abilityTypes.Add(abilityType.abilityType, abilityType);
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
    // Set the super types by copying the super types of the higher level ability with
    // the same name and adding the higher level ability to the list.
    this.superTypes = new List<string>();
    if (level != null) {
      AbilityType? parent = Find(GetAbilityName(abilityType, level + 1));
      if (parent != null) {
        this.superTypes.AddRange(parent.superTypes);
        this.superTypes.Add(parent.abilityType);
      }
    }
    
  }

  // The name of the ability type.
  // This is the string used to refer to the ability in lists.
  public readonly string abilityType;

  // The name of the parent ability type.
  public readonly string parentType;

  // The list of higher level abilities that also grant this ability.
  public readonly List<string> superTypes;

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