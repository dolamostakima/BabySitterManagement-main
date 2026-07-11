
namespace SmartBabySitter.Models
{
    public class BabySitter
    {
        public int Id { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PasswordHash { get; set; }

        public string BabySitterSkill { get; set; }
        public int ExperienceYears { get; set; }

        public decimal HourlyRate { get; set; }
        public string Location { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;


        // Navigation
        public ICollection<Booking> Bookings { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<Availability> Availabilities { get; set; }
    }
}

