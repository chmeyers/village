using Village.Abilities;
using Village.Attributes;
using Village.Base;
using Village.Buildings;
using Village.Items;
using Village.Persons;
using Village.Skills;

namespace Village.Effects;

public class PlantCropEffect : Effect
{
  public PlantCropEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a field.
    if (target != EffectTargetType.Field)
    {
      throw new Exception("Plant Crop effect must target a field: " + effect);
    }
    if (data == null)
    {
      throw new Exception("Plant Crop effect must have a config dictionary: " + effect);
    }
    // The name of the crop to plant.
    string cropName = (string)data["crop"];
    // Get the crop type from the name.
    crop = ItemType.Find(cropName) ?? throw new Exception("Unknown crop: " + cropName + " in effect: " + effect);
  }

  // Apply the effect to the target.
  public override void ApplySync(ChosenEffectTarget chosenEffectTarget, double multiplier = 1, int timeBatch = 1)
  {
    // Get the field from the chosen target.
    Field field = (Field)chosenEffectTarget.target!;
    // Make sure the field is not null.
    if (field == null)
    {
      // This effect should never be called without a valid target field.
      throw new Exception("Field is null in plant crop effect: " + effect);
    }
    if (!field.Plant(crop, multiplier))
    {
      // TODO(chmeyers): Make sure the field has room for the crop before running the task.
      // This effect should never be called without a valid target field.
      throw new Exception("Unable to plant crop in plant crop effect: " + effect);
    }
  }

  public override bool IsOptional()
  {
    // plant crop effects are not optional.
    // If you can't apply the effect, then you shouldn't run the task.
    return false;
  }

  // The type of the crop to plant.
  public ItemType crop;
}

public class HarvestCropEffect : Effect
{
  public const string defaultYieldAttributeType = "crop_yield";
  public HarvestCropEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a field.
    if (target != EffectTargetType.Field)
    {
      throw new Exception("Harvest Crop effect must target a field: " + effect);
    }
    if (data == null)
    {
      throw new Exception("Harvest Crop effect must have a config dictionary: " + effect);
    }
    // The name of the crop to harvest.
    string cropName = (string)data["crop"];
    // Get the crop type from the name.
    crop = ItemType.Find(cropName) ?? throw new Exception("Unknown crop: " + cropName + " in effect: " + effect);
    if (crop.harvestItems.Count == 0)
    {
      throw new Exception("Crop has no harvest items: " + cropName + " in harvest effect: " + effect);
    }
    // The name of the attribute type to use for the yield.
    yieldAttributeTypeName = (string)data.GetValueOrDefault("yieldAttributeType", defaultYieldAttributeType);
  }

  // Apply the effect to the target.
  public override void ApplySync(ChosenEffectTarget chosenEffectTarget, double multiplier = 1, int timeBatch = 1)
  {
    // Get the field from the chosen target.
    Field field = (Field)chosenEffectTarget.target!;
    // Make sure the field is not null.
    if (field == null)
    {
      // This effect should never be called without a valid target field.
      throw new Exception("Field is null in harvest crop effect: " + effect);
    }
    // Determine the yield.
    double yield = 0;
    yield = field.state.GetAttributeValue(yieldAttributeType!);
    // TODO(chmeyers): Figure out the correct quantity to harvest, and deal with partial harvests.
    if (!field.Harvest(crop, multiplier))
    {
      // This effect should never be called without a valid target field.
      throw new Exception("Unable to harvest crop in harvest crop effect: " + effect);
    }
    // Actual yield is the difference between the yield before and after the harvest.
    yield -= field.state.GetAttributeValue(yieldAttributeType!);

    // Add the yield of each item in the itemtype's harvestItems to the inventory.
    foreach (var harvestItem in crop.harvestItems)
    {
      Item item = new Item(harvestItem.Key);
      int quantity = (int)(yield * harvestItem.Value);
      if (quantity <= 0) continue;
      // The field's harvest always goes to the household inventory that owns the field,
      // regardless of the target context.
      field.household.inventory.AddItem(item, quantity);
    }
  }

  // Initialize should resolve the attribute names to the actual attribute type.
  public override void Initialize()
  {
    // Get the attribute type from the name.
    yieldAttributeType = AttributeType.Find(yieldAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + yieldAttributeTypeName + " in effect: " + effect);
  }

  public override bool IsOptional()
  {
    // harvest crop effects are not optional.
    // If you can't apply the effect, then you shouldn't run the task.
    return false;
  }

  // The type of the crop to harvest.
  public ItemType crop;
  // The Attribute Type holding the crop yield.
  public string yieldAttributeTypeName;
  public AttributeType? yieldAttributeType;
}