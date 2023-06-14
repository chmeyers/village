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
public class UtilityUnitTest
{
  public void Advance(Household household, HashSet<WorkTask> dailyTasks, uint days, bool untilIdle = false)
  {
    bool done = false;
    for (int i = 0; i < days * 10; i++)
    {
      Calendar.Advance(1);
      WeatherAttributes.AdvanceWeather();
      household.AdvanceBuildings();
      foreach (var person in Person.global_persons[household])
      {
        if (Calendar.StartOfDay)
        {
          person.PickTaskFromSet(dailyTasks, true);
        }
        TaskRunner.AdvanceTask(person);
        person.attributes.Advance();
        if (untilIdle && person.runningTasks.Count == 0)
        {
          done = true;
        }
      }
      if (done)
      {
        break;
      }
    }
  }

  public string NextTask(Person person, Household household, HashSet<WorkTask> dailyTasks)
  {
    Advance(household, dailyTasks, 5, true);
    WorkTask? task = person.PickTask();
    if (task == null)
    {
      return "";
    }
    Assert.IsNotNull(task);
    return task!.task;
  }

  [TestMethod]
  public void TestUtility()
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
        'winter' : { },
        'spring' : { },
        'summer' : { },
        'fall' : { },
        'not_winter' : { },
        'not_spring' : { },
        'not_summer' : { },
        'not_fall' : { },
        'weeding' : { levels: 8 },
        'plowing' : { levels: 8 },
        'planting' : { levels: 8 },
        'harvesting' : { levels: 8 },
        'cereals' : { levels: 8 },
        'legumes' : { levels: 8 },
        'hoe' : { levels: 8 },
        'plow' : { levels: 8 },
        'sickle' : { levels: 8 },
        'dexterity' : { levels: 20 },
        'strength' : { levels: 20 },
        'intelligence' : { levels: 20 },
        'basketmaking' : { levels: 7 },
        'pottery' : { levels: 7 },
        'tilemaking' : { levels: 7 },
        'brickmaking' : { levels: 7 },
        'kiln_firing' : { levels: 7 },
        'stone_tools' : { levels: 7 },
        'joinery' : { levels: 7 },
        'blacksmithing' : { levels: 7 },
        'charcoalmaking' : { levels: 7 },
        'lumberjacking' : { levels: 7 },
        'quarrying' : { levels: 7 },
        'mining' : { levels: 7 },
        'foraging' : { levels: 7 },
        'hunting' : { levels: 7 },
        'cooking' : { levels: 7 },
        'smelting' : { levels: 7 },
      }";
      // Load the ability types.
      AbilityType.LoadString(json);
    }
    {
      ItemType.Clear();
      string json = """
{
  "straw": { "group": "RESOURCE"},
  "food": { "group": "FOOD", "stockpile": [{"perPerson": 200, "utility": 200}]},
  "wheat": { "group": "FOOD", "parents" : ["food"], "weight": 0.5, "cropSettings": {"cropSkill": "cereals", "cropSkillLevel": 4, "minSoilQuality": 5, "minPlantingTemp": 40, "frostTolerance": 30, "heatTolerance": 85, "droughtTolerance": 0.5, "weedSusceptibleDays": 20, "initDays": 20, "devDays": 25, "midDays": 60, "lateDays": 30, "kcInit": 0.3, "kcMid": 1.15, "kcEnd": 0.25, "perTickYieldGrowth": 0.4444, "targetYieldPerAcre": 600, "seedPerAcre": 150, "hasHarvestableStraw": true, "nitrogenPerYield": 0.025, "phosphorusPerYield": 0.004142, "potassiumPerYield": 0.004565, "strawPerYield": 1.417, "nitrogenPerStraw": 0.0085, "phosphorusPerStraw": 0.000807, "potassiumPerStraw": 0.012035, "fieldCrop": true, "temperatePlantingMonths": [0,1], "harvestItems": { "wheat" : 1 , "straw": 1.417 }, "cropAttribute": "crop_wheat_growing"} },
  "hay": { "group": "FOOD", "parents" : ["food"], "weight": 1, "cropSettings": {"cropSkill": "cereals", "cropSkillLevel": 1, "minSoilQuality": 0, "minPlantingTemp": 32, "frostTolerance": 20, "heatTolerance": 100, "droughtTolerance": 0.75, "weedSusceptibleDays": 10, "initDays": 10, "devDays": 15, "midDays": 75, "lateDays": 35, "kcInit": 0.4, "kcMid": 0.85, "kcEnd": 0.85, "perTickYieldGrowth": 2.963, "targetYieldPerAcre": 4000, "seedPerAcre": 10, "nitrogenPerYield": 0.019, "phosphorusPerYield": 0.0025, "potassiumPerYield": 0.018, "strawPerYield": 1, "nitrogenPerStraw": 0.006, "phosphorusPerStraw": 0, "potassiumPerStraw": 0, "nitrogenFixing": 0.8, "fieldCrop": true, "temperatePlantingMonths": [0,1], "harvestItems": { "hay" : 1 }, "cropAttribute": "crop_hay_growing"} },
  "field_peas": { "group": "FOOD", "parents" : ["food"], "weight": 0.5, "cropSettings": {"cropSkill": "legumes", "cropSkillLevel": 1, "minSoilQuality": 2, "minPlantingTemp": 40, "frostTolerance": 28, "heatTolerance": 85, "droughtTolerance": 0.5, "weedSusceptibleDays": 40, "initDays": 20, "devDays": 30, "midDays": 40, "lateDays": 25, "kcInit": 0.4, "kcMid": 1.15, "kcEnd": 0.3, "perTickYieldGrowth": 0.5739, "targetYieldPerAcre": 660, "seedPerAcre": 180, "hasHarvestableStraw": true, "nitrogenPerYield": 0.04, "phosphorusPerYield": 0.008717, "potassiumPerYield": 0.009817, "strawPerYield": 1.5, "nitrogenPerStraw": 0.008, "phosphorusPerStraw": 0.0007, "potassiumPerStraw": 0.009, "nitrogenFixing": 0.6, "temperatePlantingMonths": [0,1], "fieldCrop": true, "harvestItems": { "field_peas" : 1 , "straw": 1.5 }, "cropAttribute": "crop_field_peas_growing"} },
  "wood": { "group": "RESOURCE" },
  "logs": { "group": "RESOURCE" },
  "clay": { "group": "RESOURCE" },
  "stone": { "group": "RESOURCE" },
  "iron_ore": { "group": "RESOURCE" },
  "iron": { "group": "RESOURCE" },
  "charcoal": { "group": "RESOURCE" },
  "unfired_tile": { "group": "RESOURCE" },
  "unfired_brick": { "group": "RESOURCE" },
  "unfired_pottery": { "group": "RESOURCE" },
  "rattan": { "group": "RESOURCE" },
  "coin": { "group": "CURRENCY" },
  "basket": { "group": "HOUSEHOLD" },
  "pottery": { "group": "HOUSEHOLD" },
  "brick": { "group": "RESOURCE" },
  "tile": { "group": "RESOURCE" },
  "lumber": { "group": "RESOURCE" },
  "anvil": { "group": "HOUSEHOLD" },
  "iron_anvil": { "group": "HOUSEHOLD", "parents": ["anvil"], "scrapItems": {"iron":300} },
  "chest": { "group": "HOUSEHOLD", "flammable": true },
  "quern": { "group": "HOUSEHOLD" },
  "meal":  { "group": "FOOD", "stockpile": [{"perPerson": 10, "utility": 20000},{"perPerson": 20, "utility": 500},{ "perPerson": 100, "perHousehold": 50, "utility": 250}] },
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
"minor_touch_crop" : { "target": "Crop", "effectType": "TouchCrop", "config": { "healthRate" : 0.2, } },
"major_touch_crop" : { "target": "Crop", "effectType": "TouchCrop", "config": { "healthRate" : 5.0, } },
"minor_touch_field" : { "target": "Field", "effectType": "TouchCrop", "config": { "healthRate" : 0.2, } },
"major_touch_field" : { "target": "Field", "effectType": "TouchCrop", "config": { "healthRate" : 5.0, } },
"minor_learn_crop" : { "target": "Crop", "effectType": "CropSkill", "config": { "amount": 2, } },
"major_learn_crop" : { "target": "Crop", "effectType": "CropSkill", "config": { "amount": 20, } },
"minor_learn_field" : { "target": "Field", "effectType": "CropSkill", "config": { "amount": 2, } },
"major_learn_field" : { "target": "Field", "effectType": "CropSkill", "config": { "amount": 20, } },
"grow_crop" : { "target": "Crop", "effectType": "GrowCrop", "config": {  } },
"rotting" : { "target": "Crop", "effectType": "RotCrop", "config": { "rotRate" : { "val": 0.003, "modifiers": {"wet_surface_soil": {"mult": 2}}}, } },
"kill_crop" : { "target": "Crop", "effectType": "KillCrop", "config": {  } },
"plant_wheat" : { "target": "Field", "effectType": "PlantCrop", "config": { "crop" : "wheat", "chainedEffects": ["major_touch_crop", "major_learn_crop"] } },
"harvest_wheat" : { "target": "Crop", "effectType": "HarvestCrop", "config": { "crop" : "wheat" } },
"plant_peas" : { "target": "Field", "effectType": "PlantCrop", "config": { "crop" : "field_peas", "chainedEffects": ["major_touch_crop", "major_learn_crop"] } },
"harvest_peas" : { "target": "Crop", "effectType": "HarvestCrop", "config": { "crop" : "field_peas" } },
"plant_hay" : { "target": "Field", "effectType": "PlantCrop", "config": { "crop" : "hay", "chainedEffects": ["major_touch_crop", "major_learn_crop"] } },
"plow_under" : { "target": "Field", "effectType": "KillCrop", "config": {  } },
"plow" : { "target": "Field", "effectType": "AttributePuller", "config": { "soil_quality" : { "target": {"val": "plowing", "add": 1 }, "amount": 0.3}, "weeds" : { "target": 0, "amount": {"val": "plowing", "add": 2, "mult": 20 }}, } },
"weed" : { "target": "Field", "effectType": "AttributeAdder", "config": { "weeds" : { "target": { "val": "weeding", "add": -5, "mult": -1}, "amount": {"val": "weeding", "add": 10, "mult": -2.0 }}, } },
"degrade_1" : { "target": "Item", "effectType": "Degrade", "config": { "amount": 1 } },
"skill_weeding_3" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "weeding", "level": 3} },
"skill_harvesting_3" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "harvesting", "level": 3, "amount": 10} },
"skill_planting_3" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "planting", "level": 3, "amount": 10 } },
"skill_plowing_3" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "plowing", "level": 3, "amount": 10 } },
"skill_basketmaking_1" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "basketmaking", "level": 1, "amount": 1 } },
"skill_pottery_1" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "pottery", "level": 1, "amount": 1 } },
"skill_tilemaking_1" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "tilemaking", "level": 1, "amount": 1 } },
"skill_brickmaking_1" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "brickmaking", "level": 1, "amount": 1 } },
"skill_kiln_firing_1" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "kiln_firing", "level": 1, "amount": 1 } },
"skill_kiln_firing_2" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "kiln_firing", "level": 2, "amount": 1 } },
"skill_stone_tools_1" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "stone_tools", "level": 1, "amount": 1 } },
"skill_joinery_1" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "joinery", "level": 1, "amount": 1 } },
"skill_blacksmithing_4" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "blacksmithing", "level": 4, "amount": 1 } },
"skill_charcoalmaking_1" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "charcoalmaking", "level": 1, "amount": 1 } },
"skill_lumberjacking_1" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "lumberjacking", "level": 1, "amount": 1 } },
"skill_lumberjacking_3" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "lumberjacking", "level": 3, "amount": 1 } },
"skill_quarrying_1" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "quarrying", "level": 1, "amount": 1 } },
"skill_mining_1" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "mining", "level": 1, "amount": 1 } },
"skill_foraging_2" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "foraging", "level": 2, "amount": 1 } },
"skill_foraging_3" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "foraging", "level": 3, "amount": 1 } },
"skill_hunting_2" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "hunting", "level": 2, "amount": 1 } },
"skill_cooking_1" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "cooking", "level": 1, "amount": 1 } },
"skill_smelting_4" : { "target" : "Person", "effectType" : "Skill", "config" : { "skill": "smelting", "level": 4, "amount": 1 } },
      }
""";
      EffectLoader.LoadString(json);
    }
    {
      AttributeType.Clear();
      string json = """
      {
"field" : { "min": 0, "max": 1.01, "group": "field" , "initial": 0, "intervals": [{"lower": 0, "abilities": [], "ongoing_effects": ["field_changes","drain_update","field_maintenance"]}]},
"surface_moisture" : { "min": 0, "max": 2, "group": "field" , "initial": 1, "intervals": [{"lower": 0, "abilities": ["dry_surface_soil"]},{"lower": 0.05, "abilities": ["wet_surface_soil"]}]},
"deep_moisture" : { "min": 0, "max": 10, "group": "field" , "initial": 4, "intervals": [{"lower": 0, "abilities": ["low_deep_moisture"]},{"lower": 0.1, "abilities": ["high_deep_moisture"]}]},
"drainage" : { "min": 0.2, "max": 2, "group": "field" , "initial": 1, "intervals": [{"lower": 0.2, "abilities": []}]},
"soil_quality" : { "min": 1, "max": 7.5, "group": "field" , "initial": 4, "intervals": [{"lower": 1, "abilities": []},{"lower": 4, "abilities": []}]},
"nitrogen" : { "min": 0, "max": 500, "group": "field" , "initial": 100, "intervals": [{"lower": 0, "abilities": ["low_nitrogen"]},{"lower": 5, "abilities": []}]},
"phosphorus" : { "min": 0, "max": 1000, "group": "field" , "initial": 150, "intervals": [{"lower": 0, "abilities": ["low_phosphorus"]},{"lower": 5, "abilities": []}]},
"potassium" : { "min": 0, "max": 10000, "group": "field" , "initial": 5000, "intervals": [{"lower": 0, "abilities": ["low_potassium"]},{"lower": 50, "abilities": []}]},
"weeds" : { "min": 0, "max": 100, "group": "field" , "initial": 100, "utilityType": "Linear", "intervals": [{"lower": 0, "abilities": ["low_weeds"], "utility": { "val": "field", "mult": 10000} },{"lower": 5, "abilities": ["mid_weeds"], "utility": { "val": "field", "mult": 9000}},{"lower": 30, "abilities": ["high_weeds"], "utility": { "val": "field", "mult": 5000}, "utilityUpper": 0}]},
"crop_health" : { "min": 0, "max": 200, "group": "crop" , "initial": 100, "intervals": [{"lower": 0, "abilities": []},{"lower": 10, "abilities": []}]},
"crop_yield" : { "min": 0, "max": 100000, "group": "crop" , "initial": 0, "intervals": [{"lower": 0, "abilities": []}]},
"crop_wheat_growing": { "min": 0, "max": 190, "changePerTick": 0.1, "initial": 0, "intervals": [{"lower": 0, "ongoing_effects": ["grow_crop"]}, {"lower": 130, "abilities": ["harvestable"], "ongoing_effects": ["grow_crop"]}, {"lower": 135, "abilities": ["harvestable"]}, {"lower": 155, "ongoing_effects": ["rotting"]}, {"lower": 185, "entry_effects": ["kill_crop"]} ]},
"crop_field_peas_growing": { "min": 0, "max": 190, "changePerTick": 0.1, "initial": 0, "intervals": [{"lower": 0, "ongoing_effects": ["grow_crop"]}, {"lower": 110, "abilities": ["harvestable"], "ongoing_effects": ["grow_crop"]}, {"lower": 115, "abilities": ["harvestable"]}, {"lower": 155, "ongoing_effects": ["rotting"]}, {"lower": 175, "entry_effects": ["kill_crop"]} ]},
"crop_hay_growing": { "min": 0, "max": 190, "changePerTick": 0.1, "initial": 0, "intervals": [{"lower": 0, "ongoing_effects": ["grow_crop"]}, {"lower": 130, "abilities": ["harvestable"], "ongoing_effects": ["grow_crop"]}, {"lower": 135, "abilities": ["harvestable"]}, {"lower": 155, "ongoing_effects": ["rotting"]}, {"lower": 185, "entry_effects": ["kill_crop"]} ]},
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
  "cereals" : [ {"xp": 100, "requirements": [], "abilities": ["cereals_1"] },{"xp": 200, "requirements": [], "abilities": ["cereals_2"] },{"xp": 400, "requirements": [], "abilities": ["cereals_3"] },{"xp": 800, "requirements": [], "abilities": ["cereals_4"] },{"xp": 1600, "requirements": [], "abilities": ["cereals_5"] },{"xp": 3200, "requirements": [], "abilities": ["cereals_6"] },{"xp": 3200, "requirements": [], "abilities": ["cereals_6"] },{"xp": 3200, "requirements": [], "abilities": ["cereals_6"] },{"xp": 3200, "requirements": [], "abilities": ["cereals_6"] },{"xp": 3200, "requirements": [], "abilities": ["cereals_6"] } ],
  "legumes" : [ {"xp": 100, "requirements": [], "abilities": ["legumes_1"] },{"xp": 200, "requirements": [], "abilities": ["legumes_2"] },{"xp": 400, "requirements": [], "abilities": ["legumes_3"] },{"xp": 800, "requirements": [], "abilities": ["legumes_4"] },{"xp": 1600, "requirements": [], "abilities": ["legumes_5"] },{"xp": 3200, "requirements": [], "abilities": ["legumes_6"] } ],
  "harvesting" : [ {"xp": 100, "requirements": [], "abilities": ["harvesting_1"] },{"xp": 200, "requirements": [], "abilities": ["harvesting_2"] },{"xp": 400, "requirements": [], "abilities": ["harvesting_3"] },{"xp": 800, "requirements": [], "abilities": ["harvesting_4"] },{"xp": 1600, "requirements": [], "abilities": ["harvesting_5"] },{"xp": 3200, "requirements": [], "abilities": ["harvesting_6"] } ],
  "planting" : [ {"xp": 100, "requirements": [], "abilities": ["planting_1"] },{"xp": 200, "requirements": [], "abilities": ["planting_2"] },{"xp": 400, "requirements": [], "abilities": ["planting_3"] },{"xp": 800, "requirements": [], "abilities": ["planting_4"] },{"xp": 1600, "requirements": [], "abilities": ["planting_5"] },{"xp": 3200, "requirements": [], "abilities": ["planting_6"] } ],
  "plowing" : [ {"xp": 200, "requirements": [], "abilities": ["plowing_1"] },{"xp": 400, "requirements": [], "abilities": ["plowing_2"] },{"xp": 800, "requirements": [], "abilities": ["plowing_3"] },{"xp": 1600, "requirements": [], "abilities": ["plowing_4"] },{"xp": 3200, "requirements": [], "abilities": ["plowing_5"] } ],
  "weeding" : [ {"xp": 200, "requirements": [], "abilities": ["weeding_1"] },{"xp": 400, "requirements": [], "abilities": ["weeding_2"] },{"xp": 800, "requirements": [], "abilities": ["weeding_3"] },{"xp": 1600, "requirements": [], "abilities": ["weeding_4"] },{"xp": 3200, "requirements": [], "abilities": ["weeding_5"] } ],
  "basketmaking" : [ {"xp": 100, "requirements": [], "abilities": ["basketmaking_1"] },{"xp": 200, "requirements": [], "abilities": ["basketmaking_2"] },{"xp": 400, "requirements": [], "abilities": ["basketmaking_3"] },{"xp": 800, "requirements": [], "abilities": ["basketmaking_4"] },{"xp": 1600, "requirements": [], "abilities": ["basketmaking_5"] },{"xp": 3200, "requirements": [], "abilities": ["basketmaking_6"] } ],
  "pottery" : [ {"xp": 100, "requirements": [], "abilities": ["pottery_1"] },{"xp": 200, "requirements": [], "abilities": ["pottery_2"] },{"xp": 400, "requirements": [], "abilities": ["pottery_3"] },{"xp": 800, "requirements": [], "abilities": ["pottery_4"] },{"xp": 1600, "requirements": [], "abilities": ["pottery_5"] },{"xp": 3200, "requirements": [], "abilities": ["pottery_6"] } ],
  "tilemaking" : [ {"xp": 100, "requirements": [], "abilities": ["tilemaking_1"] },{"xp": 200, "requirements": [], "abilities": ["tilemaking_2"] },{"xp": 400, "requirements": [], "abilities": ["tilemaking_3"] },{"xp": 800, "requirements": [], "abilities": ["tilemaking_4"] },{"xp": 1600, "requirements": [], "abilities": ["tilemaking_5"] },{"xp": 3200, "requirements": [], "abilities": ["tilemaking_6"] } ],
  "brickmaking" : [ {"xp": 100, "requirements": [], "abilities": ["brickmaking_1"] },{"xp": 200, "requirements": [], "abilities": ["brickmaking_2"] },{"xp": 400, "requirements": [], "abilities": ["brickmaking_3"] },{"xp": 800, "requirements": [], "abilities": ["brickmaking_4"] },{"xp": 1600, "requirements": [], "abilities": ["brickmaking_5"] },{"xp": 3200, "requirements": [], "abilities": ["brickmaking_6"] } ],
  "kiln_firing" : [ {"xp": 100, "requirements": [], "abilities": ["kiln_firing_1"] },{"xp": 200, "requirements": [], "abilities": ["kiln_firing_2"] },{"xp": 400, "requirements": [], "abilities": ["kiln_firing_3"] },{"xp": 800, "requirements": [], "abilities": ["kiln_firing_4"] },{"xp": 1600, "requirements": [], "abilities": ["kiln_firing_5"] },{"xp": 3200, "requirements": [], "abilities": ["kiln_firing_6"] } ],
  "stone_tools" : [ {"xp": 100, "requirements": [], "abilities": ["stone_tools_1"] },{"xp": 200, "requirements": [], "abilities": ["stone_tools_2"] },{"xp": 400, "requirements": [], "abilities": ["stone_tools_3"] },{"xp": 800, "requirements": [], "abilities": ["stone_tools_4"] },{"xp": 1600, "requirements": [], "abilities": ["stone_tools_5"] },{"xp": 3200, "requirements": [], "abilities": ["stone_tools_6"] } ],
  "joinery" : [ {"xp": 100, "requirements": [], "abilities": ["joinery_1"] },{"xp": 200, "requirements": [], "abilities": ["joinery_2"] },{"xp": 400, "requirements": [], "abilities": ["joinery_3"] },{"xp": 800, "requirements": [], "abilities": ["joinery_4"] },{"xp": 1600, "requirements": [], "abilities": ["joinery_5"] },{"xp": 3200, "requirements": [], "abilities": ["joinery_6"] } ],
  "blacksmithing" : [ {"xp": 100, "requirements": [], "abilities": ["blacksmithing_1"] },{"xp": 200, "requirements": [], "abilities": ["blacksmithing_2"] },{"xp": 400, "requirements": [], "abilities": ["blacksmithing_3"] },{"xp": 800, "requirements": [], "abilities": ["blacksmithing_4"] },{"xp": 1600, "requirements": [], "abilities": ["blacksmithing_5"] },{"xp": 3200, "requirements": [], "abilities": ["blacksmithing_6"] } ],
  "charcoalmaking" : [ {"xp": 100, "requirements": [], "abilities": ["charcoalmaking_1"] },{"xp": 200, "requirements": [], "abilities": ["charcoalmaking_2"] },{"xp": 400, "requirements": [], "abilities": ["charcoalmaking_3"] },{"xp": 800, "requirements": [], "abilities": ["charcoalmaking_4"] },{"xp": 1600, "requirements": [], "abilities": ["charcoalmaking_5"] },{"xp": 3200, "requirements": [], "abilities": ["charcoalmaking_6"] } ],
  "lumberjacking" : [ {"xp": 100, "requirements": [], "abilities": ["lumberjacking_1"] },{"xp": 200, "requirements": [], "abilities": ["lumberjacking_2"] },{"xp": 400, "requirements": [], "abilities": ["lumberjacking_3"] },{"xp": 800, "requirements": [], "abilities": ["lumberjacking_4"] },{"xp": 1600, "requirements": [], "abilities": ["lumberjacking_5"] },{"xp": 3200, "requirements": [], "abilities": ["lumberjacking_6"] } ],
  "quarrying" : [ {"xp": 100, "requirements": [], "abilities": ["quarrying_1"] },{"xp": 200, "requirements": [], "abilities": ["quarrying_2"] },{"xp": 400, "requirements": [], "abilities": ["quarrying_3"] },{"xp": 800, "requirements": [], "abilities": ["quarrying_4"] },{"xp": 1600, "requirements": [], "abilities": ["quarrying_5"] },{"xp": 3200, "requirements": [], "abilities": ["quarrying_6"] } ],
  "mining" : [ {"xp": 100, "requirements": [], "abilities": ["mining_1"] },{"xp": 200, "requirements": [], "abilities": ["mining_2"] },{"xp": 400, "requirements": [], "abilities": ["mining_3"] },{"xp": 800, "requirements": [], "abilities": ["mining_4"] },{"xp": 1600, "requirements": [], "abilities": ["mining_5"] },{"xp": 3200, "requirements": [], "abilities": ["mining_6"] } ],
  "foraging" : [ {"xp": 100, "requirements": [], "abilities": ["foraging_1"] },{"xp": 200, "requirements": [], "abilities": ["foraging_2"] },{"xp": 400, "requirements": [], "abilities": ["foraging_3"] },{"xp": 800, "requirements": [], "abilities": ["foraging_4"] },{"xp": 1600, "requirements": [], "abilities": ["foraging_5"] },{"xp": 3200, "requirements": [], "abilities": ["foraging_6"] } ],
  "hunting" : [ {"xp": 100, "requirements": [], "abilities": ["hunting_1"] },{"xp": 200, "requirements": [], "abilities": ["hunting_2"] },{"xp": 400, "requirements": [], "abilities": ["hunting_3"] },{"xp": 800, "requirements": [], "abilities": ["hunting_4"] },{"xp": 1600, "requirements": [], "abilities": ["hunting_5"] },{"xp": 3200, "requirements": [], "abilities": ["hunting_6"] } ],
  "cooking" : [ {"xp": 100, "requirements": [], "abilities": ["cooking_1"] },{"xp": 200, "requirements": [], "abilities": ["cooking_2"] },{"xp": 400, "requirements": [], "abilities": ["cooking_3"] },{"xp": 800, "requirements": [], "abilities": ["cooking_4"] },{"xp": 1600, "requirements": [], "abilities": ["cooking_5"] },{"xp": 3200, "requirements": [], "abilities": ["cooking_6"] } ],
  "smelting" : [ {"xp": 100, "requirements": [], "abilities": ["smelting_1"] },{"xp": 200, "requirements": [], "abilities": ["smelting_2"] },{"xp": 400, "requirements": [], "abilities": ["smelting_3"] },{"xp": 800, "requirements": [], "abilities": ["smelting_4"] },{"xp": 1600, "requirements": [], "abilities": ["smelting_5"] },{"xp": 3200, "requirements": [], "abilities": ["smelting_6"] } ],


}
""";
      // Load the skills.
      Skill.LoadString(json);
    }
    {
      WorkTask.Clear();
      string json = """
{
"weed_field": { "timeCost": {"val": 15, "min":1, "modifiers": { "weeding_1":{ "mult":0.8},"weeding_2":{ "mult":0.8}}}, "requirements": ["hoe_1"], "inputs": {}, "outputs": { }, "effects": {"degrade_1": ["hoe_1"],"skill_weeding_3": [""],"weed": ["@1"], "minor_touch_field": ["@1"], "minor_learn_field": ["@1"]} },
"plow_field": { "timeCost": {"val": 15, "min":1, "modifiers": { "plowing_1":{ "mult":0.8},"plowing_2":{ "mult":0.8}}}, "requirements": ["plow_1"], "inputs": {}, "outputs": { }, "effects": {"degrade_1": ["plow_1"],"skill_plowing_3": [""],"minor_learn_field": ["@1"], "plow_under": ["@1"], "plow": ["@1"]} },
"plant_wheat": { "timeCost": {"val": 15, "min":1, "modifiers": { "planting_1":{ "mult":0.8},"planting_2":{ "mult":0.8}}}, "inputs": {"wheat" : 150}, "outputs": { }, "effects": {"skill_planting_3": [""],"plant_wheat":["@1"]} },
"harvest_wheat": { "timeCost": {"val": 63, "min":1, "modifiers": { "harvesting_1":{ "mult":0.8},"harvesting_2":{ "mult":0.8}}}, "requirements": ["sickle_1"], "inputs": {}, "outputs": { }, "effects": {"degrade_1": ["sickle_1"],"skill_harvesting_3": [""],"major_learn_crop":["@1"], "harvest_wheat":["@1"]} },
"plant_peas": { "timeCost": {"val": 15, "min":1, "modifiers": { "planting_1":{ "mult":0.8},"planting_2":{ "mult":0.8}}}, "inputs": {"field_peas" : 150}, "outputs": { }, "effects": {"skill_planting_3": [""],"plant_peas":["@1"]} },
"harvest_peas": { "timeCost": {"val": 63, "min":1, "modifiers": { "harvesting_1":{ "mult":0.8},"harvesting_2":{ "mult":0.8}}}, "requirements": ["sickle_1"], "inputs": {}, "outputs": { }, "effects": {"degrade_1": ["sickle_1"],"skill_harvesting_3": [""],"major_learn_crop":["@1"], "harvest_peas":["@1"]} },
"plant_hay": { "timeCost": {"val": 15, "min":1, "modifiers": { "planting_1":{ "mult":0.8},"planting_2":{ "mult":0.8}}}, "inputs": {"hay" : 150}, "outputs": { }, "effects": {"skill_planting_3": [""],"plant_hay":["@1"]} },
"craft_basket": { "timeCost": {"val": 2, "min":1, "modifiers": { "dexterity_10":{ "mult":0.9},"dexterity_15":{ "mult":0.9}, "basketmaking_1":{ "mult":0.8},"basketmaking_2":{ "mult":0.8}}}, "requirements": [], "inputs": {"rattan" : 2}, "outputs": {"basket" : 1 }, "effects": {"skill_basketmaking_1": [""]} },
"craft_unfired_pottery": { "timeCost": {"val": 5, "min":1, "modifiers": { "dexterity_10":{ "mult":0.9},"dexterity_15":{ "mult":0.9}, "pottery_1":{ "mult":0.8},"pottery_2":{ "mult":0.8}}}, "requirements": [], "inputs": {"clay" : 2}, "outputs": {"unfired_pottery" : 5 }, "effects": {"skill_pottery_1": [""]} },
"craft_unfired_tile": { "timeCost": {"val": 1, "min":1, "modifiers": { "dexterity_10":{ "mult":0.9},"dexterity_15":{ "mult":0.9}, "tilemaking_1":{ "mult":0.8},"tilemaking_2":{ "mult":0.8}}}, "requirements": [], "inputs": {"clay" : 5}, "outputs": {"unfired_tile" : 10 }, "effects": {"skill_tilemaking_1": [""]} },
"craft_unfired_brick": { "timeCost": {"val": 1, "min":1, "modifiers": { "dexterity_10":{ "mult":0.9},"dexterity_15":{ "mult":0.9}, "brickmaking_1":{ "mult":0.8},"brickmaking_2":{ "mult":0.8}}}, "requirements": [], "inputs": {"clay" : 5}, "outputs": {"unfired_brick" : 10 }, "effects": {"skill_brickmaking_1": [""]} },
"craft_pottery": { "timeCost": {"val": 10, "min":1, "modifiers": { "intelligence_10":{ "mult":0.9},"intelligence_15":{ "mult":0.9}, "kiln_firing_1":{ "mult":0.8},"kiln_firing_2":{ "mult":0.8}}}, "requirements": [], "inputs": {"unfired_pottery" : 5, "charcoal" : 2}, "outputs": {"pottery" : 10 }, "effects": {"skill_kiln_firing_2": [""]} },
"craft_tile": { "timeCost": {"val": 10, "min":1, "modifiers": { "intelligence_10":{ "mult":0.9},"intelligence_15":{ "mult":0.9}, "kiln_firing_1":{ "mult":0.8},"kiln_firing_2":{ "mult":0.8}}}, "requirements": [], "inputs": {"unfired_tile" : 100, "charcoal" : 40}, "outputs": {"tile" : 100 }, "effects": {"skill_kiln_firing_1": [""]} },
"craft_brick": { "timeCost": {"val": 10, "min":1, "modifiers": { "intelligence_10":{ "mult":0.9},"intelligence_15":{ "mult":0.9}, "kiln_firing_1":{ "mult":0.8},"kiln_firing_2":{ "mult":0.8}}}, "requirements": [], "inputs": {"unfired_brick" : 100, "charcoal" : 40}, "outputs": {"brick" : 100 }, "effects": {"skill_kiln_firing_1": [""]} },
"craft_quern": { "timeCost": {"val": 30, "min":1, "modifiers": { "strength_10":{ "mult":0.9},"strength_15":{ "mult":0.9}, "stone_tools_1":{ "mult":0.8},"stone_tools_2":{ "mult":0.8}}}, "requirements": [], "inputs": {"stone" : 5}, "outputs": {"quern" : 1 }, "effects": {"skill_stone_tools_1": [""]} },
"craft_chest": { "timeCost": {"val": 20, "min":1, "modifiers": { "dexterity_10":{ "mult":0.9},"dexterity_15":{ "mult":0.9}, "joinery_1":{ "mult":0.8},"joinery_2":{ "mult":0.8}}}, "requirements": [], "inputs": {"lumber" : 8}, "outputs": {"chest" : 1 }, "effects": {"skill_joinery_1": [""]} },
"craft_iron_anvil": { "timeCost": {"val": 50, "min":1, "modifiers": { "strength_10":{ "mult":0.9},"strength_15":{ "mult":0.9}, "blacksmithing_1":{ "mult":0.8},"blacksmithing_2":{ "mult":0.8}}}, "requirements": [], "inputs": {"iron" : 320}, "outputs": {"iron_anvil" : 1 }, "effects": {"skill_blacksmithing_4": [""]} },
"make_charcoal_1": { "timeCost": 50, "requirements": [], "inputs": {"wood" : 240}, "outputs": {"charcoal" : {"val": 200, "modifiers": { "intelligence_10":{ "mult":1.1},"intelligence_15":{ "mult":1.1}  ,"charcoalmaking_1":{ "mult":1},"charcoalmaking_2":{ "mult":1.1}}} }, "effects": {"skill_charcoalmaking_1": [""]} },
"hew_lumber_1": { "timeCost": 20, "requirements": [], "inputs": {"logs" : 50}, "outputs": {"lumber" : {"val": 10, "modifiers": { "strength_10":{ "mult":1.1},"strength_15":{ "mult":1.1}  ,"lumberjacking_1":{ "mult":1},"lumberjacking_2":{ "mult":1.1}}} }, "effects": {"skill_lumberjacking_1": [""]} },
"smelt_iron_1": { "timeCost": 30, "requirements": [], "inputs": {"iron_ore" : 120, "charcoal" : 250}, "outputs": {"iron" : {"val": 300, "modifiers": { "intelligence_10":{ "mult":1.1},"intelligence_15":{ "mult":1.1}  ,"smelting_1":{ "mult":1},"smelting_2":{ "mult":1.1}}} }, "effects": {"skill_smelting_4": [""]} },
"gather_wood_1": { "timeCost": 10, "requirements": [],  "outputs": {"wood" : {"val": 10, "modifiers": { "strength_10":{ "mult":1.1},"strength_15":{ "mult":1.1} ,"lumberjacking_1":{ "mult":3},"lumberjacking_2":{ "mult":1.5}}} }, "effects": {"skill_lumberjacking_1": [""]} },
"gather_logs_3": { "timeCost": 20, "requirements": [],  "outputs": {"logs" : {"val": 20, "modifiers": { "strength_10":{ "mult":1.1},"strength_15":{ "mult":1.1} ,"lumberjacking_1":{ "mult":3},"lumberjacking_2":{ "mult":1.5}}} }, "effects": {"skill_lumberjacking_3": [""]} },
"gather_clay_by_hand": { "timeCost": 10, "requirements": [],  "outputs": {"clay" : {"val": 20, "modifiers": { "strength_10":{ "mult":1.1},"strength_15":{ "mult":1.1} }} }, "effects": {} },
"gather_stone_1": { "timeCost": 10, "requirements": [],  "outputs": {"stone" : {"val": 1, "modifiers": { "strength_10":{ "mult":1.1},"strength_15":{ "mult":1.1} ,"quarrying_1":{ "mult":3},"quarrying_2":{ "mult":1.5}}} }, "effects": {"skill_quarrying_1": [""]} },
"mine_iron_1": { "timeCost": 10, "requirements": [],  "outputs": {"iron_ore" : {"val": 10, "modifiers": { "strength_10":{ "mult":1.1},"strength_15":{ "mult":1.1} ,"mining_1":{ "mult":3},"mining_2":{ "mult":1.5}}} }, "effects": {"skill_mining_1": [""]} },
"gather_rattan_1": { "timeCost": 10, "requirements": [],  "outputs": {"rattan" : {"val": 35, "modifiers": { "dexterity_10":{ "mult":1.1},"dexterity_15":{ "mult":1.1} ,"foraging_1":{ "mult":3},"foraging_2":{ "mult":1.5}}} }, "effects": {"skill_foraging_3": [""]} },
"go_hungry": { "timeCost": 0, "compulsory": true, "inputs": {}, "outputs": { }, "effects": {} },
"scrounge_for_food": { "timeCost": {"val": 1, "min":1, "modifiers": { }}, "compulsory": true,"supercedes": ["go_hungry"], "requirements": ["not_winter","not_spring"], "inputs": {}, "outputs": { }, "effects": {} },
"forage_to_eat": { "timeCost": {"val": 1, "min":1, "modifiers": { "intelligence_10":{ "mult":0.9},"intelligence_15":{ "mult":0.9}, "foraging_1":{ "mult":0.8},"foraging_2":{ "mult":0.8}}}, "compulsory": true, "supercedes": ["scrounge_for_food"], "requirements": ["foraging_2","not_winter"], "inputs": {}, "outputs": {"food" : 1 }, "effects": {"skill_foraging_2": [""],} },
"hunt_to_eat": { "timeCost": {"val": 1, "min":1, "modifiers": { "intelligence_10":{ "mult":0.9},"intelligence_15":{ "mult":0.9}, "hunting_1":{ "mult":0.8},"hunting_2":{ "mult":0.8}}}, "compulsory": true, "supercedes": ["scrounge_for_food"], "requirements": ["hunting_2"], "inputs": {}, "outputs": {"food" : 1 }, "effects": {"skill_hunting_2": [""],} },
"cook_for_self_with_leftovers": { "timeCost": {"val": 1.2, "min":1, "modifiers": { "dexterity_10":{ "mult":0.9},"dexterity_15":{ "mult":0.9}, "cooking_1":{ "mult":0.8},"cooking_2":{ "mult":0.8}}}, "compulsory": true, "supercedes": ["forage_to_eat","hunt_to_eat"], "inputs": {"food" : 3}, "outputs": {"meal" : 20 }, "effects": {"skill_cooking_1": [""],} },
"eat_meal": { "timeCost": 0, "compulsory": true, "supercedes": ["cook_for_self_with_leftovers"], "inputs": {"meal" : 10}, "outputs": { }, "effects": {} },
"hunt": { "timeCost": {"val": 10, "min":1, "modifiers": { "intelligence_10":{ "mult":0.9},"intelligence_15":{ "mult":0.9}, "hunting_1":{ "mult":0.8},"hunting_2":{ "mult":0.8}}}, "inputs": {}, "outputs": {"food" : 12 }, "effects": {"skill_hunting_2": [""],} },
"quick_cook_meal": { "timeCost": 1, "inputs": {"food" : 1}, "outputs": {"meal" : 10 } },
"quick_cook_meals": { "timeCost": 2, "supercedes": ["quick_cook_meal"], "inputs": {"food" : 3}, "outputs": {"meal" : 30 } },
"cook_meals": { "timeCost": {"val": 5, "min":1, "modifiers": { "dexterity_10":{ "mult":0.9},"dexterity_15":{ "mult":0.9}, "cooking_1":{ "mult":0.8},"cooking_2":{ "mult":0.8}}}, "supercedes": ["quick_cook_meals"], "inputs": {"food" : 15}, "outputs": {"meal" : 150 }, "effects": {"skill_cooking_1": [""]} },
}
""";
      // Load the tasks.
      WorkTask.LoadString(json);
    }
    {
      TaskSet.Clear();
      string json = """
{
  "daily": [ "go_hungry", "scrounge_for_food", "forage_to_eat", "hunt_to_eat", "cook_for_self_with_leftovers", "eat_meal" ],
}
""";
      TaskSet.LoadString(json);
    }
    {
      string json = """
{
  "coin": { "buy":1, "sell":1 },
  "straw": { "buy":5, "sell":150 },
  "wheat": { "buy":155, "sell":350 },
  "field_peas": { "buy":90, "sell":300 },
  "hay": { "buy":25, "sell":150 },
  "wood": { "buy":5, "sell":100 },
  "logs": { "buy":15, "sell":200 },
  "clay": { "buy":2, "sell":50 },
  "stone": { "buy":10, "sell":300 },
  "iron": { "buy":50, "sell":400 },
  "charcoal": { "buy":8, "sell":120 },
  "rattan": { "buy":2, "sell":50 },
  "basket": { "buy":45, "sell":150 },
  "pottery": { "buy":45, "sell":150 },
  "brick": { "buy":45, "sell":100 },
  "tile": { "buy":45, "sell":100 },
  "lumber": { "buy":40, "sell":500 },
  "iron_anvil": { "buy":500, "sell":4000 },
  "chest": { "buy":300, "sell":2000 },
  "quern": { "buy":400, "sell":2500 },
}
""";
      // Load the default prices.
      ConfigPriceList.LoadDefaultFromString(json);
    }
    StaticAttributes.Initialize(true);
    EffectLoader.Initialize();
    ItemType.InitializeAll();
    WeatherAttributes.Init();
    Calendar.Reset();

    Household household = new Household();
    Person person = new Person("Bob", "bob", household, Role.HeadOfHousehold);
    // Give the household a field so they can plant stuff.
    household.AddField(BuildingType.Find("field")!);

    // Abilities for field work.
    person.GrantAbility(AbilityType.Find("hoe_1")!);
    person.GrantAbility(AbilityType.Find("plow_1")!);
    person.GrantAbility(AbilityType.Find("sickle_1")!);

    HashSet<WorkTask> dailyTasks = TaskSet.Find("daily")!;
    ItemType wheat = ItemType.Find("wheat")!;
    ItemType hay = ItemType.Find("hay")!;
    ItemType peas = ItemType.Find("field_peas")!;
    ItemType basket = ItemType.Find("basket")!;
    ItemType iron_anvil = ItemType.Find("iron_anvil")!;

    // Check the person's time utility.
    // This will recursively check all the tasks they can do.
    Assert.AreEqual(18, person.DetermineTimeUtility(), 0.5);

    // We don't have any food, so wheat has high utility.
    Assert.AreEqual(200100, household.Utility(person, wheat, 1), 1);
    Assert.AreEqual(-200250, household.Utility(person, wheat, -1), 1);

    // Basket's utility is solely based on the market price.
    Assert.AreEqual(45, household.Utility(person, basket, 1), 0.5);
    // Since we don't have any, we would need to buy one or produce one.
    // It's cheaper to produce one, so the utility is based on that.
    Assert.AreEqual(-45, household.Utility(person, basket, -1), 0.5);
    household.inventory.AddItem(new Item(basket), 1);
    // Now that we have one, utility is based on sell price.
    Assert.AreEqual(-45, household.Utility(person, basket, -1), 0.5);
    Assert.AreEqual(-90, household.Utility(person, basket, -2), 0.5);

    // Unlike baskets, it's cheaper to buy an anvil than to produce one.
    Assert.AreEqual(500, household.Utility(person, iron_anvil, 1), 0.5);
    Assert.AreEqual(-4000, household.Utility(person, iron_anvil, -1), 0.5);
    household.inventory.AddItem(new Item(iron_anvil), 1);
    // Now that we have one, utility is based on sell price.
    Assert.AreEqual(-500, household.Utility(person, iron_anvil, -1), 0.5);
    Assert.AreEqual(-4500, household.Utility(person, iron_anvil, -2), 0.5);

    // Pick a task. They need food, so they'll pick "hunt".
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    // They have food, so they'll cook it.
    Assert.AreEqual("quick_cook_meals", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    // Enough food for a big cooking job.
    Assert.AreEqual("cook_meals", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("cook_meals", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("cook_meals", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    Assert.AreEqual("hunt", NextTask(person, household, dailyTasks));
    // They've filled up their food stockpile, so they'll do something else.
    // They'll plow the field, even though they don't have any seed, because
    // they assume they can buy some.
    Assert.AreEqual("plow_field", NextTask(person, household, dailyTasks));
    // Buying not implemented, so they do something else instead
    Assert.AreEqual("", NextTask(person, household, dailyTasks));
    Assert.AreEqual("gather_wood_1", NextTask(person, household, dailyTasks));

    // // Give them wheat, hayseed, and peas so they can plant.
    household.inventory.AddItem(new Item(wheat), 300);
    household.inventory.AddItem(new Item(hay), 300);
    household.inventory.AddItem(new Item(peas), 300);

    Assert.AreEqual("plant_wheat", NextTask(person, household, dailyTasks));
    Assert.AreEqual("weed_field", NextTask(person, household, dailyTasks));
    Assert.AreEqual("weed_field", NextTask(person, household, dailyTasks));
    Assert.AreEqual("mine_iron_1", NextTask(person, household, dailyTasks));
    Assert.AreEqual("mine_iron_1", NextTask(person, household, dailyTasks));
    Assert.AreEqual("weed_field", NextTask(person, household, dailyTasks));
    Assert.AreEqual("cook_meals", NextTask(person, household, dailyTasks));
    Assert.AreEqual("mine_iron_1", NextTask(person, household, dailyTasks));

    HashSet<string> validTasks = new HashSet<string>();
    validTasks.Add("mine_iron_1");
    validTasks.Add("weed_field");
    validTasks.Add("hunt");
    validTasks.Add("cook_meals");
    validTasks.Add("");
    validTasks.Add("gather_wood_1");
    validTasks.Add("gather_clay_by_hand");
    validTasks.Add("craft_unfired_brick");
    validTasks.Add("craft_unfired_tile");
    validTasks.Add("make_charcoal_1");
    validTasks.Add("craft_brick");
    validTasks.Add("gather_rattan_1");
    validTasks.Add("craft_basket");
    validTasks.Add("craft_unfired_pottery");
    validTasks.Add("craft_pottery");
    validTasks.Add("smelt_iron_1");
    for (int i = 0; i < 171; ++i) {
      string task = NextTask(person, household, dailyTasks);
      // print the task so we can see what's going on.
      //Console.WriteLine(task);
      Assert.IsTrue(validTasks.Contains(task), "Task ("+ i + ") " + task + " is not in valid set.");
    }

    // Time to harvest the wheat.
    Assert.AreEqual("harvest_wheat", NextTask(person, household, dailyTasks));
    
    
  }
}