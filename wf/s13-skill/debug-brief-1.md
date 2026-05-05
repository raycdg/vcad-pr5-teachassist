# Debugging Brief: Grade Notification Sends All Grades Instead of Changed Only

## Date
2026-05-05

## Symptom
When a teacher saves grades on the student grades page, the student receives an email notification containing ALL grades for the course, including those that were set previously and not changed in the current save operation.

## Expected Behavior
The notification email should only include grades that were changed/added in the current save operation.

## Affected Components

### Backend
- `backend/Api/Controllers/CoursesController.cs` — method `SaveGrades` (line 260-311)
- `backend/Api/Controllers/CoursesController.cs` — method `NotifyGradesSavedSafeAsync` (line 313-323)
- `backend/Api/Services/GradeNotificationAdapter.cs` — method `NotifyGradesSavedAsync` (line 28-124)
- `backend/Api/Services/GradeNotificationAdapter.cs` — method `BuildEmailBody` (line 131-153)
- `backend/Api/DTOs/CourseDtos.cs` — `GradeEntryDto`, `BulkSaveGradesDto`

### Frontend (potential caller)
- To be identified — the Vue component that sends the POST request to `api/courses/{id}/grades`

### Tests
- `tests/TeachAssist.Api.Tests/GradeNotificationAdapterTests.cs`

## Data Flow

1. Frontend sends `POST api/courses/{id}/grades` with `BulkSaveGradesDto` containing list of `GradeEntryDto`
2. `SaveGrades` iterates over `dto.Grades`, updates or creates `StudentGrade` records
3. After `SaveChangesAsync`, calls `NotifyGradesSavedSafeAsync(id, dto.Grades)`
4. `NotifyGradesSavedSafeAsync` calls `_notificationAdapter.NotifyGradesSavedAsync(courseId, grades, ...)`
5. `NotifyGradesSavedAsync` groups grades by student, fetches course/student/task data, builds email with ALL grades passed in
6. Email body includes every grade from the `grades` parameter

## Root Cause Candidates

The `dto.Grades` passed to `NotifyGradesSavedAsync` contains ALL grades sent from the frontend, not just the changed ones. There are two possibilities:

1. **Frontend sends all grades** — the frontend component sends the entire grade matrix (all students, all tasks) on every save, not just the changed entries.
2. **Backend does not filter** — even if frontend sends only changed grades, the backend might be re-fetching all grades from DB before notification.

From the code at `CoursesController.cs:308`: `NotifyGradesSavedSafeAsync(id, dto.Grades)` — it passes `dto.Grades` directly, which is whatever the frontend sent. If frontend sends all grades, the notification will include all grades.

## Investigation Points

1. Check frontend component that calls `POST api/courses/{id}/grades` — does it send all grades or only changed?
2. Check if `GradeEntryDto.Value` for unchanged grades is the same as before or always populated
3. Check `BuildEmailBody` — it iterates over all grades passed in, with no filtering

## Log Evidence
- `backend/grade-notifications.log` shows notifications sent to students with course grades
- No evidence in logs of what grades were included (changed vs all)
