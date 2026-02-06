using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ficha_Tecnica.Data;
using Ficha_Tecnica.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

[Authorize]
public class IndexModel : PageModel
{
    private static readonly string[] SupportedPeriods = { "last7", "last30", "quarter", "year" };

    private readonly IRecipeRepository _recipeRepository;
    private readonly IPriceMovementRepository _priceMovementRepository;
    private readonly ILogger<IndexModel> _logger;

    public DashboardViewModel ViewModel { get; private set; } = DashboardViewModel.CreateEmpty();

    public IndexModel(
        IRecipeRepository recipeRepository,
        IPriceMovementRepository priceMovementRepository,
        ILogger<IndexModel> logger)
    {
        _recipeRepository = recipeRepository ?? throw new ArgumentNullException(nameof(recipeRepository));
        _priceMovementRepository = priceMovementRepository ?? throw new ArgumentNullException(nameof(priceMovementRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var selectedPeriod = ResolveSelectedPeriod(Request.Query["period"].ToString());

        var userId = GetUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Unable to resolve user id while loading dashboard data.");
            return Challenge();
        }

        try
        {
            var recipes = await _recipeRepository.GetRecipesAsync(userId.Value, cancellationToken);
            var startDate = GetPeriodStartDate(selectedPeriod);
            var priceMovements = await _priceMovementRepository.GetMovementsAsync(
                userId.Value,
                startDate,
                endDate: null,
                ingredientId: null,
                cancellationToken: cancellationToken);

            ViewModel = DashboardViewModel.FromData(recipes, priceMovements, selectedPeriod);

            _logger.LogInformation(
                "Dashboard data loaded for user {UserId} using period {Period}. Recipes: {RecipeCount}; price movements: {MovementCount}.",
                userId.Value,
                selectedPeriod,
                recipes?.Count ?? 0,
                priceMovements?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dashboard for user {UserId}.", userId.Value);
            ViewModel = DashboardViewModel.CreateEmpty(selectedPeriod);
        }

        return Page();
    }

    private static string ResolveSelectedPeriod(string? requestedPeriod)
    {
        if (!string.IsNullOrWhiteSpace(requestedPeriod) && SupportedPeriods.Contains(requestedPeriod, StringComparer.Ordinal))
        {
            return requestedPeriod;
        }

        return "last30";
    }

    private static DateTime? GetPeriodStartDate(string selectedPeriod)
    {
        var now = DateTime.UtcNow;

        return selectedPeriod switch
        {
            "last7" => now.AddDays(-7),
            "last30" => now.AddDays(-30),
            "quarter" => now.AddMonths(-3),
            "year" => now.AddYears(-1),
            _ => now.AddDays(-30),
        };
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
}

public record DashboardViewModel
{
    public IReadOnlyList<DashboardMetric> Metrics { get; init; } = new List<DashboardMetric>();
    public IReadOnlyList<CategoryPerformance> CategoryPerformances { get; init; } = new List<CategoryPerformance>();
    public IReadOnlyList<DashboardAlert> Alerts { get; init; } = new List<DashboardAlert>();
    public IReadOnlyList<RecipePerformance> TopRecipes { get; init; } = new List<RecipePerformance>();
    public IReadOnlyList<QuickActionLink> QuickActions { get; init; } = new List<QuickActionLink>();
    public string SelectedPeriod { get; init; } = "last30";

    public static DashboardViewModel CreateEmpty(string? selectedPeriod = null) => new()
    {
        SelectedPeriod = string.IsNullOrWhiteSpace(selectedPeriod) ? "last30" : selectedPeriod,
        QuickActions = GetDefaultQuickActions(),
    };

    public static DashboardViewModel FromData(
        IReadOnlyList<Recipe>? recipes,
        IReadOnlyList<PriceMovement>? priceMovements,
        string selectedPeriod)
    {
        var culture = CultureInfo.GetCultureInfo("pt-PT");
        var recipeList = recipes ?? Array.Empty<Recipe>();
        var movementList = priceMovements ?? Array.Empty<PriceMovement>();
        var recipeCount = recipeList.Count;

        var totalCost = recipeList.Sum(recipe => Math.Max(recipe.IngredientCost, 0m));
        var averageCost = recipeCount > 0 ? totalCost / recipeCount : 0m;

        var profitSamples = recipeList.Select(recipe => recipe.SuggestedPrice - recipe.IngredientCost).ToList();
        var averageProfit = profitSamples.Count > 0 ? profitSamples.Average() : 0m;

        var marginSamples = recipeList
            .Where(recipe => recipe.SuggestedPrice > 0m)
            .Select(recipe => (recipe.SuggestedPrice - recipe.IngredientCost) / recipe.SuggestedPrice)
            .ToList();
        var averageMargin = marginSamples.Count > 0 ? marginSamples.Average() : 0m;

        var metrics = new List<DashboardMetric>
        {
            new(
                "Custo total",
                totalCost.ToString("C", culture),
                recipeCount > 0 ? $"Ticket médio de {averageCost.ToString("C", culture)}" : "Sem dados cadastrados",
                TrendDirection.Stable,
                "Somatório do custo de ingredientes das fichas técnicas cadastradas.")
            {
                TitleEn = "Total cost",
                TrendEn = recipeCount > 0 ? $"Average ticket of {averageCost.ToString("C", culture)}" : "No registered data",
                DescriptionEn = "Sum of ingredient costs for all registered recipes."
            },
            /*new(
                "Receita projetada",
                "€ 86.120,40",
                "+3,1% vs. mês anterior",
                TrendDirection.Up,
                "Projeção baseada em reservas confirmadas e vendas recorrentes.")
            {
                TitleEn = "Projected revenue",
                TrendEn = "+3.1% vs. previous month",
                DescriptionEn = "Projection based on confirmed reservations and recurring sales."
            },
            new(
                "Lucro bruto",
                "€ 37.469,65",
                "+2,3% vs. mês anterior",
                TrendDirection.Up,
                "Diferença entre receita projetada e custos totais.")
            {
                TitleEn = "Gross profit",
                TrendEn = "+2.3% vs. previous month",
                DescriptionEn = "Difference between projected revenue and total costs."
            },*/
            new(
                "Margem média",
                averageMargin.ToString("P1", culture),
                recipeCount > 0 ? $"Lucro médio de {averageProfit.ToString("C", culture)}" : "Sem dados cadastrados",
                TrendDirection.Stable,
                "Margem média calculada a partir do preço sugerido e do custo dos ingredientes.")
            {
                TitleEn = "Average margin",
                TrendEn = recipeCount > 0 ? $"Average profit of {averageProfit.ToString("C", culture)}" : "No registered data",
                DescriptionEn = "Average margin calculated from suggested price and ingredient cost."
            },
        };

        var totalSuggested = recipeList.Sum(recipe => Math.Max(recipe.SuggestedPrice, 0m));
        var categoryPerformances = recipeList
            .GroupBy(recipe => string.IsNullOrWhiteSpace(recipe.CategoryName) ? "Sem categoria" : recipe.CategoryName!)
            .Select(group =>
            {
                var suggestedTotal = group.Sum(recipe => Math.Max(recipe.SuggestedPrice, 0m));
                var contribution = totalSuggested > 0m ? suggestedTotal / totalSuggested : 0m;
                var count = group.Count();
                var labelBuilder = new StringBuilder();
                var labelBuilderEn = new StringBuilder();
                var name = string.IsNullOrWhiteSpace(group.Key) ? "Sem categoria" : group.Key;
                var nameEn = string.IsNullOrWhiteSpace(group.Key) ? "Uncategorized" : group.Key;

                labelBuilder.Append(count == 1 ? "1 receita" : $"{count} receitas");
                labelBuilder.Append(" com preço sugerido total de ");
                labelBuilder.Append(suggestedTotal.ToString("C", culture));
                labelBuilder.Append('.');

                labelBuilderEn.Append(count == 1 ? "1 recipe" : $"{count} recipes");
                labelBuilderEn.Append(" with a total suggested price of ");
                labelBuilderEn.Append(suggestedTotal.ToString("C", culture));
                labelBuilderEn.Append('.');

                return new CategoryPerformance(name, contribution, labelBuilder.ToString())
                {
                    NameEn = nameEn,
                    TrendLabelEn = labelBuilderEn.ToString(),
                };
            })
            .OrderByDescending(performance => performance.ContributionPercentage)
            .ThenBy(performance => performance.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var alerts = movementList
            .OrderByDescending(movement => movement.EffectiveDate)
            .ThenByDescending(movement => movement.RecordedAt)
            .Take(5)
            .Select(movement => CreateAlertFromMovement(movement, culture))
            .ToList();

        var topRecipes = recipeList
            .Select(recipe =>
            {
                var profit = recipe.SuggestedPrice - recipe.IngredientCost;
                var margin = recipe.SuggestedPrice > 0m && profit > 0m
                    ? profit / recipe.SuggestedPrice
                    : 0m;
                return new RecipePerformance(recipe.Name, recipe.SuggestedPrice, recipe.IngredientCost, profit, margin);
            })
            .OrderByDescending(recipe => recipe.Profit)
            .ThenBy(recipe => recipe.Name, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        return new DashboardViewModel
        {
            SelectedPeriod = selectedPeriod,
            Metrics = metrics,
            CategoryPerformances = categoryPerformances,
            Alerts = alerts,
            TopRecipes = topRecipes,
            QuickActions = GetDefaultQuickActions(),
        };
    }

    private static DashboardAlert CreateAlertFromMovement(PriceMovement movement, CultureInfo culture)
    {
        var englishCulture = CultureInfo.GetCultureInfo("en-US");
        var previous = Math.Max(movement.PreviousPrice, 0m);
        var changeFraction = previous > 0m
            ? (movement.NewPrice - movement.PreviousPrice) / previous
            : 0m;
        var absoluteChange = Math.Abs(changeFraction);

        var severity = absoluteChange switch
        {
            >= 0.15m => DashboardAlertSeverity.Critical,
            >= 0.05m => DashboardAlertSeverity.Warning,
            _ => DashboardAlertSeverity.Info,
        };

        var directionLabel = changeFraction >= 0m ? "aumento" : "redução";
        var directionLabelEn = changeFraction >= 0m ? "price increase" : "price decrease";
        var percentageLabel = absoluteChange > 0m
            ? absoluteChange.ToString("P1", culture)
            : "sem variação";
        var percentageLabelEn = absoluteChange > 0m
            ? absoluteChange.ToString("P1", englishCulture)
            : "no variation";

        var descriptionBuilder = new StringBuilder();
        descriptionBuilder.Append("Valor alterado de ");
        descriptionBuilder.Append(movement.PreviousPrice.ToString("C", culture));
        descriptionBuilder.Append(" para ");
        descriptionBuilder.Append(movement.NewPrice.ToString("C", culture));
        if (absoluteChange > 0m)
        {
            descriptionBuilder.Append(" (");
            descriptionBuilder.Append(percentageLabel);
            descriptionBuilder.Append(')');
        }
        descriptionBuilder.Append(" em ");
        descriptionBuilder.Append(movement.EffectiveDate.ToString("d", culture));
        descriptionBuilder.Append('.');

        if (!string.IsNullOrWhiteSpace(movement.Notes))
        {
            descriptionBuilder.Append(' ');
            descriptionBuilder.Append(movement.Notes);
        }

        var descriptionBuilderEn = new StringBuilder();
        descriptionBuilderEn.Append("Price changed from ");
        descriptionBuilderEn.Append(movement.PreviousPrice.ToString("C", culture));
        descriptionBuilderEn.Append(" to ");
        descriptionBuilderEn.Append(movement.NewPrice.ToString("C", culture));
        if (absoluteChange > 0m)
        {
            descriptionBuilderEn.Append(" (");
            descriptionBuilderEn.Append(percentageLabelEn);
            descriptionBuilderEn.Append(')');
        }
        descriptionBuilderEn.Append(" on ");
        descriptionBuilderEn.Append(movement.EffectiveDate.ToString("d", englishCulture));
        descriptionBuilderEn.Append('.');

        if (!string.IsNullOrWhiteSpace(movement.Notes))
        {
            descriptionBuilderEn.Append(' ');
            descriptionBuilderEn.Append(movement.Notes);
        }

        var title = $"{movement.IngredientName}: {directionLabel} de preço";
        var titleEn = $"{movement.IngredientName}: {directionLabelEn}";

        return new DashboardAlert(severity, title, descriptionBuilder.ToString())
        {
            TitleEn = titleEn,
            DescriptionEn = descriptionBuilderEn.ToString(),
        };
    }

    private static IReadOnlyList<QuickActionLink> GetDefaultQuickActions() => new List<QuickActionLink>
    {
        new("Cadastrar ingrediente", "/Ingredients", "Adicione novos insumos e fornecedores.")
        {
            TitleEn = "Register ingredient",
            DescriptionEn = "Add new supplies and suppliers.",
        },
        new("Atualizar ficha técnica", "/Recipes", "Revise custos e margens das receitas.")
        {
            TitleEn = "Update recipe card",
            DescriptionEn = "Review recipe costs and margins.",
        },
        //new("Revisar contratos", "/Suppliers", "Negocie reajustes com fornecedores estratégicos."),
        //new("Exportar base", "/Settings/Export", "Faça o backup financeiro em poucos cliques."),
    };
}

public record DashboardMetric(string Title, string Value, string Trend, TrendDirection Direction, string Description)
{
    public string TitleEn { get; init; } = string.Empty;
    public string TrendEn { get; init; } = string.Empty;
    public string DescriptionEn { get; init; } = string.Empty;
}

public record CategoryPerformance(string Name, decimal ContributionPercentage, string TrendLabel)
{
    public string NameEn { get; init; } = string.Empty;
    public string TrendLabelEn { get; init; } = string.Empty;
}

public record DashboardAlert(DashboardAlertSeverity Severity, string Title, string Description)
{
    public string TitleEn { get; init; } = string.Empty;
    public string DescriptionEn { get; init; } = string.Empty;

    public string SeverityLabel => Severity switch
    {
        DashboardAlertSeverity.Critical => "Crítico",
        DashboardAlertSeverity.Warning => "Atenção",
        _ => "Informativo"
    };

    public string SeverityLabelEn => Severity switch
    {
        DashboardAlertSeverity.Critical => "Critical",
        DashboardAlertSeverity.Warning => "Warning",
        _ => "Informational"
    };
}

public record RecipePerformance(string Name, decimal SuggestedPrice, decimal Cost, decimal Profit, decimal Margin);

public record QuickActionLink(string Title, string Url, string Description)
{
    public string TitleEn { get; init; } = string.Empty;
    public string DescriptionEn { get; init; } = string.Empty;
}

public enum TrendDirection
{
    Up,
    Down,
    Stable
}

public enum DashboardAlertSeverity
{
    Info,
    Warning,
    Critical
}
