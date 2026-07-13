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

    PaymentPending = 5,

    Completed = 6,
    Cancelled = 7
}

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4,
            Rejected = 5
}

public enum NotificationType
{
    Email = 1,
    Sms = 2,
    InApp = 3
}