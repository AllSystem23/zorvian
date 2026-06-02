namespace Zorvian.Application.DTOs.Notifications;

public sealed record RegisterDeviceRequest(
    string Token,
    string Platform
);
