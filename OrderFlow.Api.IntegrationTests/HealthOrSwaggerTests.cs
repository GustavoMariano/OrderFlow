using FluentAssertions;
using System.Net;

namespace OrderFlow.Api.IntegrationTests;

public sealed class HealthOrSwaggerTests : IClassFixture<OrderFlowApiFactory>
{
    private readonly HttpClient _client;

    public HealthOrSwaggerTests(OrderFlowApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_should_be_available()
    {
        var res = await _client.GetAsync("/swagger/index.html");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
