namespace Domain.Interfaces;

public interface IGroupCourseScheduleRepository
{
    Task<(bool Success, string Message)> CheckConflictAsync(
        int courseId,
        int coachId,
        DateTime courseDate,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
}
