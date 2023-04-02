using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Ability;
namespace VillageTest;

[TestClass]
public class AbilityUnitTest
{
  [TestMethod]
  public void TestLoadAbilityTypes()
  {
    AbilityType.Clear();
    string json = @"{ 'cutting' : { 'levels': 10 } }";
    // Load the ability types.
    AbilityType.LoadString(json);
    // Check that the ability types were loaded.
    Assert.AreEqual(10, AbilityType.abilityTypes.Count);
    // Check that the ability types were loaded correctly.
    Assert.AreEqual("cutting", AbilityType.abilityTypes["cutting_9"].parentType);
    Assert.AreEqual(9, AbilityType.abilityTypes["cutting_9"].subTypes.Count);
    Assert.IsTrue(AbilityType.abilityTypes["cutting_9"].subTypes.Contains("cutting_8"));
    Assert.IsTrue(AbilityType.abilityTypes["cutting_9"].subTypes.Contains("cutting_0"));
  }
    
}