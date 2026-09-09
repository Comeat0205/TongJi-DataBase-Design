using Domain.Entities;

namespace Domain.Interfaces;

public interface ITimeSlotRepository
{
    Task<TimeSlotTemplate?> GetTemplateAsync(string timeSlotId, CancellationToken cancellationToken = default);

    Task AddTemplateAsync(TimeSlotTemplate template, CancellationToken cancellationToken = default);

    Task RemoveInstancesAsync(string timeSlotId, CancellationToken cancellationToken = default);

    Task AddInstancesAsync(
        IEnumerable<TimeSlotInstance> instances,
        CancellationToken cancellationToken = default);
}
