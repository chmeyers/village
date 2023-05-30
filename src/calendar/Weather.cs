using Village.Attributes;

namespace Village.Base;

public class WeatherAttributes
{
  // TODO(chmeyers): There should be one set of attributes per environment.
  private static AttributeSet _attributes = new AttributeSet(null, null, null);
  // TODO(chmeyers): This should take an environment.
  public static AttributeSet GetWeather()
  {
    return _attributes;
  }

  public static void Init()
  {
  }

  public static void AdvanceWeather()
  {
    // Hardcoded weather for now based on the season.
    Season season = Calendar.Season;

    switch (season)
    {
      case Season.Spring:
        _attributes.SetValue(StaticAttributes.weeklyHigh!, 70);
        _attributes.SetValue(StaticAttributes.weeklyLow!, 45);
        _attributes.SetValue(StaticAttributes.weeklySun!, 50);
        _attributes.SetValue(StaticAttributes.weeklyRain!, 1.2);
        _attributes.SetValue(StaticAttributes.weeklyTickET!, 0.015);
        _attributes.SetValue(StaticAttributes.seasonalGrowth!, 1.0);
        break;
      case Season.Summer:
        _attributes.SetValue(StaticAttributes.weeklyHigh!, 85);
        _attributes.SetValue(StaticAttributes.weeklyLow!, 60);
        _attributes.SetValue(StaticAttributes.weeklySun!, 75);
        _attributes.SetValue(StaticAttributes.weeklyRain!, 0.9);
        _attributes.SetValue(StaticAttributes.weeklyTickET!, 0.025);
        _attributes.SetValue(StaticAttributes.seasonalGrowth!, 1.0);
        break;
      case Season.Fall:
        _attributes.SetValue(StaticAttributes.weeklyHigh!, 65);
        _attributes.SetValue(StaticAttributes.weeklyLow!, 40);
        _attributes.SetValue(StaticAttributes.weeklySun!, 40);
        _attributes.SetValue(StaticAttributes.weeklyRain!, 1.1);
        _attributes.SetValue(StaticAttributes.weeklyTickET!, 0.010);
        _attributes.SetValue(StaticAttributes.seasonalGrowth!, 1.0);
        break;
      case Season.Winter:
        _attributes.SetValue(StaticAttributes.weeklyHigh!, 40);
        _attributes.SetValue(StaticAttributes.weeklyLow!, 20);
        _attributes.SetValue(StaticAttributes.weeklySun!, 20);
        _attributes.SetValue(StaticAttributes.weeklyRain!, 0.4);
        _attributes.SetValue(StaticAttributes.weeklyTickET!, 0.005);
        _attributes.SetValue(StaticAttributes.seasonalGrowth!, 1.0);
        break;
    }

  }
}