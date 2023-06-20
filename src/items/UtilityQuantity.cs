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

  public static bool operator <(UtilityQuantity a, UtilityQuantity b)
  {
    return a.CompareTo(b) < 0;
  }

  public static bool operator >(UtilityQuantity a, UtilityQuantity b)
  {
    return a.CompareTo(b) > 0;
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
    string utility = "";
    if (marginalUtility >= int.MaxValue)
    {
      utility = "∞";
    }
    else if (marginalUtility <= int.MinValue)
    {
      utility = "-∞";
    }
    else
    {
      utility = $"{marginalUtility:F2}";
    }
    if (totalQuantity >= int.MaxValue)
    {
      return $"(∞, {utility})";
    }
    return $"({totalQuantity}, {utility})";
  }

}

public class UtilityQuantityList : List<UtilityQuantity>
{
  public UtilityQuantityList() : base() { }
  public UtilityQuantityList(IEnumerable<UtilityQuantity> collection) : base(collection) { }
  public UtilityQuantityList(int capacity) : base(capacity) { }

  public new void Sort()
  {
    SetMarginals();
  }

  public void SetMarginals()
  {
    base.Sort();
    // Assume that the total quantities are correct, and set the marginal
    // quantities by subtracting the previous total quantity.
    for (int i = 1; i < this.Count; i++)
    {
      if (this[i].totalQuantity <= this[i - 1].totalQuantity)
      {
        // This entry would have <=0 marginal quantity, so remove it.
        this.RemoveAt(i);
        i--;
      }
      else if (this[i].marginalUtility == this[i - 1].marginalUtility)
      {
        // This entry has the same marginal utility as the previous one,
        // so merge the two entries.
        this[i - 1].marginalQuantity += this[i].totalQuantity - this[i - 1].totalQuantity;
        this[i - 1].totalQuantity = this[i].totalQuantity;
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

  public void SetTotals()
  {
    base.Sort();
    // Assume that the marginal quantities are correct, and set the totals
    // by adding them up.
    int runningTotal = 0;
    for (int i = 0; i < this.Count; i++)
    {
      // If the marginal utility is the same as the previous one, then
      // merge the two entries.
      if (i > 0 && this[i].marginalUtility == this[i - 1].marginalUtility)
      {
        this[i - 1].marginalQuantity += this[i].marginalQuantity;
        this[i - 1].totalQuantity += this[i].marginalQuantity;
        this.RemoveAt(i);
        i--;
        continue;
      }
      runningTotal += this[i].marginalQuantity;
      this[i].totalQuantity = runningTotal;
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
    SetMarginals();
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
    SetMarginals();

    return this;
  }

  public UtilityQuantityList FilterByBudget(double budget)
  {
    for (int i = 0; i < this.Count; i++)
    {
      if (-this[i].marginalUtility * this[i].marginalQuantity <= budget)
      {
        budget -= -this[i].marginalUtility * this[i].marginalQuantity;
      }
      else
      {
        // Keep a fractional amount of the last element.
        int inBudget = (int)(budget / -this[i].marginalUtility);
        if (inBudget > 0)
        {
          this[i].totalQuantity -= this[i].marginalQuantity - inBudget;
          this[i].marginalQuantity = inBudget;
        }
        else
        {
          // Decrement i so that this element is removed.
          i--;
        }
        // Completely remove all subsequent elements.
        this.RemoveRange(i + 1, this.Count - i - 1);
        break;
      }
    }
    return this;
  
  }

  // Stack the two lists, adding the marginal quantities together.
  public static UtilityQuantityList Stack(UtilityQuantityList? a, UtilityQuantityList? b)
  {
    if (a == null)
    {
      return b?.Clone() ?? new UtilityQuantityList();
    }
    if (b == null)
    {
      return a.Clone();
    }
    var result = new UtilityQuantityList();
    int i = 0;
    int j = 0;
    int runningTotal = 0;
    while (i < a.Count || j < b.Count)
    {
      if (i >= a.Count)
      {
        result.Add(b[j].Clone());
        j++;
      }
      else if (j >= b.Count)
      {
        result.Add(a[i].Clone());
        i++;
      }
      else if (a[i].marginalUtility > b[j].marginalUtility)
      {
        result.Add(a[i].Clone());
        i++;
      }
      else if (a[i].marginalUtility < b[j].marginalUtility)
      {
        result.Add(b[j].Clone());
        j++;
      }
      else
      {
        result.Add(new UtilityQuantity(0, a[i].marginalQuantity + b[j].marginalQuantity, a[i].marginalUtility));
        i++;
        j++;
      }
      // Fix the total quantity of the element we added.
      runningTotal += result[result.Count - 1].marginalQuantity;
      result[result.Count - 1].totalQuantity = runningTotal;
    }
    return result;
  }

  public double? GetLastUtility()
  {
    if (this.Count == 0)
    {
      return null;
    }
    // return the utility of the last element if it has MaxInt total quantity, otherwise
    // retun null to indicate they should use a fallback value.
    if (this[this.Count - 1].totalQuantity == int.MaxValue)
    {
      return this[this.Count - 1].marginalUtility;
    }
    return null;
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

  public double? GetUtility(int quantity)
  {
    double total = 0;
    foreach (var element in this)
    {
      total += element.marginalUtility * Math.Min(element.marginalQuantity, quantity);
      quantity -= element.marginalQuantity;
      if (quantity <= 0)
      {
        return total;
      }
    }
    return null;
  }

  public double GetTotalUtility()
  {
    // Add up all the marginal utilities.
    double total = 0;
    foreach (var element in this)
    {
      total += element.marginalUtility * element.marginalQuantity;
    }
    return total;
  }

  public override string ToString()
  {
    return $"[{string.Join(", ", this)}]";
  }

}

public class UtilityCacheValue
{
  public UtilityCacheValue(long expiry, UtilityQuantityList values)
  {
    this.expiry = expiry;
    this.values = values;
  }

  public long expiry;
  public UtilityQuantityList values;

  public override string ToString()
  {
    // For friendly printing, create a comma separated list of the values.
    return string.Join(", ", values);
  }
}

