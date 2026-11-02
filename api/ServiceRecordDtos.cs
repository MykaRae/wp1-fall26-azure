using System.ComponentModel.DataAnnotations;

/// <summary>Fields required to register a service record.</summary>
public sealed record CreateServiceRecordRequest
{
    [Required, StringLength(200)]
    public required string ServiceName { get; init; }

    [Required, StringLength(100)]
    public required string Environment { get; init; }

    [Required, StringLength(100)]
    public required string Status { get; init; }
}

/// <summary>A service record stored by the API.</summary>
public sealed record ServiceRecordResponse(
    long Id,
    string ServiceName,
    string Environment,
    string Status,
    DateTimeOffset CreatedAt);
