namespace TeachAssist.Domain.Models;

public class CourseTeacher
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string TeacherId { get; set; } = string.Empty;
}
