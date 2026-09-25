namespace Wib.Api.Data.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<ChoreTag> ChoreTags { get; set; } = new List<ChoreTag>();
}
