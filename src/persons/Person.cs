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

  // Calculate the ability sets for this Person.
  private void CalculateAbilities()
  {
    if (_attributeAbilitiesDirty || _itemAbilitiesDirty || _householdAbilitiesDirty || _allAbilitiesDirty)
    {
      // Clear the allAbilities set.
      allAbilities.Clear();
      // Add the abilities set to the allAbilities set.
      allAbilities.UnionWith(abilities);
      // Add the sub-abilities of the abilities set to the allAbilities set.
      foreach (AbilityType abilityType in abilities)
      {
        allAbilities.UnionWith(abilityType.subTypes);
      }
      // Recalculate the itemAbilities set if it's dirty.
      CalculateItemAbilities();
      // Add the itemAbilities set to the allAbilities set.
      allAbilities.UnionWith(itemAbilities.Keys);
      // Add the sub-abilities of the itemAbilities set to the allAbilities set.
      foreach (AbilityType abilityType in itemAbilities.Keys)
      {
        allAbilities.UnionWith(abilityType.subTypes);
      }
      // Get the attribute abilities
      var attributeAbilities = attributes.AttributeAbilities();
      // Add the attributeAbilities set to the allAbilities set.
      allAbilities.UnionWith(attributeAbilities);
      // Add the sub-abilities of the attributeAbilities set to the allAbilities set.
      foreach (AbilityType abilityType in attributeAbilities)
      {
        allAbilities.UnionWith(abilityType.subTypes);
      }
      _attributeAbilitiesDirty = false;
      // Get the household abilities
      var householdAbilities = household.BuildingAbilities();
      // Add the householdAbilities set to the allAbilities set.
      allAbilities.UnionWith(householdAbilities.Keys);
      _householdAbilitiesDirty = false;
      // Add the sub-abilities of the householdAbilities set to the allAbilities set.
      foreach (AbilityType abilityType in householdAbilities.Keys)
      {
        allAbilities.UnionWith(abilityType.subTypes);
      }
      // The ability set is no longer dirty.
      _allAbilitiesDirty = false;
    }
  }

  // Calculate the item abilities for this Person.
  private void CalculateItemAbilities()
  {
    // Only recalculate the itemAbilities set if it's dirty.
    if (_itemAbilitiesDirty)
    {
      // Clear the itemAbilities set.
      itemAbilities.Clear();
      itemAbilities = inventory.ItemAbilities();
      // The item abilities are no longer dirty.
      _itemAbilitiesDirty = false;
    }
  }

  // Calculate the valid tasks for this Person.
  private void CalculateValidTasks()
  {
    // Only recalculate the validTasks set if it's dirty.
    if (_validTasksDirty || _itemAbilitiesDirty || _householdAbilitiesDirty || _allAbilitiesDirty)
    {
      // Clear the validTasks set.
      validTasks.Clear();
      CalculateAbilities();
      // Get Tasks based on abilities.
      validTasks.UnionWith(WorkTask.GetTasksForAbilities(allAbilities));
      // The valid tasks are no longer dirty.
      _validTasksDirty = false;
    }
  }

  // Readonly accessors for the abilities.
  public HashSet<AbilityType> Abilities
  {
    get
    {
      lock (_cacheLock)
      {
        CalculateAbilities();
        return abilities;
      }
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
    // Only recalculate the validTasks set if it's dirty.
    if (_validBuildingsDirty || _itemAbilitiesDirty || _householdAbilitiesDirty || _allAbilitiesDirty)
    {
      // Clear the validTasks set.
      validBuildings.Clear();
      CalculateAbilities();
      // Only the head of household can build buildings.
      if (householdRole == Role.HeadOfHousehold)
      {
        foreach (BuildingType buildingType in BuildingType.buildingTypes.Values)
        {
          if (allAbilities.IsSupersetOf(buildingType.requirements))
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

  public int AddAttribute(AttributeType attributeType, int value)
  {
    return attributes.AddValue(attributeType, value);
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
    attributes.AbilitiesChanged += () => { _attributeAbilitiesDirty = true; _allAbilitiesDirty = true; _validBuildingsDirty = true; _validTasksDirty = true; };
    this.skills = new SkillSet(this);
    // If household is null, create a new household for the person.
    if (household == null && role != null && role != Role.HeadOfHousehold)
    {
      throw new System.ArgumentException("Persons without a existing household must be the head of household.");
    }
    this.household = (household == null ? new Household() : household);
    this.householdRole = ((Role)(role == null ? Role.HeadOfHousehold : role));
    this.household.AbilitiesChanged += () => { _householdAbilitiesDirty = true; _allAbilitiesDirty = true; _validBuildingsDirty = true; _validTasksDirty = true; };
    // Watch for changes to the inventory.
    this.inventory.AbilitiesChanged += () => { _itemAbilitiesDirty = true; _allAbilitiesDirty = true; _validBuildingsDirty = true; _validTasksDirty = true; };
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

  // The person's current job.
  // TODO(chmeyers): Support configable job types.
  // At the moment this is just a placeholder for the job system.
  public bool isTrader = false;

  // Set of permanant abilities the person has.
  protected HashSet<AbilityType> abilities = new HashSet<AbilityType>();

  // Cache of temporary abilities the person has from items, along with the item
  // that granted the ability.
  // This contains just the item's direct abilities, not the
  // sub-abilities of those abilities.
  protected Dictionary<AbilityType, List<Item>> itemAbilities = new Dictionary<AbilityType, List<Item>>();

  // Item abilities for the inventory interface.
  public Dictionary<AbilityType, List<Item>> ItemAbilities()
  {
    lock (_cacheLock)
    {
      CalculateItemAbilities();
      return itemAbilities;
    }
  }

  public void GrantAbility(AbilityType ability)
  {
    lock(_cacheLock)
    {
      // Add the ability to the abilities set.
      abilities.Add(ability);
      // Set the dirty bit for the allAbilities set.
      _allAbilitiesDirty = true;
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

  // Dirty bit for attribute abilities.
  protected bool _attributeAbilitiesDirty = true;

  // Dirty bit for the item abilities, it should be set to dirty
  // whenever the person loses an item that gives them an ability.
  protected bool _itemAbilitiesDirty = true;

  // Dirty bit for the household abilities, it should be set to dirty
  // whenever the person's household building abilities change.
  protected bool _householdAbilitiesDirty = true;

  // Cache for all the abilities the person currently has.
  // This is the union of the abilities and itemAbilities sets, as
  // well as the sub-abilities of those abilities.
  // TODO(chmeyers): Include abilities granted by the environment and buildings.
  protected HashSet<AbilityType> allAbilities = new HashSet<AbilityType>();

  // Dirty bit for the all abilities, it should be set to dirty
  // whenever the person's abilities change.
  protected bool _allAbilitiesDirty = true;

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