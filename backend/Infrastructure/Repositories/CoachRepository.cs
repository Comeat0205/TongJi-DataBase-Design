using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class CoachRepository : Repository<Coach, int>, ICoachRepository
{
    public CoachRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Coach?> GetByNameAndPhoneAsync(string name, string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await Context.Coaches
            .FirstOrDefaultAsync(x => x.CoachName == name && x.PhoneNumber == phoneNumber, cancellationToken);
    }
}
