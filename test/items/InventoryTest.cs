using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Items;
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
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, 0, 0, false, null, null);
    ItemType itemType2 = new("type2", ItemGroup.CURRENCY, null, 0, 0, false, null, null);
    // Create some items.
    Item item = new(itemType);
    Item item2 = new(itemType2);
    Assert.IsFalse(inventory.Contains(item));
    Assert.IsFalse(inventory.Contains(item2));
    // Add an item to the inventory.
    inventory.AddItem(item, 1);
    // Check that the item was added to the inventory.
    Assert.IsTrue(inventory.Contains(item));
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(1, inventory[item]);
    // Add another item to the inventory.
    inventory.AddItem(item, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(2, inventory[item]);
    // Add two more of item to the inventory.
    inventory.AddItem(item, 2);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(4, inventory[item]);
    // Add an item to the inventory.
    inventory.AddItem(item2, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(1, inventory[item2]);
    // Add another item to the inventory.
    inventory.AddItem(item2, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(2, inventory[item2]);
    // Add two more of item to the inventory.
    inventory.AddItem(item2, 2);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(4, inventory[item2]);
    Assert.AreEqual(8, inventory.CountAll());
    Assert.IsTrue(inventory.Contains(item));
    Assert.IsTrue(inventory.Contains(item2));
    Assert.IsTrue(inventory.Contains(new Dictionary<ItemType, int>() { { itemType, 1 }, { itemType2, 1 } }));

  }

  [TestMethod]
  public void TestInventoryRemove()
  {
    // Create an inventory.
    Inventory inventory = new();
    // Create item types.
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, 0, 0, false, null, null);
    ItemType itemType2 = new("type2", ItemGroup.CURRENCY, null, 0, 0, false, null, null);
    // Create some items.
    Item item = new(itemType);
    Item item2 = new(itemType2);
    // Add an item to the inventory.
    inventory.AddItem(item, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(1, inventory[item]);
    // Add another item to the inventory.
    inventory.AddItem(item, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(2, inventory[item]);
    // Add two more of item to the inventory.
    inventory.AddItem(item, 2);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(4, inventory[item]);
    // Add an item to the inventory.
    inventory.AddItem(item2, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(1, inventory[item2]);
    // Add another item to the inventory.
    inventory.AddItem(item2, 1);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(2, inventory[item2]);
    // Add two more of item to the inventory.
    inventory.AddItem(item2, 2);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(4, inventory[item2]);
    // Remove an item from the inventory.
    inventory.RemoveItem(item, 1);
    // Check that the item was removed from the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(3, inventory[item]);
    // Remove another item from the inventory.
    inventory.RemoveItem(item, 3);
    // Check that the item was removed from the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(0, inventory[item]);
    Assert.AreEqual(4, inventory[item2]);
    // Remove another item from the inventory.
    inventory.RemoveItem(item2, 4);
    Assert.AreEqual(0, inventory.Count());
  }

  [TestMethod]
  public void TestInventoryTransfer()
  {
    // Create an inventory.
    Inventory inventory = new();
    // Create item types.
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, 0, 0, false, null, null);
    ItemType itemType2 = new("type2", ItemGroup.CURRENCY, null, 0, 0, false, null, null);
    // Create some items.
    Item item = new(itemType);
    Item item2 = new(itemType2);
    // Add an item to the inventory.
    inventory.AddItem(item, 4);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(4, inventory[item]);
    // Add an item to the inventory.
    inventory.AddItem(item2, 5);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(5, inventory[item2]);
    // Create a second inventory.
    Inventory inventory2 = new();
    // Transfer an item from inventory to inventory2.
    var transfering = new Dictionary<Item, int> { { item, 1 } };
    var transfering2 = new Dictionary<Item, int> { { item, 2 } };
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
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, 0, 0, false, null, null);
    ItemType itemType2 = new("type2", ItemGroup.CURRENCY, null, 0, 0, false, null, null);
    // Create some items.
    Item item = new(itemType);
    Item item2 = new(itemType2);
    // Add an item to the inventory.
    inventory.AddItem(item, 4);
    // Check that the item was added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(4, inventory[item]);
    // Add an item to the inventory.
    inventory.AddItem(item2, 5);
    // Check that the item was added to the inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(5, inventory[item2]);
    // Create a second inventory.
    Inventory inventory2 = new();
    // Fail to trade an item from inventory to inventory2, as inventory2 has no items.
    var trading = new Dictionary<Item, int> { { item, 1 } };
    var trading2 = new Dictionary<Item, int> { { item2, 1 } };
    Assert.IsFalse(inventory.Trade(inventory2, trading, trading2));
    // Check that the item was not traded to the second inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(4, inventory[item]);
    Assert.AreEqual(0, inventory2.Count());
    Assert.AreEqual(0, inventory2[item]);
    // Add an item to the second inventory.
    inventory2.AddItem(item2, 1);
    // Trade an item from inventory to inventory2.
    Assert.IsTrue(inventory.Trade(inventory2, trading, trading2));
    // Check that the item was traded to the second inventory.
    Assert.AreEqual(2, inventory.Count());
    Assert.AreEqual(3, inventory[item]);
    Assert.AreEqual(6, inventory[item2]);
    Assert.AreEqual(1, inventory2.Count());
    Assert.AreEqual(1, inventory2[item]);
  }

  [TestMethod]
  public void TestInventoryRemoveTypes()
  {
    // Create an inventory.
    Inventory inventory = new();
    // Create item types.
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, 100, 0, false, null, null);
    // Create some items.
    Item item = new(itemType);
    Item item2 = new(itemType);
    Item item3 = new(itemType);
    // Make item2 spoil sooner and item3 later.
    item2.timeUntilSpoilage = 1;
    item3.timeUntilSpoilage = 500;
    // Add some of item and item2 to the inventory.
    inventory.AddItem(item, 4);
    inventory.AddItem(item2, 4);
    inventory.AddItem(item3, 4);
    // Check that the items were added to the inventory.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(3, inventory.CountUnique());
    Assert.AreEqual(12, inventory.CountAll());
    Assert.AreEqual(4, inventory[item]);
    Assert.AreEqual(4, inventory[item2]);
    Assert.AreEqual(4, inventory[item3]);
    // Create input dictionary for removing itemType.
    var removing = new Dictionary<ItemType, int> { { itemType, 3 } };
    // Remove 3 of itemType from the inventory.
    Assert.IsTrue(inventory.Contains(removing));
    Assert.IsTrue(inventory.Remove(removing));
    // Check that the items were removed from the inventory, and
    // that item2 was removed before item.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(3, inventory.CountUnique());
    Assert.AreEqual(9, inventory.CountAll());
    Assert.AreEqual(4, inventory[item]);
    Assert.AreEqual(1, inventory[item2]);
    Assert.AreEqual(4, inventory[item3]);
    // Remove more.
    Assert.IsTrue(inventory.Remove(removing));
    // Check that all of item2 was removed, and some of item.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(2, inventory.CountUnique());
    Assert.AreEqual(6, inventory.CountAll());
    Assert.AreEqual(2, inventory[item]);
    Assert.AreEqual(4, inventory[item3]);
    // Remove more.
    Assert.IsTrue(inventory.Remove(removing));
    // Check that all of item was removed and one of item3.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(1, inventory.CountUnique());
    Assert.AreEqual(3, inventory.CountAll());
    Assert.AreEqual(3, inventory[item3]);
    // Remove more.
    Assert.IsTrue(inventory.Remove(removing));
    // Check that all of item3 was removed.
    Assert.AreEqual(0, inventory.Count());
    Assert.AreEqual(0, inventory.CountUnique());
    Assert.AreEqual(0, inventory.CountAll());
    // Try to remove more.
    Assert.IsFalse(inventory.Remove(removing));

  }
}