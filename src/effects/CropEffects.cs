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
  }

  public override void FinishSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Nothing to do here.
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
    // TODO(chmeyers): Target should be a crop?
    // Target must be a field.
    if (target != EffectTargetType.Field)
    {
      throw new Exception("Harvest Crop effect must target a crop: " + effect);
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
    // The name of the attribute type to use for the yield.
    yieldAttributeTypeName = (string)data.GetValueOrDefault("yieldAttributeType", defaultYieldAttributeType);
  }

  public override void StartSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // TODO(chmeyers): Verify that the field has a crop to harvest here.
    Field field = (Field)chosenEffectTarget.target!;
    // Make sure the field is not null.
    if (field == null)
    {
      // This effect should never be called without a valid target field.
      throw new Exception("Field is null in harvest crop effect: " + effect);
    }
    // Advance the field before starting the harvest.
    field.Advance();
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
      throw new Exception("Field is null in harvest crop effect: " + effect);
    }
    // Determine the yield.
    double yield = 0;
    yield = field.GetValue(crop, yieldAttributeType!);
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
    if (!field.Harvest(crop, harvestAmount))
    {
      // This effect should never be called without a valid target field.
      throw new Exception("Unable to harvest crop in harvest crop effect: " + effect);
    }
    // Actual yield is the difference between the yield before and after the harvest.
    yield -= field.GetValue(crop, yieldAttributeType!);

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

public class GrowCropEffect : Effect
{
  public const string defaultYieldAttributeType = "crop_yield";
  public const string defaultHealthAttributeType = "crop_health";
  public const string defaultWeeklyTickEtAttributeType = "weekly_tick_et";
  public const string defaultSeasonalGrowthAttributeType = "seasonal_growth";
  public const string defaultWeeklyHighAttributeType = "weekly_high";
  public const string defaultWeeklyLowAttributeType = "weekly_low";
  public const string defaultSurfaceMoistureAttributeType = "surface_moisture";
  public const string defaultDeepMoistureAttributeType = "deep_moisture";
  public const string defaultSoilQualityAttributeType = "soil_quality";
  public const string defaultWeedsAttributeType = "weeds";
  public const string defaultNitrogenAttributeType = "nitrogen";
  public const string defaultPhosphorusAttributeType = "phosphorus";
  public const string defaultPotassiumAttributeType = "potassium";
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
  public GrowCropEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a field.
    if (target != EffectTargetType.Crop)
    {
      throw new Exception("Crop Yield effect must target a field: " + effect);
    }
    if (data == null)
    {
      throw new Exception("Crop Yield effect must have a config dictionary: " + effect);
    }
    yieldAttributeTypeName = (string)data.GetValueOrDefault("yieldAttributeType", defaultYieldAttributeType);
    healthAttributeTypeName = (string)data.GetValueOrDefault("healthAttributeType", defaultHealthAttributeType);
    weeklyEtAttributeTypeName = (string)data.GetValueOrDefault("weeklyEtAttributeType", defaultWeeklyTickEtAttributeType);
    seasonalGrowthAttributeTypeName = (string)data.GetValueOrDefault("seasonalGrowthAttributeType", defaultSeasonalGrowthAttributeType);
    weeklyHighAttributeTypeName = (string)data.GetValueOrDefault("weeklyHighAttributeType", defaultWeeklyHighAttributeType);
    weeklyLowAttributeTypeName = (string)data.GetValueOrDefault("weeklyLowAttributeType", defaultWeeklyLowAttributeType);
    surfaceMoistureAttributeTypeName = (string)data.GetValueOrDefault("surfaceMoistureAttributeType", defaultSurfaceMoistureAttributeType);
    deepMoistureAttributeTypeName = (string)data.GetValueOrDefault("deepMoistureAttributeType", defaultDeepMoistureAttributeType);
    soilQualityAttributeTypeName = (string)data.GetValueOrDefault("soilQualityAttributeType", defaultSoilQualityAttributeType);
    weedsAttributeTypeName = (string)data.GetValueOrDefault("weedsAttributeType", defaultWeedsAttributeType);
    nitrogenAttributeTypeName = (string)data.GetValueOrDefault("nitrogenAttributeType", defaultNitrogenAttributeType);
    phosphorusAttributeTypeName = (string)data.GetValueOrDefault("phosphorusAttributeType", defaultPhosphorusAttributeType);
    potassiumAttributeTypeName = (string)data.GetValueOrDefault("potassiumAttributeType", defaultPotassiumAttributeType);

  }

  public override void StartSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Nothing to do here.
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
      throw new Exception("Crop Info is null in harvest crop effect: " + effect);
    }
    ItemType crop = cropInfo.itemType;
    double currentHealth = cropInfo.GetAttributeValue(healthAttr!);
    if (currentHealth <= 0)
    {
      // The crop is dead.
      // TODO(chmeyers): Deal with dead crops. Remove from field and reincorporate nutrients.
      return;
    }
    int cropDay = (int)cropInfo.GetUnscaledAttributeValue(cropInfo.itemType.cropSettings!.cropAttribute!);
    // Determine the maximum yield gain for this tick.
    double yieldGain = cropInfo.itemType.cropSettings!.currentYieldGrowth(cropDay) * scaler * batchSize;
    double healthPenalty = 0.0;
    // The maximum yield gain varies based on the seasonal growth.
    double seasonalGrowth = cropInfo.GetAttributeValue(seasonalGrowthAttr!);
    yieldGain *= seasonalGrowth;
    // Reduce the maximum yield based on the crop's health.
    double currentHealthPercentage = cropInfo.GetUnscaledAttributeValue(healthAttr!) / 100;
    yieldGain *= currentHealthPercentage;
    // Reduce the maximum yield based on the lack of soil quality.
    double minSoilQuality = cropInfo.itemType.cropSettings!.minSoilQuality;
    double soilQuality = cropInfo.GetUnscaledAttributeValue(soilQualityAttr!);
    double soilQualityDeficitMultiplier = Math.Clamp(soilQuality / minSoilQuality, 0.1, 1);
    yieldGain *= soilQualityDeficitMultiplier;

    // Weeds
    // During the first weedSusceptibleDays, the crop is susceptible to weeds,
    // and will receive both a health penalty and a yield penalty.
    // After the first weedSusceptibleDays, the crop will only receive a yield penalty.
    // The penalties are based on the percentage of weeds in the field.
    double weedPercentage = cropInfo.GetUnscaledAttributeValue(weedsAttr!) / 100;
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
    double et = cropInfo.GetAttributeValue(weeklyTickEtAttr!);
    double kc = cropInfo.itemType.cropSettings!.currentKC(cropDay);
    double waterNeeds = et * kc * scaler * batchSize + waterEpsilon;
    // Determine the current water available to the crop.
    // The water available is based on the current surface and deep moisture.
    double surfaceMoisture = cropInfo.GetAttributeValue(surfaceMoistureAttr!);
    double deepMoisture = cropInfo.GetAttributeValue(deepMoistureAttr!);
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
    cropInfo.SetAttribute(surfaceMoistureAttr!, Math.Max(0, surfaceMoisture - waterNeeds));
    // If we still need more water, remove it from the deep moisture.
    if (waterNeeds > surfaceMoisture && cropDay > cropInfo.itemType.cropSettings!.initDays)
    {
      cropInfo.SetAttribute(deepMoistureAttr!, Math.Max(0, deepMoisture - (waterNeeds - surfaceMoisture)));
    }

    // 2) It must have enough nutrients (NPK).
    // Determine the nutrient needs of the crop to gain the specified yield.
    double nitrogenNeeds = cropInfo.itemType.cropSettings!.totalNitrogen * yieldGain;
    double phosphorusNeeds = cropInfo.itemType.cropSettings!.totalPhosphorus * yieldGain;
    double potassiumNeeds = cropInfo.itemType.cropSettings!.totalPotassium * yieldGain;
    // Nitrogen fixing crops get a percentage of their nitrogen needs for free.
    nitrogenNeeds *= (1 - cropInfo.itemType.cropSettings!.nitrogenFixing);
    // Determine the current nutrients available to the crop.
    double nitrogenAvailable = cropInfo.GetAttributeValue(nitrogenAttr!);
    double phosphorusAvailable = cropInfo.GetAttributeValue(phosphorusAttr!);
    double potassiumAvailable = cropInfo.GetAttributeValue(potassiumAttr!);

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
    cropInfo.AddAttribute(nitrogenAttr!, -nitrogenNeeds);
    cropInfo.AddAttribute(phosphorusAttr!, -phosphorusNeeds);
    cropInfo.AddAttribute(potassiumAttr!, -potassiumNeeds);

    // 3) The temperature must be within the range of the crop.
    // Determine the current temperature.
    double weeklyHigh = cropInfo.GetAttributeValue(weeklyHighAttr!);
    double weeklyLow = cropInfo.GetAttributeValue(weeklyLowAttr!);
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
    cropInfo.AddAttribute(healthAttr!, -healthPenalty);
    cropInfo.AddAttribute(yieldAttr!, yieldGain);
    if (currentHealth - healthPenalty <= 0)
    {
      // The crop is dead.
      // TODO(chmeyers): Deal with dead crops. Remove from field and reincorporate nutrients.
      // Decide if any of it is still harvestable, maybe if the day is near the end of the
      // crop's life cycle.
    }
  }

  // Initialize should resolve the attribute names to the actual attribute type.
  public override void Initialize()
  {
    yieldAttr = AttributeType.Find(yieldAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + yieldAttributeTypeName + " in effect: " + effect);
    healthAttr = AttributeType.Find(defaultHealthAttributeType) ?? throw new Exception("Unknown attribute type: " + defaultHealthAttributeType + " in effect: " + effect);
    weeklyTickEtAttr = AttributeType.Find(weeklyEtAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + weeklyEtAttributeTypeName + " in effect: " + effect);
    seasonalGrowthAttr = AttributeType.Find(defaultSeasonalGrowthAttributeType) ?? throw new Exception("Unknown attribute type: " + defaultSeasonalGrowthAttributeType + " in effect: " + effect);
    weeklyHighAttr = AttributeType.Find(weeklyHighAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + weeklyHighAttributeTypeName + " in effect: " + effect);
    weeklyLowAttr = AttributeType.Find(weeklyLowAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + weeklyLowAttributeTypeName + " in effect: " + effect);
    surfaceMoistureAttr = AttributeType.Find(surfaceMoistureAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + surfaceMoistureAttributeTypeName + " in effect: " + effect);
    deepMoistureAttr = AttributeType.Find(deepMoistureAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + deepMoistureAttributeTypeName + " in effect: " + effect);
    soilQualityAttr = AttributeType.Find(soilQualityAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + soilQualityAttributeTypeName + " in effect: " + effect);
    weedsAttr = AttributeType.Find(defaultWeedsAttributeType) ?? throw new Exception("Unknown attribute type: " + defaultWeedsAttributeType + " in effect: " + effect);
    nitrogenAttr = AttributeType.Find(nitrogenAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + nitrogenAttributeTypeName + " in effect: " + effect);
    phosphorusAttr = AttributeType.Find(phosphorusAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + phosphorusAttributeTypeName + " in effect: " + effect);
    potassiumAttr = AttributeType.Find(potassiumAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + potassiumAttributeTypeName + " in effect: " + effect);
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

  // The Attribute Type holding the crop yield.
  public string yieldAttributeTypeName;
  public AttributeType? yieldAttr;
  // The Attribute Type holding the crop health.
  public string healthAttributeTypeName;
  public AttributeType? healthAttr;
  // The Attribute Type holding the weekly et.
  public string weeklyEtAttributeTypeName;
  public AttributeType? weeklyTickEtAttr;
  // The Attribute Type holding the seasonal growth.
  public string seasonalGrowthAttributeTypeName;
  public AttributeType? seasonalGrowthAttr;
  // The Attribute Type holding the weekly high.
  public string weeklyHighAttributeTypeName;
  public AttributeType? weeklyHighAttr;
  // The Attribute Type holding the weekly low.
  public string weeklyLowAttributeTypeName;
  public AttributeType? weeklyLowAttr;
  // The Attribute Type holding the surface moisture.
  public string surfaceMoistureAttributeTypeName;
  public AttributeType? surfaceMoistureAttr;
  // The Attribute Type holding the deep moisture.
  public string deepMoistureAttributeTypeName;
  public AttributeType? deepMoistureAttr;
  // The Attribute Type holding the soil quality.
  public string soilQualityAttributeTypeName;
  public AttributeType? soilQualityAttr;
  // The Attribute Type holding the weeds.
  public string weedsAttributeTypeName;
  public AttributeType? weedsAttr;
  // The Attribute Type holding the nitrogen.
  public string nitrogenAttributeTypeName;
  public AttributeType? nitrogenAttr;
  // The Attribute Type holding the phosphorus.
  public string phosphorusAttributeTypeName;
  public AttributeType? phosphorusAttr;
  // The Attribute Type holding the potassium.
  public string potassiumAttributeTypeName;
  public AttributeType? potassiumAttr;

}


public class FieldMaintenanceEffect : Effect
{
  public const string defaultWeeklyTickEtAttributeType = "weekly_tick_et";
  public const string defaultWeeklyLowAttributeType = "weekly_low";
  public const string defaultWeeklyRainAttributeType = "weekly_rain";
  public const string defaultSurfaceMoistureAttributeType = "surface_moisture";
  public const string defaultDeepMoistureAttributeType = "deep_moisture";
  public const string defaultSoilQualityAttributeType = "soil_quality";
  public const string defaultDrainageAttributeType = "drainage";
  public const string defaultWeedsAttributeType = "weeds";
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
    if (data == null)
    {
      throw new Exception("Field Maintenance effect must have a config dictionary: " + effect);
    }
    // The names of the attribute types.
    weeklyTickEtAttributeTypeName = (string)data.GetValueOrDefault("weeklyTickEtAttributeType", defaultWeeklyTickEtAttributeType);
    weeklyLowAttributeTypeName = (string)data.GetValueOrDefault("weeklyLowAttributeType", defaultWeeklyLowAttributeType);
    weeklyRainAttributeTypeName = (string)data.GetValueOrDefault("weeklyRainAttributeType", defaultWeeklyRainAttributeType);
    surfaceMoistureAttributeTypeName = (string)data.GetValueOrDefault("surfaceMoistureAttributeType", defaultSurfaceMoistureAttributeType);
    deepMoistureAttributeTypeName = (string)data.GetValueOrDefault("deepMoistureAttributeType", defaultDeepMoistureAttributeType);
    soilQualityAttributeTypeName = (string)data.GetValueOrDefault("soilQualityAttributeType", defaultSoilQualityAttributeType);
    drainageAttributeTypeName = (string)data.GetValueOrDefault("drainageAttributeType", defaultDrainageAttributeType);
    weedsAttributeTypeName = (string)data.GetValueOrDefault("weedsAttributeType", defaultWeedsAttributeType);
  }

  public override void StartSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Nothing to do.
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
    double weedsPercentage = field.GetUnscaledAttributeValue(weedsAttr!) / 100;
    double cropPercentage = field.GetCropCanopyUtilization();
    double targetWeeds = 1 - cropPercentage;
    double canopyPercentage = Math.Min(1.0, cropPercentage + weedsPercentage);

    // Note: It's assumed that Drainage was called before this effect.

    // Evaporate
    // Evaporation depends on the canopy percentage, at 100% the evaporation is 0,
    // and water loss depends only on usage by the plants. Below that, we have to
    // calculate the evaporation based on the weekly et.
    double surfaceMoisture = field.GetAttributeValue(surfaceMoistureAttr!);
    double perTickEt = field.GetAttributeValue(weeklyTickEtAttr!);
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
    double rain = (field.GetAttributeValue(weeklyRainAttr!) / Calendar.ticksPerWeek) * scaler * batchSize;
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
        weedsWaterAvailable += field.GetAttributeValue(deepMoistureAttr!);
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
        field.AddAttribute(deepMoistureAttr!, -weedsWaterUsed);
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
    double weedsChange = perTickWeedPercentageChange * 100 * scaler * batchSize;
    field.AddAttribute(weedsAttr!, weedsChange);

    // Set the surface moisture.
    field.SetAttribute(surfaceMoistureAttr!, surfaceMoisture);
  }

  // Initialize should resolve the attribute names to the actual attribute type.
  public override void Initialize()
  {
    // Get the attribute types from the names.
    weeklyTickEtAttr = AttributeType.Find(weeklyTickEtAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + weeklyTickEtAttributeTypeName + " in effect: " + effect);
    weeklyLowAttr = AttributeType.Find(weeklyLowAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + weeklyLowAttributeTypeName + " in effect: " + effect);
    weeklyRainAttr = AttributeType.Find(weeklyRainAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + weeklyRainAttributeTypeName + " in effect: " + effect);
    surfaceMoistureAttr = AttributeType.Find(surfaceMoistureAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + surfaceMoistureAttributeTypeName + " in effect: " + effect);
    deepMoistureAttr = AttributeType.Find(deepMoistureAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + deepMoistureAttributeTypeName + " in effect: " + effect);
    soilQualityAttr = AttributeType.Find(soilQualityAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + soilQualityAttributeTypeName + " in effect: " + effect);
    drainageAttr = AttributeType.Find(drainageAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + drainageAttributeTypeName + " in effect: " + effect);
    weedsAttr = AttributeType.Find(weedsAttributeTypeName) ?? throw new Exception("Unknown attribute type: " + weedsAttributeTypeName + " in effect: " + effect);
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

  // The Attribute Types.
  public string weeklyTickEtAttributeTypeName;
  public AttributeType? weeklyTickEtAttr;
  public string weeklyLowAttributeTypeName;
  public AttributeType? weeklyLowAttr;
  public string weeklyRainAttributeTypeName;
  public AttributeType? weeklyRainAttr;
  public string surfaceMoistureAttributeTypeName;
  public AttributeType? surfaceMoistureAttr;
  public string deepMoistureAttributeTypeName;
  public AttributeType? deepMoistureAttr;
  public string soilQualityAttributeTypeName;
  public AttributeType? soilQualityAttr;
  public string drainageAttributeTypeName;
  public AttributeType? drainageAttr;
  public string weedsAttributeTypeName;
  public AttributeType? weedsAttr;
}