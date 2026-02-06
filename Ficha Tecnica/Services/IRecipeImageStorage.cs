using Microsoft.AspNetCore.Http;

namespace Ficha_Tecnica.Services;

public interface IRecipeImageStorage
{
    Task<string> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default);

    Task DeleteImageAsync(string? storedPath, CancellationToken cancellationToken = default);

    Task<string?> GetImageUrlAsync(string? storedPath, CancellationToken cancellationToken = default);
}
