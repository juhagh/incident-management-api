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
        var incident = CreateAssignedIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => incident.Resolve("done"));
        Assert.Equal(originalUpdatedAt, incident.UpdatedAt);
    }

    [Fact]
    public void Resolve_ShouldSetStatusToResolved_WhenInProgress()
    {
        // Arrange
        var incident = CreateInProgressIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act
        incident.Resolve("fixed router config");
        
        // Assert
        Assert.Equal(IncidentStatus.Resolved, incident.Status);
        Assert.Equal("fixed router config", incident.ResolutionSummary);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
        Assert.Null(incident.ClosedAt);
        Assert.Equal(1, incident.EngineerId);
    }

    [Fact]
    public void Close_ShouldSetClosedAt_WhenResolved()
    {
        // Arrange
        var incident = CreateResolvedIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act
        incident.Close();
        
        // Assert
        Assert.Equal(IncidentStatus.Closed, incident.Status);
        Assert.Equal("Resolved", incident.ResolutionSummary);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
        Assert.NotNull(incident.ClosedAt);
        Assert.Equal(1, incident.EngineerId);
    }

    [Fact]
    public void Close_ShouldSetClosedAt_WhenInvalid()
    {
        // Arrange
        var incident = CreateInvalidIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act
        incident.Close();
        
        // Assert
        Assert.Equal(IncidentStatus.Closed, incident.Status);
        Assert.Equal("Invalid", incident.InvalidReason);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
        Assert.NotNull(incident.ClosedAt);
        Assert.Null(incident.EngineerId);
        Assert.Null(incident.ResolutionSummary);
    }
    
    [Fact]
    public void Close_ShouldThrow_WhenInProgress()
    {
        // Arrange
        var incident = CreateInProgressIncident();
        
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => incident.Close());
        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        Assert.Equal(originalUpdatedAt, incident.UpdatedAt);
        Assert.Null(incident.ClosedAt);
        Assert.Equal(1, incident.EngineerId);
    }

    [Fact]
    public void ChangeSeverity_ShouldThrow_WhenResolved()
    {
        // Arrange
        var incident = CreateResolvedIncident();
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
        var incident = CreateInProgressIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act
        incident.MarkWaiting("Waiting for more information");
        
        // Assert
        Assert.Equal(IncidentStatus.Waiting, incident.Status);
        Assert.Equal("Waiting for more information", incident.WaitingReason);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void MarkWaiting_ShouldUpdateReason_WhenAlreadyWaiting()
    {
        // Arrange
        var incident = CreateWaitingIncident();
        var originalResolutionSummary = incident.ResolutionSummary;
        var originalEngineerId = incident.EngineerId;
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
        var incident = CreateAssignedIncident();
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
        var incident = CreateInProgressIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        // Act + Assert
        Assert.Throws<InvalidOperationException>(() =>
            incident.MarkInvalid("Incident is invalid"));
        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        Assert.Null(incident.InvalidReason);
        Assert.Equal(originalUpdatedAt, incident.UpdatedAt);
    }

    [Fact]
    public void AssignEngineer_FromOpen_SetsAssignedStatusAndEngineerId()
    {
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        var originalUpdatedAt = incident.UpdatedAt;
        
        incident.AssignEngineer(1);
        
        Assert.Equal(IncidentStatus.Assigned, incident.Status);
        Assert.Equal(1, incident.EngineerId);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
    }
    
    [Fact]
    public void AssignEngineer_WithZeroEngineerId_ThrowsArgumentOutOfRangeException()
    {
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        var originalUpdatedAt = incident.UpdatedAt;
        
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            incident.AssignEngineer(0));
        Assert.Equal(IncidentStatus.Open, incident.Status);
        Assert.Null(incident.EngineerId);
        Assert.Equal(incident.UpdatedAt, originalUpdatedAt);
    }
    
    [Fact]
    public void AssignEngineer_FromAssigned_UpdatesEngineerId()
    {
        var incident = CreateAssignedIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        incident.AssignEngineer(2);
        
        Assert.Equal(IncidentStatus.Assigned, incident.Status);
        Assert.Equal(2, incident.EngineerId);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
    }
    
    [Fact]
    public void AssignEngineer_FromInProgress_UpdatesEngineerId()
    {
        var incident = CreateInProgressIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        incident.AssignEngineer(2);
        
        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        Assert.Equal(2, incident.EngineerId);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
    }
    
    [Fact]
    public void AssignEngineer_FromWaiting_UpdatesEngineerId()
    {
        var incident = CreateWaitingIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        incident.AssignEngineer(2);
        
        Assert.Equal(IncidentStatus.Waiting, incident.Status);
        Assert.Equal(2, incident.EngineerId);
        Assert.Equal("Waiting", incident.WaitingReason);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
    }
    
    [Fact]
    public void AssignEngineer_FromResolved_Throws()
    {
        var incident = CreateResolvedIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        Assert.Throws<InvalidOperationException>(() =>
            incident.AssignEngineer(9));
        Assert.Equal(IncidentStatus.Resolved, incident.Status);
        Assert.Equal(1, incident.EngineerId);
        Assert.Equal(incident.UpdatedAt, originalUpdatedAt);
    }
    
    [Fact]
    public void AssignEngineer_FromInvalid_Throws()
    {
        var incident = CreateInvalidIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        Assert.Throws<InvalidOperationException>(() =>
            incident.AssignEngineer(1));
        Assert.Equal(IncidentStatus.Invalid, incident.Status);
        Assert.Null(incident.EngineerId);
        Assert.Equal(incident.UpdatedAt, originalUpdatedAt);
    }
    
    [Fact]
    public void AssignEngineer_FromClosed_Throws()
    {
        var incident = CreateClosedIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        Assert.Throws<InvalidOperationException>(() =>
            incident.AssignEngineer(1));
        Assert.Equal(IncidentStatus.Closed, incident.Status);
        Assert.Null(incident.EngineerId);
        Assert.Equal(incident.UpdatedAt, originalUpdatedAt);
    }

    [Fact]
    public void StartProgress_FromAssigned_SetsStatusToInProgress()
    {
        var incident = CreateAssignedIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        incident.StartProgress();
        
        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void StartProgress_FromWaiting_SetsStatusToInProgress()
    {
        var incident = CreateWaitingIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        incident.StartProgress();
        
        Assert.Equal(IncidentStatus.InProgress, incident.Status);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);
    }
    
    [Fact]
    public void StartProgress_FromResolved_Throws()
    {
        var incident = CreateResolvedIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        Assert.Throws<InvalidOperationException>(() =>
            incident.StartProgress());
        Assert.Equal(IncidentStatus.Resolved, incident.Status);
        Assert.Equal(incident.UpdatedAt, originalUpdatedAt);
    }
    
    [Fact]
    public void StartProgress_FromInvalid_Throws()
    {
        var incident = CreateInvalidIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        Assert.Throws<InvalidOperationException>(() =>
            incident.StartProgress());
        Assert.Equal(IncidentStatus.Invalid, incident.Status);
        Assert.Equal(incident.UpdatedAt, originalUpdatedAt);
    }
    
    [Fact]
    public void StartProgress_FromClosed_Throws()
    {
        var incident = CreateClosedIncident();
        var originalUpdatedAt = incident.UpdatedAt;
        
        Assert.Throws<InvalidOperationException>(() =>
            incident.StartProgress());
        Assert.Equal(IncidentStatus.Closed, incident.Status);
        Assert.Equal(incident.UpdatedAt, originalUpdatedAt);
    }
    
    [Fact]
    public void Create_WithEmptyTitle_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Incident.Create("", "Test Description", IncidentSeverity.Critical, 1));
    }
    
    [Fact]
    public void Create_WithEmptyDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Incident.Create("Title", "", IncidentSeverity.Critical, 1));
    }
    
    [Fact]
    public void Create_WithZeroNetworkElementId_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Incident.Create("Title", "Description", IncidentSeverity.Critical, 0));
    }
    
    [Fact]
    public void Resolve_WithBlankSummary_Throws()
    {
        var incident = CreateInProgressIncident();
        Assert.Throws<ArgumentException>(() =>
            incident.Resolve(""));
    }
    
    [Fact]
    public void MarkWaiting_WithBlankReason_Throws()
    {
        var incident = CreateInProgressIncident();
        Assert.Throws<ArgumentException>(() =>
            incident.MarkWaiting(""));
    }
    
    [Fact]
    public void MarkInvalid_WithBlankReason_Throws()
    {
        var incident = CreateAssignedIncident();
        Assert.Throws<ArgumentException>(() =>
            incident.MarkInvalid(""));
    }
    
    [Fact]
    public void Close_FromOpen_Throws()
    {
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        Assert.Throws<InvalidOperationException>(() =>
            incident.Close());
    }
    
    [Fact]
    public void Close_FromAssigned_Throws()
    {
        var incident = CreateAssignedIncident();
        Assert.Throws<InvalidOperationException>(() =>
            incident.Close());
    }
    
    [Fact]
    public void Close_FromWaiting_Throws()
    {
        var incident = CreateWaitingIncident();
        Assert.Throws<InvalidOperationException>(() =>
            incident.Close());
    }
    
    [Fact]
    public void Close_FromClosed_Throws()
    {
        var incident = CreateClosedIncident();
        Assert.Throws<InvalidOperationException>(() =>
            incident.Close());
    }
    
    [Fact]
    public void MarkInvalid_FromWaiting_Throws()
    {
        var incident = CreateWaitingIncident();
        Assert.Throws<InvalidOperationException>(() =>
            incident.MarkInvalid("Invalid"));
    }
    
    [Fact]
    public void MarkInvalid_FromResolved_Throws()
    {
        var incident = CreateResolvedIncident();
        Assert.Throws<InvalidOperationException>(() =>
            incident.MarkInvalid("Invalid"));
    }
    
    [Fact]
    public void MarkInvalid_FromClosed_Throws()
    {
        var incident = CreateClosedIncident();
        Assert.Throws<InvalidOperationException>(() =>
            incident.MarkInvalid("Invalid"));
    }
    
    [Fact]
    public void ChangeSeverity_FromOpen_UpdatesSeverity()
    {
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        var originalSeverity = incident.Severity;
        var originalUpdatedAt = incident.UpdatedAt;
        
        incident.ChangeSeverity(IncidentSeverity.Minor);
        
        Assert.Equal(IncidentSeverity.Minor, incident.Severity);
        Assert.True(incident.UpdatedAt >= originalUpdatedAt);

    }
    
    private Incident CreateAssignedIncident()
    {
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.AssignEngineer(1);

        return incident;
    }
    
    private Incident CreateInProgressIncident()
    {
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.AssignEngineer(1);
        incident.StartProgress();

        return incident;
    }
    
    private Incident CreateWaitingIncident()
    {
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.AssignEngineer(1);
        incident.StartProgress();
        incident.MarkWaiting("Waiting");

        return incident;
    }
    
    private Incident CreateResolvedIncident()
    {
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.AssignEngineer(1);
        incident.StartProgress();
        incident.Resolve("Resolved");

        return incident;
    }
    
    private Incident CreateInvalidIncident()
    {
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.MarkInvalid("Invalid");

        return incident;
    }
    
    private Incident CreateClosedIncident()
    {
        var incident = Incident.Create("Test Incident", "Test Description", IncidentSeverity.Critical, 1);
        incident.MarkInvalid("Invalid");
        incident.Close();

        return incident;
    }
}
