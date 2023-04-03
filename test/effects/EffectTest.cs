using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Effects;
namespace VillageTest;


[TestClass]
public class EffectUnitTest
{
  [TestMethod]
  public void TestLoadEffects()
  {
    Effect.Clear();
    string json = @"{
  'degrade_1' : { 'target' : 'Item', 'effectType' : 'Degrade', 'config' : {'amount': 1} },
  'skill_cutting_1' : { 'target' : 'Person', 'effectType' : 'Skill', 'config' : {'skill': 'cutting', 'amount': 1, 'maxLevel': 5} }
}";
    // Load the effects.
    Effect.LoadString(json);
    // Check that the effects were loaded.
    Assert.AreEqual(2, Effect.effects.Count);
    // Check that the effects were loaded correctly.
    Assert.AreEqual(EffectType.Degrade, Effect.effects["degrade_1"].effectType);
    Assert.AreEqual(EffectTargetType.Item, Effect.effects["degrade_1"].target);
    // Check that it's a DegradeEffect class.
    Assert.IsInstanceOfType(Effect.effects["degrade_1"], typeof(DegradeEffect));
    // Cast it to a DegradeEffect and check the amount.
    DegradeEffect degradeEffect = (DegradeEffect)Effect.effects["degrade_1"];
    Assert.AreEqual(1, degradeEffect.amount);
    // Check the second effect.
    Assert.AreEqual(EffectType.Skill, Effect.effects["skill_cutting_1"].effectType);
    Assert.AreEqual(EffectTargetType.Person, Effect.effects["skill_cutting_1"].target);
    // Check that it's a SkillEffect class.
    Assert.IsInstanceOfType(Effect.effects["skill_cutting_1"], typeof(SkillEffect));
    // Cast it to a SkillEffect and check the members.
    SkillEffect skillEffect = (SkillEffect)Effect.effects["skill_cutting_1"];
    Assert.AreEqual("cutting", skillEffect.skill);
    Assert.AreEqual(1, skillEffect.amount);
    Assert.AreEqual(5, skillEffect.maxLevel);
  }

}