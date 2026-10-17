namespace Wib.UnitTests.TestData;

/// <summary>
/// Entry point for test data builders: <c>A.Chore().WithCadenceDays(7).Build()</c>.
/// </summary>
public static class A
{
    public static ChoreBuilder Chore() => new();
    public static MemberBuilder Member() => new();
}
