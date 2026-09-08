using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class TimeSlotRepository : ITimeSlotRepository
{
    private readonly AppDbContext _context;

    public TimeSlotRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<TimeSlotTemplate?> GetTemplateAsync(
        string timeSlotId,
        CancellationToken cancellationToken = default)
    {
        return _context.TimeSlotTemplates
            .FirstOrDefaultAsync(x => x.TimeSlotId == timeSlotId, cancellationToken);
    }

    public async Task AddTemplateAsync(
        TimeSlotTemplate template,
        CancellationToken cancellationToken = default)
    {
        await _context.TimeSlotTemplates.AddAsync(template, cancellationToken);
    }

    public async Task RemoveInstancesAsync(
        string timeSlotId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.TimeSlotInstances
            .Where(x => x.TimeSlotId == timeSlotId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _context.TimeSlotInstances.RemoveRange(existing);
        }
    }

    public async Task AddInstancesAsync(
        IEnumerable<TimeSlotInstance> instances,
        CancellationToken cancellationToken = default)
    {
        await _context.TimeSlotInstances.AddRangeAsync(instances, cancellationToken);
    }
}
