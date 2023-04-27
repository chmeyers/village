using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Abilities;
using Village.Effects;
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
    Effect.Clear();
    json = @"{
  'degrade_1' : { 'target' : 'Item', 'effectType' : 'Degrade', 'config' : {'amount': 1} },
  'skill_swords_1' : { 'target' : 'Person', 'effectType' : 'Skill', 'config' : {'skill': 'swords', 'amount': 1, 'level': 5} },
  'skill_swords_2' : { 'target' : 'Person', 'effectType' : 'Skill', 'config' : {'skill': 'swords', 'amount': {'val' : 2, 'modifiers': {'cutting_1' : {'add' : 1, 'mult': 2.0}, 'cutting_2' : {'add' : 3, 'mult': 5.0}}}, 'level': 5} }
}";
    // Load the effects.
    EffectLoader.LoadString(json);
    // Check that the effects were loaded.
    Assert.AreEqual(3, Effect.effects.Count);
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
  }

}