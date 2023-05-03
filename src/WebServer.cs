// Serve a web page with the game.
//
// Usage: WebServer.exe [port]
//   port: The port to listen on. Defaults to 8080.
//
// The web server will serve the game at http://localhost:8080/.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

public class GameServer
{
  // The port to listen on.
  private static int port = 8080;
  // The web server.
  private static HttpListener listener = new HttpListener();

  // The main player household.
  private static Household household = new Household(true);
  // The main person.
  private static Person person = new Person("protagonist", "Protagonist", household, Role.HeadOfHousehold);

  private static Person trader = StockTrader();

  private static Person StockTrader()
  {
    Person trader = new Person("trader", "Trader");
    trader.isTrader = true;
    // Stock the trader's inventory with iron tools, copper, iron, and leather.
    Dictionary<ItemType, int> itemTypes = new Dictionary<ItemType, int>();
    itemTypes[ItemType.Find("iron_axe")!] = 100;
    itemTypes[ItemType.Find("iron_pickaxe")!] = 100;
    itemTypes[ItemType.Find("iron_shovel")!] = 100;
    itemTypes[ItemType.Find("iron_saw")!] = 100;
    itemTypes[ItemType.Find("iron_knife")!] = 100;
    itemTypes[ItemType.Find("iron_hoe")!] = 100;
    itemTypes[ItemType.Find("copper")!] = 10000;
    itemTypes[ItemType.Find("iron")!] = 10000;
    itemTypes[ItemType.Find("leather")!] = 10000;
    itemTypes[ItemType.Find("coin")!] = 100000;
    // Make items from itemTypes and add them to the trader's inventory.
    foreach (var itemType in itemTypes)
    {
      trader.inventory.AddItem(new Item(itemType.Key, trader), itemType.Value);
    }
    trader.priceList = ConfigPriceList.Default;
    return trader;
  }
  
  // Start the web server.
  public static void Start()
  {
    // Get the port from the command line.
    string[] args = Environment.GetCommandLineArgs();
    if (args.Length > 1)
    {
      port = int.Parse(args[1]);
    }
    // Add the prefixes.
    listener.Prefixes.Add($"http://localhost:{port}/");
    
    // Start the listener.
    listener.Start();
    Console.WriteLine($"Listening on port {port}...");

    // Loop until the listener is stopped.
    while (listener.IsListening)
    {
      // Wait for a request.
      HttpListenerContext context = listener.GetContext();
      // Get the request and response.
      HttpListenerRequest request = context.Request;
      HttpListenerResponse response = context.Response;
      // Get the request URL.
      var url = request!.Url!;
      Console.WriteLine($"Request: {url.ToString()}");
      // If the request is for a task, perform the task.
      if (url.AbsolutePath == "/task")
      {
        // Get the task name.
        string taskName = request.QueryString["task"]!;
        bool isPersonal = bool.TryParse(request.QueryString["personal"], out bool personal) && personal;
        int? target = null;
        if (request.QueryString["target"] != null)
        {
          target = int.Parse(request.QueryString["target"]!);
        }
        // Perform the task.
        PerformTask(taskName, isPersonal, target);
      }
      // if the request is for a building, build the building.
      else if (url.AbsolutePath == "/build")
      {
        // Get the building name.
        string buildingName = request.QueryString["building"]!;
        // Build the building.
        BuildBuilding(buildingName);
      }
      // if the request is to sell, sell the item.
      else if (url.AbsolutePath == "/sell")
      {
        // Get the item name.
        string itemName = request.QueryString["item"]!;
        int quantity = int.Parse(request.QueryString["quantity"]!);
        bool personal = bool.TryParse(request.QueryString["personal"], out bool isPersonal) && isPersonal;
        // Sell the item.
        SellItem(itemName, quantity, personal);
      }
      // if the request is to buy, buy the item.
      else if (url.AbsolutePath == "/buy")
      {
        // Get the item name.
        string itemName = request.QueryString["item"]!;
        int quantity = int.Parse(request.QueryString["quantity"]!);
        // Buy the item.
        BuyItem(itemName, quantity);
      }

      // Return the GetGamePage() response.
      string responseString = GetGamePage();
      // Convert the response string to a byte array.
      byte[] buffer = Encoding.UTF8.GetBytes(responseString);
      // Get the output stream.
      Stream output = response.OutputStream;
      // Write the response to the output stream.
      output.Write(buffer, 0, buffer.Length);
      // Close the output stream.
      output.Close();
    }
  }

  private static void PerformTask(string taskName, bool personal, int? target)
  {
    // Get the lists of tasks.
    HashSet<WorkTask> tasks = personal ? person.AvailablePersonalTasks : person.AvailableHouseholdTasks;
    WorkTask? task = WorkTask.Find(taskName);
    // If the task is not valid, return.
    if (task == null)
    {
      Console.WriteLine($"Task {taskName} not found.");
      return;
    }
    // If the task is not in the set, return.
    if (!tasks.Contains(task))
    {
      Console.WriteLine($"Task {taskName} not available.");
      return;
    }
    // Get the targets.
    Dictionary<string, ChosenEffectTarget>? targets = null;
    if (task.targets.Count > 1)
    {
      Console.WriteLine("Task cancelled, no valid target.");
      return;
    }
    else if (task.targets.Count == 1 && target != null && target >= 0 && target < person.household.buildings.Count)
    {
      if (task.targets.First().Value.effectTargetType != EffectTargetType.Building)
      {
        Console.WriteLine("Task cancelled, no valid target.");
        return;
      }
      targets = new Dictionary<string, ChosenEffectTarget>();
      
      targets[task.targets.First().Key] = new ChosenEffectTarget(task.targets.First().Value.effectTargetType, person.household.buildings[target!.Value], person.household, person);
    }
    
    // Perform the task using the TaskRunner
    var runningTask = TaskRunner.StartTask(person, (personal ? person.inventory : person.household.inventory), task, targets);
    if (runningTask != null)
    {
      Console.WriteLine("Task started.");
      // Enqueue the task to the person.
      person.runningTasks.Enqueue(runningTask);
    }
    else
    {
      Console.WriteLine("Task not started.");
    }
  }

  private static void BuildBuilding(string buildingName)
  {
    // Get the building type.
    BuildingType? buildingType = BuildingType.Find(buildingName);
    // If the building type is not valid, return.
    if (buildingType == null)
    {
      Console.WriteLine($"Building {buildingName} not found.");
      return;
    }
    // If the building type is not in the set, return.
    if (!person.AvailableBuildings.Contains(buildingType))
    {
      Console.WriteLine($"Building {buildingName} not available.");
      return;
    }
    // Build the building.
    household.AddBuilding(buildingType);
    Console.WriteLine($"Building {buildingName} built.");
  }

  private static void SellItem(string itemName, int quantity, bool personal)
  {
    // Get the item type.
    ItemType? itemType = ItemType.Find(itemName);
    // If the item type is not valid, return.
    if (itemType == null)
    {
      Console.WriteLine($"Item {itemName} not found.");
      return;
    }
    // If trading from the household inventory, pull the item into the person's inventory.
    if (!personal)
    {
      var householdItems = household.inventory.Get(itemType, quantity);
      if (householdItems == null)
      {
        Console.WriteLine($"Item {itemName} not in inventory!");
        return;
      }
      household.inventory.Transfer(person.inventory, householdItems);
    }
    // Get the Trader's price for the item.
    var items = person.inventory.Get(itemType, quantity);
    if (items == null)
    {
      Console.WriteLine($"Item {itemName} not in inventory!");
      return;
    }
    int price = trader.GetOffer(items, person);
    Item coin = new Item(ItemType.Find("coin")!);
    // Create a dictionary of price number of coin.
    Dictionary<Item, int> priceDict = new Dictionary<Item, int>();
    priceDict[coin] = price;

    // Sell the item.
    if (!person.ProposeTrade(trader, items, priceDict))
    {
      Console.WriteLine($"Item {itemName} not sold!");
      return;
    }
  }

  private static void BuyItem(string itemName, int quantity)
  {
    // Get the item type.
    ItemType? itemType = ItemType.Find(itemName);
    // If the item type is not valid, return.
    if (itemType == null)
    {
      Console.WriteLine($"Item {itemName} not found.");
      return;
    }
    // Get the Trader's price for the item.
    var items = trader.inventory.Get(itemType, quantity);
    if (items == null)
    {
      Console.WriteLine($"Item {itemName} not in inventory!");
      return;
    }
    int price = trader.GetPrice(items, person);
    Item coin = new Item(ItemType.Find("coin")!);
    // Create a dictionary of price number of coin.
    Dictionary<Item, int> priceDict = new Dictionary<Item, int>();
    priceDict[coin] = price;

    // Buy the item.
    if (!person.ProposeTrade(trader, priceDict, items))
    {
      Console.WriteLine($"Item {itemName} not bought!");
      return;
    }

    // Move any bought resources from personal inventory to household inventory.
    if (itemType.itemGroup == ItemGroup.RESOURCE)
    {
      person.inventory.Transfer(household.inventory, items);
    }
  }

  private static string GetGamePage()
  {
    // Show the person's inventory on the left, with the household's inventory underneath it.
    // and the household's buildings on the below that.
    // On the right show the person's skills, attributes, and abilities.
    // On the bottom show the person's tasks and constructions.
    StringBuilder sb = new StringBuilder();
    sb.Append("<html>");
    sb.Append("<head>");
    sb.Append("<title>Village</title>");
    // Add the CSS for right and left alignment.
    sb.Append("<style>");
    sb.Append("table { border-collapse: collapse; }");
    sb.Append("td, th { border: 1px solid black; padding: 5px; }");
    sb.Append(".left { text-align: left; }");
    sb.Append(".right { text-align: right; }");
    sb.Append("</style>");
    sb.Append("</head>");
    sb.Append("<body>");
    sb.Append("<h1>Village</h1>");
    // Game Tick.
    sb.Append($"<p>Tick: {Calendar.Ticks}</p>");
    // Peek at the person's current task.
    if (person.runningTasks.Count > 0)
    {
      person.runningTasks.TryPeek(out RunningTask? runningTask);
      if (runningTask != null)
      {
        sb.Append($"<p>Current Task: {runningTask.task.task}</p>");
      }
      else 
      {
        sb.Append($"<p>Current Task: None</p>");
      }
    }
    sb.Append("<div style=\"display:flex\">");
    // On the left.
    sb.Append("<div style=\"flex: 1\">");
    sb.Append("<h2>Inventory</h2>");
    sb.Append(GetInventoryTable(person.inventory, true));
    sb.Append("<h2>Household Inventory</h2>");
    sb.Append(GetInventoryTable(household.inventory, false));
    sb.Append("<h2>Buildings</h2>");
    sb.Append(GetBuildingsTable());
    sb.Append("<h2>Skills</h2>");
    sb.Append(GetSkillsTable());
    sb.Append("<h2>Attributes</h2>");
    sb.Append(GetAttributesTable());
    sb.Append("<h2>Abilities</h2>");
    sb.Append(GetAbilitiesTable());
    sb.Append("</div>");
    // In the middle.
    sb.Append("<div style=\"flex: 1\">");
    sb.Append("<h2>Craft Tools</h2>");
    sb.Append(GetToolCraftingTable());
    sb.Append("<h2>Gathering</h2>");
    sb.Append(GetGatheringTable());
    sb.Append("<h2>Resource Processing</h2>");
    sb.Append(GetResourceProcessingTable());
    sb.Append("<h2>Building Tasks</h2>");
    sb.Append(GetBuildingComponentTable());
    sb.Append("<h2>Other Tasks</h2>");
    sb.Append(GetOtherTasksTable());
    sb.Append("</div>");
    // On the far right.
    sb.Append("<div style=\"flex: 1\">");
    sb.Append("<h2>Purchase</h2>");
    sb.Append(GetPurchaseTable());
    sb.Append("<h2>Construction</h2>");
    sb.Append(GetConstructionTable());
    sb.Append("</div>");
    sb.Append("</div>");
    sb.Append("</body>");
    sb.Append("</html>");
    return sb.ToString();
  }

  // Stop the web server.
  public static void Stop()
  {
    listener.Stop();
  }

  // Return a web page table with the person's inventory.
  private static string GetInventoryTable(Inventory inventory, bool personal)
  {
    StringBuilder sb = new StringBuilder();
    sb.Append("<table>");
    sb.Append("<tr><th>Item</th><th>Count</th><th>MinQuality</th><th>Sale Price</th></tr>");
    foreach (var itemtype in inventory.items)
    {
      // Sum up the quantities of the itemtype.
      int quantity = 0;
      int minQuality = int.MaxValue;
      foreach (var item in itemtype.Value)
      {
        quantity += item.Value;
        if (item.Key.quality < minQuality)
        {
          minQuality = item.Key.quality;
        }
      }
      // Get the default sale price for the item.
      var singleItem = inventory.Get(itemtype.Key, 1);
      int price = trader.GetOffer(singleItem!, person);
      sb.Append($"<tr><td>{itemtype.Key.itemType}</td><td>{quantity}</td><td>{minQuality}</td><td>{price}</td>");
      // Add buttons to sell 1, 100 or all of the item. Diable the buttons if the price is zero.
      sb.Append($"<td><form action=\"sell\" method=\"get\"><input type=\"hidden\" name=\"item\" value=\"{itemtype.Key.itemType}\" /><input type=\"hidden\" name=\"quantity\" value=\"1\" /><input type=\"hidden\" name=\"personal\" value={personal}><input type=\"submit\" value=\"Sell 1\" ");
      if (price == 0)
      {
        sb.Append("disabled");
      }
      sb.Append(" /></form></td>");
      // Show the sell 100 button but disable it if there are less than 100 of the item.
      sb.Append($"<td><form action=\"sell\" method=\"get\"><input type=\"hidden\" name=\"item\" value=\"{itemtype.Key.itemType}\" /><input type=\"hidden\" name=\"quantity\" value=\"100\" /><input type=\"hidden\" name=\"personal\" value={personal}><input type=\"submit\" value=\"Sell 100\" ");
      if (quantity < 100 || price == 0)
      {
        sb.Append("disabled");
      }
      sb.Append(" /></form></td>");
      // Show the sell all button but disable it if there are no items or the price is zero.
      sb.Append($"<td><form action=\"sell\" method=\"get\"><input type=\"hidden\" name=\"item\" value=\"{itemtype.Key.itemType}\" /><input type=\"hidden\" name=\"quantity\" value=\"{quantity}\" /><input type=\"hidden\" name=\"personal\" value={personal}><input type=\"submit\" value=\"Sell All\" ");
      if (quantity == 0 || price == 0)
      {
        sb.Append("disabled");
      }
      sb.Append(" /></form></td>");

      sb.Append("</tr>");
    }
    sb.Append("</table>");
    return sb.ToString();
  }

  // Get the purchase table.
  private static string GetPurchaseTable()
  {
    StringBuilder sb = new StringBuilder();
    // A table with all the items in the trader's inventory, with buttons to purchase 1 or 100 of the item.
    sb.Append("<table>");
    sb.Append("<tr><th>Item</th><th>Price</th></tr>");
    foreach (var itemtype in trader.inventory.items)
    {
      // Get the default purchase price for the item.
      var singleItem = trader.inventory.Get(itemtype.Key, 1);
      int price = trader.GetPrice(singleItem!, person);
      sb.Append($"<tr><td>{itemtype.Key.itemType}</td><td>{price}</td>");
      // Add buttons to buy 1 or 100 of the item.
      sb.Append($"<td><form action=\"buy\" method=\"get\"><input type=\"hidden\" name=\"item\" value=\"{itemtype.Key.itemType}\" /><input type=\"hidden\" name=\"quantity\" value=\"1\" /><input type=\"submit\" value=\"Buy\" /></form></td>");
      sb.Append($"<td><form action=\"buy\" method=\"get\"><input type=\"hidden\" name=\"item\" value=\"{itemtype.Key.itemType}\" /><input type=\"hidden\" name=\"quantity\" value=\"100\" /><input type=\"submit\" value=\"Buy 100\" /></form></td>");

      sb.Append("</tr>");
    }
    sb.Append("</table>");
    return sb.ToString();
  }

  // Return a web page table with the household's buildings.
  private static string GetBuildingsTable()
  {
    StringBuilder sb = new StringBuilder();
    sb.Append("<table>");
    sb.Append("<tr><th>Building</th><th>Phase</th></tr>");
    // Print and enumerate the buildings.
    foreach (var building in household.buildings)
    {
      sb.Append($"<tr><td>{building.buildingType.name}</td>");
      sb.Append($"<td>{building.currentPhase}</td></tr>");
      sb.Append("</tr>");
    }
    sb.Append("</table>");
    return sb.ToString();
  }

  // Return a table with the person's skills.
  private static string GetSkillsTable()
  {
    StringBuilder sb = new StringBuilder();
    sb.Append("<table>");
    sb.Append("<tr><th>Skill</th><th>Level</th></tr>");
    // Print and enumerate the skills.
    foreach (var skill in person.skills.skills)
    {
      sb.Append($"<tr><td>{skill.Key.id}</td><td>{skill.Value.level}</td><td>({skill.Value.XP} xp)</td></tr>");
    }
    sb.Append("</table>");
    return sb.ToString();
  }

  // Return a table with the person's attributes.
  private static string GetAttributesTable()
  {
    StringBuilder sb = new StringBuilder();
    sb.Append("<table>");
    sb.Append("<tr><th>Attribute</th><th>Value</th></tr>");
    // Print and enumerate the attributes.
    foreach (var attribute in person.attributes.attributes)
    {
      sb.Append($"<tr><td>{attribute.Key.name}</td><td>{attribute.Value.value}</td></tr>");
    }
    sb.Append("</table>");
    return sb.ToString();
  }

  // Return a table with the person's abilities.
  private static string GetAbilitiesTable()
  {
    StringBuilder sb = new StringBuilder();
    sb.Append("<table>");
    sb.Append("<tr><th>Ability</th><th>Level</th></tr>");
    // Print and enumerate the abilities.
    foreach (var ability in person.Abilities)
    {
      sb.Append($"<tr><td>{ability.abilityType}</td></tr>");
    }
    sb.Append("</table>");
    return sb.ToString();
  }

  // Return a table with the person's tasks.
  private static string GetTasksTable(bool personal)
  {
    // Get the set of tasks.
    HashSet<WorkTask> tasks = personal ? person.AvailablePersonalTasks : person.AvailableHouseholdTasks;
    StringBuilder sb = new StringBuilder();
    sb.Append("<table>");
    sb.Append("<tr><th>Task</th></tr>");
    // Print the tasks.
    foreach (var task in tasks)
    {
      // Each task is a row in the table, and is a button that can be
      // clicked to start the task. The button is a form that submits
      // the task ID.
      sb.Append($"<tr><td><form action=\"task\" method=\"get\"><input type=\"hidden\" name=\"personal\" value={personal} ><input type=\"submit\" name=\"task\" value=\"{task.task}\"></form></td></tr>");
    }
    sb.Append("</table>");
    return sb.ToString();
  }

  // Return a table of Tool Crafting tasks.
  private static string GetToolCraftingTable()
  {
    StringBuilder sb = new StringBuilder();
    sb.Append("<table>");
    sb.Append("<tr><th>Tool</th></tr>");
    // Print the tasks.
    foreach (var task in person.AvailableHouseholdTasks)
    {
      if (!task.IsToolCraftingTask())
      {
        continue;
      }

      // Each task is a row in the table, and is a button that can be
      // clicked to start the task. The button is a form that submits
      // the task ID.
      sb.Append($"<tr><td><form action=\"task\" method=\"get\"><input type=\"hidden\" name=\"personal\" value=false ><input type=\"submit\" name=\"task\" value=\"{task.task}\"></form></td></tr>");
    }
    sb.Append("</table>");
    return sb.ToString();
  }

  // Return a table of Gathering tasks.
  private static string GetGatheringTable()
  {
    StringBuilder sb = new StringBuilder();
    sb.Append("<table>");
    sb.Append("<tr><th>Resource</th></tr>");
    // Print the tasks.
    foreach (var task in person.AvailableHouseholdTasks)
    {
      if (!task.IsGatheringTask())
      {
        continue;
      }

      // Each task is a row in the table, and is a button that can be
      // clicked to start the task. The button is a form that submits
      // the task ID.
      sb.Append($"<tr><td><form action=\"task\" method=\"get\"><input type=\"hidden\" name=\"personal\" value=false ><input type=\"submit\" name=\"task\" value=\"{task.task}\"></form></td></tr>");
    }
    sb.Append("</table>");
    return sb.ToString();
  }

  // Return a table of resource processing tasks.
  private static string GetResourceProcessingTable()
  {
    StringBuilder sb = new StringBuilder();
    sb.Append("<table>");
    sb.Append("<tr><th>Resource</th></tr>");
    // Print the tasks.
    foreach (var task in person.AvailableHouseholdTasks)
    {
      if (!task.IsResourceProcessingTask())
      {
        continue;
      }

      // Each task is a row in the table, and is a button that can be
      // clicked to start the task. The button is a form that submits
      // the task ID.
      sb.Append($"<tr><td><form action=\"task\" method=\"get\"><input type=\"hidden\" name=\"personal\" value=false ><input type=\"submit\" name=\"task\" value=\"{task.task}\"></form></td></tr>");
    }
    sb.Append("</table>");
    return sb.ToString();
  }

  // Return a table of building component tasks.
  private static string GetBuildingComponentTable()
  {
    StringBuilder sb = new StringBuilder();
    sb.Append("<table>");
    sb.Append("<tr><th>Building Component</th></tr>");
    // Print the tasks.
    foreach (var task in person.AvailableHouseholdTasks)
    {
      if (task.BuildingComponents().Count == 0)
      {
        continue;
      }

      // Each task is a row in the table, and has a button that can be
      // clicked to start the task. The button is a form that submits
      // the task ID. The form also has a dropdown to select the
      // building target.
      bool printed = false;
      // Enumerate the buildings.
      for (int i = 0; i < household.buildings.Count; i++)
      {
        // Check whether the building needs the component provided by
        // the task.
        foreach (var component in task.BuildingComponents())
        {
          if (household.buildings[i].NeedsComponent(component))
          {
            // if this is the first building that needs the component,
            // add the task to the table.
            if (!printed)
            {
              sb.Append($"<tr><td><form action=\"task\" method=\"get\"><input type=\"hidden\" name=\"personal\" value=false ><input type=\"submit\" name=\"task\" value=\"{task.task}\"><select name=\"target\">");
              printed = true;
            }
            sb.Append($"<option value=\"{i}\">{household.buildings[i].buildingType.name}</option>");
            break;
          }
        }
      }
      if (printed)
      {
        sb.Append("</select></form></td></tr>");
      }
    }
    sb.Append("</table>");
    return sb.ToString();
  }

  // Return a table of other tasks.
  private static string GetOtherTasksTable()
  {
    StringBuilder sb = new StringBuilder();
    sb.Append("<table>");
    sb.Append("<tr><th>Task</th></tr>");
    // Print the tasks.
    foreach (var task in person.AvailableHouseholdTasks)
    {
      if (task.IsToolCraftingTask() || task.IsGatheringTask() || task.IsResourceProcessingTask() || task.BuildingComponents().Count > 0)
      {
        continue;
      }

      // Each task is a row in the table, and is a button that can be
      // clicked to start the task. The button is a form that submits
      // the task ID.
      sb.Append($"<tr><td><form action=\"task\" method=\"get\"><input type=\"hidden\" name=\"personal\" value=false ><input type=\"submit\" name=\"task\" value=\"{task.task}\"></form></td></tr>");
    }
    sb.Append("</table>");
    return sb.ToString();
  }

  // Return a table of buildings that can be built.
  private static string GetConstructionTable()
  {
    StringBuilder sb = new StringBuilder();
    sb.Append("<table>");
    List<BuildingType> availableBuildings = new List<BuildingType>(person.AvailableBuildings);
    availableBuildings.Sort((a, b) => a.name.CompareTo(b.name));
    foreach (var building in availableBuildings)
    {
      // Each building is a row in the table, and is a button that can be
      // clicked to start the building. The button is a form that submits
      // the building ID.
      sb.Append($"<tr><td><form action=\"build\" method=\"get\"><input type=\"submit\" name=\"building\" value=\"{building.name}\"></form></td></tr>");
    }
    sb.Append("</table>");
    return sb.ToString();
  }

}
