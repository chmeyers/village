using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Abilities;
using Village.Effects;
using Village.Items;
using Village.Persons;
using Village.Tasks;
namespace VillageTest;


[TestClass]
public class TaskUnitTest
{
  [TestMethod]
  public void TestLoadTasks()
  {
    {
      AbilityType.Clear();
      string json = @"{ 'chopping' : { 'levels': 3 } }";
      // Load the ability types.
      AbilityType.LoadString(json);
    }
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
      string json = @"{ 'chopping' : { 'levels': 4 } }";
      // Load the ability types.
      AbilityType.LoadString(json);
    }
    {
      Effect.Clear();
      string json = @"{
    'degrade_1' : { 'target' : 'Item', 'effectType' : 'Degrade', 'config' : {'amount': 1} },
    'skill_chopping_1' : { 'target' : 'Person', 'effectType' : 'Skill', 'config' : {'skill': 'chopping', 'amount': 1, 'maxLevel': 5} }
  }";
      // Load the item types.
      EffectLoader.LoadString(json);
    }
    {
      WorkTask.Clear();
      string json = @"{
    'gather_wood': { 'timeCost': 10, 'requirements': ['chopping_1'], 'repeatable': true, 'outputs' : { 'wood' : 100 }, 'effects' : { 'skill_chopping_1' : [''], 'degrade_1': ['chopping_1'] } },
    'teach_chopping_1': { 'timeCost': 10, 'requirements': ['chopping_2'],  'effects' : { 'skill_chopping_1' : ['@1'] } },
    'gather_wood2': { 'timeCost': 10, 'requirements': ['chopping_3'], 'inputs' : { 'wood' : {'val' : 50 } } },
      }";
      // Load the tasks.
      WorkTask.LoadString(json);
      // Check that the tasks were loaded.
      Assert.AreEqual(3, WorkTask.tasks.Count);
      // Check that the tasks were loaded correctly.
      Assert.AreEqual(10, WorkTask.tasks["gather_wood"].timeCost);
      Assert.AreEqual(10, WorkTask.tasks["teach_chopping_1"].timeCost);
      // Check that the requirements were loaded correctly.
      Assert.AreEqual(1, WorkTask.tasks["gather_wood"].requirements.Count);
      Assert.AreEqual(1, WorkTask.tasks["teach_chopping_1"].requirements.Count);
      Assert.AreEqual("chopping_1", WorkTask.tasks["gather_wood"].requirements[0].abilityType);
      Assert.AreEqual("chopping_2", WorkTask.tasks["teach_chopping_1"].requirements[0].abilityType);
      // Check that the outputs were loaded correctly.
      Assert.AreEqual(1, WorkTask.tasks["gather_wood"].outputs.Count);
      Assert.AreEqual(100, WorkTask.tasks["gather_wood"].outputs[ItemType.Find("wood")!]);
      // Check that the effects were loaded correctly.
      Assert.AreEqual(2, WorkTask.tasks["gather_wood"].effects.Count);
      Assert.AreEqual(1, WorkTask.tasks["teach_chopping_1"].effects.Count);
      Assert.AreEqual(1, WorkTask.tasks["gather_wood"].effects[Effect.Find("skill_chopping_1")!].Count);
      Assert.AreEqual(EffectTargetType.Person, WorkTask.tasks["gather_wood"].effects[Effect.Find("skill_chopping_1")!][0].effectTargetType);
      // Check the tasks by ability index.
      Assert.AreEqual(2, WorkTask.tasksByAbility["chopping_2"].Count);
      // chopping_2 should have gather_wood and teach_chopping_1.
      Assert.IsTrue(WorkTask.tasksByAbility["chopping_2"].Contains(WorkTask.tasks["gather_wood"]));
      Assert.IsTrue(WorkTask.tasksByAbility["chopping_2"].Contains(WorkTask.tasks["teach_chopping_1"]));
      // chopping_1 should have gather_wood.
      Assert.AreEqual(1, WorkTask.tasksByAbility["chopping_1"].Count);
      Assert.IsTrue(WorkTask.tasksByAbility["chopping_1"].Contains(WorkTask.tasks["gather_wood"]));
      // Check that gather_wood2 was loaded correctly.
      Assert.AreEqual(1, WorkTask.tasks["gather_wood2"].inputs.Count);
      Assert.AreEqual(50, WorkTask.tasks["gather_wood2"].inputs[ItemType.Find("wood")!].GetBaseValue());

      HashSet<AbilityType> abilityTypes = new HashSet<AbilityType>();
      abilityTypes.Add(AbilityType.abilityTypes["chopping_1"]);
      ConcreteAbilityContext context = new ConcreteAbilityContext(abilityTypes);

      Assert.AreEqual(50, WorkTask.tasks["gather_wood2"].Inputs(context)[ItemType.Find("wood")!]);

      // Create a Person to run tasks on.
      Person person = new Person("bob", "Bob");
      // Can't perform task because person doesn't have the ability.
      Assert.IsFalse(TaskRunner.PerformTask(person, WorkTask.tasks["gather_wood"], null));
      // Person should have no wood.
      Assert.IsFalse(person.Inventory.Contains(new Dictionary<ItemType, int> { { ItemType.Find("wood")!, 1 } }));
      // Give them two axes.
      Item axe = new Item(ItemType.Find("axe")!);
      person.AddItem(axe, 200);
      var axes = person.Inventory[ItemType.Find("axe")!];
      Assert.AreEqual(1, axes.Count);
      Assert.AreEqual(100, axes.First().Key.quality);
      Assert.AreEqual(200, axes.First().Value);
      // Try to perform the task again
      Assert.IsTrue(TaskRunner.PerformTask(person, WorkTask.tasks["gather_wood"], null));
      // Person should have 100 wood.
      Assert.IsTrue(person.Inventory.Contains(new Dictionary<ItemType, int> { { ItemType.Find("wood")!, 100 } }));
      // Person should have one axe with full quality, and one degrade axe.
      axes = person.Inventory[ItemType.Find("axe")!];
      Assert.AreEqual(2, axes.Count);
      // The lower quality one should be sorted first.
      Assert.AreEqual(99, axes.First().Key.quality);
      Assert.AreEqual(100, axes.First().Value);
      Assert.AreEqual(100, axes.Last().Key.quality);
      Assert.AreEqual(100, axes.Last().Value);
      // Try to perform the task again
      Assert.IsTrue(TaskRunner.PerformTask(person, WorkTask.tasks["gather_wood"], null));
      // Person should have 200 wood.
      Assert.IsTrue(person.Inventory.Contains(new Dictionary<ItemType, int> { { ItemType.Find("wood")!, 200 } }));
      // Person should have one axe with full quality, and one axe degraded two times.
      axes = person.Inventory[ItemType.Find("axe")!];
      Assert.AreEqual(2, axes.Count);
      // The lower quality one should be sorted first.
      Assert.AreEqual(98, axes.First().Key.quality);
      Assert.AreEqual(100, axes.First().Value);
      Assert.AreEqual(100, axes.Last().Key.quality);
      Assert.AreEqual(100, axes.Last().Value);

    }
  }
}