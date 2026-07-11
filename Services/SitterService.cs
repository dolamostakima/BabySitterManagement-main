using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Models;
using SmartBabySitter.Services.DTOs;
using System.Data;

namespace SmartBabySitter.Services;

public interface ISitterService
{
    Task<int> UpsertMySitterProfileAsync(SitterProfileUpsertDto dto);
    Task<MySitterProfileDto?> GetMyProfileAsync();

    Task ApproveSitterAsync(int babySitterProfileId, bool approve);
    Task<SitterCardDto?> GetSitterAsync(int babySitterProfileId);
    Task<PagedResult<SitterCardDto>> SearchAsync(SitterSearchQueryDto q);
}

public class SitterService : ISitterService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _me;
    private readonly UserManager<ApplicationUser> _userManager;

    public SitterService(ApplicationDbContext db, ICurrentUser me, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _me = me;
        _userManager = userManager;
    }

    public async Task<int> UpsertMySitterProfileAsync(SitterProfileUpsertDto dto)
    {
        if (!_me.IsAuthenticated) throw new UnauthorizedAccessException();

        var user = await _db.Users.FirstAsync(u => u.Id == _me.UserId);
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains("BabySitter"))
            throw new InvalidOperationException("Only BabySitter can create sitter profile.");

        var profile = await _db.BabySitterProfiles
            .Include(x => x.BabySitterSkills)
            .FirstOrDefaultAsync(x => x.UserId == _me.UserId);

        if (profile == null)
        {
            profile = new BabySitterProfile
            {
                UserId = _me.UserId,
                BabySitterSkills = new List<BabySitterSkill>()
            };
            _db.BabySitterProfiles.Add(profile);
        }

        profile.SkillsText = dto.SkillsText ?? "";
        profile.ExperienceYears = dto.ExperienceYears;
        profile.HourlyRate = dto.HourlyRate;
        profile.LocationText = dto.LocationText ?? "";
        profile.Latitude = dto.Latitude;
        profile.Longitude = dto.Longitude;

        profile.BabySitterSkills ??= new List<BabySitterSkill>();

        var skillNames = (dto.Skills ?? new List<string>())
            .Select(s => (s ?? "").Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (skillNames.Count == 0)
        {
            profile.BabySitterSkills.Clear();
            await _db.SaveChangesAsync();
            return profile.Id;
        }

        var lower = skillNames.Select(x => x.ToLower()).ToList();
        var existing = await _db.Skills.Where(s => lower.Contains(s.Name.ToLower())).ToListAsync();

        foreach (var name in skillNames)
            if (!existing.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                _db.Skills.Add(new Skill { Name = name });

        await _db.SaveChangesAsync();

        var allSkills = await _db.Skills.Where(s => lower.Contains(s.Name.ToLower())).ToListAsync();

        profile.BabySitterSkills.Clear();
        foreach (var sk in allSkills)
        {
            profile.BabySitterSkills.Add(new BabySitterSkill
            {
                BabySitterProfileId = profile.Id,
                SkillId = sk.Id
            });
        }

        await _db.SaveChangesAsync();
        return profile.Id;
    }

    public async Task<MySitterProfileDto?> GetMyProfileAsync()
    {
        if (!_me.IsAuthenticated) throw new UnauthorizedAccessException();

        var profile = await _db.BabySitterProfiles
            .AsNoTracking()
            .Include(p => p.BabySitterSkills).ThenInclude(bs => bs.Skill)
            .Include(p => p.Availabilities)
            .FirstOrDefaultAsync(p => p.UserId == _me.UserId);

        if (profile == null) return null;

        var skills = profile.BabySitterSkills?
            .Where(x => x.Skill != null)
            .Select(x => x.Skill!.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList() ?? new List<string>();

        var avs = profile.Availabilities?
            .OrderBy(a => a.Date ?? DateTime.MinValue)
            .ThenBy(a => a.Day ?? DayOfWeek.Sunday)
            .ThenBy(a => a.StartTime)
            .Select(a => new AvailabilityDto(
    a.Id,
    a.Day,
    a.Date,
    a.EndDate,
    a.StartTime,
    a.EndTime,
    a.IsAvailable
))
            .ToList() ?? new List<AvailabilityDto>();

        return new MySitterProfileDto(
            profile.Id,
            profile.SkillsText ?? "",
            profile.ExperienceYears,
            profile.HourlyRate,
            profile.LocationText ?? "",
            profile.Latitude,
            profile.Longitude,
            skills,
            avs
        );
    }

    public async Task ApproveSitterAsync(int babySitterProfileId, bool approve)
    {
        var p = await _db.BabySitterProfiles.FirstOrDefaultAsync(x => x.Id == babySitterProfileId)
            ?? throw new KeyNotFoundException("Sitter profile not found.");

        p.IsApproved = approve;
        await _db.SaveChangesAsync();
    }

    // ✅ FIXED: rating already comes from SQL ReviewAgg (Approved + not hidden)
    public async Task<SitterCardDto?> GetSitterAsync(int babySitterProfileId)
    {
        var q = new SitterSearchQueryDto
        {
            OnlyApproved = false,
            Page = 1,
            PageSize = 1
        };

        var (sql, _, prms) = BuildSearchSql(q, sitterId: babySitterProfileId);
        var items = await ExecuteSitterCardsAsync(sql, prms);

        return items.FirstOrDefault();
    }

    public async Task<PagedResult<SitterCardDto>> SearchAsync(SitterSearchQueryDto q)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 100);

        q.Page = page;
        q.PageSize = size;

        var (sql, countSql, prms) = BuildSearchSql(q, sitterId: null);

        var total = await ExecuteScalarIntAsync(countSql, prms);
        var items = await ExecuteSitterCardsAsync(sql, prms);

        return new PagedResult<SitterCardDto>(items, total, page, size);
    }

    // ---------------- Helpers ----------------

    private static SqlParameter[] CloneParams(SqlParameter[] prms)
    {
        return prms.Select(p =>
        {
            var np = new SqlParameter(p.ParameterName, p.SqlDbType)
            {
                Value = p.Value ?? DBNull.Value,
                Direction = p.Direction,
                Size = p.Size,
                Precision = p.Precision,
                Scale = p.Scale,
                IsNullable = p.IsNullable
            };
            return np;
        }).ToArray();
    }

    private string GetConnectionStringOrThrow()
    {
        var cs = _db.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(cs))
            cs = _db.Database.GetDbConnection().ConnectionString;

        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Database connection string not found.");

        return cs;
    }

    private async Task<int> ExecuteScalarIntAsync(string sql, SqlParameter[] prms)
    {
        var cs = GetConnectionStringOrThrow();

        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(sql, conn);
        cmd.CommandType = CommandType.Text;
        cmd.Parameters.AddRange(CloneParams(prms));

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private async Task<List<SitterCardDto>> ExecuteSitterCardsAsync(string sql, SqlParameter[] prms)
    {
        var cs = GetConnectionStringOrThrow();

        var list = new List<SitterCardDto>();

        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(sql, conn);
        cmd.CommandType = CommandType.Text;
        cmd.Parameters.AddRange(CloneParams(prms));

        await using var reader = await cmd.ExecuteReaderAsync();

        int oId = reader.GetOrdinal("BabySitterProfileId");
        int oUserId = reader.GetOrdinal("UserId");
        int oFullName = reader.GetOrdinal("FullName");
        int oEmail = reader.GetOrdinal("Email");
        int oPhone = reader.GetOrdinal("Phone");
        int oHourly = reader.GetOrdinal("HourlyRate");
        int oExp = reader.GetOrdinal("ExperienceYears");
        int oLoc = reader.GetOrdinal("LocationText");
        int oApproved = reader.GetOrdinal("IsApproved");
        int oAvg = reader.GetOrdinal("AvgRating");
        int oCount = reader.GetOrdinal("ReviewCount");

        while (await reader.ReadAsync())
        {
            list.Add(new SitterCardDto(
                reader.GetInt32(oId),
                reader.GetInt32(oUserId),
                reader.GetString(oFullName),
                reader.GetString(oEmail),
                reader.GetString(oPhone),
                reader.GetDecimal(oHourly),
                reader.GetInt32(oExp),
                reader.GetString(oLoc),
                reader.GetBoolean(oApproved),
                Convert.ToDouble(reader.GetValue(oAvg)),
                reader.GetInt32(oCount)
            ));
        }

        return list;
    }

    private (string sql, string countSql, SqlParameter[] prms) BuildSearchSql(SitterSearchQueryDto q, int? sitterId)
    {
        var skills = (q.Skills ?? new List<string>())
            .Select(x => (x ?? "").Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var skillCsv = string.Join(",", skills);
        var hasAvail = q.Date.HasValue && q.StartTime.HasValue && q.EndTime.HasValue;

        var pOnlyApproved = new SqlParameter("@OnlyApproved", SqlDbType.Bit) { Value = q.OnlyApproved };
        var pLocation = new SqlParameter("@Location", SqlDbType.NVarChar, 200) { Value = (object?)q.LocationText ?? DBNull.Value };
        var pMinRate = new SqlParameter("@MinRate", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = (object?)q.MinRate ?? DBNull.Value };
        var pMaxRate = new SqlParameter("@MaxRate", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = (object?)q.MaxRate ?? DBNull.Value };
        var pMinExp = new SqlParameter("@MinExp", SqlDbType.Int) { Value = (object?)q.MinExperienceYears ?? DBNull.Value };

        var pSitterId = new SqlParameter("@SitterId", SqlDbType.Int) { Value = (object?)sitterId ?? DBNull.Value };

        var pPage = new SqlParameter("@Page", SqlDbType.Int) { Value = q.Page };
        var pPageSize = new SqlParameter("@PageSize", SqlDbType.Int) { Value = q.PageSize };

        var pHasSkills = new SqlParameter("@HasSkills", SqlDbType.Bit) { Value = skills.Count > 0 };
        var pSkillCount = new SqlParameter("@SkillCount", SqlDbType.Int) { Value = skills.Count };
        var pSkillCsv = new SqlParameter("@SkillCsv", SqlDbType.NVarChar, 4000) { Value = skills.Count > 0 ? skillCsv : DBNull.Value };

        var pHasAvail = new SqlParameter("@HasAvail", SqlDbType.Bit) { Value = hasAvail };
        var pDate = new SqlParameter("@Date", SqlDbType.Date) { Value = (object?)(q.Date?.Date) ?? DBNull.Value };
        var pStart = new SqlParameter("@StartTime", SqlDbType.Time) { Value = (object?)q.StartTime ?? DBNull.Value };
        var pEnd = new SqlParameter("@EndTime", SqlDbType.Time) { Value = (object?)q.EndTime ?? DBNull.Value };

        var prms = new[]
        {
            pOnlyApproved, pLocation, pMinRate, pMaxRate, pMinExp, pSitterId,
            pPage, pPageSize,
            pHasSkills, pSkillCount, pSkillCsv,
            pHasAvail, pDate, pStart, pEnd
        };

        // ✅ FIX: ReviewAgg now only approved + not hidden
        var baseCte = @"
;WITH ReviewAgg AS
(
    SELECT
        r.BabySitterProfileId,
        AVG(CAST(r.Rating AS float)) AS AvgRating,
        COUNT(*) AS ReviewCount
    FROM Reviews r
    WHERE r.IsApproved = 1 AND r.IsHidden = 0
    GROUP BY r.BabySitterProfileId
),
SkillIds AS
(
    SELECT s.Id
    FROM Skills s
    WHERE @HasSkills = 1
      AND LOWER(s.Name) IN (SELECT LOWER(value) FROM STRING_SPLIT(@SkillCsv, ','))
),
MatchedSitters AS
(
    SELECT bs.BabySitterProfileId
    FROM BabySitterSkills bs
    WHERE @HasSkills = 1 AND bs.SkillId IN (SELECT Id FROM SkillIds)
    GROUP BY bs.BabySitterProfileId
    HAVING COUNT(DISTINCT bs.SkillId) >= @SkillCount
),
AvailSitters AS
(
    SELECT DISTINCT a.BabySitterProfileId
    FROM Availabilities a
    WHERE @HasAvail = 1
      AND a.IsAvailable = 1
      AND (
            (a.[Date] IS NOT NULL AND CAST(a.[Date] AS date) = @Date)
         OR (a.[Date] IS NULL AND a.[Day] = DATEPART(WEEKDAY, @Date) - 1)
          )
      AND a.StartTime <= @StartTime
      AND a.EndTime >= @EndTime
),
Base AS
(
    SELECT
        s.Id AS BabySitterProfileId,
        s.UserId,
        u.FullName,
        ISNULL(u.Email,'') AS Email,
        ISNULL(u.PhoneNumber,'') AS Phone,
        s.HourlyRate,
        s.ExperienceYears,
        s.LocationText,
        s.IsApproved,
        ISNULL(ra.AvgRating, 0) AS AvgRating,
        ISNULL(ra.ReviewCount, 0) AS ReviewCount
    FROM BabySitterProfiles s
    INNER JOIN AspNetUsers u ON u.Id = s.UserId
    LEFT JOIN ReviewAgg ra ON ra.BabySitterProfileId = s.Id
    WHERE
        (@SitterId IS NULL OR s.Id = @SitterId)
        AND (@OnlyApproved = 0 OR s.IsApproved = 1)
        AND (@Location IS NULL OR s.LocationText LIKE '%' + @Location + '%')
        AND (@MinRate IS NULL OR s.HourlyRate >= @MinRate)
        AND (@MaxRate IS NULL OR s.HourlyRate <= @MaxRate)
        AND (@MinExp IS NULL OR s.ExperienceYears >= @MinExp)
        AND (@HasSkills = 0 OR s.Id IN (SELECT BabySitterProfileId FROM MatchedSitters))
        AND (@HasAvail = 0 OR s.Id IN (SELECT BabySitterProfileId FROM AvailSitters))
)
";

        var sql = baseCte + @"
SELECT *
FROM Base
ORDER BY
    AvgRating DESC,
    ReviewCount DESC,
    ExperienceYears DESC,
    HourlyRate ASC
OFFSET (@Page - 1) * @PageSize ROWS
FETCH NEXT @PageSize ROWS ONLY;";

        var countSql = baseCte + @"
SELECT COUNT(*) AS Total
FROM Base;";

        return (sql, countSql, prms);
    }
}