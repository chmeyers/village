using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Abilities;
using Village.Attributes;
using Village.Items;
using Village.Persons;
using Attribute = Village.Attributes.Attribute;

namespace VillageTest;

[TestClass]
public class AttributeUnitTest
{
  public class ScopedAttributeSets : IAbilityContext
  {
    public AttributeSet set = new AttributeSet(null, null, null);

    public Dictionary<AbilityType, HashSet<IAbilityProvider>> AbilityProviders => throw new NotImplementedException();

    public HashSet<AbilityType> _abilities = new HashSet<AbilityType>();

    public HashSet<AbilityType> Abilities => _abilities;

    // Not Currently Used.
    public event AbilitiesChanged? AbilitiesChanged { add { } remove { } }

    public double? GetNamedValue(string name)
    {
      return set.GetNamedValue(name);
    }

    public void GrantAbility(AbilityType ability)
    {
      throw new NotImplementedException();
    }
  }
  [TestMethod]
  public void TestLoadAttributeTypes()
  {
    {
      AbilityType.Clear();
      AbilityType.LoadString(@"{ 'weak' : { 'levels': 2 } }");
      AbilityType.LoadString(@"{ 'strong' : { 'levels': 3 } }");
      AbilityType.LoadString(@"{ 'plus' : { 'levels': 3 } }");
    }
    AttributeType.Clear();
    string json = @"{ 'strength' : { 'min': 0, 'max': 11, 'initial': {'val': 2, 'modifiers': {'plus_1': {'add': 3}, 'plus_2': { 'add': 3 } }},
     'utilityType': 'Sigmoid',
     'intervals':
      [{'lower': 0, 'abilities': ['weak_1'], 'utility': 0},
       {'lower': 3, 'utility': 1, 'utilityType': 'Linear'},
       {'lower': 6, 'abilities': ['strong_1'], 'utility': 2},
       {'lower': 9, 'abilities': ['strong_2'], 'utility': 3, 'utilityUpper': 4}
      ] },
      'strength_mod' : { 'min': 0, 'max': 3, 'initial': 0, 'intervals': [{'lower': 1, 'abilities': ['plus_1']}, {'lower': 2, 'abilities': ['plus_2']}] } }";
    // Load the attribute types.
    AttributeType.LoadString(json);
    // Check that the attribute types were loaded.
    Assert.AreEqual(2, AttributeType.types.Count);
    // Check that the attribute types were loaded correctly.
    Assert.AreEqual(0, AttributeType.types["strength"].minValue);
    Assert.AreEqual(11, AttributeType.types["strength"].maxValue);
    Assert.AreEqual(2, AttributeType.types["strength"].initialValue.baseValue);
    Assert.AreEqual(4, AttributeType.types["strength"].intervals.Count);
    Assert.AreEqual(0, AttributeType.types["strength"].intervals.GetValueAtIndex(0).lower);
    Assert.AreEqual(3, AttributeType.types["strength"].intervals.GetValueAtIndex(0).upper);
    Assert.AreEqual(3, AttributeType.types["strength"].intervals.GetValueAtIndex(1).lower);
    Assert.AreEqual(6, AttributeType.types["strength"].intervals.GetValueAtIndex(1).upper);
    Assert.AreEqual(6, AttributeType.types["strength"].intervals.GetValueAtIndex(2).lower);
    Assert.AreEqual(9, AttributeType.types["strength"].intervals.GetValueAtIndex(2).upper);
    Assert.AreEqual(9, AttributeType.types["strength"].intervals.GetValueAtIndex(3).lower);
    Assert.AreEqual(11, AttributeType.types["strength"].intervals.GetValueAtIndex(3).upper);
    Assert.AreEqual(2, AttributeType.types["strength"].intervals.GetValueAtIndex(0).Abilities.Count);
    Assert.AreEqual(0, AttributeType.types["strength"].intervals.GetValueAtIndex(1).Abilities.Count);
    Assert.AreEqual(2, AttributeType.types["strength"].intervals.GetValueAtIndex(2).Abilities.Count);
    Assert.AreEqual(3, AttributeType.types["strength"].intervals.GetValueAtIndex(3).Abilities.Count);

    Assert.IsTrue(AttributeType.types["strength"].intervals[0].Abilities.Contains(AbilityType.Find("weak_1")!));
    Assert.IsTrue(AttributeType.types["strength"].intervals[0].Abilities.Contains(AbilityType.Find("weak_0")!));

    Person person = new Person("bob", "Bob");
    // Create an attribute.
    AttributeType strength = AttributeType.Find("strength")!;
    person.AddAttribute(strength);
    
    Attribute a = person.attributes.attributes[strength];
    // Check that the attribute was created correctly.
    Assert.AreEqual(2, a.value);
    Assert.AreEqual(0, a.rangeMin);
    Assert.AreEqual(3, a.rangeMax);
    // GetAbilities should contain the ability from the first interval, and it's sub-ability.
    Assert.AreEqual(2, a.Abilities.Count);
    Assert.IsTrue(a.Abilities.Contains(AbilityType.Find("weak_1")!));
    Assert.IsTrue(a.Abilities.Contains(AbilityType.Find("weak_0")!));
    // Set the value to 1 and verify that abilities didn't change.
    Assert.AreEqual(1, a.SetValue(1));
    Assert.AreEqual(0, a.rangeMin);
    Assert.AreEqual(3, a.rangeMax);
    Assert.AreEqual(2, a.Abilities.Count);
    Assert.IsTrue(a.Abilities.Contains(AbilityType.Find("weak_1")!));
    // Set the value to 4 and verify that abilities changed.
    Assert.AreEqual(4, a.SetValue(4));
    Assert.AreEqual(3, a.rangeMin);
    Assert.AreEqual(6, a.rangeMax);
    Assert.AreEqual(0, a.Abilities.Count);
    // Set the value to 7 and verify that abilities changed.
    Assert.AreEqual(7, a.SetValue(7));
    Assert.AreEqual(6, a.rangeMin);
    Assert.AreEqual(9, a.rangeMax);
    Assert.AreEqual(2, a.Abilities.Count);
    Assert.IsTrue(a.Abilities.Contains(AbilityType.Find("strong_1")!));
    // Set the value to 4 and verify that they have no abilities.
    Assert.AreEqual(4, a.SetValue(4));
    Assert.AreEqual(3, a.rangeMin);
    Assert.AreEqual(6, a.rangeMax);
    Assert.AreEqual(4, a.value);
    Assert.AreEqual(0, a.Abilities.Count);
    // Give person a strength_mod attribute.
    person.AddAttribute(AttributeType.Find("strength_mod")!);
    // Check that the strength value didn't change.
    Assert.AreEqual(4, a.value);
    // Check that the strength_mod value is 0.
    Assert.AreEqual(0, person.attributes.attributes[AttributeType.Find("strength_mod")!].value);
    // Set the strength_mod value to 1 and check that the strength value changed.
    Assert.AreEqual(1, person.attributes.attributes[AttributeType.Find("strength_mod")!].SetValue(1));
    Assert.AreEqual(7, a.value);
    // The strength value should have the strong_1 ability.
    Assert.AreEqual(2, a.Abilities.Count);
    Assert.IsTrue(a.Abilities.Contains(AbilityType.Find("strong_1")!));
    // Set the strength_mod value to 2 and check that the strength value changed.
    Assert.AreEqual(2, person.attributes.attributes[AttributeType.Find("strength_mod")!].SetValue(2));
    Assert.AreEqual(10, a.value);
    // The strength value should have the strong_2 ability.
    Assert.AreEqual(3, a.Abilities.Count);
    Assert.IsTrue(a.Abilities.Contains(AbilityType.Find("strong_1")!));
    Assert.IsTrue(a.Abilities.Contains(AbilityType.Find("strong_2")!));
    // Change the strength value to zero and check that it still has the plus_2 modifier of +6.
    Assert.AreEqual(6, a.SetValue(0));
    Assert.AreEqual(2, a.Abilities.Count);
    Assert.IsTrue(a.Abilities.Contains(AbilityType.Find("strong_1")!));

    // Rescale the person's strength attribute.
    a.Rescale(20);
    // Check that the strength value is now 6*20 = 120.
    Assert.AreEqual(120, a.value);
    // Check that the strength value has the strong_1 ability.
    Assert.AreEqual(2, a.Abilities.Count);
    Assert.IsTrue(a.Abilities.Contains(AbilityType.Find("strong_1")!));

    // Add 5 to the strength value.
    Assert.AreEqual(145, a.AddValue(25));
    // Check that the strength value is now 145.
    Assert.AreEqual(145, a.value);

    // Set the strength value to 175
    Assert.AreEqual(175, a.SetValue(175));
    // Check that the strength value is now 175.
    Assert.AreEqual(175, a.value);

    // Try to set the strength value to 75, but it should be clamped to 125.
    Assert.AreEqual(120, a.SetValue(75));
    Assert.AreEqual(145, a.SetValue(145));

    // Rescale it back.
    a.Rescale(1);
    // Check that the strength value is now 145/20 = 7.25.
    Assert.AreEqual(7.25, a.value);

    // Create an AbilityValue with a named value.
    AbilityValue av = new AbilityValue(0);
    av.namedValue = "strength";
    // Check that the value is 7.25
    Assert.AreEqual(7.25, av.GetValue(person));

    // Try it with a scoped set.
    ScopedAttributeSets sas = new ScopedAttributeSets();
    sas.set.AddScopedSet(person.attributes);
    Assert.AreEqual(7.25, av.GetValue(sas));
    // Explicitly override the attribute in sas.
    sas.set.Add(strength);
    Assert.AreEqual(2, av.GetValue(sas));

    // Reset strength_mod to zero.
    Assert.AreEqual(0, person.attributes.attributes[AttributeType.Find("strength_mod")!].SetValue(0));

    // Check utility for values of strength.
    // The utility for values 0-3 should be a sigmoid from 0 to 1,
    // but since it's the first interval, the sigmoid will bottom
    // out at 0.5.
    Assert.AreEqual(0, a.SetValue(0));
    Assert.AreEqual(0.5, a.currentUtility, 0.0001);

    Assert.AreEqual(2.4, a.SetValue(2.4));
    Assert.AreEqual(0.55, a.currentUtility, 0.05);

    Assert.AreEqual(2.99, a.SetValue(2.99));
    Assert.AreEqual(0.99, a.currentUtility, 0.01);

    Assert.AreEqual(3, a.SetValue(3));
    Assert.AreEqual(1.0, a.currentUtility, 0.01);

    Assert.AreEqual(5.4, a.SetValue(3+2.4));
    Assert.AreEqual(1.8, a.currentUtility, 0.01);

    Assert.AreEqual(6, a.SetValue(6));
    Assert.AreEqual(2.0, a.currentUtility, 0.01);

    Assert.AreEqual(6.6, a.SetValue(9 - 2.4));
    Assert.AreEqual(2.45, a.currentUtility, 0.01);

    Assert.AreEqual(8.4, a.SetValue(6 + 2.4));
    Assert.AreEqual(2.55, a.currentUtility, 0.01);

    Assert.AreEqual(9, a.SetValue(9));
    Assert.AreEqual(3.0, a.currentUtility, 0.01);

    // Top interval is only 2 wide, so the sigmoid hits 0.45
    // at 0.4 from the bottom.
    Assert.AreEqual(9.4, a.SetValue(9.4));
    Assert.AreEqual(3.45, a.currentUtility, 0.01);

    Assert.AreEqual(10.99, a.SetValue(11));
    Assert.AreEqual(3.5, a.currentUtility, 0.001);

    Assert.AreEqual(3.5, (double)person.GetNamedValue("utility:strength")!, 0.001);

  }

}