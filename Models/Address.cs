namespace SmartBabySitter.Models;

public class Address
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public string Label { get; set; } = "Home"; // Home/Office
    public string City { get; set; } = "";
    public string Area { get; set; } = "";
    public string AddressLine { get; set; } = "";

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool IsDefault { get; set; } = false;
}