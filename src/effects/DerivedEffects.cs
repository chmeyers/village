// Classes that inherit from Effect
using Village.Abilities;
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
    amount = AbilityValue.FromJson(data["amount"]);
    // The maximum level the skill can be increased to.
    maxLevel = AbilityValue.FromJson(data["maxLevel"]);
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
    // TODO(chmeyers): Use the maxLevel setting.
    
    person.GrantXP(_skill, amount.GetValue(chosenEffectTarget.runningContext));
  }

  // The name of the skill to increase.
  public string skill;
  // Cached Skill object.
  private Skill? _skill;
  // The amount to increase the skill by.
  public AbilityValue amount;
  // The maximum level the skill can be increased to.
  public AbilityValue maxLevel;
}