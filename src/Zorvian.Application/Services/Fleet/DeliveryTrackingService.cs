using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Services.Fleet;

/// <summary>
/// Delivery tracking service: status transitions, ETA notifications,
/// client-facing tracking, and delivery confirmation with evidence.
/// </summary>
public sealed class DeliveryTrackingService
{
    private readonly IDeliveryRepository _deliveryRepo;
    private readonly IVehicleRepository _vehicleRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly INotificationService _notification;
    private readonly IMapper _mapper;

    private static readonly HashSet<string> ValidTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending->InPreparation", "Pending->Cancelled",
        "InPreparation->Prepared", "InPreparation->Cancelled",
        "Prepared->InRoute", "Prepared->Cancelled",
        "InRoute->Delivered", "InRoute->Partial", "InRoute->Returned"
    };

    public DeliveryTrackingService(
        IDeliveryRepository deliveryRepo,
        IVehicleRepository vehicleRepo,
        IDriverRepository driverRepo,
        INotificationService notification,
        IMapper mapper)
    {
        _deliveryRepo = deliveryRepo;
        _vehicleRepo = vehicleRepo;
        _driverRepo = driverRepo;
        _notification = notification;
        _mapper = mapper;
    }

    /// <summary>Update delivery status with validation of allowed transitions.</summary>
    public async Task<DeliveryTrackingResponse?> UpdateStatusAsync(Guid deliveryId, DeliveryStatusUpdateRequest request)
    {
        var delivery = await _deliveryRepo.GetByIdAsync(deliveryId);
        if (delivery == null) return null;

        // Validate transition
        var transition = $"{delivery.Status}->{request.Status}";
        if (!ValidTransitions.Contains(transition))
            throw new InvalidOperationException(
                $"Invalid transition from '{delivery.Status}' to '{request.Status}'");

        var previousStatus = delivery.Status;
        delivery.Status = request.Status;
        delivery.GpsLatitude = request.GpsLatitude;
        delivery.GpsLongitude = request.GpsLongitude;
        delivery.Observations = request.Observations;

        if (request.Status == "Delivered")
            delivery.DeliveredAt = DateTime.UtcNow;

        await _deliveryRepo.UpdateAsync(delivery);
        await _deliveryRepo.SaveChangesAsync();

        // Send notification on status change
        await NotifyStatusChangeAsync(delivery, previousStatus);

        return new DeliveryTrackingResponse(
            delivery.Id,
            delivery.Code,
            delivery.Status,
            delivery.DeliveredAt,
            delivery.GpsLatitude,
            delivery.GpsLongitude,
            delivery.Observations,
            delivery.UpdatedAt ?? delivery.CreatedAt);
    }

    /// <summary>Confirm delivery with receiver info and evidence.</summary>
    public async Task<DeliveryTrackingResponse?> ConfirmDeliveryAsync(Guid deliveryId, ConfirmDeliveryRequest request)
    {
        var delivery = await _deliveryRepo.GetByIdAsync(deliveryId);
        if (delivery == null) return null;

        if (delivery.Status != "InRoute" && delivery.Status != "Prepared")
            throw new InvalidOperationException(
                $"Cannot confirm delivery in status '{delivery.Status}'. Must be 'InRoute' or 'Prepared'");

        delivery.Status = "Delivered";
        delivery.DeliveredAt = DateTime.UtcNow;
        delivery.ReceiverName = request.ReceiverName;
        delivery.ReceiverId = request.ReceiverId;
        delivery.GpsLatitude = request.GpsLatitude;
        delivery.GpsLongitude = request.GpsLongitude;
        delivery.Observations = request.Observations;
        delivery.SignatureUrl = request.SignatureUrl;
        delivery.PhotosJson = request.PhotosJson;

        await _deliveryRepo.UpdateAsync(delivery);
        await _deliveryRepo.SaveChangesAsync();

        // Notify client of successful delivery
        await NotifyDeliveryConfirmedAsync(delivery);

        return new DeliveryTrackingResponse(
            delivery.Id, delivery.Code, "Delivered",
            delivery.DeliveredAt, delivery.GpsLatitude, delivery.GpsLongitude,
            delivery.Observations, delivery.UpdatedAt ?? delivery.CreatedAt);
    }

    /// <summary>Get delivery tracking timeline for internal use.</summary>
    public async Task<DeliveryTrackingTimeline?> GetTrackingTimelineAsync(Guid deliveryId)
    {
        var delivery = await _deliveryRepo.GetByIdAsync(deliveryId);
        if (delivery == null) return null;

        var events = new List<TrackingEvent>
        {
            new("Pending", delivery.CreatedAt, null, null, "Entrega creada")
        };

        if (delivery.DeliveredAt.HasValue)
            events.Add(new("Delivered", delivery.DeliveredAt.Value,
                delivery.GpsLatitude, delivery.GpsLongitude,
                $"Entregado a {delivery.ReceiverName}"));

        return new DeliveryTrackingTimeline(
            delivery.Id,
            delivery.Code,
            delivery.Client != null ? $"{delivery.Client.FirstName} {delivery.Client.LastName}" : "N/A",
            delivery.DeliveryAddress,
            delivery.Status,
            delivery.ScheduledDate.ToDateTime(TimeOnly.MinValue),
            delivery.TimeWindowStart,
            delivery.TimeWindowEnd,
            delivery.DeliveredAt,
            delivery.Vehicle?.Plate,
            delivery.Driver?.FullName,
            events);
    }

    /// <summary>Get client-facing delivery tracking (public, no auth needed).</summary>
    public async Task<ClientDeliveryTracking?> GetClientTrackingAsync(string deliveryCode)
    {
        var deliveries = await _deliveryRepo.GetAllAsync();
        var delivery = deliveries.FirstOrDefault(d => d.Code == deliveryCode);
        if (delivery == null) return null;

        var events = new List<TrackingEvent>();
        if (delivery.DeliveredAt.HasValue)
            events.Add(new("Delivered", delivery.DeliveredAt.Value,
                delivery.GpsLatitude, delivery.GpsLongitude, "Entrega completada"));

        return new ClientDeliveryTracking(
            delivery.Id,
            delivery.Code,
            delivery.Status,
            null, // Would calculate ETA from route
            delivery.DeliveredAt,
            delivery.Driver?.FullName,
            delivery.Vehicle?.Plate,
            delivery.DeliveryAddress,
            delivery.ScheduledDate,
            events);
    }

    /// <summary>Send ETA notification to client.</summary>
    public async Task<EtaNotificationResponse?> SendEtaNotificationAsync(EtaNotificationRequest request)
    {
        var delivery = await _deliveryRepo.GetByIdAsync(request.DeliveryId);
        if (delivery == null) return null;

        var estimatedArrival = DateTime.UtcNow.AddMinutes(30); // Placeholder — would use route ETA

        var title = "Entrega en camino";
        var message = $"Su entrega {delivery.Code} está en ruta. " +
                      $"Estimada llegada: {estimatedArrival:HH:mm}. " +
                      $"Dirección: {delivery.DeliveryAddress}";

        var tenantId = delivery.TenantId;
        if (delivery.Client != null)
        {
            // Send push notification
            await _notification.NotifyTenantAsync(
                tenantId, title, message, "delivery_eta", delivery.Id.ToString());
        }

        return new EtaNotificationResponse(
            delivery.Id, delivery.Code, estimatedArrival, true, request.NotificationType);
    }

    // ── Private notification helpers ──

    private async Task NotifyStatusChangeAsync(Delivery delivery, string previousStatus)
    {
        var title = $"Entrega {delivery.Code} — {delivery.Status}";
        var message = previousStatus == "Pending"
            ? $"La entrega {delivery.Code} ha sido preparada."
            : $"Estado actualizado de '{previousStatus}' a '{delivery.Status}'.";

        await _notification.NotifyTenantAsync(
            delivery.TenantId, title, message, "delivery_status", delivery.Id.ToString());
    }

    private async Task NotifyDeliveryConfirmedAsync(Delivery delivery)
    {
        var title = $"Entrega completada: {delivery.Code}";
        var message = $"La entrega {delivery.Code} ha sido confirmada por {delivery.ReceiverName}.";

        await _notification.NotifyTenantAsync(
            delivery.TenantId, title, message, "delivery_confirmed", delivery.Id.ToString());
    }
}
