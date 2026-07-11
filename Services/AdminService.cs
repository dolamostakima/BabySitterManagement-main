using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;

namespace SmartBabySitter.Services;

public interface IAdminService
{
    Task<object> GetDashboardAsync();
    Task<object> GetRevenueMonthlyAsync(int year);
    Task<object> GetCalendarAsync(DateTime from, DateTime to);
}

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _db;

    public AdminService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<object> GetDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var yearStart = new DateTime(now.Year, 1, 1);

        var total = await _db.Bookings.CountAsync();
        var pending = await _db.Bookings.CountAsync(x => x.Status == BookingStatus.Pending);
        var accepted = await _db.Bookings.CountAsync(x => x.Status == BookingStatus.Accepted);
        var confirmed = await _db.Bookings.CountAsync(x => x.Status == BookingStatus.Confirmed);
        var completed = await _db.Bookings.CountAsync(x => x.Status == BookingStatus.Completed);
        var cancelled = await _db.Bookings.CountAsync(x => x.Status == BookingStatus.Cancelled);
        var rejected = await _db.Bookings.CountAsync(x => x.Status == BookingStatus.Rejected);

        var totalRevenue = await _db.Payments
            .Where(x => x.Status == PaymentStatus.Paid)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var monthlyRevenue = await _db.Payments
            .Where(x => x.Status == PaymentStatus.Paid && x.PaidAt != null && x.PaidAt >= monthStart)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        var yearlyRevenue = await _db.Payments
            .Where(x => x.Status == PaymentStatus.Paid && x.PaidAt != null && x.PaidAt >= yearStart)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        return new
        {
            bookings = new { total, pending, accepted, confirmed, completed, cancelled, rejected },
            revenue = new { totalRevenue, monthlyRevenue, yearlyRevenue }
        };
    }

    public async Task<object> GetRevenueMonthlyAsync(int year)
    {
        year = Math.Clamp(year, 2000, 2100);
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);

        return await _db.Payments.AsNoTracking()
            .Where(x => x.Status == PaymentStatus.Paid && x.PaidAt != null && x.PaidAt >= start && x.PaidAt < end)
            .GroupBy(x => x.PaidAt!.Value.Month)
            .Select(g => new { month = g.Key, total = g.Sum(x => x.Amount), count = g.Count() })
            .OrderBy(x => x.month)
            .ToListAsync();
    }

    public async Task<object> GetCalendarAsync(DateTime from, DateTime to)
    {
        var f = from.Date;
        var t = to.Date;

        return await _db.Bookings.AsNoTracking()
            .Where(x => x.BookingDate >= f && x.BookingDate <= t)
            .Select(x => new
            {
                x.Id,
                status = x.Status.ToString(),
                x.BookingDate,
                x.StartTime,
                x.EndTime,
                x.TotalAmount,
                x.ServiceAddressText
            })
            .ToListAsync();
    }
}