// An Attribute is an integer variable that will grant abilities
// depending on its current value. The ability will be granted
// to a person so long as the attribute is within a certain range.
// Effects can be set to run when the value of an attribute reaches
// certain trigger thresholds.
using Newtonsoft.Json;
using Village.Abilities;
using Village.Effects;
using Village.Items;

namespace Village.Attributes;

public class AttributeInterval
{
  // The lower limit of the interval, inclusive.
  public int lower;
  // The upper limit of the interval, exclusive.
  public int upper;
  // The abilities that will be granted while in this interval.
  public HashSet<AbilityType> abilities = new HashSet<AbilityType>();
  // The effects that will be triggered when entering this interval.
  // Effects are not triggered during initialization.
  public List<Effect> effects = new List<Effect>();
}

public class AttributeType
{
  // Dictionary of attribute types.
  public static Dictionary<string, AttributeType> types { get; private set; } = new Dictionary<string, AttributeType>();

  // Clear the types dictionary.
  public static void Clear()
  {
    types.Clear();
  }

  // Find an attribute type by name.
  public static AttributeType? Find(string name)
  {
    if (types.ContainsKey(name)) {
      return types[name];
    }
    return null;
  }

  // Loader function to load all attribute types from a JSON Dictionary.
  public static void Load(Dictionary<string, Dictionary<string, object>> data)
  {
    foreach (var attribute in data)
    {
      var min = (int)(long)attribute.Value["min"];
      var max = (int)(long)attribute.Value["max"];
      var init = (int)(long)attribute.Value["initial"];
      AttributeType a = new AttributeType(attribute.Key, init, min, max);
      // Check whether it's a calendar attribute.
      if (attribute.Value.ContainsKey("calendar"))
      {
        a.calendar = (bool)attribute.Value["calendar"];
      }
      // Cast the intervals to a newtonsoft JArray and check for null.
      var intervalArray = (Newtonsoft.Json.Linq.JArray)attribute.Value["intervals"];
      if (intervalArray == null)
      {
        throw new Exception("Failed to load intervals for attribute: " + attribute.Key);
      }
      // Convert the intervals to a list of dictionaries.
      var intervals = intervalArray.ToObject<List<Dictionary<string, object>>>();
      if (intervals == null)
      {
        throw new Exception("Failed to load interval array for attribute: " + attribute.Key);
      }
      // If the intervals are empty, create a single interval that covers the entire range.
      if (intervals.Count == 0)
      {
        AttributeInterval attributeInterval = new AttributeInterval();
        attributeInterval.lower = min;
        attributeInterval.upper = max;
        a.intervals.Add(min, attributeInterval);
      }
      // if the first interval doesn't start at min, insert a blank interval that
      // covers the range from min to the start of the first interval.
      else if ((int)(long)intervals[0]["lower"] > min)
      {
        AttributeInterval attributeInterval = new AttributeInterval();
        attributeInterval.lower = min;
        attributeInterval.upper = (int)(long)intervals[0]["lower"];
        a.intervals.Add(min, attributeInterval);
      }
      foreach (var interval in intervals)
      {
        var intervalData = interval;
        var lower = (int)(long)intervalData["lower"];
        // Peek at the next interval to get the upper bound, unless this is the last interval.
        // In that case, use the max value.
        var upper = max;
        if (intervals.IndexOf(interval) < intervals.Count - 1)
        {
          upper = (int)(long)intervals[intervals.IndexOf(interval) + 1]["lower"];
        }
        // Check that the lower bound is less than the upper bound.
        if (lower >= upper)
        {
          throw new Exception("Intervals must be sorted for attribute: " + attribute.Key);
        }

        AttributeInterval attributeInterval = new AttributeInterval();
        attributeInterval.lower = lower;
        attributeInterval.upper = upper;
        // Check whether abilities is present.
        if (intervalData.ContainsKey("abilities"))
        {
          var abilities = ((Newtonsoft.Json.Linq.JArray)intervalData["abilities"]).ToObject<List<string>>();
          
          foreach (var ability in abilities!)
          {
            AbilityType? abilityType = AbilityType.Find(ability);
            if (abilityType == null)
            {
              throw new Exception("Failed to find ability type: " + ability + " for attribute: " + attribute.Key);
            }
            attributeInterval.abilities.Add(abilityType);
          }
        }
        
        // Check whether effects is present.
        if (intervalData.ContainsKey("effects"))
        {
          var effects = ((Newtonsoft.Json.Linq.JArray)intervalData["effects"]).ToObject<List<string>>();
          foreach (var effect in effects!)
          {
            Effect? effectType = Effect.Find(effect);
            if (effectType == null)
            {
              throw new Exception("Failed to find effect type: " + effect + " for attribute: " + attribute.Key);
            }
            attributeInterval.effects.Add(effectType);
          }
        }
        
        a.intervals.Add(lower, attributeInterval);
      }
      types.Add(attribute.Key, a);
    }
  }

  // Loader function to load all attribute types from a JSON string.
  public static void LoadString(string json)
  {
    // Load the JSON into a dictionary.
    Dictionary<string, Dictionary<string, object>>? data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);
    if (data == null) {
      throw new Exception("Failed to load attribute types from string");
    }
    // Load the dictionary.
    Load(data);
  }

  // Loader from a file.
  public static void LoadFile(string filename)
  {
    // Load the JSON from the file.
    string json = File.ReadAllText(filename);
    try
    {
      LoadString(json);
    }
    catch (Exception e)
    {
      throw new Exception("Failed to load attribute types from file: " + filename + "\n" + e.Message);
    }
  }

  // The name of the attribute.
  public string name { get; private set; }
  // The initial value of the attribute.
  public int initialValue { get; private set; }
  // The minimum value of the attribute.
  public int minValue { get; private set; }
  // The maximum value of the attribute.
  public int maxValue { get; private set; }
  // Whether this is a calendar attribute.
  public bool calendar { get; private set; } = false;
  // Intervals at which abilities will be granted and effects will be triggered.
  // The key is the lower limit of the interval, inclusive. The upper limit is
  // the key of the next interval, exclusive. The last interval will include
  // the maxvalue of the attribute.
  public SortedList<int, AttributeInterval> intervals { get; private set; } = new SortedList<int, AttributeInterval>();

  public int FindIntervalIndex(int value)
  {
    int lower = 0;
    int upper = intervals.Keys.Count - 1;

    if (value < intervals.Keys[lower]) return lower;
    if (value >= intervals.Keys[upper]) return upper;

    while (lower <= upper)
    {
      int middle = lower + (upper - lower) / 2;
      if (intervals.Keys[middle] == value) return middle;
      else if (intervals.Keys[middle] > value) upper = middle - 1;
      else lower = middle + 1;
    }

    return upper;
  }

  public AttributeType(string name, int initialValue, int minValue, int maxValue)
  {
    this.name = name;
    this.initialValue = initialValue;
    this.minValue = minValue;
    this.maxValue = maxValue;
  }


}

public class Attribute
{
  // The current value of the attribute.
  public int value { get; private set; }
  // Cached min of the range the attribute is currently in.
  public int rangeMin { get; private set;}
  // Cached max of the range the attribute is currently in.
  public int rangeMax { get; private set;}
  // Cached index of the interval the attribute is currently in.
  private int intervalIndex;
  // The attribute type.
  public AttributeType attributeType;
  // target for effects
  private IInventoryContext? target;
  // context for effects
  private IAbilityContext? context;

  // List of async effects that are currently running.
  List<Task> _asyncEffects = new List<Task>();

  public Attribute(AttributeType attributeType, IInventoryContext? effectTarget, IAbilityContext? effectContext)
  {
    this.attributeType = attributeType;
    this.value = attributeType.initialValue;
    // Use a binary search to find the interval that the initial value is in.
    // We find the key of the interval that is just less than the initial value.
    // The interval that the initial value is in is the interval that starts at
    // the key we found.
    this.intervalIndex = attributeType.FindIntervalIndex(attributeType.initialValue);
    var interval = attributeType.intervals.GetValueAtIndex(this.intervalIndex);
    this.rangeMin = interval.lower;
    this.rangeMax = interval.upper;
    this.target = effectTarget;
    this.context = effectContext;
  }

  public HashSet<AbilityType> GetAbilities()
  {
    return attributeType.intervals.GetValueAtIndex(intervalIndex).abilities;
  }

  // Set the value of the attribute.
  // Returns true if the range of the attribute has changed and that
  // caused the abilities to change.
  public bool SetValue(int newValue)
  {
    if (newValue == value) return false;

    if (newValue < attributeType.minValue)
    {
      newValue = attributeType.minValue;
    }
    else if (newValue > attributeType.maxValue)
    {
      newValue = attributeType.maxValue;
    }

    value = newValue;

    // if value is still inside the current range, we don't need to update the range
    if (value >= rangeMin && value < rangeMax) return false;

    // Use a binary search to find the interval that the new value is in.
    // We find the key of the interval that is just less than the new value.
    // The interval that the new value is in is the interval that starts at
    // the key we found.
    int newIntervalIndex = attributeType.FindIntervalIndex(newValue);
    if (newIntervalIndex != intervalIndex)
    {
      // Check whether the new intervals' abilities are different from
      // the current intervals' abilities.
      var newInterval = attributeType.intervals.GetValueAtIndex(newIntervalIndex);
      var oldInterval = attributeType.intervals.GetValueAtIndex(intervalIndex);
      intervalIndex = newIntervalIndex;
      rangeMin = newInterval.lower;
      rangeMax = newInterval.upper;
      // Run effects on the new interval, if we have a target and context.
      if (target != null && context != null)
      {
        
        foreach (var effect in newInterval.effects)
        {
          // Apply the effect, the target is always the person whose attribute this is.
          effect.Apply(new ChosenEffectTarget(effect.target, null, target, context));
        }
      }
      return !newInterval.abilities.SetEquals(oldInterval.abilities);
    }

    return false;
  }

  // Add a value to the attribute.
  // Returns true if the range of the attribute has changed and that
  // caused the abilities to change.
  public bool AddValue(int addValue)
  {
    return SetValue(value + addValue);
  }
}