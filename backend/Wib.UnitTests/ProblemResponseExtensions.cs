using System.Net;
using System.Text.Json;
using FluentAssertions;
using Wib.Domain.Common;

namespace Wib.UnitTests;

public static class ProblemResponseExtensions
{
    public static async Task ShouldBeProblem(this HttpResponseMessage response, DomainError error)
    {
        var expectedStatus = error.Kind == ErrorKind.NotFound ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest;
        response.StatusCode.Should().Be(expectedStatus);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("code").GetString().Should().Be(error.Code);
    }
}
