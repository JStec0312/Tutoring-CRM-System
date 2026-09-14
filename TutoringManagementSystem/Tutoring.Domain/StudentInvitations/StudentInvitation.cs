using Tutoring.Domain.Common;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Tutors;

namespace Tutoring.Domain.StudentInvitations;

public sealed class StudentInvitation
    : Entity<StudentInvitationId>
{
    private StudentInvitation()
    {
    }

    public TutorId TutorId { get; private set; }
    public Tutor Tutor { get; private set; } = null!;

    public EmailAddress Recipient { get; private set; } = null!;

    public string TokenHash { get; private set; } = null!;

    public AgreementTitle Title { get; private set; } = null!;

    public HourlyRate? HourlyRate { get; private set; }

    public InvitationStatus Status { get; private set; }

    public DateTimeOffset ValidUntilUtc { get; private set; }
    public Subject Subject { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public StudentInvitation(
        TutorId tutorId,
        EmailAddress recipient,
        string tokenHash,
        AgreementTitle title,
        HourlyRate? hourlyRate,
        Subject subject,
        DateTimeOffset validUntilUtc,
        DateTimeOffset createdAtUtc)
    {
        TutorId = tutorId;
        Recipient = recipient;
        TokenHash = tokenHash;
        Title = title;
        HourlyRate = hourlyRate;
        Subject = subject;              
        ValidUntilUtc = validUntilUtc;
        CreatedAtUtc = createdAtUtc;

        Status = InvitationStatus.Created;
    }

    public void MarkAsSent()
    {
        Status = InvitationStatus.Sent;
    }

    public void Accept()
    {
        Status = InvitationStatus.Accepted;
    }

    public void Decline()
    {
        Status = InvitationStatus.Rejected;
    }

    public void Expire()
    {
        Status = InvitationStatus.Expired;
    }
}