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
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, 0, 0, false, 0.5, 1.0, null, null, null, null);
    ItemType itemType2 = new("type2", ItemGroup.CURRENCY, null, 0, 0, false, 0.5, 1.0, null, null, null, null);
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
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, 0, 0, false, 0.5, 1.0, null, null, null, null);
    ItemType itemType2 = new("type2", ItemGroup.CURRENCY, null, 0, 0, false, 0.5, 1.0, null, null, null, null);
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
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, 0, 0, false, 0.5, 1.0, null, null, null, null);
    ItemType itemType2 = new("type2", ItemGroup.CURRENCY, null, 0, 0, false, 0.5, 1.0, null, null, null, null);
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
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, 0, 0, false, 0.5, 1.0, null, null, null, null);
    ItemType itemType2 = new("type2", ItemGroup.CURRENCY, null, 0, 0, false, 0.5, 1.0, null, null, null, null);
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
    ItemType itemType = new("type1", ItemGroup.CURRENCY, null, 100, 0, false, 0.5, 1.0, null, null, null, null);
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
    Assert.IsTrue(inventory.Remove(inventory.Get(removing)!));
    // Check that the items were removed from the inventory, and
    // that item2 was removed before item.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(3, inventory.CountUnique());
    Assert.AreEqual(9, inventory.CountAll());
    Assert.AreEqual(4, inventory[item]);
    Assert.AreEqual(1, inventory[item2]);
    Assert.AreEqual(4, inventory[item3]);
    // Remove more.
    Assert.IsTrue(inventory.Remove(inventory.Get(removing)!));
    // Check that all of item2 was removed, and some of item.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(2, inventory.CountUnique());
    Assert.AreEqual(6, inventory.CountAll());
    Assert.AreEqual(2, inventory[item]);
    Assert.AreEqual(4, inventory[item3]);
    // Remove more.
    Assert.IsTrue(inventory.Remove(inventory.Get(removing)!));
    // Check that all of item was removed and one of item3.
    Assert.AreEqual(1, inventory.Count());
    Assert.AreEqual(1, inventory.CountUnique());
    Assert.AreEqual(3, inventory.CountAll());
    Assert.AreEqual(3, inventory[item3]);
    // Remove more.
    Assert.IsTrue(inventory.Remove(inventory.Get(removing)!));
    // Check that all of item3 was removed.
    Assert.AreEqual(0, inventory.Count());
    Assert.AreEqual(0, inventory.CountUnique());
    Assert.AreEqual(0, inventory.CountAll());
    // Try to remove more.
    Assert.IsTrue(inventory.Get(removing) == null);

  }

  [TestMethod]
  public void TestInventoryParentTypes()
  {
    // Create an inventory.
    Inventory inventory = new();
    // Create item types, with a chain of parent types.
    ItemType grandparent1 = new("grandparent1", ItemGroup.CURRENCY, null, 100, 0, false, 0.5, 1.0, null, null, null, null);
    ItemType grandparent2 = new("grandparent2", ItemGroup.CURRENCY, null, 100, 0, false, 0.5, 1.0, null, null, null, null);
    ItemType parent1 = new("parent1", ItemGroup.CURRENCY, new List<ItemType> {grandparent1, grandparent2}, 100, 0, false, 0.5, 1.0, null, null, null, null);
    ItemType parent2 = new("parent2", ItemGroup.CURRENCY, null, 100, 0, false, 0.5, 1.0, null, null, null, null);
    ItemType child1 = new("child1", ItemGroup.CURRENCY, new List<ItemType> {parent1, parent2}, 100, 0, false, 0.5, 1.0, null, null, null, null);
    ItemType child2 = new("child2", ItemGroup.CURRENCY, new List<ItemType> {parent1, parent2}, 100, 0, false, 0.5, 1.0, null, null, null, null);
    // Create some items.
    Item grandparent1Item = new(grandparent1);
    Item grandparent2Item = new(grandparent2);
    Item parent1Item = new(parent1);
    Item parent2Item = new(parent2);
    Item child1Item = new(child1);
    Item child2Item = new(child2);
    // Add some of each item to the inventory.
    inventory.AddItem(grandparent1Item, 4);
    inventory.AddItem(grandparent2Item, 4);
    inventory.AddItem(parent1Item, 4);
    inventory.AddItem(parent2Item, 4);
    inventory.AddItem(child1Item, 4);
    inventory.AddItem(child2Item, 4);
    // Test the Contains method. It should return both the parent and child items.
    // If we ask for only one item, the parent should be returned.
    var containing = new Dictionary<ItemType, int> { { parent1, 1 } };
    Assert.IsTrue(inventory.Contains(containing));
    Assert.AreEqual(1, inventory.Get(containing)!.Count);
    Assert.AreEqual(1, inventory.Get(containing)![parent1Item]);
    // If we ask for more than is available of the parent, the child should be returned, too.
    var containing2 = new Dictionary<ItemType, int> { { parent1, 5 } };
    Assert.IsTrue(inventory.Contains(containing2));
    Assert.AreEqual(2, inventory.Get(containing2)!.Count);
    Assert.AreEqual(4, inventory.Get(containing2)![parent1Item]);
    Assert.AreEqual(1, inventory.Get(containing2)![child1Item]);
    // If we ask for more than is available of the parent and one child,
    // the other child should be returned, too.
    var containing3 = new Dictionary<ItemType, int> { { parent1, 9 } };
    Assert.IsTrue(inventory.Contains(containing3));
    Assert.AreEqual(3, inventory.Get(containing3)!.Count);
    Assert.AreEqual(4, inventory.Get(containing3)![parent1Item]);
    Assert.AreEqual(4, inventory.Get(containing3)![child1Item]);
    Assert.AreEqual(1, inventory.Get(containing3)![child2Item]);


    // Test the ContainExact method. It should return only the parent item.
    Assert.IsTrue(inventory.ContainsExact(containing));
    Assert.AreEqual(1, inventory.GetExact(containing)!.Count);
    Assert.AreEqual(1, inventory.GetExact(containing)![parent1Item]);
    // If we ask for more than is available of the parent, the child should not be returned.
    Assert.IsFalse(inventory.ContainsExact(containing2));
    Assert.IsTrue(inventory.GetExact(containing2) == null);

    // If we ask for both a child and parent, they should both be returned.
    var containing4 = new Dictionary<ItemType, int> { { parent1, 1 }, { child1, 1 } };
    Assert.IsTrue(inventory.Contains(containing4));
    Assert.AreEqual(2, inventory.Get(containing4)!.Count);
    Assert.AreEqual(1, inventory.Get(containing4)![parent1Item]);
    Assert.AreEqual(1, inventory.Get(containing4)![child1Item]);
    // If we ask for more than is available of the parent, the extra should be the child.
    var containing5 = new Dictionary<ItemType, int> { { parent1, 5 }, { child1, 1 } };
    Assert.IsTrue(inventory.Contains(containing5));
    Assert.AreEqual(2, inventory.Get(containing5)!.Count);
    Assert.AreEqual(4, inventory.Get(containing5)![parent1Item]);
    Assert.AreEqual(2, inventory.Get(containing5)![child1Item]);
    // If we ask for more than is available of the parent and one child,
    // the other child should be returned, too.
    var containing6_bad_order = new List<KeyValuePair<ItemType, int>> { new KeyValuePair<ItemType, int>(parent1, 6), new KeyValuePair<ItemType, int>(child1, 3) };
    var containing6 = new List<KeyValuePair<ItemType, int>> { new KeyValuePair<ItemType, int>(child1, 3), new KeyValuePair<ItemType, int>(parent1, 6) };
    // The bad order should not work as the parent will take some of the child's items.
    Assert.IsFalse(inventory.Contains(containing6_bad_order));
    // The good order should work.
    Assert.IsTrue(inventory.Contains(containing6));
    Assert.AreEqual(3, inventory.Get(containing6)!.Count);
    Assert.AreEqual(4, inventory.Get(containing6)![parent1Item]);
    Assert.AreEqual(4, inventory.Get(containing6)![child1Item]);
    Assert.AreEqual(1, inventory.Get(containing6)![child2Item]);

    // Asking for a child and grandparent should return the grandparent first.
    var containing7 = new Dictionary<ItemType, int> { { grandparent1, 1 }, { child1, 1 } };
    Assert.IsTrue(inventory.Contains(containing7));
    Assert.AreEqual(2, inventory.Get(containing7)!.Count);
    Assert.AreEqual(1, inventory.Get(containing7)![grandparent1Item]);
    Assert.AreEqual(1, inventory.Get(containing7)![child1Item]);

    // When Asking for a child and a lot of grandparent, the parent and child1 should get the overflow.
    var containing8 = new Dictionary<ItemType, int> { { grandparent1, 8 }, { child1, 1 } };
    Assert.IsTrue(inventory.Contains(containing8));
    Assert.AreEqual(3, inventory.Get(containing8)!.Count);
    Assert.AreEqual(4, inventory.Get(containing8)![grandparent1Item]);
    Assert.AreEqual(4, inventory.Get(containing8)![parent1Item]);
    Assert.AreEqual(1, inventory.Get(containing8)![child1Item]);

    // Even more and it should overflow to children.
    var containing9 = new Dictionary<ItemType, int> { { grandparent1, 12 }, { child2, 1 } };
    Assert.IsTrue(inventory.Contains(containing9));
    Assert.AreEqual(4, inventory.Get(containing9)!.Count);
    Assert.AreEqual(4, inventory.Get(containing9)![grandparent1Item]);
    Assert.AreEqual(4, inventory.Get(containing9)![parent1Item]);
    Assert.AreEqual(4, inventory.Get(containing9)![child1Item]);
    Assert.AreEqual(1, inventory.Get(containing9)![child2Item]);

    // Asking for 10 of each grandparent and 4 of parent2 should return 4 over everybody.
    var containing10 = new Dictionary<ItemType, int> { { grandparent1, 10 }, { grandparent2, 10 }, { parent2, 4 } };
    Assert.IsTrue(inventory.Contains(containing10));
    Assert.AreEqual(6, inventory.Get(containing10)!.Count);
    Assert.AreEqual(4, inventory.Get(containing10)![grandparent1Item]);
    Assert.AreEqual(4, inventory.Get(containing10)![grandparent2Item]);
    Assert.AreEqual(4, inventory.Get(containing10)![parent1Item]);
    Assert.AreEqual(4, inventory.Get(containing10)![parent2Item]);
    Assert.AreEqual(4, inventory.Get(containing10)![child1Item]);
    Assert.AreEqual(4, inventory.Get(containing10)![child2Item]);


  }
}