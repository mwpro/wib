namespace Wib.Api.Data.Entities;

public class Member
{
    public int Id { get; set; }
    public string Auth0UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Picture { get; set; }
    public int WalletBalance { get; set; }
    public int EarnedPoints { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
