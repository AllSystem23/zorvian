namespace Zorvian.Core.Entities;

/// <summary>
/// Enterprise support system (P4.10)
/// Tickets, Knowledge Base, SLA tracking
/// </summary>

/// <summary>
/// Support ticket with SLA tracking
/// </summary>
public class SupportTicket : BaseEntity
{
    public string Code { get; set; } = string.Empty; // TKT-001
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "open"; // open, in_progress, waiting, resolved, closed
    public string Priority { get; set; } = "normal"; // low, normal, high, urgent
    public string Category { get; set; } = string.Empty; // bug, feature, question
    public Guid? AssignedTo { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? SlaDueAt { get; set; }
    public int PriorityLevel { get; set; } // 1=Critical, 2=High, 3=Normal, 4=Low
    public bool SlaBreached { get; set; }
}

/// <summary>
/// Ticket reply/comment
/// </summary>
public class TicketReply : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = false; // Internal note vs public reply
    public List<string> Attachments { get; set; } = new();
}

/// <summary>
/// Knowledge Base article
/// </summary>
public class KnowledgeBaseArticle : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public int Views { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
}

/// <summary>
/// SLA policy
/// </summary>
public class SlaPolicy
{
    public string Priority { get; set; } = string.Empty; // low, normal, high, urgent
    public TimeSpan FirstResponseTime { get; set; }
    public TimeSpan ResolutionTime { get; set; }
    public int BusinessHoursPerDay { get; set; } = 8;
    public Dictionary<string, TimeSpan> BusinessHours { get; set; } = new();
}

/// <summary>
/// Support agent/team member
/// </summary>
public class SupportAgent : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string[] Categories { get; set; } = Array.Empty<string>();
    public string[] Languages { get; set; } = Array.Empty<string>();
    public int MaxActiveTickets { get; set; } = 20;
    public int CurrentActiveTickets { get; set; }
    public bool IsAvailable { get; set; } = true;
}

/// <summary>
/// SLA service that calculates compliance
/// </summary>
public static class SlaService
{
    public static readonly SlaPolicy[] DefaultPolicies = new[]
    {
        new SlaPolicy
        {
            Priority = "urgent",
            FirstResponseTime = TimeSpan.FromHours(1),
            ResolutionTime = TimeSpan.FromHours(4),
        },
        new SlaPolicy
        {
            Priority = "high",
            FirstResponseTime = TimeSpan.FromHours(4),
            ResolutionTime = TimeSpan.FromHours(24),
        },
        new SlaPolicy
        {
            Priority = "normal",
            FirstResponseTime = TimeSpan.FromHours(8),
            ResolutionTime = TimeSpan.FromDays(3),
        },
        new SlaPolicy
        {
            Priority = "low",
            FirstResponseTime = TimeSpan.FromHours(24),
            ResolutionTime = TimeSpan.FromDays(7),
        },
    };

    public static SlaPolicy GetPolicy(string priority)
    {
        return DefaultPolicies.FirstOrDefault(p => p.Priority == priority) ?? DefaultPolicies[2];
    }

    public static DateTime CalculateSlaDueDate(string priority, DateTime startTime)
    {
        var policy = GetPolicy(priority);
        return startTime.Add(policy.ResolutionTime);
    }

    public static bool IsSlaBreached(SupportTicket ticket)
    {
        if (ticket.SlaDueAt == null) return false;
        if (ticket.Status == "resolved" || ticket.Status == "closed") return false;
        return DateTime.UtcNow > ticket.SlaDueAt;
    }

    public static TimeSpan GetTimeRemaining(SupportTicket ticket)
    {
        if (ticket.SlaDueAt == null) return TimeSpan.Zero;
        var remaining = ticket.SlaDueAt.Value - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
