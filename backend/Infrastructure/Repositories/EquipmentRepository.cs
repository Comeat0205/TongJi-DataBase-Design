using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class EquipmentRepository : Repository<Equipment, int>, IEquipmentRepository
{
    public EquipmentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Equipment>> GetManagementListAsync(string? keyword, string? status, int? venueId, CancellationToken cancellationToken = default)
    {
        var query = Context.Equipment.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(x =>
                x.EquipName.Contains(normalizedKeyword) ||
                x.EquipId.ToString().Contains(normalizedKeyword) ||
                (x.VenueId != null && x.VenueId.ToString()!.Contains(normalizedKeyword)));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            var normalizedStatus = status == "inactive" ? "0" : "1";
            query = query.Where(x => x.Status == normalizedStatus);
        }

        if (venueId.HasValue)
        {
            query = query.Where(x => x.VenueId == venueId.Value);
        }

        return await query.OrderBy(x => x.EquipId).ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextEquipmentIdAsync(CancellationToken cancellationToken = default)
    {
        var maxId = await Context.Equipment.MaxAsync(x => (int?)x.EquipId, cancellationToken) ?? 0;
        return maxId + 1;
    }
}
