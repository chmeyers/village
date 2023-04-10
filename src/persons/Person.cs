// Person is the main class describing every person in the game.
// Even cities and non-primary villages are subclasses of Person.

using System;
using Village.Abilities;
using Village.Attributes;
using Village.Items;
using Village.Skills;
using Village.Tasks;

namespace Village.Persons;


public class Person : ISkillContext, IAbilityContext, IInventoryContext
{

  // Calculate the ability sets for this Person.
  public void CalculateAbilities()
  {
    if (_attributeAbilitiesDirty || _itemAbilitiesDirty || _allAbilitiesDirty)
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
      // The ability set is no longer dirty.
      _allAbilitiesDirty = false;
    }
  }

  // Calculate the item abilities for this Person.
  public void CalculateItemAbilities()
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
  public void CalculateValidTasks()
  {
    // Only recalculate the validTasks set if it's dirty.
    if (validTasksDirty || _itemAbilitiesDirty || _allAbilitiesDirty)
    {
      // Clear the validTasks set.
      validTasks.Clear();
      CalculateAbilities();
      // Get Tasks based on abilities.
      validTasks.UnionWith(WorkTask.GetTasksForAbilities(allAbilities));
      // The valid tasks are no longer dirty.
      validTasksDirty = false;
    }
  }

  // Readonly accessors for the abilities.
  public HashSet<AbilityType> Abilities
  {
    get
    {
      CalculateAbilities();
      return allAbilities;
    }
  }

  // Readonly accessor for valid tasks, will have all the tasks
  // available to a person based on their abilities, but is not
  // filtered by the person's current inventory.
  public HashSet<WorkTask> PotentialTasks
  {
    get
    {
      CalculateValidTasks();
      return validTasks;
    }
  }

  // Set of available tasks, filtered by the person's current inventory.
  public HashSet<WorkTask> AvailableTasks
  {
    get
    {
      CalculateValidTasks();
      HashSet<WorkTask> availableTasks = new HashSet<WorkTask>();
      foreach (WorkTask task in validTasks)
      {
        // Check that all the inputs required for the task are in the inventory.
        if (inventory.Contains(task.Inputs(this)))
        {
          availableTasks.Add(task);
        }
      }
      return availableTasks;
    }
  }


  // Add an item and quantity to the inventory. If an item with abilities is
  // added and the person doesn't already have the ability,
  // the itemAbilitiesDirty flag is set.
  public void AddItem(Item item, int quantity)
  {
    if (item.itemType.abilities.Count > 0)
    {
      // It might be more efficient to just add the new abilities to the itemAbilities set here,
      // but it's more clear to just set the dirty flag and recalculate the itemAbilities set.
      // Revisit this decision if it becomes a performance issue.
      _itemAbilitiesDirty = true;
    }
    inventory.AddItem(item, quantity);
  }

  public bool RemoveItem(Item item, int quantity)
  {
    if (item.itemType.abilities.Count > 0)
    {
      // It might be more efficient to just remove abilities to the itemAbilities set here,
      // but it's more clear to just set the dirty flag and recalculate the itemAbilities set.
      // Revisit this decision if it becomes a performance issue.
      _itemAbilitiesDirty = true;
    }
    return inventory.RemoveItem(item, quantity);
  }

  public int SetAttribute(AttributeType attributeType, int value)
  {
    _attributeAbilitiesDirty = attributes.SetValue(attributeType, value);
    return attributes.GetValue(attributeType);
  }

  public int AddAttribute(AttributeType attributeType, int value)
  {
    _attributeAbilitiesDirty = attributes.AddValue(attributeType, value);
    return attributes.GetValue(attributeType);
  }


  // Constructor for a person.
  public Person(string id, string name)
  {
    this.id = id;
    this.name = name;
    // Target and Context for Attribute effects point back at this Person.
    this.attributes = new AttributeSet(this, this);
  }

  // Unique ID for the person.
  public readonly string id;

  // The person's name.
  public string name;

  // The person's main inventory.
  public Inventory inventory { get ; protected set; } = new Inventory();

  // The person's attributes.
  public AttributeSet attributes { get ; protected set; }

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
    CalculateItemAbilities();
    return itemAbilities;
  }

  public void GrantAbility(AbilityType ability)
  {
    // Add the ability to the abilities set.
    abilities.Add(ability);
    // Set the dirty bit for the allAbilities set.
    _allAbilitiesDirty = true;
  }

  public PersonSkill? GetSkill(Skill skill)
  {
    return null;
  }

  // Dirty bit for attribute abilities.
  protected bool _attributeAbilitiesDirty = true;

  // Dirty bit for the item abilities, it should be set to dirty
  // whenever the person loses an item that gives them an ability.
  protected bool _itemAbilitiesDirty = true;

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

  // Dirty bit for the valid tasks, it should be set to dirty
  // whenever the person's abilities change.
  protected bool validTasksDirty = true;
}