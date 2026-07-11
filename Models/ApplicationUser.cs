using Microsoft.AspNetCore.Identity;
using System.Net;

namespace SmartBabySitter.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string NidNo { get; set; } = "";
    public string Gender { get; set; } = "";
    public string DateOfBirth { get; set; } = "";
    public string Address { get; set; } = "";
    public string PhoneNo { get; set; } = "";
    public string Password { get; set; } = "";
    public string Experience { get; set; } = "";
    public string Type { get; set; } = "";


    // optional profile fields
    public string? DefaultLocationText { get; set; }

    // navigation
    public ICollection<Booking> BookingsAsParent { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}