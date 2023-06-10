using Newtonsoft.Json;
using Village.Persons;

namespace Village.Items;

public interface IPriceList
{
  // Given an item, return the price they are willing to pay for the item.
  double BuyPrice(Item item);
  double BuyPrice(ItemType itemType);

  // Given an item, return the price they are willing to sell the item for.
  double SellPrice(Item item);
  double SellPrice(ItemType itemType);
}


public class ConfigItemPrice
{
  public int buy { get; set; }
  public int sell { get; set; }
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
  public double BuyPrice(ItemType itemType)
  {
    if (_prices.ContainsKey(itemType))
    {
      return _prices[itemType].buy;
    }
    return 0;
  }

  public double BuyPrice(Item item) => BuyPrice(item.itemType);

  // Given an item, return the price they are willing to sell the item for.
  public double SellPrice(ItemType itemType)
  {
    if (_prices.ContainsKey(itemType))
    {
      return _prices[itemType].sell;
    }
    return int.MaxValue;
  }

  public double SellPrice(Item item) => SellPrice(item.itemType);
}

