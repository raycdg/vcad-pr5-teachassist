using System.ComponentModel.DataAnnotations;

namespace TeachAssist.Api.DTOs;

public class DisciplineTaskDto
{
    public int Id { get; set; }
    public int DisciplineId { get; set; }
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GradingType { get; set; }
    public int? MaxScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateDisciplineTaskDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(1, 2)]
    public int GradingType { get; set; }

    public int? MaxScore { get; set; }
}

public class UpdateDisciplineTaskDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(1, 2)]
    public int GradingType { get; set; }

    public int? MaxScore { get; set; }
}
