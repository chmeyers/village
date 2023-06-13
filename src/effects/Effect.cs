// Classes describing a side effect from a task.
// Effects can target Self, Persons, Items, Buildings, etc.
// Effects can be positive or negative, and can add or remove abilities, skills, experience, etc.
using Village.Abilities;
using Village.Base;
using Village.Buildings;
using Village.Households;
using Village.Items;
using Village.Tasks;

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
  // A Field, any effects targetting attributes should be able to support this.
  Field,
  // A Crop, any effects targetting Fields should also be able to support this.
  Crop,
}

// The type of effect. Each effect type will require a subclass of Effect.
public enum EffectType
{
  // Degrade an item, typically the tool used for the task.
  Degrade,
  // Increase a Person's skill level.
  Skill,
  // Construct a building component.
  BuildingComponent,
  // Propagate Skills up and down the skill tree.
  SkillTree,
  // Pulls an attribute towards a target value.
  AttributePuller,
  // Transfer a value from one attribute to another.
  AttributeTransfer,
  // Add a value to an attribute
  AttributeAdder,
  // Plant a crop in a field.
  PlantCrop,
  // Harvest a crop from a field.
  HarvestCrop,
  // Grow a crop in a field.
  GrowCrop,
  // Ongoing Field Maintenance.
  FieldMaintenance,
  // Rot a crop in a field.
  RotCrop,
  // Kill a crop in a field.
  KillCrop,
  // Interact with a crop, effecting it's health.
  TouchCrop,
  // Learn XP in a skill from a crop interaction.
  CropSkill,
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
      case EffectTargetType.Field:
      case EffectTargetType.Crop:
        if (target != "" && !IsTargetString(target))
        {
          throw new Exception("Invalid target for " + effectTargetType + ": " + target);
        }
        break;
      case EffectTargetType.Building:
      case EffectTargetType.Item:
        // Should be a valid ability name or a target string.
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

  // Returns true if the two EffectTargets can point to the same target.
  public bool Equals(EffectTarget target)
  {
    return effectTargetType == target.effectTargetType
           && this.target == target.target;
  }
  

}

// The ChosenEffectTarget represents an EffectTarget that has been resolved
// to a specific target.
public class ChosenEffectTarget
{
  // The type of target.
  public EffectTargetType effectTargetType;
  // The target item. Should be owned by the target context,
  // but that is not checked.
  public object? target;
  // Inventory Context for the effect, typically the person performing the task,
  // but could be the person who owns the item being used for the task, or a
  // building that owns the item being used for the task,
  // or the household that owns the building being constructed, etc.
  public IInventoryContext? targetContext;
  // Context of the object that is running the effect, typically the person performing the task.
  // Used to see what abilities the person has.
  public IAbilityContext? runningContext;
  // The effect target constructor.
  public ChosenEffectTarget(EffectTargetType effectTargetType, object? target, IInventoryContext? targetContext, IAbilityContext? runningContext)
  {
    this.effectTargetType = effectTargetType;
    this.target = target;
    this.targetContext = targetContext;
    this.runningContext = runningContext;
  }
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

  // Building Componenets provided by the effect.
  public virtual HashSet<BuildingComponent> BuildingComponents()
  {
    return new HashSet<BuildingComponent>();
  }

  // Apply the effect to the chosen target, using the given person as context.
  public void Apply(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Effects where the runningContext is the same as targetContext or they are
    // in the same household are applied synchronously.
    var runningHousehold = chosenEffectTarget.runningContext as IHouseholdContext;
    var targetHousehold = chosenEffectTarget.targetContext as IHouseholdContext;
    if ((chosenEffectTarget.runningContext == chosenEffectTarget.targetContext) || (runningHousehold != null && targetHousehold != null && runningHousehold.household == targetHousehold.household))
    {
      ApplySync(chosenEffectTarget, scaler, batchSize);
    }
    // Everything else is applied asynchronously.
    else
    {
      Task.Run(() => ApplySync(chosenEffectTarget, scaler, batchSize));
    }
  }

  // Start the effect.
  public virtual void Start(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // TODO(chmeyers): Should Start always be synchronous?
    var runningHousehold = chosenEffectTarget.runningContext as IHouseholdContext;
    var targetHousehold = chosenEffectTarget.targetContext as IHouseholdContext;
    if ((chosenEffectTarget.runningContext == chosenEffectTarget.targetContext) || (runningHousehold != null && targetHousehold != null && runningHousehold.household == targetHousehold.household))
    {
      StartSync(chosenEffectTarget, scaler, batchSize);
    }
    // Everything else is applied asynchronously.
    else
    {
      Task.Run(() => StartSync(chosenEffectTarget, scaler, batchSize));
    }
  }

  // Finish the effect.
  public virtual void Finish(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    var runningHousehold = chosenEffectTarget.runningContext as IHouseholdContext;
    var targetHousehold = chosenEffectTarget.targetContext as IHouseholdContext;
    if ((chosenEffectTarget.runningContext == chosenEffectTarget.targetContext) || (runningHousehold != null && targetHousehold != null && runningHousehold.household == targetHousehold.household))
    {
      FinishSync(chosenEffectTarget, scaler, batchSize);
    }
    // Everything else is applied asynchronously.
    else
    {
      Task.Run(() => FinishSync(chosenEffectTarget, scaler, batchSize));
    }
  }

  public void ApplySync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    StartSync(chosenEffectTarget, scaler, batchSize);
    FinishSync(chosenEffectTarget, scaler, batchSize);
  }

  public virtual void StartSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Do Nothing.
  }

  public virtual void FinishSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Do Nothing.
  }

  // Initialize is called after all effects and other types have been loaded.
  // This is used to resolve any references between effects.
  public virtual void Initialize() { }

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

  // Whether an effect is optional.
  // An optional effect will not be applied if the target is not found.
  public virtual bool IsOptional()
  {
    return true;
  }

  // Whether an effect supports batching.
  // If an effect supports batching, then the effect can receive a multiplier
  // for the number of times the effect should be applied.
  // Batched effects are allowed to apply approximations of the effect,
  // instead of calculating the exact effect for each batch.
  public virtual bool SupportsBatching()
  {
    return false;
  }

  public virtual double MinScale(ChosenEffectTarget target)
  {
    // For Crops and Fields, the default min scale is the min plant quantity.
    // For other targets, the default max scale is 1.
    if (target.effectTargetType == EffectTargetType.Crop)
    {
      double? quantity = (target.target as Field.CropInfo)?.quantity;
      if ( quantity != null && quantity > 0 && quantity < Field.minPlantQuantity)
      {
        return quantity.Value;
      }
      return Field.minPlantQuantity;
    }
    else if (target.effectTargetType == EffectTargetType.Field)
    {
      return Field.minPlantQuantity;
    }
    return 1.0;
  }

  private double _MaxScale(ChosenEffectTarget target)
  {
    // For Crops and Fields, the default max scale is the size of the field or crop.
    // For other targets, the default max scale is 1.
    if (target.effectTargetType == EffectTargetType.Crop)
    {
      var crop = target.target as Field.CropInfo;
      if (crop != null)
      {
        return crop.quantity;
      }
      return 0.0;
    }
    else if (target.effectTargetType == EffectTargetType.Field)
    {
      var field = target.target as Field;
      if (field != null)
      {
        return field.size;
      }
    }
    return 1.0;
  }

  public virtual double MaxScale(ChosenEffectTarget target)
  {
    return _MaxScale(target);
  }

  public virtual double? PreferredScale(ChosenEffectTarget target)
  {
    // For Crops and Fields, the default preferred scale is the max scale.
    // For other targets, the default preferred scale is the max scale only
    // if it is less than 1, otherwise it is null.
    if (target.effectTargetType == EffectTargetType.Crop || target.effectTargetType == EffectTargetType.Field || MaxScale(target) < 1.0)
    {
      return _MaxScale(target);
    }
    return null;
  }

  // Whether this effect always targets the person performing the task.
  public virtual bool AlwaysTargetsRunner()
  {
    return false;
  }

  // Utility function for this effect.
  public virtual double Utility(IHouseholdContext household, ITaskRunner runner, ChosenEffectTarget chosenEffectTarget, double scaler = 1)
  {
    // Default utility is 0.
    // Effects that are never called by tasks don't need to override this.
    return 0;
  }
}
