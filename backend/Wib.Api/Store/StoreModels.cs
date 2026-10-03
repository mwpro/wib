using System.ComponentModel.DataAnnotations;
using Wib.Api.Vouchers;
using Wib.Domain.Store;

namespace Wib.Api.Store;

public record RewardItemResponse(
    int Id,
    string Title,
    string? Description,
    int PointCost,
    int? Quantity,
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
            item.Quantity,
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
    int PointCost,
    string? Description = null,
    int? Quantity = null
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

        if (Quantity.HasValue && Quantity.Value < 1)
        {
            yield return new ValidationResult("Quantity must be at least 1 if specified.", [nameof(Quantity)]);
        }
    }
}

public record UpdateRewardItemRequest(
    string Title,
    int PointCost,
    string? Description = null,
    int? Quantity = null
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

        if (Quantity.HasValue && Quantity.Value < 1)
        {
            yield return new ValidationResult("Quantity must be at least 1 if specified.", [nameof(Quantity)]);
        }
    }
}
