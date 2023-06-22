

using System.Collections.Concurrent;
using Village.Households;

namespace Village.Items;

public interface IMarketParticipant : IInventoryContext
{
  // Callbacks for the market to request a new bid or ask.
  public void SubmitBid(ItemType itemType);
  public void SubmitAsk(ItemType itemType);
}

public class Market : IPriceList
{
  public static HashSet<Market> global_markets = new HashSet<Market>();

  // Constructor.
  public Market()
  {
    global_markets.Add(this);
  }

  public UtilityQuantityList AskPrice(ItemType itemType)
  {
    if (!_askCache.TryGetValue(itemType, out var asks))
    {
      return new UtilityQuantityList();
    }
    
    return asks.orderBook.Clone();
  }
  public UtilityQuantityList AskPrice(Item item) => AskPrice(item.itemType);


  public UtilityQuantityList BidPrice(ItemType itemType)
  {
    if (!_bidCache.TryGetValue(itemType, out var bids))
    {
      return new UtilityQuantityList();
    }
    return bids.orderBook.Clone();
  }
  public UtilityQuantityList BidPrice(Item item) => BidPrice(item.itemType);

  // Given an item, return the counterparty that is willing to sell the item for the lowest price.
  public IMarketParticipant? AskCounterparty(ItemType itemType, out UtilityQuantity? ask)
  {
    if (!_askCache.TryGetValue(itemType, out var asks))
    {
      ask = null;
      return null;
    }
    ask = asks.bestPrice;
    return asks.bestCounterparty;
  }

  // Given an item, return the counterparty that is willing to buy the item for the highest price.
  // Is the counterparty guaranteed to have enough money to buy the item?
  public IMarketParticipant? BidCounterparty(ItemType itemType, out UtilityQuantity? bid)
  {
    if (!_bidCache.TryGetValue(itemType, out var bids))
    {
      bid = null;
      return null;
    }
    bid = bids.bestPrice;
    return bids.bestCounterparty;
  }
  
  // Add delegate for ConcurrentDictionary.AddOrUpdate
  private MarketCacheValue AddCacheValue(UtilityQuantityList input, IMarketParticipant participant)
  {
    return new MarketCacheValue(input, participant, input[0]);
  }

  // Update delegate for ConcurrentDictionary.AddOrUpdate
  private MarketCacheValue UpdateCacheValue(MarketCacheValue oldValue, UtilityQuantityList input, IMarketParticipant participant)
  {
    if (input[0] < oldValue.bestPrice)
    {
      // The new one is better than the existing best.
      oldValue.bestCounterparty = participant;
      oldValue.bestPrice = input[0].Clone();
    }
    // Merge the new one into the existing order book.
    oldValue.orderBook.Merge(input);
    oldValue.participants.Add(participant);
    return oldValue;
  }

  public void AddAsk(ItemType itemType, IMarketParticipant participant, UtilityQuantityList ask)
  {
    if (itemType == ItemType.Coin)
    {
      return;
    }
    // If the ask is empty, ignore.
    if (ask.Count == 0)
    {
      return;
    }
    // Throw if they tried to ask minValue or for an infinite amount.
    if (ask[ask.Count - 1].totalQuantity == int.MaxValue || ask[ask.Count - 1].marginalUtility == double.MinValue)
    {
      throw new Exception($"Invalid market ask: {ask}");
    }
    // They need to have the item they are trying to sell.
    if (!participant.inventory.Contains(itemType, ask[0].totalQuantity))
    {
      throw new Exception($"Market participant {participant} does not have enough {itemType} to make ask {ask}");
    }

    _askCache.AddOrUpdate(itemType, (k) => AddCacheValue(ask, participant), (k, v) => UpdateCacheValue(v, ask, participant)); 

  }

  public void AddBid(ItemType itemType, IMarketParticipant participant, UtilityQuantityList bid)
  {
    if (itemType == ItemType.Coin)
    {
      return;
    }
    // If the bid is empty, ignore it.
    if (bid.Count == 0)
    {
      return;
    }
    // Throw if they tried to bid zero, or for an infinite amount.
    if (bid[bid.Count - 1].totalQuantity == int.MaxValue || bid[bid.Count - 1].marginalUtility == 0)
    {
      throw new Exception($"Invalid market bid: {bid}");
    }

    // They are required to have enough coin to cover the highest bid they are making, but
    // not the full list.
    int neededCoin = (int)Math.Ceiling(bid[0].marginalUtility * bid[0].marginalQuantity);
    if (!participant.inventory.Contains(ItemType.Coin, neededCoin))
    {
      throw new Exception($"Market participant {participant} does not have enough coin to make bid {bid}");
    }

    _bidCache.AddOrUpdate(itemType, (k) => AddCacheValue(bid, participant), (k, v) => UpdateCacheValue(v, bid, participant));
  }

  public void ClearBids(ItemType itemType)
  {
    _bidCache.Remove(itemType, out var ignored);
  }

  public void ClearBids()
  {
    _bidCache.Clear();
  }

  public void ClearAsks(ItemType itemType)
  {
    _askCache.Remove(itemType, out var ignored);
  }

  public void ClearAsks()
  {
    _askCache.Clear();
  }

  public void Clear()
  {
    _bidCache.Clear();
    _askCache.Clear();
  }

  public void CollectNewBids(ItemType itemType)
  {
    
    _bidCache.Remove(itemType, out MarketCacheValue? removed);
    if (removed == null)
    {
      // No existing participants.
      return;
    }
    HashSet<IMarketParticipant> participants = removed.participants;
    // Ask all the participants to re-submit their bids.
    foreach (IMarketParticipant participant in participants)
    {
      participant.SubmitBid(itemType);
    }
  }

  public void CollectNewAsks(ItemType itemType)
  {
    _askCache.Remove(itemType, out MarketCacheValue? removed);
    if (removed == null)
    {
      // No existing participants.
      return;
    }
    HashSet<IMarketParticipant> participants = removed.participants;
    // Ask all the participants to re-submit their asks.
    foreach (IMarketParticipant participant in participants)
    {
      participant.SubmitAsk(itemType);
    }
  }

  public void ReportSale(ItemType itemType, int quantity, int price)
  {
    CollectNewAsks(itemType);
    if (quantity == 0) return;
    // Note: We don't care about overflows here. The sales info is just for debugging.
    if (!totalSales.ContainsKey(itemType))
    {
      totalSales[itemType] = quantity;
      lastPrice[itemType] = (double)price/quantity;
    }
    else
    {
      totalSales[itemType] += quantity;
      lastPrice[itemType] = (double)price/quantity;
    }
  }

  public class MarketCacheValue
  {
    public UtilityQuantityList orderBook;
    public IMarketParticipant bestCounterparty;
    public UtilityQuantity bestPrice;
    public HashSet<IMarketParticipant> participants = new HashSet<IMarketParticipant>();
    public MarketCacheValue(UtilityQuantityList orderBook, IMarketParticipant bestCounterparty, UtilityQuantity bestPrice)
    {
      this.orderBook = orderBook.Clone();
      this.bestCounterparty = bestCounterparty;
      this.bestPrice = bestPrice.Clone();
      participants.Add(bestCounterparty);
    }

    public override string ToString()
    {
      return $"{bestPrice}, {bestCounterparty}, {orderBook}";
    }
  }

  public IReadOnlyDictionary<ItemType, MarketCacheValue> Asks => _askCache;

  // Concurrent Dictionaries for thread safety.
  // There will be contention as all the households try to update the market at the same time.
  private ConcurrentDictionary<ItemType, MarketCacheValue> _askCache = new ConcurrentDictionary<ItemType, MarketCacheValue>();

  private ConcurrentDictionary<ItemType, MarketCacheValue> _bidCache = new ConcurrentDictionary<ItemType, MarketCacheValue>();

  public ConcurrentDictionary<ItemType, long> totalSales = new ConcurrentDictionary<ItemType, long>();
  public ConcurrentDictionary<ItemType, double> lastPrice = new ConcurrentDictionary<ItemType, double>();
}

public class PurchasePriority : IComparable<PurchasePriority>
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
    // Add the market utility as it is negative.
    this.percentage = (ourUtility.marginalUtility + marketUtility.marginalUtility) / ourUtility.marginalUtility;
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

public class PurchaseList : List<PurchasePriority>
{
  public PurchaseList() : base() { }

  public void FilterByPercentage(double percentage)
  {
    for (int i = Count - 1; i >= 0; i--)
    {
      if (this[i].percentage < percentage)
      {
        RemoveAt(i);
      }
    }
  }

  public void MakePurchases(IMarketParticipant buyer, Market market, int budget)
  {
    this.Sort();

    // Buy the items in order of priority, until we run out of budget.
    foreach (var purchase in this)
    {
      UtilityQuantity ourUtility = purchase.ourUtility;
      bool purchaseMore = true;
      while (purchaseMore)
      {
        purchaseMore = false;
        // Get the counterparty for this ask.
        IMarketParticipant? seller = market.AskCounterparty(purchase.itemType, out UtilityQuantity? marketUtility);
        if (seller == null || marketUtility == null || seller == buyer || -marketUtility.marginalUtility > budget) break;
        // The marketUtility might have changed since we calculated our utility, so make sure we're
        // still willing to buy it. Note that we don't attempt to re-sort the priorities.
        if (ourUtility.marginalUtility < -marketUtility.marginalUtility) break;
        // Buy as much as we can afford.
        int quantity = (int)Math.Floor(budget / -marketUtility.marginalUtility);
        quantity = Math.Min(quantity, ourUtility.marginalQuantity);
        quantity = Math.Min(quantity, marketUtility.totalQuantity);
        if (quantity <= 0) break;
        int purchaseCost = (int)Math.Ceiling(-marketUtility.marginalUtility * quantity);
        // Ensure that rounding didn't cause the purchaseCost to exceed our utility.
        if (purchaseCost > ourUtility.marginalUtility * quantity) break;
        // Make the trade offer
        if (!IMarketParticipant.ProposePurchase(buyer, seller, purchase.itemType, quantity, purchaseCost))
        {
          // For some reason the trade offer was rejected. We could inform the market and
          // try again, but for now we'll just give up.
          break;
        }

        // If the trade offer was accepted, update our budget.
        budget -= purchaseCost;
        // Inform the market of the trade so that prices can be updated.
        market.ReportSale(purchase.itemType, quantity, purchaseCost);
        // Update our utility.
        ourUtility.marginalQuantity -= quantity;

        // Purchase more from another seller if we are unsatiated.
        if (ourUtility.marginalQuantity > 0) purchaseMore = true;
      }

    }
  }
}