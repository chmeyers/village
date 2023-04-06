// Classes describing a task.
// Tasks are performed by Persons, have a cost in inputs and time,
// and produce outputs and side effects. They can only be performed
// by Persons with the correct abilities.

using Newtonsoft.Json;
using System;
using Village.Abilities;
using Village.Effects;
using Village.Items;



namespace Village.Tasks
{
  // Named WorkTask instead of Task to avoid conflict with System.Task
  public class WorkTask
  {
    // Dictionary to store the loaded Tasks
    private static Dictionary<string, WorkTask> _tasks = new Dictionary<string, WorkTask>();

    // Index of tasks based on the abilities required to perform them.
    // Tasks are listed under each ability they require, as well as under the
    // super types of those abilities.
    private static Dictionary<string, List<WorkTask>> _tasksByAbility = new Dictionary<string, List<WorkTask>>();

    // Clear the task dictionaries.
    public static void Clear()
    {
      _tasks.Clear();
      _tasksByAbility.Clear();
    }
    // Readonly Accessor for the task dictionary.
    public static IReadOnlyDictionary<string, WorkTask> tasks
    {
      get { return _tasks; }
    }

    // Readonly Accessor for the tasksByAbility dictionary.
    public static IReadOnlyDictionary<string, List<WorkTask>> tasksByAbility
    {
      get { return _tasksByAbility; }
    }

    // Find a Task by name.
    public static WorkTask? Find(string name)
    {
      if (_tasks.ContainsKey(name))
      {
        return _tasks[name];
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
      if (_tasksByAbility.ContainsKey(""))
      {
        foreach (WorkTask task in _tasksByAbility[""])
        {
          tasks.Add(task);
        }
      }
      // Iterate over the abilities.
      foreach (AbilityType ability in abilities)
      {
        // If the ability is in the tasksByAbility dictionary, add the tasks to the list.
        if (_tasksByAbility.ContainsKey(ability.abilityType))
        {
          // For each task, check that all the requirements are met.
          foreach (WorkTask task in _tasksByAbility[ability.abilityType])
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
        Dictionary<string, int>? inputs = task.Value.ContainsKey("inputs") ? ((Newtonsoft.Json.Linq.JObject)task.Value["inputs"]).ToObject<Dictionary<string, int>>() : null;

        // If present, get the outputs setting from the Value
        Dictionary<string, int>? outputs = task.Value.ContainsKey("outputs") ? ((Newtonsoft.Json.Linq.JObject)task.Value["outputs"]).ToObject<Dictionary<string, int>>() : null;
        // If present, get the effects setting from the Value
        Dictionary<string, List<string>>? effects = task.Value.ContainsKey("effects") ? ((Newtonsoft.Json.Linq.JObject)task.Value["effects"]).ToObject<Dictionary<string, List<string>>>() : null;
        // Get the time setting from the Value, this setting is required.
        int time = (int)(long)task.Value["timeCost"];
        // Get the repeatable setting, defaulting to true unless timeCost is zero.
        bool repeatable = task.Value.ContainsKey("repeatable") ? (bool)task.Value["repeatable"] : time == 0;

        // Create the task.
        WorkTask newTask = new WorkTask(name, requirements, inputs, outputs, effects, time, repeatable);
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
    public WorkTask(string task, List<string>? requirements, Dictionary<string, int>? inputs, Dictionary<string, int>? outputs, Dictionary<string, List<string>>? effects, int timeCost, bool repeatable)
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
          (KeyValuePair<string, int> input) =>
          {
            ItemType? itemType = ItemType.Find(input.Key);
            if (itemType == null)
            {
              throw new Exception("Invalid item type in task config: " + input.Key);
            }
            return itemType;
          },
          (KeyValuePair<string, int> input) => input.Value
        );
      }
      else
      {
        this.inputs = new Dictionary<ItemType, int>();
      }
      // Verify that the outputs are all valid ItemTypes,
      // and convert the strings to ItemTypes.
      if (outputs != null)
      {
        this.outputs = outputs.ToDictionary(
          (KeyValuePair<string, int> output) =>
          {
            ItemType? itemType = ItemType.Find(output.Key);
            if (itemType == null)
            {
              throw new Exception("Invalid item type in task config: " + output.Key);
            }
            return itemType;
          },
          (KeyValuePair<string, int> output) => output.Value
        );
      }
      else
      {
        this.outputs = new Dictionary<ItemType, int>();
      }

      // Verify that the effects are all valid Effects,
      // and convert the strings to Effects.
      if (effects != null)
      {
        this.effects = effects.ToDictionary(
          (KeyValuePair<string, List<string>> effect) =>
          {
            Effect? effectType = Effect.Find(effect.Key);
            if (effectType == null)
            {
              throw new Exception("Invalid effect type in task config: " + effect.Key);
            }
            return effectType;
          },
          (KeyValuePair<string, List<string>> effect) =>
          {
            return effect.Value.Select((string target) =>
            {
              return new EffectTarget(Effect.Find(effect.Key)!.target, target);
            }).ToList();
          }
        );
      }
      else
      {
        this.effects = new Dictionary<Effect, List<EffectTarget>>();
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

      // Set the time cost.
      this.timeCost = timeCost;
      // Set the repeatable flag.
      this.repeatable = false;
      
      // Add the task to the dictionary.
      _tasks.Add(task, this);

      // Add the task to the tasks by ability index.
      foreach (AbilityType ability in this.requirements)
      {
        // Add it for the ability type and all it's super types.
        foreach (AbilityType superType in ability.superTypes)
        {
          if (!_tasksByAbility.ContainsKey(superType.abilityType))
          {
            _tasksByAbility.Add(superType.abilityType, new List<WorkTask>());
          }
          _tasksByAbility[superType.abilityType].Add(this);
        }
        if (!_tasksByAbility.ContainsKey(ability.abilityType))
        {
          _tasksByAbility.Add(ability.abilityType, new List<WorkTask>());
        }
        _tasksByAbility[ability.abilityType].Add(this);
      }
      // If the task has no requirements, add it to the empty string key.
      if (this.requirements.Count == 0)
      {
        if (!_tasksByAbility.ContainsKey(""))
        {
          _tasksByAbility.Add("", new List<WorkTask>());
        }
        _tasksByAbility[""].Add(this);
      }
    }

    // The name of the task.
    public string task;
    // The abilities required to perform the task.
    public List<AbilityType> requirements;
    // The ItemType and quantities of inputs required to perform the task.
    public Dictionary<ItemType, int> inputs;
    // The outputs produced by the task.
    public Dictionary<ItemType, int> outputs;
    // The side effects of the task, along with their targets.
    // Duplicate Effects are allowed with different targets.
    public Dictionary<Effect, List<EffectTarget>> effects;
    // The time required to perform the task.
    // Measured in tenths of a day, so Persons can perform
    // 300 units worth of tasks per month/turn.
    // Zero cost tasks are free to perform.
    public int timeCost;
    // Whether a task is repeatable in a single turn.
    public bool repeatable;
    // Set of targets for this task.
    // Targets are specified by @1, @2, etc in the config.
    public Dictionary<string, EffectTarget> targets;
  }

}

