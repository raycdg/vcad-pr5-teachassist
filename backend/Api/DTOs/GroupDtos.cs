using System.ComponentModel.DataAnnotations;

namespace TeachAssist.Api.DTOs;

public class GroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public int YearStarted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateGroupDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ShortName { get; set; } = string.Empty;

    [Range(2000, 2100)]
    public int YearStarted { get; set; }
}

public class UpdateGroupDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ShortName { get; set; } = string.Empty;

    [Range(2000, 2100)]
    public int YearStarted { get; set; }
}
