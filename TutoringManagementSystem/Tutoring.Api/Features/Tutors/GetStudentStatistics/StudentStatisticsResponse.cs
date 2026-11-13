namespace Tutoring.Api.Features.Tutors.GetStudentStatistics;

public sealed record StudentStatisticsResponse(
    StudentLessonStatistics Lessons,
    StudentAttendanceStatistics Attendance,
    IReadOnlyList<StudentPaymentStatistics> Payments);

public sealed record StudentLessonStatistics(
    int ScheduledCount,
    int CompletedCount,
    int CancelledCount,
    int MissedCount,
    decimal CompletedMinutes);

public sealed record StudentAttendanceStatistics(
    decimal? AttendanceRatePercent,
    int CancelledByStudentCount,
    int CancelledByTutorCount);

public sealed record StudentPaymentStatistics(
    string Currency,
    decimal PaidAmount,
    decimal UnpaidAmount,
    int UnpaidChargeCount);
