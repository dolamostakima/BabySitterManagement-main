using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartBabySitter.Models;
using SmartBabySitter.Models.QueryModels;

namespace SmartBabySitter.Data;

public class ApplicationDbContext
  : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<OrganizationRequest> OrganizationRequests => Set<OrganizationRequest>();
    public DbSet<BabySitterProfile> BabySitterProfiles => Set<BabySitterProfile>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<BabySitterSkill> BabySitterSkills => Set<BabySitterSkill>();

    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingStatusHistory> BookingStatusHistories => Set<BookingStatusHistory>();

    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Address> Addresses => Set<Address>();


    public DbSet<SitterCardRow> SitterCardRows => Set<SitterCardRow>();

    public DbSet<SitterProfile> SitterProfiles { get; set; }
    public DbSet<SitterSkill> SitterSkills { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SitterCardRow>().HasNoKey().ToView(null);

        // ---------- BabySitterProfile 1-to-1 User ----------
        modelBuilder.Entity<BabySitterProfile>()
            .HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<BabySitterProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BabySitterProfile>()
            .Property(x => x.HourlyRate)
            .HasPrecision(10, 2);

        // ---------- Skill many-to-many ----------
        modelBuilder.Entity<BabySitterSkill>()
            .HasKey(x => new { x.BabySitterProfileId, x.SkillId });

        modelBuilder.Entity<BabySitterSkill>()
            .HasOne(x => x.BabySitterProfile)
            .WithMany(x => x.BabySitterSkills)
            .HasForeignKey(x => x.BabySitterProfileId);

        modelBuilder.Entity<BabySitterSkill>()
            .HasOne(x => x.Skill)
            .WithMany(x => x.BabySitterSkills)
            .HasForeignKey(x => x.SkillId);

        modelBuilder.Entity<Skill>()
            .HasIndex(x => x.Name)
            .IsUnique();

        // ---------- Availability ----------
        modelBuilder.Entity<Availability>()
            .HasOne(x => x.BabySitterProfile)
            .WithMany(x => x.Availabilities)
            .HasForeignKey(x => x.BabySitterProfileId);

        // ---------- Booking ----------
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.ParentUser)
            .WithMany(u => u.BookingsAsParent)
            .HasForeignKey(b => b.ParentUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.BabySitterProfile)
            .WithMany(s => s.Bookings)
            .HasForeignKey(b => b.BabySitterProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.BabySitterProfileId, b.BookingDate });

        // ---------- BookingStatusHistory ----------
        modelBuilder.Entity<BookingStatusHistory>()
            .HasOne(h => h.Booking)
            .WithMany(b => b.StatusHistory)
            .HasForeignKey(h => h.BookingId);

        modelBuilder.Entity<BookingStatusHistory>()
            .HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---------- Review (1-to-1 Booking) ----------
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Booking)
            .WithOne(b => b.Review)
            .HasForeignKey<Review>(r => r.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.ParentUser)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.ParentUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.BabySitterProfile)
            .WithMany(s => s.Reviews)
            .HasForeignKey(r => r.BabySitterProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---------- Payment (1-to-1 Booking) ----------
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(10, 2);

        // ---------- Attendance (1-to-1 Booking) ----------
        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Booking)
            .WithOne(b => b.Attendance)
            .HasForeignKey<Attendance>(a => a.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---------- Notification ----------
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.ReceiverUser)
            .WithMany()
            .HasForeignKey(n => n.ReceiverUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---------- Favorite ----------
        modelBuilder.Entity<Favorite>()
            .HasIndex(f => new { f.ParentUserId, f.BabySitterProfileId })
            .IsUnique();

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.ParentUser)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.ParentUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.BabySitterProfile)
            .WithMany()
            .HasForeignKey(f => f.BabySitterProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---------- Address ----------
        modelBuilder.Entity<Address>()
            .HasOne(a => a.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}