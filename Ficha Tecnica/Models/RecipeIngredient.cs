namespace Ficha_Tecnica.Models;

public class RecipeIngredient
{
    public int Id { get; set; }

    public int RecipeId { get; set; }

    public int IngredientId { get; set; }

    public string IngredientName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public decimal CostPerUnit { get; set; }

    public decimal TotalCost { get; set; }
}
