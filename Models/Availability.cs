namespace SmartBabySitter.Models;

public class Availability
{
    public int Id { get; set; }

    public int BabySitterProfileId { get; set; }
    public BabySitterProfile BabySitterProfile { get; set; } = default!;

    // Weekly schedule
    public DayOfWeek? Day { get; set; } // null হলে date specific

    // Date-specific availability
    public DateTime? Date { get; set; } // null হলে weekly

    public DateTime? EndDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public bool IsAvailable { get; set; } = true;
}