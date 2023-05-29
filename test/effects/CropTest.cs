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
  "wheat": { "group": "FOOD", "parents" : ["food"], "weight": 0.5, "cropSettings": {"minSoilQuality": 5, "minPlantingTemp": 40, "frostTolerance": 30, "heatTolerance": 85, "droughtTolerance": 0.5, "weedSusceptibleDays": 20, "initDays": 20, "devDays": 25, "midDays": 60, "lateDays": 30, "kcInit": 0.3, "kcMid": 1.15, "kcEnd": 0.25, "perTickYieldGrowth": 0.0444, "targetYieldPerAcreTenth": 60, "seedPerAcreTenth": 15, "hasHarvestableStraw": true, "nitrogenPerYield": 0.025, "phosphorusPerYield": 0.004142, "potassiumPerYield": 0.004565, "strawPerYield": 1.417, "nitrogenPerStraw": 0.0085, "phosphorusPerStraw": 0.000807, "potassiumPerStraw": 0.012035, "temperatePlantingMonths": [0,1], "harvestItems": { "wheat" : 1 , "straw": 1.417 }, "cropAttribute": "crop_wheat_growing"} },
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
"drain_update" : { "target": "Field", "effectType": "AttributeTransfer", "config": { "surface_moisture" : { "sourceMin":1, "amount": { "val": "drainage"}, "dest": "deep_moisture", "destMax": { "val": "soil_quality"} }, } },
"field_changes" : { "target": "Field", "effectType": "AttributeAdder", "config": { "soil_quality" : { "amount": 0.0001}, "nitrogen" : { "amount": { "val": "soil_quality", "add": 2.5, "mult": 0.000052, "prescaled": true} }, "phosphorus" : { "amount": 0.0000972 }, "potassium" : { "amount": { "val": "soil_quality", "mult": 0.0000925, "prescaled": true} } } },
"field_maintenance" : { "target": "Field", "effectType": "FieldMaintenance", "config": {  } },
"rotting" : { "target": "Crop", "effectType": "AttributeAdder", "config": { "crop_health" : { "target":0, "amount": { "val": -0.5, "modifiers": { "wet_surface_soil": { "mult": 2} } } }, "crop_yield" : { "target":0, "amount": { "val": -5, "modifiers": { "wet_surface_soil": { "mult": 2} } } }, } },
"grow_crop" : { "target": "Crop", "effectType": "GrowCrop", "config": { } },
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
"nitrogen" : { "min": 0, "max": 500, "group": "field" , "initial": 10, "intervals": [{"lower": 0, "abilities": ["low_nitrogen"]},{"lower": 5, "abilities": []}]},
"phosphorus" : { "min": 0, "max": 1000, "group": "field" , "initial": 15, "intervals": [{"lower": 0, "abilities": ["low_phosphorus"]},{"lower": 5, "abilities": []}]},
"potassium" : { "min": 0, "max": 1000, "group": "field" , "initial": 500, "intervals": [{"lower": 0, "abilities": ["low_potassium"]},{"lower": 50, "abilities": []}]},
"weeds" : { "min": 0, "max": 100, "group": "field" , "initial": 100, "intervals": [{"lower": 0, "abilities": ["low_weeds"]},{"lower": 10, "abilities": ["mid_weeds"]},{"lower": 20, "abilities": ["high_weeds"]}]},
"crop_health" : { "min": 0, "max": 100, "group": "crop" , "initial": 100, "intervals": [{"lower": 0, "abilities": []},{"lower": 10, "abilities": []}]},
"crop_yield" : { "min": 0, "max": 3000, "group": "crop" , "initial": 0, "intervals": [{"lower": 0, "abilities": []}]},
"crop_wheat_growing": { "min": 0, "max": 140, "changePerTick": 0.1, "initial": 0, "intervals": [{"lower": 0, "ongoing_effects": ["grow_crop"]}, {"lower": 135, "ongoing_effects": ["rotting"]}]},
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
    EffectLoader.Initialize();
    ItemType.InitializeAll();
    WeatherAttributes.Init();
    // Reset the calendar so we are at the start of spring.
    Calendar.Reset();
    // Create a Household.
    Household household = new Household();
    // Create a person in the household.
    Person person = new Person("Bob", "Bob", household, Role.HeadOfHousehold);
    // Create a field.
    Field field = new Field(BuildingType.Find("field")!, household);
    // Set the field's soil quality to 5 (times 10), the minimum for wheat.
    field.SetAttribute(AttributeType.Find("soil_quality")!, 50);
    // Set the field's weeds to a low amount.
    field.SetAttribute(AttributeType.Find("weeds")!, 0);
    // Plant a crop in the field.
    ItemType wheat = ItemType.Find("wheat")!;
    PlantCropEffect plantCrop = (PlantCropEffect)Effect.effects["plant_crop"];
    ChosenEffectTarget fieldTarget = new ChosenEffectTarget(EffectTargetType.Field, field, field, field);
    // Plant 9 wheat. The field can hold 10, but we leave one empty to test for bugs
    // in the scaling code.
    plantCrop.ApplySync(fieldTarget, 10, 1);

    AttributeType crop_health = AttributeType.Find("crop_health")!;
    AttributeType crop_yield = AttributeType.Find("crop_yield")!;
    AttributeType weekly_low = AttributeType.Find("weekly_low")!;

    // Advance the calendar five days (50 ticks) at a time for 135 days.
    for (int i = 0; i < 27; i++)
    {
      Calendar.Advance(50);
      WeatherAttributes.AdvanceWeather();
      // Advance the field.
      field.Advance();
      // print all the attributes.
      Console.WriteLine("Week {0}", i);
      foreach (var attribute in field.state.attributes)
      {
        Console.WriteLine("{0}: {1}", attribute.Key.name, attribute.Value.value);
      }
      // And the crop attributes.
      Console.WriteLine("Weekly_low: {0}", field.GetValue(wheat, weekly_low));
      Console.WriteLine("Health: {0}", field.GetValue(wheat, crop_health));
      Console.WriteLine("Yield: {0}", field.GetValue(wheat, crop_yield));
      Console.WriteLine();
    }

    // Harvest the crop.
    HarvestCropEffect harvestCrop = (HarvestCropEffect)Effect.effects["harvest_crop"];
    harvestCrop.ApplySync(fieldTarget, 9, 1);
    // Check household inventory for the wheat.
    Item wheatItem = new Item(wheat);
    // Item should have been converted from pounds to food units.
    Assert.IsTrue(household.inventory[wheatItem] > 890);

    // Clear the inventory.
    household.inventory.RemoveItem(wheatItem, household.inventory[wheatItem]);
    // Plant a new crop.
    plantCrop.ApplySync(fieldTarget, 9, 1);
    // Advance the calendar 135 days.
    for (int i = 0; i < 27; i++)
    {
      Calendar.Advance(50);
      WeatherAttributes.AdvanceWeather();
      // Advance the field.
      field.Advance();
      // print all the attributes.
      Console.WriteLine("Week {0}", i+27);
      foreach (var attribute in field.state.attributes)
      {
        Console.WriteLine("{0}: {1}", attribute.Key.name, attribute.Value.value);
      }
      // And the crop attributes.
      Console.WriteLine("Health: {0}", field.GetValue(wheat, crop_health));
      Console.WriteLine("Yield: {0}", field.GetValue(wheat, crop_yield));
      Console.WriteLine();
    }

    // Harvest the crop.
    harvestCrop.ApplySync(fieldTarget, 9, 1);
    // Check household inventory for the wheat.
    Assert.IsTrue(household.inventory[wheatItem] > 500);

    // Clear the inventory.
    household.inventory.RemoveItem(wheatItem, household.inventory[wheatItem]);
    // Plant a new crop.
    plantCrop.ApplySync(fieldTarget, 9, 1);
    // Advance the calendar 135 days.
    for (int i = 0; i < 27; i++)
    {
      Calendar.Advance(50);
      WeatherAttributes.AdvanceWeather();
      // Advance the field.
      field.Advance();
      // print all the attributes.
      Console.WriteLine("Week {0}", i + 54);
      foreach (var attribute in field.state.attributes)
      {
        Console.WriteLine("{0}: {1}", attribute.Key.name, attribute.Value.value);
      }
      // And the crop attributes.
      Console.WriteLine("Weekly_low: {0}", field.GetValue(wheat, weekly_low));
      Console.WriteLine("Health: {0}", field.GetValue(wheat, crop_health));
      Console.WriteLine("Yield: {0}", field.GetValue(wheat, crop_yield));
      Console.WriteLine();
    }
  }

}