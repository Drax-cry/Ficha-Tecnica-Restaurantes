using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Ficha_Tecnica.Data;
using Ficha_Tecnica.Models;
using Ficha_Tecnica.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Ficha_Tecnica.Pages;

[Authorize]
public class IngredientsModel : PageModel
{
    private static readonly CultureInfo Culture = new("pt-PT");
    private static readonly CultureInfo EnglishCulture = new("en-US");

    private static readonly IReadOnlyDictionary<string, string> IconLibrary = new Dictionary<string, string>
    {
        ["add"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><rect x='3' y='11' width='18' height='2' rx='1' fill='currentColor'/><rect x='11' y='3' width='2' height='18' rx='1' fill='currentColor'/></svg>",
        ["caret"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M5 8L10 13L15 8' stroke='currentColor' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'/></svg>",
        ["import"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M7 4H17C18.1046 4 19 4.89543 19 6V9H17V6H7V18H17V15H19V18C19 19.1046 18.1046 20 17 20H7C5.89543 20 5 19.1046 5 18V6C5 4.89543 5.89543 4 7 4Z' fill='currentColor'/><path d='M13 11V7H11V11H8L12 15L16 11H13Z' fill='currentColor'/></svg>",
        ["inventory"] = "<svg viewBox='0 0 32 32' fill='none' xmlns='http://www.w3.org/2000/svg'><rect x='4' y='8' width='24' height='18' rx='3' stroke='currentColor' stroke-width='2'/><path d='M10 5H22C23.1046 5 24 5.89543 24 7V8H8V7C8 5.89543 8.89543 5 10 5Z' stroke='currentColor' stroke-width='2'/><path d='M12 15H20' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M12 20H16' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["details"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M5 7H19' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M5 12H14' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M5 17H11' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["ingredient"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M12.53 3.22C12.2371 2.92699 11.7629 2.92699 11.47 3.22L6.22 8.47C5.92701 8.76294 5.92701 9.23706 6.22 9.53L14.47 17.78C14.7629 18.073 15.2371 18.073 15.53 17.78L20.78 12.53C21.073 12.2371 21.073 11.7629 20.78 11.47L12.53 3.22Z' stroke='currentColor' stroke-width='2'/><path d='M5 19L9.5 14.5' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["category"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><rect x='3' y='3' width='7' height='7' rx='2' stroke='currentColor' stroke-width='2'/><rect x='14' y='3' width='7' height='7' rx='2' stroke='currentColor' stroke-width='2'/><rect x='3' y='14' width='7' height='7' rx='2' stroke='currentColor' stroke-width='2'/><rect x='14' y='14' width='7' height='7' rx='2' stroke='currentColor' stroke-width='2'/></svg>",
        ["supplier"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M4 7H20V9H4V7Z' fill='currentColor'/><path d='M5 10H19V17C19 18.1046 18.1046 19 17 19H7C5.89543 19 5 18.1046 5 17V10Z' stroke='currentColor' stroke-width='2'/><path d='M9 13H11V15H9V13Z' fill='currentColor'/><path d='M13 13H15V15H13V13Z' fill='currentColor'/></svg>",
        ["currency"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M7 6.5C7 4.567 8.567 3 10.5 3H13.5C15.433 3 17 4.567 17 6.5C17 8.433 15.433 10 13.5 10H10.5C8.567 10 7 11.567 7 13.5C7 15.433 8.567 17 10.5 17H17' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M12 1V23' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["package"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M4 7L12 3L20 7V17L12 21L4 17V7Z' stroke='currentColor' stroke-width='2'/><path d='M12 12L20 8' stroke='currentColor' stroke-width='2'/><path d='M12 12L4 8' stroke='currentColor' stroke-width='2'/><path d='M12 12V21' stroke='currentColor' stroke-width='2'/></svg>",
        ["notes"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><rect x='4' y='3' width='16' height='18' rx='2' stroke='currentColor' stroke-width='2'/><path d='M8 7H16' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M8 12H16' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M8 17H12' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["measurement"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M6 3H18L20 9H4L6 3Z' stroke='currentColor' stroke-width='2'/><path d='M6 9V20C6 21.1046 6.89543 22 8 22H16C17.1046 22 18 21.1046 18 20V9' stroke='currentColor' stroke-width='2'/><path d='M10 13H14' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["scale"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><circle cx='7' cy='15' r='3' stroke='currentColor' stroke-width='2'/><circle cx='17' cy='15' r='3' stroke='currentColor' stroke-width='2'/><path d='M4 6H20L17 15H7L4 6Z' stroke='currentColor' stroke-width='2'/><path d='M12 6V3' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["drop"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M12 2C12 2 6 9 6 13C6 16.3137 8.68629 19 12 19C15.3137 19 18 16.3137 18 13C18 9 12 2 12 2Z' stroke='currentColor' stroke-width='2'/></svg>",
        ["unit"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><rect x='5' y='5' width='14' height='14' rx='2' stroke='currentColor' stroke-width='2'/><path d='M9 9H15V15H9V9Z' fill='currentColor'/></svg>",
        ["edit"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M11.586 3.586L16.414 8.414L7.828 17H3V12.172L11.586 3.586Z' stroke='currentColor' stroke-width='1.5' stroke-linejoin='round'/></svg>",
        ["duplicate"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><rect x='5' y='5' width='12' height='12' rx='2' stroke='currentColor' stroke-width='1.5'/><rect x='3' y='3' width='12' height='12' rx='2' stroke='currentColor' stroke-width='1.5' opacity='0.4'/></svg>",
        ["price"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M4 10H12C13.6569 10 15 11.3431 15 13C15 14.6569 13.6569 16 12 16H6' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/><path d='M9 4V18' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/></svg>",
        ["sync"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M5 9C5 6.23858 7.23858 4 10 4C11.3868 4 12.6764 4.56183 13.6377 5.46243L12 7H16V3L14.5623 4.43766C13.2646 3.1785 11.4636 2.4 9.5 2.4C5.91015 2.4 3 5.31015 3 8.9V10H5V9Z' fill='currentColor'/><path d='M15 11C15 13.7614 12.7614 16 10 16C8.61325 16 7.3236 15.4382 6.36233 14.5376L8 13H4V17L5.43767 15.5623C6.73537 16.8215 8.53641 17.6 10.5 17.6C14.0898 17.6 17 14.6899 17 11.1V10H15V11Z' fill='currentColor'/></svg>",
        ["link"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M7.5 6C6.11929 6 5 7.11929 5 8.5C5 9.88071 6.11929 11 7.5 11H9' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/><path d='M13 9C14.3807 9 15.5 10.1193 15.5 11.5C15.5 12.8807 14.3807 14 13 14H11' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/><path d='M8 12L12 8' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/></svg>",
        ["save"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M5 4C3.89543 4 3 4.89543 3 6V18C3 19.1046 3.89543 20 5 20H19C20.1046 20 21 19.1046 21 18V8.41421C21 7.88378 20.7893 7.37507 20.4142 7L17 3.58579C16.6249 3.21071 16.1162 3 15.5858 3H5Z' stroke='currentColor' stroke-width='2'/><path d='M7 4V9H15V4H7Z' fill='currentColor'/><path d='M7 13H17V20H7V13Z' stroke='currentColor' stroke-width='2'/></svg>",
        ["reset"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M4 4V10H10' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'/><path d='M20 20V14H14' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'/><path d='M5.63604 18.364C7.73259 20.4606 10.7326 21.3431 13.5563 20.7339C16.3799 20.1247 18.7979 18.0978 19.9256 15.4114C21.0534 12.7249 20.7688 9.65446 19.1455 7.20894C17.5221 4.76343 14.7931 3.24391 11.8459 3.20186C8.89868 3.15982 6.12905 4.60033 4.43502 7.00003' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["search"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><circle cx='11' cy='11' r='6' stroke='currentColor' stroke-width='2'/><path d='M20 20L16 16' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["grain"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M12 3C12 3 8 6.68629 8 10C8 13.3137 12 21 12 21C12 21 16 13.3137 16 10C16 6.68629 12 3 12 3Z' stroke='currentColor' stroke-width='2'/><path d='M12 10L9 7' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M12 12L15 9' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["dairy"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M8 3H16L17.5 7H6.5L8 3Z' stroke='currentColor' stroke-width='2'/><path d='M6.5 7H17.5V18C17.5 19.1046 16.6046 20 15.5 20H8.5C7.39543 20 6.5 19.1046 6.5 18V7Z' stroke='currentColor' stroke-width='2'/><path d='M10 11H14' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["produce"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M12 4C10.7614 4 9.75 5.01143 9.75 6.25C9.75 7.48857 10.7614 8.5 12 8.5C13.2386 8.5 14.25 7.48857 14.25 6.25C14.25 5.01143 13.2386 4 12 4Z' stroke='currentColor' stroke-width='2'/><path d='M6 13C6 9.68629 8.68629 7 12 7C15.3137 7 18 9.68629 18 13V18C18 19.6569 16.6569 21 15 21H9C7.34315 21 6 19.6569 6 18V13Z' stroke='currentColor' stroke-width='2'/></svg>",
        ["seafood"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M4 12C4 12 7 5 12 5C17 5 20 12 20 12C20 12 17 19 12 19C7 19 4 12 4 12Z' stroke='currentColor' stroke-width='2'/><path d='M9.5 12C9.5 13.3807 8.38071 14.5 7 14.5C5.61929 14.5 4.5 13.3807 4.5 12C4.5 10.6193 5.61929 9.5 7 9.5C8.38071 9.5 9.5 10.6193 9.5 12Z' stroke='currentColor' stroke-width='2'/></svg>",
        ["editors-choice"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M12 3L13.9021 8.8541H20L15.0489 12.5459L16.9511 18.4L12 14.7082L7.04894 18.4L8.95106 12.5459L4 8.8541H10.0979L12 3Z' stroke='currentColor' stroke-width='2' stroke-linejoin='round'/></svg>",
        ["price-tag"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M4 4H13L20 11L11 20L4 13V4Z' stroke='currentColor' stroke-width='2'/><circle cx='9' cy='9' r='1.5' fill='currentColor'/></svg>",
        ["stock"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M4 4H20V20H4V4Z' stroke='currentColor' stroke-width='2'/><path d='M8 16L10.5 12.5L13 15L16 11' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'/></svg>",
        ["unit-qty"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M7 6H17V10H7V6Z' stroke='currentColor' stroke-width='2'/><path d='M7 14H17V18H7V14Z' stroke='currentColor' stroke-width='2'/></svg>",
        ["link-external"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M11 4H16V9' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/><path d='M9 11L16 4' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/><path d='M8 6H5C4.44772 6 4 6.44772 4 7V15C4 15.5523 4.44772 16 5 16H13C13.5523 16 14 15.5523 14 15V12' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/></svg>",
        ["default"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><circle cx='12' cy='12' r='9' stroke='currentColor' stroke-width='2'/></svg>",
    };

    private readonly ICategoryRepository _categoryRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly ILogger<IngredientsModel> _logger;

    public IReadOnlyList<IngredientViewModel> Ingredients { get; private set; } = Array.Empty<IngredientViewModel>();

    public IReadOnlyList<OptionViewModel> CategoryIconOptions { get; }

    public IReadOnlyList<SupplierViewModel> Suppliers { get; private set; } = Array.Empty<SupplierViewModel>();

    public IReadOnlyList<OptionViewModel> SupplierOptions { get; private set; } = Array.Empty<OptionViewModel>();

    public IReadOnlyList<MeasurementOptionViewModel> MeasurementUnits { get; }

    public IReadOnlyList<OptionViewModel> Categories { get; private set; } = Array.Empty<OptionViewModel>();

    [BindProperty]
    public CategoryFormModel CategoryInput { get; set; } = new();

    [BindProperty]
    public IngredientFormModel IngredientInput { get; set; } = new();

    [BindProperty]
    public SupplierFormModel SupplierInput { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? StatusMessageEn { get; set; }

    [TempData]
    public string? StatusMessageTarget { get; set; }

    public IngredientsModel(ICategoryRepository categoryRepository, IIngredientRepository ingredientRepository, ISupplierRepository supplierRepository, ILogger<IngredientsModel> logger)
    {
        _categoryRepository = categoryRepository;
        _ingredientRepository = ingredientRepository;
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        CategoryIconOptions = new List<OptionViewModel>
        {
            new("Grãos e cereais", "grain", "grain", "Grains & cereals"),
            new("Laticínios", "dairy", "dairy", "Dairy"),
            new("Hortifruti", "produce", "produce", "Produce"),
            new("Proteínas", "seafood", "seafood", "Proteins"),
            new("Mercearia", "package", "package", "Pantry"),
            new("Etiqueta de preço", "price-tag", "price-tag", "Price tag"),
            new("Curadoria", "editors-choice", "editors-choice", "Curated"),
            new("Estoque", "stock", "stock", "Stock"),
            new("Unidades", "unit-qty", "unit-qty", "Units"),
        };

        MeasurementUnits = new List<MeasurementOptionViewModel>
        {
            new("Quilogramas", "kg", "scale", "Ideal para sacarias e proteínas.", "Kilograms", "Ideal for bulk proteins and sacks."),
            new("Gramas", "g", "unit", "Pesagens de menor volume.", "Grams", "For smaller weight measurements."),
            new("Litros", "l", "drop", "Líquidos e caldos em volume.", "Liters", "Liquids and broths measured by volume."),
            new("Unidades", "un", "unit-qty", "Itens contados individualmente.", "Units", "Items counted individually."),
        };
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadSuppliersAsync(cancellationToken);
        await LoadCategoriesAsync(cancellationToken);
        await LoadIngredientsAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(IngredientInput.Unit) && MeasurementUnits.Count > 0)
        {
            IngredientInput.Unit = MeasurementUnits[0].Value;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateCategoryAsync(CancellationToken cancellationToken)
    {
        await LoadSuppliersAsync(cancellationToken);
        await LoadCategoriesAsync(cancellationToken);
        await LoadIngredientsAsync(cancellationToken);

        ModelState.Clear();

        if (!TryValidateModel(CategoryInput, nameof(CategoryInput)))
        {
            _logger.LogWarning("Category creation aborted due to validation errors: {Errors}", ModelState.GetErrorMessages());
            StatusMessage = null;
            StatusMessageEn = null;
            StatusMessageTarget = null;
            return Page();
        }

        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        var category = new IngredientCategory
        {
            UserId = userId.Value,
            Name = CategoryInput.Name!,
            IsActive = true,
        };

        if (!string.IsNullOrWhiteSpace(CategoryInput.IconKey))
        {
            category.IconKey = CategoryInput.IconKey!;
        }

        try
        {
            var created = await _categoryRepository.CreateCategoryAsync(category, cancellationToken);
            StatusMessage = $"Categoria \"{created.Name}\" salva com sucesso.";
            StatusMessageEn = $"Category \"{created.Name}\" saved successfully.";
            StatusMessageTarget = "Category";
            return RedirectToPage();
        }
        catch (DuplicateCategoryException ex)
        {
            _logger.LogWarning(ex, "Duplicate category detected while creating category {CategoryName} for user {UserId}.", CategoryInput.Name, userId);
            ModelState.AddModelError("CategoryInput.Name", "Já existe uma categoria com esse nome.");
            StatusMessage = null;
            StatusMessageEn = null;
            StatusMessageTarget = null;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create category {CategoryName} for user {UserId}.", CategoryInput.Name, userId);
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar a categoria. Tente novamente.");
            StatusMessage = null;
            StatusMessageEn = null;
            StatusMessageTarget = null;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveIngredientAsync(CancellationToken cancellationToken)
    {
        await LoadSuppliersAsync(cancellationToken);
        await LoadCategoriesAsync(cancellationToken);
        await LoadIngredientsAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(IngredientInput.Unit) && MeasurementUnits.Count > 0)
        {
            IngredientInput.Unit = MeasurementUnits[0].Value;
        }

        ModelState.Clear();

        if (!TryValidateModel(IngredientInput, nameof(IngredientInput)))
        {
            _logger.LogWarning("Ingredient creation aborted due to validation errors: {Errors}", ModelState.GetErrorMessages());
            StatusMessage = null;
            StatusMessageEn = null;
            StatusMessageTarget = null;
            return Page();
        }

        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        var totalCost = decimal.Round(IngredientInput.TotalCost!.Value, 2, MidpointRounding.AwayFromZero);
        var packageQuantity = IngredientInput.PackageQuantity!.Value;
        var costPerUnit = packageQuantity == 0
            ? 0
            : decimal.Round(totalCost / packageQuantity, 2, MidpointRounding.AwayFromZero);

        var ingredient = new Ingredient
        {
            Id = IngredientInput.Id ?? 0,
            UserId = userId.Value,
            Name = IngredientInput.Name!,
            CategoryId = IngredientInput.CategoryId,
            Supplier = IngredientInput.Supplier,
            Unit = IngredientInput.Unit!,
            PackageQuantity = packageQuantity,
            TotalCost = totalCost,
            CostPerUnit = costPerUnit,
            Currency = "EUR",
            Notes = IngredientInput.Notes,
            LastPriceUpdate = DateTime.UtcNow,
            IsActive = true,
        };

        try
        {
            if (IngredientInput.Id.HasValue)
            {
                await _ingredientRepository.UpdateIngredientAsync(ingredient, cancellationToken);
                _logger.LogInformation("Ingredient {IngredientId} updated for user {UserId}.", ingredient.Id, userId);
                StatusMessage = $"Ingrediente \"{ingredient.Name}\" atualizado com sucesso.";
                StatusMessageEn = $"Ingredient \"{ingredient.Name}\" updated successfully.";
            }
            else
            {
                await _ingredientRepository.CreateIngredientAsync(ingredient, cancellationToken);
                _logger.LogInformation("Ingredient {IngredientName} created for user {UserId}.", ingredient.Name, userId);
                StatusMessage = $"Ingrediente \"{ingredient.Name}\" salvo com sucesso.";
                StatusMessageEn = $"Ingredient \"{ingredient.Name}\" saved successfully.";
            }
        }
        catch (DuplicateIngredientException ex)
        {
            _logger.LogWarning(ex, "Duplicate ingredient detected while saving ingredient {IngredientName} for user {UserId}.", IngredientInput.Name, userId);
            ModelState.AddModelError("IngredientInput.Name", "Já existe um ingrediente com esse nome.");
            StatusMessage = null;
            StatusMessageEn = null;
            StatusMessageTarget = null;
            return Page();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Attempted to update ingredient {IngredientId} that does not exist for user {UserId}.", ingredient.Id, userId);
            ModelState.AddModelError(string.Empty, "O ingrediente selecionado não foi encontrado. Atualize a página e tente novamente.");
            StatusMessage = null;
            StatusMessageEn = null;
            StatusMessageTarget = null;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save ingredient {IngredientName} for user {UserId}.", IngredientInput.Name, userId);
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar o ingrediente. Tente novamente.");
            StatusMessage = null;
            StatusMessageEn = null;
            StatusMessageTarget = null;
            return Page();
        }

        StatusMessageTarget = "Ingredient";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveSupplierAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return new JsonResult(new { success = false, error = "Sessão expirada. Faça login novamente." })
            {
                StatusCode = StatusCodes.Status401Unauthorized,
            };
        }

        ModelState.Clear();

        if (!TryValidateModel(SupplierInput, nameof(SupplierInput)))
        {
            var errorMessages = ModelState.GetErrorMessages();
            var message = errorMessages.Count > 0
                ? string.Join(" ", errorMessages)
                : "Os dados informados são inválidos.";

            return new JsonResult(new { success = false, error = message })
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
        }

        var supplier = new Supplier
        {
            Id = SupplierInput.Id.GetValueOrDefault(),
            UserId = userId.Value,
            Name = SupplierInput.Name!,
            Notes = SupplierInput.Notes,
            IsActive = true,
            IsPreferred = false,
        };

        try
        {
            if (SupplierInput.Id.HasValue)
            {
                await _supplierRepository.UpdateSupplierAsync(supplier, cancellationToken);
                _logger.LogInformation("Supplier {SupplierId} updated for user {UserId}.", supplier.Id, userId.Value);
            }
            else
            {
                supplier = await _supplierRepository.CreateSupplierAsync(supplier, cancellationToken);
                _logger.LogInformation("Supplier {SupplierName} created for user {UserId}.", supplier.Name, userId.Value);
            }
        }
        catch (DuplicateSupplierException ex)
        {
            _logger.LogWarning(ex, "Duplicate supplier detected while saving supplier {SupplierName} for user {UserId}.", SupplierInput.Name, userId.Value);
            return new JsonResult(new { success = false, error = "Já existe um fornecedor com esse nome." })
            {
                StatusCode = StatusCodes.Status409Conflict,
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Attempted to update supplier {SupplierId} that does not exist for user {UserId}.", supplier.Id, userId.Value);
            return new JsonResult(new { success = false, error = "O fornecedor selecionado não foi encontrado." })
            {
                StatusCode = StatusCodes.Status404NotFound,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save supplier {SupplierName} for user {UserId}.", SupplierInput.Name, userId.Value);
            return new JsonResult(new { success = false, error = "Não foi possível salvar o fornecedor. Tente novamente." })
            {
                StatusCode = StatusCodes.Status500InternalServerError,
            };
        }

        await LoadSuppliersAsync(cancellationToken);
        await LoadIngredientsAsync(cancellationToken);

        return new JsonResult(new
        {
            success = true,
            supplier = new
            {
                id = supplier.Id,
                name = supplier.Name,
                notes = FormatSupplierNotes(supplier.Notes),
            },
        });
    }

    public HtmlString RenderIcon(string key)
    {
        return new HtmlString(IconLibrary.TryGetValue(key, out var svg) ? svg : IconLibrary["default"]);
    }

    private async Task LoadIngredientsAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            Ingredients = Array.Empty<IngredientViewModel>();
            Suppliers = Array.Empty<SupplierViewModel>();
            SupplierOptions = Array.Empty<OptionViewModel>();
            return;
        }

        var ingredients = await _ingredientRepository.GetIngredientsAsync(userId.Value, cancellationToken);

        if (ingredients.Count == 0)
        {
            Ingredients = Array.Empty<IngredientViewModel>();
            return;
        }

        var comparer = StringComparer.Create(Culture, ignoreCase: true);

        var activeIngredients = ingredients
            .Where(ingredient => ingredient.IsActive)
            .ToList();

        if (activeIngredients.Count == 0)
        {
            Ingredients = Array.Empty<IngredientViewModel>();
            return;
        }

        Ingredients = activeIngredients
            .OrderBy(ingredient => ingredient.Name, comparer)
            .Select(CreateViewModel)
            .ToList();

        MergeSupplierOptions(activeIngredients);
    }

    private async Task LoadSuppliersAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            Suppliers = Array.Empty<SupplierViewModel>();
            SupplierOptions = Array.Empty<OptionViewModel>();
            return;
        }

        var suppliers = await _supplierRepository.GetSuppliersAsync(userId.Value, cancellationToken);
        var comparer = StringComparer.Create(Culture, ignoreCase: true);

        var activeSuppliers = suppliers
            .Where(supplier => supplier.IsActive)
            .OrderBy(supplier => supplier.Name, comparer)
            .ToList();

        Suppliers = activeSuppliers
            .Select(supplier =>
            {
                var notes = FormatSupplierNotes(supplier.Notes);
                return new SupplierViewModel(supplier.Id, supplier.Name, notes.Pt, notes.En, hasCustomDescription: !notes.IsDefault);
            })
            .ToList();

        SupplierOptions = Suppliers
            .Select(supplier => new OptionViewModel(supplier.Name, supplier.Name, "supplier", supplier.Name))
            .ToList();
    }

    private void MergeSupplierOptions(IEnumerable<Ingredient> ingredients)
    {
        if (ingredients is null)
        {
            return;
        }

        var comparer = StringComparer.Create(Culture, ignoreCase: true);
        var options = SupplierOptions?.ToList() ?? new List<OptionViewModel>();
        var knownSuppliers = new HashSet<string>(options.Select(option => option.Value), comparer);
        var added = false;

        foreach (var supplierName in ingredients
                     .Select(ingredient => ingredient.Supplier)
                     .Where(name => !string.IsNullOrWhiteSpace(name))
                     .Distinct(comparer))
        {
            if (supplierName is null)
            {
                continue;
            }

            if (knownSuppliers.Add(supplierName))
            {
                options.Add(new OptionViewModel(supplierName, supplierName, "supplier", supplierName));
                added = true;
            }
        }

        if (added)
        {
            options = options
                .OrderBy(option => option.Label, comparer)
                .ToList();
        }

        SupplierOptions = options;
    }

    private static SupplierNotes FormatSupplierNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return new SupplierNotes("Informações não registradas.", "Information not provided.", IsDefault: true);
        }

        var normalized = notes.Trim();
        return new SupplierNotes(normalized, normalized, IsDefault: false);
    }

    private async Task LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            Categories = Array.Empty<OptionViewModel>();
            return;
        }

        var categories = await _categoryRepository.GetCategoriesAsync(userId.Value, cancellationToken);
        Categories = categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder ?? int.MaxValue)
            .ThenBy(c => c.Name, StringComparer.Create(Culture, ignoreCase: true))
            .Select(c => new OptionViewModel(c.Name, c.Id.ToString(CultureInfo.InvariantCulture), c.IconKey, c.Name))
            .ToList();
    }

    private IngredientViewModel CreateViewModel(Ingredient ingredient)
    {
        var measurement = FindMeasurementUnit(ingredient.Unit);
        var measurementLabelPt = measurement?.Label ?? ingredient.Unit;
        var measurementLabelEn = measurement?.LabelEn ?? measurementLabelPt;
        var measurementLabel = new LocalizedText(measurementLabelPt, measurementLabelEn);

        var packageQuantity = ingredient.PackageQuantity.HasValue && ingredient.PackageQuantity.Value > 0
            ? ingredient.PackageQuantity.Value
            : 1m;

        var totalCost = ingredient.TotalCost ?? (ingredient.CostPerUnit * packageQuantity);
        var packageSize = FormatPackageSize(ingredient, measurementLabel, packageQuantity);
        var lastUpdate = FormatLastUpdate(ingredient);
        var categoryLabel = DetermineCategoryLabel(ingredient);
        var iconKey = DetermineIconKey(ingredient);

        return new IngredientViewModel(
            ingredient.Id,
            ingredient.Name,
            ingredient.CategoryId,
            categoryLabel,
            ingredient.Supplier,
            ingredient.Unit,
            measurementLabel,
            packageQuantity,
            totalCost,
            packageSize,
            lastUpdate,
            iconKey,
            ingredient.Notes
        );
    }

    private LocalizedText DetermineCategoryLabel(Ingredient ingredient)
    {
        if (!string.IsNullOrWhiteSpace(ingredient.CategoryName))
        {
            var label = ingredient.CategoryName!.Trim();
            return new LocalizedText(label, label);
        }

        if (ingredient.CategoryId.HasValue)
        {
            foreach (var category in Categories)
            {
                if (int.TryParse(category.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id == ingredient.CategoryId.Value)
                {
                    var english = string.IsNullOrWhiteSpace(category.LabelEn) ? category.Label : category.LabelEn!;
                    return new LocalizedText(category.Label, english);
                }
            }
        }

        return new LocalizedText("Sem categoria", "No category");
    }

    private string DetermineIconKey(Ingredient ingredient)
    {
        if (!string.IsNullOrWhiteSpace(ingredient.IconKey))
        {
            return ingredient.IconKey!;
        }

        if (!string.IsNullOrWhiteSpace(ingredient.CategoryIconKey))
        {
            return ingredient.CategoryIconKey!;
        }

        if (ingredient.CategoryId.HasValue)
        {
            foreach (var category in Categories)
            {
                if (int.TryParse(category.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id == ingredient.CategoryId.Value)
                {
                    return category.IconKey;
                }
            }
        }

        return "default";
    }

    private LocalizedText FormatPackageSize(Ingredient ingredient, LocalizedText measurementLabel, decimal packageQuantity)
    {
        if (!string.IsNullOrWhiteSpace(ingredient.PackageSize))
        {
            var value = ingredient.PackageSize!.Trim();
            return new LocalizedText(value, value);
        }

        if (ingredient.PackageQuantity.HasValue && ingredient.PackageQuantity.Value > 0)
        {
            var quantity = packageQuantity.ToString("0.##", Culture);
            return new LocalizedText(
                $"{quantity} {measurementLabel.Pt}",
                $"{quantity} {measurementLabel.En}");
        }

        return measurementLabel;
    }

    private LocalizedText FormatLastUpdate(Ingredient ingredient)
    {
        var reference = ingredient.LastPriceUpdate ?? ingredient.UpdatedAt ?? ingredient.CreatedAt;
        var referenceUtc = reference.Kind switch
        {
            DateTimeKind.Utc => reference,
            DateTimeKind.Local => reference.ToUniversalTime(),
            _ => DateTime.SpecifyKind(reference, DateTimeKind.Utc),
        };

        var now = DateTime.UtcNow;
        var difference = now - referenceUtc;

        if (difference.TotalDays < 1)
        {
            return new LocalizedText(" hoje", " today");
        }

        if (difference.TotalDays < 2)
        {
            return new LocalizedText(" ontem", " yesterday");
        }

        if (difference.TotalDays < 7)
        {
            var days = Math.Max(1, (int)Math.Round(difference.TotalDays, MidpointRounding.AwayFromZero));
            var portuguese = $" há {days} dia{(days > 1 ? "s" : string.Empty)}";
            var english = days == 1 ? " 1 day ago" : $" {days} days ago";
            return new LocalizedText(portuguese, english);
        }

        var referenceLocal = referenceUtc.ToLocalTime();
        var formatPt = referenceLocal.Year == DateTime.Today.Year ? "d 'de' MMMM" : "d 'de' MMMM 'de' yyyy";
        var formatEn = referenceLocal.Year == DateTime.Today.Year ? "MMMM d" : "MMMM d, yyyy";
        return new LocalizedText(
            $" em {referenceLocal.ToString(formatPt, Culture)}",
            $" on {referenceLocal.ToString(formatEn, EnglishCulture)}");
    }

    private MeasurementOptionViewModel? FindMeasurementUnit(string value)
    {
        return MeasurementUnits.FirstOrDefault(unit => string.Equals(unit.Value, value, StringComparison.OrdinalIgnoreCase));
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

    public record OptionViewModel(string Label, string Value, string IconKey, string? LabelEn = null);

    public record MeasurementOptionViewModel(
        string Label,
        string Value,
        string IconKey,
        string Description,
        string? LabelEn = null,
        string? DescriptionEn = null);

    public record LocalizedText(string Pt, string En);

    private record SupplierNotes(string Pt, string En, bool IsDefault);

    public class SupplierViewModel
    {
        public SupplierViewModel(int id, string name, string description, string descriptionEn, bool hasCustomDescription)
        {
            Id = id;
            Name = name;
            Description = description;
            DescriptionEn = string.IsNullOrWhiteSpace(descriptionEn) ? description : descriptionEn;
            HasCustomDescription = hasCustomDescription;
        }

        public int Id { get; }

        public string Name { get; }

        public string Description { get; }

        public string DescriptionEn { get; }

        public bool HasCustomDescription { get; }
    }

    public class IngredientViewModel
    {
        public IngredientViewModel(
            int id,
            string name,
            int? categoryId,
            LocalizedText category,
            string? supplier,
            string unit,
            LocalizedText measurementLabel,
            decimal packageQuantity,
            decimal totalCost,
            LocalizedText packageSize,
            LocalizedText lastUpdate,
            string iconKey,
            string? notes)
        {
            Id = id;
            Name = name;
            CategoryId = categoryId;
            Category = category.Pt;
            CategoryEn = category.En;
            Supplier = supplier;
            SupplierLabel = string.IsNullOrWhiteSpace(supplier) ? "Fornecedor não informado" : supplier!;
            SupplierLabelEn = string.IsNullOrWhiteSpace(supplier) ? "Supplier not provided" : supplier!;
            Unit = unit;
            MeasurementLabel = measurementLabel.Pt;
            MeasurementLabelEn = measurementLabel.En;
            PackageQuantity = packageQuantity;
            TotalCost = totalCost;
            PackageSize = packageSize.Pt;
            PackageSizeEn = packageSize.En;
            LastUpdate = lastUpdate.Pt;
            LastUpdateEn = lastUpdate.En;
            IconKey = iconKey;
            Notes = notes;

            PackageQuantityDisplay = $"{PackageQuantity.ToString("0.##", Culture)} {MeasurementLabel}";
            PackageQuantityDisplayEn = $"{PackageQuantity.ToString("0.##", Culture)} {MeasurementLabelEn}";
        }

        public int Id { get; }

        public int? CategoryId { get; }

        public string Name { get; }

        public string Category { get; }

        public string CategoryEn { get; }

        public string? Supplier { get; }

        public string SupplierLabel { get; }

        public string SupplierLabelEn { get; }

        public string Unit { get; }

        public string MeasurementLabel { get; }

        public string MeasurementLabelEn { get; }

        public decimal PackageQuantity { get; }

        public decimal TotalCost { get; }

        public string PackageSize { get; }

        public string PackageSizeEn { get; }

        public string LastUpdate { get; }

        public string LastUpdateEn { get; }

        public string IconKey { get; }

        public string? Notes { get; }

        public string PackageQuantityDisplay { get; }

        public string PackageQuantityDisplayEn { get; }

        public decimal CostPerUnit => PackageQuantity == 0 ? 0 : TotalCost / PackageQuantity;

        public string TotalCostDisplay => TotalCost.ToString("C", Culture);

        public string CostPerUnitDisplay => CostPerUnit.ToString("C", Culture);
    }

    public class CategoryFormModel
    {
        private string? _name;
        private string? _iconKey;

        [Required(ErrorMessage = "Informe o nome da categoria.")]
        [StringLength(150, ErrorMessage = "O nome pode ter no máximo 150 caracteres.")]
        public string? Name
        {
            get => _name;
            set => _name = value?.Trim();
        }

        [StringLength(50, ErrorMessage = "O identificador do ícone é inválido.")]
        public string? IconKey
        {
            get => _iconKey;
            set => _iconKey = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    public class SupplierFormModel
    {
        public int? Id { get; set; }

        private string? _name;
        private string? _notes;

        [Required(ErrorMessage = "Informe o nome do fornecedor.")]
        [StringLength(150, ErrorMessage = "O nome pode ter no máximo 150 caracteres.")]
        public string? Name
        {
            get => _name;
            set => _name = value?.Trim();
        }

        [StringLength(2000, ErrorMessage = "As observações podem ter no máximo 2000 caracteres.")]
        public string? Notes
        {
            get => _notes;
            set => _notes = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    public class IngredientFormModel
    {
        public int? Id { get; set; }

        private string? _name;
        private string? _supplier;
        private string? _unit;
        private string? _notes;

        [Required(ErrorMessage = "Informe o nome do ingrediente.")]
        [StringLength(150, ErrorMessage = "O nome pode ter no máximo 150 caracteres.")]
        public string? Name
        {
            get => _name;
            set => _name = value?.Trim();
        }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione uma categoria válida.")]
        public int? CategoryId { get; set; }

        [StringLength(150, ErrorMessage = "O nome do fornecedor pode ter no máximo 150 caracteres.")]
        public string? Supplier
        {
            get => _supplier;
            set => _supplier = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        [Required(ErrorMessage = "Informe o valor total do ingrediente.")]
        [Range(typeof(decimal), "0.01", "99999999", ErrorMessage = "Informe um valor maior que zero.")]
        public decimal? TotalCost { get; set; }

        [Required(ErrorMessage = "Selecione a unidade de medida.")]
        [StringLength(50, ErrorMessage = "A unidade selecionada é inválida.")]
        public string? Unit
        {
            get => _unit;
            set => _unit = value?.Trim();
        }

        [Required(ErrorMessage = "Informe a quantidade adquirida.")]
        [Range(typeof(decimal), "0.01", "99999999", ErrorMessage = "Informe uma quantidade maior que zero.")]
        public decimal? PackageQuantity { get; set; }

        [StringLength(2000, ErrorMessage = "As observações podem ter no máximo 2000 caracteres.")]
        public string? Notes
        {
            get => _notes;
            set => _notes = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
