namespace SmartBabySitter.Models;

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    public ICollection<BabySitterSkill> BabySitterSkills { get; set; } = new List<BabySitterSkill>();
}