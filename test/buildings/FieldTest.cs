using Microsoft.VisualStudio.TestTools.UnitTesting;
using Village.Abilities;
using Village.Attributes;
using Village.Base;
using Village.Buildings;
using Village.Effects;
using Village.Households;
using Village.Items;
using Village.Persons;
using Village.Tasks;
namespace VillageTest;


[TestClass]
public class FieldUnitTest
{
  [TestMethod]
  public void TestField()
  {
    {
      AbilityType.Clear();
      string json = @"{
        'low_surface_moisture' : { },
        'high_surface_moisture' : { },
        'low_weeds' : { },
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
      // Load effects.
      EffectLoader.Clear();
      string json = @"{
        'rain_1' : { 'target': 'Field', 'effectType': 'AttributePuller', 'config': { 'surface_moisture' : { 'target':50, 'amount': 0.1} } },
'evaporate_1' : { 'target': 'Field', 'effectType': 'AttributePuller', 'config': { 'surface_moisture' : { 'target':0, 'amount': 0.1} } },
'drain_1' : { 'target': 'Field', 'effectType': 'AttributePuller', 'config': { 'surface_moisture' : { 'target':1, 'amount': { 'val':0.1, 'modifiers':{ 'low_surface_moisture': { 'mult': 0} } } }, 'deep_moisture' : { 'target': { 'val': 'soil_quality'}, 'amount': { 'val':0.1, 'modifiers':{ 'low_surface_moisture': { 'mult': 0} } } } } },
'drain_5' : { 'target': 'Field', 'effectType': 'AttributeTransfer', 'config': { 'surface_moisture' : { 'sourceMin':1, 'amount': { 'val':0.5, 'modifiers':{ 'low_surface_moisture': { 'mult': 0} } }, 'dest': 'deep_moisture', 'destMax': { 'val': 'soil_quality'} } } },
'field_maintainence' : { 'target': 'Field', 'effectType': 'AttributePuller', 'config': { 'soil_quality' : { 'target':500, 'amount': 0.01}, 'weeds' : { 'target': 500, 'amount': { 'val':0.1, 'modifiers':{ 'low_surface_moisture': { 'mult': -1} } } }, 'nitrogen' : { 'target': 500, 'amount': 0.03 }, 'potassium' : { 'target': 500, 'amount': 0.03 }, 'phosphorus' : { 'target': 500, 'amount': 0.03 } } },
'crop_require_surface_moisture' : { 'target': 'Crop', 'effectType': 'AttributePuller', 'config': { 'crop_health' : { 'target':0, 'amount': { 'val': 0.1, 'modifiers': { 'high_surface_moisture': { 'mult': 0} } } }, } },
'crop_weed_sensitive' : { 'target': 'Crop', 'effectType': 'AttributePuller', 'config': { 'crop_health' : { 'target':0, 'amount': { 'val': 0.5, 'modifiers': { 'low_weeds': { 'mult': 0} } } }, } },
'crop_weed_competing' : { 'target': 'Crop', 'effectType': 'AttributePuller', 'config': { 'crop_yield' : { 'target':0, 'amount': { 'val': 0.5, 'modifiers': { 'low_weeds': { 'mult': 0} } } }, } },
'rotting' : { 'target': 'Crop', 'effectType': 'AttributePuller', 'config': { 'crop_health' : { 'target':0, 'amount': { 'val': 0.5, 'modifiers': { 'high_surface_moisture': { 'mult': 2} } } }, 'crop_yield' : { 'target':0, 'amount': { 'val': 5, 'modifiers': { 'high_surface_moisture': { 'mult': 2} } } }, } },
'low_water_usage' : { 'target': 'Crop', 'effectType': 'AttributePuller', 'config': { 'crop_health' : { 'target':0, 'amount': { 'val': 1, 'modifiers': { 'high_surface_moisture': { 'mult': 0},'high_deep_moisture': { 'mult': 0} } } },'deep_moisture' : { 'target':0, 'amount': { 'val': 0, 'modifiers': { 'low_surface_moisture': { 'add': 0.05} } } }, 'surface_moisture' : { 'target':0, 'amount': { 'val': 0.05, 'modifiers': { 'low_surface_moisture': { 'mult': 0} } } }, } },
'high_water_usage' : { 'target': 'Crop', 'effectType': 'AttributePuller', 'config': { 'crop_health' : { 'target':0, 'amount': { 'val': 1, 'modifiers': { 'high_surface_moisture': { 'mult': 0},'high_deep_moisture': { 'mult': 0} } } },'deep_moisture' : { 'target':0, 'amount': { 'val': 0, 'modifiers': { 'low_surface_moisture': { 'add': 0.2} } } }, 'surface_moisture' : { 'target':0, 'amount': { 'val': 0.2, 'modifiers': { 'low_surface_moisture': { 'mult': 0} } } }, } },
'low_npk_usage' : { 'target': 'Crop', 'effectType': 'AttributePuller', 'config': { 'crop_health' : { 'target':0, 'amount': { 'val': 0, 'modifiers': { 'low_nitrogen': { 'add': 0.1},'low_potassium': { 'add': 0.1},'low_phosphorus': { 'add': 0.1} } } },'crop_yield' : { 'target':0, 'amount': { 'val': 0, 'modifiers': { 'low_nitrogen': { 'add': 1},'low_potassium': { 'add': 1},'low_phosphorus': { 'add': 1} } } }, 'nitrogen' : { 'target':0, 'amount': 0.05}, 'potassium' : { 'target':0, 'amount': 0.05}, 'phosphorus' : { 'target':0, 'amount': 0.05} } },
'high_npk_usage' : { 'target': 'Crop', 'effectType': 'AttributeAdder', 'config': { 'crop_health' : { 'target':0, 'amount': { 'val': 0, 'modifiers': { 'low_nitrogen': { 'add': -0.1},'low_potassium': { 'add': -0.1},'low_phosphorus': { 'add': -0.1} } } },'crop_yield' : { 'target':0, 'amount': { 'val': 0, 'modifiers': { 'low_nitrogen': { 'add': -3},'low_potassium': { 'add': -3},'low_phosphorus': { 'add': -3} } } }, 'nitrogen' : { 'target':0, 'amount': -0.1}, 'potassium' : { 'target':0, 'amount': -0.1}, 'phosphorus' : { 'target':0, 'amount': -0.1} } },
'increase_yield' : { 'target': 'Crop', 'effectType': 'AttributeAdder', 'config': { 'crop_yield' : { 'amount': { 'val': 1, 'modifiers': { 'low_nitrogen': { 'add': -0.4},'low_potassium': { 'add': -0.1},'low_phosphorus': { 'add': -0.1},'low_deep_moisture': { 'add': -0.4} } } } } },
      }";
      EffectLoader.LoadString(json);
    }
    {
      AttributeType.Clear();
      string json = @"{
        'field' : { 'min': 0, 'max': 1, 'group': 'field' , 'initial': 0, 'intervals': [{'lower': 0, 'abilities': [], 'ongoing_effects': ['field_maintainence','rain_1']}]},
'surface_moisture' : { 'min': 0, 'max': 50, 'group': 'field' , 'initial': 10, 'intervals': [{ 'lower': 0, 'abilities': ['low_surface_moisture']},{ 'lower': 3, 'abilities': ['high_surface_moisture']}]},
'deep_moisture' : { 'min': 0, 'max': 500, 'group': 'field' , 'initial': 100, 'intervals': [{ 'lower': 0, 'abilities': ['low_deep_moisture']},{ 'lower': 10, 'abilities': ['high_deep_moisture']}]},
'nitrogen' : { 'min': 0, 'max': 500, 'group': 'field' , 'initial': 150, 'intervals': [{ 'lower': 0, 'abilities': ['low_nitrogen']},{ 'lower': 25, 'abilities': []}]},
'potassium' : { 'min': 0, 'max': 500, 'group': 'field' , 'initial': 150, 'intervals': [{ 'lower': 0, 'abilities': ['low_potassium']},{ 'lower': 25, 'abilities': []}]},
'phosphorus' : { 'min': 0, 'max': 500, 'group': 'field' , 'initial': 150, 'intervals': [{ 'lower': 0, 'abilities': ['low_phosphorus']},{ 'lower': 25, 'abilities': []}]},
'drainage' : { 'min': 0, 'max': 100, 'group': 'field' , 'initial': 50, 'intervals': [{ 'lower': 0, 'abilities': [], 'ongoing_effects': ['drain_1']},{ 'lower': 10, 'abilities': [],'ongoing_effects': ['drain_5']}]},
'soil_quality' : { 'min': 0, 'max': 500, 'group': 'field' , 'initial': 100, 'intervals': [{ 'lower': 0, 'abilities': []},{ 'lower': 50, 'abilities': []}]},
'weeds' : { 'min': 0, 'max': 100, 'group': 'field' , 'initial': 100, 'intervals': [{ 'lower': 0, 'abilities': ['low_weeds']},{ 'lower': 50, 'abilities': []}]},
'crop_health' : { 'min': 0, 'max': 100, 'group': 'crop' , 'initial': 100, 'intervals': [{ 'lower': 0, 'abilities': []},{ 'lower': 10, 'abilities': []}]},
'crop_yield' : { 'min': 0, 'max': 3000, 'group': 'crop' , 'initial': 0, 'intervals': [{ 'lower': 0, 'abilities': []}]},
'crop_wheat_growing' : { 'min': 0, 'max': 115, 'changePerTick': 0.1 , 'initial': 0, 'intervals': [{ 'lower': 0, 'abilities': [], 'ongoing_effects': ['crop_require_surface_moisture','crop_weed_sensitive']},{ 'lower': 7, 'abilities': [], 'ongoing_effects': ['crop_require_surface_moisture','crop_weed_sensitive','low_npk_usage','increase_yield']},{ 'lower': 28, 'abilities': [], 'ongoing_effects': ['crop_weed_competing','high_npk_usage','high_water_usage','increase_yield']},{ 'lower': 95, 'abilities': [], 'ongoing_effects': ['low_water_usage','low_npk_usage']},{'lower': 110, 'abilities': [], 'ongoing_effects': ['rotting']}]},
      }";
      // Load the attributes.
      AttributeType.LoadString(json);
    }
    {
      BuildingType.Clear();
      string data = @"{ 'field': {} }";
      BuildingType.LoadString(data);
    }
    {
      ItemType.Clear();
      string json = @"{
  'wheat': { 'group': 'FOOD', 'cropSettings' : {'cropAttribute': 'crop_wheat_growing', 'harvestItems': { 'wheat': 1} } }
}";
      // Load the item types.
      ItemType.LoadString(json);
    }
    EffectLoader.Initialize();
    ItemType.InitializeAll();
    // Create a Household.
    Household household = new Household();
    // Create a person in the household.
    Person person = new Person("Bob", "Bob", household, Role.HeadOfHousehold);
    // Create a field.
    Field field = new Field(BuildingType.Find("field")!, household);
    field.Resize(10);
    // Plant a crop in the field.
    ItemType wheat = ItemType.Find("wheat")!;
    field.RemoveAll(); // Plow.
    Assert.IsTrue(field.Plant(wheat, 1));
    // The field can hold 10 crops.
    Assert.IsTrue(field.Plant(wheat, 8));
    // The field has nine, so it can only hold one more.
    Assert.IsFalse(field.Plant(wheat, 2));
    Assert.IsTrue(field.Plant(wheat, 1));

    // Check that all the Attributes got created in the Field's attibute set.
    Assert.AreEqual(9, field.state.attributes.Count);

    // Advance the game calendar by 1 tick.
    Calendar.Advance();
    // Advance the Field to the current tick.
    field.Advance();

    // crop_wheat_growing should have increased by 0.1*10.
    AttributeType crop_wheat_growing = AttributeType.Find("crop_wheat_growing")!;
    Assert.AreEqual(1, field.GetValue(wheat, crop_wheat_growing));
    // crop_health should have decreased 5 due to weeds
    AttributeType crop_health = AttributeType.Find("crop_health")!;
    Assert.AreEqual(995, field.GetValue(wheat, crop_health));
    // crop_yield should have stayed the same.
    AttributeType crop_yield = AttributeType.Find("crop_yield")!;
    Assert.AreEqual(0, field.GetValue(wheat, crop_yield));
    // The Field's surface_moisture should have decreased by 5 and rain should have added 1.
    AttributeType surface_moisture = AttributeType.Find("surface_moisture")!;
    Assert.AreEqual(96, field.state.attributes[surface_moisture].value);
    // The Field's deep_moisture should increase.
    AttributeType deep_moisture = AttributeType.Find("deep_moisture")!;
    Assert.AreEqual(1005, field.state.attributes[deep_moisture].value);

    // Decrease the Field's weeds to 0.
    AttributeType weeds = AttributeType.Find("weeds")!;
    field.state.SetValue(weeds, 0);

    // Advance the game calendar by 1 tick.
    Calendar.Advance();
    // Advance the Field to the current tick.
    field.Advance();

    // crop_wheat_growing should have increased by 0.1*10.
    Assert.AreEqual(2, field.GetValue(wheat, crop_wheat_growing));
    // crop_health should have stayed the same.
    Assert.AreEqual(995, field.GetValue(wheat, crop_health));
    // crop_yield should have stayed the same.
    Assert.AreEqual(0, field.GetValue(wheat, crop_yield));
    // The Field's surface_moisture should have decreased
    Assert.AreEqual(92, field.state.attributes[surface_moisture].value);
    Assert.AreEqual(1010, field.state.attributes[deep_moisture].value);

    // Make it rain.
    field.state.SetValue(surface_moisture, 100);
    // Advance the game calendar to tick 70
    Calendar.Advance(68);
    // Advance the Field to the current tick.
    field.Advance();

    // crop_wheat_growing should be at day 7.
    Assert.AreEqual(70, field.GetValue(wheat, crop_wheat_growing));
    Assert.AreEqual(927, field.GetValue(wheat, crop_health));

    // Advance the game calendar to tick 280
    for (int i = 0; i < 3; i++)
    {
      Calendar.Advance(70);
      // Advance the Field to the current tick.
      field.Advance();
      // Make it rain.
      field.state.SetValue(surface_moisture, 100);
    }

    // crop_wheat_growing should be at day 28.
    Assert.AreEqual(280, field.GetValue(wheat, crop_wheat_growing));
    Assert.AreEqual(717, field.GetValue(wheat, crop_health));
    // crop_yield should have increased by 10 each tick
    Assert.AreEqual(2100, field.GetValue(wheat, crop_yield));
    

    // Advance the game calendar to tick 910, at most 70 ticks at a time.
    for (int i = 0; i < 9; i++)
    {
      Calendar.Advance(70);
      // Advance the Field to the current tick.
      field.Advance();
      // Make it rain.
      field.state.SetValue(surface_moisture, 100);
      // Remove weeds.
      field.state.SetValue(weeds, 0);
    }


    // crop_wheat_growing should be at day 91.
    Assert.AreEqual(910, field.GetValue(wheat, crop_wheat_growing));
    Assert.AreEqual(717, field.GetValue(wheat, crop_health));
    Assert.AreEqual(8400, field.GetValue(wheat, crop_yield));

    // Advance the game calendar to tick 1050
    for (int i = 0; i < 2; i++)
    {
      Calendar.Advance(70);
      // Advance the Field to the current tick.
      field.Advance();
    }

    // crop_wheat_growing should be at day 105.
    Assert.AreEqual(1050, field.GetValue(wheat, crop_wheat_growing));
    Assert.AreEqual(717, field.GetValue(wheat, crop_health));
    Assert.AreEqual(8800, field.GetValue(wheat, crop_yield));

    // Advance the game calendar to tick 1400
    for (int i = 0; i < 4; i++)
    {
      Calendar.Advance(70);
      // Advance the Field to the current tick.
      field.Advance();
    }

    // crop_wheat_growing should have capped out at day 115
    Assert.AreEqual(1149.9, field.GetValue(wheat, crop_wheat_growing), 0.1);
    // crop_health should be at zero.
    Assert.AreEqual(0, field.GetValue(wheat, crop_health));
    // crop_yield should be at 0.
    Assert.AreEqual(0, field.GetValue(wheat, crop_yield));



  }
}