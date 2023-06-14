// Households are container for multiple people and buildings.
// They have an inventory which is shared by all the people in the household.
// They also have a list of buildings that they own.
// People in the household have assigned roles, which determine what they do.
// They can typically access the household inventory, but do not gain abilities
// from the items in it. They can also access the buildings in the household.

using Village.Abilities;
using Village.Base;
using Village.Buildings;
using Village.Effects;
using Village.Items;
using Village.Tasks;

namespace Village.Households;

public interface IHouseholdContext
{
  // Returns which household the context is in.
  Household household { get; }
}

public class Household : IInventoryContext, IHouseholdContext, IAbilityCollection
{
  // Registry of all the households.
  public static HashSet<Household> global_households = new HashSet<Household>();
  // The household that the context is in.
  public Household household => this;
  // Whether this is a player household.
  // Player households can be controlled by the player.
  public bool isPlayerHousehold { get; set; } = false;
  // The inventory of the household.
  public Inventory inventory { get; private set; }

  // The buildings owned by the household.
  // TODO(chmeyers): protect with a lock.
  public List<Building> buildings { get; private set; }

  // Event handler for when the abilities of a person change.
  public event AbilitiesChanged? AbilitiesChanged;

  // Cache of the abilities granted by the buildings in the household.
  private Dictionary<AbilityType, HashSet<IAbilityProvider>> _abilityProviders = new Dictionary<AbilityType, HashSet<IAbilityProvider>>();

  public Dictionary<AbilityType, HashSet<IAbilityProvider>> AbilityProviders { get { return _abilityProviders; } }

  private HashSet<AbilityType> _abilities = new HashSet<AbilityType>();

  public HashSet<AbilityType> Abilities { get { return _abilities; } }

  public Household(bool isPlayerHousehold = false)
  {
    inventory = new Inventory();
    buildings = new List<Building>();
    this.isPlayerHousehold = isPlayerHousehold;
    // Add the household to the global registry.
    global_households.Add(this);
  }

  // The default context for the household will typically be the head of household.
  // If there is no head of household, it will be a void context.
  public IAbilityContext defaultContext { get; private set; } = ConcreteAbilityContext.voidContext;
  // list of people in the household.
  public HashSet<ITaskRunner> people { get; private set; } = new HashSet<ITaskRunner>();
  public double householdSize { get { return people.Count; } }

  public void AddPerson(ITaskRunner person, Role role)
  {
    if (role == Role.HeadOfHousehold)
    {
      defaultContext = person;
    }
    people.Add(person);
  }

  public void AddBuilding(BuildingType buildingType)
  {
    Building building = new Building(buildingType);
    building.AbilitiesChanged += UpdateAbilities;
    buildings.Add(building);
    if (building.Abilities.Count > 0)
    {
      UpdateAbilities(building, building.Abilities, null, null);
    }
  }

  // TODO(chmeyers): Refactor this.
  public void AddField(BuildingType buildingType)
  {
    Building building = new Field(buildingType, household);
    building.AbilitiesChanged += UpdateAbilities;
    buildings.Add(building);
    if (building.Abilities.Count > 0)
    {
      UpdateAbilities(building, building.Abilities, null, null);
    }
  }

  public void AdvanceBuildings()
  {
    foreach (var building in buildings)
    {
      // Currently only fields need to be advanced.
      (building as Field)?.Advance();
    }
  }

  // Return a list of possible effect targets of the given type
  // for a person running a task for this household.
  // Note that the person is not required to be in the household.
  public List<ChosenEffectTarget> GetPossibleTargets(IAbilityContext context, EffectTargetType targetType)
  {
    List<ChosenEffectTarget> targets = new List<ChosenEffectTarget>();
    // Person, Item, Building, Field, and Crop currently supported.
    switch (targetType)
    {
      case EffectTargetType.Building:
        foreach (var building in buildings)
        {
          targets.Add(new ChosenEffectTarget(targetType, building, this, context));
        }
        break;
      case EffectTargetType.Field:
        foreach (var building in buildings)
        {
          if (building is Field field)
          {
            targets.Add(new ChosenEffectTarget(targetType, field, this, context));
          }
        }
        break;
      case EffectTargetType.Crop:
        foreach (var building in buildings)
        {
          if (building is Field field)
          {
            foreach (var crop in field.crops)
            {
              targets.Add(new ChosenEffectTarget(targetType, crop.Value, this, context));
            }
          }
        }
        break;
      case EffectTargetType.Item:
        // Can target either the household inventory or the person's inventory.
        foreach (var item in inventory.items)
        {
          targets.Add(new ChosenEffectTarget(targetType, item, this, context));
        }
        if (context != this && context is IInventoryContext inventoryContext)
        {
          foreach (var item in inventoryContext.inventory.items)
          {
            targets.Add(new ChosenEffectTarget(targetType, item, this, context));
          }
        }
        break;
      case EffectTargetType.Person:
        // Can target either the household people or the person,
        // but make sure not to target the person twice.
        targets.Add(new ChosenEffectTarget(targetType, context, this, context));
        foreach (var person in Persons.Person.global_persons[household])
        {
          if (person != context)
          {
            targets.Add(new ChosenEffectTarget(targetType, person, this, context));
          }
        }
        break;
    }
    return targets;
  }

  // Functions for determining Utility scores for this household.

  // Effect Utility
  public double Utility(ITaskRunner runner, Effect effect, ChosenEffectTarget chosenTarget, double scale)
  {
    // TODO(chmeyers): This currently assumes that the chosenTarget and runner
    // belong to the household. Once we have effects that can target other households,
    // run by people not in the household, or hired people, this will need to be updated,
    // as those effects aren't useful for this household. They should instead be taken
    // into account by the price of the labor, rent, etc.

    return effect.Utility(household, runner, chosenTarget, scale);
  }

  private double _MarginalUtility(List<DesireUtility> desiredStockpile, int have, int delta, List<DesireUtility> valueList, double costPrice = double.MaxValue)
  {
    if (delta < 0)
    {
      // Removing items.

      // When removing items, we don't consider the values if we have that total quantity,
      // as that is the expected use of the item. Instead we compare against the next best value.
      // We DO consider all the desired stockpile values, as the expected use of the stockpile
      // is to keep the item.
      // TODO(chmeyers): We should only remove them if the associated task is available to run.
      List<DesireUtility> allValues = DesireUtility.MergeExceptQuantity(desiredStockpile, valueList, have);

      double utility = 0;
      double price = valueList.Count > 0 ? valueList[valueList.Count - 1].marginalUtility : 0;
      for (int i = allValues.Count - 1; i >= 0; i--)
      {
        int totalDesired = allValues[i].totalQuantity;
        int excess = Math.Max(have - totalDesired, 0);
        int undesired_amount = Math.Min(-delta, excess);
        delta += undesired_amount;
        utility += price * -undesired_amount;  // +/- epsilon?
        if (delta == 0)
        {
          return utility;
        }
        // Desired Utilities should be higher than the price,
        // but the price might be more up to date, so we use
        // that as a lower bound.
        price = Math.Max(allValues[i].marginalUtility, price);
        have = Math.Min(have, totalDesired);
      }
      int from_inventory = Math.Min(-delta, have);
      int need_to_buy = -delta - from_inventory;
      utility += price * -from_inventory;  // +/- epsilon?
      price = Math.Max(costPrice, price);
      utility += price * -need_to_buy;  // +/- epsilon?
      return utility;
    }
    else if (delta > 0)
    {
      List<DesireUtility> allValues = DesireUtility.Merge(desiredStockpile, valueList);
      // Adding items.
      double utility = 0;
      for (int i = 0; i < allValues.Count; i++)
      {
        int totalDesired = allValues[i].totalQuantity;
        int needed = Math.Max(totalDesired - have, 0);
        int desired_amount = Math.Min(delta, needed);
        delta -= desired_amount;
        utility += Math.Max(allValues[i].marginalUtility, 0) * desired_amount;
        if (delta == 0)
        {
          return utility;
        }
        have = Math.Max(have, totalDesired);
      }
      double fallbackValue = valueList.Count > 0 ? valueList[valueList.Count - 1].marginalUtility : 0;
      utility += fallbackValue * delta;  // +/- epsilon?
      return utility;
    }
    return 0;
  }

  private object _DesiredStockpileLock = new object();
  // TODO(chmeyers): Occasionally clear out the cache.
  private Dictionary<ItemType, KeyValuePair<long, List<DesireUtility>>> _CachedDesiredStockpile = new Dictionary<ItemType, KeyValuePair<long, List<DesireUtility>>>();
  // How many items of this type the household wants to have.
  // This includes stock for vendors and traders, items/food
  // for consumption, and items for household living standards.
  // Inputs for production will generally not be included, as
  // only the end product is really desired.
  // Returns a list of DesiredUtilities, sorted from highest marginal
  // to lowest marginal. (i.e. highest totalQuantity last.)
  public List<DesireUtility> DesiredStockpile(ItemType itemType, int daysInFuture = 0, bool ignoreCache = false)
  {
    lock (_DesiredStockpileLock)
    {
      if (!ignoreCache && daysInFuture == 0 && _CachedDesiredStockpile.TryGetValue(itemType, out var cached))
      {
        if (cached.Key >= Calendar.Ticks)
        {
          return cached.Value;
        }
      }

      if (itemType.itemGroup == ItemGroup.CURRENCY)
      {
        // CURRENCY type items are never desired (for more than their sell price).
        // Cache this forever, since it will never change.
        _CachedDesiredStockpile[itemType] = new KeyValuePair<long, List<DesireUtility>>(long.MaxValue, new List<DesireUtility>());
        return _CachedDesiredStockpile[itemType].Value;
      }
      SortedDictionary<double, int> quantityByUtility = itemType.GetUtilityQuantities(defaultContext, householdSize, daysInFuture);
      // TODO(chmeyers): Add in seed corn needed for next year.
      // TODO(chmeyers): Add tools that household members need.
      // TODO(chmeyers): Add vendor stock w/ BuyPrice+Profit Utility.
      // Recurse and merge the parent items' desired stockpile.
      foreach (var parent in itemType.parentTypes)
      {
        var parentDesired = DesiredStockpile(parent, daysInFuture, ignoreCache);
        foreach (var parentDesire in parentDesired)
        {
          if (quantityByUtility.ContainsKey(parentDesire.marginalUtility))
          {
            quantityByUtility[parentDesire.marginalUtility] += parentDesire.marginalQuantity;
          }
          else
          {
            quantityByUtility[parentDesire.marginalUtility] = parentDesire.marginalQuantity;
          }
        }
      }
      List<DesireUtility> desired = new List<DesireUtility>();
      int runningTotal = 0;
      foreach (var pair in quantityByUtility.Reverse())
      {
        runningTotal += pair.Value;
        desired.Add(new DesireUtility(runningTotal, pair.Value, pair.Key));
      }
      if (daysInFuture == 0)
      {
        // Cache for a week.
        long cacheUntil = Calendar.Ticks + Calendar.ticksPerWeek;
        _CachedDesiredStockpile[itemType] = new KeyValuePair<long, List<DesireUtility>>(cacheUntil, desired);
        return _CachedDesiredStockpile[itemType].Value;
      }
      else
      {
        // Don't cache future values.
        return desired;
      }
    }
  }

  // The actual best offer price of the item on the local market,
  // not including offers from this household, or it's value as an input
  // to further production. Traders will generally backstop this price
  // with an offer equal to the price at another market minus transport
  // costs and a desired profit.
  public List<DesireUtility> ValuePrice(ItemType itemType)
  {
    // TODO(chmeyers): Use a market instead of a price list.
    IPriceList priceList = ConfigPriceList.Default;
    double marketPrice = priceList.BidPrice(itemType);
    // See if any people in the household can beat the market price.
    List<DesireUtility> bestValue = new List<DesireUtility>();
    bestValue.Add(new DesireUtility(int.MaxValue, int.MaxValue, marketPrice));
    foreach (var person in people)
    {
      DesireUtility.MergeFrom(bestValue, person.WorthAsInput(itemType));
    }
    return bestValue;
  }

  // The actual price of the item on the local market not
  // including offers from this household, or
  // the cost to produce it yourself plus a desired profit.
  // Vendors will generally sell items for this price.
  // If the vendor's cost+profit is higher than the market
  // price, then the vendor will not replenish their stock,
  // and may have to adjust their desired profit.
  // Traders will generally backstop this price with an offer
  // equal to the price at another market plus transport costs
  // and a desired profit.
  // TODO(chmeyers): Make sure we are dealing correctly with vendor "overstock".
  public double CostPrice(ItemType itemType)
  {
    // TODO(chmeyers): Use a market instead of a price list.
    IPriceList priceList = ConfigPriceList.Default;
    double marketPrice = priceList.AskPrice(itemType);
    // See if any people in the household can beat the market price.
    double bestPrice = marketPrice;
    foreach (var person in people)
    {
      bestPrice = Math.Min(bestPrice, person.ProductionCost(itemType));
    }
    return bestPrice;
  }

  // Item Utility
  // Negative Quantities are for removing items.
  // Returned units are in currency.
  // Note that this household should be willing to buy items
  // for it's utility value minus epsilon, and sell them for plus epsilon.
  public double Utility(ITaskRunner runner, ItemType itemType, int quantity)
  {
    double utility = _MarginalUtility(DesiredStockpile(itemType), inventory.Count(itemType), quantity, ValuePrice(itemType), (quantity < 0 ? CostPrice(itemType) : double.MaxValue));
    // Add in the utility for the parents/ancestors of this item, as it can
    // also be used as any of them.
    foreach (var parent in itemType.parentTypes)
    {
      // We pass in zero for the costPrice here, because we don't want to
      // double count the cost.
      utility += _MarginalUtility(DesiredStockpile(parent), inventory.Count(parent), quantity, ValuePrice(parent), 0);
    }
    return utility;
  }

  // Utility of a quantity of an item expected to be produced in the future.
  public double FutureUtility(ITaskRunner runner, ItemType itemType, int quantity, int days)
  {
    // TODO(chmeyers): Do we need to track seasonal market prices? Currently this is looking
    // at future stockpile utility, but assuming static market prices.
    // TODO(chmeyers): Do we need to simulate a burn down rate for the household's current
    // inventory? Currently this just assumes we'll have the same amount of everything as now.
    double utility = _MarginalUtility(DesiredStockpile(itemType, days), inventory.Count(itemType), quantity, ValuePrice(itemType), (quantity < 0 ? CostPrice(itemType) : double.MaxValue));
    // Add in the utility for the parents/ancestors of this item, as it can
    // also be used as any of them.
    foreach (var parent in itemType.parentTypes)
    {
      utility += _MarginalUtility(DesiredStockpile(parent, days), inventory.Count(parent), quantity, ValuePrice(parent), 0);
    }
    return utility;
  }

  // Time Utility
  public double TimeUtility(ITaskRunner runner, int time)
  {
    // The time cost is either the opportunity cost of the runner, or the
    // wage they are paid, depending on what their role is.
    // TODO(chmeyers): If the runner isn't a member of this household, then
    // replace this with the cost of hiring them.
    return runner.TimeUtility() * time;
  }

  private void UpdateAbilities(IAbilityProvider? addedProvider, IEnumerable<AbilityType>? added, IAbilityProvider? removedProvider, IEnumerable<AbilityType>? removed)
  {
    IAbilityCollection.UpdateAbilities(ref _abilityProviders, ref _abilities, addedProvider, added, removedProvider, removed, AbilitiesChanged);
  }

}
