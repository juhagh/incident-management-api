using System.Net;
using System.Net.Http.Headers;
using System.Text;
using API.Tests;


public class IncidentsControllerTests 
    : IClassFixture<CustomWebApplicationFactory>, IClassFixture<AuthenticatedWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;

    public IncidentsControllerTests(CustomWebApplicationFactory factory, AuthenticatedWebApplicationFactory authFactory)
    {
        _client = factory.CreateClient();
        _authenticatedClient = authFactory.CreateClient();
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

    [Fact]
    public async Task GetIncident_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync("/api/incidents/999");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateIncident_Returns401_WhenNoToken()
    {
        var body = new StringContent(
            """
            {
                "title": "Test Incident",
                "description": "Test Description",
                "severity": "Minor",
                "networkElementId": 1
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/incidents", body);
    
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateIncident_Returns201_WhenAuthenticated()
    {
        // Arrange
        var body = new StringContent(
            """
            {
                "title": "Test Incident",
                "description": "Test Description",
                "severity": "Minor",
                "networkElementId": 1
            }
            """,
            Encoding.UTF8,
            "application/json");
        
        _authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("TestScheme");

        // Act
        var response = await _authenticatedClient.PostAsync("/api/incidents", body);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateIncident_Returns400_WhenInvalidBody()
    {
        // Arrange
        var body = new StringContent(
            """
            {}
            """,
            Encoding.UTF8,
            "application/json");
        
        _authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("TestScheme");

        // Act
        var response = await _authenticatedClient.PostAsync("/api/incidents", body);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}