using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Item;
namespace VillageTest;

[TestClass]
public class ItemUnitTest
{
  [TestMethod]
  public void TestLoadItemTypes()
  {
    string json = @"{
  'type1': { 'group': 'CURRENCY', 'displayName': 'Type 1', uxAsset: 'uxAsset1'},
  'type2': { 'parent': 'type1', 'group': 'RESOURCE', 'displayName': 'Type 2', uxAsset: 'uxAsset2', spoilTime: 2, lossRate: 2, flammable: true, scrapItems: { 'type1': 50 } }
}";
    // Load the item types.
    ItemType.LoadString(json);
    // Check that the item types were loaded.
    Assert.AreEqual(2, ItemType.itemTypes.Count);
    // Check that the item types were loaded correctly.
    Assert.AreEqual("type1", ItemType.itemTypes["type1"].itemType);
    Assert.AreEqual("CURRENCY", ItemType.itemTypes["type1"].itemGroup.ToString());
    Assert.AreEqual(false, ItemType.itemTypes["type1"].flammable);
    Assert.AreEqual(0, ItemType.itemTypes["type1"].spoilTime);
    Assert.AreEqual(0, ItemType.itemTypes["type1"].lossRate);
    Assert.AreEqual(ItemType.itemTypes["type1"], ItemType.itemTypes["type2"].parentType);


    Assert.AreEqual("Type 1", ItemType.itemTypes["type1"].displayName);
    Assert.AreEqual("uxAsset1", ItemType.itemTypes["type1"].uxAsset);
    Assert.AreEqual("type2", ItemType.itemTypes["type2"].itemType);
    Assert.AreEqual("RESOURCE", ItemType.itemTypes["type2"].itemGroup.ToString());
    Assert.AreEqual("Type 2", ItemType.itemTypes["type2"].displayName);
    Assert.AreEqual("uxAsset2", ItemType.itemTypes["type2"].uxAsset);
    Assert.AreEqual(2, ItemType.itemTypes["type2"].spoilTime);
    Assert.AreEqual(2, ItemType.itemTypes["type2"].lossRate);
    Assert.AreEqual(true, ItemType.itemTypes["type2"].flammable);
    Assert.IsNotNull(ItemType.itemTypes["type2"].scrapItems);
    Assert.AreEqual(1, ItemType.itemTypes["type2"].scrapItems!.Count);
    Assert.AreEqual(50, ItemType.itemTypes["type2"].scrapItems![ItemType.itemTypes["type1"]]);

  }
}