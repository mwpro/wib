using Wib.Domain.Members;

namespace Wib.UnitTests.TestData;

/// <summary>
/// Builds a <see cref="Member"/> with sane defaults: a unique subject id and "Test Member".
/// </summary>
public class MemberBuilder
{
    private string _externalSubjectId = $"test-{Guid.NewGuid():N}";
    private string _name = "Test Member";

    public MemberBuilder WithExternalSubjectId(string externalSubjectId) { _externalSubjectId = externalSubjectId; return this; }
    public MemberBuilder WithName(string name) { _name = name; return this; }

    public Member Build() => Member.Create(_externalSubjectId, _name, DateTime.UtcNow);
}
