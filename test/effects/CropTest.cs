using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Abilities;
using Village.Attributes;
using Village.Base;
using Village.Buildings;
using Village.Effects;
using Village.Households;
using Village.Items;
using Village.Persons;
using Village.Skills;
using Village.Tasks;

namespace VillageTest;


[TestClass]
public class CropUnitTest
{

  public void RunCrop(Field field, ItemType crop, AttributeType cropAttribute, uint days, uint batchSize, bool print = false)
  {
    while(field.Count(crop) > 0 && field.GetUnscaledValue(crop, cropAttribute) < days)
    {
      Calendar.Advance(batchSize);
      WeatherAttributes.AdvanceWeather();
      field.Advance();

      if (print && field.GetUnscaledValue(crop, AttributeType.Find("crop_wheat_growing")!)% 10 == 0)
      {
        foreach (var attribute in field.state.attributes)
        {
          Console.WriteLine("{0}: {1}", attribute.Key.name, attribute.Value.value);
        }
        Console.WriteLine("Health: {0}", field.GetValue(crop, AttributeType.Find("crop_health")!));
        Console.WriteLine("Yield: {0}", field.GetValue(crop, AttributeType.Find("crop_yield")!));
        Console.WriteLine("Day: {0}", field.GetUnscaledValue(crop, AttributeType.Find("crop_wheat_growing")!));
        Console.WriteLine();
      }
    }
  }

  public void Advance(Field field, Person person, uint days)
  {
    for (int i = 0; i < days * 10; i++)
    {
      Calendar.Advance(1);
      WeatherAttributes.AdvanceWeather();
      field.Advance();
      TaskRunner.AdvanceTask(person);
      person.attributes.Advance();
    }
  }

  [TestMethod]
  public void TestField()
  {
    {
      AbilityType.Clear();
      string json = @"{
        'dry_surface_soil' : { },
        'wet_surface_soil' : { },
        'low_weeds' : { },
        'mid_weeds' : { },
        'high_weeds' : { },
        'low_deep_moisture' : { },
        'high_deep_moisture' : { },
        'low_nitrogen' : { },
        'high_nitrogen' : { },
        'low_phosphorus' : { },
        'high_phosphorus' : { },
        'low_potassium' : { },
        'high_potassium' : { },
        'harvestable' : { },
        'weeding' : { levels: 8 },
        'plowing' : { levels: 8 },
        'planting' : { levels: 8 },
        'harvesting' : { levels: 8 },
        'cereals' : { levels: 8 },
        'hoe' : { levels: 8 },
        'plow' : { levels: 8 },
        'sickle' : { levels: 8 },
      }";
      // Load the ability types.
      AbilityType.LoadString(json);
    }
    {
      ItemType.Clear();
      string json = """
{
  "straw": { "group": "RESOURCE"},
  "food": { "group": "FOOD"},
  "wheat": { "group": "FOOD", "parents" : ["food"], "weight": 0.5, "cropSettings": {"cropSkill": "cereals", "cropSkillLevel": 4, "minSoilQuality": 5, "minPlantingTemp": 40, "frostTolerance": 30, "heatTolerance": 85, "droughtTolerance": 0.5, "weedSusceptibleDays": 20, "initDays": 20, "devDays": 25, "midDays": 60, "lateDays": 30, "kcInit": 0.3, "kcMid": 1.15, "kcEnd": 0.25, "perTickYieldGrowth": 0.4444, "targetYieldPerAcre": 600, "seedPerAcre": 150, "hasHarvestableStraw": true, "nitrogenPerYield": 0.025, "phosphorusPerYield": 0.004142, "potassiumPerYield": 0.004565, "strawPerYield": 1.417, "nitrogenPerStraw": 0.0085, "phosphorusPerStraw": 0.000807, "potassiumPerStraw": 0.012035, "temperatePlantingMonths": [0,1], "harvestItems": { "wheat" : 1 , "straw": 1.417 }, "cropAttribute": "crop_wheat_growing"} },
}
""";
      // Load the item types.
      ItemType.LoadString(json);
    }
    {
      // Load effects.
      EffectLoader.Clear();
      string json = """
      {
"drain_update" : { "target": "Field", "effectType": "AttributeTransfer", "config": { "surface_moisture" : { "sourceMin":1, "amount": {"val": "drainage", "prescaled": true}, "dest": "deep_moisture", "destMax": { "val": "soil_quality", "prescaled": true}}, } },
"field_changes" : { "target": "Field", "effectType": "AttributeAdder", "config": { "soil_quality" : { "amount": 0.0001}, "nitrogen" : { "amount": { "val": "soil_quality", "add": 2.5, "mult": 0.00052, "prescaled": true} }, "phosphorus" : { "amount": 0.000972 }, "potassium" : { "amount": { "val": "soil_quality", "mult": 0.000925, "prescaled": true} } } },
"field_maintenance" : { "target": "Field", "effectType": "FieldMaintenance", "config": {  } },
"grow_crop" : { "target": "Crop", "effectType": "GrowCrop", "config": {  } },
"rotting" : { "target": "Crop", "effectType": "RotCrop", "config": { "rotRate" : { "val": 0.003, "modifiers": {"wet_surface_soil": {"mult": 2}}}, } },
"kill_crop" : { "target": "Crop", "effectType": "KillCrop", "config": {  } },
"plant_wheat" : { "target": "Field", "effectType": "PlantCrop", "config": { "crop" : "wheat" } },
"harvest_wheat" : { "target": "Crop", "effectType": "HarvestCrop", "config": { "crop" : "wheat" } },
"plow_under" : { "target": "Field", "effectType": "KillCrop", "config": {  } },
"plow" : { "target": "Field", "effectType": "AttributePuller", "config": { "soil_quality" : { "target": {"val": "plowing", "add": 1 }, "amount": 0.3}, "weeds" : { "target": 0, "amount": {"val": "plowing", "add": 2, "mult": 20 }}, } },
"weed" : { "target": "Field", "effectType": "AttributeAdder", "config": { "weeds" : { "target": { "val": "weeding", "add": -10, "mult": -0.25}, "amount": {"val": "weeding", "add": 10, "mult": 0.3 }}, } },
"minor_touch_crop" : { "target": "Crop", "effectType": "TouchCrop", "config": { "healthRate" : 0.2, } },
"major_touch_crop" : { "target": "Crop", "effectType": "TouchCrop", "config": { "healthRate" : 5.0, } },
"minor_touch_field" : { "target": "Field", "effectType": "TouchCrop", "config": { "healthRate" : 0.2, } },
"major_touch_field" : { "target": "Field", "effectType": "TouchCrop", "config": { "healthRate" : 5.0, } },
"minor_learn_crop" : { "target": "Crop", "effectType": "CropSkill", "config": { "amount": 1, } },
"major_learn_crop" : { "target": "Crop", "effectType": "CropSkill", "config": { "amount": 5, } },
"minor_learn_field" : { "target": "Field", "effectType": "CropSkill", "config": { "amount": 1, } },
"major_learn_field" : { "target": "Field", "effectType": "CropSkill", "config": { "amount": 5, } },
"degrade_1" : { "target": "Item", "effectType": "Degrade", "config": { "amount": 1 } },
"skill_weeding_3" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "weeding", "level": 3} },
"skill_harvesting_3" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "harvesting", "level": 3} },
"skill_planting_3" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "planting", "level": 3} },
"skill_plowing_3" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "plowing", "level": 3} },
      }
""";
      EffectLoader.LoadString(json);
    }
    {
      AttributeType.Clear();
      string json = """
      {
"field" : { "min": 0, "max": 1, "group": "field" , "initial": 0, "intervals": [{"lower": 0, "abilities": [], "ongoing_effects": ["field_changes","drain_update","field_maintenance"]}]},
"surface_moisture" : { "min": 0, "max": 2, "group": "field" , "initial": 1, "intervals": [{"lower": 0, "abilities": ["dry_surface_soil"]},{"lower": 0.05, "abilities": ["wet_surface_soil"]}]},
"deep_moisture" : { "min": 0, "max": 10, "group": "field" , "initial": 4, "intervals": [{"lower": 0, "abilities": ["low_deep_moisture"]},{"lower": 0.1, "abilities": ["high_deep_moisture"]}]},
"drainage" : { "min": 0.2, "max": 2, "group": "field" , "initial": 1, "intervals": [{"lower": 0.2, "abilities": []}]},
"soil_quality" : { "min": 1, "max": 7.5, "group": "field" , "initial": 4, "intervals": [{"lower": 1, "abilities": []},{"lower": 4, "abilities": []}]},
"nitrogen" : { "min": 0, "max": 500, "group": "field" , "initial": 100, "intervals": [{"lower": 0, "abilities": ["low_nitrogen"]},{"lower": 5, "abilities": []}]},
"phosphorus" : { "min": 0, "max": 1000, "group": "field" , "initial": 150, "intervals": [{"lower": 0, "abilities": ["low_phosphorus"]},{"lower": 5, "abilities": []}]},
"potassium" : { "min": 0, "max": 10000, "group": "field" , "initial": 5000, "intervals": [{"lower": 0, "abilities": ["low_potassium"]},{"lower": 50, "abilities": []}]},
"weeds" : { "min": 0, "max": 100, "group": "field" , "initial": 100, "intervals": [{"lower": 0, "abilities": ["low_weeds"]},{"lower": 10, "abilities": ["mid_weeds"]},{"lower": 20, "abilities": ["high_weeds"]}]},
"crop_health" : { "min": 0, "max": 100, "group": "crop" , "initial": 100, "intervals": [{"lower": 0, "abilities": []},{"lower": 10, "abilities": []}]},
"crop_yield" : { "min": 0, "max": 100000, "group": "crop" , "initial": 0, "intervals": [{"lower": 0, "abilities": []}]},
"crop_wheat_growing": { "min": 0, "max": 190, "changePerTick": 0.1, "initial": 0, "intervals": [{"lower": 0, "ongoing_effects": ["grow_crop"]}, {"lower": 130, "abilities": ["harvestable"], "ongoing_effects": ["grow_crop"]}, {"lower": 135, "abilities": ["harvestable"]}, {"lower": 155, "ongoing_effects": ["rotting"]}, {"lower": 185, "entry_effects": ["kill_crop"]} ]},
  "weekly_high" : { "min": -110, "max": 212, "group": "weather" , "initial": 60, "intervals": []},
  "weekly_low" : { "min": -110, "max": 212, "group": "weather" , "initial": 40, "intervals": [{"lower": -110, "abilities": [], "entry_effects": []}, {"lower": 33}]},
  "weekly_tick_et" : { "min": 0, "max": 0.1, "group": "weather" , "initial": 0.015, "intervals": []},
  "seasonal_growth" : { "min": 0, "max": 5, "group": "weather" , "initial": 1, "intervals": []},
  "weekly_sun" : { "min": 0, "max": 120, "group": "weather" , "initial": 0, "intervals": []},
  "weekly_rain" : { "min": 0, "max": 200, "group": "weather" , "initial": 0, "intervals": []},
      }
""";
      // Load the attributes.
      AttributeType.LoadString(json);
    }
    {
      BuildingType.Clear();
      string data = @"{ 'field': {} }";
      BuildingType.LoadString(data);
    }
    {
      Skill.Clear();
      string json = """
{
  "cereals" : [ {"xp": 100, "requirements": [], "abilities": ["cereals_1"] },{"xp": 200, "requirements": [], "abilities": ["cereals_2"] },{"xp": 400, "requirements": [], "abilities": ["cereals_3"] },{"xp": 800, "requirements": [], "abilities": ["cereals_4"] },{"xp": 1600, "requirements": [], "abilities": ["cereals_5"] },{"xp": 3200, "requirements": [], "abilities": ["cereals_6"] } ],
  "harvesting" : [ {"xp": 100, "requirements": [], "abilities": ["harvesting_1"] },{"xp": 200, "requirements": [], "abilities": ["harvesting_2"] },{"xp": 400, "requirements": [], "abilities": ["harvesting_3"] },{"xp": 800, "requirements": [], "abilities": ["harvesting_4"] },{"xp": 1600, "requirements": [], "abilities": ["harvesting_5"] },{"xp": 3200, "requirements": [], "abilities": ["harvesting_6"] } ],
  "planting" : [ {"xp": 100, "requirements": [], "abilities": ["planting_1"] },{"xp": 200, "requirements": [], "abilities": ["planting_2"] },{"xp": 400, "requirements": [], "abilities": ["planting_3"] },{"xp": 800, "requirements": [], "abilities": ["planting_4"] },{"xp": 1600, "requirements": [], "abilities": ["planting_5"] },{"xp": 3200, "requirements": [], "abilities": ["planting_6"] } ],
  "plowing" : [ {"xp": 200, "requirements": [], "abilities": ["plowing_1"] },{"xp": 400, "requirements": [], "abilities": ["plowing_2"] },{"xp": 800, "requirements": [], "abilities": ["plowing_3"] },{"xp": 1600, "requirements": [], "abilities": ["plowing_4"] },{"xp": 3200, "requirements": [], "abilities": ["plowing_5"] } ],
  "weeding" : [ {"xp": 200, "requirements": [], "abilities": ["weeding_1"] },{"xp": 400, "requirements": [], "abilities": ["weeding_2"] },{"xp": 800, "requirements": [], "abilities": ["weeding_3"] },{"xp": 1600, "requirements": [], "abilities": ["weeding_4"] },{"xp": 3200, "requirements": [], "abilities": ["weeding_5"] } ],
}
""";
      // Load the skills.
      Skill.LoadString(json);
    }
    {
      WorkTask.Clear();
      string json = """
{
"weed_field": { "timeCost": {"val": 10, "min":1, "modifiers": { "weeding_1":{ "mult":0.8},"weeding_2":{ "mult":0.8}}}, "requirements": ["hoe_1"], "inputs": {}, "outputs": { }, "effects": {"degrade_1": ["hoe_1"],"skill_weeding_3": [""],"weed": ["@1"], "minor_touch_field": ["@1"], "minor_learn_field": ["@1"]} },
"plow_field": { "timeCost": {"val": 10, "min":1, "modifiers": { "plowing_1":{ "mult":0.8},"plowing_2":{ "mult":0.8}}}, "requirements": ["plow_1"], "inputs": {}, "outputs": { }, "effects": {"degrade_1": ["plow_1"],"skill_plowing_3": [""],"minor_learn_field": ["@1"], "plow_under": ["@1"], "plow": ["@1"]} },
"plant_wheat": { "timeCost": {"val": 10, "min":1, "modifiers": { "planting_1":{ "mult":0.8},"planting_2":{ "mult":0.8}}}, "inputs": {"wheat" : 150}, "outputs": { }, "effects": {"skill_planting_3": [""],"plant_wheat":["@1"], "major_touch_crop":["@1"], "major_learn_crop":["@1"]} },
"harvest_wheat": { "timeCost": {"val": 40, "min":1, "modifiers": { "harvesting_1":{ "mult":0.8},"harvesting_2":{ "mult":0.8}}}, "requirements": ["sickle_1"], "inputs": {}, "outputs": { }, "effects": {"degrade_1": ["sickle_1"],"skill_harvesting_3": [""],"major_learn_crop":["@1"], "harvest_wheat":["@1"]} },
}
""";
      // Load the tasks.
      WorkTask.LoadString(json);
    }
    StaticAttributes.Initialize(true);
    EffectLoader.Initialize();
    ItemType.InitializeAll();
    WeatherAttributes.Init();
    // Reset the calendar so we are at the start of spring.
    Calendar.Reset();
    // Create a Household.
    Household household = new Household();
    // Create a person in the household.
    Person person = new Person("Bob", "Bob", household, Role.HeadOfHousehold);

    ItemType wheat = ItemType.Find("wheat")!;
    PlantCropEffect plantCrop = (PlantCropEffect)Effect.effects["plant_wheat"];
    
    AttributeType crop_health = AttributeType.Find("crop_health")!;
    AttributeType crop_yield = AttributeType.Find("crop_yield")!;
    AttributeType weeds = AttributeType.Find("weeds")!;

    Item wheatItem = new Item(wheat);
    Assert.AreEqual(0, household.inventory[wheatItem]);


    Field field = new Field(BuildingType.Find("field")!, household);
    ChosenEffectTarget fieldTarget = new ChosenEffectTarget(EffectTargetType.Field, field, field, field);
    // Set the field's soil quality to 5 (times 10), the minimum for wheat.
    field.SetAttribute(AttributeType.Find("soil_quality")!, 50);
    // Set the field's weeds to a low amount.
    field.SetAttribute(weeds, 0);
    // Plant 9 wheat. The field can hold 10, but we leave one empty to test for bugs
    // in the scaling code.
    plantCrop.ApplySync(fieldTarget, 0.9, 1);

    // Advance the calendar five days (50 ticks) at a time for 135 days.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 135, 50);

    // Harvest the crop.
    HarvestCropEffect harvestCrop = (HarvestCropEffect)Effect.effects["harvest_wheat"];
    harvestCrop.ApplySync(fieldTarget, 0.9, 1);
    // Check household inventory for the wheat.
    // Item should have been converted from pounds to food units.
    Assert.AreEqual(912, household.inventory[wheatItem]);

    // Clear the inventory.
    household.inventory.RemoveItem(wheatItem, household.inventory[wheatItem]);

    field = new Field(BuildingType.Find("field")!, household);
    fieldTarget = new ChosenEffectTarget(EffectTargetType.Field, field, field, field);
    // Advance to the beginning of the next year.
    Calendar.Advance((uint)(Calendar.ticksPerYear - Calendar.Ticks % Calendar.ticksPerYear));
    field.SetAttribute(AttributeType.Find("soil_quality")!, 50);
    field.SetAttribute(weeds, 0);
    field.SetAttribute(AttributeType.Find("deep_moisture")!, 40);
    field.SetAttribute(AttributeType.Find("surface_moisture")!, 10);
    plantCrop.ApplySync(fieldTarget, 0.9, 1);

    // Advance the calendar one day (10 ticks) at a time for 135 days.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 135, 10);

    harvestCrop.ApplySync(fieldTarget, 0.9, 1);
    Assert.AreEqual(908, household.inventory[wheatItem]);

    household.inventory.RemoveItem(wheatItem, household.inventory[wheatItem]);

    field = new Field(BuildingType.Find("field")!, household);
    fieldTarget = new ChosenEffectTarget(EffectTargetType.Field, field, field, field);
    // Advance to the beginning of the next year.
    Calendar.Advance((uint)(Calendar.ticksPerYear - Calendar.Ticks % Calendar.ticksPerYear));
    field.SetAttribute(AttributeType.Find("soil_quality")!, 50);
    field.SetAttribute(weeds, 0);
    field.SetAttribute(AttributeType.Find("deep_moisture")!, 40);
    field.SetAttribute(AttributeType.Find("surface_moisture")!, 10);
    plantCrop.ApplySync(fieldTarget, 0.9, 1);

    // Advance the calendar one tick at a time for 135 days.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 135, 1);

    harvestCrop.ApplySync(fieldTarget, 0.9, 1);
    Assert.AreEqual(905, household.inventory[wheatItem]);

    household.inventory.RemoveItem(wheatItem, household.inventory[wheatItem]);

    // Another crop planted immediately after harvest in the same field.
    field.SetAttribute(AttributeType.Find("deep_moisture")!, 40);
    field.SetAttribute(AttributeType.Find("surface_moisture")!, 10);
    plantCrop.ApplySync(fieldTarget, 0.9, 1);

    // Advance the calendar one tick at a time for 135 days.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 135, 1);

    harvestCrop.ApplySync(fieldTarget, 0.9, 1);
    Assert.AreEqual(683, household.inventory[wheatItem]);

    household.inventory.RemoveItem(wheatItem, household.inventory[wheatItem]);

    // A third crop planted immediately after harvest in the same field.
    field.SetAttribute(AttributeType.Find("deep_moisture")!, 40);
    field.SetAttribute(AttributeType.Find("surface_moisture")!, 10);
    plantCrop.ApplySync(fieldTarget, 0.9, 1);

    // Advance the calendar one tick at a time for 135 days.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 135, 1);

    harvestCrop.ApplySync(fieldTarget, 0.9, 1);
    Assert.AreEqual(0, household.inventory[wheatItem]);

    household.inventory.RemoveItem(wheatItem, household.inventory[wheatItem]);

    field = new Field(BuildingType.Find("field")!, household);
    fieldTarget = new ChosenEffectTarget(EffectTargetType.Field, field, field, field);
    // Advance to the beginning of the next year.
    Calendar.Advance((uint)(Calendar.ticksPerYear - Calendar.Ticks % Calendar.ticksPerYear));
    field.SetAttribute(AttributeType.Find("soil_quality")!, 50);
    field.SetAttribute(weeds, 0);
    field.SetAttribute(AttributeType.Find("deep_moisture")!, 40);
    field.SetAttribute(AttributeType.Find("surface_moisture")!, 10);
    plantCrop.ApplySync(fieldTarget, 0.9, 1);

    // Advance the calendar one tick at a time for 135 days.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 135, 1);
    Assert.AreEqual(452, field.GetValue(wheat, crop_yield), 1.0);

    // Yield should stay stable for the next 20 days.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 155, 1);
    Assert.AreEqual(452, field.GetValue(wheat, crop_yield), 1.0);

    // The crop should start to rot, dropping the yield.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 165, 1);
    Assert.AreEqual(0.9, field.Count(wheat));
    Assert.AreEqual(247, field.GetValue(wheat, crop_yield), 1.0);
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 175, 1);
    Assert.AreEqual(0.9, field.Count(wheat));
    Assert.AreEqual(135, field.GetValue(wheat, crop_yield), 1.0);
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 184, 1);
    Assert.AreEqual(79, field.GetValue(wheat, crop_yield), 1.0);

    // The crop should be entirely killed and removed from the field on the 185th day.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 185, 1);
    Assert.AreEqual(0, field.GetValue(wheat, crop_yield));
    // The field shouldn't have any wheat left.
    Assert.AreEqual(0, field.Count(wheat));


    // Now test with the person doing the work.

    // Reset the field and the calendar.
    field = new Field(BuildingType.Find("field")!, household);
    fieldTarget = new ChosenEffectTarget(EffectTargetType.Field, field, person, person);
    Calendar.Advance((uint)(Calendar.ticksPerYear - Calendar.Ticks % Calendar.ticksPerYear));
    field.SetAttribute(AttributeType.Find("soil_quality")!, 50);
    field.SetAttribute(weeds, 0);
    field.SetAttribute(AttributeType.Find("deep_moisture")!, 40);
    field.SetAttribute(AttributeType.Find("surface_moisture")!, 10);

    // Give the household 135 wheat, enough to plant 0.9 acres.
    household.inventory.RemoveItem(wheatItem, household.inventory[wheatItem]);
    household.inventory.AddItem(wheatItem, 135);

    // Don't bother with tools, just permanently grant the person
    // the hoe, plow, and sickle abilities.
    // They are basically Edward Scissorhands.
    person.GrantAbility(AbilityType.Find("hoe_1")!);
    person.GrantAbility(AbilityType.Find("plow_1")!);
    person.GrantAbility(AbilityType.Find("sickle_1")!);

    // weed_field, plow_field, plant_wheat, and harvest_wheat
    // should all be available to the person.
    WorkTask weed = WorkTask.tasks["weed_field"];
    WorkTask plow = WorkTask.tasks["plow_field"];
    WorkTask plant = WorkTask.tasks["plant_wheat"];
    WorkTask harvest = WorkTask.tasks["harvest_wheat"];
    Dictionary<string, ChosenEffectTarget> targetField = new Dictionary<string, ChosenEffectTarget>();
    targetField["@1"] = fieldTarget;

    // Plow the field
    var runningTask = TaskRunner.StartTask(person, person.household, plow, targetField, 1.0);
    Assert.IsNotNull(runningTask);
    person.EnqueueTask(runningTask, false);

    // Plant 90% of the field, that's all the wheat we have.
    runningTask = TaskRunner.StartTask(person, person.household, plant, targetField, 0.9);
    Assert.IsNotNull(runningTask);
    person.EnqueueTask(runningTask, false);

    // Do a first weeding
    runningTask = TaskRunner.StartTask(person, person.household, weed, targetField, 1.0);
    Assert.IsNotNull(runningTask);
    person.EnqueueTask(runningTask, false);

    // Tasks are enqueued, advance the calendar 3 days.
    Advance(field, person, 3);

    // Check that the field is planted.
    Assert.AreEqual(0.9, field.Count(wheat));
    // Check the weeds.
    Assert.AreEqual(3, field.GetAttributeValue(weeds), 0.1);
    // Check the crop health and yield
    Assert.AreEqual(71, field.GetValue(wheat, crop_health), 1.0);
    Assert.AreEqual(6.7, field.GetValue(wheat, crop_yield), 0.2);

    // Wait for a week and then do more weeding.
    Advance(field, person, 5);
    runningTask = TaskRunner.StartTask(person, person.household, weed, targetField, 1.0);
    Assert.IsNotNull(runningTask);
    person.EnqueueTask(runningTask, false);
    Advance(field, person, 1);

    // Check the weeds, health, and yield.
    Assert.AreEqual(7, field.GetAttributeValue(weeds), 0.1);
    Assert.AreEqual(70.5, field.GetValue(wheat, crop_health), 0.5);
    Assert.AreEqual(25.6, field.GetValue(wheat, crop_yield), 0.2);

    // Run until the crop is 135 days old.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 135, 1);
    Assert.AreEqual(16.3, field.GetAttributeValue(weeds), 0.1);
    Assert.AreEqual(69.5, field.GetValue(wheat, crop_health), 0.5);
    Assert.AreEqual(339, field.GetValue(wheat, crop_yield), 1.0);

    // Harvest the crop.
    runningTask = TaskRunner.StartTask(person, person.household, harvest, targetField, 0.9);
    Assert.IsNotNull(runningTask);
    person.EnqueueTask(runningTask, false);

    // Advance until the harvest is complete.
    Advance(field, person, 4);
    // Check the harvested yield.
    Assert.AreEqual(678, household.inventory[wheatItem]);

    // Check the person's skills.
    Assert.AreEqual(4, person.GetXP(Skill.Find("weeding")!));
    Assert.AreEqual(1, person.GetXP(Skill.Find("harvesting")!));
    Assert.AreEqual(1, person.GetXP(Skill.Find("planting")!));
    Assert.AreEqual(2, person.GetXP(Skill.Find("plowing")!));
    Assert.AreEqual(20, person.GetXP(Skill.Find("cereals")!));
  }

}