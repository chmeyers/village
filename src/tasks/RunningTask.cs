
using Village.Abilities;
using Village.Effects;
using Village.Items;

namespace Village.Tasks;

// A WorkTask that a person or persons is currently working on.
public class RunningTask
{
  // The task that is being run.
  public WorkTask task { get; private set; }
  // The person that is running the task.
  public IAbilityContext owner { get; private set; }
  // The game tick that the task was started.
  public long startTime;
  // The game tick that the task is estimated to end.
  public long endTime;
  // The number of ticks remaining until the task is complete.
  public int ticksRemaining;
  // Multiplier to apply to effects.
  public int effectMultiplier = 1;

  // The inventory that the task is running against.
  public IInventoryContext target { get; private set; }

  // The chosen targets for the task's effects.
  public Dictionary<string, ChosenEffectTarget>? chosenTargets { get; private set; }

  // The inputs that the task already consumed.
  // Used to provide refunds when the task is cancelled.
  public Dictionary<Item, int> inputs { get; private set; }

  // Constructor.
  public RunningTask(WorkTask task, IAbilityContext owner, Dictionary<Item, int> inputs, IInventoryContext inventory, Dictionary<string, ChosenEffectTarget>? chosenTargets, long startTime, int effectMultiplier = 1)
  {
    this.task = task;
    this.owner = owner;
    this.startTime = startTime;
    this.ticksRemaining = (int)task.timeCost.GetValue(owner);
    this.endTime = startTime + ticksRemaining;
    this.inputs = inputs;
    this.target = inventory;
    this.chosenTargets = chosenTargets;
    this.effectMultiplier = effectMultiplier;
  }
}