using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ficha_Tecnica.Services;

public class RecipeImageStorage : IRecipeImageStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp",
    };

    private readonly StorageClient _storageClient;
    private readonly Lazy<UrlSigner> _urlSigner;
    private readonly RecipeImageStorageOptions _options;
    private readonly ILogger<RecipeImageStorage> _logger;
    private readonly IMalwareScanner _malwareScanner;
    private readonly string _bucketName;
    private readonly string _normalizedPrefix;

    public RecipeImageStorage(
        IOptions<RecipeImageStorageOptions> options,
        ILogger<RecipeImageStorage> logger,
        IMalwareScanner malwareScanner,
        StorageClient? storageClient = null,
        Func<UrlSigner>? urlSignerFactory = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _malwareScanner = malwareScanner ?? throw new ArgumentNullException(nameof(malwareScanner));

        _bucketName = _options.ResolveBucketName();
        _normalizedPrefix = _options.NormalizePrefix();

        _storageClient = storageClient ?? StorageClient.Create();
        _urlSigner = new Lazy<UrlSigner>(() => (urlSignerFactory ?? CreateDefaultUrlSigner)());
    }

    public async Task<string> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file is null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        if (file.Length == 0)
        {
            throw new ArgumentException("The provided image file is empty.", nameof(file));
        }

        var maxUploadBytes = _options.ResolveMaxUploadBytes();
        if (file.Length > maxUploadBytes)
        {
            throw new ArgumentException(
                $"The provided image file exceeds the maximum allowed size of {maxUploadBytes:N0} bytes.",
                nameof(file));
        }

        var extension = NormalizeExtension(file);
        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
        var objectName = string.IsNullOrEmpty(_normalizedPrefix)
            ? fileName
            : $"{_normalizedPrefix}/{fileName}";

        var contentType = NormalizeContentType(file.ContentType, extension);
        await using (var scanStream = file.OpenReadStream())
        {
            var scanContext = new MalwareScanContext(
                Path.GetFileName(file.FileName ?? string.Empty),
                contentType,
                extension);

            var scanResult = await _malwareScanner.ScanAsync(scanStream, scanContext, cancellationToken);
            if (scanResult.IsMalicious)
            {
                var reason = string.IsNullOrWhiteSpace(scanResult.Reason)
                    ? "Malware detected during scanning."
                    : scanResult.Reason;

                _logger.LogWarning(
                    "Blocked recipe image upload for {FileName} because malware was detected (Threat: {ThreatName}, Reason: {Reason}).",
                    scanContext.FileName,
                    scanResult.ThreatName ?? "Unknown",
                    reason);

                throw new InvalidOperationException(
                    $"The uploaded image failed the malware scan: {reason}");
            }
        }

        var storageObject = new Google.Apis.Storage.v1.Data.Object
        {
            Bucket = _bucketName,
            Name = objectName,
            ContentType = contentType,
            CacheControl = _options.CacheControl,
            Metadata = new Dictionary<string, string>
            {
                ["originalFileName"] = Path.GetFileName(file.FileName ?? string.Empty),
            },
        };

        using var uploadStream = file.OpenReadStream();
        await _storageClient.UploadObjectAsync(
            storageObject,
            uploadStream,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Stored recipe image {ObjectName} in bucket {BucketName} ({FileSize} bytes).",
            objectName,
            _bucketName,
            file.Length);

        return objectName;
    }

    public async Task DeleteImageAsync(string? storedPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return;
        }

        if (!TryResolveObjectName(storedPath, out var objectName))
        {
            _logger.LogDebug("Skipping deletion for non-cloud image path {StoredPath}.", storedPath);
            return;
        }

        try
        {
            await _storageClient.DeleteObjectAsync(_bucketName, objectName, cancellationToken: cancellationToken);
            _logger.LogInformation(
                "Removed recipe image {ObjectName} from bucket {BucketName}.",
                objectName,
                _bucketName);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug(
                ex,
                "Recipe image {ObjectName} not found in bucket {BucketName} during deletion.",
                objectName,
                _bucketName);
        }
    }

    public Task<string?> GetImageUrlAsync(string? storedPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return Task.FromResult<string?>(null);
        }

        if (!TryResolveObjectName(storedPath, out var objectName))
        {
            return Task.FromResult<string?>(storedPath);
        }

        var expiration = DateTimeOffset.UtcNow.Add(_options.ResolveSignedUrlTtl());
        var requestTemplate = UrlSigner.RequestTemplate
            .FromBucket(_bucketName)
            .WithObjectName(objectName)
            .WithHttpMethod(HttpMethod.Get)
            .WithQueryParameters(BuildResponseHeaderQueryParameters(objectName));

        var options = UrlSigner.Options.FromExpiration(expiration);

        var signedUrl = _urlSigner.Value.Sign(requestTemplate, options);

        return Task.FromResult<string?>(signedUrl);
    }

    private static UrlSigner CreateDefaultUrlSigner()
    {
        var credential = GoogleCredential.GetApplicationDefault();
        if (credential.IsCreateScopedRequired)
        {
            credential = credential.CreateScoped(StorageService.Scope.DevstorageFullControl);
        }

        return UrlSigner.FromCredential(credential);
    }

    private static string NormalizeExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = GuessExtensionFromContentType(file.ContentType);
        }

        extension = extension?.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension))
        {
            return extension;
        }

        return ".jpg";
    }

    private static string NormalizeContentType(string? contentType, string extension)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            return contentType;
        }

        return extension switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg",
        };
    }

    private static string GuessExtensionFromContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return ".jpg";
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => ".jpg",
        };
    }

    private static string GuessContentTypeFromName(string objectName)
    {
        var extension = Path.GetExtension(objectName).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg",
        };
    }

    private IEnumerable<KeyValuePair<string, IEnumerable<string>>> BuildResponseHeaderQueryParameters(string objectName)
    {
        var parameters = new List<KeyValuePair<string, IEnumerable<string>>>
        {
            new("response-content-type", new[] { GuessContentTypeFromName(objectName) }),
        };

        if (!string.IsNullOrWhiteSpace(_options.CacheControl))
        {
            parameters.Add(new("response-cache-control", new[] { _options.CacheControl }));
        }

        return parameters;
    }

    private bool TryResolveObjectName(string storedPath, out string objectName)
    {
        objectName = string.Empty;
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return false;
        }

        var trimmed = storedPath.Trim();

        if (trimmed.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
        {
            var withoutScheme = trimmed.Substring(5);
            var separatorIndex = withoutScheme.IndexOf('/');
            if (separatorIndex <= 0 || separatorIndex >= withoutScheme.Length - 1)
            {
                return false;
            }

            objectName = withoutScheme[(separatorIndex + 1)..];
            return true;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            if (uri.Host.EndsWith("storage.googleapis.com", StringComparison.OrdinalIgnoreCase))
            {
                var segments = uri.AbsolutePath.Trim('/');
                if (string.IsNullOrEmpty(segments))
                {
                    return false;
                }

                var firstSeparator = segments.IndexOf('/');
                if (firstSeparator <= 0 || firstSeparator >= segments.Length - 1)
                {
                    return false;
                }

                objectName = WebUtility.UrlDecode(segments[(firstSeparator + 1)..]);
                return true;
            }

            return false;
        }

        if (trimmed.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(_normalizedPrefix) && !trimmed.StartsWith(_normalizedPrefix, StringComparison.Ordinal))
        {
            _logger.LogDebug(
                "Image path {StoredPath} does not match configured prefix {Prefix}. Proceeding with provided value.",
                trimmed,
                _normalizedPrefix);
        }

        objectName = trimmed;
        return true;
    }
}
