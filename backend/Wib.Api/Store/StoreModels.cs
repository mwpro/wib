using System.ComponentModel.DataAnnotations;
using Wib.Api.Data.Entities;
using Wib.Api.Vouchers;

namespace Wib.Api.Store;

public record RewardItemResponse(
    int Id,
    string Title,
    string? Description,
    int PointCost,
    bool IsActive,
    int CreatedByMemberId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
)
{
    public static RewardItemResponse Create(RewardItem item)
    {
        return new RewardItemResponse(
            item.Id,
            item.Title,
            item.Description,
            item.PointCost,
            item.IsActive,
            item.CreatedByMemberId,
            item.CreatedAt,
            item.UpdatedAt
        );
    }
}

public record BuyRewardResponse(
    VoucherResponse Voucher,
    int MemberWalletBalance
);

public record CreateRewardItemRequest(
    string Title,
    string? Description = null,
    int PointCost = 1
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult("Title cannot be empty.", [nameof(Title)]);
        }
        else if (Title.Trim().Length > 255)
        {
            yield return new ValidationResult("Title cannot exceed 255 characters.", [nameof(Title)]);
        }

        if (!string.IsNullOrWhiteSpace(Description) && Description.Trim().Length > 2000)
        {
            yield return new ValidationResult("Description cannot exceed 2000 characters.", [nameof(Description)]);
        }

        if (PointCost < 1)
        {
            yield return new ValidationResult("PointCost must be at least 1.", [nameof(PointCost)]);
        }
    }
}

public record UpdateRewardItemRequest(
    string Title,
    string? Description = null,
    int PointCost = 1
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult("Title cannot be empty.", [nameof(Title)]);
        }
        else if (Title.Trim().Length > 255)
        {
            yield return new ValidationResult("Title cannot exceed 255 characters.", [nameof(Title)]);
        }

        if (!string.IsNullOrWhiteSpace(Description) && Description.Trim().Length > 2000)
        {
            yield return new ValidationResult("Description cannot exceed 2000 characters.", [nameof(Description)]);
        }

        if (PointCost < 1)
        {
            yield return new ValidationResult("PointCost must be at least 1.", [nameof(PointCost)]);
        }
    }
}
