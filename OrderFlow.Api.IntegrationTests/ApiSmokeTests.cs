using FluentAssertions;

namespace OrderFlow.Api.IntegrationTests;

public sealed class ApiSmokeTests
{
    [Fact]
    public void Should_run_tests()
    {
        true.Should().BeTrue();
    }
}
