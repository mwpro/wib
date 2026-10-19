namespace Wib.Domain.Common;

public enum ErrorKind
{
    Invalid,
    NotFound
}

public sealed record DomainError(string Code, ErrorKind Kind)
{
    public static DomainError Invalid(string code) => new(code, ErrorKind.Invalid);

    public static DomainError NotFound(string code) => new(code, ErrorKind.NotFound);
}
