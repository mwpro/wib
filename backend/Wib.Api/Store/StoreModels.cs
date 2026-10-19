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
);

public record UpdateRewardItemRequest(
    string Title,
    int PointCost,
    string? Description = null,
    int? Quantity = null
);
