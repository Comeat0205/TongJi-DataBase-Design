namespace Application.DTOs;

public sealed class CourseTypeRequestDto
{
    public int TypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
}
