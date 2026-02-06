using System;

namespace Ficha_Tecnica.Services;

public class RecipeImageStorageOptions
{
    public const string SectionName = "RecipeImageStorage";

    public string BucketName { get; set; } = string.Empty;

    public string ImagePrefix { get; set; } = "recipes";

    public int SignedUrlTtlMinutes { get; set; } = 30;

    public const long DefaultMaxUploadBytes = 5L * 1024 * 1024;

    public string CacheControl { get; set; } = "public, max-age=86400";

    public long MaxUploadBytes { get; set; } = DefaultMaxUploadBytes;

    internal string ResolveBucketName()
    {
        if (string.IsNullOrWhiteSpace(BucketName))
        {
            throw new InvalidOperationException("Recipe image storage bucket name must be configured.");
        }

        return BucketName.Trim();
    }

    internal string NormalizePrefix()
    {
        if (string.IsNullOrWhiteSpace(ImagePrefix))
        {
            return string.Empty;
        }

        return ImagePrefix.Trim().Trim('/');
    }

    internal TimeSpan ResolveSignedUrlTtl()
    {
        var minutes = Math.Clamp(SignedUrlTtlMinutes, 1, 7 * 24 * 60);
        return TimeSpan.FromMinutes(minutes);
    }

    internal long ResolveMaxUploadBytes()
    {
        const long minBytes = 32 * 1024; // 32 KB
        const long maxBytes = 20L * 1024 * 1024; // 20 MB

        var configured = MaxUploadBytes <= 0 ? DefaultMaxUploadBytes : MaxUploadBytes;
        return Math.Clamp(configured, minBytes, maxBytes);
    }
}
