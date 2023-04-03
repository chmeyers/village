using Village.Ability;
using Village.Effects;
using Village.Item;

namespace Village
{
    class Program
    {
        // Load configurations from JSON files.
        public static void LoadConfig()
        {
            // Load the ability types.
            AbilityType.LoadFile("config/abilities/abilitytypes.json");
            // Load the item types.
            ItemType.LoadFile("config/items/itemtypes.json");
            // Load the effects.
            Effect.LoadFile("config/effects/effects.json");
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
            // Print the ability types.
            foreach (AbilityType abilityType in AbilityType.abilityTypes.Values)
            {
                Console.WriteLine(abilityType.abilityType);
            }
            // Print the effects.
            foreach (Effect effect in Effect.effects.Values)
            {
                Console.WriteLine(effect.effect);
            }
        }
    }
}
