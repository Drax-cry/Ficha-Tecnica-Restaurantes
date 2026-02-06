using Ficha_Tecnica.Models;

namespace Ficha_Tecnica.Data;

public interface IRecipeCategoryRepository
{
    Task<IReadOnlyList<RecipeCategory>> GetCategoriesAsync(int userId, CancellationToken cancellationToken = default);

    Task<RecipeCategory> CreateCategoryAsync(RecipeCategory category, CancellationToken cancellationToken = default);
}
