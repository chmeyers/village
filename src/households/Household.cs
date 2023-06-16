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

public interface IHouseholdContext : IEffectTargetContext
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

  public Market? market { get; private set; }

  // The default context for the household will typically be the head of household.
  // If there is no head of household, it will be a void context.
  public IAbilityContext defaultContext { get; private set; } = ConcreteAbilityContext.voidContext;
  // list of people in the household.
  public HashSet<ITaskRunner> people { get; private set; } = new HashSet<ITaskRunner>();
  public double householdSize { get { return people.Count; } }

  public void JoinMarket(Market market)
  {
    this.market = market;
  }
  public void LeaveMarket()
  {
    this.market = null;
  }

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

  private double _MarginalUtility(UtilityQuantityList desiredStockpile, int have, int delta, UtilityQuantityList valueList, UtilityQuantityList? costPrice)
  {
    if (delta < 0)
    {
      // Removing items.

      // When removing items, we don't consider the values if we have that total quantity,
      // as that is the expected use of the item. Instead we compare against the next best value.
      // We DO consider all the desired stockpile values, as the expected use of the stockpile
      // is to keep the item.
      // TODO(chmeyers): We should only remove them if the associated task is available to run.
      UtilityQuantityList allValues = desiredStockpile.Clone().MergeExceptQuantity(valueList, have);

      double utility = 0;
      double price = valueList.GetLastUtility() ?? 0;
      for (int i = allValues.Count - 1; i >= 0; i--)
      {
        // TODO(chmeyers): This could be refactored to skip entries for better performance.
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
      // An null costPrice means we'll use a zero, an empty one means min value.
      double aquirePrice = costPrice == null ? 0 : costPrice.GetLastUtility() ?? double.MinValue;
      price = Math.Max(-aquirePrice, price);
      utility += price * -need_to_buy;  // +/- epsilon?
      return utility;
    }
    else if (delta > 0)
    {
      UtilityQuantityList allValues = desiredStockpile.Clone().Merge(valueList);
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
      // Fallback to the lowest ValuePrice, NOT the desire stockpile.
      double fallbackValue = valueList.GetLastUtility() ?? 0;
      utility += fallbackValue * delta;  // +/- epsilon?
      return utility;
    }
    return 0;
  }

  private UtilityQuantity? _MarginalQuantity(UtilityQuantityList desiredStockpile, int have, bool adding, UtilityQuantityList valueList)
  {
    // Note that unlike _MarginalUtility(), we never ignore items that we have from the valueList.
    // This is because we don't want to sell items that we might want to use.
    UtilityQuantityList allValues = desiredStockpile.Clone().Merge(valueList);
    // Find the interval that includes have.
    for (int i = 0; i < allValues.Count; i++)
    {
      if (have < allValues[i].totalQuantity)
      {
        // We're in this interval.
        if (adding)
        {
          int toNextInterval = allValues[i].totalQuantity - have;
          return new UtilityQuantity(toNextInterval, toNextInterval, allValues[i].marginalUtility);
        }
        else
        {
          int toNextInterval = (i == 0 ? have : have - allValues[i-1].totalQuantity);
          return new UtilityQuantity(toNextInterval, toNextInterval, -allValues[i].marginalUtility);
        }
      }
    }
    // If we have more than the last interval, we have zero marginal utility.
    return null;
  }

  // Return the quantity of an item that share the lowest marginal utility.
  public UtilityQuantity? MarginalQuantity(ItemType itemType, bool adding, UtilityQuantityList? childStockpile = null)
  {
    UtilityQuantityList stockpile = UtilityQuantityList.Stack(DesiredStockpile(itemType), childStockpile);
    UtilityQuantity? quantity = _MarginalQuantity(stockpile, inventory.Count(itemType), adding, ValuePrice(itemType));
    // Add in the utility for the parents/ancestors of this item, as it can
    // also be used as any of them.
    foreach (var parent in itemType.parentTypes)
    {
      // We pass in null for the costPrice here, because we don't want to
      // double count the cost.
      var parentQuantity = MarginalQuantity(parent, adding, stockpile);
      if (quantity == null)
      {
        quantity = parentQuantity;
        continue;
      }
      if (parentQuantity == null)
      {
        continue;
      }
      quantity.marginalUtility += parentQuantity.marginalUtility;
      if (parentQuantity.totalQuantity < quantity.totalQuantity)
      {
        quantity.totalQuantity = parentQuantity.totalQuantity;
        quantity.marginalQuantity = parentQuantity.marginalQuantity;
      }
    }
    return quantity;
  }

  private object _DesiredStockpileLock = new object();
  // TODO(chmeyers): Occasionally clear out the cache.
  private Dictionary<ItemType, KeyValuePair<long, UtilityQuantityList>> _CachedDesiredStockpile = new Dictionary<ItemType, KeyValuePair<long, UtilityQuantityList>>();
  // How many items of this type the household wants to have.
  // This includes stock for vendors and traders, items/food
  // for consumption, and items for household living standards.
  // Inputs for production will generally not be included, as
  // only the end product is really desired.
  // Returns a list of DesiredUtilities, sorted from highest marginal
  // to lowest marginal. (i.e. highest totalQuantity last.)
  public UtilityQuantityList DesiredStockpile(ItemType itemType, int daysInFuture = 0, bool ignoreCache = false)
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
        // TODO(chmeyers): Should households want to keep some currency on hand?
        _CachedDesiredStockpile[itemType] = new KeyValuePair<long, UtilityQuantityList>(long.MaxValue, new UtilityQuantityList());
        return _CachedDesiredStockpile[itemType].Value;
      }
      UtilityQuantityList desired = itemType.GetUtilityQuantities(defaultContext, householdSize, daysInFuture);
      // TODO(chmeyers): Add in seed corn needed for next year.
      // TODO(chmeyers): Add tools that household members need.
      // TODO(chmeyers): Add vendor stock w/ BuyPrice+Profit Utility.
      if (daysInFuture == 0)
      {
        // Cache for a week.
        long cacheUntil = Calendar.Ticks + Calendar.ticksPerWeek;
        _CachedDesiredStockpile[itemType] = new KeyValuePair<long, UtilityQuantityList>(cacheUntil, desired);
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
  public UtilityQuantityList ValuePrice(ItemType itemType)
  {
    // TODO(chmeyers): Use a market instead of a price list.
    IPriceList priceList = ConfigPriceList.Default;
    UtilityQuantityList bestValue = priceList.BidPrice(itemType);
    // See if any people in the household can beat the market price.
    // The BidPrice above probably includes this household's market offers,
    // but that's fine, as those offers are always epsilon worse than
    // what WorthAsInput will return, so Merge will remove them.
    foreach (var person in people)
    {
      bestValue.Merge(person.WorthAsInput(itemType));
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
  // TODO: By convention, these values are negative.
  // TODO(chmeyers): Make sure we are dealing correctly with vendor "overstock".
  public UtilityQuantityList CostPrice(ItemType itemType)
  {
    // TODO(chmeyers): Use a market instead of a price list.
    IPriceList priceList = ConfigPriceList.Default;
    UtilityQuantityList bestPrice = priceList.AskPrice(itemType);
    // See if any people in the household can beat the market price.
    // The AskPrice above probably includes this household's market offers,
    // but that's fine, as those offers are always epsilon worse than
    // what ProductionCost will return, so Merge will remove them.
    foreach (var person in people)
    {
      bestPrice.Merge(person.ProductionCost(itemType));
    }
    return bestPrice;
  }


  private double _Utility(ItemType itemType, int quantity, int days, UtilityQuantityList? cost, UtilityQuantityList? childStockpile)
  {
    UtilityQuantityList stockpile = UtilityQuantityList.Stack(DesiredStockpile(itemType, days), childStockpile);
    double utility = _MarginalUtility(stockpile, inventory.Count(itemType), quantity, ValuePrice(itemType), cost);
    // Add in the utility for the parents/ancestors of this item, as it can
    // also be used as any of them.
    foreach (var parent in itemType.parentTypes)
    {
      // We pass in null for the costPrice here, because we don't want to
      // double count the cost.
      utility += _Utility(parent, quantity, days, null, stockpile);
    }
    return utility;
  }

  // Item Utility
  // Negative Quantities are for removing items.
  // Returned units are in currency.
  // Note that this household should be willing to buy items
  // for it's utility value minus epsilon, and sell them for plus epsilon.
  public double Utility(ItemType itemType, int quantity)
  {
    return _Utility(itemType, quantity, 0, (quantity < 0 ? CostPrice(itemType) : null), null);
  }

  public double Utility(ITaskRunner runner, ItemType itemType, int quantity)
  {
    return Utility(itemType, quantity);
  }

  // Utility of a quantity of an item expected to be produced in the future.
  public double FutureUtility(ITaskRunner runner, ItemType itemType, int quantity, int days)
  {
    // TODO(chmeyers): Do we need to track seasonal market prices? Currently this is looking
    // at future stockpile utility, but assuming static market prices.
    // TODO(chmeyers): Do we need to simulate a burn down rate for the household's current
    // inventory? Currently this just assumes we'll have the same amount of everything as now.
    return _Utility(itemType, quantity, days, (quantity < 0 ? CostPrice(itemType) : null), null);
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

  // TODO(chmeyers): Make profit adjustable.
  public const double market_profit = 0.05;

  // Submit ask prices to the market.
  public void SubmitAskPrices()
  {
    if (market == null)
    {
      return;
    }
    // We are willing to sell every item in our inventory for it's utility value plus a profit.
    foreach (var itemType in inventory.items)
    {
      // TODO(chmeyers): Deal with sales of degraded items.
      var quantity = MarginalQuantity(itemType.Key, false);
      if (quantity == null || quantity.marginalUtility == 0 || quantity.marginalUtility == double.MinValue || quantity.totalQuantity == int.MaxValue) continue;
      quantity.marginalUtility *= (1 + market_profit);
      UtilityQuantityList price = new UtilityQuantityList();
      price.Add(quantity);
      market.AddAsk(itemType.Key, this, price);
    }
  }

  private class PurchasePriority : IComparable<PurchasePriority>
  {
    public ItemType itemType;
    public UtilityQuantity utility;
    public double percentage;
    public UtilityQuantity ourUtility;

    public PurchasePriority(ItemType itemType, UtilityQuantity marketUtility, UtilityQuantity ourUtility)
    {
      this.itemType = itemType;
      this.utility = marketUtility;
      this.ourUtility = ourUtility;
      // Calculate the percentage difference between the market price and our utility.
      // Flip the sign so that the percentage is positive.
      this.percentage = -(marketUtility.marginalUtility - ourUtility.marginalUtility) / ourUtility.marginalUtility;
    }

    // Sort by highest percentage first, then most negative individual utility,
    // then highest total quantity, and finally by item type.
    public int CompareTo(PurchasePriority? other)
    {
      if (other == null) return 1;
      int result = other.percentage.CompareTo(percentage);
      if (result != 0) return result;
      result = utility.marginalUtility.CompareTo(other.utility.marginalUtility);
      if (result != 0) return result;
      result = other.utility.totalQuantity.CompareTo(utility.totalQuantity);
      if (result != 0) return result;
      return itemType.itemType.CompareTo(other.itemType.itemType);
    } 
  }

  public void MakePurchases()
  {
    if (market == null)
    {
      return;
    }
    // We are willing to purchase any item that is for sale for less than it's utility value.
    // We'll prioritize items that are most underpriced on a percentage basis.
    
    // Get our budget.
    double budget = inventory.Count(ItemType.Coin);

    if (budget <= 0) return;

    // Store things we are considering buying sorted by priority.
    List<PurchasePriority> purchases = new List<PurchasePriority>();

    foreach (var ask in market.Asks)
    {
      UtilityQuantity marketUtility = ask.Value.bestPrice;
      // Get our utility for this item.
      UtilityQuantity? ourUtility = MarginalQuantity(ask.Key, true);
      // Ignore anything that's not worth buying.
      // Asks are negative numbers, so we want to buy things that are less negative.
      if (ourUtility == null || ourUtility.marginalUtility == 0 || ourUtility.marginalUtility > marketUtility.marginalUtility) continue;
      // Ignore anything out of our budget.
      if (-marketUtility.marginalUtility > budget) continue;
      // Add this item to our list of potential purchases.
      purchases.Add(new PurchasePriority(ask.Key, marketUtility, ourUtility));
    }

    // Sort the list of potential purchases by priority.
    purchases.Sort();

    // Buy the items in order of priority, until we run out of budget.
    foreach (var purchase in purchases)
    {
      UtilityQuantity ourUtility = purchase.ourUtility;
      bool purchaseMore = true;
      while(purchaseMore)
      {
        purchaseMore = false;
        // Get the counterparty for this ask.
        IInventoryContext? seller = market.AskCounterparty(purchase.itemType, out UtilityQuantity? marketUtility);
        if (seller == null || marketUtility == null || seller == this || -marketUtility.marginalUtility > budget) break;
        // The marketUtility might have changed since we calculated our utility, so make sure we're
        // still willing to buy it. Note that we don't attempt to re-sort the priorities.
        if (ourUtility.marginalUtility > marketUtility.marginalUtility) break;
        // Buy as much as we can afford.
        int quantity = (int)Math.Floor(budget / -marketUtility.marginalUtility);
        quantity = Math.Min(quantity, ourUtility.marginalQuantity);
        quantity = Math.Min(quantity, marketUtility.totalQuantity);
        if (quantity <= 0) break;
        int purchaseCost = (int)Math.Ceiling(-marketUtility.marginalUtility * quantity);
        // Ensure that rounding didn't cause the purchaseCost to exceed our utility.
        if (purchaseCost > ourUtility.marginalUtility * quantity) break;
        // Make the trade offer
        if(!IInventoryContext.ProposePurchase(this, seller, purchase.itemType, quantity, purchaseCost))
        {
          // For some reason the trade offer was rejected. We could inform the market and
          // try again, but for now we'll just give up.
          break;
        }
        
        // If the trade offer was accepted, update our budget.
        budget -= purchaseCost;
        // Inform the market of the trade so that prices can be updated.
        market.CollectNewAsks(purchase.itemType);
        // Update our utility.
        ourUtility.marginalQuantity -= quantity;
        
        // Purchase more from another seller if we are unsatiated.
        if (ourUtility.marginalQuantity > 0) purchaseMore = true;
      }
      
    }

  }

  public void SubmitBidPrices()
  {
    // TODO
  }

  public double GetOffer(IDictionary<Item, int> items, IInventoryContext seller)
  {
    // Get the utility of having the items.
    double offer = 0;
    foreach (KeyValuePair<Item, int> item in items)
    {
      offer += Utility(item.Key.itemType, item.Value);
    }
    return offer;
  }

  public double GetPrice(IDictionary<Item, int> items, IInventoryContext buyer)
  {
    // Get the utility of removing the items.
    double offer = 0;
    foreach (KeyValuePair<Item, int> item in items)
    {
      // Price will be negative.
      offer += Utility(item.Key.itemType, -item.Value);
    }
    return offer;
  }

  private void UpdateAbilities(IAbilityProvider? addedProvider, IEnumerable<AbilityType>? added, IAbilityProvider? removedProvider, IEnumerable<AbilityType>? removed)
  {
    IAbilityCollection.UpdateAbilities(ref _abilityProviders, ref _abilities, addedProvider, added, removedProvider, removed, AbilitiesChanged);
  }
}
