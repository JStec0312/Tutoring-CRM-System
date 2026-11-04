using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.LearningMaterials.Exceptions;

public sealed class MaterialFileNotFoundException()
    : NotFoundException("LearningMaterials.FileNotFound", "The learning material file could not be found.");
