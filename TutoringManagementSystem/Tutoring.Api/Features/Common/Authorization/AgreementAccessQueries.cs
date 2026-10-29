using Tutoring.Domain.Identity;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Tutors;

namespace Tutoring.Api.Features.Common.Authorization;

public static class AgreementAccessQueries
{
    public static IQueryable<TutoringAgreement> ForParticipant(
        this IQueryable<TutoringAgreement> agreements, UserAccountId accountId) =>
        agreements.Where(agreement => agreement.Tutor.UserAccountId == accountId ||
                                      agreement.Student.UserAccountId == accountId);

    public static IQueryable<Lesson> ForParticipant(
        this IQueryable<Lesson> lessons, UserAccountId accountId) =>
        lessons.Where(lesson => lesson.Agreement.Tutor.UserAccountId == accountId ||
                                lesson.Agreement.Student.UserAccountId == accountId);

    public static IQueryable<TutoringAgreement> ForTutor(
        this IQueryable<TutoringAgreement> agreements, UserAccountId accountId) =>
        agreements.Where(agreement => agreement.Tutor.UserAccountId == accountId);

    public static IQueryable<TutoringAgreement> ForTutor(
        this IQueryable<TutoringAgreement> agreements, TutorId tutorId) =>
        agreements.Where(agreement => agreement.TutorId == tutorId);
}
