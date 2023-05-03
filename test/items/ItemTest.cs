using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Abilities;
using Village.Items;
namespace VillageTest;

[TestClass]
public class ItemUnitTest
{
  [TestMethod]
  public void TestLoadItemTypes()
  {
    ItemType.Clear();
    string json = @"{
  'type1': { 'group': 'CURRENCY' },
  'type2': { 'parent': 'type1', 'group': 'RESOURCE', spoilTime: 2, lossRate: 0.02, flammable: true, scrapItems: { 'type1': 50 } }
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


    Assert.AreEqual("type2", ItemType.itemTypes["type2"].itemType);
    Assert.AreEqual("RESOURCE", ItemType.itemTypes["type2"].itemGroup.ToString());
    Assert.AreEqual(2, ItemType.itemTypes["type2"].spoilTime);
    Assert.AreEqual(0.02, ItemType.itemTypes["type2"].lossRate);
    Assert.AreEqual(true, ItemType.itemTypes["type2"].flammable);
    Assert.IsNotNull(ItemType.itemTypes["type2"].scrapItems);
    Assert.AreEqual(1, ItemType.itemTypes["type2"].scrapItems!.Count);
    Assert.AreEqual(50, ItemType.itemTypes["type2"].scrapItems![ItemType.itemTypes["type1"]]);

  }

  [TestMethod]
  public void TestLoadItemTypesWithAbility()
  {
    ItemType.Clear();
    AbilityType.Clear();
    // Load ability types.
    AbilityType.LoadString(@"{ 'cutting' : { 'levels': 10 } }");
    string json = @"{
  'type3': { 'group': 'CURRENCY'},
  'type4': { 'parent': 'type3', 'group': 'RESOURCE', spoilTime: 2, lossRate: 0.02, flammable: true, scrapItems: { 'type3': 50 }, abilities: ['cutting_2'] }
}";
    // Load the item types.
    ItemType.LoadString(json);
    // Check that the item types were loaded correctly.
    Assert.AreEqual("type3", ItemType.itemTypes["type3"].itemType);
    Assert.AreEqual("CURRENCY", ItemType.itemTypes["type3"].itemGroup.ToString());
    Assert.AreEqual(false, ItemType.itemTypes["type3"].flammable);
    Assert.AreEqual(0, ItemType.itemTypes["type3"].spoilTime);
    Assert.AreEqual(0, ItemType.itemTypes["type3"].lossRate);
    Assert.AreEqual(ItemType.itemTypes["type3"], ItemType.itemTypes["type4"].parentType);
    // Check that item2 contains the ability.
    Assert.IsNotNull(ItemType.itemTypes["type4"].abilities);
    Assert.AreEqual(1, ItemType.itemTypes["type4"].abilities!.Count);
    AbilityType c2 = AbilityType.abilityTypes["cutting_2"];
    Assert.IsTrue(ItemType.itemTypes["type4"].abilities!.Contains(c2));
  }
}