using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ficha_Tecnica.Services;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FichaTecnica.Tests;

public class RecipeImageStorageTests
{
    [Fact]
    public async Task SaveImageAsync_ThrowsWhenFileExceedsConfiguredLimit()
    {
        var scanner = new StubMalwareScanner();
        var (storage, storageClientMock) = CreateStorage(scanner, options => options.MaxUploadBytes = 10);
        var file = CreateFormFile(new byte[11], "oversized.jpg", "image/jpeg");

        await Assert.ThrowsAsync<ArgumentException>(() => storage.SaveImageAsync(file, CancellationToken.None));

        Assert.False(scanner.WasCalled);
        storageClientMock.Verify(
            c => c.UploadObjectAsync(
                It.IsAny<Object>(),
                It.IsAny<Stream>(),
                It.IsAny<UploadObjectOptions?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IProgress<IUploadProgress>>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveImageAsync_ThrowsWhenMalwareDetected()
    {
        var scanner = new StubMalwareScanner
        {
            Result = MalwareScanResult.Malicious("TestThreat", "simulated detection"),
        };

        var (storage, storageClientMock) = CreateStorage(scanner);
        var file = CreateFormFile(new byte[128], "clean.jpg", "image/jpeg");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => storage.SaveImageAsync(file, CancellationToken.None));

        Assert.Contains("simulated detection", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(scanner.WasCalled);
        storageClientMock.Verify(
            c => c.UploadObjectAsync(
                It.IsAny<Object>(),
                It.IsAny<Stream>(),
                It.IsAny<UploadObjectOptions?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IProgress<IUploadProgress>>()),
            Times.Never);
    }

    private static (RecipeImageStorage Storage, Mock<StorageClient> ClientMock) CreateStorage(
        StubMalwareScanner scanner,
        Action<RecipeImageStorageOptions>? configure = null)
    {
        var options = new RecipeImageStorageOptions
        {
            BucketName = "test-bucket",
            ImagePrefix = "recipes",
            CacheControl = "public, max-age=3600",
            SignedUrlTtlMinutes = 15,
        };

        configure?.Invoke(options);

        var optionsWrapper = Options.Create(options);

        var storageClientMock = new Mock<StorageClient>(MockBehavior.Strict);
        storageClientMock
            .Setup(c => c.UploadObjectAsync(
                It.IsAny<Object>(),
                It.IsAny<Stream>(),
                It.IsAny<UploadObjectOptions?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IProgress<IUploadProgress>>()))
            .ReturnsAsync(new Object());

        var storage = new RecipeImageStorage(
            optionsWrapper,
            NullLogger<RecipeImageStorage>.Instance,
            scanner,
            storageClientMock.Object,
            urlSignerFactory: () => UrlSigner.FromHmacSha256Key("test@example.com", new byte[32]));

        return (storage, storageClientMock);
    }

    private static IFormFile CreateFormFile(byte[] content, string fileName, string contentType)
    {
        var stream = new MemoryStream(content);
        var file = new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };

        return file;
    }

    private sealed class StubMalwareScanner : IMalwareScanner
    {
        public MalwareScanResult Result { get; set; } = MalwareScanResult.Clean();

        public bool WasCalled { get; private set; }

        public Task<MalwareScanResult> ScanAsync(
            Stream fileStream,
            MalwareScanContext context,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(Result);
        }
    }
}
