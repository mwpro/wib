namespace Wib.Api.Data.Entities;

public class RewardItem
{
    private RewardItem() { }

    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int PointCost { get; private set; }
    public bool IsActive { get; private set; }
    public int CreatedByMemberId { get; private set; }
    public Member CreatedByMember { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static RewardItem Create(
        string title,
        string? description,
        int pointCost,
        int createdByMemberId,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty or whitespace.", nameof(title));

        if (pointCost < 1)
            throw new ArgumentOutOfRangeException(nameof(pointCost), "PointCost must be at least 1.");

        return new RewardItem
        {
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            PointCost = pointCost,
            IsActive = true,
            CreatedByMemberId = createdByMemberId,
            CreatedAt = nowUtc
        };
    }

    public void Update(string title, string? description, int pointCost, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty or whitespace.", nameof(title));

        if (pointCost < 1)
            throw new ArgumentOutOfRangeException(nameof(pointCost), "PointCost must be at least 1.");

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        PointCost = pointCost;
        UpdatedAt = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAt = nowUtc;
    }
}
