// Classes that inherit from Effect
using Village.Items;
using Village.Persons;

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
    amount = (int)(long)data["amount"];
  }

  private void DegradeItem(Item item)
  {
    // Decrease the new item's quality by the specified amount.
    // TODO(chmeyers): What should we do if the item's quality reaches 0?
    item.quality -= amount;
    if (item.quality < 0)
    {
      item.quality = 0;
    }
  }
  // Apply the effect to the target.
  public override void Apply(ChosenEffectTarget chosenEffectTarget)
  {
    // Get the item from the chosen target.
    Item item = (Item)chosenEffectTarget.target!;
    // Get the person from the context.
    Person person = (Person)chosenEffectTarget.context!;
    // We are only degrading a single item, so if the item is a stack, we need to split the stack.
    // So we create a new item that is a copy of the original item, remove it from the inventory,
    // degrade it, then add it back to the inventory.
    
    // Check if person has more than one of the item.
    if (person.Inventory[item] > Inventory.DEFAULT_QUANTITY)
    {
      Item newItem = item.Clone();
      // Remove the original item from the inventory of the person in the context.
      person.RemoveItem(item, Inventory.DEFAULT_QUANTITY);
      
      DegradeItem(newItem);
      // Add the new item back to the inventory of the person in the context.
      person.AddItem(newItem, Inventory.DEFAULT_QUANTITY);
    }
    else
    {
      // Degrade the item.
      DegradeItem(item);
    }

    
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

  // Apply the effect to the target.
  public override void Apply(ChosenEffectTarget chosenEffectTarget)
  {
    // Get the person from the chosen target.
    Person person = (Person)chosenEffectTarget.target!;
    // Increase the skill.
    // TODO(chmeyers): Skills aren't implemented yet.
  }

  // The name of the skill to increase.
  public string skill;
  // The amount to increase the skill by.
  public int amount;
  // The maximum level the skill can be increased to.
  public int maxLevel;
}