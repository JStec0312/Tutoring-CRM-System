using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Infrastructure.FileStorage;

public class UploadValidationException(string code, string message) : UseCaseException(code, message);

public sealed class UploadTooLargeException()
    : UploadValidationException("LearningMaterials.FileTooLarge", "The file exceeds the configured size limit.");
