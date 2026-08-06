namespace CrowdCoffee.Application.Common;

/// <summary>
/// Port for Story 13.1's product-image upload. Local-disk-backed for now (LocalImageStorage,
/// Infrastructure) — no cloud object storage account exists for this project, same "no live
/// creds yet" situation as Epic 10's Paymob integration. Swap the Infrastructure implementation
/// for a cloud-storage adapter later without touching any caller of this interface.
/// </summary>
public interface IImageStorage
{
    /// Returns the public URL the saved image is reachable at (e.g. "/uploads/products/xyz.jpg"),
    /// not a filesystem path — callers (Product.ImageUrl) only ever deal in URLs.
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);
}
