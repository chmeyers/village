using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Abilities;
using Village.Attributes;
using Village.Effects;
using Village.Persons;
using Village.Skills;

namespace VillageTest;


[TestClass]
public class EffectUnitTest
{
  [TestMethod]
  public void TestLoadEffects()
  {
    AbilityType.Clear();
    string json = @"{ 'cutting' : { 'levels': 10 } }";
    // Load the ability types.
    AbilityType.LoadString(json);
    // Load a strength attribute type.
    AttributeType.Clear();
    json = @"{ 'strength' : { 'min': 0, 'max': 11, 'initial': 8, 'intervals': [{'lower': 6, 'abilities': ['cutting_3']}, {'lower': 10, 'abilities': ['cutting_4']}] },
     'bin1' : {'min':0, 'max': 2000, 'initial': 500, 'intervals': [] },
     'bin2' : {'min':0, 'max': 2000, 'initial': 500, 'intervals': [] },
     }";
    // Load the attribute types.
    AttributeType.LoadString(json);
    // Load a swords skill.
    Skill.Clear();
    json = @"{ 'swords' : [ {'xp': 100, 'requirements' : [], 'abilities': [] }, {'xp': 500, 'requirements' : [], 'abilities': [] } ] }";
    // Load the skill types.
    Skill.LoadString(json);
    Effect.Clear();
    json = @"{
  'degrade_1' : { 'target' : 'Item', 'effectType' : 'Degrade', 'config' : {'amount': 1} },
  'skill_swords_1' : { 'target' : 'Person', 'effectType' : 'Skill', 'config' : {'skill': 'swords', 'amount': 1, 'level': 5} },
  'skill_swords_2' : { 'target' : 'Person', 'effectType' : 'Skill', 'config' : {'skill': 'swords', 'amount': {'val' : 2, 'modifiers': {'cutting_1' : {'add' : 1, 'mult': 2.0}, 'cutting_2' : {'add' : 3, 'mult': 5.0}}}, 'level': 5} },
  'pull_strength_10' : { 'target' : 'Person', 'effectType' : 'AttributePuller', 'config' : {'strength':{ 'amount': 1, 'target': 10}} },
  'pull_strength_5' : { 'target' : 'Person', 'effectType' : 'AttributePuller', 'config' : {'strength':{ 'amount': 1, 'target': 5}} },
  'transfer1' : { 'target' : 'Person', 'effectType' : 'AttributeTransfer', 'config' : { 'bin1' : { 'amount': 100, 'dest': 'bin2' } } },
  'transfer2' : { 'target' : 'Person', 'effectType' : 'AttributeTransfer', 'config' : { 'bin1' : { 'amount': 200, 'dest': 'bin2', 'multiplier': 2 } } },
  'transfer3' : { 'target' : 'Person', 'effectType' : 'AttributeTransfer', 'config' : { 'bin1' : { 'amount': 200, 'dest': 'bin2', 'sourceMin': 400, 'destMax': 600 } } },
  'transfer4' : { 'target' : 'Person', 'effectType' : 'AttributeTransfer', 'config' : { 'bin1' : { 'amount': -200, 'dest': 'bin2', 'sourceMin': 400, 'destMax': 600, 'retainOverflow': true, 'multiplier': 2 } } },
}";
    // Create person.
    Person person = new Person("bob", "Bob");
    // Load the effects.
    EffectLoader.LoadString(json);
    EffectLoader.Initialize();
    // Check that the effects were loaded.
    Assert.AreEqual(9, Effect.effects.Count);
    // Check that the effects were loaded correctly.
    Assert.AreEqual(EffectType.Degrade, Effect.effects["degrade_1"].effectType);
    Assert.AreEqual(EffectTargetType.Item, Effect.effects["degrade_1"].target);
    // Check that it's a DegradeEffect class.
    Assert.IsInstanceOfType(Effect.effects["degrade_1"], typeof(DegradeEffect));
    // Cast it to a DegradeEffect and check the amount.
    DegradeEffect degradeEffect = (DegradeEffect)Effect.effects["degrade_1"];
    Assert.AreEqual(1, degradeEffect.amount.GetBaseValue());
    // Check the second effect.
    Assert.AreEqual(EffectType.Skill, Effect.effects["skill_swords_1"].effectType);
    Assert.AreEqual(EffectTargetType.Person, Effect.effects["skill_swords_1"].target);
    // Check that it's a SkillEffect class.
    Assert.IsInstanceOfType(Effect.effects["skill_swords_1"], typeof(SkillEffect));
    // Cast it to a SkillEffect and check the members.
    SkillEffect skillEffect = (SkillEffect)Effect.effects["skill_swords_1"];
    Assert.AreEqual("swords", skillEffect.skill);
    Assert.AreEqual(1, skillEffect.amount.GetBaseValue());
    Assert.AreEqual(5, skillEffect.level.GetBaseValue());

    // Create an AbilityType hashset with cutting_1.
    HashSet<AbilityType> abilityTypes = new HashSet<AbilityType>();
    abilityTypes.Add(AbilityType.abilityTypes["cutting_1"]);
    ConcreteAbilityContext context = new ConcreteAbilityContext(abilityTypes);

    Assert.AreEqual(1, skillEffect.amount.GetValue(context));
    Assert.AreEqual(5, skillEffect.level.GetValue(context));

    // Check the third effect.
    Assert.AreEqual(EffectType.Skill, Effect.effects["skill_swords_2"].effectType);
    Assert.AreEqual(EffectTargetType.Person, Effect.effects["skill_swords_2"].target);
    // Check that it's a SkillEffect class.
    Assert.IsInstanceOfType(Effect.effects["skill_swords_2"], typeof(SkillEffect));
    // Cast it to a SkillEffect and check the members.
    skillEffect = (SkillEffect)Effect.effects["skill_swords_2"];
    Assert.AreEqual("swords", skillEffect.skill);
    Assert.AreEqual(2, skillEffect.amount.GetBaseValue());
    Assert.AreEqual(5, skillEffect.level.GetBaseValue());

    // With the context, the amount should be (2+1)*2 = 6
    Assert.AreEqual(6, skillEffect.amount.GetValue(context));
    Assert.AreEqual(5, skillEffect.level.GetValue(context));

    // Add cutting_2 to the context.
    abilityTypes.Add(AbilityType.abilityTypes["cutting_2"]);
    // With the context, the amount should be (2+1+3)*2*5 = 60
    Assert.AreEqual(60, skillEffect.amount.GetValue(context));
    Assert.AreEqual(5, skillEffect.level.GetValue(context));
    // Apply the effect to person.
    skillEffect.ApplySync(new ChosenEffectTarget(EffectTargetType.Person, person, person, person));
    // Check that the skill was added.
    Skill swords = Skill.Find("swords")!;
    Assert.AreEqual(0, person.GetLevel(swords));
    // Check that the skill was added correctly.
    Assert.AreEqual(4, person.GetXP(swords));
    // Apply as a batch.
    skillEffect.ApplySync(new ChosenEffectTarget(EffectTargetType.Person, person, person, person), 10, 5);
    // Check that the skill xp changed.
    Assert.AreEqual(204, person.GetXP(swords));
    skillEffect.ApplySync(new ChosenEffectTarget(EffectTargetType.Person, person, person, person), 100, 5);
    // XP should max out at 100+500 = 600.
    Assert.AreEqual(600, person.GetXP(swords));

    // Check the AttributePuller effects.
    Assert.AreEqual(EffectType.AttributePuller, Effect.effects["pull_strength_10"].effectType);
    Assert.AreEqual(EffectTargetType.Person, Effect.effects["pull_strength_10"].target);
    // Check that it's a AttributePullerEffect class.
    Assert.IsInstanceOfType(Effect.effects["pull_strength_10"], typeof(AttributePullerEffect));

    AttributeType strength = AttributeType.Find("strength")!;
    // Initial value of strength is 8.
    Assert.AreEqual(8, person.GetAttributeValue(strength));
    
    AttributePullerEffect pull10 = (AttributePullerEffect)Effect.effects["pull_strength_10"];
    // Pull strength to 10.
    ChosenEffectTarget target = new ChosenEffectTarget(EffectTargetType.Person, person, person, person);
    pull10.ApplySync(target);
    // Strength should be 9.
    Assert.AreEqual(9, person.GetAttributeValue(strength));
    // Pull strength to 10 again.
    pull10.ApplySync(target);
    // Strength should be 10.
    Assert.AreEqual(10, person.GetAttributeValue(strength));
    // Pull strength to 10 again.
    pull10.ApplySync(target);
    // Strength should still be 10.
    Assert.AreEqual(10, person.GetAttributeValue(strength));
    // Now pull strength to 5.
    AttributePullerEffect pull5 = (AttributePullerEffect)Effect.effects["pull_strength_5"];
    pull5.ApplySync(target);
    // Strength should be 9.
    Assert.AreEqual(9, person.GetAttributeValue(strength));
    // Pull strength to a bunch more in a loop.
    for (int i = 0; i < 100; i++) {
      pull5.ApplySync(target);
    }
    // Strength should be 5.
    Assert.AreEqual(5, person.GetAttributeValue(strength));
    // Batch apply the pull10 effect.
    pull10.ApplySync(target, 3, 1);
    // Strength should be 8.
    Assert.AreEqual(8, person.GetAttributeValue(strength));
    pull10.ApplySync(target, 100, 1);
    // Strength should be 11 - epsilon.
    Assert.AreEqual(10.99, person.GetAttributeValue(strength));
    
    // Test AttributeTransfer Effects.
    Assert.AreEqual(EffectType.AttributeTransfer, Effect.effects["transfer1"].effectType);
    Assert.AreEqual(EffectTargetType.Person, Effect.effects["transfer1"].target);
    // Check that it's a AttributeTransferEffect class.
    Assert.IsInstanceOfType(Effect.effects["transfer1"], typeof(AttributeTransferEffect));
    AttributeTransferEffect transfer1 = (AttributeTransferEffect)Effect.effects["transfer1"];
    // Transfer 100 from bin1 to bin2.
    transfer1.ApplySync(target);
    // bin1 should be 400.
    Assert.AreEqual(400, person.GetAttributeValue(AttributeType.Find("bin1")!));
    // bin2 should be 600.
    Assert.AreEqual(600, person.GetAttributeValue(AttributeType.Find("bin2")!));
    // Transfer 200 from bin1 to bin2.
    AttributeTransferEffect transfer2 = (AttributeTransferEffect)Effect.effects["transfer2"];
    transfer2.ApplySync(target);
    // bin1 should be 200.
    Assert.AreEqual(200, person.GetAttributeValue(AttributeType.Find("bin1")!));
    // bin2 should be 1000.
    Assert.AreEqual(1000, person.GetAttributeValue(AttributeType.Find("bin2")!));
    // Try to transfer 200 from bin1 to bin2 with min and max set.
    AttributeTransferEffect transfer3 = (AttributeTransferEffect)Effect.effects["transfer3"];
    transfer3.ApplySync(target);
    // Neither should have changed.
    Assert.AreEqual(200, person.GetAttributeValue(AttributeType.Find("bin1")!));
    Assert.AreEqual(1000, person.GetAttributeValue(AttributeType.Find("bin2")!));

    // Reset them both back to 450 and try again.
    person.SetAttribute(AttributeType.Find("bin1")!, 450);
    person.SetAttribute(AttributeType.Find("bin2")!, 450);
    transfer3.ApplySync(target);
    // bin1 should have hit the min at 400.
    Assert.AreEqual(400, person.GetAttributeValue(AttributeType.Find("bin1")!));
    // bin2 should have gotten 50 from bin1
    Assert.AreEqual(500, person.GetAttributeValue(AttributeType.Find("bin2")!));

    // Reset them both back to 550 and try again.
    person.SetAttribute(AttributeType.Find("bin1")!, 550);
    person.SetAttribute(AttributeType.Find("bin2")!, 550);
    transfer3.ApplySync(target);
    // bin1 should have hit the min at 400.
    Assert.AreEqual(400, person.GetAttributeValue(AttributeType.Find("bin1")!));
    // bin2 should have gotten 50 from bin1, the overflow was discarded.
    Assert.AreEqual(600, person.GetAttributeValue(AttributeType.Find("bin2")!));

    person.SetAttribute(AttributeType.Find("bin1")!, 300);
    person.SetAttribute(AttributeType.Find("bin2")!, 700);
    AttributeTransferEffect transfer4 = (AttributeTransferEffect)Effect.effects["transfer4"];
    transfer4.ApplySync(target);
    // bin2 should have hit the max at 600.
    Assert.AreEqual(600, person.GetAttributeValue(AttributeType.Find("bin2")!));
    // bin1 should have only given 50 to bin2, the overflow was retained.
    Assert.AreEqual(350, person.GetAttributeValue(AttributeType.Find("bin1")!));
    

  }

}