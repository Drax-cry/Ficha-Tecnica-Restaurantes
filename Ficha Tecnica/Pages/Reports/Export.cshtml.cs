using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Ficha_Tecnica.Data;
using Ficha_Tecnica.Models;
using Ficha_Tecnica.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Ficha_Tecnica.Pages.Reports;

[Authorize]
public class ExportModel : PageModel
{
    private static readonly string[] SupportedPeriods = { "last7", "last30", "quarter", "year" };
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-PT");

    private readonly IRecipeRepository _recipeRepository;
    private readonly IPriceMovementRepository _priceMovementRepository;
    private readonly ILogger<ExportModel> _logger;
    private readonly IDashboardReportPdfExporter _dashboardReportPdfExporter;

    public ExportModel(
        IRecipeRepository recipeRepository,
        IPriceMovementRepository priceMovementRepository,
        ILogger<ExportModel> logger,
        IDashboardReportPdfExporter dashboardReportPdfExporter)
    {
        _recipeRepository = recipeRepository ?? throw new ArgumentNullException(nameof(recipeRepository));
        _priceMovementRepository = priceMovementRepository ?? throw new ArgumentNullException(nameof(priceMovementRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dashboardReportPdfExporter = dashboardReportPdfExporter ?? throw new ArgumentNullException(nameof(dashboardReportPdfExporter));
    }

    public async Task<IActionResult> OnGetAsync(string? period, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Unable to resolve user id while exporting dashboard report.");
            return Challenge();
        }

        var selectedPeriod = ResolveSelectedPeriod(period);
        var startDate = GetPeriodStartDate(selectedPeriod);

        try
        {
            var recipes = await _recipeRepository.GetRecipesAsync(userId.Value, cancellationToken);
            var movements = await _priceMovementRepository.GetMovementsAsync(
                userId.Value,
                startDate,
                endDate: null,
                ingredientId: null,
                cancellationToken: cancellationToken);

            var viewModel = DashboardViewModel.FromData(recipes, movements, selectedPeriod);
            var pdfBytes = _dashboardReportPdfExporter.Export(viewModel, recipes, movements);
            var fileName = $"relatorio-dashboard-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export dashboard report for user {UserId}.", userId.Value);
            TempData["DashboardStatusMessage"] = "Não foi possível gerar o relatório. Tente novamente.";
            return RedirectToPage("/Index");
        }
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
