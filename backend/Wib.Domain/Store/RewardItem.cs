using Wib.Domain.Common;
using Wib.Domain.Members;
using Wib.Domain.Vouchers;

namespace Wib.Domain.Store;

public class RewardItem
{
    private RewardItem() { }

    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int PointCost { get; private set; }
    public int? Quantity { get; private set; }
    public bool IsActive { get; private set; }
    public int CreatedByMemberId { get; private set; }
    public Member CreatedByMember { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public const int TitleMaxLength = 255;
    public const int DescriptionMaxLength = 2000;

    public static Result<RewardItem> Create(
        string title,
        string? description,
        int pointCost,
        int? quantity,
        int createdByMemberId,
        DateTime nowUtc)
    {
        var validation = Validate(title, description, pointCost, quantity);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        return new RewardItem
        {
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            PointCost = pointCost,
            Quantity = quantity,
            IsActive = true,
            CreatedByMemberId = createdByMemberId,
            CreatedAt = nowUtc
        };
    }

    public Result Update(string title, string? description, int pointCost, int? quantity, DateTime nowUtc)
    {
        var validation = Validate(title, description, pointCost, quantity);
        if (validation.IsFailure)
        {
            return validation;
        }

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        PointCost = pointCost;
        Quantity = quantity;
        UpdatedAt = nowUtc;
        return Result.Success;
    }

    private static Result Validate(string title, string? description, int pointCost, int? quantity)
    {
        if (string.IsNullOrWhiteSpace(title))
            return RewardItemErrors.TitleRequired;
        if (title.Trim().Length > TitleMaxLength)
            return RewardItemErrors.TitleTooLong;
        if (description is not null && description.Trim().Length > DescriptionMaxLength)
            return RewardItemErrors.DescriptionTooLong;
        if (pointCost < 1)
            return RewardItemErrors.PointCostTooLow;
        if (quantity < 1)
            return RewardItemErrors.QuantityTooLow;

        return Result.Success;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAt = nowUtc;
    }

    public Result<Voucher> Purchase(Member member, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (!IsActive)
        {
            return RewardItemErrors.Unavailable;
        }

        var debit = member.DebitWallet(PointCost);
        if (debit.IsFailure)
        {
            return debit.Error;
        }

        var voucher = Voucher.Create(this, member, nowUtc);

        if (Quantity.HasValue)
        {
            Quantity -= 1;
            if (Quantity == 0)
            {
                IsActive = false;
            }
            UpdatedAt = nowUtc;
        }

        member.AddVoucher(voucher);
        return voucher;
    }
}
