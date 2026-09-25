namespace Wib.Api.Data.Entities;

public class Chore
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Points { get; set; } = 1;
    public int? CadenceDays { get; set; }
    public DateTime? LastCompletedAt { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ChoreTag> ChoreTags { get; set; } = new List<ChoreTag>();
}
