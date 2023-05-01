using Newtonsoft.Json;
using Village.Persons;

namespace Village.Items;

public interface IPriceList
{
  // Given an item, return the price they are willing to pay for the item.
  int BuyPrice(Item item);

  // Given an item, return the price they are willing to sell the item for.
  int SellPrice(Item item);
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

  // Load a price list from a JSON file.
  public static ConfigPriceList Load(string filename)
  {
    var prices = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ConfigItemPrice>>(System.IO.File.ReadAllText(filename));
    return new ConfigPriceList(prices!);
  }

  // Given an item type, return the price they are willing to pay for the item.
  public int BuyPrice(Item item)
  {
    if (_prices.ContainsKey(item.itemType))
    {
      return _prices[item.itemType].buy;
    }
    return 0;
  }

  // Given an item, return the price they are willing to sell the item for.
  public int SellPrice(Item item)
  {
    if (_prices.ContainsKey(item.itemType))
    {
      return _prices[item.itemType].sell;
    }
    return int.MaxValue;
  }
}

