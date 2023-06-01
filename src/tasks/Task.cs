// Classes describing a task.
// Tasks are performed by Persons, have a cost in inputs and time,
// and produce outputs and side effects. They can only be performed
// by Persons with the correct abilities.

using Newtonsoft.Json;
using Village.Abilities;
using Village.Base;
using Village.Effects;
using Village.Items;



namespace Village.Tasks
{
  // Named WorkTask instead of Task to avoid conflict with System.Task
  public class WorkTask
  {
    // Dictionary to store the loaded Tasks
    public static Dictionary<string, WorkTask> tasks { get; private set; } = new Dictionary<string, WorkTask>();

    // Index of tasks based on the abilities required to perform them.
    // Tasks are listed under each ability they require, as well as under the
    // super types of those abilities.
    public static Dictionary<AbilityType, List<WorkTask>> tasksByAbility { get; private set; } = new Dictionary<AbilityType, List<WorkTask>>();

    // Clear the task dictionaries.
    public static void Clear()
    {
      tasks.Clear();
      tasksByAbility.Clear();
    }

    // Find a Task by name.
    public static WorkTask? Find(string name)
    {
      if (tasks.ContainsKey(name))
      {
        return tasks[name];
      }
      return null;
    }

    // Get a list of potential tasks for given set of abilities, the passed in abilities
    // should already be expanded to include all subtypes.
    public static HashSet<WorkTask> GetTasksForAbilities(HashSet<AbilityType> abilities)
    {
      // Create a set to store the tasks.
      HashSet<WorkTask> tasks = new HashSet<WorkTask>();
      // Add all the tasks that have no requirements.
      if (tasksByAbility.ContainsKey(AbilityType.NULL))
      {
        foreach (WorkTask task in tasksByAbility[AbilityType.NULL])
        {
          tasks.Add(task);
        }
      }
      // Iterate over the abilities.
      foreach (AbilityType ability in abilities)
      {
        // If the ability is in the tasksByAbility dictionary, add the tasks to the list.
        if (tasksByAbility.ContainsKey(ability))
        {
          // For each task, check that all the requirements are met.
          foreach (WorkTask task in tasksByAbility[ability])
          {
            // If the task has no requirements, or all the requirements are met, add it to the set.
            if (abilities.IsSupersetOf(task.requirements))
            {
              tasks.Add(task);
            }
          }
        }
      }
      // Return the list of tasks.
      return tasks;
    }

    public static HashSet<WorkTask> FilterTasksForInventory(HashSet<WorkTask> tasks, Inventory inventory, IAbilityContext context)
    {
      // Create a set to store the tasks.
      HashSet<WorkTask> filteredTasks = new HashSet<WorkTask>();
      // Keep track of which tasks are superceded so that we can remove them at the end.
      HashSet<WorkTask> supercededTasks = new HashSet<WorkTask>();
      // Iterate over the tasks.
      foreach (WorkTask task in tasks)
      {
        // Check that all the inputs required for the task are in the inventory.
        // TODO(chmeyers): Check that all the effects have a possible valid target.
        if (inventory.Contains(task.Inputs(context)))
        {
          filteredTasks.Add(task);
          // Add the superceded tasks to the supercededTasks set.
          supercededTasks.UnionWith(task.supercedes);
        }
      }
      // Remove the superceded tasks.
      filteredTasks.ExceptWith(supercededTasks);
      // Return the list of tasks.
      return filteredTasks;
        
    }

    // Loader function to load all tasks from a JSON Dictionary.
    public static void Load(Dictionary<string, Dictionary<string, object>> data)
    {
      // Iterate over the tasks.
      foreach (var task in data)
      {
        // Get the task name.
        string name = task.Key;
        // If present, get the requirements setting from the Value
        List<string>? requirements = task.Value.ContainsKey("requirements") ? ((Newtonsoft.Json.Linq.JArray)task.Value["requirements"]).ToObject<List<string>>() : null;
        // If present, get the inputs setting from the Value
        Dictionary<string, AbilityValue>? inputs = task.Value.ContainsKey("inputs") ? ((Newtonsoft.Json.Linq.JObject)task.Value["inputs"]).ToObject<Dictionary<string, AbilityValue>>() : null;

        // If present, get the outputs setting from the Value
        Dictionary<string, AbilityValue>? outputs = task.Value.ContainsKey("outputs") ? ((Newtonsoft.Json.Linq.JObject)task.Value["outputs"]).ToObject<Dictionary<string, AbilityValue>>() : null;
        // If present, get the effects setting from the Value
        Dictionary<string, List<string>>? effects = task.Value.ContainsKey("effects") ? ((Newtonsoft.Json.Linq.JObject)task.Value["effects"]).ToObject<Dictionary<string, List<string>>>() : null;
        // If present get the supercedes list from the Value
        List<string>? supercedes = task.Value.ContainsKey("supercedes") ? ((Newtonsoft.Json.Linq.JArray)task.Value["supercedes"]).ToObject<List<string>>() : null;
        // Get the time setting from the Value, this setting is required.
        AbilityValue time = AbilityValue.FromJson(task.Value["timeCost"]);
        // Time values get a default min of 0, unless it's already set to something higher.
        if (time.min < 0)
        {
          time.min = 0;
        }
        // Get the repeatable setting, defaulting to true unless timeCost is zero.
        bool repeatable = task.Value.ContainsKey("repeatable") ? (bool)task.Value["repeatable"] : time.GetBaseValue() == 0;

        // Create the task.
        WorkTask newTask = new WorkTask(name, requirements, inputs, outputs, effects, supercedes, time, repeatable);
      }
    }

    // Load a Task from a JSON string.
    public static void LoadString(string json)
    {
      // Parse the JSON string into a dictionary of item type names and data.
      Dictionary<string, Dictionary<string, object>>? data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);
      if (data == null)
      {
        throw new Exception("Failed to load work tasks from string");
      }
      Load(data);
    }

    // Load a Task from a JSON file.
    public static void LoadFile(string path)
    {
      // Read the JSON file.
      string json = File.ReadAllText(path);
      // Load the Task from the JSON string.
      LoadString(json);
    }

    // Constructor for a WorkTask.
    public WorkTask(string task, List<string>? requirements, Dictionary<string, AbilityValue>? inputs, Dictionary<string, AbilityValue>? outputs, Dictionary<string, List<string>>? effects, List<string>? supercedes, AbilityValue timeCost, bool repeatable)
    {
      // Set the task name.
      this.task = task;
      // Verify that the requirements are all valid AbilityTypes,
      // and convert the strings to AbilityTypes.
      if (requirements != null)
      {
        this.requirements = requirements.Select((string requirement) =>
        {
          AbilityType? abilityType = AbilityType.Find(requirement);
          if (abilityType == null)
          {
            throw new Exception("Invalid ability type in task config: " + requirement);
          }
          return abilityType;
        }).ToList();
      }
      else
      {
        this.requirements = new List<AbilityType>();
      }
      // Verify that the inputs are all valid ItemTypes,
      // and convert the strings to ItemTypes.
      if (inputs != null)
      {
        this.inputs = inputs.ToDictionary(
          (KeyValuePair<string, AbilityValue> input) =>
          {
            ItemType? itemType = ItemType.Find(input.Key);
            if (itemType == null)
            {
              throw new Exception("Invalid item type in task config: " + input.Key);
            }
            return itemType;
          },
          (KeyValuePair<string, AbilityValue> input) => input.Value
        ).ToList();
        // Reorder the inputs so that children appear before their ancestors.
        this.inputs.Sort((KeyValuePair<ItemType, AbilityValue> a, KeyValuePair<ItemType, AbilityValue> b) =>
        {
          return a.Key.IsDescendentOf(b.Key) ? 1 : -1;
        });
        // Verify that all the inputs have disjoint sets of descendants,
        // unless one is the direct descendent of the other.
        // This is to avoid having two inputs that are competing for the same item.
        for (int i = 0; i < this.inputs.Count; i++)
        {
          for (int j = i + 1; j < this.inputs.Count; j++)
          {
            if (!this.inputs[i].Key.IsDescendentOf(this.inputs[j].Key) && !this.inputs[j].Key.IsDescendentOf(this.inputs[i].Key))
            {
              // Check that GetAllDescendants() return disjoint sets.
              HashSet<ItemType> aDescendants = this.inputs[i].Key.GetAllDescendants();
              HashSet<ItemType> bDescendants = this.inputs[j].Key.GetAllDescendants();
              aDescendants.IntersectWith(bDescendants);
              if (aDescendants.Count > 0)
              {
                throw new Exception("Invalid inputs in task config " + task + ": " + this.inputs[i].Key + " and " + this.inputs[j].Key + " are competing for the same child item type: " + aDescendants.First());
              }
            }
          }
        }
      }
      else
      {
        this.inputs = new List<KeyValuePair<ItemType, AbilityValue>>();
      }
      // Verify that the outputs are all valid ItemTypes,
      // and convert the strings to ItemTypes.
      if (outputs != null)
      {
        this.outputs = outputs.ToDictionary(
          (KeyValuePair<string, AbilityValue> output) =>
          {
            ItemType? itemType = ItemType.Find(output.Key);
            if (itemType == null)
            {
              throw new Exception("Invalid item type in task config: " + output.Key);
            }
            return itemType;
          },
          (KeyValuePair<string, AbilityValue> output) => output.Value
        );
      }
      else
      {
        this.outputs = new Dictionary<ItemType, AbilityValue>();
      }

      // Verify that the effects are all valid Effects,
      // and convert the strings to Effects.
      if (effects != null)
      {
        this.effects = effects.Select((KeyValuePair<string, List<string>> effect) =>
        {
          Effect? effectType = Effect.Find(effect.Key);
          if (effectType == null)
          {
            throw new Exception("Invalid effect type in task config: " + effect.Key);
          }
          // Verify that the targets are all valid EffectTargets,
          // and convert the strings to EffectTargets.
          List<EffectTarget> targets = effect.Value.Select((string target) =>
          {
            EffectTarget? targetType = new EffectTarget(Effect.Find(effect.Key)!.target, target);
            if (targetType == null)
            {
              throw new Exception("Invalid effect target in task config: " + target);
            }
            return targetType;
          }).ToList();
          return new KeyValuePair<Effect, List<EffectTarget>>(effectType, targets);
        }).ToList();
      }
      else
      {
        this.effects = new List<KeyValuePair<Effect, List<EffectTarget>>>();
      }

      // Target set starts empty.
      this.targets = new Dictionary<string, EffectTarget>();
      // Add any effect targets that are TargetStrings.
      foreach (var effect in this.effects)
      {
        foreach (var target in effect.Value)
        {
          if (EffectTarget.IsTargetString(target.target))
          {
            // If the target is already in the set, verify that it's the same target.
            if (this.targets.ContainsKey(target.target))
            {
              if (this.targets[target.target] != target)
              {
                throw new Exception("Task " + task + " has duplicate effect target with different effect types: " + target.target);
              }
            }
            else
            {
              // Add the target to the set.
              this.targets.Add(target.target, target);
            }
          }
        }
      }

      // Verify that the supercedes are all valid TaskTypes,
      // and convert the strings to TaskTypes.
      if (supercedes != null)
      {
        this.supercedes = supercedes.Select((string supercede) =>
        {
          WorkTask? taskType = WorkTask.Find(supercede);
          if (taskType == null)
          {
            throw new Exception("Invalid superceded task type in task config: " + supercede + " in task " + task);
          }
          return taskType;
        }).ToHashSet();
      }
      else
      {
        this.supercedes = new HashSet<WorkTask>();
      }
      // Add the superceded task's superceded list to this task's superceded list.
      HashSet<WorkTask> supercededTasks = new HashSet<WorkTask>();
      foreach (WorkTask superceded in this.supercedes)
      {
        foreach (WorkTask supercededSuperceded in superceded.supercedes)
        {
          supercededTasks.Add(supercededSuperceded);
        }
      }
      this.supercedes.UnionWith(supercededTasks);

      // Set the time cost.
      this.timeCost = timeCost;
      // Set the repeatable flag.
      this.repeatable = false;
      
      // Add the task to the dictionary.
      tasks.Add(task, this);

      // Add the task to the tasks by ability index.
      foreach (AbilityType ability in this.requirements)
      {
        // Add it for the ability type and all it's super types.
        foreach (AbilityType superType in ability.superTypes)
        {
          if (!tasksByAbility.ContainsKey(superType))
          {
            tasksByAbility.Add(superType, new List<WorkTask>());
          }
          tasksByAbility[superType].Add(this);
        }
        if (!tasksByAbility.ContainsKey(ability))
        {
          tasksByAbility.Add(ability, new List<WorkTask>());
        }
        tasksByAbility[ability].Add(this);
      }
      // If the task has no requirements, add it to the empty string key.
      if (this.requirements.Count == 0)
      {
        if (!tasksByAbility.ContainsKey(AbilityType.NULL))
        {
          tasksByAbility.Add(AbilityType.NULL, new List<WorkTask>());
        }
        tasksByAbility[AbilityType.NULL].Add(this);
      }
    }

    public List<KeyValuePair<ItemType, int>> Inputs(IAbilityContext context, double scale = 1.0)
    {
      List<KeyValuePair<ItemType, int>> inputs = new List<KeyValuePair<ItemType, int>>();
      foreach (var input in this.inputs)
      {
        inputs.Add(new KeyValuePair<ItemType, int>(input.Key, (int)Math.Ceiling(scale * input.Value.GetValue(context))));
      }
      return inputs;
    }

    public Dictionary<ItemType, int> OutputTypes(IAbilityContext context)
    {
      Dictionary<ItemType, int> outputs = new Dictionary<ItemType, int>();
      foreach (var output in this.outputs)
      {
        outputs.Add(output.Key, (int)output.Value.GetValue(context));
      }
      return outputs;
    }
    public Dictionary<Item, int> Outputs(IAbilityContext context)
    {
      Dictionary<Item, int> outputs = new Dictionary<Item, int>();
      foreach (var output in this.outputs)
      {
        outputs.Add(new Item(output.Key, context), (int)output.Value.GetValue(context));
      }
      return outputs;
    }

    // Building Components that the task provides.
    public HashSet<BuildingComponent> BuildingComponents()
    {
      // Return the union of all BuildingComponents provided by the effects.
      HashSet<BuildingComponent> components = new HashSet<BuildingComponent>();
      foreach (var effect in this.effects)
      {
        components.UnionWith(effect.Key.BuildingComponents());
      }
      return components;
    }

    // Whether the task is a tool crafting task.
    public bool IsToolCraftingTask()
    {
      // Check that there is one output and it is a tool.
      return this.outputs.Count == 1 && this.outputs.Keys.First().itemGroup == ItemGroup.TOOL;
    }

    // Whether the task is a gathering task.
    public bool IsGatheringTask()
    {
      // Check that there are not inputs and one output and it is a resource.
      return this.inputs.Count == 0 && this.outputs.Count == 1 && this.outputs.Keys.First().itemGroup == ItemGroup.RESOURCE;
    }

    // Whether the task is a resource processing task.
    public bool IsResourceProcessingTask()
    {
      // Check that all the inputs are resources and there is one resource output.
      return this.inputs.Count > 0 && this.inputs.All<KeyValuePair<ItemType,AbilityValue>>(pair => pair.Key.itemGroup == ItemGroup.RESOURCE) && this.outputs.Count == 1 && this.outputs.Keys.First().itemGroup == ItemGroup.RESOURCE;
    }

    // The name of the task.
    public string task;
    // The abilities required to perform the task.
    public List<AbilityType> requirements;
    // The ItemType and quantities of inputs required to perform the task.
    // Ordered such that children types come before their ancestors.
    public List<KeyValuePair<ItemType, AbilityValue>> inputs;
    // The outputs produced by the task.
    public Dictionary<ItemType, AbilityValue> outputs;
    // The side effects of the task, along with their targets.
    // Duplicate Effects are allowed with different targets.
    public List<KeyValuePair<Effect, List<EffectTarget>>> effects;
    // The time required to perform the task.
    // Measured in tenths of a day, so Persons can perform
    // 300 units worth of tasks per month/turn.
    // Zero cost tasks are free to perform.
    public AbilityValue timeCost;
    // Whether a task is repeatable in a single turn.
    public bool repeatable;
    // Set of targets for this task.
    // Targets are specified by @1, @2, etc in the config.
    public Dictionary<string, EffectTarget> targets;
    // Tasks that this task supercedes.
    // People can still technically perform superceded tasks,
    // but they are expected to be strictly worse than the superceding task.
    private HashSet<WorkTask> supercedes;
  }

}

