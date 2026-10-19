using Wib.Domain.Common;

namespace Wib.Api.Common;

public static class DomainErrorHttpExtensions
{
    public static IResult ToProblem(this DomainError error)
    {
        var statusCode = error.Kind switch
        {
            ErrorKind.Invalid => StatusCodes.Status400BadRequest,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Kind, null)
        };

        return Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
