namespace AffordStack.Domain.Enums;

public enum EnquiryStatus
{
    New = 1,
    Reviewed = 2,
    Quoted = 3,
    FollowUp = 4,
    Converted = 5,
    Closed = 6,
    Spam = 7
}

public enum QuotationStatus
{
    Draft = 1,
    Sent = 2,
    Viewed = 3,
    Accepted = 4,
    Rejected = 5,
    Expired = 6,
    Cancelled = 7
}

public enum ProjectStatus
{
    NotStarted = 1,
    InProgress = 2,
    OnHold = 3,
    Completed = 4,
    Cancelled = 5
}

public enum PaymentType
{
    Advance = 1,
    Milestone = 2,
    Final = 3,
    Additional = 4
}

public enum PaymentStatus
{
    Pending = 1,
    Received = 2,
    Refunded = 3
}

public enum EmailOutboxStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4
}

public enum AdminRole
{
    SuperAdmin = 1,
    Admin = 2,
    Manager = 3
}
