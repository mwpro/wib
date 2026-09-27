namespace Wib.Api.Data.Entities;

public class ChoreCompletion
{
    private ChoreCompletion() { }

    public int Id { get; private set; }
    public int ChoreId { get; private set; }
    public Chore Chore { get; private set; } = null!;
    public int CompletedByMemberId { get; private set; }
    public Member CompletedByMember { get; private set; } = null!;
    public DateTime CompletedAt { get; private set; }
    public int PointsAwarded { get; private set; }

    internal static ChoreCompletion Create(int choreId, int completedByMemberId, int pointsAwarded, DateTime completedAtUtc)
    {
        return new ChoreCompletion
        {
            ChoreId = choreId,
            CompletedByMemberId = completedByMemberId,
            PointsAwarded = pointsAwarded,
            CompletedAt = completedAtUtc
        };
    }
}
