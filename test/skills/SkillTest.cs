using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Abilities;
using Village.Effects;
using Village.Items;
using Village.Persons;
using Village.Skills;
using Village.Tasks;
namespace VillageTest;


[TestClass]
public class SkillUnitTest
{
  // Concrete implementation of the skill context.
  public class ConcreteSkillContext : ConcreteAbilityContext, ISkillContext, IInventoryContext
  {
    // Set of PersonSkills.
    public SkillSet skills;
    // Constructor.
    public ConcreteSkillContext(HashSet<AbilityType> abilities)
      : base(abilities)
    {
      this.skills = new SkillSet(this);
    }

    public void AddItem(Item item, int quantity)
    {
      throw new NotImplementedException();
    }

    public bool RemoveItem(Item item, int quantity)
    {
      throw new NotImplementedException();
    }

    public Dictionary<AbilityType, List<Item>> ItemAbilities()
    {
      throw new NotImplementedException();
    }

    public bool GrantXP(Skill skill, int xp)
    {
      return skills.GrantXP(skill, xp);
    }

    public bool GrantLevel(Skill skill)
    {
      return skills.GrantLevel(skill);
    }

    public bool GrantLevel(Skill skill, int level)
    {
      return skills.GrantLevel(skill, level);
    }

    public int GetLevel(Skill skill)
    {
      return skills.GetLevel(skill);
    }

    public int GetXP(Skill skill)
    {
      return skills.GetXP(skill);
    }
  }

  [TestMethod]
  public void TestLoadSkills()
  {
    {
      ItemType.Clear();
      string json = @"{
    'wood': { 'group': 'RESOURCE', 'displayName': 'Wood'},
    'axe': { 'group': 'TOOL', 'abilities': ['chopping_1'] }
  }";
      // Load the item types.
      ItemType.LoadString(json);
    }
    {
      AbilityType.Clear();
      string json = @"{ 'chopping' : { 'levels': 5 } }";
      // Load the ability types.
      AbilityType.LoadString(json);
    }
    {
      Effect.Clear();
      string json = @"{
    'degrade_1' : { 'target' : 'Item', 'effectType' : 'Degrade', 'config' : {'amount': 1} },
    'skill_woodcraft_100' : { 'target' : 'Person', 'effectType' : 'Skill', 'config' : {'skill': 'woodcraft', 'amount': 100, 'maxLevel': 5} }
  }";
      // Load the effect types.
      EffectLoader.LoadString(json);
    }
    {
      Skill.Clear();
      string json = @"{
        'farming' : [ {'xp': 100, 'requirements' : [], 'abilities': [], 'effects': ['skill_woodcraft_100'] } ],
  'woodcraft' : [ { 'xp': 100, 'requirements' : [], 'abilities': ['chopping_1'], 'effects': [] }, { 'xp': 200, 'requirements' : [], 'abilities': ['chopping_2'], 'effects': [] } ],
  'basic': [ { 'xp': 100}, { 'xp': 200} ]
  }";
      // Load the skills.
      Skill.LoadString(json);
      // Check that the skills were loaded.
      Assert.AreEqual(3, Skill.skills.Count);
      // Check that the skills were loaded correctly.
      Assert.AreEqual(1, Skill.skills["farming"].levels.Count);
      Assert.AreEqual(2, Skill.skills["woodcraft"].levels.Count);
      Assert.AreEqual(2, Skill.skills["basic"].levels.Count);
      // Check that the skill levels were loaded correctly.
      Assert.AreEqual(100, Skill.skills["farming"][0].xp);
      Assert.AreEqual(100, Skill.skills["woodcraft"][0].xp);
      Assert.AreEqual(200, Skill.skills["woodcraft"][1].xp);
      Assert.AreEqual(100, Skill.skills["basic"][0].xp);
      Assert.AreEqual(200, Skill.skills["basic"][1].xp);
      // Check that the skill requirements were loaded correctly.
      Assert.AreEqual(0, Skill.skills["farming"][0].requirements.Count);
      Assert.AreEqual(0, Skill.skills["woodcraft"][0].requirements.Count);
      Assert.AreEqual(0, Skill.skills["woodcraft"][1].requirements.Count);
      Assert.AreEqual(0, Skill.skills["basic"][0].requirements.Count);
      Assert.AreEqual(0, Skill.skills["basic"][1].requirements.Count);
      // Check that the skill abilities were loaded correctly.
      Assert.AreEqual(0, Skill.skills["farming"][0].abilities.Count);
      Assert.AreEqual(1, Skill.skills["woodcraft"][0].abilities.Count);
      Assert.AreEqual(1, Skill.skills["woodcraft"][1].abilities.Count);
      Assert.AreEqual(0, Skill.skills["basic"][0].abilities.Count);
      Assert.AreEqual(0, Skill.skills["basic"][1].abilities.Count);
      Assert.IsTrue(Skill.skills["woodcraft"][0].abilities.Contains(AbilityType.Find("chopping_1")!));
      Assert.IsTrue(Skill.skills["woodcraft"][1].abilities.Contains(AbilityType.Find("chopping_2")!));
      // Check that the skill effects were loaded correctly.
      Assert.AreEqual(1, Skill.skills["farming"][0].effects.Count);
      Assert.AreEqual(0, Skill.skills["woodcraft"][0].effects.Count);
      Assert.AreEqual(0, Skill.skills["woodcraft"][1].effects.Count);
      Assert.AreEqual(0, Skill.skills["basic"][0].effects.Count);
      Assert.AreEqual(0, Skill.skills["basic"][1].effects.Count);

      // Create a Concrete Skill Context
      HashSet<AbilityType> abilityTypes = new HashSet<AbilityType>();
      ConcreteSkillContext context = new ConcreteSkillContext(abilityTypes);
      context.skills.Add(Skill.skills["farming"]);
      context.skills.Add(Skill.skills["woodcraft"]);
      
      // Grant 50 XP in farming.
      Assert.IsTrue(context.GrantXP(Skill.skills["farming"], 50));
      // Check that the XP was granted, but farming is still level 0.
      Assert.AreEqual(50, context.GetXP(Skill.skills["farming"]));
      Assert.AreEqual(0, context.GetLevel(Skill.skills["farming"]));
      // Check that the XP was not granted in woodcraft.
      Assert.AreEqual(0, context.GetXP(Skill.skills["woodcraft"]));
      // Grant 100 XP in farming.
      Assert.IsTrue(context.GrantXP(Skill.skills["farming"], 100));
      // Check that the XP was granted. farming only has one level, so it should be capped at 100.
      Assert.AreEqual(100, context.GetXP(Skill.skills["farming"]));
      // Check that farming leveled up.
      Assert.AreEqual(1, context.GetLevel(Skill.skills["farming"]));
      // Check that the 100 XP was granted in woodcraft when farming leveled up.
      Assert.AreEqual(100, context.GetXP(Skill.skills["woodcraft"]));
      // Check that woodcraft leveled up.
      Assert.AreEqual(1, context.GetLevel(Skill.skills["woodcraft"]));
      // Check that the chopping_1 ability was granted.
      Assert.IsTrue(context.Abilities.Contains(AbilityType.Find("chopping_1")!));
      

      
    }
  }
}