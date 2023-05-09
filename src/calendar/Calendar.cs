using Village.Abilities;
using Village.Attributes;

namespace Village.Base;

// Season of the year.
public enum Season
{
  // The first year starts in Spring.
  Spring,
  Summer,
  Fall,
  Winter
}

// Calendar which tracks the game time.
public class Calendar
{
  // Constants defining the length of a day, month, and year in ticks.
  public const uint ticksPerDay = 10;
  public const uint ticksPerMonth = 300;
  public const uint ticksPerYear = 3600;

  // Singleton instance of the calendar.
  private static Calendar global_calendar = new Calendar();
  // The current time in game ticks.
  // There are 10 ticks per game day.
  // 30 days per game month.
  // 12 months per game year.
  private long _ticks = 0;

  // The AttributeSet with the Calendar-based attributes.
  // TODO(chmeyers): Set the target and context for the attributes to the environment?
  private AttributeSet attributes = new AttributeSet(null, null, null);

  public event AbilitiesChanged? _AbilitiesChanged;

  public static event AbilitiesChanged? AbilitiesChanged
  {
    add
    {
      global_calendar._AbilitiesChanged += value;
    }
    remove
    {
      global_calendar._AbilitiesChanged -= value;
    }
  }

  // Constructor for the Calendar.
  private Calendar()
  {
    // Set the event handler for when the attributes change.
    attributes.AbilitiesChanged += (IAbilityProvider? addedProvider, IEnumerable<AbilityType>? added, IAbilityProvider? removedProvider, IEnumerable<AbilityType>? removed) =>
    {
      _AbilitiesChanged?.Invoke(addedProvider, added, removedProvider, removed);
    };
  }


  // The current time in game ticks.
  private long ticks { get { return _ticks; } }

  // The current time in game days.
  private long days { get { return _ticks / ticksPerDay; } }

  // The current time in game months.
  private long months { get { return _ticks / ticksPerMonth; } }

  // The current time in game years.
  private long year { get { return _ticks / ticksPerYear; } }

  // The current month of the year.
  private int month { get { return (int)(months % 12); } }

  // The current day of the month.
  private int day { get { return (int)(days % 30); } }

  // The current hour of the day.
  private int hour { get { return (int)(_ticks % ticksPerDay); } }

  // The current day of the year.
  private int dayOfYear { get { return (int)(days % 360); } }

  // The current season.
  private Season season { get { return (Season)(month / 3); } }

  // Whether this tick is the start of a new day.
  private bool startOfDay { get { return _ticks % ticksPerDay == 0; } }

  // Static versions of the above properties using the global calendar.
  public static long Ticks { get { return global_calendar.ticks; } }
  public static long Days { get { return global_calendar.days; } }
  public static long Months { get { return global_calendar.months; } }
  public static long Year { get { return global_calendar.year; } }
  public static int Month { get { return global_calendar.month; } }
  public static int Day { get { return global_calendar.day; } }
  public static int Hour { get { return global_calendar.hour; } }
  public static int DayOfYear { get { return global_calendar.dayOfYear; } }
  public static Season Season { get { return global_calendar.season; } }
  public static bool StartOfDay { get { return global_calendar.startOfDay; } }


  
  


  // Advance the calendar by the given number of ticks.
  public static void Advance(uint ticks)
  {
    SetTime(global_calendar._ticks + ticks);
  }

  // Advance one tick.
  public static void Advance()
  {
    Advance(1);
  }

  // Reset the calendar.
  public static void Reset()
  {
    SetTime(0);
  }

  // Set the calendar to the given time.
  public static void SetTime(long ticks)
  {
    global_calendar._ticks = ticks;
    // Set all the attributes to the DayOfYear.
    foreach (var attribute in global_calendar.attributes.attributes.Keys)
    {
      global_calendar.attributes.SetValue(attribute, global_calendar.dayOfYear);
    }
  }

  public static HashSet<AbilityType> CalendarAbilities()
  {
    return global_calendar.attributes.Abilities;
  }

  public static IAbilityCollection CalendarAbilityCollection()
  {
    return global_calendar.attributes;
  }

  private void AddAttribute(AttributeType attributeType)
  {
    // Verify that the attribute is calendar compatible, by checking
    // that the min is zero and the max 360.
    if (attributeType.minValue != 0 || attributeType.maxValue != 360)
    {
      throw new System.Exception("Attribute " + attributeType.name + " is not calendar compatible.");
    }

    attributes.Add(attributeType);
  }

  // Go through all the attributes and add the calendar attributes.
  public static void AddCalendarAttributes()
  {
    foreach (var attributeType in AttributeType.types.Values)
    {
      if (attributeType.calendar)
      {
        global_calendar.AddAttribute(attributeType);
        // Set the value of the attribute to the current day of the year.
        global_calendar.attributes.SetValue(attributeType, global_calendar.dayOfYear);
      }
    }
  }

}