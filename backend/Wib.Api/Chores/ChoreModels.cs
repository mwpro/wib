using System.ComponentModel.DataAnnotations;
using Wib.Domain.Chores;

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
    public static ChoreResponse Create(Chore chore, DateTime nowUtc)
    {
        var freshness = chore.GetFreshness(nowUtc);
        var tagNames = chore.Tags
            .Select(t => t.Name)
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

public record CompleteChoreResponse(
    ChoreResponse Chore,
    int MemberWalletBalance,
    int PointsAwarded
);

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
        else if (Title.Trim().Length > 255)
        {
            yield return new ValidationResult("Title cannot exceed 255 characters.", [nameof(Title)]);
        }

        if (!string.IsNullOrWhiteSpace(Description) && Description.Trim().Length > 2000)
        {
            yield return new ValidationResult("Description cannot exceed 2000 characters.", [nameof(Description)]);
        }

        if (Points < 1)
        {
            yield return new ValidationResult("Points must be at least 1.", [nameof(Points)]);
        }

        if (CadenceDays.HasValue && CadenceDays.Value < 1)
        {
            yield return new ValidationResult("CadenceDays must be at least 1 day if specified.", [nameof(CadenceDays)]);
        }

        if (Tags?.Any(t => !string.IsNullOrWhiteSpace(t) && t.Trim().Length > 50) == true)
        {
            yield return new ValidationResult("Each tag name must be 50 characters or fewer.", [nameof(Tags)]);
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
        else if (Title.Trim().Length > 255)
        {
            yield return new ValidationResult("Title cannot exceed 255 characters.", [nameof(Title)]);
        }

        if (!string.IsNullOrWhiteSpace(Description) && Description.Trim().Length > 2000)
        {
            yield return new ValidationResult("Description cannot exceed 2000 characters.", [nameof(Description)]);
        }

        if (Points < 1)
        {
            yield return new ValidationResult("Points must be at least 1.", [nameof(Points)]);
        }

        if (CadenceDays.HasValue && CadenceDays.Value < 1)
        {
            yield return new ValidationResult("CadenceDays must be at least 1 day if specified.", [nameof(CadenceDays)]);
        }

        if (Tags?.Any(t => !string.IsNullOrWhiteSpace(t) && t.Trim().Length > 50) == true)
        {
            yield return new ValidationResult("Each tag name must be 50 characters or fewer.", [nameof(Tags)]);
        }
    }
}
