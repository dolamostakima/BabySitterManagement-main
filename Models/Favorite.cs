namespace SmartBabySitter.Models;

public class Favorite
{
    public int Id { get; set; }

    public int ParentUserId { get; set; }
    public ApplicationUser ParentUser { get; set; } = default!;

    public int BabySitterProfileId { get; set; }
    public BabySitterProfile BabySitterProfile { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}