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
  // Resolve a given effect target, given a person.
  // Returns a ChosenEffectTarget.
  static ChosenEffectTarget? ResolveEffectTarget(EffectTarget effectTarget, Person person)
  {
    switch (effectTarget.effectTargetType)
    {
      case EffectTargetType.Village:
      case EffectTargetType.Environment:
        return new ChosenEffectTarget(effectTarget, null);
      case EffectTargetType.Building:
        // Not yet implemented, throw.
        throw new Exception("Building targets not yet implemented.");
      case EffectTargetType.Person:
        // Always return the passed person.
        return new ChosenEffectTarget(effectTarget, person);
      case EffectTargetType.Item:
        // Resolve the item from the person's inventory.
        // Pick an item that gives the ability specified by the effect target.
        // If no item is found, return null, as the effect will not be applied.
        // This can happen if the person has the ability, but didn't get it from an item.
        // First get the list of itemtypes that give the ability.
        var targetAbility = AbilityType.Find(effectTarget.target);
        if (targetAbility == null)
        {
          throw new Exception("Invalid ability target: " + effectTarget.target);
        }
        // Get the list of items that give the ability.
        var items = person.ItemAbilities[targetAbility];
        if (items != null && items.Count > 0)
        {
          // Chose the worst item that gives the ability. The logic behind this
          // is that item effects are typically negative unless the item is
          // specified. i.e. degrade a tool when used.
          return new ChosenEffectTarget(effectTarget, items.Min());
        }
        // The person doesn't have an item that gives the ability,
        // so return null and don't run the effect.
        return null;
    }
    return null;
  }

  // Have the person perform a task, with the given list of Chosen Targets
  // Returns true if the task was performed, false otherwise.
  static bool PerformTask(Person person, WorkTask task, Dictionary<string, ChosenEffectTarget> chosenTargets)
  {
    // Verify that the size of the chosenTargets list matches the size of the task's argets list.
    if (chosenTargets.Count != task.targets.Count)
    {
      throw new Exception("Invalid number of chosen targets for task: " + task + " (" + chosenTargets.Count + " != " + task.targets.Count + ")");
    }
    // Check that the task is potential. Inputs will be checked by the inventory later.
    if (person.PotentialTasks.Contains(task))
    {
      // Remove the inputs from the inventory or return false if they are not present.
      // The inventory will choose the worst version of the item that matches.
      if (!person.Inventory.Remove(task.inputs))
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
            if (!chosenTargets.ContainsKey(effectTarget.target))
            {
              throw new Exception("Invalid target string: " + effectTarget.target + " for effect: " + effect.Key + " in task: " + task);
            }
            chosenTarget = chosenTargets[effectTarget.target];
          }
          else
          {
            // The target isn't a target string, so resolve it.
            chosenTarget = ResolveEffectTarget(effectTarget, person);
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
      

    }
    // The person did not perform the task.
    return false;
  }
}