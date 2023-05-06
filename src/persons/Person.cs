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


public class Person : ISkillContext, IAbilityContext, IInventoryContext, IHouseholdContext
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
  public HashSet<WorkTask> AvailableHouseholdTasks
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

  public int SetAttribute(AttributeType attributeType, int value)
  {
    return attributes.SetValue(attributeType, value);
  }

  public int GetAttributeValue(AttributeType attributeType)
  {
    return attributes.GetValue(attributeType);
  }

  public int AddAttribute(AttributeType attributeType, int value)
  {
    return attributes.AddValue(attributeType, value);
  }

  public int AddAttribute(AttributeType attributeType)
  {
    attributes.Add(attributeType);
    return attributes.GetValue(attributeType);
  }

  // Get the amount this person will offer for a set of items.
  // In this case the other person (the seller) is assumed to be the one initiating the trade.
  // The offer may depend on the seller, such as the buyer's relationship with the seller.
  public int GetOffer(IDictionary<Item, int> items, Person seller)
  {
    // TODO(chmeyers): Implement buyer/seller relationships.
    // TODO(chmeyers): Implement non-linear quantities.
    // Look up the items in the price list.
    int offer = 0;
    foreach (KeyValuePair<Item, int> item in items)
    {
      offer += priceList.BuyPrice(item.Key) * item.Value;
    }
    return offer;
  }

  // Get the price this person wants for a set of items.
  // In this case the other person (the buyer) is assumed to be the one initiating the trade.
  // The offer may depend on the buyer, such as the buyer's relationship with the seller.
  public int GetPrice(IDictionary<Item, int> items, Person buyer)
  {
    // TODO(chmeyers): Implement buyer/seller relationships.
    // TODO(chmeyers): Implement non-linear quantities.
    // Look up the items in the price list.
    int offer = 0;
    foreach (KeyValuePair<Item, int> item in items)
    {
      offer += priceList.SellPrice(item.Key) * item.Value;
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
    int offerValue = otherPerson.GetOffer(offer, this);
    int priceValue = otherPerson.GetPrice(price, this);
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
    this.attributes = new AttributeSet(this, this);
    attributes.AbilitiesChanged += UpdateAbilities;
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
    // Watch for changes to the global Calendar abilities.
    Calendar.AbilitiesChanged += UpdateAbilities;
    PopulateAbilities(Calendar.CalendarAbilityCollection());
    // Add the person to the registry.
    if (global_persons.ContainsKey(this.household))
    {
      global_persons[this.household].Add(this);
    }
    else
    {
      global_persons.Add(this.household, new HashSet<Person>() { this });
    }
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

  public bool GrantXP(Skill skill, int xp)
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

  public int GetXP(Skill skill)
  {
    return skills.GetXP(skill);
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
    var available = AvailableHouseholdTasks.Intersect(taskset);
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


  // Cache of work tasks the person can do, based on their abilities.
  // They may not have the required item inputs to do the task.
  protected HashSet<WorkTask> validTasks = new HashSet<WorkTask>();

  // Cache of buildings that the person can build, based on their abilities.
  protected HashSet<BuildingType> validBuildings = new HashSet<BuildingType>();

  // Dirty bit for the valid tasks, it should be set to dirty
  // whenever the person's abilities change.
  protected bool _validTasksDirty = true;
  private bool _validBuildingsDirty = true;

  // lock for the caches.
  private object _cacheLock = new object();
}