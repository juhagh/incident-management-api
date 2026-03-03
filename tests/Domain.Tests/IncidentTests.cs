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
}
