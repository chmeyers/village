using Village.Abilities;
using Village.Effects;
using Village.Items;
using Village.Persons;


namespace Village.Tasks;

// The TaskRunner class will perform tasks for a Person,
// resolving the inputs, outputs, and targets of the task.
// The TaskRunner will also handle the task's effects, applying
// them to the person, the village, or the environment.
public class TaskRunner
{
  // Start a task for a person, with the given list of Chosen Targets
  // If the task can be performed, returns a RunningTask and removes the
  // inputs from the target inventory. Otherwise, returns null.
  public static RunningTask? StartTask(Person person, IInventoryContext target, WorkTask task, Dictionary<string, ChosenEffectTarget>? chosenTargets)
  {
    // Verify that the size of the chosenTargets list matches the size of the task's targets list.
    // Or that the chosenTargets list is null iff the task's list is empty.
    if (chosenTargets == null && task.targets.Count > 0)
    {
      throw new Exception("Invalid number of chosen targets for task: " + task + " ( null vs " + task.targets.Count + ")");
    }
    if (chosenTargets != null && chosenTargets.Count != task.targets.Count)
    {
      throw new Exception("Invalid number of chosen targets for task: " + task + " (" + chosenTargets.Count + " != " + task.targets.Count + ")");
    }
    // Check that the task is potential. Inputs will be checked by the inventory later.
    if (person.PotentialTasks.Contains(task))
    {
      // Remove the inputs from the inventory or return null if they are not present.
      // The inventory will choose the worst version of the item that matches.
      var inputs = target.inventory.Get(task.Inputs(person));
      if (inputs == null || !target.inventory.Remove(inputs))
      {
        return null;
      }
      return new RunningTask(task, person, inputs, target, chosenTargets, Calendar.Ticks);
    }
    return null;
  }

  // Finish a task.
  // This will add the outputs to the inventory, and apply the effects to the targets.
  public static void FinishTask(RunningTask runningTask, bool forceSync = false)
  {
    runningTask.ticksRemaining = 0;
    runningTask.endTime = Calendar.Ticks;
    // Add the outputs to the inventory.
    foreach (var output in runningTask.task.Outputs(runningTask.owner))
    {
      runningTask.target.inventory.AddItem(output.Key, output.Value);
    }

    // For each effect, resolve the target and apply the effect.
    foreach (var effect in runningTask.task.effects)
    {
      // Effects are run once for each target in the list.
      foreach (var effectTarget in effect.Value)
      {
        ChosenEffectTarget? chosenTarget = null;
        if (EffectTarget.IsTargetString(effectTarget.target))
        {
          // Match the target string to a chosen target.
          if (runningTask.chosenTargets == null || !runningTask.chosenTargets.ContainsKey(effectTarget.target))
          {
            throw new Exception("Invalid target string: " + effectTarget.target + " for effect: " + effect.Key + " in task: " + runningTask.task);
          }
          chosenTarget = runningTask.chosenTargets[effectTarget.target];
        }
        else
        {
          // The target isn't a target string, so resolve it.
          IInventoryContext target = runningTask.target;
          chosenTarget = EffectTargetResolver.ResolveEffectTarget(effect.Key, effectTarget, target, runningTask.owner);
        }
        if (chosenTarget == null)
        {
          // If the effect is optional, skip it, otherwise throw.
          if (!effect.Key.IsOptional())
          {
            throw new Exception("Invalid effect target: " + effectTarget + " for effect: " + effect.Key + " in task: " + runningTask.task);
          }
          continue;
        }
        // Apply the effect to the chosen target.
        if (forceSync)
        {
          effect.Key.ApplySync(chosenTarget);
        }
        else
        {
          effect.Key.Apply(chosenTarget);
        }
      }
    }
  }

  public static bool AdvanceTask(RunningTask runningTask, int ticks)
  {
    // TODO(chmeyers): Verify that the task is still valid. i.e. the tools
    // and buildings are still available to this person.
    // Advance the task by the given number of ticks.
    runningTask.ticksRemaining -= ticks;
    // If the task is complete, finish it.
    if (runningTask.ticksRemaining <= 0)
    {
      FinishTask(runningTask);
      return true;
    }
    return false;
  }

  // Advance a person's task by one tick.
  // Returns true if the task is finished or they had no task, false otherwise.
  // This should only be called by the GameLoop.
  public static bool AdvanceTask(Person person)
  {
    // Peek at the first item in the person's running task queue.
    if (!person.runningTasks.TryPeek(out var runningTask))
    {
      // If there is no task, return true.
      return true;
    }
    // Advance the task by one tick.
    if (AdvanceTask(runningTask, 1))
    {
      // If the task is finished, remove it from the queue.
      person.runningTasks.TryDequeue(out _);
      return true;
    }
    return false;
  }

  // Have the person perform a task, with the given list of Chosen Targets
  // Immediately finishes the task, ignoring the time cost.
  // Returns true if the task was performed, false otherwise.
  public static bool PerformTask(Person person, IInventoryContext inventory, WorkTask task, Dictionary<string, ChosenEffectTarget>? chosenTargets, bool forceSync = false)
  {
    var runningTask = StartTask(person, inventory, task, chosenTargets);
    if (runningTask == null)
    {
      return false;
    }

    FinishTask(runningTask, forceSync);

    return true;
  }
}