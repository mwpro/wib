using Wib.Domain.Common;

namespace Wib.Domain.Store;

public static class RewardItemErrors
{
    public static readonly DomainError NotFound = DomainError.NotFound("RewardItem.NotFound");
    public static readonly DomainError Unavailable = DomainError.Invalid("RewardItem.Unavailable");
    public static readonly DomainError TitleRequired = DomainError.Invalid("RewardItem.TitleRequired");
    public static readonly DomainError TitleTooLong = DomainError.Invalid("RewardItem.TitleTooLong");
    public static readonly DomainError DescriptionTooLong = DomainError.Invalid("RewardItem.DescriptionTooLong");
    public static readonly DomainError PointCostTooLow = DomainError.Invalid("RewardItem.PointCostTooLow");
    public static readonly DomainError QuantityTooLow = DomainError.Invalid("RewardItem.QuantityTooLow");
}
