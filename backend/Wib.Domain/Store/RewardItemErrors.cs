using Wib.Domain.Common;

namespace Wib.Domain.Store;

public static class RewardItemErrors
{
    public static readonly DomainError NotFound = DomainError.NotFound("RewardItem.NotFound");
    public static readonly DomainError Unavailable = DomainError.Invalid("RewardItem.Unavailable");
}
