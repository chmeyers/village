using Newtonsoft.Json;
using Village.Persons;

namespace Village.Items;

public interface IPriceList
{
  // Given an item, return the price they are willing to pay for the item.
  UtilityQuantityList BidPrice(Item item);
  UtilityQuantityList BidPrice(ItemType itemType);

  // Given an item, return the price they are willing to sell the item for.
  // By convention, these are negative numbers.
  UtilityQuantityList AskPrice(Item item);
  UtilityQuantityList AskPrice(ItemType itemType);
}


public class ConfigItemPrice
{
  public int bid { get; set; }
  public int ask { get; set; }

  public override string ToString()
  {
    return $"bid: {bid}, ask: {ask}";
  }
}

public class ConfigPriceList : IPriceList
{
  // static default instance.
  public static ConfigPriceList Default { get; private set; } = new ConfigPriceList(new Dictionary<string, ConfigItemPrice>());

  // The price list.
  private Dictionary<ItemType, ConfigItemPrice> _prices = new Dictionary<ItemType, ConfigItemPrice>();

  // Constructor.
  public ConfigPriceList(Dictionary<string, ConfigItemPrice> prices)
  {
    foreach (var price in prices)
    {
      _prices[ItemType.Find(price.Key)!] = price.Value;
    }
  }

  // Load the default price list.
  public static void LoadDefault(string filename)
  {
    Default = Load(filename);
  }

  public static void LoadDefaultFromString(string json)
  {
    var prices = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ConfigItemPrice>>(json);
    Default = new ConfigPriceList(prices!);
  }

  // Load a price list from a JSON file.
  public static ConfigPriceList Load(string filename)
  {
    var prices = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ConfigItemPrice>>(System.IO.File.ReadAllText(filename));
    return new ConfigPriceList(prices!);
  }

  // Given an item type, return the price they are willing to pay for the item.
  public UtilityQuantityList BidPrice(ItemType itemType)
  {
    UtilityQuantityList bid = new UtilityQuantityList();
    if (_prices.ContainsKey(itemType))
    {
      bid.Add(new UtilityQuantity(int.MaxValue, int.MaxValue, _prices[itemType].bid));
    }
    return bid;
  }

  public UtilityQuantityList BidPrice(Item item) => BidPrice(item.itemType);

  // Given an item, return the price they are willing to sell the item for.
  // By convention, these are negative.
  public UtilityQuantityList AskPrice(ItemType itemType)
  {
    UtilityQuantityList ask = new UtilityQuantityList();
    if (_prices.ContainsKey(itemType))
    {
      ask.Add(new UtilityQuantity(int.MaxValue, int.MaxValue, -_prices[itemType].ask));
    }
    return ask;
  }

  public UtilityQuantityList AskPrice(Item item) => AskPrice(item.itemType);
}

