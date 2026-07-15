using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;
using SmartBabySitter.Services.DTOs;

namespace SmartBabySitter.Services;

public interface IAttendanceService
{
    Task CheckInAsync(AttendanceCheckInDto dto);
    Task CheckOutAsync(int bookingId);
    Task<AttendanceDetailsDto?> GetByBookingAsync(int bookingId);

    Task<List<AttendanceDetailsDto>> GetMyAttendanceAsync();

    Task<List<AttendanceDetailsDto>> GetParentAttendanceAsync();
}

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _me;
    private readonly INotificationService _notifications;

    public AttendanceService(
        ApplicationDbContext db,
        ICurrentUser me,
        INotificationService notifications)
    {
        _db = db;
        _me = me;
        _notifications = notifications;
    }

    public async Task CheckInAsync(AttendanceCheckInDto dto)
    {
        var booking = await _db.Bookings
            .Include(b => b.BabySitterProfile)
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.BabySitterProfile.UserId != _me.UserId)
            throw new UnauthorizedAccessException("You are not allowed.");

        if (booking.Status != BookingStatus.Confirmed)
            throw new InvalidOperationException(
                "Attendance is allowed only after payment approval.");

        var exists = await _db.Attendances
            .AnyAsync(a => a.BookingId == dto.BookingId);

        if (exists)
            throw new InvalidOperationException(
                "Attendance already marked.");

        var attendance = new Attendance
        {
            BookingId = dto.BookingId,
            CheckInTime = DateTime.UtcNow,
            LocationText = dto.LocationText,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };

        _db.Attendances.Add(attendance);

        await _notifications.CreateInAppAsync(
            booking.ParentUserId,
            "Sitter Checked In",
            $"Your sitter has checked in for Booking #{booking.Id}.");

        await _db.SaveChangesAsync();
    }

    public async Task CheckOutAsync(int bookingId)
    {
        var attendance = await _db.Attendances
            .Include(a => a.Booking)
                .ThenInclude(b => b.BabySitterProfile)
            .FirstOrDefaultAsync(a => a.BookingId == bookingId)
            ?? throw new KeyNotFoundException("Attendance not found.");

        if (attendance.Booking.BabySitterProfile.UserId != _me.UserId)
            throw new UnauthorizedAccessException("You are not allowed.");

        if (attendance.CheckOutTime != null)
            throw new InvalidOperationException("Already checked out.");

        attendance.CheckOutTime = DateTime.UtcNow;

        attendance.Booking.Status = BookingStatus.Completed;

        await _notifications.CreateInAppAsync(
            attendance.Booking.ParentUserId,
            "Service Completed",
            $"Your sitter has completed Booking #{bookingId}.");

        await _db.SaveChangesAsync();
    }

    public async Task<AttendanceDetailsDto?> GetByBookingAsync(int bookingId)
    {
        return await _db.Attendances
            .AsNoTracking()
            .Where(a => a.BookingId == bookingId)
            .Select(a => new AttendanceDetailsDto
            {
                AttendanceId = a.Id,
                BookingId = a.BookingId,
                CheckInTime = a.CheckInTime,
                CheckOutTime = a.CheckOutTime,
                LocationText = a.LocationText,
                Latitude = a.Latitude,
                Longitude = a.Longitude
            })
            .FirstOrDefaultAsync();
    }


    public async Task<List<AttendanceDetailsDto>> GetMyAttendanceAsync()
    {
        return await _db.Attendances
            .AsNoTracking()
            .Include(a => a.Booking)
                .ThenInclude(b => b.ParentUser)
            .Include(a => a.Booking)
                .ThenInclude(b => b.BabySitterProfile)
            .Where(a => a.Booking.BabySitterProfile.UserId == _me.UserId)
            .OrderByDescending(a => a.CheckInTime)
            .Select(a => new AttendanceDetailsDto
            {
                AttendanceId = a.Id,
                BookingId = a.BookingId,
                CheckInTime = a.CheckInTime,
                CheckOutTime = a.CheckOutTime,
                LocationText = a.LocationText,
                Latitude = a.Latitude,
                Longitude = a.Longitude
            })
            .ToListAsync();
    }

    public async Task<List<AttendanceDetailsDto>> GetParentAttendanceAsync()
    {
        return await _db.Attendances
            .AsNoTracking()
            .Include(a => a.Booking)
                .ThenInclude(b => b.BabySitterProfile)
                    .ThenInclude(s => s.User)
            .Where(a => a.Booking.ParentUserId == _me.UserId)
            .OrderByDescending(a => a.CheckInTime)
            .Select(a => new AttendanceDetailsDto
            {
                AttendanceId = a.Id,
                BookingId = a.BookingId,
                CheckInTime = a.CheckInTime,
                CheckOutTime = a.CheckOutTime,
                LocationText = a.LocationText,
                Latitude = a.Latitude,
                Longitude = a.Longitude
            })
            .ToListAsync();
    }

}