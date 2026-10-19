using Wib.Domain.Common;

namespace Wib.Domain.Chores;

public static class ChoreErrors
{
    public static readonly DomainError NotFound = DomainError.NotFound("Chore.NotFound");
}
