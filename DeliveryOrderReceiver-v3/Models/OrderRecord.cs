using System;
using System.Text.Json.Serialization;

namespace DeliveryOrderReceiver.Models;

public enum UploadStatus
{
    Pending,
    Success,
    Failed,
    Duplicate
}

public class OrderRecord
{
    public int Seq { get; set; }

    /// <summary>UTC ISO-8601 (W-TIME fix: DateTime.UtcNow.ToString("o"))</summary>
    public string ReceivedAt { get; set; } = DateTime.UtcNow.ToString("o");

    public string Port { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UploadStatus UploadStatus { get; set; } = UploadStatus.Pending;

    public string IdempotencyKey { get; set; } = string.Empty;

    public string? LastError { get; set; }

    public DateTime ReceivedAtUtc =>
        DateTime.TryParse(ReceivedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt.ToUniversalTime()
            : DateTime.UtcNow;
}
