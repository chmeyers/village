using Newtonsoft.Json;
using Village.Abilities;
using Village.Effects;

namespace Village.Skills;

// A single level in a skill.
public class SkillLevel
{
  // XP required to reach this level, relative to the previous level.
  public int xp { get; private set; }
  // Abilities required to reach or gain xp in this level.
  public HashSet<AbilityType> requirements { get; private set; } = new HashSet<AbilityType>();
  // Abilities gained by reaching this level.
  public HashSet<AbilityType> abilities { get; private set; } = new HashSet<AbilityType>();
  // Effects run when this level is reached. Every effect must target the person.
  public HashSet<Effect> effects { get; private set; } = new HashSet<Effect>();

  public static SkillLevel FromJson(Newtonsoft.Json.Linq.JToken? json)
  {
    if (json == null)
    {
      throw new Exception("Failed to load skill level from json");
    }
    var level = new SkillLevel();
    var dict = json.ToObject<Dictionary<string, object>>();
    if (dict == null)
    {
      throw new Exception("Failed to load skill level dict from json");
    }
    level.xp = (int)(long)dict["xp"];
    List<string>? requirements = dict.ContainsKey("requirements") ? ((Newtonsoft.Json.Linq.JArray)dict["requirements"]).ToObject<List<string>>() : null;
    if (requirements != null)
    {
      foreach (var requirement in requirements)
      {
        var req = AbilityType.Find(requirement);
        if (req == null)
        {
          throw new Exception("Failed to find required ability: " + requirement + " for skill level: " + json.ToString());
        }
        level.requirements.Add(req);
      }
    }

    List<string>? abilities = dict.ContainsKey("abilities") ? ((Newtonsoft.Json.Linq.JArray)dict["abilities"]).ToObject<List<string>>() : null;
    if (abilities != null)
    {
      foreach (var ability in abilities)
      {
        var ab = AbilityType.Find(ability);
        if (ab == null)
        {
          throw new Exception("Failed to find ability: " + ability + " for skill level: " + json.ToString());
        }
        level.abilities.Add(ab);
      }
    }
    List<string>? effects = dict.ContainsKey("effects") ? ((Newtonsoft.Json.Linq.JArray)dict["effects"]).ToObject<List<string>>() : null;
    if (effects != null)
    {
      foreach (var effect in effects)
      {
        var ef = Effect.Find(effect);
        if (ef == null)
        {
          throw new Exception("Failed to find effect: " + effect + " for skill level: " + json.ToString());
        }
        if (ef.target != EffectTargetType.Person)
        {
          throw new Exception("SkillLevel effects must target the person: " + effect + " for skill level: " + json.ToString());
        }
        level.effects.Add(ef);
      }
    }
    return level;
  }

}

// A skill is a learnable trait for a person.
// It unlocks abilities and effects as it is leveled up.
public class Skill
{
  // Dictionary of all the skills.
  public static Dictionary<string, Skill> skills { get; private set;} = new Dictionary<string, Skill>();
  // Find a skill by ID.
  public static Skill? Find(string id)
  {
    if (skills.ContainsKey(id))
    {
      return skills[id];
    }
    return null;
  }
  public static void Clear()
  {
    skills.Clear();
  }
  // ID of the skill.
  public string id { get; }
  // List of skill levels.
  public List<SkillLevel> levels { get; private set; } = new List<SkillLevel>();
  // Pointer to the parent skills.
  public HashSet<Skill> parents { get; private set; } = new HashSet<Skill>();
  // Pointers to the child skills.
  public HashSet<Skill> children { get; private set; } = new HashSet<Skill>();

  // The bracket operator allows you to get the skill level by index.
  public SkillLevel this[int index]
  {
    get
    {
      return levels[index];
    }
  }
  // Constructor.
  private Skill(string id)
  {
    this.id = id;
  }

  // Load the skills from the given json.
  public static void Load(Dictionary<string, List<object>> data)
  {
    // Iterate over the skills
    foreach (var skill in data)
    {
      // Create the skill
      var s = new Skill(skill.Key);
      // Iterate over the levels
      foreach (var level in skill.Value)
      {
        // Add the level
        s.levels.Add(SkillLevel.FromJson((Newtonsoft.Json.Linq.JToken)level));
      }
      // Add the skill to the dictionary
      skills.Add(skill.Key, s);

    }
  }

  // Loader funtion for a json string.
  public static void LoadString(string json)
  {
    var data = JsonConvert.DeserializeObject<Dictionary<string, List<object>>>(json);
    if (data == null)
    {
      throw new Exception("Failed to load skills from json");
    }
    Load(data);
  }

  // Loader function for a json file.
  public static void LoadFile(string filename)
  {
    var json = File.ReadAllText(filename);
    try
    {
      LoadString(json);
    }
    catch (Exception e)
    {
      throw new Exception("Failed to load skills from file: " + filename + "\n" + e.Message);
    }
  }

  // Load the parents from the given json.
  public static void LoadParents(Dictionary<string, List<string>> data)
  {
    // Iterate over the skills
    foreach (var skill in data)
    {
      // Find the skill
      var s = Find(skill.Key);
      if (s == null)
      {
        throw new Exception("Failed to find skill: " + skill.Key + " as a parent");
      }
      // Iterate over the children
      foreach (var child in skill.Value)
      {
        // Find the child
        var c = Find(child);
        if (c == null)
        {
          throw new Exception("Failed to find child skill: " + child + " for skill: " + skill.Key);
        }
        // Set the parent and child
        s.children.Add(c);
        c.parents.Add(s);
      }
    }
  }

  // Loader function for the parents json string.
  public static void LoadParentsString(string json)
  {
    var data = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
    if (data == null)
    {
      throw new Exception("Failed to load skill parents from json");
    }
    LoadParents(data);
  }

  // Loader function for the parents file.
  public static void LoadParentsFile(string filename)
  {
    var json = File.ReadAllText(filename);
    try
    {
      LoadParentsString(json);
    }
    catch (Exception e)
    {
      throw new Exception("Failed to load skill parents from file: " + filename + "\n" + e.Message);
    }
  }
}

public interface ISkillContext : IAbilityContext
{
  // Get the skill for the given skill ID.
  public PersonSkill? GetSkill(Skill skill);
}

// A skill for a particular person.
public class PersonSkill
{
  // Pointer to the skill.
  public Skill skill { get; }
  // Current level of the skill. 1-based.
  public int level { get ; private set; }
  // XP that was required to reach the current level.
  private int xpBase;
  // Current XP of the skill since the last level.
  private int xp;
  // Total XP of the skill.
  public int XP { get { return xpBase + xp; } }

  // Constructor.
  public PersonSkill(Skill skill)
  {
    this.skill = skill;
    this.level = 0;
    this.xpBase = 0;
    this.xp = 0;
  }

  // Re-apply the abilities for all the levels up to the current level.
  // This is used when a person is loaded from a save, as the abilities
  // associated with the skill might have changed.
  // Note that we don't remove abilities that are no longer granted, nor
  // do we run the effects for the levels that have already been reached.
  public void RefreshAbilities(ISkillContext context)
  {
    for (int i = 0; i < level; i++)
    {
      foreach (var ability in skill.levels[i].abilities)
      {
        context.GrantAbility(ability);
      }
    }
  }

  private bool meetsRequirements(IAbilityContext context, SkillLevel level)
  {
    var contextAbilities = context.Abilities;
    foreach (var requirement in level.requirements)
    {
      if (!contextAbilities.Contains(requirement))
      {
        return false;
      }
    }
    return true;
  }


  // Grant a particular level of the skill.
  private bool GrantLevel(ISkillContext context, int level)
  {
    if (level <= 0 || level > skill.levels.Count)
    {
      return false;
    }
    if (level <= this.level)
    {
      // They already have this level.
      return true;
    }
    // Check the requirements for this level.
    if (!meetsRequirements(context, skill.levels[level - 1]))
    {
      return false;
    }
    this.level = level;
    // Sum the XP required to reach this level and save that as the base.
    this.xpBase = 0;
    for (int i = 0; i <= level - 1; i++)
    {
      this.xpBase += skill.levels[i].xp;
    }
    this.xp = 0;
    foreach (var ability in skill.levels[level - 1].abilities)
    {
      context.GrantAbility(ability);
    }
    foreach (var effect in skill.levels[level - 1].effects)
    {
      // Apply the effect, the target is always the person whose skill this is.
      effect.Apply(new ChosenEffectTarget(EffectTargetType.Person, context, context, context));
    }
    return true;
  }

  private bool GrantLevel(ISkillContext context)
  {
    return GrantLevel(context, this.level + 1);
  }

  // Grant XP to the skill.
  private bool GrantXP(ISkillContext context, int xp)
  {
    if (xp <= 0)
    {
      return false;
    }
    // If we have enough XP to reach the next level, give just that much.
    // Then we have to check if we can level up multiple times.
    bool gaveXP = false;
    while (xp > 0)
    {
      // If we are at the max level, we can't give any more XP.
      if (level >= skill.levels.Count)
      {
        return gaveXP;
      }
      // Check the requirements for the next level to see if we are allowed to give xp.
      if (!meetsRequirements(context, skill.levels[level]))
      {
        return gaveXP;
      }
      int xpToNextLevel = skill.levels[level].xp - this.xp;
      if (xpToNextLevel <= xp)
      {
        // We have enough XP to reach the next level.
        gaveXP = true;
        xp -= xpToNextLevel;
        GrantLevel(context);
      }
      else
      {
        // We don't have enough XP to reach the next level.
        this.xp += xp;
        break;
      }
    }
    return true;
  }

  // As a person can grant a skill to another person (i.e. teach them),
  // we look up the skill ourselves to ensure it's given in the correct context.
  public static bool GrantLevel(ISkillContext context, Skill skill, int level)
  {
    var personSkill = context.GetSkill(skill);
    if (personSkill == null)
    {
      return false;
    }
    return personSkill.GrantLevel(context, level);
  }

  public static bool GrantLevel(ISkillContext context, Skill skill)
  {
    var personSkill = context.GetSkill(skill);
    if (personSkill == null)
    {
      return false;
    }
    return personSkill.GrantLevel(context);
  }

  public static bool GrantXP(ISkillContext context, Skill skill, int xp)
  {
    var personSkill = context.GetSkill(skill);
    if (personSkill == null)
    {
      return false;
    }
    return personSkill.GrantXP(context, xp);
  }

}