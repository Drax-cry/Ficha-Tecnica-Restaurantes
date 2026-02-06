namespace Ficha_Tecnica.Models;

public class Recipe
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string? CategoryIconKey { get; set; }

    public string? CategoryColor { get; set; }

    public string? Description { get; set; }

    public string? ChefNotes { get; set; }

    public string? PreparationTime { get; set; }

    public string? Yield { get; set; }

    public decimal TargetMargin { get; set; }

    public decimal IngredientCost { get; set; }

    public decimal SuggestedPrice { get; set; }

    public string? ImagePath { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public IReadOnlyList<RecipeIngredient> Ingredients { get; set; } = Array.Empty<RecipeIngredient>();
}
