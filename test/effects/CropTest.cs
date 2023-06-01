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
  "wheat": { "group": "FOOD", "parents" : ["food"], "weight": 0.5, "cropSettings": {"minSoilQuality": 5, "minPlantingTemp": 40, "frostTolerance": 30, "heatTolerance": 85, "droughtTolerance": 0.5, "weedSusceptibleDays": 20, "initDays": 20, "devDays": 25, "midDays": 60, "lateDays": 30, "kcInit": 0.3, "kcMid": 1.15, "kcEnd": 0.25, "perTickYieldGrowth": 0.444, "targetYieldPerAcre": 600, "seedPerAcre": 150, "hasHarvestableStraw": true, "nitrogenPerYield": 0.025, "phosphorusPerYield": 0.004142, "potassiumPerYield": 0.004565, "strawPerYield": 1.417, "nitrogenPerStraw": 0.0085, "phosphorusPerStraw": 0.000807, "potassiumPerStraw": 0.012035, "temperatePlantingMonths": [0,1], "harvestItems": { "wheat" : 1 , "straw": 1.417 }, "cropAttribute": "crop_wheat_growing"} },
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
"plant_crop" : { "target": "Field", "effectType": "PlantCrop", "config": { "crop" : "wheat" } },
"harvest_crop" : { "target": "Field", "effectType": "HarvestCrop", "config": { "crop" : "wheat" } },
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
    PlantCropEffect plantCrop = (PlantCropEffect)Effect.effects["plant_crop"];
    
    AttributeType crop_health = AttributeType.Find("crop_health")!;
    AttributeType crop_yield = AttributeType.Find("crop_yield")!;

    Item wheatItem = new Item(wheat);
    Assert.AreEqual(0, household.inventory[wheatItem]);


    Field field = new Field(BuildingType.Find("field")!, household);
    ChosenEffectTarget fieldTarget = new ChosenEffectTarget(EffectTargetType.Field, field, field, field);
    // Set the field's soil quality to 5 (times 10), the minimum for wheat.
    field.SetAttribute(AttributeType.Find("soil_quality")!, 50);
    // Set the field's weeds to a low amount.
    field.SetAttribute(AttributeType.Find("weeds")!, 0);
    // Plant 9 wheat. The field can hold 10, but we leave one empty to test for bugs
    // in the scaling code.
    plantCrop.ApplySync(fieldTarget, 0.9, 1);

    // Advance the calendar five days (50 ticks) at a time for 135 days.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 135, 50);

    // Harvest the crop.
    HarvestCropEffect harvestCrop = (HarvestCropEffect)Effect.effects["harvest_crop"];
    harvestCrop.ApplySync(fieldTarget, 0.9, 1);
    // Check household inventory for the wheat.
    // Item should have been converted from pounds to food units.
    Assert.AreEqual(911, household.inventory[wheatItem]);

    // Clear the inventory.
    household.inventory.RemoveItem(wheatItem, household.inventory[wheatItem]);

    field = new Field(BuildingType.Find("field")!, household);
    fieldTarget = new ChosenEffectTarget(EffectTargetType.Field, field, field, field);
    // Advance to the beginning of the next year.
    Calendar.Advance((uint)(Calendar.ticksPerYear - Calendar.Ticks % Calendar.ticksPerYear));
    field.SetAttribute(AttributeType.Find("soil_quality")!, 50);
    field.SetAttribute(AttributeType.Find("weeds")!, 0);
    field.SetAttribute(AttributeType.Find("deep_moisture")!, 40);
    field.SetAttribute(AttributeType.Find("surface_moisture")!, 10);
    plantCrop.ApplySync(fieldTarget, 0.9, 1);

    // Advance the calendar one day (10 ticks) at a time for 135 days.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 135, 10);

    harvestCrop.ApplySync(fieldTarget, 0.9, 1);
    Assert.AreEqual(907, household.inventory[wheatItem]);

    household.inventory.RemoveItem(wheatItem, household.inventory[wheatItem]);

    field = new Field(BuildingType.Find("field")!, household);
    fieldTarget = new ChosenEffectTarget(EffectTargetType.Field, field, field, field);
    // Advance to the beginning of the next year.
    Calendar.Advance((uint)(Calendar.ticksPerYear - Calendar.Ticks % Calendar.ticksPerYear));
    field.SetAttribute(AttributeType.Find("soil_quality")!, 50);
    field.SetAttribute(AttributeType.Find("weeds")!, 0);
    field.SetAttribute(AttributeType.Find("deep_moisture")!, 40);
    field.SetAttribute(AttributeType.Find("surface_moisture")!, 10);
    plantCrop.ApplySync(fieldTarget, 0.9, 1);

    // Advance the calendar one tick at a time for 135 days.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 135, 1);

    harvestCrop.ApplySync(fieldTarget, 0.9, 1);
    Assert.AreEqual(904, household.inventory[wheatItem]);

    household.inventory.RemoveItem(wheatItem, household.inventory[wheatItem]);

    // Another crop planted immediately after harvest in the same field.
    field.SetAttribute(AttributeType.Find("deep_moisture")!, 40);
    field.SetAttribute(AttributeType.Find("surface_moisture")!, 10);
    plantCrop.ApplySync(fieldTarget, 0.9, 1);

    // Advance the calendar one tick at a time for 135 days.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 135, 1);

    harvestCrop.ApplySync(fieldTarget, 0.9, 1);
    Assert.AreEqual(682, household.inventory[wheatItem]);

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
    field.SetAttribute(AttributeType.Find("weeds")!, 0);
    field.SetAttribute(AttributeType.Find("deep_moisture")!, 40);
    field.SetAttribute(AttributeType.Find("surface_moisture")!, 10);
    plantCrop.ApplySync(fieldTarget, 0.9, 1);

    // Advance the calendar one tick at a time for 135 days.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 135, 1);
    Assert.AreEqual(452, field.GetValue(wheat, AttributeType.Find("crop_yield")!), 1.0);

    // Yield should stay stable for the next 20 days.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 155, 1);
    Assert.AreEqual(452, field.GetValue(wheat, AttributeType.Find("crop_yield")!), 1.0);

    // The crop should start to rot, dropping the yield.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 165, 1);
    Assert.AreEqual(0.9, field.Count(wheat));
    Assert.AreEqual(247, field.GetValue(wheat, AttributeType.Find("crop_yield")!), 1.0);
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 175, 1);
    Assert.AreEqual(0.9, field.Count(wheat));
    Assert.AreEqual(135, field.GetValue(wheat, AttributeType.Find("crop_yield")!), 1.0);
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 184, 1);
    Assert.AreEqual(78, field.GetValue(wheat, AttributeType.Find("crop_yield")!), 1.0);

    // The crop should be entirely killed and removed from the field on the 185th day.
    RunCrop(field, wheat, wheat.cropSettings!.cropAttribute!, 185, 1);
    Assert.AreEqual(0, field.GetValue(wheat, AttributeType.Find("crop_yield")!));
    // The field shouldn't have any wheat left.
    Assert.AreEqual(0, field.Count(wheat));

    
  }

}