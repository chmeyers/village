// Effect aimed at Attributes.
using Village.Abilities;
using Village.Attributes;
using Village.Base;
using Village.Buildings;
using Village.Households;
using Village.Items;
using Village.Persons;
using Village.Skills;

namespace Village.Effects;

public class AttributePullerEffect : Effect
{
  public AttributePullerEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a person, Crop, or Field.
    if (target != EffectTargetType.Person && target != EffectTargetType.Crop && target != EffectTargetType.Field)
    {
      throw new Exception("AttributePuller effect must target a person, crop, or field: " + effect);
    }
    if (data == null)
    {
      throw new Exception("AttributePuller effect must have a config dictionary: " + effect);
    }
    // All the keys in the data dictionary are the attributes to pull.
    // The value is the the AttributePuller info.
    foreach (var key in data.Keys)
    {
      var attributePullerData = ((Newtonsoft.Json.Linq.JToken)data[key]).ToObject<Dictionary<string, object>>();
      if (attributePullerData == null)
      {
        throw new Exception("AttributePuller effect " + effect + " has an invalid config entry: " + key);
      }

      // The target value of the attribute.
      var targetVal = AbilityValue.FromJson(attributePullerData["target"]);
      // The amount to pull the attribute by.
      var amount = AbilityValue.FromJson(attributePullerData["amount"]);
      _pullers.Add(new AttributePuller(key, targetVal, amount));
    }
  }

  // Apply the effect to the target.
  public override void StartSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Get the person from the chosen target.
    IAttributeContext attributes = (IAttributeContext)chosenEffectTarget.target!;
    // Make sure attribute context is not null.
    if (attributes == null)
    {
      // We ignore this effect if the person is null.
      return;
    }

    foreach (var puller in _pullers)
    {
      // TODO(chmeyers): Advance the attribute first?
      double deltaValue = puller.GetDelta(attributes, chosenEffectTarget.runningContext, scaler, batchSize);
      attributes.AddAttribute(puller.type!, deltaValue);

    }
  }

  public override void FinishSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Nothing to do here.
  }

  // Initialize should resolve the attribute names to the actual attribute type.
  public override void Initialize()
  {
    foreach (var puller in _pullers)
    {
      puller.type = AttributeType.Find(puller.attribute);
      // Make sure the attribute exists.
      if (puller.type == null)
      {
        throw new Exception("Attribute does not exist: " + puller.attribute + " in attribute puller effect " + effect);
      }
    }
  }

  public override bool SupportsBatching()
  {
    return true;
  }

  // Attribute Effects can scale by any amount.
  public override double MinScale(ChosenEffectTarget target)
  {
    return Double.MinValue;
  }

  public override double MaxScale(ChosenEffectTarget target)
  {
    return Double.MaxValue;
  }

  public override double Utility(IHouseholdContext household, IAbilityContext runner, ChosenEffectTarget chosenEffectTarget, double scaler = 1)
  {
    IAttributeContext attributes = (IAttributeContext)chosenEffectTarget.target!;
    if (attributes == null)
    {
      return 0.0;
    }
    double utility = 0.0;
    foreach (var puller in _pullers)
    {
      double deltaValue = puller.GetDelta(attributes, chosenEffectTarget.runningContext, scaler);
      utility += attributes.Utility(puller.type!, deltaValue);
    }
    return utility;
  }

  class AttributePuller
  {
    // Constructor
    public AttributePuller(string attribute, AbilityValue target, AbilityValue amount)
    {
      this.attribute = attribute;
      this.target = target;
      this.amount = amount;
    }

    public double GetDelta(IAttributeContext attributes, IAbilityContext? context, double scaler = 1, int batchSize = 1)
    {
      double amount = this.amount.GetScaledValue(context, scaler) * batchSize;
      if (amount == 0) return 0;
      double target = this.target.GetScaledValue(context, scaler);
      // Get the attribute from the person.
      double currentValue = attributes.GetAttributeValue(type!);
      // If the current value is within amount of the target, then set it to the target.
      if (currentValue >= target - amount && currentValue <= target + amount)
      {
        return target - currentValue;
      }
      // Otherwise, pull the attribute towards the target by amount.
      else if (currentValue < target)
      {
        return amount;
      }
      else
      {
        return -amount;
      }
    }

    public string attribute = "";
    public AttributeType? type;
    public AbilityValue target;
    public AbilityValue amount;
  }
  // List of attributes to pull.
  private List<AttributePuller> _pullers = new List<AttributePuller>();
}

public class AttributeTransferEffect : Effect
{
  public AttributeTransferEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a person, Crop, or Field.
    if (target != EffectTargetType.Person && target != EffectTargetType.Crop && target != EffectTargetType.Field)
    {
      throw new Exception("AttributeTransfer effect must target a person, crop, or field: " + effect);
    }
    if (data == null)
    {
      throw new Exception("AttributeTransfer effect must have a config dictionary: " + effect);
    }
    // All the keys in the data dictionary are the attributes to pull.
    // The value is the the AttributeTransfer info.
    foreach (var key in data.Keys)
    {
      var attributeTransferData = ((Newtonsoft.Json.Linq.JToken)data[key]).ToObject<Dictionary<string, object>>();
      if (attributeTransferData == null)
      {
        throw new Exception("AttributeTransfer effect " + effect + " has an invalid config entry: " + key);
      }

      // The target value of the attribute.
      AbilityValue? sourceMin = null;
      if (attributeTransferData.ContainsKey("sourceMin"))
      {
        sourceMin = AbilityValue.FromJson(attributeTransferData["sourceMin"]);
      }
      // The amount to pull the attribute by.
      var amount = AbilityValue.FromJson(attributeTransferData["amount"]);
      string destinationAttribute = (string)attributeTransferData["dest"];
      AbilityValue? destMax = null;
      if (attributeTransferData.ContainsKey("destMax"))
      {
        destMax = AbilityValue.FromJson(attributeTransferData["destMax"]);
      }
      AbilityValue multiplier = 1;
      if (attributeTransferData.ContainsKey("multiplier"))
      {
        multiplier = AbilityValue.FromJson(attributeTransferData["multiplier"]);
      }
      bool retainOverflow = false;
      if (attributeTransferData.ContainsKey("retainOverflow"))
      {
        retainOverflow = (bool)attributeTransferData["retainOverflow"];
      }

      _transferers.Add(new AttributeTransfer(key, sourceMin, amount, destinationAttribute, destMax, multiplier, retainOverflow));
    }
  }

  // Apply the effect to the target.
  public override void StartSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Get the person from the chosen target.
    IAttributeContext attributes = (IAttributeContext)chosenEffectTarget.target!;
    // Make sure attribute context is not null.
    if (attributes == null)
    {
      // We ignore this effect if the person is null.
      return;
    }

    foreach (var transferer in _transferers)
    {
      // TODO(chmeyers): Advance the attributes first?
      double transferAmount = 0;
      double destDeltaAmount = 0;
      transferer.GetTransferDeltas(ref transferAmount, ref destDeltaAmount, attributes, chosenEffectTarget.runningContext, scaler, batchSize);
      // Transfer the amount from the source to the destination.
      attributes.AddAttribute(transferer.type!, -transferAmount);
      attributes.AddAttribute(transferer.destType!, destDeltaAmount);
    }
  }

  // Initialize should resolve the attribute names to the actual attribute type.
  public override void Initialize()
  {
    foreach (var puller in _transferers)
    {
      puller.type = AttributeType.Find(puller.attribute);
      // Make sure the attribute exists.
      if (puller.type == null)
      {
        throw new Exception("Attribute does not exist: " + puller.attribute + " in attribute puller effect " + effect);
      }
      if (puller.sourceMin == null)
      {
        // Pull the default value from the attribute.
        // This doesn't take attribute scaling into account. I think that is the correct behavior.
        puller.sourceMin = puller.type.minValue;
      }
      puller.destType = AttributeType.Find(puller.dest);
      // Make sure the attribute exists.
      if (puller.destType == null)
      {
        throw new Exception("Attribute does not exist: " + puller.dest + " in attribute puller effect " + effect);
      }
      if (puller.destMax == null)
      {
        // Pull the default value from the attribute.
        // This doesn't take attribute scaling into account. I think that is the correct behavior.
        puller.destMax = puller.destType.maxValue;
      }
    }
  }

  public override bool SupportsBatching()
  {
    return true;
  }

  // Attribute Effects can scale by any amount.
  public override double MinScale(ChosenEffectTarget target)
  {
    return Double.MinValue;
  }

  public override double MaxScale(ChosenEffectTarget target)
  {
    return Double.MaxValue;
  }

  public override double Utility(IHouseholdContext household, IAbilityContext runner, ChosenEffectTarget chosenEffectTarget, double scaler = 1)
  {
    // TODO(chmeyers): Implement.
    IAttributeContext attributes = (IAttributeContext)chosenEffectTarget.target!;
    if (attributes == null)
    {
      return 0.0;
    }
    double utility = 0.0;
    foreach (var transferer in _transferers)
    {
      double transferAmount = 0;
      double destDeltaAmount = 0;
      transferer.GetTransferDeltas(ref transferAmount, ref destDeltaAmount, attributes, chosenEffectTarget.runningContext, scaler);
      // Transfer the amount from the source to the destination.
      utility += attributes.Utility(transferer.type!, -transferAmount);
      utility += attributes.Utility(transferer.destType!, destDeltaAmount);
    }
    return utility;
  }

  class AttributeTransfer
  {
    // Constructor
    public AttributeTransfer(string attribute, AbilityValue? sourceMin, AbilityValue amount, string dest, AbilityValue? destMax, AbilityValue multiplier, bool retainOverflow)
    {
      this.attribute = attribute;
      this.sourceMin = sourceMin;
      this.amount = amount;
      this.dest = dest;
      this.destMax = destMax;
      this.multiplier = multiplier;
      this.retainOverflow = retainOverflow;
    }

    public void GetTransferDeltas(ref double transferAmount, ref double destDeltaAmount, IAttributeContext attributes, IAbilityContext? context, double scaler = 1, int batchSize = 1)
    {
      destDeltaAmount = 0.0;
      transferAmount = 0.0;
      double amount = this.amount.GetScaledValue(context, scaler) * batchSize;
      if (amount == 0) return;
      double min = this.sourceMin!.GetScaledValue(context, scaler);
      double max = this.destMax!.GetScaledValue(context, scaler);
      // Get the attribute from the person.
      double currentValue = attributes.GetAttributeValue(this.type!);
      double currentDestValue = attributes.GetAttributeValue(this.destType!);
      transferAmount = amount;
      double destMultiplier = this.multiplier.GetValue(context);
      // Positive amounts always remove from the source and add to the destination,
      // Never taking the source below the min or the destination above the max.
      if (amount > 0)
      {
        // If the current value is less than the min, then we can't transfer anything.
        if (currentValue <= min)
        {
          transferAmount = 0;
        }
        else if (currentValue - amount < min)
        {
          transferAmount = currentValue - min;
        }
        else
        {
          transferAmount = amount;
        }
        destDeltaAmount = transferAmount * destMultiplier;
        // If we are retaining overflow, then we can't transfer more than the max.
        if (currentDestValue + destDeltaAmount > max)
        {
          double allowedRecieveAmount = Math.Max(max - currentDestValue, 0);
          if (this.retainOverflow)
          {
            // Reduce the amount taken from the source by the ratio of the
            // allowed amount to the desired amount.
            transferAmount *= allowedRecieveAmount / destDeltaAmount;
          }
          destDeltaAmount = allowedRecieveAmount;
        }
      }
      // Negative amounts always remove from the destination and add to the source,
      // Never taking the destination below the max or the source above the min.
      else
      {
        // If the current value is greater than the min, then we can't transfer anything.
        if (currentValue >= min)
        {
          transferAmount = 0;
        }
        else if (currentValue - amount > min)
        {
          transferAmount = currentValue - min;
        }
        else
        {
          transferAmount = amount;
        }
        destDeltaAmount = transferAmount * destMultiplier;
        // If we are retaining overflow, then we can't transfer more than the max.
        if (currentDestValue + destDeltaAmount < max)
        {
          double allowedRemoveAmount = Math.Min(max - currentDestValue, 0);
          if (this.retainOverflow)
          {
            // Reduce the amount taken from the source by the ratio of the
            // allowed amount to the desired amount.
            transferAmount *= allowedRemoveAmount / destDeltaAmount;
          }
          destDeltaAmount = allowedRemoveAmount;
        }
      }
    }

    public string attribute = "";
    public AttributeType? type;
    public AbilityValue? sourceMin;
    public AbilityValue amount;
    public string dest = "";
    public AttributeType? destType;
    public AbilityValue? destMax;
    public AbilityValue multiplier = 1;
    public bool retainOverflow = false;

  }
  // List of attributes to pull.
  private List<AttributeTransfer> _transferers = new List<AttributeTransfer>();
}

public class AttributeAdderEffect : Effect
{
  public AttributeAdderEffect(string effect, EffectTargetType target, EffectType effectType, Dictionary<string, object>? data) : base(effect, target, effectType)
  {
    // Target must be a person, Crop, or Field.
    if (target != EffectTargetType.Person && target != EffectTargetType.Crop && target != EffectTargetType.Field)
    {
      throw new Exception("AttributeAdder effect must target a person, crop, or field: " + effect);
    }
    if (data == null)
    {
      throw new Exception("AttributeAdder effect must have a config dictionary: " + effect);
    }
    // All the keys in the data dictionary are the attributes to pull.
    // The value is the the AttributeAdder info.
    foreach (var key in data.Keys)
    {
      var attributePullerData = ((Newtonsoft.Json.Linq.JToken)data[key]).ToObject<Dictionary<string, object>>();
      if (attributePullerData == null)
      {
        throw new Exception("AttributeAdder effect " + effect + " has an invalid config entry: " + key);
      }

      // The target value of the attribute.
      AbilityValue targetVal = Double.MaxValue;
      if (attributePullerData.ContainsKey("target"))
      {
        targetVal = AbilityValue.FromJson(attributePullerData["target"]);
      }
      // The amount to increase the attribute by.
      var amount = AbilityValue.FromJson(attributePullerData["amount"]);
      _adders.Add(new AttributeAdder(key, targetVal, amount));
    }
  }

  // Apply the effect to the target.
  public override void StartSync(ChosenEffectTarget chosenEffectTarget, double scaler = 1, int batchSize = 1)
  {
    // Get the person from the chosen target.
    IAttributeContext attributes = (IAttributeContext)chosenEffectTarget.target!;
    // Make sure attribute context is not null.
    if (attributes == null)
    {
      // We ignore this effect if the person is null.
      return;
    }

    foreach (var adder in _adders)
    {
      // TODO(chmeyers): Advance the attribute first?
      double delta = adder.GetDelta(attributes, chosenEffectTarget.runningContext, scaler, batchSize);
      attributes.AddAttribute(adder.type!, delta);
    }
  }

  // Initialize should resolve the attribute names to the actual attribute type.
  public override void Initialize()
  {
    foreach (var adder in _adders)
    {
      adder.type = AttributeType.Find(adder.attribute);
      // Make sure the attribute exists.
      if (adder.type == null)
      {
        throw new Exception("Attribute does not exist: " + adder.attribute + " in attribute puller effect " + effect);
      }
    }
  }

  public override bool SupportsBatching()
  {
    return true;
  }

  // Attribute Effects can scale by any amount.
  public override double MinScale(ChosenEffectTarget target)
  {
    return Double.MinValue;
  }

  public override double MaxScale(ChosenEffectTarget target)
  {
    return Double.MaxValue;
  }

  public override double Utility(IHouseholdContext household, IAbilityContext runner, ChosenEffectTarget chosenEffectTarget, double scaler = 1)
  {
    IAttributeContext attributes = (IAttributeContext)chosenEffectTarget.target!;
    if (attributes == null)
    {
      return 0.0;
    }
    double utility = 0.0;
    foreach (var adder in _adders)
    {
      double deltaValue = adder.GetDelta(attributes, chosenEffectTarget.runningContext, scaler);
      utility += attributes.Utility(adder.type!, deltaValue);
    }
    return utility;
  }

  class AttributeAdder
  {
    // Constructor
    public AttributeAdder(string attribute, AbilityValue target, AbilityValue amount)
    {
      this.attribute = attribute;
      this.target = target;
      this.amount = amount;
    }

    public double GetDelta(IAttributeContext attributes, IAbilityContext? context, double scaler = 1, int batchSize = 1)
    {
      double amount = this.amount.GetScaledValue(context, scaler) * batchSize;
      if (amount == 0) return 0;
      double target = this.target.GetScaledValue(context, scaler);
      // Get the attribute from the person.
      double currentValue = attributes.GetAttributeValue(this.type!);
      // Positive Amounts add up to the target, negative amounts subtract down to the target.
      if (amount > 0)
      {
        // If we are already greater than the target, then we don't need to do anything.
        if (currentValue >= target) return 0;
        // If the current value is within amount of the target, then set it to the target.
        if (currentValue + amount >= target)
        {
          return target - currentValue;
        }
        // Otherwise, increase the attribute by amount.
        else
        {
          return amount;
        }
      }
      else
      {
        // If we are already less than the target, then we don't need to do anything.
        if (currentValue <= target) return 0;
        // If the current value is within amount of the target, then set it to the target.
        if (currentValue + amount <= target)
        {
          return target - currentValue;
        }
        // Otherwise, decrease the attribute by amount.
        else
        {
          return amount;
        }
      }
    }

    public string attribute = "";
    public AttributeType? type;
    public AbilityValue target;
    public AbilityValue amount;
  }
  // List of attributes to pull.
  private List<AttributeAdder> _adders = new List<AttributeAdder>();
}