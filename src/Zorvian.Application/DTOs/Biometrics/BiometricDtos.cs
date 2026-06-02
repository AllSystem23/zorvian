using System.Text.Json.Serialization;

namespace Zorvian.Application.DTOs.Biometrics;

public sealed record RegisterBiometricRequest(
    [property: JsonPropertyName("deviceId")] string DeviceId,
    [property: JsonPropertyName("deviceName")] string DeviceName
);

public sealed record VerifyBiometricRequest(
    [property: JsonPropertyName("deviceId")] string DeviceId
);

public sealed record BiometricDeviceResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("deviceId")] string DeviceId,
    [property: JsonPropertyName("deviceName")] string DeviceName,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("lastVerifiedAt")] DateTime? LastVerifiedAt,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt
);
