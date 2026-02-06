using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ficha_Tecnica.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ficha_Tecnica.Services;

public class RecipePdfExporter : IRecipePdfExporter
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-PT");

    static RecipePdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Export(Recipe recipe, byte[]? dishImageBytes = null)
    {
        if (recipe is null)
        {
            throw new ArgumentNullException(nameof(recipe));
        }

        var ingredients = (recipe.Ingredients ?? Array.Empty<RecipeIngredient>())
            .OrderByDescending(i => i.TotalCost)
            .ToList();

        var hasDishImage = dishImageBytes is { Length: > 0 } && IsSupportedImage(dishImageBytes!);

        var totalIngredientCost = ingredients.Sum(i => i.TotalCost);
        var yieldQuantity = ParseYield(recipe.Yield);
        var costPerPortion = yieldQuantity > 0
            ? SafeDivide(totalIngredientCost, yieldQuantity)
            : recipe.IngredientCost;

        if (costPerPortion <= 0 && recipe.IngredientCost > 0)
        {
            costPerPortion = recipe.IngredientCost;
        }

        var suggestedPrice = recipe.SuggestedPrice;
        var contributionPerPortion = suggestedPrice - costPerPortion;
        var contributionTotal = yieldQuantity > 0 ? contributionPerPortion * yieldQuantity : contributionPerPortion;
        var marginPercent = recipe.TargetMargin * 100m;
        var lastUpdated = recipe.UpdatedAt ?? recipe.CreatedAt;

        var financialMetrics = new (string Label, string Value)[]
        {
            ("Custo por porção", FormatCurrency(costPerPortion)),
            ("Custo total", FormatCurrency(totalIngredientCost)),
            ("Preço sugerido (por porção)", FormatCurrency(suggestedPrice)),
            ("Margem alvo", FormatPercentage(marginPercent)),
            ("Contribuição por porção", FormatCurrency(contributionPerPortion)),
            ("Contribuição total", FormatCurrency(contributionTotal)),
        };

        var operationalMetrics = new (string Label, string Value)[]
        {
            ("Categoria", string.IsNullOrWhiteSpace(recipe.CategoryName) ? "Sem categoria" : recipe.CategoryName!),
            ("Rendimento", FormatYield(recipe.Yield)),
            ("Tempo de preparo", FormatPreparationTime(recipe.PreparationTime)),
            ("Última atualização", lastUpdated.ToString("dd/MM/yyyy HH:mm", Culture)),
        };

        var description = string.IsNullOrWhiteSpace(recipe.Description) ? null : recipe.Description.Trim();
        var chefNotes = string.IsNullOrWhiteSpace(recipe.ChefNotes) ? null : recipe.ChefNotes.Trim();

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
                    row.RelativeColumn().Text("Ficha técnica de prato")
                        .SemiBold().FontSize(12).FontColor("#111827");
                    row.ConstantColumn(80).AlignRight()
                        .Text(DateTime.Now.ToString("dd/MM/yyyy", Culture))
                        .FontSize(10).FontColor("#6B7280");
                });

                page.Content().Column(column =>
                {
                    column.Spacing(16);

                    column.Item().Text(recipe.Name)
                        .FontSize(24)
                        .SemiBold()
                        .FontColor("#111827");

                    column.Item().Text(string.IsNullOrWhiteSpace(recipe.CategoryName) ? "Sem categoria" : recipe.CategoryName)
                        .FontSize(12)
                        .FontColor("#6B7280");

                    if (hasDishImage)
                    {
                        column.Item().Column(imageSection =>
                        {
                            imageSection.Spacing(8);
                            imageSection.Item().Text("Imagem do prato")
                                .SemiBold()
                                .FontSize(13)
                                .FontColor("#111827");
                            imageSection.Item().Element(imageContainer =>
                            {
                                imageContainer
                                    .Background("#F9FAFB")
                                    .Border(1)
                                    .BorderColor("#E5E7EB")
                                    .Padding(10)
                                    .AlignCenter()
                                    .Height(220)
                                    .Image(dishImageBytes!)
                                    .FitArea();
                            });
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        column.Item().Text(description)
                            .FontSize(11)
                            .FontColor("#374151");
                    }

                    if (!string.IsNullOrWhiteSpace(chefNotes))
                    {
                        column.Item().Element(notesContainer =>
                        {
                            notesContainer.Border(1)
                                .BorderColor("#E5E7EB")
                                .Background("#F9FAFB")
                                .Padding(12)
                                .Column(section =>
                                {
                                    section.Spacing(6);
                                    section.Item().Text("Notas do chef")
                                        .SemiBold()
                                        .FontSize(13)
                                        .FontColor("#111827");
                                    section.Item().Text(chefNotes)
                                        .FontSize(11)
                                        .FontColor("#374151");
                                });
                        });
                    }

                    column.Item().Element(container =>
                        BuildMetricSection(container, "Resumo financeiro", financialMetrics));

                    column.Item().Element(container =>
                        BuildMetricSection(container, "Detalhes operacionais", operationalMetrics));

                    column.Item().Element(container =>
                    {
                        container.Border(1).BorderColor("#E5E7EB").Padding(12).Column(section =>
                        {
                            section.Spacing(10);
                            section.Item().Text("Ingredientes cadastrados")
                                .SemiBold().FontSize(13).FontColor("#111827");

                            if (ingredients.Count == 0)
                            {
                                section.Item().Text("Nenhum ingrediente registado para esta receita.")
                                    .FontSize(11)
                                    .FontColor("#6B7280");
                                return;
                            }

                            section.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("Ingrediente");
                                    header.Cell().Element(HeaderCell).Text("Quantidade");
                                    header.Cell().Element(HeaderCell).Text("Custo unitário");
                                    header.Cell().Element(HeaderCell).Text("Custo total");
                                });

                                foreach (var ingredient in ingredients)
                                {
                                    table.Cell().Element(BodyCell).Text(ingredient.IngredientName);
                                    table.Cell().Element(BodyCell).Text(FormatQuantity(ingredient.Quantity, ingredient.Unit));
                                    table.Cell().Element(BodyCell).Text(FormatCurrency(ingredient.CostPerUnit));
                                    table.Cell().Element(BodyCell).Text(FormatCurrency(ingredient.TotalCost));
                                }
                            });
                        });
                    });
                });

                page.Footer().AlignCenter().Text("Documento gerado automaticamente pelo sistema de fichas técnicas.")
                    .FontSize(9)
                    .FontColor("#9CA3AF");
            });
        });

        return document.GeneratePdf();
    }

    private static void BuildMetricSection(IContainer container, string title, IEnumerable<(string Label, string Value)> metrics)
    {
        container.Background("#F9FAFB")
            .Border(1)
            .BorderColor("#E5E7EB")
            .Padding(12)
            .Column(section =>
            {
                section.Spacing(10);
                section.Item().Text(title).SemiBold().FontSize(13).FontColor("#111827");

                section.Item().Grid(grid =>
                {
                    grid.Columns(2);
                    grid.Spacing(12);

                    foreach (var (label, value) in metrics.Where(metric => !string.IsNullOrWhiteSpace(metric.Value)))
                    {
                        grid.Item().Column(item =>
                        {
                            item.Spacing(2);
                            item.Item().Text(label).FontSize(10).FontColor("#6B7280");
                            item.Item().Text(value).FontSize(11).SemiBold().FontColor("#111827");
                        });
                    }
                });
            });
    }

    private static bool IsSupportedImage(byte[] data)
    {
        if (data.Length < 4)
        {
            return false;
        }

        ReadOnlySpan<byte> span = data;

        static bool HasSignature(ReadOnlySpan<byte> source, ReadOnlySpan<byte> signature)
        {
            return source.Length >= signature.Length && source.Slice(0, signature.Length).SequenceEqual(signature);
        }

        static bool HasSuffix(ReadOnlySpan<byte> source, byte[] suffix)
        {
            return source.Length >= suffix.Length
                && source.Slice(source.Length - suffix.Length, suffix.Length).SequenceEqual(suffix);
        }

        return HasSignature(span, new byte[] { 0xFF, 0xD8, 0xFF }) && HasSuffix(span, new byte[] { 0xFF, 0xD9 }) // JPEG
            || HasSignature(span, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) // PNG
            || HasSignature(span, new byte[] { 0x47, 0x49, 0x46, 0x38 }) // GIF87a/GIF89a
            || HasSignature(span, new byte[] { 0x42, 0x4D }); // BMP
    }

    private static string FormatQuantity(decimal quantity, string unit)
    {
        var formatted = quantity % 1m == 0m
            ? quantity.ToString("N0", Culture)
            : quantity.ToString("N2", Culture);

        return string.IsNullOrWhiteSpace(unit)
            ? formatted
            : $"{formatted} {unit}";
    }

    private static decimal SafeDivide(decimal value, int divisor)
    {
        return divisor <= 0 ? 0 : Math.Round(value / divisor, 2, MidpointRounding.AwayFromZero);
    }

    private static int ParseYield(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) && result > 0
            ? result
            : 0;
    }

    private static string FormatCurrency(decimal value) => string.Format(Culture, "€ {0:N2}", value);

    private static string FormatPercentage(decimal value) => string.Format(Culture, "{0:N0}%", value);

    private static string FormatPreparationTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Tempo não informado";
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) && minutes > 0)
        {
            return minutes == 1 ? "1 minuto" : $"{minutes} minutos";
        }

        return value;
    }

    private static string FormatYield(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Porções não informadas";
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var portions) && portions > 0)
        {
            return portions == 1 ? "1 porção" : $"{portions} porções";
        }

        return value;
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background("#F3F4F6").Padding(6).DefaultTextStyle(s => s.SemiBold().FontSize(10).FontColor("#111827"));

    private static IContainer BodyCell(IContainer container) =>
        container.Padding(6).DefaultTextStyle(s => s.FontSize(10).FontColor("#1F2937"));
}
