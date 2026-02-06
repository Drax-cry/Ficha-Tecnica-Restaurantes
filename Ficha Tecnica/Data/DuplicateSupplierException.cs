using System;

namespace Ficha_Tecnica.Data;

public class DuplicateSupplierException : Exception
{
    public DuplicateSupplierException(string supplierName)
        : base($"Um fornecedor com o nome '{supplierName}' já está cadastrado.")
    {
        SupplierName = supplierName;
    }

    public DuplicateSupplierException(string supplierName, Exception innerException)
        : base($"Um fornecedor com o nome '{supplierName}' já está cadastrado.", innerException)
    {
        SupplierName = supplierName;
    }

    public string SupplierName { get; }
}
