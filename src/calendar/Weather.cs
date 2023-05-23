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

  private static AttributeType? _weeklyHigh = null;
  private static AttributeType? _weeklyLow = null;
  private static AttributeType? _weeklySun = null;
  private static AttributeType? _weeklyRain = null;

  public static void Init()
  {
    _weeklyHigh = AttributeType.Find("weekly_high") ?? throw new Exception("Unable to find weekly_high attribute.");
    _weeklyLow = AttributeType.Find("weekly_low") ?? throw new Exception("Unable to find weekly_low attribute.");
    _weeklySun = AttributeType.Find("weekly_sun") ?? throw new Exception("Unable to find weekly_sun attribute.");
    _weeklyRain = AttributeType.Find("weekly_rain") ?? throw new Exception("Unable to find weekly_rain attribute.");
  }

  public static void AdvanceWeather()
  {
    // Hardcoded weather for now based on the season.
    Season season = Calendar.Season;

    switch (season)
    {
      case Season.Spring:
        _attributes.SetValue(_weeklyHigh!, 70);
        _attributes.SetValue(_weeklyLow!, 45);
        _attributes.SetValue(_weeklySun!, 50);
        _attributes.SetValue(_weeklyRain!, 0.7);
        break;
      case Season.Summer:
        _attributes.SetValue(_weeklyHigh!, 90);
        _attributes.SetValue(_weeklyLow!, 60);
        _attributes.SetValue(_weeklySun!, 75);
        _attributes.SetValue(_weeklyRain!, 0.5);
        break;
      case Season.Fall:
        _attributes.SetValue(_weeklyHigh!, 65);
        _attributes.SetValue(_weeklyLow!, 40);
        _attributes.SetValue(_weeklySun!, 40);
        _attributes.SetValue(_weeklyRain!, 0.3);
        break;
      case Season.Winter:
        _attributes.SetValue(_weeklyHigh!, 40);
        _attributes.SetValue(_weeklyLow!, 20);
        _attributes.SetValue(_weeklySun!, 20);
        _attributes.SetValue(_weeklyRain!, 0.1);
        break;
    }

  }
}