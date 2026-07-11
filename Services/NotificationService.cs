using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;

namespace SmartBabySitter.Services;

public interface INotificationService
{
    Task<int> CreateInAppAsync(int receiverUserId, string title, string message);
    Task MarkSentAsync(int notificationId);
    Task SendEmailAsync(string toEmail, string subject, string body); // placeholder
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateInAppAsync(int receiverUserId, string title, string message)
    {
        var n = new Notification
        {
            ReceiverUserId = receiverUserId,
            Type = NotificationType.InApp,
            Title = title,
            Message = message,
            IsSent = true,
            SentAt = DateTime.UtcNow
        };

        _db.Notifications.Add(n);
        await _db.SaveChangesAsync();
        return n.Id;
    }

    public async Task MarkSentAsync(int notificationId)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId);
        if (n == null) return;

        n.IsSent = true;
        n.SentAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public Task SendEmailAsync(string toEmail, string subject, string body)
    {
        // এখানে SMTP / SendGrid / provider বসবে
        return Task.CompletedTask;
    }
}