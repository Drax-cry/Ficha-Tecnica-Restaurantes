namespace Ficha_Tecnica.Models;

public enum PriceMovementDirection
{
    Neutral = 0,
    Increase = 1,
    Decrease = 2,
}

public class PriceMovement
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int IngredientId { get; set; }

    public string IngredientName { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public string Currency { get; set; } = "EUR";

    public decimal PreviousPrice { get; set; }

    public decimal NewPrice { get; set; }

    public decimal ChangeAmount { get; set; }

    public decimal? ChangePercentage { get; set; }

    public DateTime EffectiveDate { get; set; }

    public DateTime RecordedAt { get; set; }

    public string? Notes { get; set; }

    public PriceMovementDirection Direction
        => ChangeAmount switch
        {
            > 0 => PriceMovementDirection.Increase,
            < 0 => PriceMovementDirection.Decrease,
            _ => PriceMovementDirection.Neutral,
        };
}
