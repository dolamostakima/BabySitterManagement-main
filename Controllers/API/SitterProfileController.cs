using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Data;
using SmartBabySitter.Services.DTOs;
using System.Security.Claims;
using SmartBabySitter.Models;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SitterProfileController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SitterProfileController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ================= GET PROFILE =================
    [HttpGet("me/profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out int id))
            return BadRequest("Invalid user id");

        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        var babyProfile = await _context.BabySitterProfiles
            .FirstOrDefaultAsync(x => x.UserId == id);

        var profile = await _context.SitterProfiles
            .Include(x => x.Skills)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        return Ok(new
        {
            fullName = user.FullName,
            email = user.Email,
            mobileNo = user.PhoneNumber,

            nid = profile?.Nid,
            gender = profile?.Gender,
            dateOfBirth = profile?.DateOfBirth,
            address = profile?.Address,

            photoUrl = babyProfile?.PhotoUrl,

            skillsText = babyProfile?.SkillsText,
            experienceYears = babyProfile?.ExperienceYears,
            hourlyRate = babyProfile?.HourlyRate,
            locationText = babyProfile?.LocationText,

            skills = profile?.Skills?.Select(s => s.Name).ToList()
        });
    }

    // ================= CREATE / UPDATE =================
    [HttpPost]
    public async Task<IActionResult> SaveProfile([FromForm] SitterProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);




        if (!int.TryParse(userId, out int id))
            return BadRequest("Invalid user id");

        var user = await _context.Users.FindAsync(id);




        if (user == null) return Unauthorized();

        // ===== Update USER TABLE =====
        if (!string.IsNullOrWhiteSpace(dto.FullName))
            user.FullName = dto.FullName;

        if (!string.IsNullOrWhiteSpace(dto.Email))
            user.Email = dto.Email;

        if (!string.IsNullOrWhiteSpace(dto.MobileNo))
            user.PhoneNumber = dto.MobileNo;

        // ===== Get or Create Profile =====
        var profile = await _context.SitterProfiles
            .Include(x => x.Skills)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile == null)
        {
            profile = new SitterProfile
            {
                UserId = userId
            };
            _context.SitterProfiles.Add(profile);
        }


        // ===== Get or Create BabySitterProfile =====

        var babyProfile = await _context.BabySitterProfiles
            .FirstOrDefaultAsync(x => x.UserId == id);

        if (babyProfile == null)
        {
            babyProfile = new BabySitterProfile
            {
                UserId = id
            };

            _context.BabySitterProfiles.Add(babyProfile);
        }



        if (dto.Image != null && dto.Image.Length > 0)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Image.FileName);

            var folder = Path.Combine(
     Directory.GetCurrentDirectory(),
     "wwwroot",
     "images",
     "sitters"
 );


            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Image.CopyToAsync(stream);
            }

            var imageUrl = $"{Request.Scheme}://{Request.Host}/images/sitters/{fileName}";

            profile.PhotoUrl = imageUrl;
            babyProfile.PhotoUrl = imageUrl;


        }




        // ===== Update PROFILE =====
        if (!string.IsNullOrWhiteSpace(dto.Nid))
            profile.Nid = dto.Nid;

        if (!string.IsNullOrWhiteSpace(dto.Gender))
            profile.Gender = dto.Gender;

        if (dto.DateOfBirth.HasValue)
            profile.DateOfBirth = dto.DateOfBirth.Value;

        if (!string.IsNullOrWhiteSpace(dto.Address))
            profile.Address = dto.Address;
        if (!string.IsNullOrWhiteSpace(dto.SkillsText))
        {
            profile.SkillsText = dto.SkillsText;
            babyProfile.SkillsText = dto.SkillsText;
        }

        if (dto.ExperienceYears.HasValue)
        {
            profile.ExperienceYears = dto.ExperienceYears.Value;
            babyProfile.ExperienceYears = dto.ExperienceYears.Value;
        }

        if (dto.HourlyRate.HasValue)
        {
            profile.HourlyRate = dto.HourlyRate.Value;
            babyProfile.HourlyRate = dto.HourlyRate.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.LocationText))
        {
            profile.LocationText = dto.LocationText;
            babyProfile.LocationText = dto.LocationText;
        }

        // ===== Skills Update =====
        if (dto.Skills != null)
        {
            if (profile.Skills != null)
                _context.SitterSkills.RemoveRange(profile.Skills);

            profile.Skills = dto.Skills
                .Select(s => new SitterSkill { Name = s })
                .ToList();
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Saved Successfully" });
    }

    // ================= DELETE =================
    [HttpDelete]
    public async Task<IActionResult> DeleteProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var profile = await _context.SitterProfiles
            .Include(x => x.Skills)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (profile == null) return NotFound();

        if (profile.Skills != null)
            _context.SitterSkills.RemoveRange(profile.Skills);

        _context.SitterProfiles.Remove(profile);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Profile Deleted" });
    }
}