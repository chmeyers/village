

using Newtonsoft.Json;

namespace Village.Tasks;

public class TaskSet
{
  // Dictionary of all task sets.
  public static Dictionary<string, HashSet<WorkTask>> tasksets { get; private set; } = new Dictionary<string, HashSet<WorkTask>>();

  // Clear the task sets.
  public static void Clear()
  {
    tasksets.Clear();
  }

  // Find a task set by name.
  public static HashSet<WorkTask>? Find(string name)
  {
    if (tasksets.ContainsKey(name))
    {
      return tasksets[name];
    }
    return null;
  }

  // Loader for task sets.
  public static void Load(Dictionary<string, List<string>> data)
  {
    // Iterate over the task sets.
    foreach (var taskset in data)
    {
      // Get the task set name.
      string name = taskset.Key;
      // Create a new task set.
      HashSet<WorkTask> newTaskSet = new HashSet<WorkTask>();
      // Iterate over the task set items.
      foreach (var task in taskset.Value)
      {
        // Find the task by name.
        WorkTask? newTask = WorkTask.Find(task);
        if (newTask == null)
        {
          throw new Exception("Failed to find task: " + task + " in taskset: " + name);
        }
        // Add the task to the task set.
        newTaskSet.Add(newTask);
      }
      // Add the task set to the dictionary.
      tasksets.Add(name, newTaskSet);
    }
  }
  public static void LoadString(string json)
  {
    // Parse the JSON string into a dictionary of item type names and data.
    Dictionary<string, List<string>>? data = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
    if (data == null)
    {
      throw new Exception("Failed to load tasksets from string");
    }
    Load(data);
  }

  // Load from a File.
  public static void LoadFile(string path)
  {
    // Read the file.
    string json = File.ReadAllText(path);
    // Load the JSON string.
    LoadString(json);
  }
}