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
  // Have the person perform a task, with the given list of Chosen Targets
  // Returns true if the task was performed, false otherwise.
  public static bool PerformTask(Person person, WorkTask task, Dictionary<string, ChosenEffectTarget>? chosenTargets)
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
      // Remove the inputs from the inventory or return false if they are not present.
      // The inventory will choose the worst version of the item that matches.
      if (!person.Inventory.Remove(task.Inputs(person)))
      {
        return false;
      }
      // Add the outputs to the inventory.
      person.Inventory.Add(task.outputs);
      // For each effect, resolve the target and apply the effect.
      foreach (var effect in task.effects)
      {
        // Effects are run once for each target in the list.
        foreach (var effectTarget in effect.Value)
        {
          ChosenEffectTarget? chosenTarget = null;
          if (EffectTarget.IsTargetString(effectTarget.target))
          {
            // Match the target string to a chosen target.
            if (chosenTargets == null || !chosenTargets.ContainsKey(effectTarget.target))
            {
              throw new Exception("Invalid target string: " + effectTarget.target + " for effect: " + effect.Key + " in task: " + task);
            }
            chosenTarget = chosenTargets[effectTarget.target];
          }
          else
          {
            // The target isn't a target string, so resolve it.
            chosenTarget = EffectTargetResolver.ResolveEffectTarget(effectTarget, person);
          }
          if (chosenTarget == null)
          {
            // The target was not resolved, so skip this effect.
            continue;
          }
          // Apply the effect to the chosen target.
          effect.Key.Apply(chosenTarget);
        }
        
      }
      return true;
    }
    // The person did not perform the task.
    return false;
  }
}