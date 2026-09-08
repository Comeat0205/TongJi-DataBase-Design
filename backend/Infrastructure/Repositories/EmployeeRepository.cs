using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class EmployeeRepository : Repository<Employee, int>, IEmployeeRepository
{
    public EmployeeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Employee?> GetByNameAndPhoneAsync(string name, string phone, CancellationToken cancellationToken = default)
    {
        return await Context.Employees
            .FirstOrDefaultAsync(x => x.EmpName == name && x.Phone == phone, cancellationToken);
    }

    public async Task<Employee?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await Context.Employees
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }
}
