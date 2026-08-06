using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Application.Common;

namespace CrowdCoffee.Application.Tests.Catalog;

public class UploadProductImageHandlerTests
{
    private class FakeImageStorage : IImageStorage
    {
        public bool WasCalled { get; private set; }

        public Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult("/uploads/products/fake.jpg");
        }
    }

    [Fact]
    public async Task ValidJpeg_SavesAndReturnsUrl()
    {
        var storage = new FakeImageStorage();
        var handler = new UploadProductImageHandler(storage);
        using var content = new MemoryStream([1, 2, 3]);

        var result = await handler.HandleAsync(content, "photo.jpg", "image/jpeg", length: 3, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("/uploads/products/fake.jpg", result.Value);
        Assert.True(storage.WasCalled);
    }

    [Fact]
    public async Task UnsupportedContentType_IsRejected_WithoutTouchingStorage()
    {
        var storage = new FakeImageStorage();
        var handler = new UploadProductImageHandler(storage);
        using var content = new MemoryStream([1, 2, 3]);

        var result = await handler.HandleAsync(content, "photo.gif", "image/gif", length: 3, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.InvalidType", result.Error!.Code);
        Assert.False(storage.WasCalled);
    }

    [Fact]
    public async Task OversizedFile_IsRejected_WithoutTouchingStorage()
    {
        var storage = new FakeImageStorage();
        var handler = new UploadProductImageHandler(storage);
        using var content = new MemoryStream();

        var result = await handler.HandleAsync(content, "photo.jpg", "image/jpeg", length: 6 * 1024 * 1024, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.InvalidSize", result.Error!.Code);
        Assert.False(storage.WasCalled);
    }
}
