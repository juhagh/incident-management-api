using Domain.Entities;
using Domain.Enums;

namespace Domain.Tests;

public class IncidentTests
{
    [Fact]
    public void StartProgress_ShouldThrow_WhenStatusIsOpen()
    {
        // Arrange
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        
        // Act + Assert
        var originalUpdatedAt = incident.UpdatedAt;
        Assert.Throws<InvalidOperationException>(() => incident.StartProgress());
        Assert.Equal(originalUpdatedAt, incident.UpdatedAt);
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenStatusIsNotInProgress()
    {
        // Arrange
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.AssignEngineer(5);
        
        // Act + Assert
        var originalUpdatedAt = incident.UpdatedAt;
        Assert.Throws<InvalidOperationException>(() => incident.Resolve("done"));
        Assert.Equal(originalUpdatedAt, incident.UpdatedAt);
    }

    [Fact]
    public void Resolve_ShouldSetStatusToResolved_WhenInProgress()
    {
        // Arrange
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.AssignEngineer(5);
        incident.StartProgress();
        
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act
        incident.Resolve("fixed router config");
        
        // Assert
        Assert.Equal(IncidentStatus.Resolved, incident.Status);
        Assert.Equal("fixed router config", incident.ResolutionSummary);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
        Assert.Null(incident.ClosedAt);
        Assert.Equal(5, incident.EngineerId);
    }

    [Fact]
    public void Close_ShouldSetClosedAt_WhenResolved()
    {
        // Arrange
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.AssignEngineer(5);
        incident.StartProgress();
        incident.Resolve("fixed router config");
        
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act
        incident.Close();
        
        // Assert
        Assert.Equal(IncidentStatus.Closed, incident.Status);
        Assert.Equal("fixed router config", incident.ResolutionSummary);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
        Assert.NotNull(incident.ClosedAt);
        Assert.Equal(5, incident.EngineerId);
    }

    [Fact]
    public void Close_ShouldSetClosedAt_WhenInvalid()
    {
        // Arrange
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.MarkInvalid("Duplicate");
        
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act
        incident.Close();
        
        // Assert
        Assert.Equal(IncidentStatus.Closed, incident.Status);
        Assert.Equal("Duplicate", incident.InvalidReason);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
        Assert.NotNull(incident.ClosedAt);
        Assert.Null(incident.EngineerId);
        Assert.Null(incident.ResolutionSummary);
    }
    
    [Fact]
    public void Close_ShouldThrow_WhenInProgress()
    {
        // Arrange
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.AssignEngineer(5);
        incident.StartProgress();
        
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => incident.Close());
        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        Assert.Equal(originalUpdatedAt, incident.UpdatedAt);
        Assert.Null(incident.ClosedAt);
        Assert.Equal(5, incident.EngineerId);
    }

    [Fact]
    public void ChangeSeverity_ShouldThrow_WhenResolved()
    {
        // Arrange
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.AssignEngineer(5);
        incident.StartProgress();
        incident.Resolve("fixed router config");
        
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => 
            incident.ChangeSeverity(IncidentSeverity.Minor));
        Assert.Equal(IncidentSeverity.Critical, incident.Severity);
        Assert.Equal(originalUpdatedAt, incident.UpdatedAt);
    }

    [Fact]
    public void MarkWaiting_ShouldSetStatusToWaiting_WhenInProgress()
    {
        // Arrange
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.AssignEngineer(5);
        incident.StartProgress();
        
        var originalUpdatedAt = incident.UpdatedAt;
        var originalResolutionSummary = incident.ResolutionSummary;
        var originalEngineerId = incident.EngineerId;
        
        // Act
        incident.MarkWaiting("Waiting for more information");
        
        // Assert
        Assert.Equal(IncidentStatus.Waiting, incident.Status);
        Assert.Equal("Waiting for more information", incident.WaitingReason);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
        Assert.Equal(originalResolutionSummary, incident.ResolutionSummary);
        Assert.Equal(originalEngineerId, incident.EngineerId);
    }

    [Fact]
    public void MarkWaiting_ShouldUpdateReason_WhenAlreadyWaiting()
    {
        // Arrange
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.AssignEngineer(5);
        incident.StartProgress();
        
        var originalResolutionSummary = incident.ResolutionSummary;
        var originalEngineerId = incident.EngineerId;
        
        incident.MarkWaiting("Waiting for more information");
        
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act
        incident.MarkWaiting("Updated waiting reason");
        
        // Assert
        Assert.Equal(IncidentStatus.Waiting, incident.Status);
        Assert.Equal("Updated waiting reason", incident.WaitingReason);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
        Assert.Equal(originalResolutionSummary, incident.ResolutionSummary);
        Assert.Equal(originalEngineerId, incident.EngineerId);
    }

    [Fact]
    public void MarkWaiting_ShouldThrow_WhenStatusIsNotInProgressOrWaiting()
    {
        // Arrange
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => 
            incident.MarkWaiting("Updated waiting reason"));
        Assert.Equal(IncidentStatus.Open, incident.Status);
        Assert.Null(incident.WaitingReason);
        Assert.Equal(originalUpdatedAt, incident.UpdatedAt);
    }

    [Fact]
    public void MarkInvalid_ShouldSetStatusToInvalid_WhenOpen()
    {
        // Arrange
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act
        incident.MarkInvalid("Incident is invalid");
        
        // Assert
        Assert.Equal(IncidentStatus.Invalid, incident.Status);
        Assert.Equal("Incident is invalid", incident.InvalidReason);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
    }
    
    [Fact]
    public void MarkInvalid_ShouldSetStatusToInvalid_WhenAssigned()
    {
        // Arrange
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.AssignEngineer(1);
        
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act
        incident.MarkInvalid("Incident is invalid");
        
        // Assert
        Assert.Equal(IncidentStatus.Invalid, incident.Status);
        Assert.Equal("Incident is invalid", incident.InvalidReason);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
        Assert.Equal(1, incident.EngineerId);
        Assert.Null(incident.ClosedAt);
    }

    [Fact]
    public void MarkInvalid_ShouldThrow_WhenInProgress()
    {
        // Arrange
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.AssignEngineer(1);
        incident.StartProgress();
        
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act + Assert
        Assert.Throws<InvalidOperationException>(() =>
            incident.MarkInvalid("Incident is invalid"));
        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        Assert.Null(incident.InvalidReason);
        Assert.Equal(originalUpdatedAt, incident.UpdatedAt);
    }
}
