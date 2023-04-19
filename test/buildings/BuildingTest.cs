using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Abilities;
using Village.Buildings;
using Village.Effects;
using Village.Households;
using Village.Persons;
using Village.Tasks;
namespace VillageTest;


[TestClass]
public class BuildingUnitTest
{
  [TestMethod]
  public void TestBuilding()
  {
    {
      AbilityType.Clear();
      string json = @"{
        'ability_1' : { },
        'ability_2' : { },
        'ability_3' : { },
        'ability_4' : { },
        'construction_phase_phase_1' : { },
        'construction_phase_phase_2' : { },
      }";
      // Load the ability types.
      AbilityType.LoadString(json);
    }
    {
      Effect.Clear();
      string json = @"{
        'build_component_1' : { 'target' : 'Building', 'effectType' : 'BuildingComponent', 'config' : {'component': 'component_1'} },
        'build_component_2' : { 'target' : 'Building', 'effectType' : 'BuildingComponent', 'config' : {'component': 'component_2'} },
        'build_component_3' : { 'target' : 'Building', 'effectType' : 'BuildingComponent', 'config' : {'component': 'component_3'} },
        'build_component_4' : { 'target' : 'Building', 'effectType' : 'BuildingComponent', 'config' : {'component': 'component_4'} },
      }";
      EffectLoader.LoadString(json);
    }
    {
      WorkTask.Clear();
      // We need tasks that can be used to create each of the components.
      string json = @"{
        'task_1' : { 'timeCost': 0, 'requirements': ['construction_phase_phase_1'], 'effects' : { 'build_component_1' : ['@1'] } },
        'task_2' : { 'timeCost': 0, 'requirements': ['construction_phase_phase_1'], 'effects' : { 'build_component_2' : ['@1'] } },
        'task_3' : { 'timeCost': 0, 'requirements': ['construction_phase_phase_2'], 'effects' : { 'build_component_3' : ['@1'] } },
        'task_4' : { 'timeCost': 0, 'requirements': ['construction_phase_phase_2'], 'effects' : { 'build_component_4' : ['@1'] } },
      }";
      WorkTask.LoadString(json);
    }
    BuildingType.Clear();
    string data = @"{
      'test_building': {
        'abilities': [ 'ability_1', 'ability_2' ],
        'requirements': [ 'ability_3', 'ability_4' ],
        'phases': {
          'phase_1': ['component_1','component_2'],
          'phase_2': ['component_3','component_4']
        }
      }
    }";
    BuildingType.LoadString(data);
    // Building Type dictionary should now contain a single entry.
    Assert.AreEqual(1, BuildingType.buildingTypes.Count());
    // Get the building type.
    BuildingType? testBuildingType = BuildingType.Find("test_building");
    Assert.IsNotNull(testBuildingType);
    // Create a household.
    Household household = new Household();
    // Create a Building and build all it's phases.
    household.AddBuilding(testBuildingType);
    Building building = household.buildings[0];
    // Building should be in phase 1.
    Assert.AreEqual("phase_1", building.currentPhase);
    // The only ability the building should provide is the one for the phase.
    Assert.AreEqual(1, building.abilities.Count());
    Assert.IsTrue(building.abilities.Contains("construction_phase_phase_1"));
    // The building should have no components.
    Assert.AreEqual(0, building.completedComponents.Count());

    // Create a person to run tasks.
    Person person = new Person("bob", "Bob");
    // Run task_1, which should build component_1.
    WorkTask? task1 = WorkTask.Find("task_1");
    Assert.IsNotNull(task1);
    Dictionary<string, ChosenEffectTarget> targets = new Dictionary<string, ChosenEffectTarget>();
    // The target is the building.
    targets.Add("@1", new ChosenEffectTarget(EffectTargetType.Building, building, household, person));
    // The person can't perform the task because they don't have the ability.
    Assert.IsFalse(TaskRunner.PerformTask(person, person, task1, targets, true));
    // The person can perform the task if they have the ability.
    person.GrantAbility("construction_phase_phase_1");
    Assert.IsTrue(TaskRunner.PerformTask(person, person, task1, targets, true));
    // The building should now have component_1.
    Assert.AreEqual(1, building.completedComponents.Count());
    Assert.IsTrue(building.completedComponents.Contains("component_1"));
    // The building should still be in phase 1.
    Assert.AreEqual("phase_1", building.currentPhase);
    
    // Run task_2, which should build component_2.
    WorkTask? task2 = WorkTask.Find("task_2");
    Assert.IsNotNull(task2);
    // The person can perform the task since they have the ability.
    Assert.IsTrue(TaskRunner.PerformTask(person, person, task2, targets, true));
    // The building should now have component_2.
    Assert.AreEqual(2, building.completedComponents.Count());
    Assert.IsTrue(building.completedComponents.Contains("component_2"));
    // The building should now be in phase 2.
    Assert.AreEqual("phase_2", building.currentPhase);
    // The building should now provide the ability for phase 2.
    Assert.AreEqual(1, building.abilities.Count());
    Assert.IsTrue(building.abilities.Contains("construction_phase_phase_2"));

    // For phase 2, run tasks out of order, to ensure it works.
    // Run task_4, which should build component_4.
    WorkTask? task4 = WorkTask.Find("task_4");
    Assert.IsNotNull(task4);
    // Grant the ability.
    person.GrantAbility("construction_phase_phase_2");
    Assert.IsTrue(TaskRunner.PerformTask(person, person, task4, targets, true));
    // The building should now have component_4.
    Assert.AreEqual(3, building.completedComponents.Count());
    Assert.IsTrue(building.completedComponents.Contains("component_4"));
    // The building should still be in phase 2.
    Assert.AreEqual("phase_2", building.currentPhase);
    // The building should still provide the ability for phase 2.
    Assert.AreEqual(1, building.abilities.Count());
    Assert.IsTrue(building.abilities.Contains("construction_phase_phase_2"));

    // Run task_3, which should build component_3.
    WorkTask? task3 = WorkTask.Find("task_3");
    Assert.IsNotNull(task3);
    Assert.IsTrue(TaskRunner.PerformTask(person, person, task3, targets, true));
    // The building should now have component_3.
    Assert.AreEqual(4, building.completedComponents.Count());
    Assert.IsTrue(building.completedComponents.Contains("component_3"));
    // The building should now be complete.
    Assert.IsTrue(building.completed);
    // The building should now provide it's completed abilities.
    Assert.AreEqual(2, building.abilities.Count());
    Assert.IsTrue(building.abilities.Contains("ability_1"));
    Assert.IsTrue(building.abilities.Contains("ability_2"));
  }
}