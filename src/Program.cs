using Village.Abilities;
using Village.Attributes;
using Village.Base;
using Village.Buildings;
using Village.Effects;
using Village.Households;
using Village.Items;
using Village.Persons;
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
      AbilityType.LoadFile("config/abilities/abilitytypes.jsonc");
      // Load the item types, resources first since they are used by other items.
      ItemType.LoadFile("config/items/resources.jsonc");
      ItemType.LoadFile("config/items/tools.jsonc");
      ItemType.LoadFile("config/items/item.jsonc");
      ItemType.LoadFile("config/items/food.jsonc");
      // Load the effects. Must be done before Attributes, Tasks, and Skills.
      EffectLoader.LoadFile("config/effects/effects.jsonc");
      EffectLoader.LoadFile("config/effects/building_components.jsonc");
      EffectLoader.LoadFile("config/effects/skill_effects.jsonc");
      EffectLoader.LoadFile("config/effects/skilltree.jsonc");
      // Load the attributes.
      AttributeType.LoadFile("config/attributes/attributes.jsonc");
      AttributeType.LoadFile("config/attributes/weather.jsonc");
      Calendar.AddCalendarAttributes();
      // Load the tasks.
      WorkTask.LoadFile("config/tasks/gathering.jsonc");
      WorkTask.LoadFile("config/tasks/crafting.jsonc");
      WorkTask.LoadFile("config/tasks/tool_crafting.jsonc");
      WorkTask.LoadFile("config/tasks/resources.jsonc");
      WorkTask.LoadFile("config/tasks/building.jsonc");
      WorkTask.LoadFile("config/tasks/meals.jsonc");
      TaskSet.LoadFile("config/tasks/task_sets.jsonc");
      // Load the buildings.
      BuildingType.LoadFile("config/buildings/buildings.jsonc");
      // Load the skills, followed by the skill tree.
      Skill.LoadFile("config/skills/skills.jsonc");
      Skill.LoadParentsFile("config/skills/skilltree.jsonc");
      // Load the default price list.
      ConfigPriceList.LoadDefault("config/items/pricelist.jsonc");
      // Do the Effect Initialization after everything else is loaded.
      EffectLoader.Initialize();
      // Init the weather.
      WeatherAttributes.Init();
    }

    public static void PrintInventory(Inventory inventory)
    {
      // Print the inventory.
      Console.WriteLine("Inventory:");
      foreach(var itemtype in inventory.items)
      {
        // Sum up the quantities of the itemtype.
        int quantity = 0;
        foreach(var item in itemtype.Value)
        {
          quantity += item.Value;
        }
        Console.WriteLine(itemtype.Key.itemType + ": " + quantity);
      }
    }

    public static void PrintBuildings(Household household)
    {
      // Print and enumerate the buildings.
      Console.WriteLine("Buildings:");
      for(int i = 0; i < household.buildings.Count; i++)
      {
        Console.WriteLine(i + ": " + household.buildings[i].buildingType.name);
      }
    }

    public static ChosenEffectTarget? PickTarget(Person person, string targetName, EffectTarget target)
    {
      // switch on target.effectTargetType
      switch (target.effectTargetType)
      {
        case EffectTargetType.Building:
          // The target is a building in the household.
          // Print the buildings and let the player choose one.
          Console.WriteLine("Choose a building as a target for the task:");
          PrintBuildings(person.household);
          // Get the input.
          string? input = Console.ReadLine();
          if(input == null)
          {
            return null;
          }
          // Convert the input to an int.
          int choice = int.Parse(input);
          // Check if the choice is valid.
          if(choice < 0 || choice >= person.household.buildings.Count)
          {
            Console.WriteLine("Invalid choice.");
            return null;
          }
          // Return the chosen building in the household context.
          // Running Context is the person doing the task.
          return new ChosenEffectTarget(target.effectTargetType, person.household.buildings[choice], person.household, person);
        default:
          Console.WriteLine("Unsupported target type: " + target.effectTargetType);
          return null;
      }
    }

    public static void PerformTask(Person person, bool personal)
    {
      // Get the set of tasks.
      HashSet<WorkTask> tasks = personal ? person.AvailablePersonalTasks : person.AvailableHouseholdTasks;
      // Convert the set to a list.
      List<WorkTask> taskList = new List<WorkTask>(tasks);
      // Print the tasks.
      for(int i = 0; i < taskList.Count; i++)
      {
        Console.WriteLine(i + ". " + taskList[i].task);
      }
      // Get the input.
      string? input = Console.ReadLine();
      if(input == null)
      {
        return;
      }
      // Convert the input to an int.
      int choice = int.Parse(input);
      // Check if the choice is valid.
      if(choice < 0 || choice >= taskList.Count)
      {
        Console.WriteLine("Invalid choice.");
        return;
      }
      // Get the task.
      WorkTask task = taskList[choice];
      // Get the targets.
      Dictionary<string, ChosenEffectTarget>? targets = null;
      if (task.targets.Count > 0)
      {
        // Player has to pick a ChosenEffectTarget for each target.
        targets = new Dictionary<string, ChosenEffectTarget>();
        foreach (var target in task.targets)
        {
          // Get the target.
          ChosenEffectTarget? chosenTarget = PickTarget(person, target.Key, target.Value);
          if (chosenTarget == null)
          {
            // Player canceled.
            Console.WriteLine("Task cancelled, no valid target.");
            return;
          }
          // Add the target to the dictionary.
          targets.Add(target.Key, chosenTarget);
        }
        
      }
      // Perform the task using the TaskRunner
      bool result = TaskRunner.PerformTask(person, (personal ? person: person.household), task, targets);
      if(result)
      {
        Console.WriteLine("Task completed successfully.");
      }
      else
      {
        Console.WriteLine("Task failed.");
      }
    }

    public static void BuildBuilding(Person person)
    {
      // Print the person's available buildings.
      Console.WriteLine("Available buildings:");
      List<BuildingType> availableBuildings = new List<BuildingType>(person.AvailableBuildings);
      for(int i = 0; i < availableBuildings.Count; i++)
      {
        Console.WriteLine(i + ". " + availableBuildings[i].name);
      }
      // Get the input.
      string? input = Console.ReadLine();
      if(input == null)
      {
        return;
      }
      // Convert the input to an int.
      int choice = int.Parse(input);
      // Check if the choice is valid.
      if(choice < 0 || choice >= availableBuildings.Count)
      {
        Console.WriteLine("Invalid choice.");
        return;
      }
      // Get the building type.
      BuildingType buildingType = availableBuildings[choice];
      // Create the building.
      person.household.AddBuilding(buildingType);
      Console.WriteLine("Building built.");
    }

    public static void GameLoop()
    {
      // Create a single household.
      Household household = new Household();
      household.isPlayerHousehold = true;
      // Create a single person.
      Person person = new Person("bob", "Bob", household, Role.HeadOfHousehold);
      while(true)
      {
        // Print Options
        Console.WriteLine("1. See Personal Inventory.");
        Console.WriteLine("2. See Household Inventory.");
        Console.WriteLine("3. See Household Buildings.");
        Console.WriteLine("4. See Person's Skills.");
        Console.WriteLine("5. See Person's Attributes.");
        Console.WriteLine("6. See Person's Abilities.");
        Console.WriteLine("7. Perform Personal Task.");
        Console.WriteLine("8. Perform Household Task.");
        Console.WriteLine("9. Build Building.");
        Console.WriteLine("10. End Turn.");
        Console.WriteLine("11. Exit.");

        // Get Input
        string? input = Console.ReadLine();
        if(input == null)
        {
          // Exit the game.
          break;
        }
        // Convert input to an int.
        int choice = int.Parse(input);
        // Switch on the choice.
        switch(choice)
        {
          case 1:
            // Print the personal inventory.
            PrintInventory(person.inventory);
            break;
          case 2:
            // Print the household inventory.
            PrintInventory(household.inventory);
            break;
          case 3:
            // Print the household buildings.
            PrintBuildings(household);
            break;
          case 4:
            // Get the dictionary of the person's skills.
            Dictionary<Skill, PersonSkill> skills = person.skills.skills;
            // Print the skills.
            foreach(var skill in skills)
            {
              Console.WriteLine(skill.Key.id + ": " + skill.Value.level + " (" + skill.Value.XP + " xp)");
            }
            break;
          case 5:
            // Get the dictionary of the person's attributes.
            Dictionary<AttributeType, Attributes.Attribute> attributes = person.attributes.attributes;
            // Print the attributes.
            foreach(var attribute in attributes)
            {
              Console.WriteLine(attribute.Key.name + ": " + attribute.Value.value);
            }
            break;
          case 6:
            // Get a list of the person's abilities.
            List<AbilityType> abilities = new List<AbilityType>(person.Abilities);
            // Print the abilities.
            for(int i = 0; i < abilities.Count; i++)
            {
              Console.WriteLine(i + ". " + abilities[i].abilityType);
            }
            break;
          case 7:
            // Perform a personal task.
            PerformTask(person, true);
            break;
          case 8:
            // Perform a household task.
            PerformTask(person, false);
            break;
          case 9:
            // Build a building.
            BuildBuilding(person);
            break;
          case 10:
            // End the turn.
            break;
          case 11:
            // Exit the game.
            return;
          default:
            // Print an error message.
            Console.WriteLine("Invalid choice.");
            break;
        }

      }
    }

    static void Main(string[] args)
    {
      Console.WriteLine("Village Entry Point");
      //Load configs
      LoadConfig();
      
      // Create a new GameLoop
      GameLoop gameLoop = new GameLoop();
      // Start the GameLoop in it's own thread.
      Thread gameLoopThread = new Thread(new ThreadStart(gameLoop.Run));
      gameLoopThread.Start();
      // Start the GameServer.
      GameServer.Start();
    }
  }
}
