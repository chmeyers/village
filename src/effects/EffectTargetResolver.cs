using Village.Abilities;
using Village.Items;
using Village.Persons;

namespace Village.Effects;

public class EffectTargetResolver
{
  // Resolve a given effect target, given a person.
  // Returns a ChosenEffectTarget.
  public static ChosenEffectTarget? ResolveEffectTarget(Effect effect, EffectTarget effectTarget, IInventoryContext? targetContext, IAbilityContext? runningContext)
  {
    switch (effectTarget.effectTargetType)
    {
      case EffectTargetType.Village:
      case EffectTargetType.Environment:
        return new ChosenEffectTarget(effectTarget.effectTargetType, null, null, runningContext);
      case EffectTargetType.Building:
        // Not yet implemented, throw.
        throw new Exception("Building targets not yet implemented.");
      case EffectTargetType.Person:
        // Check that the context is a person.
        Person? person = null;
        if (effectTarget.target == "")
        {
          // Targetting the runner.
          person = runningContext as Person;
        }
        else
        {
          // Everything else not yet supported.
          throw new NotImplementedException("Invalid effect target context for person target: " + targetContext);
        }
        return new ChosenEffectTarget(effectTarget.effectTargetType, person, targetContext, runningContext);
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
        // Check whether the effect always targets the runner.
        // Degrade Item effects always does this as it targets the tool used.
        var inventory = targetContext as IInventoryContext;
        if (effect.AlwaysTargetsRunner())
        {
          inventory = (runningContext as IInventoryContext)!;
        }
        // Get the list of items that give the ability.
        if (inventory == null)
        {
          throw new Exception("Invalid effect context for item target: " + targetContext);
        }

        if (inventory.inventory.AbilityProviders.ContainsKey(targetAbility))
        {
          // The inventories AbilityProviders are all Items, I promise.
          var items = inventory.inventory.AbilityProviders[targetAbility].Cast<Item>();
          // Chose the worst item that gives the ability. The logic behind this
          // is that item effects are typically negative unless the item is
          // specified. i.e. degrade a tool when used.
          return new ChosenEffectTarget(effectTarget.effectTargetType, items.Min(), targetContext, runningContext);
        }
        // The person doesn't have an item that gives the ability,
        // so return null and don't run the effect.
        return null;
    }
    return null;
  }
}