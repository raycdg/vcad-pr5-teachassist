using System.Text;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MimeKit;
using TeachAssist.Api.DTOs;
using TeachAssist.Api.Options;
using TeachAssist.Domain.Data;
using TeachAssist.Domain.Models;

namespace TeachAssist.Api.Services;

public class GradeNotificationAdapter
{
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<GradeNotificationAdapter> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public GradeNotificationAdapter(SmtpOptions smtpOptions, ILogger<GradeNotificationAdapter> logger, IServiceScopeFactory scopeFactory)
    {
        _smtpOptions = smtpOptions;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task NotifyGradesSavedAsync(int courseId, IEnumerable<GradeEntryDto> grades, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_smtpOptions.Host))
        {
            _logger.LogWarning("SMTP host not configured. Skipping grade notifications.");
            return;
        }

        var gradesByStudent = grades
            .GroupBy(g => g.StudentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (gradesByStudent.Count == 0) return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DomainDbContext>();

        var course = await context.Courses
            .Include(c => c.Discipline)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (course == null)
        {
            _logger.LogError("Course with id {CourseId} not found for notification.", courseId);
            return;
        }

        var studentIds = gradesByStudent.Keys.ToList();
        var students = await context.Students
            .Where(s => studentIds.Contains(s.Id))
            .ToListAsync(ct);

        var tasks = await context.Tasks
            .ToListAsync(ct);

        var taskDict = tasks.ToDictionary(t => t.Id, t => t);
        var disciplineName = course.Discipline.Name;
        var disciplineAbbreviation = course.Discipline.Abbreviation;

        foreach (var student in students)
        {
            if (string.IsNullOrWhiteSpace(student.Email))
            {
                _logger.LogWarning("Student {StudentId} ({FirstName} {LastName}) has no email. Skipping notification.",
                    student.Id, student.FirstName, student.LastName);
                continue;
            }

            if (!IsValidEmail(student.Email))
            {
                _logger.LogWarning("Student {StudentId} ({FirstName} {LastName}) has invalid email '{Email}'. Skipping notification.",
                    student.Id, student.FirstName, student.LastName, student.Email);
                continue;
            }

            var studentGrades = gradesByStudent[student.Id];
            var body = BuildEmailBody(student, disciplineName, disciplineAbbreviation, studentGrades, taskDict);
            var subject = $"Выставлены оценки по предмету {disciplineAbbreviation}";

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_smtpOptions.FromName, _smtpOptions.FromEmail));
                message.To.Add(new MailboxAddress($"{student.FirstName} {student.LastName}", student.Email));
                message.Subject = subject;

                var builder = new BodyBuilder();
                builder.HtmlBody = body;
                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, _smtpOptions.EnableSsl, ct);

                if (!string.IsNullOrEmpty(_smtpOptions.Username))
                {
                    await client.AuthenticateAsync(_smtpOptions.Username, _smtpOptions.Password, ct);
                }

                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);

                _logger.LogInformation("Grade notification sent to {Email} for course {CourseId}.", student.Email, courseId);
            }
            catch (MailKit.Net.Smtp.SmtpCommandException ex)
            {
                _logger.LogError(ex, "SMTP command error sending grade notification to {Email} for course {CourseId}.", student.Email, courseId);
            }
            catch (MailKit.Net.Smtp.SmtpProtocolException ex)
            {
                _logger.LogError(ex, "SMTP protocol error sending grade notification to {Email} for course {CourseId}.", student.Email, courseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending grade notification to {Email} for course {CourseId}.", student.Email, courseId);
            }
        }
    }

    private static bool IsValidEmail(string email)
    {
        return MailboxAddress.TryParse(email, out _);
    }

    private static string BuildEmailBody(Student student, string disciplineName, string disciplineAbbreviation,
        List<GradeEntryDto> grades, Dictionary<int, Domain.Models.DisciplineTask> taskDict)
    {
        var now = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        var html = new StringBuilder();
        html.AppendLine("<html><body>");
        html.AppendLine($"<p>Уважаемый(ая) {student.FirstName} {student.LastName}!</p>");
        html.AppendLine($"<p>Вам выставлены оценки по предмету <strong>{disciplineName}</strong> ({disciplineAbbreviation}) на {now}.</p>");
        html.AppendLine("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse:collapse;'>");
        html.AppendLine("<tr><th>Задание</th><th>Оценка</th></tr>");

        foreach (var grade in grades)
        {
            var task = taskDict.TryGetValue(grade.TaskId, out var t) ? t : null;
            var taskName = task?.Name ?? $"Задание #{grade.TaskId}";
            var value = string.IsNullOrWhiteSpace(grade.Value) ? "—" : grade.Value;
            html.AppendLine($"<tr><td>{taskName}</td><td>{value}</td></tr>");
        }

        html.AppendLine("</table>");
        html.AppendLine("</body></html>");
        return html.ToString();
    }
}
