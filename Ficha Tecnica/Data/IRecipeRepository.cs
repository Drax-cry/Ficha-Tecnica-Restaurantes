using Ficha_Tecnica.Models;

namespace Ficha_Tecnica.Data;

public interface IRecipeRepository
{
    Task<IReadOnlyList<Recipe>> GetRecipesAsync(int userId, CancellationToken cancellationToken = default);

    Task<Recipe?> GetRecipeAsync(int userId, int recipeId, CancellationToken cancellationToken = default);

    Task<Recipe> CreateRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task<Recipe> UpdateRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default);
}
