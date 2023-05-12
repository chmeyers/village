
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Village.Abilities;

namespace Village.Attributes;




// An AttributeValue is a value that can be modified by the presence of abilities.
// The value is not concrete until the abilities are applied.
[JsonConverter(typeof(AttributeValueConverter))]
public class AttributeValue
{
  // The base value of the AttributeValue.
  public int baseValue;
  // Minimum and maximum values.
  public int min = int.MinValue;
  public int max = int.MaxValue;
  // The abilities that affect the AttributeValue.
  private Dictionary<AbilityType, int> addAbilities = new Dictionary<AbilityType, int>();
  private Dictionary<AbilityType, float> multAbilities = new Dictionary<AbilityType, float>();
  // Set of abilities that can affect the AttributeValue.
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
  // The AttributeValue constructor.
  public AttributeValue(int baseValue)
  {
    this.baseValue = baseValue;
  }

  // Copy constructor does a shallow copy, as the dictionaries are immutable.
  public AttributeValue(AttributeValue abilityValue)
  {
    this.baseValue = abilityValue.baseValue;
    this.min = abilityValue.min;
    this.max = abilityValue.max;
    this.addAbilities = abilityValue.addAbilities;
    this.multAbilities = abilityValue.multAbilities;
  }

  public AttributeValue(Newtonsoft.Json.Linq.JToken? json)
  {
    if (json == null)
    {
      this.baseValue = 0;
      return;
    }
    if (json.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
    {
      this.baseValue = ((int)(long)json);
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
      this.baseValue = (int)(long)dict["val"];
    }
    // Get the min and max values.
    if (dict.ContainsKey("min"))
    {
      this.min = (int)(long)dict["min"];
    }
    if (dict.ContainsKey("max"))
    {
      this.max = (int)(long)dict["max"];
    }
    baseValue = Math.Clamp(baseValue, min, max);
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
          this.addAbilities.Add(abilityType, (int)(long)abilityModifier["add"]);
        }

        if (abilityModifier.ContainsKey("mult"))
        {
          // Multiplier might be either a long or a double.
          if (abilityModifier["mult"] is long)
          {
            this.multAbilities.Add(abilityType, (float)(long)abilityModifier["mult"]);
          }
          else
          {
            this.multAbilities.Add(abilityType, (float)(double)abilityModifier["mult"]);
          }
        }

      }
    }
  }
  public static implicit operator AttributeValue(long x)
  {
    return new AttributeValue((int)x);
  }

  public static implicit operator AttributeValue(Newtonsoft.Json.Linq.JToken x)
  {
    return new AttributeValue((int)x);
  }

  // Read AttributeValue from JSON.
  public static AttributeValue FromJson(object? input)
  {
    if (input == null)
    {
      throw new Exception("Failed to load ability value from null");
    }
    // If the JSON is just an int, set the base value to that.
    // Otherwise parse the JSON as a dictionary.
    if (input is long)
    {
      return new AttributeValue((int)(long)input);
    }
    if (input is not Newtonsoft.Json.Linq.JToken)
    {
      throw new Exception("Failed to load ability value from type: " + input.GetType().Name);
    }

    return new AttributeValue((Newtonsoft.Json.Linq.JToken)input);
  }

  // Return the value of the AttributeValue.
  public int GetValue(IAbilityContext? context)
  {
    if (context == null || context.Abilities.Count == 0 || (addAbilities.Count == 0 && multAbilities.Count == 0))
    {
      return baseValue;
    }
    double value = baseValue;
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
    return Math.Clamp((int)value, min, max);
  }
  // Return the base value of the AttributeValue.
  public int GetBaseValue()
  {
    return baseValue;
  }
}

public class AttributeValueConverter : JsonConverter<AttributeValue>
{
  public override AttributeValue ReadJson(JsonReader reader, Type objectType, AttributeValue? existingValue, bool hasExistingValue, JsonSerializer serializer)
  {
    var token = Newtonsoft.Json.Linq.JToken.ReadFrom(reader);
    return AttributeValue.FromJson(token);
  }

  public override bool CanWrite
  {
    get { return false; }
  }
  public override void WriteJson(JsonWriter writer, AttributeValue? value, JsonSerializer serializer)
  {
    throw new NotImplementedException();
  }
}