namespace Wib.Api.Data.Entities;

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

    public static RewardItem Create(
        string title,
        string? description,
        int pointCost,
        int? quantity,
        int createdByMemberId,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentOutOfRangeException.ThrowIfLessThan(pointCost, 1);

        if (quantity.HasValue && quantity.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least 1 if specified.");
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

    public void Update(string title, string? description, int pointCost, int? quantity, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentOutOfRangeException.ThrowIfLessThan(pointCost, 1);

        if (quantity.HasValue && quantity.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least 1 if specified.");
        }

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        PointCost = pointCost;
        Quantity = quantity;
        UpdatedAt = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAt = nowUtc;
    }

    public Voucher Purchase(Member member, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (!IsActive || (Quantity.HasValue && Quantity.Value <= 0))
        {
            throw new InvalidOperationException("Wybrana nagroda nie jest już aktywna.");
        }

        if (!member.TryDebitWallet(PointCost))
        {
            throw new InvalidOperationException("Niewystarczająca liczba punktów w portfelu.");
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
