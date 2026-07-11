namespace SmartBabySitter.Models;

public enum AppRole
{
    Parent = 1,
    BabySitter = 2,
    Admin = 3
}

public enum BookingStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Confirmed = 4,
    Completed = 5,
    Cancelled = 6
}

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4
}

public enum NotificationType
{
    Email = 1,
    Sms = 2,
    InApp = 3
}