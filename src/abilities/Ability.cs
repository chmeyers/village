using Newtonsoft.Json;
using System.Runtime.CompilerServices;


namespace Village.Ability
{
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
        // Iterate over the levels.
        for (int level = 0; level < levels; level++) {
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
      // Set the sub types by copying the sub types of the lower level ability with the same name.
      // and adding the lower level ability to the list.
      this.subTypes = new List<string>();
      if (level != null && level > 0) {
        AbilityType? lowerLevelAbility = Find(GetAbilityName(abilityType, level - 1));
        if (lowerLevelAbility != null) {
          this.subTypes.AddRange(lowerLevelAbility.subTypes);
          this.subTypes.Add(lowerLevelAbility.abilityType);
        }
      }
      
    }

    // The name of the ability type.
    // This is the string used to refer to the ability in lists.
    public readonly string abilityType;

    // The name of the parent ability type.
    public readonly string parentType;

    // The list of lower level abilities that this ability also grants.
    public readonly List<string> subTypes;

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
}