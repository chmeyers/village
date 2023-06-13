namespace Village.Items;

// How desirable is a given quantity of a given item.
public class DesireUtility : IComparable<DesireUtility>
{
  // Constructor
  public DesireUtility(int totalQuantity, int marginalQuantity, double marginalUtility)
  {
    this.totalQuantity = totalQuantity;
    this.marginalQuantity = marginalQuantity;
    this.marginalUtility = marginalUtility;
  }
  // Total quantity of the item that is at least this desirable.
  public int totalQuantity;
  // Marginal quantity of the item that is this desirable.
  public int marginalQuantity;
  // Marginal desirability of the item.
  public double marginalUtility;

  // Clone
  public DesireUtility Clone()
  {
    return new DesireUtility(totalQuantity, marginalQuantity, marginalUtility);
  }

  // Sort order should be from highest marginal utility to lowest,
  // then from highest total quantity to lowest, then from
  // highest marginal quantity to lowest.
  public int CompareTo(DesireUtility? other)
  {
    if (other == null)
    {
      return 1;
    }
    if (other.marginalUtility != marginalUtility)
    {
      return other.marginalUtility.CompareTo(marginalUtility);
    }
    if (other.totalQuantity != totalQuantity)
    {
      return other.totalQuantity.CompareTo(totalQuantity);
    }
    return other.marginalQuantity.CompareTo(marginalQuantity);
  }

  public override bool Equals(object? obj)
  {
    return obj is DesireUtility utility &&
           totalQuantity == utility.totalQuantity &&
           marginalQuantity == utility.marginalQuantity &&
           marginalUtility == utility.marginalUtility;
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(totalQuantity, marginalQuantity, marginalUtility);
  }

  public static void Sort(List<DesireUtility> list)
  {
    list.Sort();
    // Prune entries that are dominated by other entries.
    for (int i = 1; i < list.Count; i++)
    {
      if (list[i].totalQuantity <= list[i - 1].totalQuantity)
      {
        list.RemoveAt(i);
        i--;
      }
      else
      {
        list[i].marginalQuantity = list[i].totalQuantity - list[i - 1].totalQuantity;
      }
    }
    // Fix up the first element if necessary.
    if (list.Count > 0)
    {
      list[0].marginalQuantity = list[0].totalQuantity;
    }
  }
  // Combine the second list of desire utilities into the first,
  // maintaining the sort order.
  public static void MergeFrom(List<DesireUtility> to, List<DesireUtility> from)
  {
    foreach (var element in from)
    {
      to.Add(element.Clone());
    }
    Sort(to);
  }

  public static List<DesireUtility> Merge(List<DesireUtility> a, List<DesireUtility> b)
  {
    var result = new List<DesireUtility>();
    foreach (var element in a)
    {
      result.Add(element.Clone());
    }

    MergeFrom(result, b);
    return result;
  }

  // Merges everything except the first element of the second list.
  public static List<DesireUtility> MergeExceptQuantity(List<DesireUtility> a, List<DesireUtility> b, int quantity)
  {
    var result = new List<DesireUtility>();
    foreach (var element in a)
    {
      result.Add(element.Clone());
    }
    for (int i = 0; i < b.Count; i++)
    {
      if (b[i].totalQuantity > quantity)
      {
        result.Add(b[i].Clone());
      }
    }
    // Sort the result.
    Sort(result);
    
    return result;
  }
}

