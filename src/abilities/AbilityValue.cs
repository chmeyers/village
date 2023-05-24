
using Newtonsoft.Json;

namespace Village.Abilities;




// An AbilityValue is a value that can be modified by the presence of abilities.
// The value is not concrete until the abilities are applied.
[JsonConverter(typeof(AbilityValueConverter))]
public class AbilityValue
{
  // The base value of the AbilityValue.
  public double baseValue;
  // The name if this is a named value type.
  public string? namedValue;
  // Minimum and maximum values.
  public double min = double.MinValue;
  public double max = double.MaxValue;
  // The abilities that affect the AbilityValue.
  private Dictionary<AbilityType, double> addAbilities = new Dictionary<AbilityType, double>();
  private Dictionary<AbilityType, double> multAbilities = new Dictionary<AbilityType, double>();
  private double namedAdd = 0;
  private double namedMult = 1;
  // Set of abilities that can affect the AbilityValue.
  public HashSet<AbilityType> Abilities
  {
    get
    {
      HashSet<AbilityType> abilities = new HashSet<AbilityType>();
      abilities.UnionWith(addAbilities.Keys);
      abilities.UnionWith(multAbilities.Keys);
      return abilities;
    }
  }
  // The AbilityValue constructor.
  public AbilityValue(double baseValue)
  {
    this.baseValue = baseValue;
  }

  // Copy constructor does a shallow copy, as the dictionaries are immutable.
  public AbilityValue(AbilityValue abilityValue)
  {
    this.baseValue = abilityValue.baseValue;
    this.min = abilityValue.min;
    this.max = abilityValue.max;
    this.addAbilities = abilityValue.addAbilities;
    this.multAbilities = abilityValue.multAbilities;
  }
  
  public AbilityValue(Newtonsoft.Json.Linq.JToken? json)
  {
    if (json == null)
    {
      this.baseValue = 0;
      return;
    }
    if (json.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
    {
      this.baseValue = (long)json;
      return;
    }
    if (json.Type == Newtonsoft.Json.Linq.JTokenType.Float)
    {
      this.baseValue = (double)json;
      return;
    }
    var dict = json.ToObject<Dictionary<string, object>>();
    if (dict == null)
    {
      throw new Exception("Failed to load ability value from json");
    }
    // Get the base value.
    if (dict.ContainsKey("val"))
    {
      // if val is a string, then this is a named value.
      if (dict["val"] is string)
      {
        this.namedValue = (string)dict["val"];
        this.baseValue = 0;
      }
      else
      {
        this.baseValue = AbilityValueConverter.GetDouble(dict, "val");
      }
    }
    // Get the min and max values.
    if (dict.ContainsKey("min"))
    {
      this.min = AbilityValueConverter.GetDouble(dict, "min");
    }
    if (dict.ContainsKey("max"))
    {
      this.max = AbilityValueConverter.GetDouble(dict, "max");
    }
    baseValue = Math.Clamp(baseValue, min, max);
    if (namedValue != null)
    {
      // Named values are allowed to have a general add and mult.
      if (dict.ContainsKey("add"))
      {
        this.namedAdd = AbilityValueConverter.GetDouble(dict, "add");
      }
      if (dict.ContainsKey("mult"))
      {
        this.namedMult = AbilityValueConverter.GetDouble(dict, "mult");
      }
    }
    // Get the abilities.
    Newtonsoft.Json.Linq.JObject? abilities = (Newtonsoft.Json.Linq.JObject?)json["modifiers"];
    if (abilities != null)
    {
      // Iterate over the abilities.
      foreach (var ability in abilities)
      {
        // Get the ability type.
        AbilityType? abilityType = AbilityType.Find(ability.Key);
        if (abilityType == null)
        {
          throw new Exception("Failed to find ability type: " + ability.Key);
        }
        if (ability.Value == null)
        {
          throw new Exception("Failed to find ability modifier value: " + ability.Key);
        }
        // Get the ability modifier.
        var abilityModifier = ability.Value.ToObject<Dictionary<string, object>>();
        if (abilityModifier == null)
        {
          throw new Exception("Failed to find ability modifier: " + ability.Key);
        }
        if (abilityModifier.ContainsKey("add"))
        {
          // Add might be either a long or a double.
          this.addAbilities.Add(abilityType, AbilityValueConverter.GetDouble(dict, "add"));
        }

        if (abilityModifier.ContainsKey("mult"))
        {
          // Multiplier might be either a long or a double.
          this.multAbilities.Add(abilityType, AbilityValueConverter.GetDouble(dict, "mult"));
        }

      }
    }
  }
  public static implicit operator AbilityValue(double x) 
  {
    return new AbilityValue((double)x);
  }

  public static implicit operator AbilityValue(Newtonsoft.Json.Linq.JToken x)
  {
    return new AbilityValue((double)x);
  }

  // Read AbilityValue from JSON.
  public static AbilityValue FromJson(object? input)
  {
    if (input == null)
    {
      throw new Exception("Failed to load ability value from null");
    }
    // If the JSON is just an int, set the base value to that.
    // Otherwise parse the JSON as a dictionary.
    if (input is long)
    {
      return new AbilityValue((long)input);
    }
    if (input is double)
    {
      return new AbilityValue((double)input);
    }
    if (input is not Newtonsoft.Json.Linq.JToken)
    {
      throw new Exception("Failed to load ability value from type: " + input.GetType().Name);
    }
    
    return new AbilityValue((Newtonsoft.Json.Linq.JToken)input);
  }

  private double GetNamedValue(IAbilityContext? context)
  {
    if (namedValue == null || context == null)
    {
      return baseValue;
    }
    return (context.GetNamedValue(namedValue) + namedAdd) * namedMult;
  }
  // Return the value of the AbilityValue.
  public double GetValue(IAbilityContext? context)
  {
    double namedValue = GetNamedValue(context);
    if (context == null || context.Abilities.Count == 0 || (addAbilities.Count == 0 && multAbilities.Count == 0))
    {
      return namedValue;
    }
    double value = namedValue;
    // Modify the base value for any abilities that are present in the context.
    // First do all the additions, then do all the multiplications.
    foreach (var ability in addAbilities)
    {
      if (context.Abilities.Contains(ability.Key))
      {
        value += ability.Value;
      }
    }
    foreach (var ability in multAbilities)
    {
      if (context.Abilities.Contains(ability.Key))
      {
        value *= ability.Value;
      }
    }
    // The return value is gated on the min and max values
    // and converted to an int.
    return Math.Clamp(value, min, max);
  }
  // Return the base value of the AbilityValue.
  public double GetBaseValue()
  {
    return baseValue;
  }
}

public class AbilityValueConverter : JsonConverter<AbilityValue>
{
  public override AbilityValue ReadJson(JsonReader reader, Type objectType, AbilityValue? existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var token = Newtonsoft.Json.Linq.JToken.ReadFrom(reader);
    return AbilityValue.FromJson(token);
  }

  public override bool CanWrite
  {
    get { return false; }
  }
  public override void WriteJson(JsonWriter writer, AbilityValue? value, JsonSerializer serializer)
  {
    throw new NotImplementedException();
  }

  public static double GetDouble(Dictionary<string, object>? dict, string name)
  {
    if (dict == null)
    {
      throw new Exception("Failed to find " + name + " in ability value");
    }
    if (!dict.ContainsKey(name))
    {
      throw new Exception("Failed to find " + name + " in ability value");
    }
    if (dict[name] is double)
    {
      return (double)dict[name];
    }
    if (dict[name] is long)
    {
      return (long)dict[name];
    }
    throw new Exception("Failed to find " + name + " in ability value");
  }
}