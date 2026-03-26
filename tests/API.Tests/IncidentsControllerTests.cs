using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using API.Http.Etags;
using API.Tests.Models;
using Domain.Enums;
using Xunit.Abstractions;

namespace API.Tests;

public class IncidentsControllerTests 
    : IClassFixture<CustomWebApplicationFactory>, IClassFixture<AuthenticatedWebApplicationFactory>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;

    public IncidentsControllerTests(CustomWebApplicationFactory factory, AuthenticatedWebApplicationFactory authFactory, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _client = factory.CreateClient();
        _authenticatedClient = authFactory.CreateClient();
        _authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("TestScheme");
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

        var etag = firstResponse.Headers.ETag?.Tag;
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
        using var body = new StringContent(
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
        using var body = new StringContent(
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

        // Act
        var response = await _authenticatedClient.PostAsync("/api/incidents", body);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateIncident_Returns400_WhenInvalidBody()
    {
        // Arrange
        using var body = new StringContent(
            """
            {}
            """,
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _authenticatedClient.PostAsync("/api/incidents", body);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task AssignEngineer_WithoutIfMatch_Returns428PreconditionRequired()
    {
        
        var (id, _) = await CreateIncidentAndGetETagAsync();
        using var assignBody = new StringContent(
            """
            {
              "engineerId": 1
            }
            """,
            Encoding.UTF8,
            "application/json");

        var assignResponse = await _authenticatedClient.PostAsync($"/api/incidents/{id}/assign-engineer", assignBody);
        
        Assert.Equal(HttpStatusCode.PreconditionRequired, assignResponse.StatusCode);
    }
    
    [Fact]
    public async Task AssignEngineer_WithMalformedIfMatch_Returns400BadRequest()
    {
        var (id, _) = await CreateIncidentAndGetETagAsync();
        
        using var assignBody = new StringContent(
            """
            {
              "engineerId": 1
            }
            """,
            Encoding.UTF8,
            "application/json");

        var malformedEtag = "MalformedEtag";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/incidents/{id}/assign-engineer")
        {
            Content = assignBody
        };
        request.Headers.TryAddWithoutValidation("If-Match", malformedEtag);

        var assignResponse = await _authenticatedClient.SendAsync(request);
        
        Assert.Equal(HttpStatusCode.BadRequest, assignResponse.StatusCode);
    }
    
    
    [Fact]
    public async Task AssignEngineer_WithStaleIfMatch_Returns412PreconditionFailed()
    {
        var (id, _) = await CreateIncidentAndGetETagAsync();
        using var assignBody = new StringContent(
            """
            {
              "engineerId": 1
            }
            """,
            Encoding.UTF8,
            "application/json");
        
        // Valid format but non-matching value — any uint that won't match a fresh row version
        var staleEtag = "W/\"99999\"";
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/incidents/{id}/assign-engineer")
        {
            Content = assignBody
        };
        request.Headers.TryAddWithoutValidation("If-Match", staleEtag);

        var assignResponse = await _authenticatedClient.SendAsync(request);
        
        Assert.Equal(HttpStatusCode.PreconditionFailed, assignResponse.StatusCode);
        
    }
    
    [Fact]
    public async Task AssignEngineer_WithValidIfMatch_Returns200OkAndUpdatedIncident()
    {
        var (id, etag) = await CreateIncidentAndGetETagAsync();
        
        using var assignBody = new StringContent(
            """
            {
              "engineerId": 1
            }
            """,
            Encoding.UTF8,
            "application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/incidents/{id}/assign-engineer")
        {
            Content = assignBody
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        
        var assignResponse = await _authenticatedClient.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        
        var incident = await assignResponse.Content.ReadFromJsonAsync<IncidentResponse>();
        Assert.NotNull(incident);
        Assert.Equal("Assigned", incident.Status);
        Assert.Equal(1, incident.EngineerId);
    }
    
    [Fact]
    public async Task Close_FromOpen_Returns409Conflict()
    {
        var (id, etag) = await CreateIncidentAndGetETagAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/incidents/{id}/close");
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        
        var closeResponse = await _authenticatedClient.SendAsync(request);
        Assert.Equal(HttpStatusCode.Conflict, closeResponse.StatusCode);
    }
    
    [Fact]
    public async Task AssignEngineer_WithoutAuthentication_Returns401Unauthorized()
    {
        var (id, etag) = await CreateIncidentAndGetETagAsync();
        
        using var assignBody = new StringContent(
            """
            {
              "engineerId": 1
            }
            """,
            Encoding.UTF8,
            "application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/incidents/{id}/assign-engineer")
        {
            Content = assignBody
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        
        var assignResponse = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, assignResponse.StatusCode);
    }
    
    [Fact]
    public async Task StartProgress_WithValidIfMatch_Returns200OkAndUpdatedIncident()
    {
        var (id, etag) = await CreateIncidentAndGetETagAsync();
        
        using var assignBody = new StringContent(
            """
            {
              "engineerId": 1
            }
            """,
            Encoding.UTF8,
            "application/json");
        
        var assignRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/incidents/{id}/assign-engineer")
        {
            Content = assignBody
        };
        assignRequest.Headers.TryAddWithoutValidation("If-Match", etag);
        
        var assignResponse = await _authenticatedClient.SendAsync(assignRequest);
        assignResponse.EnsureSuccessStatusCode();
        
        var incident = await assignResponse.Content.ReadFromJsonAsync<IncidentResponse>();
        Assert.NotNull(incident);
        Assert.Equal("Assigned", incident.Status);

        etag = assignResponse.Headers.ETag?.Tag;
        Assert.NotNull(etag);
        
        var progressRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/incidents/{id}/start-progress");
        progressRequest.Headers.TryAddWithoutValidation("If-Match", etag);
        
        var progressResponse = await _authenticatedClient.SendAsync(progressRequest);
        incident = await progressResponse.Content.ReadFromJsonAsync<IncidentResponse>();
        Assert.Equal(HttpStatusCode.OK, progressResponse.StatusCode);
        Assert.NotNull(incident);
        Assert.NotNull(progressResponse.Headers.ETag);
        Assert.Equal("InProgress", incident.Status);
    }
    
    [Fact]
    public async Task Resolve_WithValidIfMatch_Returns200OkAndUpdatedIncident()
    {
        var (id, etag) = await CreateIncidentAndGetETagAsync();
        
        using var assignBody = new StringContent(
            """
            {
              "engineerId": 1
            }
            """,
            Encoding.UTF8,
            "application/json");
        
        var assignRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/incidents/{id}/assign-engineer")
        {
            Content = assignBody
        };
        assignRequest.Headers.TryAddWithoutValidation("If-Match", etag);
        
        var assignResponse = await _authenticatedClient.SendAsync(assignRequest);
        assignResponse.EnsureSuccessStatusCode();
        
        var incident = await assignResponse.Content.ReadFromJsonAsync<IncidentResponse>();
        Assert.NotNull(incident);
        Assert.Equal("Assigned", incident.Status);
    
        etag = assignResponse.Headers.ETag?.Tag;
        Assert.NotNull(etag);
        
        var progressRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/incidents/{id}/start-progress");
        progressRequest.Headers.TryAddWithoutValidation("If-Match", etag);
        
        var progressResponse = await _authenticatedClient.SendAsync(progressRequest);
        progressResponse.EnsureSuccessStatusCode();
        
        incident = await progressResponse.Content.ReadFromJsonAsync<IncidentResponse>();
        Assert.NotNull(incident);
        Assert.Equal("InProgress", incident.Status);
        
        using var resolveBody = new StringContent(
            """
            {
              "resolutionSummary": "Resolution"
            }
            """,
            Encoding.UTF8,
            "application/json");
        
        etag = progressResponse.Headers.ETag?.Tag;
        Assert.NotNull(etag);
        
        var resolveRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/incidents/{id}/resolve")
        {
            Content = resolveBody
        };
        resolveRequest.Headers.TryAddWithoutValidation("If-Match", etag);
        
        var resolveResponse = await _authenticatedClient.SendAsync(resolveRequest);
        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);
        
        incident = await resolveResponse.Content.ReadFromJsonAsync<IncidentResponse>();
        Assert.NotNull(incident);
        Assert.NotNull(resolveResponse.Headers.ETag);
        Assert.Equal("Resolved", incident.Status);
        Assert.Equal("Resolution", incident.ResolutionSummary);
    }
    
    private async Task<(int Id, string ETag)> CreateIncidentAndGetETagAsync()
    {
        using var body = new StringContent(
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
        
        var createResponse = await _authenticatedClient.PostAsync("/api/incidents", body);
        createResponse.EnsureSuccessStatusCode();
        
        var incident = await createResponse.Content.ReadFromJsonAsync<IncidentResponse>();
        Assert.NotNull(incident);
        Assert.True(incident.Id > 0, "Expected a valid incident ID from created incident");
        
        var getResponse = await _authenticatedClient.GetAsync($"/api/incidents/{incident.Id}");
        getResponse.EnsureSuccessStatusCode();

        var etag = getResponse.Headers.ETag?.Tag;
        Assert.NotNull(etag);

        // uint value = ETagHelper.TryParseIfMatch(etag) ?? 0;
        
        return (incident.Id, etag);
    }
}