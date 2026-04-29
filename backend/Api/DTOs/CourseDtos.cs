using System.ComponentModel.DataAnnotations;

namespace TeachAssist.Api.DTOs;

public class CourseDto
{
    public int Id { get; set; }
    public int DisciplineId { get; set; }
    public string DisciplineName { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int Year { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateCourseDto
{
    [Required]
    public int DisciplineId { get; set; }

    [Required]
    public int GroupId { get; set; }

    [Range(2000, 2100)]
    public int Year { get; set; }
}

public class UpdateCourseDto
{
    [Required]
    public int DisciplineId { get; set; }

    [Required]
    public int GroupId { get; set; }

    [Range(2000, 2100)]
    public int Year { get; set; }

    public bool IsActive { get; set; }
}

public class CourseProgressDto
{
    public int CourseId { get; set; }
    public string DisciplineName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<StudentProgressDto> Students { get; set; } = new();
    public List<TaskProgressDto> Tasks { get; set; } = new();
}

public class StudentProgressDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class TaskProgressDto
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GradingType { get; set; }
    public int? MaxScore { get; set; }
}

public class GradeEntryDto
{
    public int StudentId { get; set; }
    public int TaskId { get; set; }
    public string? Value { get; set; }
}

public class BulkSaveGradesDto
{
    public List<GradeEntryDto> Grades { get; set; } = new();
}
