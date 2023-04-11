using Village.Abilities;

namespace Village.Skills;

// A SkillSet contains the set of skills for a person.
public class SkillSet
{
  private Dictionary<Skill, PersonSkill> _skills = new Dictionary<Skill, PersonSkill>();
  // lock for the skill dictionary.
  private object _lock = new object();

  // The target and context for any effects run.
  // Typically this will be the person the skills belong to.
  private ISkillContext _context;

  // Constructor for a SkillSet.
  public SkillSet(ISkillContext context)
  {
    this._context = context;
  }

  // Add a skill to the set, it starts at level 0.
  public void Add(Skill skill)
  {
    lock (_lock)
    {
      AddNoLock(skill);
    }
  }

  // Add without the lock.
  private void AddNoLock(Skill skill)
  {
    var personSkill = new PersonSkill(_context, skill);
    _skills.Add(skill, personSkill);
  }

  // Get the skill for the given skill ID.
  public PersonSkill? GetSkill(Skill skill)
  {
    lock (_lock)
    {
      if (_skills.TryGetValue(skill, out var personSkill))
      {
        return personSkill;
      }
      return null;
    }
  }

  public bool GrantXP(Skill skill, int xp)
  {
    lock (_lock)
    {
      if (_skills.TryGetValue(skill, out var personSkill))
      {
        return personSkill.GrantXP(xp);
      }
      return false;
    }
  }
  // Grant a level to the given skill.
  // Returns true if a level was granted.
  public bool GrantLevel(Skill skill)
  {
    lock (_lock)
    {
      if (_skills.TryGetValue(skill, out var personSkill))
      {
        return personSkill.GrantLevel();
      }
      return false;
    }
  }
  // Grant a specific level to the given skill.
  // Returns true if a level was granted.
  public bool GrantLevel(Skill skill, int level)
  {
    lock (_lock)
    {
      if (_skills.TryGetValue(skill, out var personSkill))
      {
        return personSkill.GrantLevel(level);
      }
      return false;
    }
  }

  // Get the current level of the given skill.
  public int GetLevel(Skill skill)
  {
    lock (_lock)
    {
      if (_skills.TryGetValue(skill, out var personSkill))
      {
        return personSkill.level;
      }
      return 0;
    }
  }

  // Get the current XP of the given skill.
  public int GetXP(Skill skill)
  {
    lock (_lock)
    {
      if (_skills.TryGetValue(skill, out var personSkill))
      {
        return personSkill.XP;
      }
      return 0;
    }
  }



}