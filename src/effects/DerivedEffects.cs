// Classes that inherit from Effect
using Village.Abilities;
using Village.Attributes;
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

  private void AddScraps(IInventoryContext target, Item item)
  {
    foreach (var scrapItem in item.itemType.scrapItems)
    {
      Item newItem = new Item(scrapItem.Key);
      target.inventory.AddItem(newItem, scrapItem.Value);
    }
  }

  // Apply the effect to the target.
  public override void FinishSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Get the item from the chosen target.
    Item item = (Item)chosenEffectTarget.target!;
    // Get the person from the context.
    IInventoryContext targetInventory = chosenEffectTarget.targetContext!;
    // We are only degrading a single item, so if the item is a stack, we need to split the stack.
    // So we create a new item that is a copy of the original item, remove it from the inventory,
    // degrade it, then add it back to the inventory.

    // Calculate the amount to degrade the item by.
    // Batching is equivalent to degrading the item for the specified amount of time.
    // Note that we don't overflow the degradation onto a second item,
    // so batching is not exactly equivalent.
    int degradeAmount = (int)Math.Ceiling(amount.GetScaledValue(chosenEffectTarget.runningContext, scaler) * batchSize);

    // Check if person has more than one of the item.
    if (targetInventory.inventory[item] > Inventory.DEFAULT_QUANTITY)
    {
      Item newItem = item.Clone();
      // Remove the original item from the inventory of the person in the context.
      targetInventory.inventory.RemoveItem(item, Inventory.DEFAULT_QUANTITY);

      if (newItem.quality <= degradeAmount)
      {
        // Item completely degraded.
        AddScraps(targetInventory, newItem);
      }
      else
      {
        // Degrade the item.
        newItem.quality -= degradeAmount;
        targetInventory.inventory.AddItem(newItem, Inventory.DEFAULT_QUANTITY);
      }
    }
    else
    {
      // Degrade the item.
      if (item.quality <= degradeAmount)
      {
        // Item completely degraded.
        targetInventory.inventory.RemoveItem(item, Inventory.DEFAULT_QUANTITY);
        AddScraps(targetInventory, item);
      }
      else
      {
        // Degrade the item.
        item.quality -= degradeAmount;
      }
    }
  }

  public override bool AlwaysTargetsRunner()
  {
    return true;
  }

  public override bool SupportsBatching()
  {
    return true;
  }

  public override double MinScale(ChosenEffectTarget target)
  {
    // Degrade can scale infinitely, and it's an optional effect, so these
    // values are mostly recommendations.
    // This min scale will degrade by 1.
    double amount = this.amount.GetValue(target.runningContext);
    return amount == 0 ? Double.MinValue: 1.0/amount;
  }

  public override double MaxScale(ChosenEffectTarget target)
  {
    // This max scale will degrade the item completely.
    double amount = this.amount.GetValue(target.runningContext);
    return amount == 0 ? Double.MaxValue : (target.target! as Item)!.quality / amount;
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

  public static bool GiveSkillXP(ISkillContext person, Skill skill, double amount, int trainingLevel)
  {
    bool granted = false;
    // We multiply by two here and divide later to allow a
    // half point to give a point in lower skill levels.
    int trainingAmount = (int)Math.Floor(2 * amount);
    while (trainingAmount > 0 && person.GetLevel(skill) <= trainingLevel)
    {
      // Grant the max of trainingAmount or the amount needed to get to the next level.
      var nextLevelXP = person.GetNextLevelXP(skill);
      if (person.GetLevel(skill) == trainingLevel)
      {
        var grant = Math.Min(trainingAmount / 2, nextLevelXP);
        if (grant == 0 || !person.GrantXP(skill, grant))
        {
          // We can't grant any more XP, so we are done.
          break;
        }
        granted = true;
        trainingAmount -= grant / 2;
      }
      else if (person.GetLevel(skill) < trainingLevel)
      {
        var grant = Math.Min(trainingAmount, nextLevelXP);
        if (grant == 0 || !person.GrantXP(skill, grant))
        {
          // We can't grant any more XP, so we are done.
          break;
        }
        granted = true;
        trainingAmount -= grant;
      }
    }
    return granted;
  }

  // Apply the effect to the target.
  public override void StartSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Get the person from the chosen target.
    ISkillContext person = (ISkillContext)chosenEffectTarget.target!;
    // Make sure person is not null.
    if (person == null)
    {
      // We ignore this effect if the person is null.
      return;
    }

    // Increase the skill of the target.
    // Note that the amount uses the ability context which may be a different context
    // than the target, for example if one person is teaching another person.

    // If the person is currently at level, they get one XP, if less they get two,
    // if more they get nothing.
    int trainingLevel = (int)level.GetValue(chosenEffectTarget.runningContext);
    double trainingAmount = amount.GetScaledValue(chosenEffectTarget.runningContext, scaler) * batchSize;
    GiveSkillXP(person, _skill!, trainingAmount, trainingLevel);
  }

  // Initialize should resolve the skill name to the actual skill object.
  public override void Initialize()
  {
    _skill = Skill.Find(skill);
    // Make sure the skill exists.
    if (_skill == null)
    {
      throw new Exception("Skill does not exist: " + skill + " in skill effect " + effect);
    }
  }

  public override bool SupportsBatching()
  {
    return true;
  }

  public override double MinScale(ChosenEffectTarget target)
  {
    // The code can scale however, but below this scale they will get no XP.
    // This effect is optional, so these are just recommendations.
    double amount = this.amount.GetValue(target.runningContext);
    return amount == 0 ? 0.0 : 1.0 / amount;
  }

  public override double MaxScale(ChosenEffectTarget target)
  {
    return Double.MaxValue;
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

  public override void StartSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // TODO(chmeyers): Verify that the building component is valid and no other task
    // is currently constructing it.
  }

  // Apply the effect to the target.
  public override void FinishSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
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

  public override double MinScale(ChosenEffectTarget target)
  {
    return 1.0;
  }

  public override double MaxScale(ChosenEffectTarget target)
  {
    return 1.0;
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

// Propagate skills up and down the skill tree
public class SkillTreeEffect : Effect
{
  public SkillTreeEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a person or self.
    if (target != EffectTargetType.Person)
    {
      throw new Exception("Skill tree effect must target a person: " + effect);
    }
    if (data == null)
    {
      throw new Exception("Skill tree effect must have a config dictionary: " + effect);
    }
    // The name of the skill to propagate.
    skill = (string)data["skill"];
    // The amount to propagate the skill by.
    amount = AbilityValue.FromJson(data["amount"]);
    // Propagate the skill up the tree to the parent, defaults to false.
    if (data.ContainsKey("parent"))
    {
      propagateUp = (bool)data["parent"];
    }
  }

  // Apply the effect to the target.
  public override void StartSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Get the person from the chosen target.
    Person person = (Person)chosenEffectTarget.target!;
    // Make sure person is not null.
    if (person == null)
    {
      // We ignore this effect if the person is null.
      return;
    }

    // If we are propagating up the tree, then add amount XP to each parent of the skill.
    var relatives = _skill!.children;
    if (propagateUp)
    {
      // Use the parents instead of the children.
      relatives = _skill!.parents;
    }
    foreach (var relative in relatives)
    {
      // Increase the skill of the target.
      person.GrantXP(relative, (int)amount.GetValue(chosenEffectTarget.runningContext));
    }
  }

  // Initialize should resolve the skill name to the actual skill object.
  public override void Initialize()
  {
    _skill = Skill.Find(skill);
    // Make sure the skill exists.
    if (_skill == null)
    {
      throw new Exception("Skill does not exist: " + skill + " in skill effect " + effect);
    }
  }

  // The name of the skill to propagate.
  public string skill;
  // Cached Skill object.
  // Skills are loaded after effects, so we can't get the Skill object during the initial load.
  private Skill? _skill;
  // The amount to propagate the skill by.
  public AbilityValue amount;
  // Whether to propagate the skill up the tree.
  public bool propagateUp = false;
}