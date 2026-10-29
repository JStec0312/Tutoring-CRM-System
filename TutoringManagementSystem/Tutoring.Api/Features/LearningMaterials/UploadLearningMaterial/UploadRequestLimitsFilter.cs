using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Tutoring.Infrastructure.FileStorage;

namespace Tutoring.Api.Features.LearningMaterials.UploadLearningMaterial;

// Resource filters run before multipart model binding.
public sealed class UploadRequestLimitsFilter(IOptions<MaterialUploadOptions> options) : IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var requestLimit = checked(options.Value.MaxSizeBytes + 65_536);
        var feature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false })
            feature.MaxRequestBodySize = requestLimit;
        context.HttpContext.Features.Set<IFormFeature>(new FormFeature(context.HttpContext.Request,
            new FormOptions { MultipartBodyLengthLimit = requestLimit }));
    }

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}
