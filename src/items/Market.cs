

using Village.Households;

namespace Village.Items;

public class Market : IPriceList
{
  public UtilityQuantityList AskPrice(ItemType itemType)
  {
    lock (_askCache)
    {
      if (!_askCache.TryGetValue(itemType, out var asks))
      {
        return new UtilityQuantityList();
      }
      return asks.orderBook;
    }
  }
  public UtilityQuantityList AskPrice(Item item) => AskPrice(item.itemType);


  public UtilityQuantityList BidPrice(ItemType itemType)
  {
    lock (_bidCache)
    {
      if (!_bidCache.TryGetValue(itemType, out var bids))
      {
        return new UtilityQuantityList();
      }
      return bids.orderBook;
    }
  }
  public UtilityQuantityList BidPrice(Item item) => BidPrice(item.itemType);

  // Given an item, return the counterparty that is willing to sell the item for the lowest price.
  public IHouseholdContext? AskCounterparty(ItemType itemType, out UtilityQuantity? ask)
  {
    lock (_askCache)
    {
      if (!_askCache.TryGetValue(itemType, out var asks))
      {
        ask = null;
        return null;
      }
      ask = asks.bestPrice;
      return asks.bestCounterparty;
    }
  }

  // Given an item, return the counterparty that is willing to buy the item for the highest price.
  // Is the counterparty guaranteed to have enough money to buy the item?
  public IHouseholdContext? BidCounterparty(ItemType itemType, out UtilityQuantity? bid)
  {
    lock (_bidCache)
    {
      if (!_bidCache.TryGetValue(itemType, out var bids))
      {
        bid = null;
        return null;
      }
      bid = bids.bestPrice;
      return bids.bestCounterparty;
    }
  }

  public void AddAsk(ItemType itemType, IHouseholdContext household, UtilityQuantityList ask)
  {
    lock (_askCache)
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
      if (!household.household.inventory.Contains(itemType, ask[0].totalQuantity))
      {
        throw new Exception($"Household {household.household} does not have enough {itemType} to make ask {ask}");
      }

      if (!_askCache.ContainsKey(itemType))
      {
        // First ask.
        _askCache[itemType] = new MarketCacheValue(ask, household, ask[0]);
      }
      else
      {
        // The first element of the ask is the best price they are willing to sell for,
        // by convention a negative number.
        if (ask[0] < _askCache[itemType].bestPrice)
        {
          // This ask is better than the existing best ask.
          _askCache[itemType].bestCounterparty = household;
          _askCache[itemType].bestPrice = ask[0];
        }
        // Merge the new ask into the existing order book.
        _askCache[itemType].orderBook.Merge(ask);
        _askCache[itemType].participants.Add(household);
      }
      
      
    }
  }

  public void AddBid(ItemType itemType, IHouseholdContext household, UtilityQuantityList bid)
  {
    lock (_bidCache)
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
      if (!household.household.inventory.Contains(ItemType.Coin, neededCoin))
      {
        throw new Exception($"Household {household.household} does not have enough coin to make bid {bid}");
      }

      if (!_bidCache.ContainsKey(itemType))
      {
        // First bidder.
        _bidCache[itemType] = new MarketCacheValue(bid, household, bid[0]);
      }
      else
      {
        // The first element of the bid is the highest price they are willing to buy for.
        // To beat the existing best, it must sort lower.
        if (bid[0] < _bidCache[itemType].bestPrice)
        {
          // This bid is better than the existing best bid.
          _bidCache[itemType].bestCounterparty = household;
          _bidCache[itemType].bestPrice = bid[0];
        }
        // Merge the new bid into the existing order book.
        _bidCache[itemType].orderBook.Merge(bid);
        _bidCache[itemType].participants.Add(household);
      }
    }
  }

  public void ClearBids(ItemType itemType)
  {
    lock (_bidCache)
    {
      _bidCache.Remove(itemType);
    }
  }

  public void ClearAsks(ItemType itemType)
  {
    lock (_askCache)
    {
      _askCache.Remove(itemType);
    }
  }

  public void CollectNewBids(ItemType itemType)
  {
    List<IHouseholdContext> participants;
    lock (_bidCache)
    {
      participants = _bidCache[itemType].participants;
      _bidCache.Remove(itemType);
      // Ask all the participants to re-submit their bids.
    }
    foreach (IHouseholdContext household in participants)
    {
      // TODO
    }
  }

  public void CollectNewAsks(ItemType itemType)
  {
    List<IHouseholdContext> participants;
    lock (_askCache)
    {
      participants = _askCache[itemType].participants;
      _askCache.Remove(itemType);
      // Ask all the participants to re-submit their asks.
    }
    foreach (IHouseholdContext household in participants)
    {
      // TODO
    }
  }

  private class MarketCacheValue
  {
    public UtilityQuantityList orderBook;
    public IHouseholdContext bestCounterparty;
    public UtilityQuantity bestPrice;
    public List<IHouseholdContext> participants = new List<IHouseholdContext>();
    public MarketCacheValue(UtilityQuantityList orderBook, IHouseholdContext bestCounterparty, UtilityQuantity bestPrice)
    {
      this.orderBook = orderBook;
      this.bestCounterparty = bestCounterparty;
      this.bestPrice = bestPrice;
      participants.Add(bestCounterparty);
    }
  }

  private Dictionary<ItemType, MarketCacheValue> _askCache = new Dictionary<ItemType, MarketCacheValue>();

  private Dictionary<ItemType, MarketCacheValue> _bidCache = new Dictionary<ItemType, MarketCacheValue>();
}