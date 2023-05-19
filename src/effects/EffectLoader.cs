using Newtonsoft.Json;

namespace Village.Effects;

// This class exists to Load the effects from JSON without
// causing any circular dependencies.
public class EffectLoader : Effect
{
  // Loader function to load all effects from a JSON Dictionary.
  public static void Load(Dictionary<string, Dictionary<string, object>> data)
  {
    // Iterate over the effects.
    foreach (var effect in data)
    {
      // Get the effect name.
      string name = effect.Key;
      // Get the target setting from the Value
      string target = (string)effect.Value["target"];
      // Get the effect type setting from the Value
      EffectType effectType = (EffectType)Enum.Parse(typeof(EffectType), (string)effect.Value["effectType"]);

      EffectTargetType effectTarget = (EffectTargetType)Enum.Parse(typeof(EffectTargetType), target);

      // Get the subclass config dictionary setting from the Value
      Dictionary<string, object>? config =
        ((Newtonsoft.Json.Linq.JObject)effect.Value["config"]).ToObject<Dictionary<string, object>>();

      // Select the correct effect class based on the effect type.
      Effect newEffect;
      switch (effectType)
      {
        case EffectType.Degrade:
          // Create the effect.
          newEffect = new DegradeEffect(name, effectTarget, effectType, config);
          break;
        case EffectType.Skill:
          // Create the effect.
          newEffect = new SkillEffect(name, effectTarget, effectType, config);
          break;
        case EffectType.BuildingComponent:
          // Create the effect.
          newEffect = new BuildingComponentEffect(name, effectTarget, effectType, config);
          break;
        case EffectType.SkillTree:
          // Create the effect.
          newEffect = new SkillTreeEffect(name, effectTarget, effectType, config);
          break;
        case EffectType.AttributePuller:
          // Create the effect.
          newEffect = new AttributePullerEffect(name, effectTarget, effectType, config);
          break;
        case EffectType.AttributeTransfer:
          // Create the effect.
          newEffect = new AttributeTransferEffect(name, effectTarget, effectType, config);
          break;
        case EffectType.AttributeAdder:
          // Create the effect.
          newEffect = new AttributeAdderEffect(name, effectTarget, effectType, config);
          break;
        default:
          throw new Exception("Unknown effect type: " + effectType);
      }
      // Add the effect to the dictionary.
      _effects.Add(newEffect.effect, newEffect);
    }
  }

  // Loader function to load all effects from a JSON string.
  public static void LoadString(string json)
  {
    // Load the JSON into a dictionary.
    Dictionary<string, Dictionary<string, object>>? data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);
    if (data == null)
    {
      throw new Exception("Failed to load effects from string");
    }
    // Load the dictionary.
    Load(data);
  }

  // Loader function to load all effects from a JSON file.
  public static void LoadFile(string filename)
  {
    // Load the JSON file.
    string json = File.ReadAllText(filename);
    LoadString(json);
  }

  // Initialize is called after all the other types have been loaded.
  // This is used to resolve any references between effects and other types.
  public new static void Initialize()
  {
    // Call initialize on all the effects.
    foreach (var effect in _effects)
    {
      effect.Value.Initialize();
    }
  }

  // Constructor, which always throws an exception.
  public EffectLoader(string effect, EffectTargetType effectTarget, EffectType effectType, Dictionary<string, object>? config) : base(effect, effectTarget, effectType)
  {
    throw new Exception("EffectLoader constructor should never be called");
  }
}