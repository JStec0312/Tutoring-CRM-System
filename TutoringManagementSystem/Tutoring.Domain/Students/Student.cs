namespace Tutoring.Domain.Students;

public sealed class Student
{
    private Student()
    {
        // Konstruktor wymagany przez EF Core.
    }

    private Student(
        string firstName,
        string lastName,
        string? email,
        string? phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public string Email { get; private set; }

    public string? PhoneNumber { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Student Create(
        string firstName,
        string lastName,
        string? email,
        string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException(
                "Imię ucznia jest wymagane.",
                nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException(
                "Nazwisko ucznia jest wymagane.",
                nameof(lastName));
        }
        if (!string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "Nieprawidłowy format adresu e-mail.",
                nameof(email));
        }

        return new Student(
            firstName.Trim(),
            lastName.Trim(),
            email?.Trim(),
            NormalizeOptionalValue(phoneNumber));
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}