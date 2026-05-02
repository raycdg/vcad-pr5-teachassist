using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeachAssist.Api.Models;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;

namespace TeachAssist.Api.Authorization;

public class ResourceOwnerAuthorizationHandler : IAuthorizationHandler
{
    private readonly DomainDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public ResourceOwnerAuthorizationHandler(
        DomainDbContext context,
        UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        foreach (var pendingRequirement in context.PendingRequirements.ToList())
        {
            if (pendingRequirement is ResourceAccessRequirement requirement)
            {
                if (await IsResourceAccessible(context, requirement))
                {
                    context.Succeed(pendingRequirement);
                }
            }
        }
    }

    private async Task<bool> IsResourceAccessible(AuthorizationHandlerContext authContext, ResourceAccessRequirement requirement)
    {
        var user = await _userManager.GetUserAsync(authContext.User);
        if (user == null) return false;

        // Admin and Manager have full access
        if (await _userManager.IsInRoleAsync(user, "Admin") ||
            await _userManager.IsInRoleAsync(user, "Manager"))
        {
            return true;
        }

        // Teacher access check
        return requirement.ResourceType switch
        {
            ResourceType.Discipline => await CanAccessDiscipline(user.Id, requirement.ResourceId),
            ResourceType.Course => await CanAccessCourse(user.Id, requirement.ResourceId),
            ResourceType.Task => await CanAccessTask(user.Id, requirement.ResourceId),
            _ => false
        };
    }

    private async Task<bool> CanAccessDiscipline(string userId, int disciplineId)
    {
        return await _context.DisciplineTeachers
            .AnyAsync(dt => dt.DisciplineId == disciplineId && dt.TeacherId == userId);
    }

    private async Task<bool> CanAccessCourse(string userId, int courseId)
    {
        return await _context.CourseTeachers
            .AnyAsync(ct => ct.CourseId == courseId && ct.TeacherId == userId);
    }

    private async Task<bool> CanAccessTask(string userId, int taskId)
    {
        var task = await _context.Tasks
            .Where(t => t.Id == taskId)
            .Select(t => new { t.DisciplineId })
            .FirstOrDefaultAsync();

        if (task == null) return false;

        return await CanAccessDiscipline(userId, task.DisciplineId);
    }
}

public enum ResourceType
{
    Discipline,
    Course,
    Task
}

public class ResourceAccessRequirement : IAuthorizationRequirement
{
    public ResourceType ResourceType { get; }
    public int ResourceId { get; }

    public ResourceAccessRequirement(ResourceType resourceType, int resourceId)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}
