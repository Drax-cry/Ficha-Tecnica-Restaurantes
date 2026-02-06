using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ficha_Tecnica.Models;

namespace Ficha_Tecnica.Data;

public interface ISupplierRepository
{
    Task<IReadOnlyList<Supplier>> GetSuppliersAsync(int userId, CancellationToken cancellationToken = default);

    Task<Supplier> CreateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task UpdateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default);
}
