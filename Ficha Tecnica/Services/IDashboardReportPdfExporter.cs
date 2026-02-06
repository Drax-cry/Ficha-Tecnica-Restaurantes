using System.Collections.Generic;
using Ficha_Tecnica.Models;
using Ficha_Tecnica.Pages;

namespace Ficha_Tecnica.Services;

public interface IDashboardReportPdfExporter
{
    byte[] Export(DashboardViewModel viewModel, IReadOnlyList<Recipe> recipes, IReadOnlyList<PriceMovement> movements);
}
