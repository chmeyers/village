// A MarketMaker is a special IInventoryContext that can participate in a Market,
// and is always willing to trade based on the price list and their desires.
// They are allowed to cheat by refreshing their inventory to a known state.
using Village.Base;

namespace Village.Items;

public class MarketMaker : IMarketParticipant
{
  public Inventory inventory { get; protected set; } = new Inventory();

  private IPriceList _priceList;
  private Market _market;
  private long _refreshRate;
  private long _lastRefresh = 0;

  private Dictionary<ItemType, int> desiredItems = new Dictionary<ItemType, int>();
  private Dictionary<ItemType, int> haveItems = new Dictionary<ItemType, int>();

  public MarketMaker(IPriceList priceList, Market market, long refreshRate = Calendar.ticksPerWeek)
  {
    this._priceList = priceList;
    this._market = market;
    this._refreshRate = refreshRate;
  }

  public void SetDesire(ItemType itemType, int amount)
  {
    if (amount == 0)
    {
      desiredItems.Remove(itemType);
    }
    else
    {
      desiredItems[itemType] = amount;
    }
  }

  public void SetHave(ItemType itemType, int amount)
  {
    if (amount == 0)
    {
      haveItems.Remove(itemType);
    }
    else
    {
      haveItems[itemType] = amount;
    }
  }

  public void Advance()
  {
    if (Calendar.Ticks - _lastRefresh > _refreshRate)
    {
      Refresh();
    }
  }

  public void Refresh()
  {
    _lastRefresh = Calendar.Ticks;
    inventory.Clear();
    foreach (var itemType in haveItems.Keys)
    {
      inventory.AddItem(new Item(itemType), haveItems[itemType]);
    }
  }

  public double Utility(ItemType itemType, int quantity)
  {
    int have = inventory.Count(itemType);
    if (quantity > 0)
    {
      quantity = Math.Min(quantity, desiredItems[itemType] - have);
      UtilityQuantityList bid = _priceList.BidPrice(itemType);
      if (bid.Count == 0)
      {
        return 0;
      }
      quantity = Math.Min(quantity, bid[0].totalQuantity);
      return quantity * bid[0].marginalUtility;
    }
    else
    {
      if (have - desiredItems[itemType] + quantity < 0)
      {
        return double.MinValue;
      }
      quantity = Math.Min(-quantity, have - desiredItems[itemType]);
      UtilityQuantityList ask = _priceList.AskPrice(itemType);
      if (ask.Count == 0)
      {
        return 0;
      }
      quantity = Math.Min(quantity, ask[0].totalQuantity);
      return quantity * ask[0].marginalUtility;
    }
  }

  public double GetOffer(IDictionary<Item, int> items, IInventoryContext seller)
  {
    double offer = 0;
    foreach (KeyValuePair<Item, int> item in items)
    {
      offer += Utility(item.Key.itemType, item.Value);
    }
    return offer;
  }

  public double GetPrice(IDictionary<Item, int> items, IInventoryContext buyer)
  {
    double offer = 0;
    foreach (KeyValuePair<Item, int> item in items)
    {
      // Price will be negative.
      offer += Utility(item.Key.itemType, -item.Value);
    }
    return offer;
  }

  private void SubmitBid(ItemType itemType, int desired)
  {
    int have = inventory.Count(itemType);
    if (have >= desired) return;
    UtilityQuantityList bid = _priceList.BidPrice(itemType).Clone();
    if (bid.Count == 0) return;
    bid[0].totalQuantity = bid[0].marginalQuantity = desired - have;
    _market.AddBid(itemType, this, bid);
  }

  public void SubmitBid(ItemType itemType)
  {
    if (!desiredItems.ContainsKey(itemType)) return;
    SubmitBid(itemType, desiredItems[itemType]);
  }

  public void SubmitBidPrices()
  {
    foreach (var itemType in desiredItems)
    {
      SubmitBid(itemType.Key, itemType.Value);
    }
  }

  public void MakePurchases()
  {
    int budget = inventory.Count(ItemType.Coin);

    if (budget <= 0) return;

    PurchaseList purchases = new PurchaseList();

    foreach (var ask in _market.Asks)
    {
      var have = inventory.Count(ask.Key);
      var desired = desiredItems.ContainsKey(ask.Key) ? desiredItems[ask.Key] : 0;
      if ( have >= desired) continue;
      UtilityQuantityList bid = _priceList.BidPrice(ask.Key);
      if (bid.Count == 0) continue;
      UtilityQuantity ourPrice = bid[0].Clone();
      ourPrice.totalQuantity = ourPrice.marginalQuantity = desired - have;
      UtilityQuantity marketUtility = ask.Value.bestPrice;
      if (ourPrice.marginalUtility <= 0 || ourPrice.marginalUtility < marketUtility.marginalUtility) continue;
      purchases.Add(new PurchasePriority(ask.Key, ourPrice, marketUtility));
    }

    purchases.MakePurchases(this, _market, budget);
  }

  public void SubmitAsk(ItemType itemType)
  {
    var have = inventory.Count(itemType);
    var desired = desiredItems.ContainsKey(itemType) ? desiredItems[itemType] : 0;
    if (have == 0 || have < desired) return;
    var price = Utility(itemType, -1);
    if (price == 0 || price == double.MinValue) return;
    UtilityQuantity ask = new UtilityQuantity(have - desired, have - desired, price);
    UtilityQuantityList priceList = new UtilityQuantityList();
    priceList.Add(ask);
    _market.AddAsk(itemType, this, priceList);
  }

  public void SubmitAskPrices()
  {
    // We are willing to sell every item in our inventory for it's utility value.
    foreach (var itemType in inventory.items)
    {
      SubmitAsk(itemType.Key);
    }
  }
}
