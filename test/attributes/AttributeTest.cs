using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Abilities;
using Village.Attributes;
using Attribute = Village.Attributes.Attribute;

namespace VillageTest;

[TestClass]
public class AttributeUnitTest
{
  [TestMethod]
  public void TestLoadAttributeTypes()
  {
    {
      AbilityType.Clear();
      AbilityType.LoadString(@"{ 'weak' : { 'levels': 2 } }");
      AbilityType.LoadString(@"{ 'strong' : { 'levels': 3 } }");
    }
    AttributeType.Clear();
    string json = @"{ 'strength' : { 'min': 0, 'max': 10, 'initial': 2, 'intervals':
      [{'lower': 0, 'abilities': ['weak_1']},
       {'lower': 3},
       {'lower': 6, 'abilities': ['strong_1']},
       {'lower': 9, 'abilities': ['strong_2']}
      ] } }";
    // Load the attribute types.
    AttributeType.LoadString(json);
    // Check that the attribute types were loaded.
    Assert.AreEqual(1, AttributeType.types.Count);
    // Check that the attribute types were loaded correctly.
    Assert.AreEqual(0, AttributeType.types["strength"].minValue);
    Assert.AreEqual(10, AttributeType.types["strength"].maxValue);
    Assert.AreEqual(2, AttributeType.types["strength"].initialValue);
    Assert.AreEqual(4, AttributeType.types["strength"].intervals.Count);
    Assert.AreEqual(0, AttributeType.types["strength"].intervals.GetValueAtIndex(0).lower);
    Assert.AreEqual(3, AttributeType.types["strength"].intervals.GetValueAtIndex(0).upper);
    Assert.AreEqual(3, AttributeType.types["strength"].intervals.GetValueAtIndex(1).lower);
    Assert.AreEqual(6, AttributeType.types["strength"].intervals.GetValueAtIndex(1).upper);
    Assert.AreEqual(6, AttributeType.types["strength"].intervals.GetValueAtIndex(2).lower);
    Assert.AreEqual(9, AttributeType.types["strength"].intervals.GetValueAtIndex(2).upper);
    Assert.AreEqual(9, AttributeType.types["strength"].intervals.GetValueAtIndex(3).lower);
    Assert.AreEqual(10, AttributeType.types["strength"].intervals.GetValueAtIndex(3).upper);
    Assert.AreEqual(1, AttributeType.types["strength"].intervals.GetValueAtIndex(0).abilities.Count);
    Assert.AreEqual(0, AttributeType.types["strength"].intervals.GetValueAtIndex(1).abilities.Count);
    Assert.AreEqual(1, AttributeType.types["strength"].intervals.GetValueAtIndex(2).abilities.Count);
    Assert.AreEqual(1, AttributeType.types["strength"].intervals.GetValueAtIndex(3).abilities.Count);
    
    Assert.IsTrue(AttributeType.types["strength"].intervals[0].abilities.Contains(AbilityType.Find("weak_1")!));

    // Create an attribute.
    Attribute a = new Attribute(AttributeType.Find("strength")!, null, null);
    // Check that the attribute was created correctly.
    Assert.AreEqual(2, a.value);
    Assert.AreEqual(0, a.rangeMin);
    Assert.AreEqual(3, a.rangeMax);
    // GetAbilities should contain the ability from the first interval.
    Assert.AreEqual(1, a.GetAbilities().Count);
    Assert.IsTrue(a.GetAbilities().Contains(AbilityType.Find("weak_1")!));
    // Set the value to 1 and verify that abilities didn't change.
    Assert.IsFalse(a.SetValue(1));
    Assert.AreEqual(1, a.value);
    Assert.AreEqual(0, a.rangeMin);
    Assert.AreEqual(3, a.rangeMax);
    Assert.AreEqual(1, a.GetAbilities().Count);
    Assert.IsTrue(a.GetAbilities().Contains(AbilityType.Find("weak_1")!));
    // Set the value to 4 and verify that abilities changed.
    Assert.IsTrue(a.SetValue(4));
    Assert.AreEqual(4, a.value);
    Assert.AreEqual(3, a.rangeMin);
    Assert.AreEqual(6, a.rangeMax);
    Assert.AreEqual(0, a.GetAbilities().Count);
    // Set the value to 7 and verify that abilities changed.
    Assert.IsTrue(a.SetValue(7));
    Assert.AreEqual(7, a.value);
    Assert.AreEqual(6, a.rangeMin);
    Assert.AreEqual(9, a.rangeMax);
    Assert.AreEqual(1, a.GetAbilities().Count);
    Assert.IsTrue(a.GetAbilities().Contains(AbilityType.Find("strong_1")!));

  }

}