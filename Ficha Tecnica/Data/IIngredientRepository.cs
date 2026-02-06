using Ficha_Tecnica.Models;

namespace Ficha_Tecnica.Data;

public interface IIngredientRepository
{
    Task<IReadOnlyList<Ingredient>> GetIngredientsAsync(int userId, CancellationToken cancellationToken = default);

    Task<Ingredient> CreateIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken = default);

    Task UpdateIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken = default);

    Task<Ingredient?> GetIngredientByIdAsync(int id, int userId, CancellationToken cancellationToken = default);
}
