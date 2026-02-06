namespace Ficha_Tecnica.Models;

public class Ingredient
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string? CategoryIconKey { get; set; }

    public string Unit { get; set; } = string.Empty;

    public decimal CostPerUnit { get; set; }

    public string Currency { get; set; } = "EUR";

    public decimal? ReorderLevel { get; set; }

    public string? Supplier { get; set; }

    public decimal? PackageQuantity { get; set; }

    public string? PackageSize { get; set; }

    public decimal? TotalCost { get; set; }

    public string? IconKey { get; set; }

    public DateTime? LastPriceUpdate { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
