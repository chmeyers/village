using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Abilities;
using Village.Effects;
using Village.Items;
using Village.Persons;
using Village.Tasks;
namespace VillageTest;

[TestClass]
public class PersonUnitTest
{
  [TestMethod]
  public void TestPerson()
  {
    AbilityType.Clear();
    Effect.Clear();
    ItemType.Clear();
    WorkTask.Clear();
    // Load effects, abilities, items, and tasks.
    EffectLoader.LoadString(@"{ 'degrade_1' : { 'target' : 'Item', 'effectType' : 'Degrade', 'config' : {'amount': 1} } }");
    AbilityType.LoadString(@"{ 'chopping' : { 'levels': 10 } }");
    ItemType.LoadString(@"{ 'axe': { 'group': 'TOOL', abilities: ['chopping_2'] }, 'wood': { 'group': 'RESOURCE' } }");
    WorkTask.LoadString(@"{ 'gather_wood': { 'timeCost': 10, 'requirements': ['chopping_1'], 'outputs' : { 'wood' : 100 }, 'effects' : { 'degrade_1': ['chopping_1'] } },
        'sleep': {'timeCost': 0},
        'stack_wood': {'timeCost': 0, 'inputs': {'wood': 100} }
      }");
    var sleepTask = WorkTask.tasks["sleep"];
    var gatherWoodTask = WorkTask.tasks["gather_wood"];
    var stackWoodTask = WorkTask.tasks["stack_wood"];
    // Create a person.
    Person person = new Person("1", "Bob");
    // Check that the person was created.
    Assert.IsNotNull(person);
    // Check that the person has no abilities.
    Assert.AreEqual(0, person.Abilities.Count);
    // Check that sleep ans stack wood are valid tasks.
    Assert.AreEqual(2, person.PotentialTasks.Count);
    Assert.IsTrue(person.PotentialTasks.Contains(sleepTask));
    Assert.IsTrue(person.PotentialTasks.Contains(stackWoodTask));
    // Check that sleep is the only available task.
    Assert.AreEqual(1, person.AvailableTasks.Count);
    Assert.IsTrue(person.AvailableTasks.Contains(sleepTask));
    // Check that the person has no inventory.
    Assert.AreEqual(0, person.Inventory.Count());

    // Add an item to the person's inventory.
    person.AddItem(new Item(ItemType.itemTypes["axe"]), 100);
    // Check that gather wood is a valid task.
    Assert.AreEqual(3, person.PotentialTasks.Count);
    Assert.IsTrue(person.PotentialTasks.Contains(gatherWoodTask));
    // Check that gather wood and sleep are the only available tasks.
    Assert.AreEqual(2, person.AvailableTasks.Count);
    Assert.IsTrue(person.AvailableTasks.Contains(gatherWoodTask));
    Assert.IsTrue(person.AvailableTasks.Contains(sleepTask));
    // Add some wood to the person's inventory.
    person.AddItem(new Item(ItemType.itemTypes["wood"]), 100);
    // Check that stack wood is a valid task.
    Assert.AreEqual(3, person.PotentialTasks.Count);
    Assert.IsTrue(person.PotentialTasks.Contains(WorkTask.tasks["stack_wood"]));
    // Check that stack wood is an available task.
    Assert.AreEqual(3, person.AvailableTasks.Count);
    Assert.IsTrue(person.AvailableTasks.Contains(WorkTask.tasks["stack_wood"]));
  }
}