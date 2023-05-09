// An Attribute is an integer variable that will grant abilities
// depending on its current value. The ability will be granted
// to a person so long as the attribute is within a certain range.
// Effects can be set to run when the value of an attribute reaches
// certain trigger thresholds.
using Newtonsoft.Json;
using Village.Abilities;
using Village.Base;
using Village.Effects;
using Village.Items;

namespace Village.Attributes;

public class AttributeInterval : IAbilityProvider
{
  // The lower limit of the interval, inclusive.
  public int lower;
  // The upper limit of the interval, exclusive.
  public int upper;
  // The abilities that will be granted while in this interval.
  public HashSet<AbilityType> Abilities { get; } = new HashSet<AbilityType>();

  // The effects that will be triggered when entering this interval.
  // Effects are not triggered during initialization.
  public List<Effect> entryEffects = new List<Effect>();

  // The effects that will be applied for every day that the attribute is in this interval.
  // These effects must be batchable, as we won't necessarily run them every day.
  public List<Effect> dailyEffects = new List<Effect>();
}

public class AttributeType
{
  // Dictionary of attribute types.
  public static Dictionary<string, AttributeType> types { get; private set; } = new Dictionary<string, AttributeType>();

  // Set of abilities that have been referenced by the attributes loaded so far.
  // This is used to check for circular dependencies.
  private static HashSet<AbilityType> _referencedAbilities = new HashSet<AbilityType>();

  // Clear the types dictionary.
  public static void Clear()
  {
    types.Clear();
  }

  // Find an attribute type by name.
  public static AttributeType? Find(string name)
  {
    if (types.ContainsKey(name))
    {
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
      AbilityValue init = AbilityValue.FromJson(attribute.Value["initial"]);
      // Check that any abilities referenced by the initial value
      // are not in the referenced abilities set. This is to prevent
      // circular dependencies.
      if (_referencedAbilities.Overlaps(init.Abilities))
      {
        throw new Exception("Possible circular dependency detected in attribute: " + attribute.Key);
      }
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
        if (lower == upper)
        {
          throw new Exception("Intervals must not be empty for attribute: " + attribute.Key + ". Lower bound: " + lower + " is equal to upper bound: " + upper);
        }
        else if (lower > upper)
        {
          throw new Exception("Intervals must be sorted for attribute: " + attribute.Key + ". Lower bound: " + lower + " is greater than upper bound: " + upper);
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
            attributeInterval.Abilities.Add(abilityType);
            attributeInterval.Abilities.UnionWith(abilityType.subTypes);
          }
          // Don't allow attributes to create abilities that they reference.
          if (attributeInterval.Abilities.Overlaps(init.Abilities))
          {
            throw new Exception("Circular dependency detected in attribute: " + attribute.Key + ". It depends on an ability that it creates.");
          }
          // Save the abilities that were referenced.
          _referencedAbilities.UnionWith(attributeInterval.Abilities);
        }

        // Check whether effects is present.
        if (intervalData.ContainsKey("entry_effects"))
        {
          var effects = ((Newtonsoft.Json.Linq.JArray)intervalData["entry_effects"]).ToObject<List<string>>();
          foreach (var effect in effects!)
          {
            Effect? effectType = Effect.Find(effect);
            if (effectType == null)
            {
              throw new Exception("Failed to find effect type: " + effect + " for attribute: " + attribute.Key);
            }
            attributeInterval.entryEffects.Add(effectType);
            // TODO(chmeyers): It would be good to check for circular dependencies caused by effects, too.
          }
        }

        if (intervalData.ContainsKey("daily_effects"))
        {
          var effects = ((Newtonsoft.Json.Linq.JArray)intervalData["daily_effects"]).ToObject<List<string>>();
          foreach (var effect in effects!)
          {
            Effect? effectType = Effect.Find(effect);
            if (effectType == null)
            {
              throw new Exception("Failed to find effect type: " + effect + " for attribute: " + attribute.Key);
            }
            // Daily effects must be batchable.
            if (!effectType.SupportsBatching())
            {
              throw new Exception("Effect type: " + effect + " for attribute: " + attribute.Key + " is not batchable.");
            }
            attributeInterval.dailyEffects.Add(effectType);
            // TODO(chmeyers): It would be good to check for circular dependencies caused by effects, too.
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
    if (data == null)
    {
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
    LoadString(json);
  }

  // The name of the attribute.
  public string name { get; private set; }
  // The initial value of the attribute.
  public AbilityValue initialValue { get; private set; }
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

  public AttributeType(string name, AbilityValue initialValue, int minValue, int maxValue)
  {
    this.name = name;
    this.initialValue = initialValue;
    this.minValue = minValue;
    this.maxValue = maxValue;
  }

  public bool HasOngoingEffects()
  {
    foreach (var interval in intervals.Values)
    {
      if (interval.dailyEffects.Count > 0)
      {
        return true;
      }
    }
    return false;
  }

}

public class Attribute : IAbilityCollection
{
  // The current value of the attribute, including modifiers.
  public int value { get; private set; }
  // The real underlying value of the attribute is stored in this ability value.
  private AbilityValue abilityValue;
  // Cached min of the range the attribute is currently in.
  public int rangeMin { get; private set; }
  // Cached max of the range the attribute is currently in.
  public int rangeMax { get; private set; }
  // Cached index of the interval the attribute is currently in.
  private int intervalIndex;
  // The attribute type.
  public AttributeType attributeType;
  // target for effects
  private object? target;
  // target Context for effects
  private IInventoryContext? targetContext;
  // context for effects and abilityValues.
  private IAbilityContext? abilityContext;
  // Set of Abilities that can trigger value updates.
  private HashSet<AbilityType> _modifierAbilities = new HashSet<AbilityType>();
  // When were the daily effects last triggered?
  private long _lastEffectTick = 0;
  

  public event AbilitiesChanged? AbilitiesChanged;

  public Attribute(AttributeType attributeType, object? effectTarget, IInventoryContext? targetContext, IAbilityContext? abilityContext)
  {
    this.attributeType = attributeType;
    this.value = attributeType.initialValue.GetValue(abilityContext);
    this.abilityValue = new AbilityValue(attributeType.initialValue);
    this._modifierAbilities = attributeType.initialValue.Abilities;
    
    // Use a binary search to find the interval that the initial value is in.
    // We find the key of the interval that is just less than the initial value.
    // The interval that the initial value is in is the interval that starts at
    // the key we found.
    this.intervalIndex = attributeType.FindIntervalIndex(this.value);
    var interval = attributeType.intervals.GetValueAtIndex(this.intervalIndex);
    this.rangeMin = interval.lower;
    this.rangeMax = interval.upper;
    this.target = effectTarget;
    this.targetContext = targetContext;
    this.abilityContext = abilityContext;
    // If the context is not null and we have modifier abilities, register for updates.
    if (abilityContext != null && _modifierAbilities.Count > 0)
    {
      abilityContext.AbilitiesChanged += OnAbilitiesChanged;
    }
    this._lastEffectTick = Calendar.Ticks;
  }

  public HashSet<AbilityType> Abilities
  {
    get => attributeType.intervals.GetValueAtIndex(intervalIndex).Abilities;
    set => throw new NotImplementedException();
  }

  public Dictionary<AbilityType, HashSet<IAbilityProvider>> AbilityProviders
  {
    get => throw new NotImplementedException();
    set => throw new NotImplementedException();
  }

  public void Advance()
  {
    // Get ongoing effects up to the current tick.
    RunOngoingEffects(attributeType.intervals.GetValueAtIndex(intervalIndex));
  }

  private void RunOngoingEffects(AttributeInterval interval)
  {
    // If the last time we ran the daily effects was not today, run them again.
    long ticksSinceLastRun = _lastEffectTick - Calendar.Ticks;
    // Apply daily effects up to the current tick.
    if (ticksSinceLastRun > Calendar.ticksPerDay / 2)
    {
      if (interval.dailyEffects.Count > 0)
      {
        foreach (var effect in interval.dailyEffects)
        {
          // Apply the effect, batching up the number of days since the last run.
          // We count a half day as a full day here.
          effect.Apply(new ChosenEffectTarget(effect.target, target, targetContext, abilityContext), 1, (int)((ticksSinceLastRun + (Calendar.ticksPerDay / 2)) / Calendar.ticksPerDay));
        }
      }
      _lastEffectTick = Calendar.Ticks;
    }
  }

  private void OnAbilitiesChanged(IAbilityProvider? addedProvider, IEnumerable<AbilityType>? added, IAbilityProvider? removedProvider, IEnumerable<AbilityType>? removed)
  {
    // Check whether the added or removed abilities are modifier abilities.
    // If so, we need to update the value.
    // TODO(chmeyers): Theoretically we could index the event by ability type and only
    // register for updates we care about, but that might be overcomplicating things.
    // We can revisit this if we find that this is a performance bottleneck.
    if ((added != null && _modifierAbilities.Overlaps(added)) ||
        (removed != null && _modifierAbilities.Overlaps(removed)))
    {
      UpdateValue();
    }
  }

  private int UpdateValue()
  {
    // Recalculate the value.
    value = abilityValue.GetValue(abilityContext);

    // Post-modifier value is also gated by the min/max.
    if (value < attributeType.minValue)
    {
      value = attributeType.minValue;
    }
    else if (value > attributeType.maxValue)
    {
      value = attributeType.maxValue;
    }

    // if value is still inside the current range, we don't need to update the range
    if (value >= rangeMin && value < rangeMax) return value;

    // Use a binary search to find the interval that the new value is in.
    // We find the key of the interval that is just less than the new value.
    // The interval that the new value is in is the interval that starts at
    // the key we found.
    int newIntervalIndex = attributeType.FindIntervalIndex(value);
    if (newIntervalIndex != intervalIndex)
    {
      // Check whether the new intervals' abilities are different from
      // the current intervals' abilities.
      var newInterval = attributeType.intervals.GetValueAtIndex(newIntervalIndex);
      var oldInterval = attributeType.intervals.GetValueAtIndex(intervalIndex);
      intervalIndex = newIntervalIndex;
      rangeMin = newInterval.lower;
      rangeMax = newInterval.upper;
      // If the old interval had daily effects, get them up to date.
      RunOngoingEffects(oldInterval);
      // Run effects on the new interval, if we have a target and context.
      if (targetContext != null && abilityContext != null)
      {
        foreach (var effect in newInterval.entryEffects)
        {
          // Apply the effect, the target is always one specified when creating the attribute,
          // typically the owner of the attribute.
          effect.Apply(new ChosenEffectTarget(effect.target, target, targetContext, abilityContext));
        }
      }
      if (!newInterval.Abilities.SetEquals(oldInterval.Abilities))
      {
        AbilitiesChanged?.Invoke(this, newInterval.Abilities, this, oldInterval.Abilities);
      }
    }

    return value;
  }
  // Set the base value of the attribute.
  public int SetValue(int newBaseValue)
  {
    if (newBaseValue == abilityValue.baseValue) return value;

    if (newBaseValue < attributeType.minValue)
    {
      newBaseValue = attributeType.minValue;
    }
    else if (newBaseValue > attributeType.maxValue)
    {
      newBaseValue = attributeType.maxValue;
    }

    abilityValue.baseValue = newBaseValue;

    return UpdateValue();
  }

  // Add a value to the attribute.
  public int AddValue(int addValue)
  {
    return SetValue(abilityValue.baseValue + addValue);
  }
}