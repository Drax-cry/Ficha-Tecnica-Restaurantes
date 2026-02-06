using Ficha_Tecnica.Models;

namespace Ficha_Tecnica.Data;

public interface ICategoryRepository
{
    Task<IReadOnlyList<IngredientCategory>> GetCategoriesAsync(int userId, CancellationToken cancellationToken = default);

    Task<IngredientCategory> CreateCategoryAsync(IngredientCategory category, CancellationToken cancellationToken = default);
}
