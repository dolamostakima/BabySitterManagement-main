using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;
using SmartBabySitter.Services.DTOs;

namespace SmartBabySitter.Services;

public interface IAvailabilityService
{
    Task<int> AddMyAvailabilityAsync(AvailabilityCreateDto dto);
    Task RemoveMyAvailabilityAsync(int availabilityId);
    Task<List<Availability>> GetMyAvailabilitiesAsync();
}

public class AvailabilityService : IAvailabilityService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _me;

    public AvailabilityService(ApplicationDbContext db, ICurrentUser me)
    {
        _db = db;
        _me = me;
    }

    public async Task<int> AddMyAvailabilityAsync(AvailabilityCreateDto dto)
    {
        if (!_me.IsAuthenticated) throw new UnauthorizedAccessException();

        var sitter = await _db.BabySitterProfiles.FirstOrDefaultAsync(x => x.UserId == _me.UserId)
            ?? throw new InvalidOperationException("Create sitter profile first.");

        if (dto.EndTime <= dto.StartTime)
            throw new InvalidOperationException("EndTime must be greater than StartTime.");

        // Weekly OR Date Range

        if (dto.Day == null && dto.Date == null)
            throw new InvalidOperationException("Provide Day or Date.");

        if (dto.Day != null && dto.Date != null)
            throw new InvalidOperationException("Choose either Weekly Day OR Date Range.");

        if (dto.Date != null)
        {
            if (dto.EndDate == null)
                throw new InvalidOperationException("Please select End Date.");

            if (dto.EndDate < dto.Date)
                throw new InvalidOperationException("End Date cannot be before Start Date.");
        }


        var a = new Availability
        {
            BabySitterProfileId = sitter.Id,

            Day = dto.Day,

            Date = dto.Date?.Date,

            EndDate = dto.EndDate?.Date,

            StartTime = dto.StartTime,

            EndTime = dto.EndTime,

            IsAvailable = dto.IsAvailable
        };

        _db.Availabilities.Add(a);
        await _db.SaveChangesAsync();
        return a.Id;
    }

    public async Task RemoveMyAvailabilityAsync(int availabilityId)
    {
        if (!_me.IsAuthenticated) throw new UnauthorizedAccessException();

        var sitter = await _db.BabySitterProfiles.FirstOrDefaultAsync(x => x.UserId == _me.UserId)
            ?? throw new InvalidOperationException("Sitter profile not found.");

        var a = await _db.Availabilities.FirstOrDefaultAsync(x => x.Id == availabilityId && x.BabySitterProfileId == sitter.Id)
            ?? throw new KeyNotFoundException("Availability not found.");

        _db.Availabilities.Remove(a);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Availability>> GetMyAvailabilitiesAsync()
    {
        if (!_me.IsAuthenticated) throw new UnauthorizedAccessException();

        var sitter = await _db.BabySitterProfiles.FirstOrDefaultAsync(x => x.UserId == _me.UserId)
            ?? throw new InvalidOperationException("Sitter profile not found.");

        return await _db.Availabilities
            .Where(x => x.BabySitterProfileId == sitter.Id)
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.Day)
            .ThenBy(x => x.StartTime)
            .ToListAsync();
    }
}