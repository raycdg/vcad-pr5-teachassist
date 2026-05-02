namespace TeachAssist.Domain.Models;

public class DisciplineTeacher
{
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;

    public string TeacherId { get; set; } = string.Empty;
}
