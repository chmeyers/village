// Classes describing a side effect from a task.
// Effects can target Self, Persons, Items, Buildings, etc.
// Effects can be positive or negative, and can add or remove abilities, skills, experience, etc.
using Newtonsoft.Json;
using System;
using Village.Abilities;
using Village.Items;

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

// The ChosenEffectTarget represents an EffectTarget that has been resolved
// to a specific target.
public class ChosenEffectTarget
{
  // The type of target.
  public EffectTargetType effectTargetType;
  // The target.
  public object? target;
  // Inventory Context for the effect, typically the person performing the task.
  public IInventoryContext? context;
  // Ability Context for the effect, typically the person performing the task.
  public IAbilityContext? abilityContext;
  // The effect target constructor.
  public ChosenEffectTarget(EffectTargetType effectTargetType, object? target, object? context)
  {
    this.effectTargetType = effectTargetType;
    this.target = target;
    this.context = context as IInventoryContext;
    this.abilityContext = context as IAbilityContext;
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
  protected static Dictionary<string, Effect> _effects = new Dictionary<string, Effect>();

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

  // Apply the effect to the chosen target, using the given person as context.
  public virtual void Apply(ChosenEffectTarget chosenEffectTarget)
  {
    throw new Exception("Effect.Apply not implemented for " + effectType);
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
