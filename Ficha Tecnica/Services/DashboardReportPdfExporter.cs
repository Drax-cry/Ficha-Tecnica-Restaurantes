using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ficha_Tecnica.Models;
using Ficha_Tecnica.Pages;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ficha_Tecnica.Services;

public class DashboardReportPdfExporter : IDashboardReportPdfExporter
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-PT");

    static DashboardReportPdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Export(DashboardViewModel viewModel, IReadOnlyList<Recipe> recipes, IReadOnlyList<PriceMovement> movements)
    {
        if (viewModel is null)
        {
            throw new ArgumentNullException(nameof(viewModel));
        }

        var recipeList = recipes ?? Array.Empty<Recipe>();
        var movementList = movements ?? Array.Empty<PriceMovement>();
        var topMovements = movementList
            .OrderByDescending(m => m.EffectiveDate)
            .ThenByDescending(m => m.RecordedAt)
            .Take(10)
            .ToList();

        var periodLabel = GetPeriodLabel(viewModel.SelectedPeriod);
        var generatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm", Culture);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontFamily("Helvetica").FontSize(11).FontColor("#1F2937"));

                page.Header().Row(row =>
                {
                    row.RelativeColumn().Column(column =>
                    {
                        column.Item().Text("Relatório do painel de gestão")
                            .FontSize(18)
                            .SemiBold()
                            .FontColor("#111827");
                        column.Item().Text($"Período selecionado: {periodLabel}")
                            .FontSize(11)
                            .FontColor("#4B5563");
                    });

                    row.ConstantColumn(120).AlignRight().Column(column =>
                    {
                        column.Item().Text("Gerado em")
                            .FontSize(10)
                            .FontColor("#6B7280");
                        column.Item().Text(generatedAt)
                            .FontSize(10)
                            .FontColor("#374151");
                    });
                });

                page.Content().Column(column =>
                {
                    column.Spacing(18);

                    column.Item().Text($"{recipeList.Count} fichas técnicas analisadas e {movementList.Count} movimentações de preço avaliadas.")
                        .FontSize(11)
                        .FontColor("#4B5563");

                    column.Item().Element(content => BuildMetricSection(content, viewModel.Metrics));
                    column.Item().Element(content => BuildCategorySection(content, viewModel.CategoryPerformances));
                    column.Item().Element(content => BuildTopRecipesSection(content, viewModel.TopRecipes));
                    column.Item().Element(content => BuildPriceMovementSection(content, topMovements));
                    column.Item().Element(content => BuildAlertSection(content, viewModel.Alerts));
                });

                page.Footer().AlignCenter().Text("Documento gerado automaticamente pelo sistema de fichas técnicas.")
                    .FontSize(9)
                    .FontColor("#9CA3AF");
            });
        });

        return document.GeneratePdf();
    }

    private static void BuildMetricSection(IContainer container, IReadOnlyList<DashboardMetric> metrics)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Indicadores principais")
                .SemiBold()
                .FontSize(13)
                .FontColor("#111827");

            if (metrics is null || metrics.Count == 0)
            {
                column.Item().Text("Nenhum indicador disponível para o período selecionado.")
                    .FontSize(11)
                    .FontColor("#6B7280");
                return;
            }

            column.Item().Grid(grid =>
            {
                grid.Columns(2);
                grid.Spacing(12);

                foreach (var metric in metrics)
                {
                    grid.Item().Border(1).BorderColor("#E5E7EB").Padding(12).Column(card =>
                    {
                        card.Spacing(6);
                        card.Item().Text(metric.Title)
                            .SemiBold()
                            .FontSize(12)
                            .FontColor("#111827");
                        card.Item().Text(metric.Value)
                            .FontSize(16)
                            .SemiBold()
                            .FontColor("#2563EB");
                        card.Item().Text(metric.Trend)
                            .FontSize(10)
                            .FontColor(GetTrendColor(metric.Direction));
                        card.Item().Text(metric.Description)
                            .FontSize(10)
                            .FontColor("#4B5563");
                    });
                }
            });
        });
    }

    private static void BuildCategorySection(IContainer container, IReadOnlyList<CategoryPerformance> categories)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Desempenho por categoria")
                .SemiBold()
                .FontSize(13)
                .FontColor("#111827");

            if (categories is null || categories.Count == 0)
            {
                column.Item().Text("Nenhuma categoria disponível.")
                    .FontSize(11)
                    .FontColor("#6B7280");
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn(3);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Categoria")
                        .SemiBold().FontSize(10).FontColor("#111827");
                    header.Cell().Element(HeaderCell).Text("Participação")
                        .SemiBold().FontSize(10).FontColor("#111827");
                    header.Cell().Element(HeaderCell).Text("Resumo")
                        .SemiBold().FontSize(10).FontColor("#111827");
                });

                foreach (var category in categories)
                {
                    table.Cell().Element(BodyCell).Text(category.Name);
                    table.Cell().Element(BodyCell).Text(category.ContributionPercentage.ToString("P1", Culture));
                    table.Cell().Element(BodyCell).Text(category.TrendLabel);
                }
            });
        });
    }

    private static void BuildTopRecipesSection(IContainer container, IReadOnlyList<RecipePerformance> recipes)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Receitas em destaque")
                .SemiBold()
                .FontSize(13)
                .FontColor("#111827");

            if (recipes is null || recipes.Count == 0)
            {
                column.Item().Text("Nenhuma receita disponível para o período selecionado.")
                    .FontSize(11)
                    .FontColor("#6B7280");
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Receita")
                        .SemiBold().FontSize(10).FontColor("#111827");
                    header.Cell().Element(HeaderCell).Text("Preço sugerido")
                        .SemiBold().FontSize(10).FontColor("#111827");
                    header.Cell().Element(HeaderCell).Text("Custo")
                        .SemiBold().FontSize(10).FontColor("#111827");
                    header.Cell().Element(HeaderCell).Text("Lucro")
                        .SemiBold().FontSize(10).FontColor("#111827");
                    header.Cell().Element(HeaderCell).Text("Margem")
                        .SemiBold().FontSize(10).FontColor("#111827");
                });

                foreach (var recipe in recipes)
                {
                    table.Cell().Element(BodyCell).Text(recipe.Name);
                    table.Cell().Element(BodyCell).Text(recipe.SuggestedPrice.ToString("C", Culture));
                    table.Cell().Element(BodyCell).Text(recipe.Cost.ToString("C", Culture));
                    table.Cell().Element(BodyCell).Text(recipe.Profit.ToString("C", Culture));
                    table.Cell().Element(BodyCell).Text(recipe.Margin.ToString("P1", Culture));
                }
            });
        });
    }

    private static void BuildPriceMovementSection(IContainer container, IReadOnlyList<PriceMovement> movements)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Principais alterações de custo")
                .SemiBold()
                .FontSize(13)
                .FontColor("#111827");

            if (movements is null || movements.Count == 0)
            {
                column.Item().Text("Nenhuma movimentação registrada recentemente.")
                    .FontSize(11)
                    .FontColor("#6B7280");
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Ingrediente")
                        .SemiBold().FontSize(10).FontColor("#111827");
                    header.Cell().Element(HeaderCell).Text("Preço anterior")
                        .SemiBold().FontSize(10).FontColor("#111827");
                    header.Cell().Element(HeaderCell).Text("Novo preço")
                        .SemiBold().FontSize(10).FontColor("#111827");
                    header.Cell().Element(HeaderCell).Text("Variação")
                        .SemiBold().FontSize(10).FontColor("#111827");
                    header.Cell().Element(HeaderCell).Text("Variação %")
                        .SemiBold().FontSize(10).FontColor("#111827");
                    header.Cell().Element(HeaderCell).Text("Vigente em")
                        .SemiBold().FontSize(10).FontColor("#111827");
                });

                foreach (var movement in movements)
                {
                    var changeAmount = movement.ChangeAmount != 0m
                        ? movement.ChangeAmount
                        : movement.NewPrice - movement.PreviousPrice;
                    var changePercentage = movement.ChangePercentage ??
                        (movement.PreviousPrice > 0m ? changeAmount / movement.PreviousPrice : 0m);

                    table.Cell().Element(BodyCell).Text(movement.IngredientName);
                    table.Cell().Element(BodyCell).Text(movement.PreviousPrice.ToString("C", Culture));
                    table.Cell().Element(BodyCell).Text(movement.NewPrice.ToString("C", Culture));
                    table.Cell().Element(BodyCell).Text(changeAmount.ToString("C", Culture));
                    table.Cell().Element(BodyCell).Text(changePercentage.ToString("P1", Culture));
                    table.Cell().Element(BodyCell).Text(movement.EffectiveDate.ToString("d", Culture));
                }
            });
        });
    }

    private static void BuildAlertSection(IContainer container, IReadOnlyList<DashboardAlert> alerts)
    {
        container.Column(column =>
        {
            column.Spacing(10);
            column.Item().Text("Alertas recentes")
                .SemiBold()
                .FontSize(13)
                .FontColor("#111827");

            if (alerts is null || alerts.Count == 0)
            {
                column.Item().Text("Nenhum alerta relevante encontrado para o período.")
                    .FontSize(11)
                    .FontColor("#6B7280");
                return;
            }

            column.Item().Column(list =>
            {
                list.Spacing(6);

                foreach (var alert in alerts)
                {
                    list.Item().Border(1).BorderColor("#FCD34D").Background("#FEF3C7").Padding(10).Column(item =>
                    {
                        item.Item().Text(alert.Title)
                            .SemiBold()
                            .FontSize(11)
                            .FontColor("#92400E");
                        item.Item().Text(alert.Description)
                            .FontSize(10)
                            .FontColor("#B45309");
                    });
                }
            });
        });
    }

    private static IContainer HeaderCell(IContainer container) => container
        .BorderBottom(1)
        .BorderColor("#D1D5DB")
        .PaddingBottom(6);

    private static IContainer BodyCell(IContainer container) => container
        .PaddingVertical(6)
        .BorderBottom(1)
        .BorderColor("#E5E7EB");

    private static string GetTrendColor(TrendDirection direction) => direction switch
    {
        TrendDirection.Up => "#059669",
        TrendDirection.Down => "#DC2626",
        _ => "#6B7280",
    };

    private static string GetPeriodLabel(string selectedPeriod) => selectedPeriod switch
    {
        "last7" => "Últimos 7 dias",
        "last30" => "Últimos 30 dias",
        "quarter" => "Trimestre atual",
        "year" => "Ano atual",
        _ => "Últimos 30 dias",
    };
}
