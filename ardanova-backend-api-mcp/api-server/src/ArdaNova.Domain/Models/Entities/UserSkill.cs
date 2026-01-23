namespace ArdaNova.Domain.Models.Entities;

public class UserSkill
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Skill { get; private set; } = null!;
    public int Level { get; private set; }

    // Navigation property
    public User User { get; private set; } = null!;

    private UserSkill() { }

    public static UserSkill Create(Guid userId, string skill, int level = 1)
    {
        return new UserSkill
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Skill = skill,
            Level = Math.Clamp(level, 1, 10)
        };
    }

    public void UpdateLevel(int level)
    {
        Level = Math.Clamp(level, 1, 10);
    }
}
