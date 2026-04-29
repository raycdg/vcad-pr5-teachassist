namespace TeachAssist.Domain.Models;

public class DisciplineTask
{
    public int Id { get; set; }
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GradingType { get; set; }
    public int? MaxScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
