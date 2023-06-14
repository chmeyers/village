using Newtonsoft.Json;
using Village.Persons;

namespace Village.Items;

public interface IPriceList
{
  // Given an item, return the price they are willing to pay for the item.
  double BidPrice(Item item);
  double BidPrice(ItemType itemType);

  // Given an item, return the price they are willing to sell the item for.
  double AskPrice(Item item);
  double AskPrice(ItemType itemType);
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
  public double BidPrice(ItemType itemType)
  {
    if (_prices.ContainsKey(itemType))
    {
      return _prices[itemType].bid;
    }
    return 0;
  }

  public double BidPrice(Item item) => BidPrice(item.itemType);

  // Given an item, return the price they are willing to sell the item for.
  public double AskPrice(ItemType itemType)
  {
    if (_prices.ContainsKey(itemType))
    {
      return _prices[itemType].ask;
    }
    return int.MaxValue;
  }

  public double AskPrice(Item item) => AskPrice(item.itemType);
}

