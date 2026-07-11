namespace SmartBabySitter.Models;

public class Notification
{
    public int Id { get; set; }

    public int ReceiverUserId { get; set; }
    public ApplicationUser ReceiverUser { get; set; } = default!;

    public NotificationType Type { get; set; } = NotificationType.InApp;
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";

    public bool IsSent { get; set; } = false;
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}