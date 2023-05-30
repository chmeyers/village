

namespace Village.Attributes;

// Holds references to attributes that the code assumes exist.
public class StaticAttributes
{
  public const string nitrogenName = "nitrogen";
  public static AttributeType? nitrogen;
  public const string phosphorusName = "phosphorus";
  public static AttributeType? phosphorus;
  public const string potassiumName = "potassium";
  public static AttributeType? potassium;
  public const string cropYieldName = "crop_yield";
  public static AttributeType? cropYield;
  public const string cropHealthName = "crop_health";
  public static AttributeType? cropHealth;
  public const string weeklyTickETName = "weekly_tick_et";
  public static AttributeType? weeklyTickET;
  public const string seasonalGrowthName = "seasonal_growth";
  public static AttributeType? seasonalGrowth;
  public const string weeklyHighName = "weekly_high";
  public static AttributeType? weeklyHigh;
  public const string weeklyLowName = "weekly_low";
  public static AttributeType? weeklyLow;
  public const string weeklySunName = "weekly_sun";
  public static AttributeType? weeklySun;
  public const string weeklyRainName = "weekly_rain";
  public static AttributeType? weeklyRain;
  public const string surfaceMoistureName = "surface_moisture";
  public static AttributeType? surfaceMoisture;
  public const string deepMoistureName = "deep_moisture";
  public static AttributeType? deepMoisture;
  public const string soilQualityName = "soil_quality";
  public static AttributeType? soilQuality;
  public const string weedsName = "weeds";
  public static AttributeType? weeds;

  public static void Initialize(bool forgiving = false)
  {
    // Find the required attributes, or throw an exception if they don't exist,
    // unless we're in forgiving mode for testing since we don't want to have to
    // define all the attributes in the test data.
    nitrogen = AttributeType.Find(nitrogenName) ?? (forgiving ? null : throw new Exception($"Attribute {nitrogenName} not found."));
    phosphorus = AttributeType.Find(phosphorusName) ?? (forgiving ? null : throw new Exception($"Attribute {phosphorusName} not found."));
    potassium = AttributeType.Find(potassiumName) ?? (forgiving ? null : throw new Exception($"Attribute {potassiumName} not found."));
    cropYield = AttributeType.Find(cropYieldName) ?? (forgiving ? null : throw new Exception($"Attribute {cropYieldName} not found."));
    cropHealth = AttributeType.Find(cropHealthName) ?? (forgiving ? null : throw new Exception($"Attribute {cropHealthName} not found."));
    weeklyTickET = AttributeType.Find(weeklyTickETName) ?? (forgiving ? null : throw new Exception($"Attribute {weeklyTickETName} not found."));
    seasonalGrowth = AttributeType.Find(seasonalGrowthName) ?? (forgiving ? null : throw new Exception($"Attribute {seasonalGrowthName} not found."));
    weeklyHigh = AttributeType.Find(weeklyHighName) ?? (forgiving ? null : throw new Exception($"Attribute {weeklyHighName} not found."));
    weeklyLow = AttributeType.Find(weeklyLowName) ?? (forgiving ? null : throw new Exception($"Attribute {weeklyLowName} not found."));
    weeklySun = AttributeType.Find(weeklySunName) ?? (forgiving ? null : throw new Exception($"Attribute {weeklySunName} not found."));
    weeklyRain = AttributeType.Find(weeklyRainName) ?? (forgiving ? null : throw new Exception($"Attribute {weeklyRainName} not found."));
    surfaceMoisture = AttributeType.Find(surfaceMoistureName) ?? (forgiving ? null : throw new Exception($"Attribute {surfaceMoistureName} not found."));
    deepMoisture = AttributeType.Find(deepMoistureName) ?? (forgiving ? null : throw new Exception($"Attribute {deepMoistureName} not found."));
    soilQuality = AttributeType.Find(soilQualityName) ?? (forgiving ? null : throw new Exception($"Attribute {soilQualityName} not found."));
    weeds = AttributeType.Find(weedsName) ?? (forgiving ? null : throw new Exception($"Attribute {weedsName} not found."));
  }
}