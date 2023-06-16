

using System.Collections.Concurrent;
using Village.Households;

namespace Village.Items;

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
  public IInventoryContext? AskCounterparty(ItemType itemType, out UtilityQuantity? ask)
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
  public IInventoryContext? BidCounterparty(ItemType itemType, out UtilityQuantity? bid)
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
  private MarketCacheValue AddCacheValue(UtilityQuantityList input, IInventoryContext participant)
  {
    return new MarketCacheValue(input, participant, input[0]);
  }

  // Update delegate for ConcurrentDictionary.AddOrUpdate
  private MarketCacheValue UpdateCacheValue(MarketCacheValue oldValue, UtilityQuantityList input, IInventoryContext participant)
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

  public void AddAsk(ItemType itemType, IInventoryContext participant, UtilityQuantityList ask)
  {
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

  public void AddBid(ItemType itemType, IInventoryContext participant, UtilityQuantityList bid)
  {
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
    HashSet<IInventoryContext> participants = removed.participants;
    // Ask all the participants to re-submit their bids.
    foreach (IInventoryContext participant in participants)
    {
      // TODO
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
    HashSet<IInventoryContext> participants = removed.participants;
    // Ask all the participants to re-submit their asks.
    foreach (IInventoryContext participant in participants)
    {
      // TODO
    }
  }

  public class MarketCacheValue
  {
    public UtilityQuantityList orderBook;
    public IInventoryContext bestCounterparty;
    public UtilityQuantity bestPrice;
    public HashSet<IInventoryContext> participants = new HashSet<IInventoryContext>();
    public MarketCacheValue(UtilityQuantityList orderBook, IInventoryContext bestCounterparty, UtilityQuantity bestPrice)
    {
      this.orderBook = orderBook.Clone();
      this.bestCounterparty = bestCounterparty;
      this.bestPrice = bestPrice.Clone();
      participants.Add(bestCounterparty);
    }
  }

  public IReadOnlyDictionary<ItemType, MarketCacheValue> Asks => _askCache;

  // Concurrent Dictionaries for thread safety.
  // There will be contention as all the households try to update the market at the same time.
  private ConcurrentDictionary<ItemType, MarketCacheValue> _askCache = new ConcurrentDictionary<ItemType, MarketCacheValue>();

  private ConcurrentDictionary<ItemType, MarketCacheValue> _bidCache = new ConcurrentDictionary<ItemType, MarketCacheValue>();
}