using Village.Attributes;
using Village.Base;
using Village.Buildings;
using Village.Households;
using Village.Items;

namespace Village.Utilities;

public class YieldEstimator
{

  public static double SoilQualityEffect(ItemType crop, double soilQuality)
  {
    double minSoilQuality = crop.cropSettings!.minSoilQuality;
    double soilQualityDeficitMultiplier = Math.Clamp(soilQuality / minSoilQuality, 0.1, 1);
    return soilQualityDeficitMultiplier;
  }
  public const double minPlantingMoisture = 1.0;
  public const double lowMoisturePenalty = 0.5;
  const double maxPlantingWeeds = 5.0;
  const double highWeedsPenalty = 0.5;
  // Estimate the yield of planting a given crop on a given field, with the farmers
  // from a given household.
  public static double EstimateYield(Field field, ItemType crop, IHouseholdContext household, double scale = 1.0)
  {
    if (crop.cropSettings == null) return 0;
    if (scale > field.size) return 0;

    // TODO(chmeyers): Support non-temperate climates.
    // TODO(chmeyers): Provide some lesser utility for shoulder seasons.
    if (!crop.cropSettings!.temperatePlantingMonths.Contains(Calendar.Month)) return 0;

    double soilQuality = field.GetUnscaledAttributeValue(StaticAttributes.soilQuality!);
    double moisture = field.GetUnscaledAttributeValue(StaticAttributes.surfaceMoisture!);
    double weeds = field.GetUnscaledAttributeValue(StaticAttributes.weeds!);
    
    if (Calendar.Ticks - field.lastPlowedTick > Field.recentlyPlowedLimit)
    {
      // Not recently plowed. Assume that plowing will take care of weeds.
      // TODO(chmeyers): Estimate NPK and soil quality effects? Or just have this
      // taken care of by household history with the crop?
      weeds = Math.Max(weeds, 5);
    }

    // TODO(chmeyers): Adjust this based on the field's/household's history with this crop.
    double targetYield = crop.cropSettings.targetYieldPerAcre * scale;

    targetYield *= SoilQualityEffect(crop, soilQuality);

    // The ground should be moist.
    if (moisture < minPlantingMoisture)
    {
      targetYield *= lowMoisturePenalty;
    }
    if (weeds > maxPlantingWeeds)
    {
      targetYield *= highWeedsPenalty;
    }

    double nitrogen = field.GetAttributeValue(StaticAttributes.nitrogen!);
    double phosphorus = field.GetAttributeValue(StaticAttributes.phosphorus!);
    double potassium = field.GetAttributeValue(StaticAttributes.potassium!);

    double nitrogenNeeds = crop.cropSettings.totalNitrogen * targetYield * (1 - crop.cropSettings.nitrogenFixing);
    if (nitrogen < nitrogenNeeds)
    {
      targetYield *= nitrogen / nitrogenNeeds;
    }
    double phosphorusNeeds = crop.cropSettings.totalPhosphorus * targetYield;
    if (phosphorus < phosphorusNeeds)
    {
      targetYield *= phosphorus / phosphorusNeeds;
    }
    double potassiumNeeds = crop.cropSettings.totalPotassium * targetYield;
    if (potassium < potassiumNeeds)
    {
      targetYield *= potassium / potassiumNeeds;
    }

    return targetYield;
  }
}
