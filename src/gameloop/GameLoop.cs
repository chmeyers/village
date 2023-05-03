using Village.Households;
using Village.Persons;
using Village.Tasks;

namespace Village.Base;
public class GameLoop
{
  // Reset the game.
  public void Reset()
  {
    Calendar.global_calendar.Reset();
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
          if (person.runningTasks.Count == 0)
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
      // 5) Do zero-time tasks.
      // 6) Register remaining needs with household.
    }
    // 7) Calculate the household's needs.
    // 8) Sell to Traders
    // 9) Buy from Traders
    // 10) Assign tasks to people.
    // 11) Hire other households' people.

    // TODO(chmeyers):
    // Spoil/rot food in the household inventory.
    // Degrade household buildings.
  }

  // Advance the game by one tick.
  public void Advance()
  {
    // Advance the calendar.
    Calendar.global_calendar.Advance();
    // Advance the households.
    foreach (var household in Household.global_households)
    {
      // TODO(chmeyers): Do this in a thread pool.
      AdvanceHousehold(household);
    }
  }

  public void Run()
  {
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
      Thread.Sleep(100);
    }
  }
}