using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class IncidentsControllerTests 
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IncidentsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetIncident_Returns200_AndETag()
    {
        var response = await _client.GetAsync("/api/incidents/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
    }

    [Fact]
    public async Task GetIncident_Returns304_WhenETagMatches()
    {
        // First request
        var firstResponse = await _client.GetAsync("/api/incidents/1");
        firstResponse.EnsureSuccessStatusCode();

        var etag = firstResponse.Headers.ETag?.ToString();
        Assert.NotNull(etag);

        // Conditional request
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/incidents/1");

        request.Headers.TryAddWithoutValidation("If-None-Match", etag);

        var secondResponse = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotModified, secondResponse.StatusCode);
    }
}