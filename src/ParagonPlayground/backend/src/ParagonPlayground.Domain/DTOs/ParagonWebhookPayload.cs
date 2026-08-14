namespace ParagonPlayground.Domain.DTOs;

/// <summary>Webhook payload sent by Paragon for managed sync lifecycle events.</summary>
public class ParagonWebhookPayload
{

  /// <summary>Webhook event type (for example: sync_complete, record_updated, record_deleted).</summary>
  public string Event { get; set; } = string.Empty;

  /// <summary>Paragon managed sync instance identifier associated with this event.</summary>
  public string SyncInstanceId { get; set; } = string.Empty;

  /// <summary>Sync descriptor from the webhook payload, when provided.</summary>
  public string Sync { get; set; } = string.Empty;

  /// <summary>Credential identifier used by the integration, when included by Paragon.</summary>
  public string CredentialId { get; set; } = string.Empty;

  /// <summary>User context attached to the webhook payload.</summary>
  public ParagonWebhookUser? User { get; set; }

  /// <summary>Event-specific data payload.</summary>
  public ParagonWebhookData? Data { get; set; }

  /// <summary>Error payload when the webhook event represents a failure state.</summary>
  public ParagonWebhookError? Error { get; set; }
}

/// <summary>User metadata included in a Paragon webhook.</summary>
public class ParagonWebhookUser
{
  /// <summary>Paragon user identifier for the connected user.</summary>
  public string Id { get; set; } = string.Empty;
}

/// <summary>Event data section of a Paragon webhook payload.</summary>
public class ParagonWebhookData
{
  /// <summary>Logical model type for the emitted record event.</summary>
  public string Model { get; set; } = string.Empty;

  /// <summary>Record identifier relevant to create/update/delete record events.</summary>
  public string? RecordId { get; set; }

  /// <summary>Timestamp indicating when the sync data was last synchronized.</summary>
  public string? SyncedAt { get; set; }

  /// <summary>Number of records referenced by the event when available.</summary>
  public int? NumRecords { get; set; }
}

/// <summary>Error details included with failed sync webhook events.</summary>
public class ParagonWebhookError
{
  /// <summary>Provider-specific error code.</summary>
  public string? Code { get; set; }

  /// <summary>Human-readable error description.</summary>
  public string? Message { get; set; }
}