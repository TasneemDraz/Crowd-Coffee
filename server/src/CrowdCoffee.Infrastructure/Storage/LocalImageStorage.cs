using CrowdCoffee.Application.Common;
using Microsoft.Extensions.Hosting;

namespace CrowdCoffee.Infrastructure.Storage;

/// <summary>
/// Saves under {ContentRootPath}/wwwroot/uploads/products — exactly where ASP.NET Core's default
/// UseStaticFiles() serves from (no extra static-file-provider config needed), keyed off
/// IHostEnvironment rather than IWebHostEnvironment so Infrastructure doesn't need a new package
/// reference (Microsoft.Extensions.Hosting.Abstractions is already a dependency here for
/// PaymentTimeoutJob's BackgroundService).
/// </summary>
public class LocalImageStorage(IHostEnvironment environment) : IImageStorage
{
    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName) is { Length: > 1 } ext ? ext : ".jpg";
        var storedFileName = $"{Guid.NewGuid()}{extension}";

        var uploadsDirectory = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "products");
        Directory.CreateDirectory(uploadsDirectory);

        var fullPath = Path.Combine(uploadsDirectory, storedFileName);
        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return $"/uploads/products/{storedFileName}";
    }
}
