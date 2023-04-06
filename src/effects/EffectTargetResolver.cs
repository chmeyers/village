using Village.Abilities;
using Village.Persons;

namespace Village.Effects;

public class EffectTargetResolver
{
  // Resolve a given effect target, given a person.
  // Returns a ChosenEffectTarget.
  public static ChosenEffectTarget? ResolveEffectTarget(EffectTarget effectTarget, Person person)
  {
    switch (effectTarget.effectTargetType)
    {
      case EffectTargetType.Village:
      case EffectTargetType.Environment:
        return new ChosenEffectTarget(effectTarget, null, null);
      case EffectTargetType.Building:
        // Not yet implemented, throw.
        throw new Exception("Building targets not yet implemented.");
      case EffectTargetType.Person:
        // Always return the passed person.
        return new ChosenEffectTarget(effectTarget, person, null);
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
          return new ChosenEffectTarget(effectTarget, items.Min(), person);
        }
        // The person doesn't have an item that gives the ability,
        // so return null and don't run the effect.
        return null;
    }
    return null;
  }
}