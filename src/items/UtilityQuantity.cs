namespace Village.Items;

// How desirable is a given quantity of a given item.
public class UtilityQuantity : IComparable<UtilityQuantity>
{
  // Constructor
  public UtilityQuantity(int totalQuantity, int marginalQuantity, double marginalUtility)
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
  public UtilityQuantity Clone()
  {
    return new UtilityQuantity(totalQuantity, marginalQuantity, marginalUtility);
  }

  // Sort order should be from highest marginal utility to lowest,
  // then from highest total quantity to lowest, then from
  // highest marginal quantity to lowest.
  public int CompareTo(UtilityQuantity? other)
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
    return obj is UtilityQuantity utility &&
           totalQuantity == utility.totalQuantity &&
           marginalQuantity == utility.marginalQuantity &&
           marginalUtility == utility.marginalUtility;
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(totalQuantity, marginalQuantity, marginalUtility);
  }

  public override string ToString()
  {
    // For friendly debugging, print the total quantity and
    // marginal utility, rounded to 2 decimal places.
    return $"({totalQuantity}, {marginalUtility:F2})";
  }

  public static void Sort(List<UtilityQuantity> list)
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
  public static void MergeFrom(List<UtilityQuantity> to, List<UtilityQuantity> from)
  {
    foreach (var element in from)
    {
      to.Add(element.Clone());
    }
    Sort(to);
  }

  public static List<UtilityQuantity> Merge(List<UtilityQuantity> a, List<UtilityQuantity> b)
  {
    var result = new List<UtilityQuantity>();
    foreach (var element in a)
    {
      result.Add(element.Clone());
    }

    MergeFrom(result, b);
    return result;
  }

  // Merges everything except the first element of the second list.
  public static List<UtilityQuantity> MergeExceptQuantity(List<UtilityQuantity> a, List<UtilityQuantity> b, int quantity)
  {
    var result = new List<UtilityQuantity>();
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

