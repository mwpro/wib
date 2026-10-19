using Wib.Domain.Common;

namespace Wib.Domain.Chores;

public static class ChoreErrors
{
    public static readonly DomainError NotFound = DomainError.NotFound("Chore.NotFound");
    public static readonly DomainError TitleRequired = DomainError.Invalid("Chore.TitleRequired");
    public static readonly DomainError TitleTooLong = DomainError.Invalid("Chore.TitleTooLong");
    public static readonly DomainError DescriptionTooLong = DomainError.Invalid("Chore.DescriptionTooLong");
    public static readonly DomainError PointsTooLow = DomainError.Invalid("Chore.PointsTooLow");
    public static readonly DomainError CadenceTooShort = DomainError.Invalid("Chore.CadenceTooShort");
    public static readonly DomainError TagTooLong = DomainError.Invalid("Chore.TagTooLong");
}
