public class SitterSkill
{
    public int Id { get; set; }
    public string Name { get; set; }

    public int SitterProfileId { get; set; }
    public SitterProfile SitterProfile { get; set; }
}