using Village.Households;
using Village.Items;
using Village.Persons;
using Village.Tasks;

namespace Village.Base;
public class GameLoop
{
  private HashSet<WorkTask> dailyTasks = new HashSet<WorkTask>();
  private Random random = new Random();

  // Constructor.
  public GameLoop()
  {
    Load();
  }

  // Load the game.
  public void Load()
  {
    dailyTasks = TaskSet.Find("daily") ?? throw new Exception("Failed to find daily taskset");
  }

  // Reset the game.
  public void Reset()
  {
    Calendar.Reset();
    // delete all the people in all the households.
    Person.global_persons.Clear();
    Household.global_households.Clear();
  }

  // Check whether the game can advance.
  public bool CanAdvance()
  {
    // To advance the game must be unpaused,
    // and each person in every player household must have an active task.
    // TODO(chmeyers): Remove the requirement of having an active task.
    foreach (var household in Household.global_households)
    {
      if (household.isPlayerHousehold)
      {
        foreach (var person in Person.global_persons[household])
        {
          if (person.runningTasks.Count == 0 && person.priorityTasks.Count == 0)
          {
            return false;
          }
        }
      }
    }
    // TODO(chmeyers): Implement the ability to pause the game.
    return true;
  }

  // Advance a household by one tick.
  public void AdvanceHousehold(Household household)
  {
    var start = Profiler.Start();
    // Once a week, advance the household's buildings, including fields.
    if (Calendar.StartOfWeek)
    {
      household.AdvanceBuildings();
    }
    start = Profiler.AddSample("Household.AdvanceBuildings", start);
    // Advance the people.
    foreach (var person in Person.global_persons[household])
    {
      // 1) Tick forward their running task.
      TaskRunner.AdvanceTask(person);
      // 2) Calculate their personal needs.
      // 3) Take from Household inventory.
      person.TakeNeedsFromHousehold();
      // 4) Transfer to Household inventory.
      person.GiveSurplusToHousehold();
      // 5) Do Mandatory tasks.
      if (Calendar.StartOfDay)
      {
        // Advance their attributes.
        person.attributes.Advance();
        // Pick a task from the "daily" taskset.
        person.PickTaskFromSet(dailyTasks, true);
      }
      // 6) Register remaining needs with household.
    }
    // 7) Calculate the household's needs.
    // 8) Start Buildings.
    // 9) Sell to Traders
    // 10) Buy from Traders
    // 11) Assign tasks to people.
    start = Profiler.AddSample("Household.PersonLoop", start);
    if (!household.isPlayerHousehold)
    {
      foreach (var person in Person.global_persons[household])
      {
        if (person.runningTasks.Count == 0)
        {
          person.PickTask();
        }
      }
    }
    start = Profiler.AddSample("Household.PickTasks", start);
    // 12) Hire other households' people.

    // TODO(chmeyers):
    // Spoil/rot food in the household inventory.
    // Degrade household buildings.
  }

  // Do pre-phase work for this household.
  public void StartHousehold(Household household)
  {
    var start = Profiler.Start();
    // Buy stuff.
    household.MakePurchases();
    start = Profiler.AddSample("Household.MakePurchases", start);
    // Submit market bids.
    household.SubmitBidPrices();
    start = Profiler.AddSample("Household.SubmitBids", start);
  }

  // Do pre-phase work for this tick.
  public void StartTick()
  {
    if (Calendar.StartOfWeek)
    {
      var start = Profiler.Start();
      // Clear all the markets bids.
      foreach (var market in Market.global_markets)
      {
        market.ClearBids();
      }
      start = Profiler.AddSample("Market.Clear", start);
      foreach (var maker in MarketMaker.global_marketMakers)
      {
        maker.SubmitBidPrices();
      }
      start = Profiler.AddSample("Maker.SubmitBids", start);
      foreach (var household in Household.global_households)
      {
        // TODO(chmeyers): Do this in a thread pool.
        StartHousehold(household);
      }
      start = Profiler.AddSample("Household.StartHousehold", start);
      foreach (var maker in MarketMaker.global_marketMakers)
      {
        maker.MakePurchases();
      }
      start = Profiler.AddSample("Maker.MakePurchases", start);
    }
  }

  // Prepare the household for the next tick.
  public void FinishHousehold(Household household)
  {
    // Submit market ask prices.
    household.SubmitAskPrices();
  }

  // Prepare for the next tick.
  public void FinishTick()
  {
    if (Calendar.StartOfWeek)
    {
      var start = Profiler.Start();
      // Clear all the markets.
      foreach (var market in Market.global_markets)
      {
        market.ClearAsks();
      }
      start = Profiler.AddSample("Market.Clear", start);
      // Get Asks from Market Makers.
      foreach (var maker in MarketMaker.global_marketMakers)
      {
        maker.Advance();
        maker.SubmitAskPrices();
      }
      start = Profiler.AddSample("Maker.SubmitAsks", start);
      // Finish each household.
      foreach (var household in Household.global_households)
      {
        // TODO(chmeyers): Do this in a thread pool.
        FinishHousehold(household);
      }
      start = Profiler.AddSample("Household.Finish", start);
    }
  }

  // Advance the game by one tick.
  public void Advance()
  {
    // Advance the calendar.
    Calendar.Advance();
    // Advance the weather once a week.
    if (Calendar.StartOfWeek)
    {
      WeatherAttributes.AdvanceWeather();
    }
    var start = Profiler.Start();
    StartTick();
    start = Profiler.AddSample("StartTick", start);
    // Advance the households.
    foreach (var household in Household.global_households)
    {
      // TODO(chmeyers): Do this in a thread pool.
      AdvanceHousehold(household);
    }
    start = Profiler.AddSample("Households.Advance", start);
    FinishTick();
    start = Profiler.AddSample("FinishTick", start);
  }

  public void Run()
  {
    Console.WriteLine("Start Clock Time:" + DateTime.Now.ToString("HH:mm:ss"));
    while (true)
    {
      if (CanAdvance())
      {
        Advance();
      }
      // Sleep for 100ms.
      // At some point I'm going to forget I put this here.
      // I'm going to spend hours trying to figure out why the game is running so slow.
      // Then I'm going to remember I put this here.
      // And I'm going to be very sad.
      // TODO(chmeyers): Remove this.
      //Thread.Sleep(100);
      // Print to the console every month.
      if (Calendar.Ticks % 300 == 0)
      {
        Console.WriteLine("Month: " + Calendar.Year + "-" + Calendar.Month + " Clock Time:" + DateTime.Now.ToString("HH:mm:ss"));

      }
      if (Calendar.Ticks > 600)
      {
        break;
      }
    }
  }
}