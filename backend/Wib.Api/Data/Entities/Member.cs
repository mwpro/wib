namespace Wib.Api.Data.Entities;

public class Member
{
    public int Id { get; set; }
    public string ExternalSubjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int WalletBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
