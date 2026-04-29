using System.ComponentModel.DataAnnotations;

namespace TeachAssist.Api.DTOs;

public class DisciplineDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateDisciplineDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Abbreviation { get; set; } = string.Empty;
}

public class UpdateDisciplineDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Abbreviation { get; set; } = string.Empty;
}
