
using Village.Abilities;
using Village.Base;
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
  // What scale is the task running at?
  public double scale;
  // Whether the task has been started.
  public bool started { get; private set; } = false;

  // The inventory that the task is running against.
  public IInventoryContext target { get; private set; }

  // The chosen targets for the task's effects.
  public Dictionary<Effect, List<ChosenEffectTarget>> chosenTargets { get; private set; }

  // The inputs that the task already consumed.
  // Used to provide refunds when the task is cancelled.
  public Dictionary<Item, int> inputs { get; private set; }

  public void Start()
  {
    if (started) return;
    started = true;
    foreach (var effect in chosenTargets)
    {
      foreach (var target in effect.Value)
      {
        effect.Key.Start(target, scale);
      }
    }
  }

  public void Finish()
  {
    if (!started)
    {
      // Don't allow skipping the start.
      Start();
    }
    ticksRemaining = 0;
    endTime = Calendar.Ticks;
    // Add the outputs to the inventory.
    foreach (var output in task.Outputs(owner, scale))
    {
      target.inventory.AddItem(output.Key, output.Value);
    }
    foreach (var effect in chosenTargets)
    {
      foreach (var target in effect.Value)
      {
        effect.Key.Finish(target, scale);
      }
    }
  }

  // Constructor.
  public RunningTask(WorkTask task, IAbilityContext owner, Dictionary<Item, int> inputs, IInventoryContext inventory, Dictionary<Effect, List<ChosenEffectTarget>> chosenTargets, long startTime, double scale = 1.0)
  {
    this.task = task;
    this.owner = owner;
    this.startTime = startTime;
    // Scale the time cost, but round up to the nearest tick, so that
    // tasks that take less than a tick still take a tick, unless they
    // already take zero ticks.
    this.ticksRemaining = (int)Math.Ceiling(task.timeCost.GetValue(owner) * scale);
    this.scale = scale;
    this.endTime = startTime + ticksRemaining;
    // Inputs should have already been scaled by the caller.
    this.inputs = inputs;
    this.target = inventory;
    this.chosenTargets = chosenTargets;
  }
}