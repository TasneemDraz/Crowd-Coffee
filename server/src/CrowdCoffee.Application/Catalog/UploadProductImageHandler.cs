using CrowdCoffee.Application.Common;
using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Application.Catalog;

/// <summary>
/// Validation lives here (Application, testable without touching a filesystem); the actual write
/// is delegated to IImageStorage (Infrastructure). Kept separate from Create/UpdateProductHandler
/// — a manager uploads an image once and reuses the returned URL across edits/retries, rather
/// than re-uploading the file on every product save.
/// </summary>
public class UploadProductImageHandler(IImageStorage imageStorage)
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public async Task<Result<string>> HandleAsync(Stream content, string fileName, string contentType, long length, CancellationToken cancellationToken)
    {
        if (length <= 0 || length > MaxBytes)
        {
            return Result.Failure<string>(Error.Validation("Image.InvalidSize", "Image must be non-empty and no larger than 5MB."));
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            return Result.Failure<string>(Error.Validation("Image.InvalidType", "Image must be JPEG, PNG, or WebP."));
        }

        var url = await imageStorage.SaveAsync(content, fileName, contentType, cancellationToken);
        return Result.Success(url);
    }
}
