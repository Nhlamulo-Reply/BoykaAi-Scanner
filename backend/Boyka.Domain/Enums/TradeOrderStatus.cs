namespace Boyka.Domain.Enums;

public enum TradeOrderStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Executed = 4,
    Cancelled = 5,
    Queued = 6,
    SentToMT5 = 7
}
