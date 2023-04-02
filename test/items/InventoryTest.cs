using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Item;
namespace VillageTest;

[TestClass]
public class InventoryUnitTest
{
  [TestMethod]
  public void TestInventoryAdd()
  {
    // Create an inventory.
    Inventory inventory = new();
    // Create item types.
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, "Type 1", "uxAsset1", 0, 0, false, null, null);
    ItemType itemType2 = new("type2", ItemGroup.CURRENCY, null, "Type 2", "uxAsset1", 0, 0, false, null, null);
    // Create some items.
    Item item = new(itemType);
    Item item2 = new(itemType2);
    // Add an item to the inventory.
    inventory.Add(item, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(1, inventory[item]);
    // Add another item to the inventory.
    inventory.Add(item, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(2, inventory[item]);
    // Add two more of item to the inventory.
    inventory.Add(item, 2);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(4, inventory[item]);
    // Add an item to the inventory.
    inventory.Add(item2, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(1, inventory[item2]);
    // Add another item to the inventory.
    inventory.Add(item2, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(2, inventory[item2]);
    // Add two more of item to the inventory.
    inventory.Add(item2, 2);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(4, inventory[item2]);
    Assert.AreEqual(8, inventory.CountAll());
  }

  [TestMethod]
  public void TestInventoryRemove()
  {
    // Create an inventory.
    Inventory inventory = new();
    // Create item types.
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, "Type 1", "uxAsset1", 0, 0, false, null, null);
    ItemType itemType2 = new("type2", ItemGroup.CURRENCY, null, "Type 2", "uxAsset1", 0, 0, false, null, null);
    // Create some items.
    Item item = new(itemType);
    Item item2 = new(itemType2);
    // Add an item to the inventory.
    inventory.Add(item, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(1, inventory[item]);
    // Add another item to the inventory.
    inventory.Add(item, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(2, inventory[item]);
    // Add two more of item to the inventory.
    inventory.Add(item, 2);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(4, inventory[item]);
    // Add an item to the inventory.
    inventory.Add(item2, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(1, inventory[item2]);
    // Add another item to the inventory.
    inventory.Add(item2, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(2, inventory[item2]);
    // Add two more of item to the inventory.
    inventory.Add(item2, 2);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(4, inventory[item2]);
    // Remove an item from the inventory.
    inventory.Remove(item, 1);
    // Check that the item was removed from the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(3, inventory[item]);
    // Remove another item from the inventory.
    inventory.Remove(item, 3);
    // Check that the item was removed from the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(0, inventory[item]);
    Assert.AreEqual(4, inventory[item2]);
    // Remove another item from the inventory.
    inventory.Remove(item2, 4);
    Assert.AreEqual(0, inventory.Count());
  }

  [TestMethod]
  public void TestInventoryCraft()
  {
    // Create an inventory.
    Inventory inventory = new();
    // Create item types.
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, "Type 1", "uxAsset1", 0, 0, false, null, null);
    ItemType itemType2 = new("type2", ItemGroup.CURRENCY, null, "Type 2", "uxAsset1", 0, 0, false, null, null);
    // Create some items.
    Item item = new(itemType);
    Item item2 = new(itemType2);
    // Add an item to the inventory.
    inventory.Add(item, 4);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(4, inventory[item]);
    // Add an item to the inventory.
    inventory.Add(item2, 5);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(5, inventory[item2]);
    // Craft an item given item and item2 as inputs.
    var inputs = new Dictionary<Item, int> { { item, 2 }, { item2, 2 } };
    ItemType outputType = new("output", ItemGroup.CURRENCY, null, "Output", "uxAsset1", 0, 0, false, null, null);
    Item output = new Item(outputType);
    Assert.IsTrue(inventory.Craft(outputType, inputs));
    // Check that the item was added to the inventory.
    Assert.AreEqual(3, inventory.Count());
    Assert.AreEqual(Inventory.DEFAULT_QUANTITY + 5, inventory.CountAll());
    Assert.AreEqual(2, inventory[item]);
    Assert.AreEqual(3, inventory[item2]);
    Assert.AreEqual(Inventory.DEFAULT_QUANTITY, inventory[output]);
    // Try to craft with insufficient inputs.
    inventory.Remove(item, 1);
    Assert.IsFalse(inventory.Craft(outputType, inputs));
    // Check that the item was not added to the inventory.
    Assert.AreEqual(3, inventory.Count());
    Assert.AreEqual(1, inventory[item]);
    Assert.AreEqual(3, inventory[item2]);
    Assert.AreEqual(Inventory.DEFAULT_QUANTITY, inventory[output]);
  }

  [TestMethod]
  public void TestInventoryTransfer()
  {
    // Create an inventory.
    Inventory inventory = new();
    // Create item types.
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, "Type 1", "uxAsset1", 0, 0, false, null, null);
    ItemType itemType2 = new("type2", ItemGroup.CURRENCY, null, "Type 2", "uxAsset1", 0, 0, false, null, null);
    // Create some items.
    Item item = new(itemType);
    Item item2 = new(itemType2);
    // Add an item to the inventory.
    inventory.Add(item, 4);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(4, inventory[item]);
    // Add an item to the inventory.
    inventory.Add(item2, 5);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(5, inventory[item2]);
    // Create a second inventory.
    Inventory inventory2 = new();
    // Transfer an item from inventory to inventory2.
    var transfering = new Dictionary<Item, int> { { item, 1 }};
    var transfering2 = new Dictionary<Item, int> { { item, 2 }};
    Assert.IsTrue(inventory.Transfer(inventory2, transfering2));
    // Check that the item was transferred to the second inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(2, inventory[item]);
    Assert.AreEqual(1, inventory2.Count());
    Assert.AreEqual(2, inventory2[item]);
    // Transfer an item from inventory2 to inventory.
    Assert.IsTrue(inventory2.Transfer(inventory, transfering));
    // Check that the item was transferred to the first inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(3, inventory[item]);
    Assert.AreEqual(1, inventory2.Count());
    Assert.AreEqual(1, inventory2[item]);
    // Transfer an item from inventory2 to inventory.
    Assert.IsTrue(inventory2.Transfer(inventory, transfering));
    // Check that the item was transferred to the first inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(4, inventory[item]);
    Assert.AreEqual(0, inventory2.Count());
    Assert.AreEqual(0, inventory2[item]);
    // Try to transfer an item from inventory2 to inventory.
    Assert.IsFalse(inventory2.Transfer(inventory, transfering));
    // Check that the item was not transferred to the first inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(4, inventory[item]);
    Assert.AreEqual(0, inventory2.Count());
    Assert.AreEqual(0, inventory2[item]);
  }

  [TestMethod]
  public void TestInventoryTrade()
  {
    // Create an inventory.
    Inventory inventory = new();
    // Create item types.
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, "Type 1", "uxAsset1", 0, 0, false, null, null);
    ItemType itemType2 = new("type2", ItemGroup.CURRENCY, null, "Type 2", "uxAsset1", 0, 0, false, null, null);
    // Create some items.
    Item item = new(itemType);
    Item item2 = new(itemType2);
    // Add an item to the inventory.
    inventory.Add(item, 4);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(4, inventory[item]);
    // Add an item to the inventory.
    inventory.Add(item2, 5);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(5, inventory[item2]);
    // Create a second inventory.
    Inventory inventory2 = new();
    // Fail to trade an item from inventory to inventory2, as inventory2 has no items.
    var trading = new Dictionary<Item, int> { { item, 1 }};
    var trading2 = new Dictionary<Item, int> { { item2, 1 }};
    Assert.IsFalse(inventory.Trade(inventory2, trading, trading2));
    // Check that the item was not traded to the second inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(4, inventory[item]);
    Assert.AreEqual(0, inventory2.Count());
    Assert.AreEqual(0, inventory2[item]);
    // Add an item to the second inventory.
    inventory2.Add(item2, 1);
    // Trade an item from inventory to inventory2.
    Assert.IsTrue(inventory.Trade(inventory2, trading, trading2));
    // Check that the item was traded to the second inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(3, inventory[item]);
    Assert.AreEqual(6, inventory[item2]);
    Assert.AreEqual(1, inventory2.Count());
    Assert.AreEqual(1, inventory2[item]);
  }
}