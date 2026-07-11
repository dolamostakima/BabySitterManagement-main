namespace SmartBabySitter.Services.DTOs;

public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);

public record SitterCardDto(
    int BabySitterProfileId,
    int UserId,
    string FullName,
    string Email,
    string Phone,
    decimal HourlyRate,
    int ExperienceYears,
    string LocationText,
    bool IsApproved,
    double AvgRating,
    int ReviewCount
)
{
    // ✅ Optional alias (front-end friendly)
    public double AverageRating => AvgRating;
    public int TotalReviews => ReviewCount;
}

public class SitterSearchQueryDto
{
    public bool OnlyApproved { get; set; } = true;

    public string? LocationText { get; set; }
    public decimal? MinRate { get; set; }
    public decimal? MaxRate { get; set; }
    public int? MinExperienceYears { get; set; }

    public List<string>? Skills { get; set; }

    public DateTime? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SitterProfileUpsertDto
{
    public string SkillsText { get; set; } = "";
    public int ExperienceYears { get; set; }
    public decimal HourlyRate { get; set; }
    public string LocationText { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<string> Skills { get; set; } = new();
}

public record MySitterProfileDto(
    int Id,
    string SkillsText,
    int ExperienceYears,
    decimal HourlyRate,
    string LocationText,
    double? Latitude,
    double? Longitude,
    List<string> Skills,
    List<AvailabilityDto> Availabilities
);

