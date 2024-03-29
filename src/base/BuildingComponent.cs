
using Village.Items;

namespace Village.Base;

// A Building Component is a named object that must be completed
// in order to finish a building phase. Like a roof or walls.
// Generally they are provided by tasks, but the building doesn't
// care which specific task provides them.
// Different tasks may provide different types of the same component.
// i.e. a roof might be thatched, wood, or tile.
public class BuildingComponent
{
  // The name of the building component.
  public string name { get; private set; }

  // The specific built component.
  public string? builtComponent { get; set; } = null;

  // The specific quality of the built component.
  public int? builtQuality { get; set; } = null;

  // The current quality of the component. Once this reaches
  // zero, the component is broken and must be replaced or repaired.
  // A building with a broken component no longer provides its
  // benefits.
  public int? currentQuality { get; set; } = null;

  // Scrap produced when the component is broken.
  public Dictionary<ItemType, int>? scrapItems;

  // Constructor from JSON dictionary.
  public BuildingComponent(string name)
  {
    this.name = name;
  }

  // Two building components are equal if they have the same name.
  public override bool Equals(object? obj)
  {
    if (obj == null || GetType() != obj.GetType())
    {
      return false;
    }
    BuildingComponent other = (BuildingComponent)obj;
    return name == other.name;
  }

  // The hash code of a building component is the hash code of its name.
  public override int GetHashCode()
  {
    return name.GetHashCode();
  }

  // Implicit conversion from string to building component.
  public static implicit operator BuildingComponent(string name)
  {
    return new BuildingComponent(name);
  }
}