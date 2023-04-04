using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Abilities;
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
    Assert.AreEqual(2, AbilityType.abilityTypes["cutting_7"].superTypes.Count);
    Assert.IsTrue(AbilityType.abilityTypes["cutting_7"].superTypes.Contains("cutting_8"));
    Assert.IsTrue(AbilityType.abilityTypes["cutting_7"].superTypes.Contains("cutting_9"));
  }
    
}