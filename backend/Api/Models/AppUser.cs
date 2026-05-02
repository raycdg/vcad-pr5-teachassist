using Microsoft.AspNetCore.Identity;
using TeachAssist.Domain.Models;

namespace TeachAssist.Api.Models;

public class AppUser : IdentityUser
{
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DisciplineTeacher> DisciplineTeachers { get; set; } = new List<DisciplineTeacher>();
    public ICollection<CourseTeacher> CourseTeachers { get; set; } = new List<CourseTeacher>();
}
