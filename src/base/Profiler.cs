

using System.Diagnostics;

public class Profiler
{
  public static Dictionary<string, KeyValuePair<long, double>> samples = new Dictionary<string, KeyValuePair<long, double>>();
  public static Stopwatch Start()
  {
    return Stopwatch.StartNew();
  }

  public static Stopwatch AddSample(string name, Stopwatch watch)
  {
    var elapsed = watch.ElapsedTicks;
    if (samples.ContainsKey(name))
    {
      var sample = samples[name];
      samples[name] = new KeyValuePair<long, double>(sample.Key + 1, sample.Value + elapsed);
    }
    else
    {
      samples[name] = new KeyValuePair<long, double>(1, elapsed);
    }
    watch.Restart();
    return watch;
  }
}