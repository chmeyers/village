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
    json = @"{ 'strength' : { 'min': 0, 'max': 11, 'initial': 8, 'intervals': [{'lower': 6, 'abilities': ['cutting_3']}, {'lower': 10, 'abilities': ['cutting_4']}] } }";
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
}";
    // Create person.
    Person person = new Person("bob", "Bob");
    // Load the effects.
    EffectLoader.LoadString(json);
    EffectLoader.Initialize();
    // Check that the effects were loaded.
    Assert.AreEqual(5, Effect.effects.Count);
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
    // Strength should be 10.
    Assert.AreEqual(10, person.GetAttributeValue(strength));
    
  }

}