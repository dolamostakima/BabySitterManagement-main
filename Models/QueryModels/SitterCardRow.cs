using Microsoft.EntityFrameworkCore;

namespace SmartBabySitter.Models.QueryModels;

[Keyless]
public class SitterCardRow
{
    public int BabySitterProfileId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public decimal HourlyRate { get; set; }
    public int ExperienceYears { get; set; }
    public string LocationText { get; set; } = "";
    public bool IsApproved { get; set; }
    public double AvgRating { get; set; }
    public int ReviewCount { get; set; }
}