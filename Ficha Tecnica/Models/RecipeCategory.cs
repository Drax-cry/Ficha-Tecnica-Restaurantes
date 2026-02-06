namespace Ficha_Tecnica.Models;

public class RecipeCategory
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string IconKey { get; set; } = "category";

    public string? Color { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
