using Village.Abilities;
using Village.Attributes;
using Village.Base;
using Village.Buildings;
using Village.Households;
using Village.Items;
using Village.Persons;
using Village.Skills;
using Village.Tasks;

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

    chainedEffects = new List<Effect>();
    // Load the list of chained effects that run after this effect.
    if (data.ContainsKey("chainedEffects"))
    {
      var chainedEffectNames = ((Newtonsoft.Json.Linq.JArray)data["chainedEffects"]).ToObject<List<string>>();
      foreach (var chainedEffectName in chainedEffectNames!)
      {
        var chainedEffect = Effect.Find((string)chainedEffectName) ?? throw new Exception("Unknown chained effect: " + chainedEffectName + " in effect: " + effect);
        chainedEffects.Add(chainedEffect);
      }
    }
  }

  // Apply the effect to the target.
  public override void StartSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Get the field from the chosen target.
    Field field = (Field)chosenEffectTarget.target!;
    // Make sure the field is not null.
    if (field == null)
    {
      // This effect should never be called without a valid target field.
      throw new Exception("Field is null in plant crop effect: " + effect);
    }
    if (!field.Plant(crop, scaler))
    {
      // TODO(chmeyers): Make sure the field has room for the crop before running the task.
      // This effect should never be called without a valid target field.
      throw new Exception("Unable to plant crop in plant crop effect: " + effect);
    }
    if (chainedEffects.Count == 0 || !field.crops.ContainsKey(crop))
    {
      // If there are no chained effects or no crop, then we are done.
      return;
    }
    // Create a new effect target for the chained effect.
    ChosenEffectTarget chainedEffectTarget = new ChosenEffectTarget(EffectTargetType.Crop, field.crops[crop], chosenEffectTarget.targetContext, chosenEffectTarget.runningContext);
    // Run the chained effects with the crop as the target.
    foreach (var chainedEffect in chainedEffects)
    {
      // Run the chained effect.
      chainedEffect.StartSync(chainedEffectTarget, scaler, batchSize);
    }
  }

  public override void FinishSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Finalize any chained effects.
    Field field = (Field)chosenEffectTarget.target!;
    if (chainedEffects.Count == 0 || !field.crops.ContainsKey(crop))
    {
      // If there are no chained effects or no crop, then we are done.
      return;
    }
    // Create a new effect target for the chained effect.
    ChosenEffectTarget chainedEffectTarget = new ChosenEffectTarget(EffectTargetType.Crop, field.crops[crop], chosenEffectTarget.targetContext, chosenEffectTarget.runningContext);
    // Run the chained effects with the crop as the target.
    foreach (var chainedEffect in chainedEffects)
    {
      // Run the chained effect.
      chainedEffect.FinishSync(chainedEffectTarget, scaler, batchSize);
    }
  }

  public override bool IsOptional()
  {
    // plant crop effects are not optional.
    // If you can't apply the effect, then you shouldn't run the task.
    return false;
  }

  const double minPlantingMoisture = 1.0;
  const double lowMoisturePenalty = 0.5;
  const double maxPlantingWeeds = 5.0;
  const double highWeedsPenalty = 0.5;
  public override double Utility(IHouseholdContext household, ITaskRunner runner, ChosenEffectTarget chosenEffectTarget, double scaler = 1)
  {
    Field field = (Field)chosenEffectTarget.target!;
    if (field == null)
    {
      return double.MinValue;
    }
    if (!field.CanPlant(crop, scaler))
    {
      return double.MinValue;
    }
    // If it's out of season, then we don't want to plant it.
    // TODO(chmeyers): Support non-temperate climates.
    // TODO(chmeyers): Provide some lesser utility for shoulder seasons.
    if (!crop.cropSettings!.temperatePlantingMonths.Contains(Calendar.Month))
    {
      return double.MinValue;
    }
    // Base utility is based on the expected yield.
    // TODO(chmeyers): Adjust this based on the household's history with this crop.
    double targetYield = crop.cropSettings!.targetYieldPerAcre * scaler;
    // Reduce utility if the soil quality isn't good enough.
    double soilQuality = field.GetUnscaledAttributeValue(StaticAttributes.soilQuality!);
    targetYield *= GrowCropEffect.SoilQualityEffect(crop, soilQuality);
    // TODO(chmeyers): The field should be recently plowed.
    // The weeds should be low.
    if (field.GetUnscaledAttributeValue(StaticAttributes.weeds!) > maxPlantingWeeds)
    {
      targetYield *= highWeedsPenalty;
    }
    // The ground should be moist.
    if (field.GetUnscaledAttributeValue(StaticAttributes.surfaceMoisture!) < minPlantingMoisture)
    {
      targetYield *= lowMoisturePenalty;
    }
    // TODO(chmeyers): Adjust for NPK in the field compared to crop needs, so that the AI
    // can make better decisions about what to plant.
    // TODO(chmeyers): Discount for all the work that needs to be done prior to harvest.
    // TODO(chmeyers): Discount for the amount of time it will take to grow.
    double utility = 0;
    // Add up the utilities of all the harvest items for this crop.
    foreach (var harvestItem in crop.cropSettings!.harvestItems)
    {
      utility += household.household.FutureUtility(runner, harvestItem.Key, (int)(targetYield * harvestItem.Value / harvestItem.Key.weight), crop.cropSettings!.totalDays);
    }
    // Add the utility of the chained effects.
    foreach (var chainedEffect in chainedEffects)
    {
      utility += chainedEffect.Utility(household, runner, chosenEffectTarget, scaler);
    }
    return utility;
  }

  // The type of the crop to plant.
  public ItemType crop;
  // Effects to run on the crop created by planting this crop.
  private List<Effect> chainedEffects;
}

public class HarvestCropEffect : Effect
{
  public HarvestCropEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a crop or field.
    if (target != EffectTargetType.Crop && target != EffectTargetType.Field)
    {
      throw new Exception("Harvest Crop effect must target a crop or field: " + effect);
    }
    if (data == null)
    {
      throw new Exception("Harvest Crop effect must have a config dictionary: " + effect);
    }
    // The name of the crop to harvest.
    string cropName = (string)data["crop"];
    // Get the crop type from the name.
    crop = ItemType.Find(cropName) ?? throw new Exception("Unknown crop: " + cropName + " in effect: " + effect);
    if (crop.cropSettings!.harvestItems.Count == 0)
    {
      throw new Exception("Crop has no harvest items: " + cropName + " in harvest effect: " + effect);
    }
  }

  public override void StartSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // TODO(chmeyers): Verify that the field has a crop to harvest here.
    Field field = (chosenEffectTarget.target as Field) ?? (chosenEffectTarget.target as Field.CropInfo)?.field ?? throw new Exception("Field is null in harvest crop effect: " + effect);
    // Advance the field before starting the harvest.
    field.Advance();
  }

  // Apply the effect to the target.
  public override void FinishSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Get the field from the chosen target.
    Field field = (chosenEffectTarget.target as Field) ?? (chosenEffectTarget.target as Field.CropInfo)?.field ?? throw new Exception("Field is null in harvest crop effect: " + effect);
    // Determine the yield.
    double yield = 0;
    yield = field.GetValue(crop, StaticAttributes.cropYield!);
    double harvestAmount = scaler;
    if (field.Count(crop) < harvestAmount)
    {
      harvestAmount = field.Count(crop);
    }
    if (harvestAmount <= 0)
    {
      // Nothing to harvest.
      return;
    }
    if (!field.Remove(crop, harvestAmount))
    {
      // This effect should never be called without a valid target field.
      throw new Exception("Unable to harvest crop in harvest crop effect: " + effect);
    }
    // Actual yield is the difference between the yield before and after the harvest.
    yield -= field.GetValue(crop, StaticAttributes.cropYield!);

    // Add the yield of each item in the itemtype's harvestItems to the inventory.
    foreach (var harvestItem in crop.cropSettings!.harvestItems)
    {
      Item item = new Item(harvestItem.Key);
      int quantity = (int)(yield * harvestItem.Value / harvestItem.Key.weight);
      if (quantity <= 0) continue;
      // The field's harvest always goes to the household inventory that owns the field,
      // regardless of the target context.
      field.household.inventory.AddItem(item, quantity);
    }
    // If the straw wasn't harvested, add it's nutrients back to the field.
    if (!crop.cropSettings!.hasHarvestableStraw && crop.cropSettings!.strawPerYield > 0)
    {
      double strawYield = yield * crop.cropSettings!.strawPerYield;
      field.AddAttribute(StaticAttributes.nitrogen!, crop.cropSettings!.nitrogenPerStraw * strawYield);
      field.AddAttribute(StaticAttributes.phosphorus!, crop.cropSettings!.phosphorusPerStraw * strawYield);
      field.AddAttribute(StaticAttributes.potassium!, crop.cropSettings!.potassiumPerStraw * strawYield);
    }
  }

  public override bool IsOptional()
  {
    // harvest crop effects are not optional.
    // If you can't apply the effect, then you shouldn't run the task.
    return false;
  }

  public override double MaxScale(ChosenEffectTarget target)
  {
    return Double.MaxValue;
  }

  public override double Utility(IHouseholdContext household, ITaskRunner runner, ChosenEffectTarget chosenEffectTarget, double scaler = 1)
  {
    // The utility of harvesting is dependent on the yield of the crop,
    // discounted depending on the amount of time this field has until
    // it starts to rot, so that the farmer will harvest the crops that
    // are closest to rotting first.
    Field field = (Field)chosenEffectTarget.target!;
    if (field == null)
    {
      return double.MinValue;
    }
    // Determine how long it is until the crop is fully grown.
    double cropDay = field.GetUnscaledValue(crop, crop.cropSettings!.cropAttribute!);
    int totalDays = crop.cropSettings!.totalDays;
    int lateDays = crop.cropSettings!.totalDays - crop.cropSettings!.lateDays;
    if (cropDay < lateDays)
    {
      // The crop is not fully grown yet.
      return double.MinValue;
    }
    double utility = 0;
    double yield = field.GetValue(crop, StaticAttributes.cropYield!);
    yield *= scaler / field.Count(crop);
    // Add up the utilities of all the harvest items for this crop.
    foreach (var harvestItem in crop.cropSettings!.harvestItems)
    {
      int quantity = (int)(yield * harvestItem.Value / harvestItem.Key.weight);
      if (quantity <= 0) continue;
      utility += household.household.Utility(runner, harvestItem.Key, quantity);
    }
    // If the health is low, we want to harvest the crop immediately.
    // Discount the utility based on the time until the crop rots.
    double cropHealth = field.GetUnscaledValue(crop, StaticAttributes.cropHealth!);
    // Before total days, discount by the health percentage, and don't harvest yet
    // if the health is above 50%.
    if (cropDay < totalDays)
    {
      if ( cropHealth > 50)
      {
        return double.MinValue;
      }
      utility *= (1 - cropHealth / 100);
    }
    // After total days, discount by half the health percentage, and harvest
    // immediately if the health is below 20%.
    else
    {
      if (cropHealth < 20)
      {
        return utility;
      }
      utility *= (1 - cropHealth / 200);
    }

    return utility;
  }

  // The type of the crop to harvest.
  public ItemType crop;
}

public class RotCropEffect : Effect
{
  public RotCropEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a crop.
    if (target != EffectTargetType.Crop)
    {
      throw new Exception("Rot Crop effect must target a crop: " + effect);
    }
    if (data == null)
    {
      throw new Exception("Rot Crop effect must have a config dictionary: " + effect);
    }
    if (!data.ContainsKey("rotRate"))
    {
      throw new Exception("Rot Crop effect must have a rotRate: " + effect);
    }
    rotRate = AbilityValue.FromJson(data["rotRate"]);
  }

  public static void Rot(Field.CropInfo cropInfo, double percent)
  {
    ItemType crop = cropInfo.itemType;
    // Reduce the crop health by the value of the percent
    // and reduce the yield by the relative value.
    // Health will be reduced quickly and yield will be reduced slowly.
    // We don't reduce the yield to zero, for that we'd need to call KillCrop.
    // TODO(chmeyers): High health should protect a crop from rotting until the health degrades.
    cropInfo.AddAttribute(StaticAttributes.cropHealth!, -percent * 100 * cropInfo.quantity);
    double yield = cropInfo.GetAttributeValue(StaticAttributes.cropYield!);
    double rotYield = percent * yield;
    cropInfo.AddAttribute(StaticAttributes.cropYield!, -rotYield);

    // Add the nutrients from the rotted yield back to the field.
    cropInfo.AddAttribute(StaticAttributes.nitrogen!, crop.cropSettings!.totalNitrogen * rotYield);
    cropInfo.AddAttribute(StaticAttributes.phosphorus!, crop.cropSettings!.totalPhosphorus * rotYield);
    cropInfo.AddAttribute(StaticAttributes.potassium!, crop.cropSettings!.totalPotassium * rotYield);
  }

  // Apply the effect to the target.
  public override void FinishSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Get the crop from the chosen target.
    Field.CropInfo cropInfo = (Field.CropInfo)chosenEffectTarget.target!;
    // Make sure the crop is not null.
    if (cropInfo == null)
    {
      // This effect should never be called without a valid target crop.
      throw new Exception("Crop Info is null in rot crop effect: " + effect);
    }
    double perTickRate = rotRate.GetScaledValue(chosenEffectTarget.runningContext, scaler) / scaler;
    // Get the total rot rate for the batchSize by calculating the exponential.
    double totalRate = (1 - Math.Pow(1 - (perTickRate), batchSize));
    Rot(cropInfo, totalRate);
  }

  public override bool IsOptional()
  {
    // crop yield effects are not optional.
    // If you can't apply the effect, then you shouldn't run the task.
    return false;
  }

  public override bool SupportsBatching()
  {
    return true;
  }

  private AbilityValue rotRate;
}

public class KillCropEffect : Effect
{
  public KillCropEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a crop or Field.
    if (target != EffectTargetType.Crop && target != EffectTargetType.Field)
    {
      throw new Exception("Kill Crop effect must target a crop or field: " + effect);
    }
  }

  public static void Kill(Field.CropInfo cropInfo, double quantity)
  {
    ItemType crop = cropInfo.itemType;
    Field field = cropInfo.field;
    // Ensure the field is up to date.
    field.Advance();
    // Get the difference in yield before and after the kill.
    double killYield = cropInfo.GetAttributeValue(StaticAttributes.cropYield!);
    // Remove the crop from the field.
    cropInfo.field.Remove(crop, quantity);
    if (field.Count(crop) > 0)
    {
      // Not all the crop was killed, so reduce the yield by the remaining.
      killYield -= cropInfo.GetAttributeValue(StaticAttributes.cropYield!);
    }

    // Add the nutrients from the rotted yield back to the field.
    field.AddAttribute(StaticAttributes.nitrogen!, crop.cropSettings!.totalNitrogen * killYield);
    field.AddAttribute(StaticAttributes.phosphorus!, crop.cropSettings!.totalPhosphorus * killYield);
    field.AddAttribute(StaticAttributes.potassium!, crop.cropSettings!.totalPotassium * killYield);
  }

  public static void KillAll(Field field)
  {
    // Ensure the field is up to date.
    field.Advance();
    // Sum up the NPK of all the crops in the field.
    double nitrogen = 0;
    double phosphorus = 0;
    double potassium = 0;
    foreach (var cropInfo in field.crops)
    {
      ItemType crop = cropInfo.Key;
      // Get the difference in yield before and after the kill.
      double killYield = cropInfo.Value.GetAttributeValue(StaticAttributes.cropYield!);
      nitrogen += crop.cropSettings!.totalNitrogen * killYield;
      phosphorus += crop.cropSettings!.totalPhosphorus * killYield;
      potassium += crop.cropSettings!.totalPotassium * killYield;
    }

    // Add the nutrients from the rotted yield back to the field.
    field.AddAttribute(StaticAttributes.nitrogen!, nitrogen);
    field.AddAttribute(StaticAttributes.phosphorus!, phosphorus);
    field.AddAttribute(StaticAttributes.potassium!, potassium);

    // Clear the field.
    field.RemoveAll();
  }

  // Apply the effect to the target.
  public override void FinishSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // If the target is a field, kill all the crops in the field.
    if (chosenEffectTarget.target is Field field)
    {
      KillAll(field);
      return;
    }
    // Get the crop from the chosen target.
    Field.CropInfo cropInfo = (Field.CropInfo)chosenEffectTarget.target!;
    // Make sure the crop is not null.
    if (cropInfo == null)
    {
      // This effect should never be called without a valid target crop.
      throw new Exception("Crop Info is null in kill crop effect: " + effect);
    }
    // Kill the crop.
    Kill(cropInfo, scaler);
  }

  public override bool IsOptional()
  {
    return false;
  }

  public override bool SupportsBatching()
  {
    return false;
  }

  public override double MinScale(ChosenEffectTarget target)
  {
    // You can kill part of a crop, but only the whole field.
    if (target.effectTargetType == EffectTargetType.Crop)
    {
      double? quantity = (target.target as Field.CropInfo)?.quantity;
      if (quantity != null && quantity > 0)
      {
        return quantity.Value;
      }
      return Field.minPlantQuantity;
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

  private double CropUtility(IHouseholdContext household, ITaskRunner runner, double scaler, Field.CropInfo cropInfo)
  {
    // TODO(chmeyers): This doesn't take into account the fertilizer benefit
    // of plowing under a crop.
    ItemType crop = cropInfo.itemType;
    // The utility for a single crop.
    double yield = cropInfo.GetAttributeValue(StaticAttributes.cropYield!);
    double health = cropInfo.GetUnscaledAttributeValue(StaticAttributes.cropHealth!);
    int cropDay = (int)cropInfo.GetAttributeValue(crop.cropSettings!.cropAttribute!);
    int totalDays = crop.cropSettings!.totalDays;
    int lateDays = crop.cropSettings!.totalDays - crop.cropSettings!.lateDays;

    if (health <= 0 && yield <= 0)
    {
      // The crop is dead, so it's utility is 0.
      return 0;
    }

    double utility = 0;
    if (cropDay >= lateDays)
    {
      // The crop is harvestable, so it's utility is the household utility.
      // TODO(chmeyers): This doesn't take into account the cost of harvesting,
      // but that's probably okay as we want to discourage killing harvestable crops.
      foreach (var harvestItem in crop.cropSettings!.harvestItems)
      {
        int quantity = (int)(yield * harvestItem.Value / harvestItem.Key.weight);
        if (quantity <= 0) continue;
        utility += household.household.Utility(runner, harvestItem.Key, quantity);
      }
      return utility;
    }
    // To early to harvest, so the utility is the max of the yield or the seed cost.
    // This is a bit simplistic, but it really only has to be a rough estimate to avoid
    // the AI plowing under valuable crops.
    int minQuantity = (int)(yield / crop.weight);
    if (health > 0) {
      minQuantity = Math.Max(minQuantity, (int)(scaler * crop.cropSettings!.seedPerAcre));
    }
    return household.household.Utility(runner, crop, minQuantity);
  }

  public override double Utility(IHouseholdContext household, ITaskRunner runner, ChosenEffectTarget chosenEffectTarget, double scaler = 1)
  {
    // if the field is empty, utility is zero.
    // otherwise it's likely negative and depends on the available yield, seed cost,
    // and the cost of the fertilizing effect.
    if (chosenEffectTarget.target is Field field)
    {
      if (field.cropCount == 0)
      {
        return 0;
      }
      double utility = 0;
      foreach (var cropInfo in field.crops)
      {
        utility -= CropUtility(household, runner, scaler, cropInfo.Value);
      }
    }
    else if (chosenEffectTarget.target is Field.CropInfo cropInfo)
    {
      return -CropUtility(household, runner, scaler, cropInfo);
    }
    return 0;
  }
}

// Every time a farmer interacts with a crop, it should get the touch crop effect,
// which will affect the crop's health positively or negatively, depending on the
// farmer's skill.
public class TouchCropEffect : Effect
{
  public TouchCropEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a crop or Field.
    if (target != EffectTargetType.Crop && target != EffectTargetType.Field)
    {
      throw new Exception("Touch Crop effect must target a crop or field: " + effect);
    }
    if (data == null)
    {
      throw new Exception("Touch Crop effect must have a config dictionary: " + effect);
    }
    if (!data.ContainsKey("healthRate"))
    {
      throw new Exception("Touch Crop effect must have a healthRate: " + effect);
    }
    healthRate = AbilityValue.FromJson(data["healthRate"]);
  }

  private double Touch(Field.CropInfo cropInfo, ISkillContext farmer, double scaler = 1, int batchSize = 1)
  {
    ItemType crop = cropInfo.itemType;
    // If the farmer has skill equal to the crop's skill level, then the crop won't
    // be affected at all. For each level of skill above/below the crop's skill level,
    // the crop will be affected by the healthRate.
    double skill = farmer.GetLevel(crop.cropSettings!.cropSkill!);
    double skillDifference = skill - crop.cropSettings!.cropSkillLevel;
    double healthDelta = healthRate.GetScaledValue(farmer, scaler) * skillDifference * batchSize;
    return healthDelta;
  }

  private void TouchAll(Field field, ISkillContext farmer, double scaler = 1, int batchSize = 1)
  {
    foreach (var cropInfo in field.crops)
    {
      double delta = Touch(cropInfo.Value, farmer, cropInfo.Value.quantity * scaler / field.size, batchSize);
      cropInfo.Value.AddAttribute(StaticAttributes.cropHealth!, delta);
    }
  }

  // Apply the effect to the target.
  public override void FinishSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    ISkillContext? farmer = chosenEffectTarget.runningContext as ISkillContext;
    if (farmer == null)
    {
      // Effect is optional, so just return.
      return;
    }
    if (chosenEffectTarget.target is Field field)
    {
      TouchAll(field, farmer, scaler, batchSize);
      return;
    }
    // Get the crop from the chosen target.
    Field.CropInfo cropInfo = (Field.CropInfo)chosenEffectTarget.target!;
    // Make sure the crop is not null.
    if (cropInfo == null)
    {
      // Effect is optional, so just return.
      return;
    }
    double delta = Touch(cropInfo, farmer, scaler, batchSize);
    cropInfo.AddAttribute(StaticAttributes.cropHealth!, delta);
  }

  public override bool IsOptional()
  {
    return true;
  }

  public override bool SupportsBatching()
  {
    return true;
  }

  private double Utility(IHouseholdContext household, Field.CropInfo cropInfo, ITaskRunner farmer, double scaler = 1)
  {
    double delta = Touch(cropInfo, farmer, scaler);
    // Changes to crop health affect future yield changes, but not existing yield,
    // so we calculate the utility based on the number of days until the crop is
    // mature.
    int cropDay = (int)cropInfo.GetAttributeValue(cropInfo.itemType.cropSettings!.cropAttribute!);
    int totalDays = cropInfo.itemType.cropSettings!.totalDays;
    if (cropDay >= totalDays)
    {
      // Crop is already mature, so the utility is zero.
      return 0;
    }
    double dailyYield = cropInfo.itemType.cropSettings!.perTickYieldGrowth * Calendar.ticksPerDay;
    double futureYield = dailyYield * (totalDays - cropDay);
    // Delta is scaled to an acre already and has the appropriate sign,
    // so we just have to treat it as a percentage.
    double futureYieldDelta = delta * futureYield / 100;
    return household.household.FutureUtility(farmer, cropInfo.itemType, (int)futureYieldDelta, totalDays - cropDay);
  
  }

  public override double Utility(IHouseholdContext household, ITaskRunner runner, ChosenEffectTarget chosenEffectTarget, double scaler = 1)
  {
    // We could delegate the calculation here to the Attribute, but since this is
    // the main/only crop health effect initiated by a task, we'll just do it here
    // for more control.
    ITaskRunner? farmer = chosenEffectTarget.runningContext as ITaskRunner;
    if (farmer == null)
    {
      return 0;
    }
    double utility = 0;
    if (chosenEffectTarget.target is Field field)
    {
      foreach (var crop in field.crops)
      {
        utility += Utility(household, crop.Value, farmer, scaler);
      }
      return utility;
    }
    // Get the crop from the chosen target.
    Field.CropInfo cropInfo = (Field.CropInfo)chosenEffectTarget.target!;
    // Make sure the crop is not null.
    if (cropInfo == null)
    {
      return 0;
    }
    return Utility(household, cropInfo, farmer, scaler);
  }

  private AbilityValue healthRate;
}

public class CropSkillEffect : Effect
{
  public CropSkillEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a crop or Field.
    if (target != EffectTargetType.Crop && target != EffectTargetType.Field)
    {
      throw new Exception("Touch Crop effect must target a crop or field: " + effect);
    }
    if (data == null)
    {
      throw new Exception("Touch Crop effect must have a config dictionary: " + effect);
    }
    if (data.ContainsKey("amount"))
    {
      amount = AbilityValue.FromJson(data["amount"]);
    }
  }

  private void Learn(Field.CropInfo cropInfo, ISkillContext farmer, double scaler = 1, int batchSize = 1)
  {
    ItemType crop = cropInfo.itemType;
    double amount = this.amount.GetScaledValue(farmer, scaler) * batchSize;
    SkillEffect.GiveSkillXP(farmer, crop.cropSettings!.cropSkill!, amount, crop.cropSettings!.cropSkillLevel);
  }

  private void LearnAll(Field field, ISkillContext farmer, double scaler = 1, int batchSize = 1)
  {
    foreach (var cropInfo in field.crops)
    {
      Learn(cropInfo.Value, farmer, cropInfo.Value.quantity * scaler / field.size, batchSize);
    }
  }

  // Apply the effect to the target.
  public override void FinishSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    ISkillContext? farmer = chosenEffectTarget.runningContext as ISkillContext;
    if (farmer == null)
    {
      // Effect is optional, so just return.
      return;
    }
    if (chosenEffectTarget.target is Field field)
    {
      LearnAll(field, farmer, scaler, batchSize);
      return;
    }
    // Get the crop from the chosen target.
    Field.CropInfo cropInfo = (Field.CropInfo)chosenEffectTarget.target!;
    // Make sure the crop is not null.
    if (cropInfo == null)
    {
      // Effect is optional, so just return.
      return;
    }
    Learn(cropInfo, farmer, scaler, batchSize);
  }

  public override bool IsOptional()
  {
    return true;
  }

  public override bool SupportsBatching()
  {
    return true;
  }

  private double Utility(ItemType crop, ISkillContext farmer, double scaler = 1)
  {
    int trainingLevel = crop.cropSettings!.cropSkillLevel;
    double amount = this.amount.GetScaledValue(farmer, scaler);
    return farmer.Utility(crop.cropSettings!.cropSkill!, trainingLevel, amount);
  }
  public override double Utility(IHouseholdContext household, ITaskRunner runner, ChosenEffectTarget chosenEffectTarget, double scaler = 1)
  {
    ISkillContext farmer = (ISkillContext)chosenEffectTarget.target!;
    if (farmer == null)
    {
      return 0.0;
    }
    
    if (chosenEffectTarget.target is Field field)
    {
      double utility = 0;
      foreach (var crop in field.crops)
      {
        utility += Utility(crop.Value.itemType, farmer, scaler);
      }
      return utility;
    }
    // Get the crop from the chosen target.
    Field.CropInfo? cropInfo = chosenEffectTarget.target as Field.CropInfo;
    // Make sure the crop is not null.
    if (cropInfo == null)
    {
      return 0.0;
    }
    return Utility(cropInfo.itemType, farmer, scaler);
  }

  // Amount to increase, defaults to 1.
  public AbilityValue amount = new AbilityValue(1);
}

public class GrowCropEffect : Effect
{
  public const double fullETGrowth = 15;
  public const double waterEpsilon = 0.001;
  public const double waterStressHealthEffect = 0.5;
  public const double nutrientStressHealthEffect = 0.05;
  public const double plantingStressHealthEffect = 0.1;
  public const double frostStressHealthEffect = 0.5;
  public const double heavyFrostDegrees = 4;
  public const double heavyFrostStressHealthEffect = 2;
  public const double heatStressHealthEffect = 0.1;
  public const double weedStressHealthEffect = 0.1;
  public const double rotPercentOnCropDeath = 0.2;
  public const double rotPercentPerTick = 0.005;
  public GrowCropEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a crop.
    if (target != EffectTargetType.Crop)
    {
      throw new Exception("Grow Crop effect must target a crop: " + effect);
    }
  }

  public static double SoilQualityEffect(ItemType crop, double soilQuality)
  {
    double minSoilQuality = crop.cropSettings!.minSoilQuality;
    double soilQualityDeficitMultiplier = Math.Clamp(soilQuality / minSoilQuality, 0.1, 1);
    return soilQualityDeficitMultiplier;
  }

  // Apply the effect to the target.
  public override void FinishSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // TODO(chmeyers): Give penalties for fields with multiple dissimilar crop types, to encourage
    // separating crops by field and setting up a rotation. Except for gardens!
    // Get the crop from the chosen target.
    Field.CropInfo cropInfo = (Field.CropInfo)chosenEffectTarget.target!;
    // Make sure the crop is not null.
    if (cropInfo == null)
    {
      // This effect should never be called without a valid target crop.
      throw new Exception("Crop Info is null in harvest crop effect: " + effect);
    }
    ItemType crop = cropInfo.itemType;
    double currentHealth = cropInfo.GetAttributeValue(StaticAttributes.cropHealth!);
    if (currentHealth <= 0)
    {
      // The crop is dead.
      double totalRate = 1 - Math.Pow(1 - rotPercentPerTick, batchSize);
      RotCropEffect.Rot(cropInfo, totalRate);
      return;
    }
    int cropDay = (int)cropInfo.GetUnscaledAttributeValue(cropInfo.itemType.cropSettings!.cropAttribute!);
    // Determine the maximum yield gain for this tick.
    double yieldGain = cropInfo.itemType.cropSettings!.currentYieldGrowth(cropDay) * scaler * batchSize;
    double healthPenalty = 0.0;
    // The maximum yield gain varies based on the seasonal growth.
    double seasonalGrowth = cropInfo.GetAttributeValue(StaticAttributes.seasonalGrowth!);
    yieldGain *= seasonalGrowth;
    // Multiply the maximum yield based on the crop's health,
    // note that the health can be greater than 100% if you are a skilled farmer.
    double currentHealthPercentage = cropInfo.GetUnscaledAttributeValue(StaticAttributes.cropHealth!) / 100;
    yieldGain *= currentHealthPercentage;
    // Reduce the maximum yield based on the lack of soil quality.
    double soilQuality = cropInfo.GetUnscaledAttributeValue(StaticAttributes.soilQuality!);
    double soilQualityDeficitMultiplier = SoilQualityEffect(crop, soilQuality);
    yieldGain *= soilQualityDeficitMultiplier;

    // Weeds
    // During the first weedSusceptibleDays, the crop is susceptible to weeds,
    // and will receive both a health penalty and a yield penalty.
    // After the first weedSusceptibleDays, the crop will only receive a yield penalty.
    // The penalties are based on the percentage of weeds in the field.
    double weedPercentage = cropInfo.GetUnscaledAttributeValue(StaticAttributes.weeds!) / 100;
    // Ignore 5% of weeds, as low levels of weeds are not a problem.
    weedPercentage = Math.Max(0, weedPercentage - 0.05);
    double weedSusceptibleDays = cropInfo.itemType.cropSettings!.weedSusceptibleDays;
    if (cropDay <= weedSusceptibleDays)
    {
      // The crop is susceptible to weeds.
      // The health penalty is based on the percentage of weeds in the field.
      healthPenalty += weedPercentage * scaler * batchSize * weedStressHealthEffect;
      // The yield penalty is based on the percentage of weeds in the field.
      yieldGain *= Math.Max(0, 1 - weedPercentage);
    }
    else
    {
      // The crop is not susceptible to weeds.
      // The yield penalty is based on the percentage of weeds in the field.
      yieldGain *= Math.Max(0, 1 - weedPercentage);
    }

    // Whether the crop actually get's it's maximum yield gain depends several factors.
    // 1) It must have enough water.
    // Determine the current water needs of the crop.
    // The water needs are based on the current et and the kc of the crop for it's
    // current stage.
    double et = cropInfo.GetAttributeValue(StaticAttributes.weeklyTickET!);
    double kc = cropInfo.itemType.cropSettings!.currentKC(cropDay);
    double waterNeeds = et * kc * scaler * batchSize + waterEpsilon;
    // Determine the current water available to the crop.
    // The water available is based on the current surface and deep moisture.
    double surfaceMoisture = cropInfo.GetAttributeValue(StaticAttributes.surfaceMoisture!);
    double deepMoisture = cropInfo.GetAttributeValue(StaticAttributes.deepMoisture!);
    double waterAvailable = surfaceMoisture + deepMoisture;
    // If the crop is still in the first init days, it can only use the surface moisture.
    if (cropDay <= cropInfo.itemType.cropSettings!.initDays)
    {
      waterAvailable = surfaceMoisture;
    }
    // Yield gain is reduced by the the percentage of water needs that are not met.
    yieldGain *= Math.Min(1, waterAvailable / waterNeeds);
    // Determine the water stress. 0 = no stress, 1 = full stress.
    double waterStress = Math.Max(0, 1 - waterAvailable / waterNeeds - cropInfo.itemType.cropSettings!.droughtTolerance);
    healthPenalty += waterStress * scaler * batchSize * waterStressHealthEffect;
    // Remove the moisture that we used, starting with the surface moisture.
    cropInfo.SetAttribute(StaticAttributes.surfaceMoisture!, Math.Max(0, surfaceMoisture - waterNeeds));
    // If we still need more water, remove it from the deep moisture.
    if (waterNeeds > surfaceMoisture && cropDay > cropInfo.itemType.cropSettings!.initDays)
    {
      cropInfo.SetAttribute(StaticAttributes.deepMoisture!, Math.Max(0, deepMoisture - (waterNeeds - surfaceMoisture)));
    }

    // 2) It must have enough nutrients (NPK).
    // Determine the nutrient needs of the crop to gain the specified yield.
    double nitrogenNeeds = cropInfo.itemType.cropSettings!.totalNitrogen * yieldGain;
    double phosphorusNeeds = cropInfo.itemType.cropSettings!.totalPhosphorus * yieldGain;
    double potassiumNeeds = cropInfo.itemType.cropSettings!.totalPotassium * yieldGain;
    // Nitrogen fixing crops get a percentage of their nitrogen needs for free.
    nitrogenNeeds *= (1 - cropInfo.itemType.cropSettings!.nitrogenFixing);
    // Determine the current nutrients available to the crop.
    double nitrogenAvailable = cropInfo.GetAttributeValue(StaticAttributes.nitrogen!);
    double phosphorusAvailable = cropInfo.GetAttributeValue(StaticAttributes.phosphorus!);
    double potassiumAvailable = cropInfo.GetAttributeValue(StaticAttributes.potassium!);

    // Determine which nutrient is the most limiting.
    double nitrogenStress = Math.Max(0, 1 - nitrogenAvailable / nitrogenNeeds);
    // Nitrogen fixing crops nitrogen stress is capped by the fixing percentage.
    nitrogenStress = Math.Min((1 - cropInfo.itemType.cropSettings!.nitrogenFixing), nitrogenStress);

    double phosphorusStress = Math.Max(0, 1 - phosphorusAvailable / phosphorusNeeds);
    double potassiumStress = Math.Max(0, 1 - potassiumAvailable / potassiumNeeds);
    double nutrientStress = Math.Max(nitrogenStress, Math.Max(phosphorusStress, potassiumStress));
    healthPenalty += nutrientStress * scaler * batchSize * nutrientStressHealthEffect;
    // Yield gain is reduced by the the percentage of nutrient needs that are not met.
    yieldGain *= Math.Max(0, 1 - nutrientStress);
    // Recalculate the nutrient needs with the reduced yield gain.
    nitrogenNeeds = cropInfo.itemType.cropSettings!.totalNitrogen * yieldGain;
    phosphorusNeeds = cropInfo.itemType.cropSettings!.totalPhosphorus * yieldGain;
    potassiumNeeds = cropInfo.itemType.cropSettings!.totalPotassium * yieldGain;
    // Remove the nutrients that we used.
    cropInfo.AddAttribute(StaticAttributes.nitrogen!, -nitrogenNeeds);
    cropInfo.AddAttribute(StaticAttributes.phosphorus!, -phosphorusNeeds);
    cropInfo.AddAttribute(StaticAttributes.potassium!, -potassiumNeeds);

    // 3) The temperature must be within the range of the crop.
    // Determine the current temperature.
    double weeklyHigh = cropInfo.GetAttributeValue(StaticAttributes.weeklyHigh!);
    double weeklyLow = cropInfo.GetAttributeValue(StaticAttributes.weeklyLow!);
    double soilTemp = (weeklyHigh + weeklyLow) / 2;
    // Plants get a health penalty if the soil temperature is below their planting temp
    // during the init days.
    if (cropDay <= cropInfo.itemType.cropSettings!.initDays && soilTemp < cropInfo.itemType.cropSettings!.minPlantingTemp)
    {
      healthPenalty += scaler * batchSize * plantingStressHealthEffect;
    }
    // Plants get a health penalty if the low temperature is below their frost temp.
    // and a larger penalty if it is 5 degrees below that.
    if (weeklyLow < cropInfo.itemType.cropSettings!.frostTolerance)
    {
      healthPenalty += scaler * batchSize * frostStressHealthEffect;
      if (weeklyLow < cropInfo.itemType.cropSettings!.frostTolerance - heavyFrostDegrees)
      {
        // This is probably enough to kill the crop.
        healthPenalty += scaler * batchSize * heavyFrostStressHealthEffect;
      }
    }
    // Plants get a health penalty if the high temperature is above their heat temp.
    if (weeklyHigh > cropInfo.itemType.cropSettings!.heatTolerance)
    {
      healthPenalty += scaler * batchSize * heatStressHealthEffect;
    }

    // Apply the health penalty and yield gain.
    cropInfo.AddAttribute(StaticAttributes.cropHealth!, -healthPenalty);
    cropInfo.AddAttribute(StaticAttributes.cropYield!, yieldGain);
    if (currentHealth - healthPenalty <= 0)
    {
      // The crop died.
      // Immediately Rot 20% of the crop.
      RotCropEffect.Rot(cropInfo, rotPercentOnCropDeath);
    }
  }

  public override bool IsOptional()
  {
    // crop yield effects are not optional.
    // If you can't apply the effect, then you shouldn't run the task.
    return false;
  }

  public override bool SupportsBatching()
  {
    return true;
  }

}


public class FieldMaintenanceEffect : Effect
{
  public const double bareSoilEvaporationConstant = 0.5;
  public const double weedsDeepMoistureCutoff = 0.3;
  public const double lowWeedsCutoff = 0.05;
  public const double daysToWeedsCutoff = 30;
  public const double daysToFullWeeds = 20;
  public const double daysToSuppressWeeds = 60;
  public const double weedsKCValue = 0.75;
  public FieldMaintenanceEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a field.
    if (target != EffectTargetType.Field)
    {
      throw new Exception("Field Maintenance effect must target a field: " + effect);
    }
  }

  // Apply the effect to the target.
  public override void FinishSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Get the field from the chosen target.
    Field field = (Field)chosenEffectTarget.target!;
    // Make sure the field is not null.
    if (field == null)
    {
      // This effect should never be called without a valid target field.
      throw new Exception("Field is null in field maintenance effect: " + effect);
    }

    // Determine the percentage of the field that is covered in weeds and crops.
    double weedsPercentage = field.GetUnscaledAttributeValue(StaticAttributes.weeds!) / 100;
    double cropPercentage = field.GetCropCanopyUtilization();
    double targetWeeds = 1 - cropPercentage;
    double canopyPercentage = Math.Min(1.0, cropPercentage + weedsPercentage);

    // Note: It's assumed that Drainage was called before this effect.

    // Evaporate
    // Evaporation depends on the canopy percentage, at 100% the evaporation is 0,
    // and water loss depends only on usage by the plants. Below that, we have to
    // calculate the evaporation based on the weekly et.
    double surfaceMoisture = field.GetAttributeValue(StaticAttributes.surfaceMoisture!);
    double perTickEt = field.GetAttributeValue(StaticAttributes.weeklyTickET!);
    if (canopyPercentage < 1.0 && surfaceMoisture > 0)
    {
      double evaporation = perTickEt * scaler * batchSize * (1 - canopyPercentage) * bareSoilEvaporationConstant;
      if (evaporation > surfaceMoisture)
      {
        evaporation = surfaceMoisture;
      }
      surfaceMoisture -= evaporation;
    }

    // Rain
    // Rain is added to the surface moisture.
    // Note that weeds get first dibs on the new surface water, but we
    // don't apply the max surface water limit until after the weeds
    // have taken their share.
    double rain = (field.GetAttributeValue(StaticAttributes.weeklyRain!) / Calendar.ticksPerWeek) * scaler * batchSize;
    surfaceMoisture += rain;

    // Grow/Diminish Weeds
    // Weeds will move towards the target percentage of the field by
    // at most perTickWeedChange each tick and are penalized if there
    // is a water deficit.
    double weedsWaterNeeds = perTickEt * scaler * batchSize * weedsKCValue * weedsPercentage;
    // Have a default value for the water satisfied, in case there are no current weeds.
    double waterSatisfied = surfaceMoisture > 0 ? 1.0 : 0.0;
    if (weedsWaterNeeds > 0)
    {
      // Weeds can only take from surface moisture, unless they are
      // above the deep moisture cutoff.
      double weedsWaterAvailable = surfaceMoisture;
      if (weedsPercentage >= weedsDeepMoistureCutoff)
      {
        weedsWaterAvailable += field.GetAttributeValue(StaticAttributes.deepMoisture!);
      }
      waterSatisfied = Math.Min(1, weedsWaterAvailable / weedsWaterNeeds);
      double weedsWaterUsed = weedsWaterNeeds * waterSatisfied;
      if (weedsWaterUsed > surfaceMoisture)
      {
        weedsWaterUsed -= surfaceMoisture;
        surfaceMoisture = 0;
      }
      else
      {
        surfaceMoisture -= weedsWaterUsed;
        weedsWaterUsed = 0;
      }
      if (weedsWaterUsed > 0 && weedsPercentage >= weedsDeepMoistureCutoff)
      {
        field.AddAttribute(StaticAttributes.deepMoisture!, -weedsWaterUsed);
      }
    }

    double perTickWeedPercentageChange = 0;
    if (weedsPercentage >= targetWeeds)
    {
      // Weeds are above the target percentage, so they will diminish.
      // They diminish at up to double speed if there is a water shortage.
      perTickWeedPercentageChange = (targetWeeds - weedsPercentage) * (2 - waterSatisfied) / (daysToSuppressWeeds * Calendar.ticksPerDay);
    }
    else if (weedsPercentage > weedsDeepMoistureCutoff)
    {
      // Weeds are big and grow fast.
      perTickWeedPercentageChange = (1.0 - weedsDeepMoistureCutoff) * waterSatisfied / (daysToFullWeeds * Calendar.ticksPerDay);
    }
    else
    {
      // Weeds are small and grow slowly.
      perTickWeedPercentageChange = weedsDeepMoistureCutoff * waterSatisfied / (daysToWeedsCutoff * Calendar.ticksPerDay);
      // Half the growth rate if we are near zero.
      if (weedsPercentage <= lowWeedsCutoff)
      {
        perTickWeedPercentageChange /= 2;
      }
    }
    // Weeds grow slowly below 40 degrees, and not at all below 32.
    double weeklyLow = field.GetAttributeValue(StaticAttributes.weeklyLow!);
    if (weeklyLow < 32)
    {
      perTickWeedPercentageChange = 0;
    }
    else if (weeklyLow < 40)
    {
      perTickWeedPercentageChange *= (weeklyLow - 32) / 8;
    }
    
    double weedsChange = perTickWeedPercentageChange * 100 * scaler * batchSize;
    field.AddAttribute(StaticAttributes.weeds!, weedsChange);

    // Set the surface moisture.
    field.SetAttribute(StaticAttributes.surfaceMoisture!, surfaceMoisture);
  }

  public override bool IsOptional()
  {
    // field maintenance effects are not optional.
    // If you can't apply the effect, then you shouldn't run the task.
    return false;
  }

  public override bool SupportsBatching()
  {
    return true;
  }
}