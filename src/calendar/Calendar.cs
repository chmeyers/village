

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
  // The current time in game ticks.
  // There are 10 ticks per game day.
  // 30 days per game month.
  // 12 months per game year.
  private long _ticks = 0;

  // The current time in game ticks.
  public long ticks { get { return _ticks; } }

  // The current time in game days.
  public long days { get { return _ticks / 10; } }

  // The current time in game months.
  public long months { get { return _ticks / 300; } }

  // The current time in game years.
  public long year { get { return _ticks / 3600; } }

  // The current month of the year.
  public int month { get { return (int)(months % 12); } }

  // The current day of the month.
  public int day { get { return (int)(days % 30); } }

  // The current hour of the day.
  public int hour { get { return (int)(_ticks % 10); } }

  // The current day of the year.
  public int dayOfYear { get { return (int)(days % 360); } }

  // The current season.
  public Season season { get { return (Season)(month / 3); } }

  // Advance the calendar by the given number of ticks.
  public void Advance(uint ticks)
  {
    _ticks += ticks;
  }

  // Advance one tick.
  public void Advance()
  {
    Advance(1);
  }

}