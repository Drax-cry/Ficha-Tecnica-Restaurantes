using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Linq;
using Ficha_Tecnica.Data;
using Ficha_Tecnica.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Ficha_Tecnica.Pages;

[Authorize]
public class PriceHistoryModel : PageModel
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-PT");
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");

    private readonly IIngredientRepository _ingredientRepository;
    private readonly IPriceMovementRepository _priceMovementRepository;
    private readonly ILogger<PriceHistoryModel> _logger;

    public PriceHistoryModel(
        IIngredientRepository ingredientRepository,
        IPriceMovementRepository priceMovementRepository,
        ILogger<PriceHistoryModel> logger)
    {
        _ingredientRepository = ingredientRepository ?? throw new ArgumentNullException(nameof(ingredientRepository));
        _priceMovementRepository = priceMovementRepository ?? throw new ArgumentNullException(nameof(priceMovementRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [BindProperty(SupportsGet = true)]
    [Display(Name = "Ingrediente")]
    public int? IngredientId { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    [Display(Name = "Início")]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    [DataType(DataType.Date)]
    [Display(Name = "Fim")]
    public DateTime? EndDate { get; set; }

    [BindProperty]
    public MovementFormModel MovementInput { get; set; } = new();

    public IReadOnlyList<IngredientOption> IngredientOptions { get; private set; } = Array.Empty<IngredientOption>();

    public IReadOnlyList<TranslatedSelectItem> IngredientFilterOptions { get; private set; } = Array.Empty<TranslatedSelectItem>();

    public IReadOnlyList<TranslatedSelectItem> MovementIngredientOptions { get; private set; } = Array.Empty<TranslatedSelectItem>();

    public IReadOnlyList<PriceMovementViewModel> Movements { get; private set; } = Array.Empty<PriceMovementViewModel>();

    public SummaryViewModel Summary { get; private set; } = SummaryViewModel.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? StatusMessageEn { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        await LoadIngredientsAsync(userId.Value, cancellationToken);
        await LoadMovementsAsync(userId.Value, cancellationToken);

        if (IngredientId.HasValue)
        {
            MovementInput.IngredientId = IngredientId;
        }

        if (MovementInput.EffectiveDate == DateTime.MinValue)
        {
            MovementInput.EffectiveDate = DateTime.Today;
        }

        if (!MovementInput.NewPrice.HasValue && IngredientId.HasValue)
        {
            var defaultIngredient = IngredientOptions.FirstOrDefault(option => option.Id == IngredientId.Value);
            MovementInput.NewPrice = defaultIngredient?.CurrentPrice;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddMovementAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        if (!IngredientId.HasValue && MovementInput.IngredientId.HasValue && MovementInput.IngredientId > 0)
        {
            IngredientId = MovementInput.IngredientId;
        }

        await LoadIngredientsAsync(userId.Value, cancellationToken);
        await LoadMovementsAsync(userId.Value, cancellationToken);

        if (!ModelState.IsValid)
        {
            _logger.LogInformation("Price movement validation failed: {Errors}", string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return Page();
        }

        var ingredientId = MovementInput.IngredientId!.Value;

        var ingredient = await _ingredientRepository.GetIngredientByIdAsync(ingredientId, userId.Value, cancellationToken);
        if (ingredient is null)
        {
            ModelState.AddModelError(string.Empty, "Ingrediente selecionado não foi encontrado.");
            return Page();
        }

        var previousPrice = ingredient.CostPerUnit;
        if (MovementInput.EffectiveDate == DateTime.MinValue)
        {
            MovementInput.EffectiveDate = DateTime.Today;
        }

        var newPrice = MovementInput.NewPrice!.Value;
        var changeAmount = newPrice - previousPrice;
        decimal? changePercentage = previousPrice == 0 ? null : Math.Round((changeAmount / previousPrice) * 100m, 2, MidpointRounding.AwayFromZero);

        var movement = new PriceMovement
        {
            UserId = userId.Value,
            IngredientId = ingredient.Id,
            IngredientName = ingredient.Name,
            Unit = ingredient.Unit,
            Currency = ingredient.Currency,
            PreviousPrice = previousPrice,
            NewPrice = newPrice,
            ChangeAmount = changeAmount,
            ChangePercentage = changePercentage,
            EffectiveDate = MovementInput.EffectiveDate,
            Notes = string.IsNullOrWhiteSpace(MovementInput.Notes) ? null : MovementInput.Notes.Trim(),
        };

        await _priceMovementRepository.CreateMovementAsync(movement, cancellationToken);

        ingredient.CostPerUnit = newPrice;
        ingredient.LastPriceUpdate = MovementInput.EffectiveDate == DateTime.MinValue ? DateTime.UtcNow : MovementInput.EffectiveDate;

        var packageQuantity = ingredient.PackageQuantity.GetValueOrDefault();
        ingredient.TotalCost = packageQuantity > 0
            ? decimal.Round(newPrice * packageQuantity, 2, MidpointRounding.AwayFromZero)
            : decimal.Round(newPrice, 2, MidpointRounding.AwayFromZero);

        try
        {
            await _ingredientRepository.UpdateIngredientAsync(ingredient, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ingredient {IngredientId} with new price.", ingredient.Id);
            ModelState.AddModelError(string.Empty, "Não foi possível atualizar o preço do ingrediente. Tente novamente.");
            return Page();
        }

        StatusMessage = $"Preço de {ingredient.Name} atualizado para {newPrice.ToString("C", Culture)}.";
        StatusMessageEn = $"Price for {ingredient.Name} updated to {newPrice.ToString("C", EnglishCulture)}.";

        return RedirectToPage(new
        {
            ingredientId = MovementInput.IngredientId,
            startDate = StartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            endDate = EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        });
    }

    private async Task LoadIngredientsAsync(int userId, CancellationToken cancellationToken)
    {
        var ingredients = await _ingredientRepository.GetIngredientsAsync(userId, cancellationToken);

        IngredientOptions = ingredients
            .OrderBy(ingredient => ingredient.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(ingredient => new IngredientOption(
                ingredient.Id,
                ingredient.Name,
                ingredient.CostPerUnit,
                ingredient.Currency,
                ingredient.Unit))
            .ToList();

        IngredientFilterOptions = BuildSelectList(
            IngredientOptions,
            option => (option.Name, option.Name),
            IngredientId,
            "Todos",
            "All ingredients");

        MovementIngredientOptions = BuildSelectList(
            IngredientOptions,
            option => (
                $"{option.Name} ({option.CurrentPrice.ToString("C", Culture)}/{option.Unit})",
                $"{option.Name} ({option.CurrentPrice.ToString("C", EnglishCulture)}/{option.Unit})"),
            MovementInput.IngredientId,
            "Selecione",
            "Select");
    }

    private async Task LoadMovementsAsync(int userId, CancellationToken cancellationToken)
    {
        var startDate = StartDate?.Date;
        var endDate = EndDate?.Date.AddDays(1).AddTicks(-1);

        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
        {
            ModelState.AddModelError(string.Empty, "O período selecionado é inválido.");
            Movements = Array.Empty<PriceMovementViewModel>();
            Summary = SummaryViewModel.Empty;
            return;
        }

        var movements = await _priceMovementRepository.GetMovementsAsync(userId, startDate, endDate, IngredientId, cancellationToken);

        var viewModels = movements.Select(movement => new PriceMovementViewModel(
            movement.Id,
            movement.IngredientId,
            movement.IngredientName,
            movement.PreviousPrice,
            movement.NewPrice,
            movement.ChangeAmount,
            movement.ChangePercentage,
            movement.Currency,
            movement.Unit,
            movement.EffectiveDate,
            movement.RecordedAt,
            movement.Notes)).ToList();

        Movements = viewModels;
        Summary = SummaryViewModel.FromMovements(viewModels);
    }

    private int? GetUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(claimValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
        {
            return userId;
        }

        return null;
    }

    public class MovementFormModel
    {
        [Display(Name = "Ingrediente")]
        [Required(ErrorMessage = "Selecione um ingrediente.")]
        public int? IngredientId { get; set; }

        [Display(Name = "Novo preço")]
        [Range(0, double.MaxValue, ErrorMessage = "Informe um valor válido para o preço.")]
        public decimal? NewPrice { get; set; }

        [Display(Name = "Data efetiva")]
        [DataType(DataType.Date)]
        public DateTime EffectiveDate { get; set; } = DateTime.Today;

        [Display(Name = "Observações")]
        [StringLength(500, ErrorMessage = "As observações devem ter no máximo 500 caracteres.")]
        public string? Notes { get; set; }
    }

    public sealed record IngredientOption(int Id, string Name, decimal CurrentPrice, string Currency, string Unit);

    private static IReadOnlyList<TranslatedSelectItem> BuildSelectList(
        IReadOnlyList<IngredientOption> options,
        Func<IngredientOption, (string Text, string TextEn)> textSelector,
        int? selectedValue,
        string placeholderPt,
        string placeholderEn)
    {
        var items = new List<TranslatedSelectItem>
        {
            new(
                placeholderPt,
                placeholderEn,
                string.Empty,
                !selectedValue.HasValue)
        };

        foreach (var option in options)
        {
            var (textPt, textEn) = textSelector(option);
            items.Add(new TranslatedSelectItem(
                textPt,
                textEn,
                option.Id.ToString(CultureInfo.InvariantCulture),
                selectedValue.HasValue && option.Id == selectedValue.Value));
        }

        if (!selectedValue.HasValue)
        {
            return items;
        }

        var selected = selectedValue.Value.ToString(CultureInfo.InvariantCulture);
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            items[i] = item with { Selected = item.Value == selected };
        }

        return items;
    }

    public sealed record PriceMovementViewModel(
        int Id,
        int IngredientId,
        string IngredientName,
        decimal PreviousPrice,
        decimal NewPrice,
        decimal ChangeAmount,
        decimal? ChangePercentage,
        string Currency,
        string Unit,
        DateTime EffectiveDate,
        DateTime RecordedAt,
        string? Notes)
    {
        public PriceMovementDirection Direction => ChangeAmount switch
        {
            > 0 => PriceMovementDirection.Increase,
            < 0 => PriceMovementDirection.Decrease,
            _ => PriceMovementDirection.Neutral,
        };

        public string PreviousPriceDisplay => PreviousPrice.ToString("C", Culture);
        public string PreviousPriceDisplayEn => PreviousPrice.ToString("C", EnglishCulture);

        public string NewPriceDisplay => NewPrice.ToString("C", Culture);
        public string NewPriceDisplayEn => NewPrice.ToString("C", EnglishCulture);

        public string ChangeLabel => Direction switch
        {
            PriceMovementDirection.Increase => $"+{ChangeAmount.ToString("C", Culture)}",
            PriceMovementDirection.Decrease => ChangeAmount.ToString("C", Culture),
            _ => ChangeAmount.ToString("C", Culture),
        };

        public string ChangeLabelEn => Direction switch
        {
            PriceMovementDirection.Increase => $"+{ChangeAmount.ToString("C", EnglishCulture)}",
            PriceMovementDirection.Decrease => ChangeAmount.ToString("C", EnglishCulture),
            _ => ChangeAmount.ToString("C", EnglishCulture),
        };

        public string ChangePercentageLabel => ChangePercentage.HasValue
            ? (Direction == PriceMovementDirection.Increase
                ? $"+{ChangePercentage.Value.ToString("F2", Culture)}%"
                : $"{ChangePercentage.Value.ToString("F2", Culture)}%")
            : "—";

        public string ChangePercentageLabelEn => ChangePercentage.HasValue
            ? (Direction == PriceMovementDirection.Increase
                ? $"+{ChangePercentage.Value.ToString("F2", EnglishCulture)}%"
                : $"{ChangePercentage.Value.ToString("F2", EnglishCulture)}%")
            : "—";

        public string EffectiveDateDisplay => EffectiveDate.ToString("d 'de' MMMM 'de' yyyy", Culture);
        public string EffectiveDateDisplayEn => EffectiveDate.ToString("MMMM dd, yyyy", EnglishCulture);

        public string RecordedAtDisplay => RecordedAt.ToLocalTime().ToString("g", Culture);
        public string RecordedAtDisplayEn => RecordedAt.ToLocalTime().ToString("g", EnglishCulture);
    }

    public sealed record SummaryViewModel(
        int TotalMovements,
        decimal TotalIncrease,
        decimal TotalDecrease,
        decimal AverageChange,
        PriceMovementViewModel? LargestIncrease,
        PriceMovementViewModel? LargestDecrease)
    {
        public static SummaryViewModel Empty { get; } = new(0, 0m, 0m, 0m, null, null);

        public static SummaryViewModel FromMovements(IReadOnlyCollection<PriceMovementViewModel> movements)
        {
            if (movements.Count == 0)
            {
                return Empty;
            }

            var increases = movements.Where(m => m.Direction == PriceMovementDirection.Increase).ToList();
            var decreases = movements.Where(m => m.Direction == PriceMovementDirection.Decrease).ToList();

            var totalIncrease = increases.Sum(m => m.ChangeAmount);
            var totalDecrease = decreases.Sum(m => Math.Abs(m.ChangeAmount));
            var averageChange = movements.Average(m => m.ChangeAmount);

            var largestIncrease = increases.OrderByDescending(m => m.ChangeAmount).FirstOrDefault();
            var largestDecrease = decreases.OrderBy(m => m.ChangeAmount).FirstOrDefault();

            return new SummaryViewModel(
                movements.Count,
                totalIncrease,
                totalDecrease,
                averageChange,
                largestIncrease,
                largestDecrease);
        }

        public string TotalIncreaseDisplay => TotalIncrease.ToString("C", Culture);
        public string TotalIncreaseDisplayEn => TotalIncrease.ToString("C", EnglishCulture);

        public string TotalDecreaseDisplay => TotalDecrease.ToString("C", Culture);
        public string TotalDecreaseDisplayEn => TotalDecrease.ToString("C", EnglishCulture);

        public string AverageChangeDisplay => AverageChange.ToString("C", Culture);
        public string AverageChangeDisplayEn => AverageChange.ToString("C", EnglishCulture);
    }

    public sealed record TranslatedSelectItem(string Text, string TextEn, string Value, bool Selected);
}
