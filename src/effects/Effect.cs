// Classes describing a side effect from a task.
// Effects can target Self, Persons, Items, Buildings, etc.
// Effects can be positive or negative, and can add or remove abilities, skills, experience, etc.
using Newtonsoft.Json;
using System;
using Village.Abilities;

namespace Village.Effects;


public enum EffectTargetType
{
  // A Person, who may or may not be the person performing the task.
  Person,
  // The entire Village, i.e. every Person.
  Village,
  // An Item in the inventory of the person performing the task, typically
  // the tool used to perform the task.
  Item,
  // A Building in the village.
  Building,
  // The Environment, i.e. how common resources are in the world.
  Environment,
}

public class EffectTarget
{
  // The type of target.
  public EffectTargetType effectTargetType;
  // The name of the target.
  public string target;
  // The effect target constructor.

  // Helper function to check whether is string is valid target string
  // that starts with an @ and is followed by a number.
  public static bool IsTargetString(string target)
  {
    return target.StartsWith("@") && int.TryParse(target.Substring(1), out int _);
  }
  public EffectTarget(EffectTargetType effectTargetType, string target)
  {
    this.effectTargetType = effectTargetType;
    // Check that the target is valid for the target type.
    // Village and Environment targets should always be self.
    // Person should either be empty or a target string.
    // Building/Item should be an ability name or a target string.
    // Target strings will be resolved by the task.
    switch (effectTargetType)
    {
      case EffectTargetType.Village:
      case EffectTargetType.Environment:
        if (target != "")
        {
          throw new Exception("Invalid target for " + effectTargetType + ": " + target);
        }
        break;
      case EffectTargetType.Person:
        if (target != "" && !IsTargetString(target))
        {
          throw new Exception("Invalid target for " + effectTargetType + ": " + target);
        }
        break;
      case EffectTargetType.Building:
      case EffectTargetType.Item:
        // Should be a empty or a valid ability name.
        if (AbilityType.Find(target) == null && !IsTargetString(target))
        {
          throw new Exception("Invalid target for " + effectTargetType + ": " + target);
        }
        break;
      default:
        throw new Exception("Unknown effect target type: " + effectTargetType);
    }
    this.target = target;
  }
}

// The type of effect. Each effect type will require a subclass of Effect.
public enum EffectType
{
  // Degrade an item, typically the tool used for the task.
  Degrade,
  // Increase a Person's skill level.
  Skill,
}

public class Effect
{
  // Dictionary to store the loaded Effects
  private static Dictionary<string, Effect> _effects = new Dictionary<string, Effect>();

  // Clear the effect dictionary.
  public static void Clear()
  {
    _effects.Clear();
  }
  // Readonly Accessor for the effect dictionary.
  public static IReadOnlyDictionary<string, Effect> effects
  {
    get { return _effects; }
  }

  // Find an Effect by name.
  public static Effect? Find(string name)
  {
    if (_effects.ContainsKey(name))
    {
      return _effects[name];
    }
    return null;
  }

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
    try
    {
      LoadString(json);
    }
    catch (Exception e)
    {
      throw new Exception("Failed to load effects from file: " + filename + "\n" + e.Message);
    }
  }

  public Effect(string effect, EffectTargetType target, EffectType effectType)
  {
    this.effect = effect;
    this.target = target;
    this.effectType = effectType;
  }
  // Unique name of the effect.
  public string effect;

  // The target type of the effect.
  public EffectTargetType target;

  // The type of the effect.
  public EffectType effectType;
}

// Degrade an item, typically the tool used for the task.
public class DegradeEffect : Effect
{
  public DegradeEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be an item.
    if (target != EffectTargetType.Item)
    {
      throw new Exception("Degrade effect must target an item: " + effect);
    }
    if (data == null)
    {
      throw new Exception("Degrade effect must have a config dictionary: " + effect);
    }
    // Get the degrade amount setting from the config
    amount = (int)(long)data["amount"];
  }
  // The amount to degrade the item by.
  public int amount;
}

// Increase a Person's skill level.
public class SkillEffect : Effect
{
  public SkillEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a person or self.
    if (target != EffectTargetType.Person)
    {
      throw new Exception("Skill effect must target a person: " + effect);
    }
    if (data == null)
    {
      throw new Exception("Skill effect must have a config dictionary: " + effect);
    }
    // Which Skill to increase.
    skill = (string)data["skill"];
    // How much to increase the skill by.
    amount = (int)(long)data["amount"];
    // The maximum level the skill can be increased to.
    maxLevel = (int)(long)data["maxLevel"];
  }
  // The name of the skill to increase.
  public string skill;
  // The amount to increase the skill by.
  public int amount;
  // The maximum level the skill can be increased to.
  public int maxLevel;
}

// namespace Village.Effect