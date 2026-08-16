namespace CinePick.Domain.ExternalProviders;

public sealed class ExternalSyncLog
{
    private ExternalSyncLog() { }

    public ExternalSyncLog(Guid id, string providerId, string operation, DateTimeOffset startedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        Id = id;
        ProviderId = providerId;
        Operation = operation;
        StartedAt = startedAt;
        Status = "running";
    }

    public Guid Id { get; private set; }
    public string ProviderId { get; private set; } = string.Empty;
    public string Operation { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int ReceivedCount { get; private set; }
    public int InsertedCount { get; private set; }
    public int UpdatedCount { get; private set; }
    public string? ErrorCode { get; private set; }

    public void Complete(DateTimeOffset completedAt, int receivedCount, int insertedCount, int updatedCount)
    {
        CompletedAt = completedAt;
        ReceivedCount = receivedCount;
        InsertedCount = insertedCount;
        UpdatedCount = updatedCount;
        Status = "succeeded";
    }

    public void Fail(DateTimeOffset completedAt, string errorCode)
    {
        CompletedAt = completedAt;
        ErrorCode = errorCode;
        Status = "failed";
    }
}
