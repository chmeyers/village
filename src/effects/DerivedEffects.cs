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
  public override void ApplySync(ChosenEffectTarget chosenEffectTarget, double multiplier = 1, int timeBatch = 1)
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
    int degradeAmount = (int)(amount.GetValue(chosenEffectTarget.runningContext) * multiplier * timeBatch);

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
  public override void ApplySync(ChosenEffectTarget chosenEffectTarget, double multiplier = 1, int timeBatch = 1)
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
    int trainingAmount = (int)(amount.GetValue(chosenEffectTarget.runningContext) * multiplier * timeBatch);
    while (trainingAmount > 0 && person.GetLevel(_skill!) <= trainingLevel)
    {
      // Grant the max of trainingAmount or the amount needed to get to the next level.
      var nextLevelXP = person.GetNextLevelXP(_skill!);
      if (person.GetLevel(_skill!) == trainingLevel)
      {
        var grant = Math.Min(trainingAmount, nextLevelXP);
        if(!person.GrantXP(_skill!, grant))
        {
          // We can't grant any more XP, so we are done.
          break;
        }
        trainingAmount -= grant;
      }
      else if (person.GetLevel(_skill!) < trainingLevel)
      {
        var grant = Math.Min(trainingAmount * 2, nextLevelXP);
        if (!person.GrantXP(_skill!, grant))
        {
          // We can't grant any more XP, so we are done.
          break;
        }
        trainingAmount -= grant/2;
      }
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

  public override bool SupportsBatching()
  {
    return true;
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
  public override void ApplySync(ChosenEffectTarget chosenEffectTarget, double multiplier = 1, int timeBatch = 1)
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
  public override void ApplySync(ChosenEffectTarget chosenEffectTarget, double multiplier = 1, int timeBatch = 1)
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

public class AttributePullerEffect : Effect
{
  public AttributePullerEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a person, Crop, or Field.
    if (target != EffectTargetType.Person && target != EffectTargetType.Crop && target != EffectTargetType.Field)
    {
      throw new Exception("AttributePuller effect must target a person, crop, or field: " + effect);
    }
    if (data == null)
    {
      throw new Exception("AttributePuller effect must have a config dictionary: " + effect);
    }
    // All the keys in the data dictionary are the attributes to pull.
    // The value is the the AttributePuller info.
    foreach (var key in data.Keys)
    {
      var attributePullerData = ((Newtonsoft.Json.Linq.JToken)data[key]).ToObject<Dictionary<string, object>>();
      if (attributePullerData == null)
      {
        throw new Exception("AttributePuller effect " + effect + " has an invalid config entry: " + key);
      }
      
      // The target value of the attribute.
      var targetVal = AbilityValue.FromJson(attributePullerData["target"]);
      // The amount to pull the attribute by.
      var amount = AbilityValue.FromJson(attributePullerData["amount"]);
      _pullers.Add(new AttributePuller(key, targetVal, amount));
    }
  }

  // Apply the effect to the target.
  public override void ApplySync(ChosenEffectTarget chosenEffectTarget, double multiplier = 1, int timeBatch = 1)
  {
    // Get the person from the chosen target.
    IAttributeContext attributes = (IAttributeContext)chosenEffectTarget.target!;
    // Make sure attribute context is not null.
    if (attributes == null)
    {
      // We ignore this effect if the person is null.
      return;
    }

    foreach (var puller in _pullers)
    {
      double amount = puller.amount.GetValue(chosenEffectTarget.runningContext) * multiplier * timeBatch;
      if (amount == 0) continue;
      double target = puller.target.GetValue(chosenEffectTarget.runningContext);
      // Get the attribute from the person.
      double currentValue = attributes.GetAttributeValue(puller.type!);
      // If the current value is within amount of the target, then set it to the target.
      if (currentValue >= target - amount && currentValue <= target + amount)
      {
        attributes.SetAttribute(puller.type!, target);
      }
      // Otherwise, pull the attribute towards the target by amount.
      else if (currentValue < target)
      {
        attributes.SetAttribute(puller.type!, currentValue + amount);
      }
      else
      {
        attributes.SetAttribute(puller.type!, currentValue - amount);
      }

    }
  }

  // Initialize should resolve the attribute names to the actual attribute type.
  public override void Initialize()
  {
    foreach (var puller in _pullers)
    {
      puller.type = AttributeType.Find(puller.attribute);
      // Make sure the attribute exists.
      if (puller.type == null)
      {
        throw new Exception("Attribute does not exist: " + puller.attribute + " in attribute puller effect " + effect);
      }
    }
  }

  public override bool SupportsBatching()
  {
    return true;
  }

  class AttributePuller
  {
    // Constructor
    public AttributePuller(string attribute, AbilityValue target, AbilityValue amount)
    {
      this.attribute = attribute;
      this.target = target;
      this.amount = amount;
    }
    
    public string attribute = "";
    public AttributeType? type;
    public AbilityValue target;
    public AbilityValue amount;
  }
  // List of attributes to pull.
  private List<AttributePuller> _pullers = new List<AttributePuller>();
}

public class AttributeTransferEffect : Effect
{
  public AttributeTransferEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a person, Crop, or Field.
    if (target != EffectTargetType.Person && target != EffectTargetType.Crop && target != EffectTargetType.Field)
    {
      throw new Exception("AttributeTransfer effect must target a person, crop, or field: " + effect);
    }
    if (data == null)
    {
      throw new Exception("AttributeTransfer effect must have a config dictionary: " + effect);
    }
    // All the keys in the data dictionary are the attributes to pull.
    // The value is the the AttributeTransfer info.
    foreach (var key in data.Keys)
    {
      var attributeTransferData = ((Newtonsoft.Json.Linq.JToken)data[key]).ToObject<Dictionary<string, object>>();
      if (attributeTransferData == null)
      {
        throw new Exception("AttributeTransfer effect " + effect + " has an invalid config entry: " + key);
      }

      // The target value of the attribute.
      AbilityValue? sourceMin = null;
      if (attributeTransferData.ContainsKey("sourceMin"))
      {
        sourceMin = AbilityValue.FromJson(attributeTransferData["sourceMin"]);
      }
      // The amount to pull the attribute by.
      var amount = AbilityValue.FromJson(attributeTransferData["amount"]);
      string destinationAttribute = (string)attributeTransferData["dest"];
      AbilityValue? destMax = null;
      if (attributeTransferData.ContainsKey("destMax"))
      {
        destMax = AbilityValue.FromJson(attributeTransferData["destMax"]);
      }
      AbilityValue multiplier = 1;
      if (attributeTransferData.ContainsKey("multiplier"))
      {
        multiplier = AbilityValue.FromJson(attributeTransferData["multiplier"]);
      }
      bool retainOverflow = false;
      if (attributeTransferData.ContainsKey("retainOverflow"))
      {
        retainOverflow = (bool)attributeTransferData["retainOverflow"];
      }
      
      _transferers.Add(new AttributeTransfer(key, sourceMin, amount, destinationAttribute, destMax, multiplier, retainOverflow));
    }
  }

  // Apply the effect to the target.
  public override void ApplySync(ChosenEffectTarget chosenEffectTarget, double multiplier = 1, int timeBatch = 1)
  {
    // Get the person from the chosen target.
    IAttributeContext attributes = (IAttributeContext)chosenEffectTarget.target!;
    // Make sure attribute context is not null.
    if (attributes == null)
    {
      // We ignore this effect if the person is null.
      return;
    }

    foreach (var transferer in _transferers)
    {
      double amount = transferer.amount.GetValue(chosenEffectTarget.runningContext) * multiplier * timeBatch;
      if (amount == 0) continue;
      double min = transferer.sourceMin!.GetValue(chosenEffectTarget.runningContext);
      double max = transferer.destMax!.GetValue(chosenEffectTarget.runningContext);
      // Get the attribute from the person.
      double currentValue = attributes.GetAttributeValue(transferer.type!);
      double currentDestValue = attributes.GetAttributeValue(transferer.destType!);
      double transferAmount = amount;
      double destMultiplier = transferer.multiplier.GetValue(chosenEffectTarget.runningContext);
      // Positive amounts always remove from the source and add to the destination,
      // Never taking the source below the min or the destination above the max.
      if (amount > 0)
      {
        // If the current value is less than the min, then we can't transfer anything.
        if (currentValue <= min)
        {
          transferAmount = 0;
        }
        else if (currentValue - amount < min)
        {
          transferAmount = currentValue - min;
        }
        else
        {
          transferAmount = amount;
        }
        double destReceiveAmount = transferAmount * destMultiplier;
        // If we are retaining overflow, then we can't transfer more than the max.
        if (currentDestValue + destReceiveAmount > max)
        {
          double allowedRecieveAmount = Math.Max(max - currentDestValue, 0);
          if (transferer.retainOverflow) {
            // Reduce the amount taken from the source by the ratio of the
            // allowed amount to the desired amount.
            transferAmount *= allowedRecieveAmount / destReceiveAmount;
          }
          destReceiveAmount = allowedRecieveAmount;
        }
        // Transfer the amount from the source to the destination.
        attributes.SetAttribute(transferer.type!, currentValue - transferAmount);
        attributes.SetAttribute(transferer.destType!, currentDestValue + destReceiveAmount);
      }
      // Negative amounts always remove from the destination and add to the source,
      // Never taking the destination below the max or the source above the min.
      else
      {
        // If the current value is greater than the min, then we can't transfer anything.
        if (currentValue >= min)
        {
          transferAmount = 0;
        }
        else if (currentValue - amount > min)
        {
          transferAmount = currentValue - min;
        }
        else
        {
          transferAmount = amount;
        }
        double destRemoveAmount = transferAmount * destMultiplier;
        // If we are retaining overflow, then we can't transfer more than the max.
        if (currentDestValue + destRemoveAmount < max)
        {
          double allowedRemoveAmount = Math.Min(max - currentDestValue, 0);
          if (transferer.retainOverflow) {
            // Reduce the amount taken from the source by the ratio of the
            // allowed amount to the desired amount.
            transferAmount *= allowedRemoveAmount / destRemoveAmount;
          }
          destRemoveAmount = allowedRemoveAmount;
        }
        // Transfer the amount from the source to the destination.
        attributes.SetAttribute(transferer.type!, currentValue - transferAmount);
        attributes.SetAttribute(transferer.destType!, currentDestValue + destRemoveAmount);
      }

    }
  }

  // Initialize should resolve the attribute names to the actual attribute type.
  public override void Initialize()
  {
    foreach (var puller in _transferers)
    {
      puller.type = AttributeType.Find(puller.attribute);
      // Make sure the attribute exists.
      if (puller.type == null)
      {
        throw new Exception("Attribute does not exist: " + puller.attribute + " in attribute puller effect " + effect);
      }
      if (puller.sourceMin == null)
      {
        // Pull the default value from the attribute.
        // TODO(chmeyers): This doesn't take attribute scaling into account. Do we care?
        puller.sourceMin = puller.type.minValue;
      }
      puller.destType = AttributeType.Find(puller.dest);
      // Make sure the attribute exists.
      if (puller.destType == null)
      {
        throw new Exception("Attribute does not exist: " + puller.dest + " in attribute puller effect " + effect);
      }
      if (puller.destMax == null)
      {
        // Pull the default value from the attribute.
        // TODO(chmeyers): This doesn't take attribute scaling into account. Do we care?
        puller.destMax = puller.destType.maxValue;
      }
    }
  }

  public override bool SupportsBatching()
  {
    return true;
  }

  class AttributeTransfer
  {
    // Constructor
    public AttributeTransfer(string attribute, AbilityValue? sourceMin, AbilityValue amount, string dest, AbilityValue? destMax, AbilityValue multiplier, bool retainOverflow)
    {
      this.attribute = attribute;
      this.sourceMin = sourceMin;
      this.amount = amount;
      this.dest = dest;
      this.destMax = destMax;
      this.multiplier = multiplier;
      this.retainOverflow = retainOverflow;
    }

    public string attribute = "";
    public AttributeType? type;
    public AbilityValue? sourceMin;
    public AbilityValue amount;
    public string dest = "";
    public AttributeType? destType;
    public AbilityValue? destMax;
    public AbilityValue multiplier = 1;
    public bool retainOverflow = false;
    
  }
  // List of attributes to pull.
  private List<AttributeTransfer> _transferers = new List<AttributeTransfer>();
}

public class AttributeIncreaserEffect : Effect
{
  public AttributeIncreaserEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a person, Crop, or Field.
    if (target != EffectTargetType.Person && target != EffectTargetType.Crop && target != EffectTargetType.Field)
    {
      throw new Exception("AttributeIncreaser effect must target a person, crop, or field: " + effect);
    }
    if (data == null)
    {
      throw new Exception("AttributeIncreaser effect must have a config dictionary: " + effect);
    }
    // All the keys in the data dictionary are the attributes to pull.
    // The value is the the AttributeIncreaser info.
    foreach (var key in data.Keys)
    {
      var attributePullerData = ((Newtonsoft.Json.Linq.JToken)data[key]).ToObject<Dictionary<string, object>>();
      if (attributePullerData == null)
      {
        throw new Exception("AttributeIncreaser effect " + effect + " has an invalid config entry: " + key);
      }

      // The target value of the attribute.
      AbilityValue targetVal = Double.MaxValue;
      if (attributePullerData.ContainsKey("target"))
      {
        targetVal = AbilityValue.FromJson(attributePullerData["target"]);
      }
      // The amount to increase the attribute by.
      var amount = AbilityValue.FromJson(attributePullerData["amount"]);
      _increasers.Add(new AttributeIncreaser(key, targetVal, amount));
    }
  }

  // Apply the effect to the target.
  public override void ApplySync(ChosenEffectTarget chosenEffectTarget, double multiplier = 1, int timeBatch = 1)
  {
    // Get the person from the chosen target.
    IAttributeContext attributes = (IAttributeContext)chosenEffectTarget.target!;
    // Make sure attribute context is not null.
    if (attributes == null)
    {
      // We ignore this effect if the person is null.
      return;
    }

    foreach (var increaser in _increasers)
    {
      double amount = increaser.amount.GetValue(chosenEffectTarget.runningContext) * multiplier * timeBatch;
      if (amount == 0) continue;
      double target = increaser.target.GetValue(chosenEffectTarget.runningContext);
      // Get the attribute from the person.
      double currentValue = attributes.GetAttributeValue(increaser.type!);
      // If we are already greater than the target, then we don't need to do anything.
      if (currentValue >= target) continue;
      // If the current value is within amount of the target, then set it to the target.
      if (currentValue + amount >= target)
      {
        attributes.SetAttribute(increaser.type!, target);
      }
      // Otherwise, increase the attribute by amount.
      else
      {
        attributes.SetAttribute(increaser.type!, currentValue + amount);
      }

    }
  }

  // Initialize should resolve the attribute names to the actual attribute type.
  public override void Initialize()
  {
    foreach (var increaser in _increasers)
    {
      increaser.type = AttributeType.Find(increaser.attribute);
      // Make sure the attribute exists.
      if (increaser.type == null)
      {
        throw new Exception("Attribute does not exist: " + increaser.attribute + " in attribute puller effect " + effect);
      }
    }
  }

  public override bool SupportsBatching()
  {
    return true;
  }

  class AttributeIncreaser
  {
    // Constructor
    public AttributeIncreaser(string attribute, AbilityValue target, AbilityValue amount)
    {
      this.attribute = attribute;
      this.target = target;
      this.amount = amount;
    }

    public string attribute = "";
    public AttributeType? type;
    public AbilityValue target;
    public AbilityValue amount;
  }
  // List of attributes to pull.
  private List<AttributeIncreaser> _increasers = new List<AttributeIncreaser>();
}

public class AttributeDecreaserEffect : Effect
{
  public AttributeDecreaserEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a person, Crop, or Field.
    if (target != EffectTargetType.Person && target != EffectTargetType.Crop && target != EffectTargetType.Field)
    {
      throw new Exception("AttributeDecreaser effect must target a person, crop, or field: " + effect);
    }
    if (data == null)
    {
      throw new Exception("AttributeDecreaser effect must have a config dictionary: " + effect);
    }
    // All the keys in the data dictionary are the attributes to pull.
    // The value is the the AttributeDecreaser info.
    foreach (var key in data.Keys)
    {
      var attributePullerData = ((Newtonsoft.Json.Linq.JToken)data[key]).ToObject<Dictionary<string, object>>();
      if (attributePullerData == null)
      {
        throw new Exception("AttributeDecreaser effect " + effect + " has an invalid config entry: " + key);
      }

      // The target value of the attribute.
      AbilityValue targetVal = Double.MinValue;
      if (attributePullerData.ContainsKey("target"))
      {
        targetVal = AbilityValue.FromJson(attributePullerData["target"]);
      }
      // The amount to increase the attribute by.
      var amount = AbilityValue.FromJson(attributePullerData["amount"]);
      _decreasers.Add(new AttributeDecreaser(key, targetVal, amount));
    }
  }

  // Apply the effect to the target.
  public override void ApplySync(ChosenEffectTarget chosenEffectTarget, double multiplier = 1, int timeBatch = 1)
  {
    // Get the person from the chosen target.
    IAttributeContext attributes = (IAttributeContext)chosenEffectTarget.target!;
    // Make sure attribute context is not null.
    if (attributes == null)
    {
      // We ignore this effect if the person is null.
      return;
    }

    foreach (var decreaser in _decreasers)
    {
      double amount = decreaser.amount.GetValue(chosenEffectTarget.runningContext) * multiplier * timeBatch;
      if (amount == 0) continue;
      double target = decreaser.target.GetValue(chosenEffectTarget.runningContext);
      // Get the attribute from the person.
      double currentValue = attributes.GetAttributeValue(decreaser.type!);
      // If we are already less than the target, then we don't need to do anything.
      if (currentValue <= target) continue;
      // If the current value is within amount of the target, then set it to the target.
      if (currentValue - amount <= target)
      {
        attributes.SetAttribute(decreaser.type!, target);
      }
      // Otherwise, decrease the attribute by amount.
      else
      {
        attributes.SetAttribute(decreaser.type!, currentValue - amount);
      }

    }
  }

  // Initialize should resolve the attribute names to the actual attribute type.
  public override void Initialize()
  {
    foreach (var decreaser in _decreasers)
    {
      decreaser.type = AttributeType.Find(decreaser.attribute);
      // Make sure the attribute exists.
      if (decreaser.type == null)
      {
        throw new Exception("Attribute does not exist: " + decreaser.attribute + " in attribute puller effect " + effect);
      }
    }
  }

  public override bool SupportsBatching()
  {
    return true;
  }

  class AttributeDecreaser
  {
    // Constructor
    public AttributeDecreaser(string attribute, AbilityValue target, AbilityValue amount)
    {
      this.attribute = attribute;
      this.target = target;
      this.amount = amount;
    }

    public string attribute = "";
    public AttributeType? type;
    public AbilityValue target;
    public AbilityValue amount;
  }
  // List of attributes to pull.
  private List<AttributeDecreaser> _decreasers = new List<AttributeDecreaser>();
}