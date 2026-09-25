using System.ComponentModel.DataAnnotations;
using Wib.Api.Data.Entities;

namespace Wib.Api.Chores;

public record ChoreResponse(
    int Id,
    string Title,
    string? Description,
    int Points,
    int? CadenceDays,
    DateTime? LastCompletedAt,
    IReadOnlyList<string> Tags,
    bool IsArchived,
    string Urgency,
    double? UrgencyRatio,
    int? DaysSinceLastDone,
    DateTime CreatedAt,
    DateTime? UpdatedAt
)
{
    public static ChoreResponse Create(Chore chore, FreshnessResult freshness)
    {
        var tagNames = chore.ChoreTags
            .Select(ct => ct.Tag.Name)
            .OrderBy(t => t)
            .ToList();

        return new ChoreResponse(
            chore.Id,
            chore.Title,
            chore.Description,
            chore.Points,
            chore.CadenceDays,
            chore.LastCompletedAt,
            tagNames,
            chore.IsArchived,
            freshness.Urgency.ToString(),
            freshness.UrgencyRatio,
            freshness.DaysSinceLastDone,
            chore.CreatedAt,
            chore.UpdatedAt
        );
    }
}

public record CreateChoreRequest(
    string Title,
    string? Description = null,
    int Points = 1,
    int? CadenceDays = null,
    IReadOnlyList<string>? Tags = null
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult("Title cannot be empty.", [nameof(Title)]);
        }

        if (Points < 1)
        {
            yield return new ValidationResult("Points must be at least 1.", [nameof(Points)]);
        }

        if (CadenceDays.HasValue && CadenceDays.Value < 1)
        {
            yield return new ValidationResult("CadenceDays must be at least 1 day if specified.", [nameof(CadenceDays)]);
        }
    }
}

public record UpdateChoreRequest(
    string Title,
    string? Description = null,
    int Points = 1,
    int? CadenceDays = null,
    IReadOnlyList<string>? Tags = null
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult("Title cannot be empty.", [nameof(Title)]);
        }

        if (Points < 1)
        {
            yield return new ValidationResult("Points must be at least 1.", [nameof(Points)]);
        }

        if (CadenceDays.HasValue && CadenceDays.Value < 1)
        {
            yield return new ValidationResult("CadenceDays must be at least 1 day if specified.", [nameof(CadenceDays)]);
        }
    }
}
