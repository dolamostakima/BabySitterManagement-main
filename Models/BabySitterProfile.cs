namespace SmartBabySitter.Models;

public class BabySitterProfile
{
   

    public int Id { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;


    public string SkillsText { get; set; } = ""; // quick display (optional)
    public int ExperienceYears { get; set; }

    public decimal HourlyRate { get; set; }
    public string LocationText { get; set; } = "";
    public string? PhotoUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool IsApproved { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // navigation
    public ICollection<BabySitterSkill> BabySitterSkills { get; set; } = new List<BabySitterSkill>();
    public ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    
}