namespace SmartBabySitter.Models;

public class BabySitterSkill
{
    public int BabySitterProfileId { get; set; }
    public BabySitterProfile BabySitterProfile { get; set; } = default!;

    public int SkillId { get; set; }
    public Skill Skill { get; set; } = default!;
}