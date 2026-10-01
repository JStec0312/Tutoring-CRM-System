using Tutoring.Domain.Common;
using Tutoring.Domain.Students;
using Tutoring.Domain.Tutors;

namespace Tutoring.Domain.TutoringAgreements;

public sealed class TutoringAgreement
    : Entity<TutoringAgreementId>
{
    private TutoringAgreement()
    {
    }

    public TutorId TutorId { get; private set; }
    public Tutor Tutor { get; private set; } = null!;

    public StudentId StudentId { get; private set; }
    public Student Student { get; private set; } = null!;

    public Subject Subject { get; private set; } = null!;

    public HourlyRate? HourlyRate { get; private set; } = null!;

    public AgreementStatus Status { get; private set; }
    public AgreementTitle AgreementTitle { get; private set; } 

    public string? PrivateNotes { get; private set; }


    public DateTimeOffset CreatedAtUtc { get; private set; }

    public TutoringAgreement(
    TutorId tutorId,
    StudentId studentId,
    Subject subject,
    HourlyRate? hourlyRate,
    AgreementTitle agreementTitle,
    DateTimeOffset createdAtUtc
    )
    {
        TutorId = tutorId;
        StudentId = studentId;
        Subject = subject;
        HourlyRate = hourlyRate;
        AgreementTitle = agreementTitle;
        Status = AgreementStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public void UpdateTutoringAgreementDetails(
        Subject? subject,
        string? notes)
    {
        if(subject is not null)
        {
            Subject = subject;
        }

        if(notes is not null)
        {
            PrivateNotes = notes;
        }
    }

    public void UpdateHourlyRate(HourlyRate? hourlyRate)
    {
        HourlyRate = hourlyRate;
    }

    public void Activate()
    {
        if (Status == AgreementStatus.Ended)
        {
            throw new TutoringAgreementEndedException(Id.Value);
        }

        Status = AgreementStatus.Active;
    }

    public void Deactivate()
    {
        if (Status == AgreementStatus.Ended)
        {
            throw new TutoringAgreementEndedException(Id.Value);
        }

        Status = AgreementStatus.Suspended;
    }

    public bool HasHourlyRate => HourlyRate is not null;
}
