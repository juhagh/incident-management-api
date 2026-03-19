namespace Application;

public enum CommandResult
{
    Success,
    NotFound,
    ConcurrencyConflict,
    InvalidStateTransition
}