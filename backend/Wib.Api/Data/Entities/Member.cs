namespace Wib.Api.Data.Entities;

public class Member
{
    private Member() { }

    public int Id { get; private set; }
    public string ExternalSubjectId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int WalletBalance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static Member Create(string externalSubjectId, string name, DateTime nowUtc)
    {
        return new Member
        {
            ExternalSubjectId = externalSubjectId,
            Name = name,
            WalletBalance = 0,
            CreatedAt = nowUtc
        };
    }

    /// <summary>
    /// Updates the member's display name if it has changed.
    /// </summary>
    public void UpdateProfile(string name, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name) || Name == name)
            return;

        Name = name;
        UpdatedAt = nowUtc;
    }

    public void CreditWallet(int amount)
    {
        WalletBalance += amount;
    }
}
