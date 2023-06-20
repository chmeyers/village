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

public class Household : IMarketParticipant, IHouseholdContext, IAbilityCollection
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
      UtilityQuantityList allValues = desiredStockpile.Clone().Merge(valueList);

      double utility = 0;
      for (int i = allValues.Count - 1; i >= 0; i--)
      {
        int intervalMin = i > 0 ? allValues[i - 1].totalQuantity : 0;
        if (intervalMin >= have) continue;
        int excess = have - intervalMin;
        int undesired_amount = Math.Min(-delta, excess);
        delta += undesired_amount;
        utility += allValues[i].marginalUtility * -undesired_amount;  // +/- epsilon?
        if (delta == 0)
        {
          return utility;
        }
        have = intervalMin;
      }
      int from_inventory = Math.Min(-delta, have);
      int need_to_buy = -delta - from_inventory;
      double price = allValues.GetFirstUtility() ?? 0;
      utility += price * -from_inventory;  // +/- epsilon?
      // An null costPrice means we'll use a zero, an empty one means min value.
      double aquirePrice = costPrice == null ? 0 : costPrice.GetUtility(need_to_buy) ?? double.MinValue;
      utility += Math.Min(price * -need_to_buy, aquirePrice);  // +/- epsilon?
      return utility;
    }
    else if (delta > 0)
    {
      UtilityQuantityList allValues = desiredStockpile.Clone().Merge(valueList);
      // Adding items.
      double utility = 0;
      for (int i = 0; i < allValues.Count; i++)
      {
        int intervalMax = allValues[i].totalQuantity;
        if (intervalMax <= have) continue;
        int needed = intervalMax - have;
        int desired_amount = Math.Min(delta, needed);
        delta -= desired_amount;
        utility += Math.Max(allValues[i].marginalUtility, 0) * desired_amount;
        if (delta == 0)
        {
          return utility;
        }
        have = intervalMax;
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
      if (adding)
      {
        // Check whether we're in this interval, note that if we have exactly the total quantity,
        // we're in the next interval for adding, but the current interval for removing.
        if (have < allValues[i].totalQuantity)
        {
        int toNextInterval = allValues[i].totalQuantity - have;
        return new UtilityQuantity(toNextInterval, toNextInterval, allValues[i].marginalUtility);
        }
      }
      else
      {
        if (have <= allValues[i].totalQuantity)
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
    if (quantity == null && !adding)
    {
      return quantity;
    }
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
    // If we are adding an item with a useful ability, the first unit gets the utility of
    // the ability, which will make that the marginal unit.
    if (adding)
    {
      double abilityUtility = _AbilityUtility(itemType);
      if (abilityUtility > 0)
      {
        if (quantity == null)
        {
          return new UtilityQuantity(1, 1, abilityUtility);
        }
        quantity.marginalUtility += abilityUtility;
        quantity.totalQuantity = 1;
        quantity.marginalQuantity = 1;
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
    IPriceList priceList = market == null ? ConfigPriceList.Default : market;
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

  public UtilityQuantityList ValuePriceExceptAvailable(ItemType itemType, int have)
  {
    IPriceList priceList = market == null ? ConfigPriceList.Default : market;
    UtilityQuantityList bestValue = priceList.BidPrice(itemType);
    // See if any people in the household can beat the market price.
    // The BidPrice above probably includes this household's market offers,
    // but that's fine, as those offers are always epsilon worse than
    // what WorthAsInput will return, so Merge will remove them.
    foreach (var person in people)
    {
      // When removing items, we don't consider the values if we have that total quantity,
      // as that is the expected use of the item. Instead we compare against the next best value.
      // We DO consider all the desired stockpile values, as the expected use of the stockpile
      // is to keep the item.
      // TODO(chmeyers): We should only remove them if the associated task is available to run.
      bestValue.MergeExceptQuantity(person.WorthAsInput(itemType), have);
    }
    return bestValue;
  }

  public void InvalidateWorthCaches(ItemType itemType)
  {
    foreach (var person in people)
    {
      person.InvalidateWorthCache(itemType);
    }
  }

  public void ReevaluateCropWorth()
  {
    foreach (var crop in ItemType.fieldCrops)
    {
      InvalidateWorthCaches(crop);
    }
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
    IPriceList priceList = market == null ? ConfigPriceList.Default : market;
    UtilityQuantityList bestPrice = priceList.AskPrice(itemType);
    // The market price does us no good if we don't have the coin to buy it.
    int budget = inventory.Count(ItemType.Coin);
    bestPrice.FilterByBudget(budget);

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

  private double _AbilityUtility(ItemType itemType)
  {
    if (itemType.abilities.Count == 0)
    {
      return 0;
    }
    double utility = 0;
    foreach (var person in people)
    {
      utility = Math.Max(person.AbilityUtility(itemType.abilities), utility);
    }
    // TODO(chmeyers): Base this on the actual quality of the item.
    return utility * itemType.craftQuality.GetBaseValue();
  }

  private double _Utility(ItemType itemType, int quantity, int days, UtilityQuantityList? cost, UtilityQuantityList? childStockpile)
  {
    UtilityQuantityList stockpile = UtilityQuantityList.Stack(DesiredStockpile(itemType, days), childStockpile);
    int have = inventory.Count(itemType);
    UtilityQuantityList valuePrice = quantity >=0 ? ValuePrice(itemType) : ValuePriceExceptAvailable(itemType, have);
    double utility = _MarginalUtility(stockpile, have, quantity, valuePrice, cost);
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
    int have = inventory.Count(itemType);
    return _Utility(itemType, quantity, 0, (quantity < 0 ? CostPrice(itemType) : null), null) + ( quantity > 0 ?_AbilityUtility(itemType) : 0);
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


  public void SubmitAsk(ItemType itemType)
  {
    // TODO(chmeyers): Deal with sales of degraded items.
    if (market == null) return;
    var quantity = MarginalQuantity(itemType, false);
    if (quantity == null || quantity.marginalUtility == 0 || quantity.marginalUtility == double.MinValue || quantity.totalQuantity == int.MaxValue || quantity.totalQuantity == 0) return;
    UtilityQuantityList price = new UtilityQuantityList();
    price.Add(quantity);
    market.AddAsk(itemType, this, price);
  }

  // Submit ask prices to the market.
  public void SubmitAskPrices()
  {
    if (market == null) return;
    // We are willing to sell every item in our inventory for it's utility value plus a profit.
    foreach (var itemType in inventory.items)
    {
      SubmitAsk(itemType.Key);
    }
  }

  // Items that cost more than this per item will be considered high value purchases.
  public const double highValuePurchaseThreshold = 50000;
  // Items that aren't at least this much as good as the best item we want to buy will be ignored.
  public const double minRelativeValueToPurchase = 0.8;
  public void MakePurchases()
  {
    if (market == null)
    {
      return;
    }
    // We are willing to purchase any item that is for sale for less than it's utility value.
    // We'll prioritize items that are most underpriced on a percentage basis.
    
    // Get our budget.
    int budget = inventory.Count(ItemType.Coin);

    if (budget <= 0) return;

    // Store things we are considering buying sorted by priority.
    PurchaseList purchases = new PurchaseList();

    double bestItemPercentage = 0;

    foreach (var ask in market.Asks)
    {
      UtilityQuantity marketUtility = ask.Value.bestPrice;
      // Get our utility for this item.
      UtilityQuantity? ourUtility = MarginalQuantity(ask.Key, true);
      // Ignore anything that's not worth buying.
      // Asks are negative numbers and ourUtility is positive.
      if (ourUtility == null || ourUtility.marginalUtility == 0 || ourUtility.marginalUtility < -marketUtility.marginalUtility) continue;
      if (marketUtility.marginalUtility < highValuePurchaseThreshold)
      {
        // Keep track of the best low value item we can buy, even if they are out of our budget.
        bestItemPercentage = Math.Max(bestItemPercentage, (ourUtility.marginalUtility + marketUtility.marginalUtility) / ourUtility.marginalUtility);
      }
      // Ignore anything out of our budget.
      if (-marketUtility.marginalUtility > budget) continue;
      // Add this item to our list of potential purchases.
      purchases.Add(new PurchasePriority(ask.Key, marketUtility, ourUtility));
    }

    // Filter out the the lower profitability items so we can save up for the best ones.
    purchases.FilterByPercentage(bestItemPercentage * minRelativeValueToPurchase);
    
    purchases.MakePurchases(this, market, budget);

  }

  private void SubmitBid(ItemType itemType, ref int budget)
  {
    var quantity = MarginalQuantity(itemType, true);
    if (quantity == null || quantity.marginalUtility == 0 || quantity.marginalUtility == 0 || quantity.totalQuantity == 0) return;
    // Cap the quantity at our budget.
    quantity.marginalQuantity = Math.Min(quantity.marginalQuantity, (int)Math.Floor(budget / quantity.marginalUtility));
    if (quantity.marginalQuantity <= 0) return;
    quantity.totalQuantity = quantity.marginalQuantity;
    UtilityQuantityList price = new UtilityQuantityList();
    price.Add(quantity);
    // Note that we don't actually check to see if the bid is less than the current ask.
    // It shouldn't be unless we were unable to purchase the item for some reason.
    market!.AddBid(itemType, this, price);
  }

  public void SubmitBid(ItemType itemType)
  {
    if (market == null) return;
    int budget = inventory.Count(ItemType.Coin);
    if (budget <= 0) return;
    SubmitBid(itemType, ref budget);
  }

  public void SubmitBidPrices()
  {
    if (market == null) return;
    int budget = inventory.Count(ItemType.Coin);
    if (budget <= 0) return;
    // Determine what items we will place bids for.
    // This will be the union of items we want a stockpile of, our people's
    // worth caches, and all items for sale.
    HashSet<ItemType> bidItems = new HashSet<ItemType>();
    foreach (var item in _CachedDesiredStockpile)
    {
      if (item.Value.Value.Count > 0) bidItems.Add(item.Key);
    }
    foreach (var person in people)
    {
      bidItems.UnionWith(person.GetDesiredItems());
    }
    bidItems.UnionWith(market.Asks.Keys);
    foreach (var itemType in bidItems)
    {
      SubmitBid(itemType, ref budget);
    }
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
