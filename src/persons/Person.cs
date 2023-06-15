// Person is the main class describing every person in the game.
// Even cities and non-primary villages are subclasses of Person.

using System;
using System.Collections.Concurrent;
using Village.Abilities;
using Village.Attributes;
using Village.Base;
using Village.Buildings;
using Village.Households;
using Village.Items;
using Village.Skills;
using Village.Tasks;

namespace Village.Persons;


public class Person : ITaskRunner, ISkillContext, IAbilityContext, IInventoryContext, IHouseholdContext, IAttributeContext
{
  // Registry of all the persons, keyed on what household they are in.
  public static Dictionary<Household, HashSet<Person>> global_persons = new Dictionary<Household, HashSet<Person>>();

  // Calculate the valid tasks for this Person.
  private void CalculateValidTasks()
  {
    // Only recalculate the validTasks set if it's dirty.
    if (_validTasksDirty)
    {
      // Clear the validTasks set.
      validTasks.Clear();
      // Get Tasks based on abilities.
      validTasks.UnionWith(WorkTask.GetTasksForAbilities(Abilities));
      validTasksByOutput.Clear();
      validTasksByInput.Clear();
      // Iterate over the valid tasks.
      foreach (WorkTask task in validTasks)
      {
        // Iterate over the outputs of the task.
        foreach (ItemType itemType in task.outputs.Keys)
        {
          // Add the task to the validTasksByOutput dictionary.
          if (!validTasksByOutput.ContainsKey(itemType))
          {
            validTasksByOutput.Add(itemType, new HashSet<WorkTask>());
          }
          validTasksByOutput[itemType].Add(task);
        }
        // Iterate over the inputs of the task.
        foreach (var pair in task.inputs)
        {
          // Add the task to the validTasksByInput dictionary.
          if (!validTasksByInput.ContainsKey(pair.Key))
          {
            validTasksByInput.Add(pair.Key, new HashSet<WorkTask>());
          }
          validTasksByInput[pair.Key].Add(task);
        }
      }
      // The valid tasks are no longer dirty.
      _validTasksDirty = false;
    }
  }

  // Readonly accessor for valid tasks, will have all the tasks
  // available to a person based on their abilities, but is not
  // filtered by the person's current inventory.
  public HashSet<WorkTask> PotentialTasks
  {
    get
    {
      lock (_cacheLock)
      {
        CalculateValidTasks();
        return validTasks;
      }
    }
  }

  private HashSet<WorkTask> _AvailableTasks(Inventory inventory)
  {
    lock (_cacheLock)
    {
      CalculateValidTasks();
      return WorkTask.FilterTasksForInventory(validTasks, inventory, this);
    }
  }

  // Set of available tasks, filtered by the person's personal inventory.
  public HashSet<WorkTask> AvailablePersonalTasks
  {
    get
    {
      return _AvailableTasks(inventory);
    }
  }

  // Set of available tasks, filtered by the person's household inventory.
  public HashSet<WorkTask> AvailableTasks
  {
    get
    {
      return _AvailableTasks(household.inventory);
    }
  }

  private void CalculateValidBuildings()
  {
    // Only recalculate the valid buildings set if it's dirty.
    if (_validBuildingsDirty)
    {
      // Clear the validTasks set.
      validBuildings.Clear();
      // Only the head of household can build buildings.
      if (householdRole == Role.HeadOfHousehold)
      {
        foreach (BuildingType buildingType in BuildingType.buildingTypes.Values)
        {
          if (Abilities.IsSupersetOf(buildingType.requirements))
          {
            validBuildings.Add(buildingType);
          }
        }
      }
      // The valid buildings are no longer dirty.
      _validBuildingsDirty = false;
    }
  }

  // Set of available buildings for this person to build.
  public HashSet<BuildingType> AvailableBuildings
  {
    get
    {
      lock (_cacheLock)
      {
        CalculateValidBuildings();
        return validBuildings;
      }
    }
  }

  public double SetAttribute(AttributeType attributeType, double value)
  {
    return attributes.SetValue(attributeType, value);
  }

  public double GetAttributeValue(AttributeType attributeType)
  {
    return attributes.GetValue(attributeType);
  }

  public double AddAttribute(AttributeType attributeType, double value)
  {
    return attributes.AddValue(attributeType, value);
  }

  public double AddAttribute(AttributeType attributeType)
  {
    attributes.Add(attributeType);
    return attributes.GetValue(attributeType);
  }

  public double Utility(AttributeType attributeType, double delta)
  {
    return attributes.Utility(attributeType, delta);
  }

  // Get the amount this person will offer for a set of items.
  // In this case the other person (the seller) is assumed to be the one initiating the trade.
  // The offer may depend on the seller, such as the buyer's relationship with the seller.
  public double GetOffer(IDictionary<Item, int> items, Person seller)
  {
    // TODO(chmeyers): Implement buyer/seller relationships.
    // TODO(chmeyers): Implement non-linear quantities.
    // Look up the items in the price list.
    double offer = 0;
    foreach (KeyValuePair<Item, int> item in items)
    {
      offer += priceList.BidPrice(item.Key) * item.Value;
    }
    return offer;
  }

  // Get the price this person wants for a set of items.
  // In this case the other person (the buyer) is assumed to be the one initiating the trade.
  // The offer may depend on the buyer, such as the buyer's relationship with the seller.
  public double GetPrice(IDictionary<Item, int> items, Person buyer)
  {
    // TODO(chmeyers): Implement buyer/seller relationships.
    // TODO(chmeyers): Implement non-linear quantities.
    // Look up the items in the price list.
    double offer = 0;
    foreach (KeyValuePair<Item, int> item in items)
    {
      offer += priceList.AskPrice(item.Key) * item.Value;
    }
    return offer;
  }

  // Propose a trade with another person.
  // The offer is the set of items this person is offering to the other person.
  // The price is the set of items this person is requesting from the other person.
  // The other person can accept or reject the trade based on their Price List.
  // Returns true if the trade is accepted, false otherwise.
  public bool ProposeTrade(Person otherPerson, IDictionary<Item, int> offer, IDictionary<Item, int> price)
  {
    // Check that the other person is not this person.
    if (otherPerson == this)
    {
      throw new System.ArgumentException("Cannot trade with self.");
    }
    // Check that the offer value is >= the price value.
    double offerValue = otherPerson.GetOffer(offer, this);
    double priceValue = otherPerson.GetPrice(price, this);
    if (offerValue < priceValue)
    {
      return false;
    }
    return inventory.Trade(otherPerson.inventory, offer, price);
  }


  // Constructor for a person.
  public Person(string id, string name, Household? household = null, Role? role = null)
  {
    this.id = id;
    this.name = name;
    // Target and Context for Attribute effects point back at this Person.
    this.attributes = new AttributeSet(this, this, this);
    attributes.AbilitiesChanged += UpdateAbilities;
    // Add the Calendar attributes as a scoped attribute set.
    attributes.AddScopedSet(Calendar.CalendarAttributes());
    this.skills = new SkillSet(this);
    // If household is null, create a new household for the person.
    if (household == null && role != null && role != Role.HeadOfHousehold)
    {
      throw new System.ArgumentException("Persons without a existing household must be the head of household.");
    }
    this.household = (household == null ? new Household() : household);
    this.householdRole = ((Role)(role == null ? Role.HeadOfHousehold : role));
    this.household.AbilitiesChanged += UpdateAbilities;
    // If the household already existed, we need to get it's existing abilities.
    if (household != null)
    {
      PopulateAbilities(this.household);
    }
    // Watch for changes to the inventory.
    this.inventory.AbilitiesChanged += UpdateAbilities;
    // Add the person to the registry.
    if (global_persons.ContainsKey(this.household))
    {
      global_persons[this.household].Add(this);
    }
    else
    {
      global_persons.Add(this.household, new HashSet<Person>() { this });
    }
    // Register with the household.
    this.household.AddPerson(this, this.householdRole);
  }

  // Unique ID for the person.
  public readonly string id;

  // The person's name.
  public string name;

  // The person's household.
  public Household household { get; protected set; }

  // Role that the person has in their household.
  public Role householdRole { get; protected set; }

  // The person's main inventory.
  public Inventory inventory { get; protected set; } = new Inventory();

  // The person's attributes.
  public AttributeSet attributes { get; protected set; }

  // The person's skill set.
  public SkillSet skills { get; protected set; }

  public IPriceList priceList { get; set; } = ConfigPriceList.Default;

  // The person's running tasks, any thread is allowed to add to this queue.
  // Only the main game loop thread should remove from this queue.
  // Only the oldest task in the queue will be advanced.
  public ConcurrentQueue<RunningTask> runningTasks { get; protected set; } = new ConcurrentQueue<RunningTask>();
  // High priority tasks, these will be run before any other tasks.
  public ConcurrentQueue<RunningTask> priorityTasks { get; protected set; } = new ConcurrentQueue<RunningTask>();
  // Zero cost tasks, any tasks in this queue will be run on the next game tick.
  public ConcurrentQueue<RunningTask> zeroCostTasks { get; protected set; } = new ConcurrentQueue<RunningTask>();
  public void EnqueueTask(RunningTask task, bool prioritize)
  {
    if (task.ticksRemaining == 0)
    {
      zeroCostTasks.Enqueue(task);
    }
    else if (prioritize)
    {
      priorityTasks.Enqueue(task);
    }
    else
    {
      runningTasks.Enqueue(task);
    }
  }

  // The person's current job.
  // TODO(chmeyers): Support configable job types.
  // At the moment this is just a placeholder for the job system.
  public bool isTrader = false;

  // Set of permanant abilities the person has.
  protected HashSet<AbilityType> _permAbilities = new HashSet<AbilityType>();

  // Cache of temporary abilities the person has from items, along with the item
  // that granted the ability.
  // This contains just the item's direct abilities, not the
  // sub-abilities of those abilities.
  protected Dictionary<AbilityType, HashSet<IAbilityProvider>> _abilityProviders = new Dictionary<AbilityType, HashSet<IAbilityProvider>>();

  public Dictionary<AbilityType, HashSet<IAbilityProvider>> AbilityProviders { get { return _abilityProviders; } }

  private HashSet<AbilityType> _abilities = new HashSet<AbilityType>();

  public HashSet<AbilityType> Abilities { get { return _abilities; } }

  // TODO(chmeyers): Unregister from events when the person is destroyed.
  // Same for all the other classes that have events, otherwise garbage
  // collection will be blocked.
  public event AbilitiesChanged? AbilitiesChanged;

  public void GrantAbility(AbilityType ability)
  {
    lock (_cacheLock)
    {
      HashSet<AbilityType> added = new HashSet<AbilityType>();
      added.Add(ability);
      added.UnionWith(ability.subTypes);
      // Add the abilities to the abilities set.
      _permAbilities.UnionWith(added);
      // Update the abilities cache.
      UpdateAbilities(this, added, null, null);

    }
  }

  public double? GetNamedValue(string name)
  {
    // Resolved from attributes first then skills.
    return attributes.GetNamedValue(name) ?? skills.GetNamedValue(name);
  }

  public bool IsSeasonalValue(string name)
  {
    return attributes.IsSeasonalValue(name);
  }

  public double? GetSeasonalValue(string name, int daysInFuture)
  {
    return attributes.GetSeasonalValue(name, daysInFuture);
  }

  private void UpdateAbilities(IAbilityProvider? addedProvider, IEnumerable<AbilityType>? added, IAbilityProvider? removedProvider, IEnumerable<AbilityType>? removed)
  {
    lock (_cacheLock)
    {
      IAbilityCollection.UpdateAbilities(ref _abilityProviders, ref _abilities, addedProvider, added, removedProvider, removed, AbilitiesChanged);
      _validTasksDirty = true;
      _validBuildingsDirty = true;
    }
  }

  private void PopulateAbilities(IAbilityCollection abilityCollection)
  {
    lock (_cacheLock)
    {
      foreach (var providerAbilities in abilityCollection.AbilityProviders)
      {
        foreach (var provider in providerAbilities.Value)
        {
          if (_abilityProviders.ContainsKey(providerAbilities.Key))
          {
            _abilityProviders[providerAbilities.Key].Add(provider);
          }
          else
          {
            _abilityProviders.Add(providerAbilities.Key, new HashSet<IAbilityProvider>() { provider });
          }
        }
        _abilities.Add(providerAbilities.Key);
      }
      _validTasksDirty = true;
      _validBuildingsDirty = true;
    }
  }

  public bool GrantXP(Skill skill, double xp)
  {
    return skills.GrantXP(skill, xp);
  }

  public bool GrantLevel(Skill skill)
  {
    return skills.GrantLevel(skill);
  }

  public bool GrantLevel(Skill skill, int level)
  {
    return skills.GrantLevel(skill, level);
  }

  public int GetLevel(Skill skill)
  {
    return skills.GetLevel(skill);
  }

  public double GetXP(Skill skill)
  {
    return skills.GetXP(skill);
  }

  public double GetNextLevelXP(Skill skill)
  {
    return skills.GetNextLevelXP(skill);
  }

  public double Utility(Skill skill, int trainingLevel, double trainingAmount)
  {
    // TODO(chmeyers): Multiply by the importance of the skill to the person's job.
    return skills.Utility(skill, trainingLevel, trainingAmount);
  }

  // Take whatever stuff from the household inventory that the person needs.
  public void TakeNeedsFromHousehold()
  {
    // At the moment this just takes all the tools a person doesn't have,
    // unless they are a trader.
    if (isTrader) return;
    Dictionary<Item, int> items = new Dictionary<Item, int>();
    foreach (var itemType in household.inventory.items)
    {
      // TODO(chmeyers): This doesn't deal properly with multiple ability levels.
      if (itemType.Key.itemGroup == ItemGroup.TOOL && !inventory.Contains(itemType.Key))
      {
        var item = household.inventory.Get(itemType.Key);
        if (item != null)
        {
          // Add the item dictionary to the items dictionary.
          items.Add(item.First().Key, item.First().Value);
        }
      }
    }
    // Transfer the items from the household to the person.
    household.inventory.Transfer(inventory, items);
  }

  // Give excess stuff to the household inventory.
  public void GiveSurplusToHousehold()
  {
    // At the moment this just gives all non-tools/currency unless they are a trader.
    if (isTrader) return;
    Dictionary<Item, int> items = new Dictionary<Item, int>();
    foreach (var itemType in inventory.items)
    {
      if (itemType.Key.itemGroup != ItemGroup.TOOL && itemType.Key.itemGroup != ItemGroup.CURRENCY)
      {
        foreach (var item in inventory[itemType.Key])
        {
          items.Add(item.Key, item.Value);
        }
      }
    }
    // Transfer the items from the person to the household.
    inventory.Transfer(household.inventory, items);
  }

  public void PickTaskFromSet(HashSet<WorkTask> taskset, bool prioritize = false)
  {
    // Filter the taskset to only tasks that the person can do.
    var available = AvailableTasks.Intersect(taskset);
    if (available.Count() == 0) return;
    // Pick the best task from the set.
    // TODO(chmeyers): Implement an intelligent picking algorithm.
    WorkTask best = available.Max()!;
    // TODO(chmeyers): Deal with targets.
    if (best.targets.Count > 0) throw new Exception("Targeted Mandatory tasks not implemented yet.");
    // Enqueue the task as a running task. Prioritized tasks go to the front of the queue.
    var runningTask = TaskRunner.StartTask(this, this.household, best, null);
    if (runningTask == null) throw new Exception("Failed to start Mandatory task: " + best.task + " for person: " + name);
    EnqueueTask(runningTask, prioritize);
  }

  public void PickRandomTask(Random randomizer, HashSet<WorkTask> blacklist)
  {
    // Traders still immune to real behavior.
    if (isTrader) return;
    // Only pick a task if the person has no tasks.
    if (runningTasks.Count > 0) return;
    // Pick a random task from the set of available tasks.
    HashSet<WorkTask> available = AvailableTasks.Except(blacklist).ToHashSet();
    // Eliminate any tasks that have targets.
    available.ExceptWith(available.Where(t => t.targets.Count > 0));
    if (available.Count() == 0) return;
    // Pick a random task from the set.
    var random = available.ElementAt(randomizer.Next(available.Count()));
    // Enqueue the task as a running task.
    var runningTask = TaskRunner.StartTask(this, this.household, random, null);
    if (runningTask == null) throw new Exception("Failed to start Random task: " + random.task + " for person: " + name);
    EnqueueTask(runningTask, false);
  }

  private WorkTask? _PickTask(HashSet<WorkTask>? blacklist, out Dictionary<string, Effects.ChosenEffectTarget>? bestTargets, out double bestScale)
  {
    bestScale = 1.0;
    bestTargets = null;
    // Only pick a task if the person has no tasks.
    if (runningTasks.Count > 0) return null;
    // Pick a task from the set of available tasks.
    HashSet<WorkTask> available = blacklist != null ? AvailableTasks.Except(blacklist).ToHashSet() : AvailableTasks.ToHashSet();
    // Eliminate any tasks that have multiple targets.
    // TODO(chmeyers): Implement multiple target task picking.
    available.ExceptWith(available.Where(t => t.targets.Count > 1));
    if (available.Count() == 0) return null;
    // Get a utility score for every task and pick the highest scoring task.
    WorkTask? best = null;
    double bestScore = 0;

    foreach (var task in available)
    {
      if (task.compulsory) continue;
      // Get the utility score for the task.
      // TODO(chmeyers): Cache the utility score for each task and only recalculate when needed.
      Dictionary<string, Effects.ChosenEffectTarget> targets = new Dictionary<string, Effects.ChosenEffectTarget>();
      double scale = 1.0;
      double score = task.Utility(this, this.household, ref targets, ref scale);
      // >= here as we are willing to do a task with zero utility, as the person
      // is still gaining due to the time utility.
      if (score >= bestScore)
      {
        best = task;
        bestScore = score;
        bestTargets = targets;
        bestScale = scale;
      }
    }
    if (best == null)
    {
      // No task was found, re-evaluate our time utility, but do not re-pick a task.
      // TODO(chmeyers): We should have a set of last-ditch tasks that we can do if
      // we have nothing else to do. Perhaps training?
      DetermineTimeUtility(true);
      return null;
    }
    return best;
  }

  public WorkTask? PickTask(HashSet<WorkTask>? blacklist = null)
  {
    WorkTask? task = _PickTask(blacklist, out Dictionary<string, Effects.ChosenEffectTarget>? bestTargets, out double bestScale);
    if (task == null) return null;
    // Enqueue the task as a running task.
    var runningTask = TaskRunner.StartTask(this, this.household, task, bestTargets, bestScale);
    if (runningTask == null) throw new Exception("Failed to start Chosen task: " + task.task + " for person: " + name);
    EnqueueTask(runningTask, false);
    return task;
  }

  public void BuildAllUnownedBuildings()
  {
    // Filter the taskset to only tasks that the person can do.
    var available = AvailableBuildings;
    if (available.Count() == 0) return;
    // Filter out any buildings the household already has.
    available.ExceptWith(household.buildings.Select(b => b.buildingType));
    if (available.Count() == 0) return;
    // Build everything in the set.
    foreach (var buildingType in available)
    {
      // Add the building to the household.
      household.AddBuilding(buildingType);
    }
  }

  private class UtilityCacheValue
  {
    public UtilityCacheValue(long expiry, UtilityQuantityList values)
    {
      this.expiry = expiry;
      this.values = values;
    }

    public long expiry;
    public UtilityQuantityList values;

    public override string ToString()
    {
      // For friendly printing, create a comma separated list of the values.
      return string.Join(", ", values);
    }
  }

  // This person's cache of item cost. Value is a pair of (cache expiry, cost).
  private Dictionary<ItemType, UtilityCacheValue> _costCache = new Dictionary<ItemType, UtilityCacheValue>();
  private const long cost_cache_duration = Calendar.ticksPerWeek;
  // What does it cost for this person to produce the given item?
  public UtilityQuantityList ProductionCost(ItemType itemType)
  {
    lock (_cacheLock)
    {
      if (_costCache.TryGetValue(itemType, out var costPair))
      {
        if (costPair.expiry >= Calendar.Ticks) return costPair.values;
      }
      // Calls here shouldn't result in recursion loops, but just in case, we'll set a cache value
      // before calling any other functions.
      _costCache[itemType] = new UtilityCacheValue(Calendar.Ticks, new UtilityQuantityList());

      CalculateValidTasks();
      if (!validTasksByOutput.ContainsKey(itemType)) return _costCache[itemType].values;
      double cost = double.MinValue;
      foreach (var task in validTasksByOutput[itemType])
      {
        if (task.compulsory) continue;
        Dictionary<string, Effects.ChosenEffectTarget> targets = new Dictionary<string, Effects.ChosenEffectTarget>();
        double scale = 1.0;
        double score = task.PotentialOutputUtility(this, this.household, ref targets, ref scale);
        // score should be negative, as it is the cost.
        // In theory, the utility of the effects could be so large that the cost is negative,
        // leading to a negative price. (Maybe when the person is able to level up from the task.)
        double thisOutput = 0;
        // Since we are looking for the cost of itemType, we add what we could get selling
        // or using the other outputs.
        foreach (var output in task.OutputTypes(this, scale))
        {
          if (output.Key == itemType) thisOutput += output.Value;
          else score += this.household.Utility(this, output.Key, output.Value);
        }
        if (thisOutput == 0) continue;
        // partition the score evenly among all outputs.
        cost = Math.Max(cost, score / thisOutput);
      }
      // Cache the cost, unless it's positive, as those are probably temporary due to one-time
      // effect benefits.
      UtilityQuantityList costList = new UtilityQuantityList();
      if (cost > double.MinValue)
      {
        costList.Add(new UtilityQuantity(int.MaxValue, int.MaxValue, cost));
      }
      if (cost <= 0)
      {
        _costCache[itemType] = new UtilityCacheValue(Calendar.Ticks + cost_cache_duration, costList);
      }
      else
      {
        // When the cost is negative, we still cache, but only for this tick.
        _costCache[itemType] = new UtilityCacheValue(Calendar.Ticks, costList);
      }
      return costList;
    }
  }

  // This person's cache of item worth. Value is a pair of (cache expiry, worth).
  private Dictionary<ItemType, UtilityCacheValue> _worthCache = new Dictionary<ItemType, UtilityCacheValue>();
  private const long worth_cache_duration = Calendar.ticksPerWeek;
  // How much is this item worth as an input to further production?
  public UtilityQuantityList WorthAsInput(ItemType itemType, double minWorth = 0)
  {
    lock (_cacheLock)
    {
      // Check the cache.
      if (_worthCache.TryGetValue(itemType, out var worthPair))
      {
        if (worthPair.expiry >= Calendar.Ticks) return worthPair.values;
      }

      // Set the cache to an empty value, to ensure that recursive calls don't loop forever.
      _worthCache[itemType] = new UtilityCacheValue(Calendar.Ticks, new UtilityQuantityList());
      // Note that we don't point the cache entry above to our real new list, as we don't want
      // to return a partial result when called recursively.
      UtilityQuantityList worthList = new UtilityQuantityList();

      CalculateValidTasks();
      if (!validTasksByInput.ContainsKey(itemType)) return _worthCache[itemType].values;
      foreach (var task in validTasksByInput[itemType])
      {
        if (task.compulsory) continue;
        Dictionary<string, Effects.ChosenEffectTarget> targets = new Dictionary<string, Effects.ChosenEffectTarget>();
        double scale = 1.0;
        double score = task.PotentialInputUtility(this, this.household, ref targets, ref scale);
        int thisInput = 0;
        // Since we are looking for the worth of itemType, we remove the cost to buy or
        // produce the other inputs from the score.
        foreach (var input in task.Inputs(this, scale))
        {
          if (input.Key == itemType) thisInput += input.Value;
          else score += this.household.Utility(this, input.Key, -input.Value);
        }
        if (thisInput == 0 || score <= 0) continue;
        double worth = score / thisInput;
        if (worth > minWorth)
        {
          worthList.Add(new UtilityQuantity(thisInput, thisInput, worth));
        }
      }
      // Sort the list with highest marginalUtility first, then go through an
      // prune out any entries worse than their predecessors.
      worthList.Sort();

      // Cache the worth.
      _worthCache[itemType] = new UtilityCacheValue(Calendar.Ticks + worth_cache_duration, worthList);
      return _worthCache[itemType].values;
    }
  }

  // How much is this person's time worth?
  public double TimeUtility()
  {
    return DetermineTimeUtility();
  }

  // How many tasks should have positive time utility.
  private const int determine_time_kth = 6;
  // How much less should the person's salary be than the kth task.
  private const double salary_buffer = 1.0;
  private const double max_salary_increase = 1000.0;
  private const long salary_cache_duration = Calendar.ticksPerWeek;
  private KeyValuePair<long, double> _salaryCache = new KeyValuePair<long, double>(-1, 100);
  public double DetermineTimeUtility(bool forceRefresh = false)
  {
    lock (_cacheLock)
    {
      // Check the cache.
      if (!forceRefresh && _salaryCache.Key >= Calendar.Ticks) return _salaryCache.Value;
      // Set the cache to the old value for the rest of the tick, to limit recursion.
      _salaryCache = new KeyValuePair<long, double>(Calendar.Ticks, _salaryCache.Value);
      // Loop through all the valid tasks and pick the kth best one.
      // store the k highest scores, sorted by score.
      List<double> bestTasks = new List<double>();

      double bestAvailableTask = 0;
      // Note here that we're checking Potential Tasks, not Available Tasks.
      // It's possible that they can't actually run any of these due to lack of inputs.
      foreach (var task in PotentialTasks)
      {
        if (task.compulsory) continue;
        double rawTime = task.timeCost.GetValue(this);
        if (rawTime <= 0) continue;
        // Get the utility score for the task.
        Dictionary<string, Effects.ChosenEffectTarget> targets = new Dictionary<string, Effects.ChosenEffectTarget>();
        double scale = 1.0;
        double score = task.PotentialTimeUtility(this, this.household, ref targets, ref scale);
        int time = (int)Math.Ceiling(rawTime * scale);
        score /= time;
        // If the list is not full, add the task.
        if (bestTasks.Count < determine_time_kth)
        {
          bestTasks.Add(score);
          bestTasks.Sort();
        }
        // If the list is full, and the score is better than the worst score, add the task.
        else if (score > bestTasks[0])
        {
          bestTasks.Add(score);
          // Remove the worst score.
          bestTasks.RemoveAt(0);
          bestTasks.Sort();
        }
        // We also track the best available task, to ensure that they can actually do
        // at least one task.
        if (score > bestAvailableTask && AvailableTasks.Contains(task))
        {
          bestAvailableTask = score;
        }
      }
      // If there were no tasks, return 0.
      if (bestTasks.Count == 0) return 0;

      // Take the kth score, or lowest score if there are less than k tasks.
      double salary = Math.Max(bestTasks[0] - salary_buffer, 0);
      salary = Math.Min(salary, bestAvailableTask);
      // cap increases, but not decreases.
      salary = Math.Min(salary, _salaryCache.Value + max_salary_increase);
      // Cache the salary.
      _salaryCache = new KeyValuePair<long, double>(Calendar.Ticks + salary_cache_duration, salary);
      return salary;
    }
  }


  // Cache of work tasks the person can do, based on their abilities.
  // They may not have the required item inputs to do the task.
  protected HashSet<WorkTask> validTasks = new HashSet<WorkTask>();

  // Index of valid tasks by output item type.
  protected Dictionary<ItemType, HashSet<WorkTask>> validTasksByOutput = new Dictionary<ItemType, HashSet<WorkTask>>();

  // Index of valid tasks by input item type.
  protected Dictionary<ItemType, HashSet<WorkTask>> validTasksByInput = new Dictionary<ItemType, HashSet<WorkTask>>();

  // Cache of buildings that the person can build, based on their abilities.
  protected HashSet<BuildingType> validBuildings = new HashSet<BuildingType>();

  // Dirty bit for the valid tasks, it should be set to dirty
  // whenever the person's abilities change.
  protected bool _validTasksDirty = true;
  private bool _validBuildingsDirty = true;

  // lock for the caches.
  private object _cacheLock = new object();
}