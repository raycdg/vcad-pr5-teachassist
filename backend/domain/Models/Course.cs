namespace TeachAssist.Domain.Models;

public class Course
{
    public int Id { get; set; }
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;
    public int GroupId { get; set; }
    public DomainGroup Group { get; set; } = null!;
    public int Year { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
