using FluentAssertions;

namespace OrderFlow.Application.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void Should_run_tests()
    {
        true.Should().BeTrue();
    }
}
