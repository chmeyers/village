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
    if (totalQuantity >= int.MaxValue)
    {
      return $"(∞, {marginalUtility:F2})";
    }
    return $"({totalQuantity}, {marginalUtility:F2})";
  }

}

public class UtilityQuantityList : List<UtilityQuantity>
{
  public UtilityQuantityList() : base() { }
  public UtilityQuantityList(IEnumerable<UtilityQuantity> collection) : base(collection) { }
  public UtilityQuantityList(int capacity) : base(capacity) { }

  public new void Sort()
  {
    base.Sort();
    // Prune entries that are dominated by other entries.
    for (int i = 1; i < this.Count; i++)
    {
      if (this[i].totalQuantity <= this[i - 1].totalQuantity)
      {
        this.RemoveAt(i);
        i--;
      }
      else
      {
        this[i].marginalQuantity = this[i].totalQuantity - this[i - 1].totalQuantity;
      }
    }
    // Fix up the first element if necessary.
    if (this.Count > 0)
    {
      this[0].marginalQuantity = this[0].totalQuantity;
    }
  }

  // Combine the second list of desire utilities into the first,
  // maintaining the sort order.
  public UtilityQuantityList Merge(UtilityQuantityList from)
  {
    foreach (var element in from)
    {
      this.Add(element.Clone());
    }
    Sort();
    return this;
  }

  public UtilityQuantityList Clone()
  {
    var result = new UtilityQuantityList();
    foreach (var element in this)
    {
      result.Add(element.Clone());
    }
    return result;
  }

  public static UtilityQuantityList Merge(UtilityQuantityList a, UtilityQuantityList b)
  {
    return a.Clone().Merge(b);
  }


  // Merges everything except with at least the given total quantity.
  public UtilityQuantityList MergeExceptQuantity(UtilityQuantityList other, int quantity)
  {
    for (int i = 0; i < other.Count; i++)
    {
      if (other[i].totalQuantity > quantity)
      {
        this.Add(other[i].Clone());
      }
    }
    // Sort the result.
    Sort();

    return this;
  }

  public double? GetLastUtility()
  {
    if (this.Count == 0)
    {
      return null;
    }
    // return the utility of the last element.
    return this[this.Count - 1].marginalUtility;
  }

  public double? GetFirstUtility()
  {
    if (this.Count == 0)
    {
      return null;
    }
    // return the utility of the first element.
    return this[0].marginalUtility;
  }

  public override string ToString()
  {
    return $"[{string.Join(", ", this)}]";
  }

}

