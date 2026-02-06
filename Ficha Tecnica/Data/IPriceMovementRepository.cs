using Ficha_Tecnica.Models;

namespace Ficha_Tecnica.Data;

public interface IPriceMovementRepository
{
    Task<IReadOnlyList<PriceMovement>> GetMovementsAsync(
        int userId,
        DateTime? startDate,
        DateTime? endDate,
        int? ingredientId,
        CancellationToken cancellationToken = default);

    Task<PriceMovement> CreateMovementAsync(PriceMovement movement, CancellationToken cancellationToken = default);
}
