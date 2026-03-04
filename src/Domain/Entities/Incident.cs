using Domain.Enums;

namespace Domain.Entities;

public class Incident
{
    private Incident(string title, string description, IncidentSeverity severity,
        int networkElementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        
        if (!Enum.IsDefined(severity))
            throw new ArgumentException($"Invalid severity value: {severity}", nameof(severity));
        
        if (networkElementId < 1)
            throw new ArgumentOutOfRangeException(nameof(networkElementId),
                "Network element ID must be greater than 0.");

        var timeStamp = DateTime.UtcNow;

        Title = title;
        Description = description;
        Severity = severity;
        NetworkElementId = networkElementId;
        Status = IncidentStatus.Open;
        CreatedAt = timeStamp;
        UpdatedAt = timeStamp;
    }

    private Incident(){}
    
    public int Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public IncidentSeverity Severity { get; private set; }
    public IncidentStatus Status { get; private set; }
    public int NetworkElementId { get; private set; }
    public int? EngineerId { get; private set; }
    public uint RowVersion { get; private set; }
    public string? WaitingReason { get; private set; }
    public string? ResolutionSummary { get; private set; }
    public string? InvalidReason { get; private set; }

    public static Incident Create(string title, string description, IncidentSeverity severity,
        int networkElementId)
    {
        return new Incident(title, description, severity, networkElementId);
    }

    public void AssignEngineer(int engineerId)
    {
        if (engineerId < 1)
            throw new ArgumentOutOfRangeException(nameof(engineerId),
                "Engineer ID must be greater than 0.");

        if (Status is IncidentStatus.Closed or IncidentStatus.Invalid or IncidentStatus.Resolved)
            throw new InvalidOperationException(
                $"Cannot (re)assign engineer when incident status is {Status}.");

        bool changed = false;
        
        if (engineerId != EngineerId)
        {
            EngineerId = engineerId;
            changed = true;
        }
        
        if (Status == IncidentStatus.Open)
        {
            Status = IncidentStatus.Assigned;
            changed = true;
        }

        if (changed)
            Touch();
    }

    public void ChangeSeverity(IncidentSeverity severity)
    {
        if (Status is IncidentStatus.Closed or IncidentStatus.Invalid or IncidentStatus.Resolved)
            throw new InvalidOperationException(
                $"Cannot change severity when incident status is {Status}.");
        
        if (!Enum.IsDefined(severity))
            throw new ArgumentException($"Invalid severity value: {severity}", nameof(severity));

        bool changed = false;
        
        if (severity != Severity)
        {
            Severity = severity;
            changed = true;
        }
            
        if(changed)
            Touch();
    }

    public void StartProgress()
    {
        if (Status is IncidentStatus.Closed or IncidentStatus.Invalid or IncidentStatus.Resolved or IncidentStatus.Open)
            throw new InvalidOperationException(
                $"Cannot change status when incident status is {Status}.");
        
        if (EngineerId is null)
            throw new InvalidOperationException(
                "Cannot start progress when no engineer is assigned.");

        if (Status is IncidentStatus.Assigned or IncidentStatus.Waiting)
        {
            Status = IncidentStatus.InProgress;
            Touch();
        }
    }

    public void MarkWaiting(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        
        if (Status is not IncidentStatus.InProgress and not IncidentStatus.Waiting)
            throw new InvalidOperationException(
                $"Operation not allowed when incident status is {Status}. " +
                "Allowed states: InProgress, Waiting.");
        
        if (EngineerId is null)
            throw new InvalidOperationException(
                "Operation not allowed when no engineer is assigned.");

        bool changed = false;
        
        if (Status is not IncidentStatus.Waiting)
        {
            Status = IncidentStatus.Waiting;
            changed = true;
        }
        
        if (WaitingReason != reason)
        {
            WaitingReason = reason.Trim();
            changed = true;
        }
        
        if(changed)
            Touch();
    }

    public void Resolve(string resolutionSummary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolutionSummary);
        
        if (Status is not IncidentStatus.InProgress)
            throw new InvalidOperationException(
                $"Operation not allowed when incident status is {Status}. " +
                "Allowed state: InProgress.");

        if (EngineerId is null)
            throw new InvalidOperationException(
                "Operation not allowed when no engineer is assigned.");

        ResolutionSummary = resolutionSummary.Trim();;
        Status = IncidentStatus.Resolved;
        
        Touch();
    }

    public void Close()
    {
        if (Status is not (IncidentStatus.Resolved or IncidentStatus.Invalid))
            throw new InvalidOperationException(
                $"Operation not allowed when incident status is {Status}. " +
                "Allowed states: Resolved, Invalid.");
        
        Status = IncidentStatus.Closed;
        var now = DateTime.UtcNow;
        UpdatedAt = now;
        ClosedAt = now;
    }

    public void MarkInvalid(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        
        if (Status is not (IncidentStatus.Open or IncidentStatus.Assigned))
            throw new InvalidOperationException(
                $"Operation not allowed when incident status is {Status}. " +
                "Allowed states: Open, Assigned.");

        InvalidReason = reason.Trim();
        Status = IncidentStatus.Invalid;
        
        Touch();
    }
    
    private void Touch() => UpdatedAt = DateTime.UtcNow;
}