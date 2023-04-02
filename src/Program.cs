using Village.Item;

namespace Village
{
    class Program
    {
        // Load configurations from JSON files.
        public static void LoadConfig()
        {
            // Load the item types.
            ItemType.LoadFile("config/items/itemtypes.json");
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Village Entry Point");
            //Load configs
            LoadConfig();
            // Print the item types.
            foreach (ItemType itemType in ItemType.itemTypes.Values)
            {
                Console.WriteLine(itemType.itemType);
            }
        }
    }
}
