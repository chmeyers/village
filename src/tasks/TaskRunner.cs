using Village.Abilities;
using Village.Base;
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
  // Choose a target for the given effect target.
  // This will return a ChosenEffectTarget if the target is valid, or null otherwise.
  // This will throw an exception if the target is invalid.
  public static ChosenEffectTarget? ChooseEffectTarget(EffectTarget effectTarget, Effect effect, Dictionary<string, ChosenEffectTarget>? chosenTargets, IEffectTargetContext? targetContext, IAbilityContext? runningContext, string taskName)
  {
    ChosenEffectTarget? chosenTarget = null;
    if (EffectTarget.IsTargetString(effectTarget.target))
    {
      // Match the target string to a chosen target.
      if (chosenTargets == null || !chosenTargets.ContainsKey(effectTarget.target))
      {
        // If the effect is optional, skip it, otherwise throw.
        if (!effect.IsOptional())
        {
          throw new Exception("Invalid effect target: " + effectTarget + " for effect: " + effect + " in task: " + taskName);
        }
        return null;
      }
      chosenTarget = chosenTargets[effectTarget.target];
      // throw if it's the wrong type.
      if (chosenTarget.effectTargetType != effectTarget.effectTargetType)
      {
        throw new Exception("Invalid effect target: " + effectTarget + " for effect: " + effect + " in task: " + taskName + " (type mismatch) : (" + chosenTarget.effectTargetType + " vs " + effectTarget.effectTargetType + ")");
      }
    }
    else
    {
      // The target isn't a target string, so resolve it.
      chosenTarget = EffectTargetResolver.ResolveEffectTarget(effect, effectTarget, targetContext, runningContext);
    }
    if (chosenTarget == null)
    {
      // If the effect is optional, skip it, otherwise throw.
      if (!effect.IsOptional())
      {
        throw new Exception("Invalid effect target: " + effectTarget + " for effect: " + effect + " in task: " + taskName);
      }
      return null;
    }
    return chosenTarget;
  }

  // Start a task for a person, with the given list of Chosen Targets
  // If the task can be performed, returns a RunningTask and removes the
  // inputs from the target inventory. Otherwise, returns null.
  public static RunningTask? StartTask(Person person, IInventoryContext target, WorkTask task, Dictionary<string, ChosenEffectTarget>? chosenTargets, double scale = 1.0)
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
      // Verify and actualize all the targets.
      Dictionary<Effect, List<ChosenEffectTarget>> targetDict = new Dictionary<Effect, List<ChosenEffectTarget>>();
      foreach (var effect in task.effects)
      {
        targetDict[effect.Key] = new List<ChosenEffectTarget>();
        foreach (var effectTarget in effect.Value)
        {
          ChosenEffectTarget? chosenTarget = ChooseEffectTarget(effectTarget, effect.Key, chosenTargets, target, person, task.task);
          // continue if it's null, since it's optional. We'd have thrown an exception otherwise.
          if (chosenTarget == null) continue;
          targetDict[effect.Key].Add(chosenTarget);
        }
      }
      // Verify that the scale is acceptable.
      // If the task has outputs, the scale must be a whole number >= 1.0.
      if (task.outputs.Count > 0 && (scale != Math.Round(scale) || scale < 1.0))
      {
        throw new Exception("Invalid scale for task: " + task + " (" + scale + ") with outputs: " + task.outputs.Count);
      }
      // If the task has effects, the scale must be within each effect's scale range.
      foreach (var effect in targetDict)
      {
        // Optional effects just won't be run if we are out of scale.
        if (effect.Key.IsOptional()) continue;
        foreach (var effectTarget in effect.Value)
        {
          // Check the scale.
          if (scale < effect.Key.MinScale(effectTarget) || scale > effect.Key.MaxScale(effectTarget))
          {
            throw new Exception("Invalid scale for task: " + task + " (" + scale + ") with effect: " + effect.Key + " and target: " + target);
          }
        }
      }

      // Remove the inputs from the inventory or return null if they are not present.
      // The inventory will choose the worst version of the item that matches.
      var inputs = target.inventory.Get(task.Inputs(person, scale));
      if (inputs == null || !target.inventory.Remove(inputs))
      {
        return null;
      }
      var runningTask = new RunningTask(task, person, inputs, target, targetDict, Calendar.Ticks, scale);
      return runningTask;
    }
    return null;
  }

  // Finish a task.
  // This will add the outputs to the inventory, and apply the effects to the targets.
  public static void FinishTask(RunningTask runningTask, bool forceSync = false)
  {
    runningTask.Finish();
  }

  public static bool AdvanceTask(RunningTask runningTask, int ticks)
  {
    // TODO(chmeyers): Verify that the task is still valid. i.e. the tools
    // and buildings are still available to this person.
    // Advance the task by the given number of ticks.
    if (!runningTask.started)
    {
      runningTask.Start();
    }
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
    // Complete all the tasks in the person's zero cost task queue.
    while (person.zeroCostTasks.TryDequeue(out var zeroCostTask))
    {
      FinishTask(zeroCostTask);
    }
    // If the person has priority tasks, take from that queue.
    if (person.priorityTasks.Count > 0)
    {
      // Peek at the first item in the person's priority task queue.
      if (!person.priorityTasks.TryPeek(out var priorityTask))
      {
        // Should never happen since we just checked the count.
        // If this happens it's probably because something other than
        // the GameLoop is calling AdvanceTask.
        throw new Exception("Priority task queue is empty with Count > 0.");
      }
      // Advance the task by one tick.
      if (AdvanceTask(priorityTask, 1))
      {
        // If the task is finished, remove it from the queue.
        person.priorityTasks.TryDequeue(out _);
        return true;
      }
      return false;
    }
    // No priority tasks, so take from the running task queue.
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
  public static bool PerformTask(Person person, IInventoryContext target, WorkTask task, Dictionary<string, ChosenEffectTarget>? chosenTargets, double scale = 1.0)
  {
    var runningTask = StartTask(person, target, task, chosenTargets, scale);
    if (runningTask == null)
    {
      return false;
    }

    FinishTask(runningTask);

    return true;
  }
}