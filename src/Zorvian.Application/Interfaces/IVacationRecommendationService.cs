namespace Zorvian.Application.Interfaces;

public interface IVacationRecommendationService
{
    Task<List<DateOnly>> RecommendDatesAsync(Guid employeeId, int daysRequested, int month, int year);
}
