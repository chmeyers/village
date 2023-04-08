
namespace Village.Abilities;




// An AbilityValue is a value that can be modified by the presence of abilities.
// The value is not concrete until the abilities are applied.
public class AbilityValue
{
  // The base value of the AbilityValue.
  public int baseValue;
  // The abilities that affect the AbilityValue.
  private Dictionary<AbilityType, int> addAbilities = new Dictionary<AbilityType, int>();
  private Dictionary<AbilityType, float> multAbilities = new Dictionary<AbilityType, float>();
  // The AbilityValue constructor.
  public AbilityValue(int baseValue)
  {
    this.baseValue = baseValue;
  }

  // Read AbilityValue from JSON.
  public static AbilityValue FromJson(object input)
  {
    if (input == null)
    {
      throw new Exception("Failed to load ability value from null");
    }
    // If the JSON is just an int, set the base value to that.
    // Otherwise parse the JSON as a dictionary.
    if (input is long)
    {
      return new AbilityValue((int)(long)input);
    }
    if (input is not Newtonsoft.Json.Linq.JToken)
    {
      throw new Exception("Failed to load ability value from type: " + input.GetType().Name);
    }
    Newtonsoft.Json.Linq.JToken json = (Newtonsoft.Json.Linq.JToken)input;
    if (json.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
    {
      return new AbilityValue((int)(long)json);
    }
    var dict = json.ToObject<Dictionary<string, object>>();
    if (dict == null)
    {
      throw new Exception("Failed to load ability value from json");
    }
    // Get the base value.
    int baseValue = (int)(long)dict["val"];
    // Create the AbilityValue.
    AbilityValue abilityValue = new AbilityValue(baseValue);
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
          abilityValue.addAbilities.Add(abilityType, (int)(long)abilityModifier["add"]);
        }
        
        if (abilityModifier.ContainsKey("mult"))
        {
          abilityValue.multAbilities.Add(abilityType, (float)(double)abilityModifier["mult"]);
        }
        
      }
    }
    // Return the ability value.
    return abilityValue;
  }

  // Return the value of the AbilityValue.
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
    // The return value is converted to an int.
    return (int)value;
  }
  // Return the base value of the AbilityValue.
  public int GetBaseValue()
  {
    return baseValue;
  }
}