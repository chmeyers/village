

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
  // Singleton instance of the calendar.
  private static Calendar global_calendar = new Calendar();
  // The current time in game ticks.
  // There are 10 ticks per game day.
  // 30 days per game month.
  // 12 months per game year.
  private long _ticks = 0;

  // The current time in game ticks.
  private long ticks { get { return _ticks; } }

  // The current time in game days.
  private long days { get { return _ticks / 10; } }

  // The current time in game months.
  private long months { get { return _ticks / 300; } }

  // The current time in game years.
  private long year { get { return _ticks / 3600; } }

  // The current month of the year.
  private int month { get { return (int)(months % 12); } }

  // The current day of the month.
  private int day { get { return (int)(days % 30); } }

  // The current hour of the day.
  private int hour { get { return (int)(_ticks % 10); } }

  // The current day of the year.
  private int dayOfYear { get { return (int)(days % 360); } }

  // The current season.
  private Season season { get { return (Season)(month / 3); } }

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
  

  // Advance the calendar by the given number of ticks.
  public static void Advance(uint ticks)
  {
    global_calendar._ticks += ticks;
  }

  // Advance one tick.
  public static void Advance()
  {
    Advance(1);
  }

  // Reset the calendar.
  public static void Reset()
  {
    global_calendar._ticks = 0;
  }

  // Set the calendar to the given time.
  public static void SetTime(long ticks)
  {
    global_calendar._ticks = ticks;
  }

}