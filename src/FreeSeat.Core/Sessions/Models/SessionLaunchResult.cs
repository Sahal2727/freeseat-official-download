namespace FreeSeat.Core.Sessions.Models;

/// <summary>
/// Represents the result of a session process launch operation.
/// </summary>
/// <param name="Success">Whether the launch succeeded.</param>
/// <param name="ProcessId">The process ID of the launched process (if successful).</param>
/// <param name="SessionId">The target session ID.</param>
/// <param name="ErrorCode">Windows error code (if failed).</param>
/// <param name="ErrorMessage">Human-readable error message (if failed).</param>
/// <param name="Timestamp">When the launch was attempted.</param>
public record SessionLaunchResult(
    bool Success,
    int? ProcessId,
    int SessionId,
    int? ErrorCode = null,
    string? ErrorMessage = null,
    DateTime? Timestamp = null
)
{
    /// <summary>
    /// Creates a successful launch result.
    /// </summary>
    public static SessionLaunchResult SuccessResult(int processId, int sessionId)
    {
        return new SessionLaunchResult(
            Success: true,
            ProcessId: processId,
            SessionId: sessionId,
            Timestamp: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Creates a failed launch result.
    /// </summary>
    public static SessionLaunchResult FailureResult(int sessionId, int errorCode, string errorMessage)
    {
        return new SessionLaunchResult(
            Success: false,
            ProcessId: null,
            SessionId: sessionId,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage,
            Timestamp: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Creates a failed launch result from an exception.
    /// </summary>
    public static SessionLaunchResult FromException(int sessionId, Exception ex)
    {
        var errorCode = ex.HResult;
        var errorMessage = ex.Message;

        // Try to get Win32 error code if available
        if (ex is System.ComponentModel.Win32Exception win32Ex)
        {
            errorCode = win32Ex.NativeErrorCode;
        }

        return new SessionLaunchResult(
            Success: false,
            ProcessId: null,
            SessionId: sessionId,
            ErrorCode: errorCode,
            ErrorMessage: $"Launch failed: {errorMessage}",
            Timestamp: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Gets a human-readable summary of the result.
    /// </summary>
    public string Summary => Success
        ? $"Launched process {ProcessId} in session {SessionId}"
        : $"Launch failed for session {SessionId}: {ErrorMessage} (Error: {ErrorCode})";
}