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

public enum AttributeUtility
{
  None,
  Linear,
  Step,
  Sigmoid
}

public class AttributeInterval : IAbilityProvider
{
  // The lower limit of the interval, inclusive.
  public double lower;
  // The upper limit of the interval, exclusive.
  public double upper;
  // The abilities that will be granted while in this interval.
  public HashSet<AbilityType> Abilities { get; } = new HashSet<AbilityType>();

  // The effects that will be triggered when entering this interval.
  // Effects are not triggered during initialization.
  public List<Effect> entryEffects = new List<Effect>();

  // The effects that will be applied for every tick that the attribute is in this interval.
  // These effects must be batchable, as we won't actually run them every tick.
  public List<Effect> ongoingEffects = new List<Effect>();

  // The utility function to use for this interval.
  public AttributeUtility utilityType = AttributeUtility.None;
  // The utility of the lower limit of this interval.
  public double utilityLower = 0;
  // The utility of the upper limit of this interval.
  public double utilityUpper = 0;
}

public class AttributeType
{
  // Dictionary of attribute types.
  public static Dictionary<string, AttributeType> types { get; private set; } = new Dictionary<string, AttributeType>();

  public static Dictionary<string, List<AttributeType>> groups { get; private set; } = new Dictionary<string, List<AttributeType>>();

  // Set of abilities that have been referenced by the attributes loaded so far.
  // This is used to check for circular dependencies.
  private static HashSet<AbilityType> _referencedAbilities = new HashSet<AbilityType>();

  // Clear the types dictionary.
  public static void Clear()
  {
    types.Clear();
    groups.Clear();
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

  private static double _Double(object? value)
  {
    if (value == null)
    {
      return 0;
    }
    if (value.GetType() == typeof(double))
    {
      return (double)value;
    }
    else if (value.GetType() == typeof(long))
    {
      return (long)value;
    }
    return 0;
  }

  // Loader function to load all attribute types from a JSON Dictionary.
  public static void Load(Dictionary<string, Dictionary<string, object>> data)
  {
    foreach (var attribute in data)
    {
      var min = _Double(attribute.Value["min"]);
      var max = _Double(attribute.Value["max"]);
      AbilityValue init = AbilityValue.FromJson(attribute.Value["initial"]);
      // Check that the initial value is NOT a named value.
      // Names generally refer to other attributes, and we don't
      // want to allow that currently.
      if (init.namedValue != null)
      {
        throw new Exception("Attribute value cannot be a named value: " + attribute.Key);
      }
      // Check that any abilities referenced by the initial value
      // are not in the referenced abilities set. This is to prevent
      // circular dependencies.
      if (_referencedAbilities.Overlaps(init.Abilities))
      {
        throw new Exception("Possible circular dependency detected in attribute: " + attribute.Key);
      }
      AttributeType a = new AttributeType(attribute.Key, init, min, max);
      // Read the attribute's group.
      if (attribute.Value.ContainsKey("group"))
      {
        a.group = (string)attribute.Value["group"];
      }
      if (attribute.Value.ContainsKey("changePerTick"))
      {
        // Check for double or long.
        if (attribute.Value["changePerTick"].GetType() == typeof(double))
        {
          a.changePerTick = (double)attribute.Value["changePerTick"];
        }
        else if (attribute.Value["changePerTick"].GetType() == typeof(long))
        {
          a.changePerTick = (long)attribute.Value["changePerTick"];
        }
      }
      AttributeUtility defaultUtility = AttributeUtility.None;
      if (attribute.Value.ContainsKey("utilityType"))
      {
        // Read the default utility function type and convert it to the enum.
        string utilityType = (string)attribute.Value["utilityType"];
        defaultUtility = (AttributeUtility)Enum.Parse(typeof(AttributeUtility), utilityType);
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
      else if (_Double(intervals[0]["lower"]) > min)
      {
        AttributeInterval attributeInterval = new AttributeInterval();
        attributeInterval.lower = min;
        attributeInterval.upper = _Double(intervals[0]["lower"]);
        a.intervals.Add(min, attributeInterval);
      }
      foreach (var interval in intervals)
      {
        var intervalData = interval;
        var lower = _Double(intervalData["lower"]);
        // Peek at the next interval to get the upper bound, unless this is the last interval.
        // In that case, use the max value.
        var upper = max;
        if (intervals.IndexOf(interval) < intervals.Count - 1)
        {
          upper = _Double(intervals[intervals.IndexOf(interval) + 1]["lower"]);
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

        if (intervalData.ContainsKey("ongoing_effects"))
        {
          var effects = ((Newtonsoft.Json.Linq.JArray)intervalData["ongoing_effects"]).ToObject<List<string>>();
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
            attributeInterval.ongoingEffects.Add(effectType);
            // TODO(chmeyers): It would be good to check for circular dependencies caused by effects, too.
          }
        }

        if (intervalData.ContainsKey("utilityType"))
        {
          string utilityType = (string)intervalData["utilityType"];
          attributeInterval.utilityType = (AttributeUtility)Enum.Parse(typeof(AttributeUtility), utilityType);
        }
        else
        {
          attributeInterval.utilityType = defaultUtility;
        }
        if (intervalData.ContainsKey("utility"))
        {
          attributeInterval.utilityLower = _Double(intervalData["utility"]);
          // If they didn't specify a utility type, throw.
          if (attributeInterval.utilityType == AttributeUtility.None)
          {
            throw new Exception("Utility type must be specified for attribute: " + attribute.Key);
          }
          if (intervalData.ContainsKey("utilityUpper"))
          {
            attributeInterval.utilityUpper = _Double(intervalData["utilityUpper"]);
          }
          else if (attributeInterval.utilityType == AttributeUtility.Step)
          {
            attributeInterval.utilityUpper = attributeInterval.utilityLower;
          }
          else if (intervals.IndexOf(interval) < intervals.Count - 1)
          {
            // If they didn't specify an upper bound, use the lower bound of the next interval.
            attributeInterval.utilityUpper = _Double(intervals[intervals.IndexOf(interval) + 1]["utility"]);
          }
          else
          {
            // Final fallback for the last interval, the upper bound is assumed to have the same
            // difference from the lower bound as the previous interval.
            attributeInterval.utilityUpper = attributeInterval.utilityLower + (attributeInterval.utilityLower - _Double(intervals[intervals.IndexOf(interval) - 1]["utility"]));
          }
        }

        a.intervals.Add(lower, attributeInterval);
      }
      types.Add(attribute.Key, a);
      if (a.group != null && a.group != "")
      {
        if (!groups.ContainsKey(a.group))
        {
          groups.Add(a.group, new List<AttributeType>());
        }
        groups[a.group].Add(a);
      }
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
  public double minValue { get; private set; }
  // The maximum value of the attribute.
  public double maxValue { get; private set; }
  // Whether this is a calendar attribute.
  public string group { get; private set; } = "";
  // How much this attribute will change each tick.
  public double changePerTick { get; private set; } = 0;
  // Intervals at which abilities will be granted and effects will be triggered.
  // The key is the lower limit of the interval, inclusive. The upper limit is
  // the key of the next interval, exclusive. The last interval will include
  // the maxvalue of the attribute.
  public SortedList<double, AttributeInterval> intervals { get; private set; } = new SortedList<double, AttributeInterval>();

  public int FindIntervalIndex(double value)
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

  public AttributeType(string name, AbilityValue initialValue, double minValue, double maxValue)
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
      if (interval.ongoingEffects.Count > 0)
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
  public double value { get; private set; }
  // The real underlying value of the attribute is stored in this ability value.
  private AbilityValue abilityValue;
  // Cached min of the range the attribute is currently in.
  public double rangeMin { get; private set; }
  // Cached max of the range the attribute is currently in.
  public double rangeMax { get; private set; }
  // Scale all the ranges in the attribute by this amount.
  public double scale { get; private set; } = 1;
  // Multiplier for effects.
  public double effectMultiplier = 1;
  private double _scaledMaxValue = 0;
  private double _scaledMinValue = 0;
  private double _scaledChangePerTick = 0;
  private AttributeUtility _currentUtilityType = AttributeUtility.None;
  private double _currentUtilityLower = 0;
  private double _currentUtilityUpper = 0;
  private double _currentUtility = 0;
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
  private long _lastAdvanceTick = 0;

  private object _lock = new object();

  public const double maxEpsilon = 0.01;
  

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
    this.intervalIndex = attributeType.FindIntervalIndex(this.value / scale);
    var interval = attributeType.intervals.GetValueAtIndex(this.intervalIndex);
    this.rangeMin = interval.lower;
    this.rangeMax = interval.upper;
    this._currentUtilityLower = interval.utilityLower;
    this._currentUtilityUpper = interval.utilityUpper;
    this._currentUtilityType = interval.utilityType;
    this.target = effectTarget;
    this.targetContext = targetContext;
    this.abilityContext = abilityContext;
    // If the context is not null and we have modifier abilities, register for updates.
    if (abilityContext != null && _modifierAbilities.Count > 0)
    {
      abilityContext.AbilitiesChanged += OnAbilitiesChanged;
    }
    this._lastAdvanceTick = Calendar.Ticks;
    _scaledMaxValue = attributeType.maxValue * scale;
    _scaledMinValue = attributeType.minValue * scale;
    _scaledChangePerTick = attributeType.changePerTick * scale;
    this._currentUtility = _Utility(this.value);
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
    lock(_lock) {
      if (Calendar.Ticks == _lastAdvanceTick) return;
      // Advance one interval at a time, to ensure that we don't miss entry effects,
      // and ongoing effects are accurately applied.
      int ticksForCurrentInterval = (int)Math.Min(TicksToNextInterval(), Calendar.Ticks - _lastAdvanceTick);
      while (ticksForCurrentInterval > 0)
      {
        _lastAdvanceTick += ticksForCurrentInterval;
        RunOngoingEffects(attributeType.intervals.GetValueAtIndex(intervalIndex), ticksForCurrentInterval);
        // Update the value by the change per tick.
        AdvanceChangePerTick(ticksForCurrentInterval);
        ticksForCurrentInterval = (int)Math.Min(TicksToNextInterval(), Calendar.Ticks - _lastAdvanceTick);
      }
    }
  }

  private void AdvanceChangePerTick(int ticks)
  {
    if (ticks == 0 || _scaledChangePerTick == 0) return;
    if (_scaledChangePerTick < 0 && IsMinned()) return;
    if (_scaledChangePerTick > 0 && IsMaxed()) return;
    _AddValueInternal(_scaledChangePerTick * ticks);
  }

  private int TicksToNextInterval()
  {
    if (_scaledChangePerTick == 0) return int.MaxValue;
    if (_scaledChangePerTick > 0)
    {
      if (IsMaxed()) return int.MaxValue;
      // Round up to the next tick.
      return (int)Math.Ceiling((rangeMax - value ) / _scaledChangePerTick);
    }
    else
    {
      if (IsMinned()) return int.MaxValue;
      return (int)Math.Floor((value - rangeMin) / -_scaledChangePerTick) + 1;
    }
  }

  private bool IsMaxed()
  {
    // True if the value is at the max value or the base value is at the max value (exclusive).
    return value >= _scaledMaxValue - maxEpsilon ||  abilityValue.baseValue * scale >= _scaledMaxValue - maxEpsilon;
  }

  private bool IsMinned()
  {
    // True if the value is at the min value or the base value is at the min value.
    return value <= _scaledMinValue || abilityValue.baseValue * scale <= _scaledMinValue;
  }

  private void RunOngoingEffects(AttributeInterval interval, int ticks)
  {
    if (interval.ongoingEffects.Count > 0)
    {
      foreach (var effect in interval.ongoingEffects)
      {
        // Apply the effect, batching up the number of ticks since the last run.
        effect.Apply(new ChosenEffectTarget(effect.target, target, targetContext, abilityContext), effectMultiplier, ticks);
      }
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
      _UpdateValue();
    }
  }

  private double _UpdateValue()
  {
    // Note that Advance() should be called before this method is called.
    // Recalculate the value.
    value = abilityValue.GetValue(abilityContext) * scale;

    // Post-modifier value is also gated by the min/max.
    if (value < _scaledMinValue)
    {
      value = _scaledMinValue;
    }
    else if (value >= _scaledMaxValue)
    {
      // The max value is exclusive, so we need to subtract epsilon.
      value = _scaledMaxValue - maxEpsilon;
    }

    // Cache the utility value.
    this._currentUtility = _Utility(value);

    // if value is still inside the current range, we don't need to update the range
    if (value >= rangeMin && value < rangeMax) return value;

    // Use a binary search to find the interval that the new value is in.
    // We find the key of the interval that is just less than the new value.
    // The interval that the new value is in is the interval that starts at
    // the key we found.
    int newIntervalIndex = attributeType.FindIntervalIndex(value / scale);
    if (newIntervalIndex != intervalIndex)
    {
      // Check whether the new intervals' abilities are different from
      // the current intervals' abilities.
      var newInterval = attributeType.intervals.GetValueAtIndex(newIntervalIndex);
      var oldInterval = attributeType.intervals.GetValueAtIndex(intervalIndex);
      intervalIndex = newIntervalIndex;
      rangeMin = newInterval.lower * scale;
      rangeMax = newInterval.upper * scale;
      this._currentUtilityLower = newInterval.utilityLower;
      this._currentUtilityUpper = newInterval.utilityUpper;
      this._currentUtilityType = newInterval.utilityType;
      // Run entry effects on the new interval, if we have a target and context.
      if (targetContext != null && abilityContext != null)
      {
        foreach (var effect in newInterval.entryEffects)
        {
          // Apply the effect, the target is always one specified when creating the attribute,
          // typically the owner of the attribute.
          effect.Apply(new ChosenEffectTarget(effect.target, target, targetContext, abilityContext), effectMultiplier);
        }
      }
      if (!newInterval.Abilities.SetEquals(oldInterval.Abilities))
      {
        AbilitiesChanged?.Invoke(this, newInterval.Abilities, this, oldInterval.Abilities);
      }
    }

    return value;
  }

  // Set the value of the attribute.
  public double SetValue(double newValue)
  {
    lock (_lock)
    {
      Advance();
      return _AddValueInternal(newValue - value);
    }
  }

  private double _AddValueInternal(double addValue)
  {
    if (addValue == 0) return value;

    abilityValue.Add(addValue / scale);

    if (abilityValue.baseValue < attributeType.minValue)
    {
      abilityValue.Set(attributeType.minValue);
    }
    else if (abilityValue.baseValue >= attributeType.maxValue - maxEpsilon)
    {
      abilityValue.Set(attributeType.maxValue - maxEpsilon);
    }

    return _UpdateValue();
  }

  // Add a value to the attribute.
  public double AddValue(double addValue)
  {
    lock (_lock)
    {
      Advance();
      return _AddValueInternal(addValue);
    }
  }

  public void Rescale(double newScale)
  {
    lock (_lock)
    {
      Advance();
      if (newScale == scale) return;
      if (scale <= 0) throw new ArgumentException("Attribute Scale must be positive.");
      scale = newScale;
      _scaledMaxValue = attributeType.maxValue * scale;
      _scaledMinValue = attributeType.minValue * scale;
      _scaledChangePerTick = attributeType.changePerTick * scale;
      var currentInterval = attributeType.intervals.GetValueAtIndex(intervalIndex);
      rangeMin = currentInterval.lower * scale;
      rangeMax = currentInterval.upper * scale;
      _UpdateValue();
    }
  }

  // In case you need the unscaled value of the attribute
  public double GetUnscaled()
  {
    return abilityValue.GetValue(abilityContext);
  }

  private double _Utility(double value)
  {
    value = Math.Clamp(value, _scaledMinValue, _scaledMaxValue - maxEpsilon);
    AttributeUtility utilityType = _currentUtilityType;
    double utilityLower = _currentUtilityLower;
    double utilityUpper = _currentUtilityUpper;
    int intervalIndex = this.intervalIndex;
    if (value < rangeMin || value >= rangeMax)
    {
      intervalIndex = attributeType.FindIntervalIndex(value / scale);
      var newInterval = attributeType.intervals.GetValueAtIndex(intervalIndex);
      utilityLower = newInterval.utilityLower;
      utilityUpper = newInterval.utilityUpper;
      utilityType = newInterval.utilityType;
    }

    if (utilityType == AttributeUtility.None) return 0;
    double fractionOfRange = (value - rangeMin) / (rangeMax - rangeMin);
    if (utilityType == AttributeUtility.Step)
    {
      return utilityLower;
    }
    if (utilityType == AttributeUtility.Linear)
    {
      return utilityLower + fractionOfRange * (utilityUpper - utilityLower);
    }
    if (utilityType == AttributeUtility.Sigmoid)
    {
      // We want a sigmoid step at the lower and upper and of the ranges,
      // with a linear transition in the middle.
      // So we add together two sigmoids.
      double rangeWidth = rangeMax - rangeMin;
      // Multiplier chosen such that the sigmoid reaches 0.45 at ~20% of the range.
      double sigmoidMult = 15 / rangeWidth;
      // Starts at 0
      // 0.5 at rangeWidth / 2
      // 1.0 at rangeWidth
      double sigmoidStart = value - rangeMin;
      // Sigmoid for the left side of the range. If we are at the leftmost interval,
      // we don't want a sigmoid, so we set it to 1.
      double leftSigmoid = intervalIndex == 0 ? 1 : (1 / (1 + Math.Exp(-sigmoidStart * sigmoidMult)));
      // Sigmoid for the right side of the range. If we are at the rightmost interval,
      // we don't want a sigmoid, so we set it to 0.
      double rightSigmoid = intervalIndex == attributeType.intervals.Count - 1 ? 0 : (1 / (1 + Math.Exp((-sigmoidStart * sigmoidMult + sigmoidMult * rangeWidth))));
      // Subtract a half so that the sigmoid starts at 0 at the lower bound of the range.
      double sigmoid = leftSigmoid + rightSigmoid - 0.5;
      return utilityLower + sigmoid * (utilityUpper - utilityLower);
    }
    return 0;
  }

  public double Utility(double delta)
  {
    if (delta == 0) return 0;
    if (_currentUtilityType == AttributeUtility.None) return 0;
    return _Utility(value + delta) - this._currentUtility;
  }
}