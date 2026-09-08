using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class GroupCourseBookingRepository
    : Repository<GroupCourseBooking, int>, IGroupCourseBookingRepository
{
    public GroupCourseBookingRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyList<GroupCourseBooking>> GetByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Package)
                .ThenInclude(p => p.Course)
                    .ThenInclude(c => c.Type)
            .Include(x => x.Package)
                .ThenInclude(p => p.Course)
                    .ThenInclude(c => c.TimeSlot)
                        .ThenInclude(t => t.TimeSlotInstances)
            .Where(x => x.MemberId == memberId)
            .OrderByDescending(x => x.BookingTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupCourseBooking?> GetActiveByMemberAndCourseAsync(
        int memberId,
        int courseId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.Package)
                .ThenInclude(p => p.Course)
            .FirstOrDefaultAsync(
                x => x.MemberId == memberId
                    && x.BookingStatus == "1"
                    && x.Package.CourseId == courseId,
                cancellationToken);
    }

    public async Task<GroupCourseBooking?> GetDetailByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.Package)
                .ThenInclude(p => p.Course)
                    .ThenInclude(c => c.TimeSlot)
                        .ThenInclude(t => t.TimeSlotInstances)
            .FirstOrDefaultAsync(x => x.BookingId == bookingId, cancellationToken);
    }

    public async Task<int> GetNextBookingIdAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Context.Database.OpenConnectionAsync(cancellationToken);
            await using var command = Context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT SEQ_GROUP_COURSE_BOOKING.NEXTVAL FROM DUAL";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }
        catch
        {
            var max = await DbSet.MaxAsync(x => (int?)x.BookingId, cancellationToken) ?? 0;
            return max + 1;
        }
        finally
        {
            await Context.Database.CloseConnectionAsync();
        }
    }
}
