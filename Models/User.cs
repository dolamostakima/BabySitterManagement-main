
namespace SmartBabySitter.Models
{
    public class User
    {

        public int Id { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PasswordHash { get; set; }

        public string Location { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Role { get; set; }  // Parent / BabySitter / Admin

        // Navigation
        public ICollection<Booking> Bookings { get; set; }
        public ICollection<Review> Reviews { get; set; }
    }
}
