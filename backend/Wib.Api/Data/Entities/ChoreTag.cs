namespace Wib.Api.Data.Entities;

public class ChoreTag
{
    public int ChoreId { get; set; }
    public Chore Chore { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
