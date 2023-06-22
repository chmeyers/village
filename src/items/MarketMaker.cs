// A MarketMaker is a special IInventoryContext that can participate in a Market,
// and is always willing to trade based on the price list and their desires.
// They are allowed to cheat by refreshing their inventory to a known state.
using Village.Base;

namespace Village.Items;

public class MarketMaker : IMarketParticipant
{
  // Registry of all MarketMakers.
  public static HashSet<MarketMaker> global_marketMakers = new HashSet<MarketMaker>();
  public Inventory inventory { get; protected set; } = new Inventory();

  private IPriceList _priceList;
  private Market _market;
  private long _refreshRate;
  private long _lastRefresh = 0;

  private Dictionary<ItemType, int> maxItems = new Dictionary<ItemType, int>();
  private Dictionary<ItemType, int> haveItems = new Dictionary<ItemType, int>();

  public MarketMaker(IPriceList priceList, Market market, long refreshRate = Calendar.ticksPerWeek)
  {
    this._priceList = priceList;
    this._market = market;
    this._refreshRate = refreshRate;
    global_marketMakers.Add(this);
  }

  public void SetDefaults()
  {
    // By default we have 1M coins, 1000 of each field crop, and 10 of everything else.
    foreach (var itemtype in ItemType.itemTypes)
    {
      SetMax(itemtype.Value, 20);
      SetHave(itemtype.Value, 10);
    }
    foreach (var crop in ItemType.fieldCrops)
    {
      SetMax(crop, 2000);
      SetHave(crop, 1000);
    }
    SetHave(ItemType.Coin, 1000000);
  }

  public void SetMax(ItemType itemType, int amount)
  {
    if (amount == 0)
    {
      maxItems.Remove(itemType);
    }
    else
    {
      maxItems[itemType] = amount;
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
    if (itemType == ItemType.Coin)
    {
      return quantity;
    }
    int have = inventory.Count(itemType);
    int max = maxItems.ContainsKey(itemType) ? maxItems[itemType] : 0;
    if (quantity > 0)
    {
      if (have >= max)
      {
        return 0;
      }
      quantity = Math.Min(quantity, max - have);
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
      if (-quantity > have)
      {
        return 0;
      }
      UtilityQuantityList ask = _priceList.AskPrice(itemType);
      if (ask.Count == 0)
      {
        return 0;
      }
      quantity = Math.Min(-quantity, ask[0].totalQuantity);
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

  private void SubmitBid(ItemType itemType, int max, ref int budget)
  {
    int have = inventory.Count(itemType);
    if (have >= max) return;
    UtilityQuantityList bid = _priceList.BidPrice(itemType).Clone();
    if (bid.Count == 0) return;
    bid[0].marginalQuantity = max - have;
    bid[0].marginalQuantity = Math.Min(bid[0].marginalQuantity, (int)Math.Floor(budget / bid[0].marginalUtility));
    if (bid[0].marginalQuantity <= 0) return;
    bid[0].totalQuantity = bid[0].marginalQuantity;
    _market.AddBid(itemType, this, bid);
  }

  public void SubmitBid(ItemType itemType)
  {
    if (!maxItems.ContainsKey(itemType)) return;
    int budget = inventory.Count(ItemType.Coin);
    if (!maxItems.ContainsKey(itemType)) return;
    SubmitBid(itemType, maxItems[itemType], ref budget);
  }

  public void SubmitBidPrices()
  {
    int budget = inventory.Count(ItemType.Coin);
    foreach (var itemType in maxItems)
    {
      SubmitBid(itemType.Key, itemType.Value, ref budget);
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
      var max = maxItems.ContainsKey(ask.Key) ? maxItems[ask.Key] : 0;
      if (have >= max) continue;
      UtilityQuantityList bid = _priceList.BidPrice(ask.Key);
      if (bid.Count == 0) continue;
      UtilityQuantity ourPrice = bid[0].Clone();
      ourPrice.totalQuantity = ourPrice.marginalQuantity = max - have;
      UtilityQuantity marketUtility = ask.Value.bestPrice;
      if (ourPrice.marginalUtility <= 0 || ourPrice.marginalUtility < -marketUtility.marginalUtility) continue;
      purchases.Add(new PurchasePriority(ask.Key, marketUtility, ourPrice));
    }

    purchases.MakePurchases(this, _market, ref budget);
  }

  public void SubmitAsk(ItemType itemType)
  {
    var have = inventory.Count(itemType);
    if (have == 0) return;
    var price = Utility(itemType, -1);
    if (price == 0) return;
    UtilityQuantity ask = new UtilityQuantity(have, have, price);
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
