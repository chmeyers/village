// Classes that inherit from Effect
using Village.Abilities;
using Village.Base;
using Village.Buildings;
using Village.Items;
using Village.Persons;
using Village.Skills;

namespace Village.Effects;

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
    amount = AbilityValue.FromJson(data["amount"]);
  }

  private void DegradeItem(IAbilityContext? context, Item item)
  {
    // Decrease the new item's quality by the specified amount.
    // TODO(chmeyers): What should we do if the item's quality reaches 0?
    item.quality -= amount.GetValue(context);
    if (item.quality < 0)
    {
      item.quality = 0;
    }
  }
  // Apply the effect to the target.
  public override void ApplySync(ChosenEffectTarget chosenEffectTarget)
  {
    // Get the item from the chosen target.
    Item item = (Item)chosenEffectTarget.target!;
    // Get the person from the context.
    Person person = (Person)chosenEffectTarget.targetContext!;
    // We are only degrading a single item, so if the item is a stack, we need to split the stack.
    // So we create a new item that is a copy of the original item, remove it from the inventory,
    // degrade it, then add it back to the inventory.

    // Check if person has more than one of the item.
    if (person.inventory[item] > Inventory.DEFAULT_QUANTITY)
    {
      Item newItem = item.Clone();
      // Remove the original item from the inventory of the person in the context.
      person.RemoveItem(item, Inventory.DEFAULT_QUANTITY);

      DegradeItem(chosenEffectTarget.runningContext, newItem);
      // Add the new item back to the inventory of the person in the context.
      person.AddItem(newItem, Inventory.DEFAULT_QUANTITY);
    }
    else
    {
      // Degrade the item.
      DegradeItem(chosenEffectTarget.runningContext, item);
    }


  }

  // The amount to degrade the item by.
  public AbilityValue amount;
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
    level = AbilityValue.FromJson(data["level"]);
    // The maximum level the skill can be increased to.
    if (data.ContainsKey("amount"))
    {
      amount = AbilityValue.FromJson(data["amount"]);
    }
  }

  // Apply the effect to the target.
  public override void ApplySync(ChosenEffectTarget chosenEffectTarget)
  {
    // Get the person from the chosen target.
    ISkillContext person = (ISkillContext)chosenEffectTarget.targetContext!;
    // Make sure person is not null.
    if (person == null)
    {
      // We ignore this effect if the person is null.
      return;
    }

    if (_skill == null)
    {
      _skill = Skill.Find(skill);
      // Make sure the skill exists.
      if (_skill == null)
      {
        throw new Exception("Skill does not exist: " + skill + " in skill effect " + effect);
      }
    }

    // Increase the skill of the target.
    // Note that the amount uses the ability context which may be a different context
    // than the target, for example if one person is teaching another person.

    // If the person is currently at level, they get one XP, if less they get two,
    // if more they get nothing.
    var trainingLevel = level.GetValue(chosenEffectTarget.runningContext);
    var trainingAmount = amount.GetValue(chosenEffectTarget.runningContext);
    if (person.GetLevel(_skill) == trainingLevel)
    {
      person.GrantXP(_skill, trainingAmount);
    }
    else if (person.GetLevel(_skill) < trainingLevel)
    {
      person.GrantXP(_skill, trainingAmount * 2);
    }
  }

  // The name of the skill to increase.
  public string skill;
  // Cached Skill object.
  // Skills are loaded after effects, so we can't get the Skill object during the initial load.
  private Skill? _skill;
  // The level of training, if the person is at this level, they get the specified
  // amount of XP, if they are below this level, they get double XP, if they are
  // above this level, they get no XP.
  public AbilityValue level;
  // Amount to increase, defaults to 1.
  public AbilityValue amount = new AbilityValue(1);
}

// Construct a building component.
public class BuildingComponentEffect : Effect
{
  public BuildingComponentEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a building.
    if (target != EffectTargetType.Building)
    {
      throw new Exception("Building component effect must target a building: " + effect);
    }
    if (data == null)
    {
      throw new Exception("Building component effect must have a config dictionary: " + effect);
    }
    // The name of the building component to construct.
    component = (string)data["component"];
    // The specific building component to construct.
    // i.e. the material used to construct the component.
    if (data.ContainsKey("type"))
    {
      specificComponent = (string)data["type"];
    }
  }

  // Apply the effect to the target.
  public override void ApplySync(ChosenEffectTarget chosenEffectTarget)
  {
    // Get the building from the chosen target.
    Building building = (Building)chosenEffectTarget.target!;
    // Make sure the building is not null.
    if (building == null)
    {
      // This effect should never be called without a valid target building.
      throw new Exception("Building is null in building component effect: " + effect);
    }
    BuildingComponent builtComponent = new BuildingComponent(component);
    builtComponent.builtComponent = specificComponent;
    // Construct the building component.
    building.AddComponent(builtComponent);
  }

  public override bool IsOptional()
  {
    // Building Component effects are not optional.
    // If you can't apply the effect, then you shouldn't run the task.
    return false;
  }

  public override HashSet<BuildingComponent> BuildingComponents()
  {
    // The building component that is being constructed.
    HashSet<BuildingComponent> components = new HashSet<BuildingComponent>();
    BuildingComponent builtComponent = new BuildingComponent(component);
    builtComponent.builtComponent = specificComponent;
    components.Add(builtComponent);
    return components;
  }

  // The name of the building component to construct.
  public string component;
  // The specific building component to construct.
  // i.e. the material used to construct the component.
  public string? specificComponent;
}