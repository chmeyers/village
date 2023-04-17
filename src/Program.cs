using Village.Abilities;
using Village.Effects;
using Village.Items;
using Village.Skills;
using Village.Tasks;

namespace Village
{
  class Program
  {
    // Load configurations from JSON files.
    public static void LoadConfig()
    {
      // Load the ability types.
      AbilityType.LoadFile("config/abilities/abilitytypes.json");
      // Load the item types, resources first since they are used by other items.
      ItemType.LoadFile("config/items/resources.jsonc");
      ItemType.LoadFile("config/items/tools.jsonc");
      ItemType.LoadFile("config/items/itemtypes.json");
      // Load the effects.
      EffectLoader.LoadFile("config/effects/effects.json");
      EffectLoader.LoadFile("config/effects/skill_effects.jsonc");
      // Load the tasks.
      WorkTask.LoadFile("config/tasks/tasks.json");
      // Load the skills, followed by the skill tree.
      Skill.LoadFile("config/skills/skills.jsonc");
      Skill.LoadParentsFile("config/skills/skilltree.json");
    }
    static void Main(string[] args)
    {
      Console.WriteLine("Village Entry Point");
      //Load configs
      LoadConfig();
      // Print the item types.
      foreach (ItemType itemType in ItemType.itemTypes.Values)
      {
        Console.WriteLine(itemType.itemType);
      }
      // Print the ability types.
      foreach (AbilityType abilityType in AbilityType.abilityTypes.Values)
      {
        Console.WriteLine(abilityType.abilityType);
      }
      // Print the effects.
      foreach (Effect effect in Effect.effects.Values)
      {
        Console.WriteLine(effect.effect);
      }
      // Print the tasks.
      foreach (WorkTask workTask in WorkTask.tasks.Values)
      {
        Console.WriteLine(workTask.task);
      }
      // Print the skills.
      foreach (Skill skill in Skill.skills.Values)
      {
        Console.WriteLine(skill.id + " " + skill.parents.Count);
      }
    }
  }
}
