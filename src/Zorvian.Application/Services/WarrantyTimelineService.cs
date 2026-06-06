using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class WarrantyTimelineService
{
    private readonly IWarrantyRepository _repo;
    private readonly IWarrantyEventRepository _eventRepo;
    private readonly IWarrantyStateHistoryRepository _historyRepo;

    public WarrantyTimelineService(
        IWarrantyRepository repo,
        IWarrantyEventRepository eventRepo,
        IWarrantyStateHistoryRepository historyRepo)
    {
        _repo = repo;
        _eventRepo = eventRepo;
        _historyRepo = historyRepo;
    }

    public async Task<List<WarrantyTimelineItem>> GetTimelineAsync(Guid warrantyId)
    {
        var events = await _eventRepo.GetByWarrantyIdAsync(warrantyId);
        var history = await _historyRepo.GetByWarrantyIdAsync(warrantyId);

        var timeline = new List<WarrantyTimelineItem>();

        foreach (var e in events)
        {
            timeline.Add(new WarrantyTimelineItem(e.OccurredAt, "EVENT", e.EventType, e.Description));
        }

        foreach (var h in history)
        {
            timeline.Add(new WarrantyTimelineItem(h.ChangedAt, "STATE_CHANGE", $"Cambio a {h.ToStatus}", h.Reason));
        }

        return timeline.OrderByDescending(t => t.Timestamp).ToList();
    }
}

public sealed record WarrantyTimelineItem(DateTime Timestamp, string Type, string Title, string? Description);
