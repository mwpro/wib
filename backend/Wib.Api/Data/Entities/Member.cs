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

    private readonly List<Voucher> _vouchers = [];
    public IReadOnlyCollection<Voucher> Vouchers => _vouchers.AsReadOnly();

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
        ArgumentOutOfRangeException.ThrowIfLessThan(amount, 1);
        WalletBalance += amount;
    }

    public bool TryDebitWallet(int amount)
    {
        if (amount <= 0 || WalletBalance < amount)
            return false;

        WalletBalance -= amount;
        return true;
    }

    public void AddVoucher(Voucher voucher)
    {
        ArgumentNullException.ThrowIfNull(voucher);
        _vouchers.Add(voucher);
    }
}
