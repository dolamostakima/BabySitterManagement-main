using System.ComponentModel.DataAnnotations.Schema;

public class SitterProfileDto
{

    public IFormFile? Image { get; set; }

    //public string ImagePath { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? MobileNo { get; set; }

    public string? Nid { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? PhotoUrl { get; set; }

    public string? SkillsText { get; set; }
    public int? ExperienceYears { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? LocationText { get; set; }

    public List<string>? Skills { get; set; }
}