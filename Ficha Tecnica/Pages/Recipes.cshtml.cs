using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Ficha_Tecnica.Data;
using Ficha_Tecnica.Extensions;
using Ficha_Tecnica.Models;
using Ficha_Tecnica.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Ficha_Tecnica.Pages;

[Authorize]
public class RecipesModel : PageModel
{
    private static readonly CultureInfo Culture = new("pt-PT");
    private static readonly CultureInfo EnglishCulture = new("en-US");

    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
    };

    private static readonly IReadOnlyDictionary<string, string> IconLibrary = new Dictionary<string, string>
    {
        ["add"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><rect x='3' y='11' width='18' height='2' rx='1' fill='currentColor'/><rect x='11' y='3' width='2' height='18' rx='1' fill='currentColor'/></svg>",
        ["import"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M7 4H17C18.1046 4 19 4.89543 19 6V9H17V6H7V18H17V15H19V18C19 19.1046 18.1046 20 17 20H7C5.89543 20 5 19.1046 5 18V6C5 4.89543 5.89543 4 7 4Z' fill='currentColor'/><path d='M13 11V7H11V11H8L12 15L16 11H13Z' fill='currentColor'/></svg>",
        ["spark"] = "<svg viewBox='0 0 28 28' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M14 3L15.9021 8.8541H22L17.0489 12.5459L18.9511 18.4L14 14.7082L9.04894 18.4L10.9511 12.5459L6 8.8541H12.0979L14 3Z' stroke='currentColor' stroke-width='2' stroke-linejoin='round'/></svg>",
        ["margin-up"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M4 14L10 8L14 12L18 8V16H4V14Z' fill='currentColor'/><path d='M4 4H18V6H4V4Z' fill='currentColor'/></svg>",
        ["timer-lite"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><circle cx='10' cy='11' r='6' stroke='currentColor' stroke-width='1.5'/><path d='M10 8V11L12.5 12.5' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/><path d='M7 2H13' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/></svg>",
        ["analytics-mini"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M4 4V16H16' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/><rect x='6.5' y='9' width='2.5' height='5' rx='1' fill='currentColor'/><rect x='10' y='7' width='2.5' height='7' rx='1' fill='currentColor'/><rect x='13.5' y='5.5' width='2.5' height='8.5' rx='1' fill='currentColor'/></svg>",
        ["search"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><circle cx='11' cy='11' r='6' stroke='currentColor' stroke-width='2'/><path d='M20 20L16 16' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["category"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><rect x='3' y='3' width='7' height='7' rx='2' stroke='currentColor' stroke-width='2'/><rect x='14' y='3' width='7' height='7' rx='2' stroke='currentColor' stroke-width='2'/><rect x='3' y='14' width='7' height='7' rx='2' stroke='currentColor' stroke-width='2'/><rect x='14' y='14' width='7' height='7' rx='2' stroke='currentColor' stroke-width='2'/></svg>",
        ["margin"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M5 19L5 11L10 16L14 12L19 17V19H5Z' fill='currentColor'/><path d='M5 5H19V7H5V5Z' fill='currentColor'/></svg>",
        ["status"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M5 4H19C20.1046 4 21 4.89543 21 6V18C21 19.1046 20.1046 20 19 20H5C3.89543 20 3 19.1046 3 18V6C3 4.89543 3.89543 4 5 4Z' stroke='currentColor' stroke-width='2'/><path d='M7 9H9V11H7V9Z' fill='currentColor'/><path d='M7 13H9V15H7V13Z' fill='currentColor'/><path d='M11 9H13V11H11V9Z' fill='currentColor'/><path d='M15 9H17V11H15V9Z' fill='currentColor'/><path d='M11 13H17V15H11V13Z' fill='currentColor'/></svg>",
        ["reset"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M4 4V10H10' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'/><path d='M20 20V14H14' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'/><path d='M5.63604 18.364C7.73259 20.4606 10.7326 21.3431 13.5563 20.7339C16.3799 20.1247 18.7979 18.0978 19.9256 15.4114C21.0534 12.7249 20.7688 9.65446 19.1455 7.20894C17.5221 4.76343 14.7931 3.24391 11.8459 3.20186C8.89868 3.15982 6.12905 4.60033 4.43502 7.00003' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["timer"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><circle cx='12' cy='13' r='8' stroke='currentColor' stroke-width='2'/><path d='M12 9V13L15 15' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M9 3H15' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["yield"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M4 9H20V19C20 20.1046 19.1046 21 18 21H6C4.89543 21 4 20.1046 4 19V9Z' stroke='currentColor' stroke-width='2'/><path d='M7 9L9 3H15L17 9' stroke='currentColor' stroke-width='2'/></svg>",
        ["complexity"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M5 7H19L17 17H7L5 7Z' stroke='currentColor' stroke-width='2'/><path d='M12 7V3' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M9 13H15' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["featured"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M12 3L13.9021 8.8541H20L15.0489 12.5459L16.9511 18.4L12 14.7082L7.04894 18.4L8.95106 12.5459L4 8.8541H10.0979L12 3Z' stroke='currentColor' stroke-width='2' stroke-linejoin='round'/></svg>",
        ["season"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><circle cx='12' cy='12' r='7' stroke='currentColor' stroke-width='2'/><path d='M12 5V2' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M12 22V19' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M5 12H2' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M22 12H19' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M6.5 6.5L4.5 4.5' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M19.5 19.5L17.5 17.5' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M17.5 6.5L19.5 4.5' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M4.5 19.5L6.5 17.5' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["alert"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M12 3L21 19H3L12 3Z' stroke='currentColor' stroke-width='2' stroke-linejoin='round'/><path d='M12 9V13' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M12 17H12.01' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["ingredient"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M12.53 3.22C12.2371 2.92699 11.7629 2.92699 11.47 3.22L6.22 8.47C5.92701 8.76294 5.92701 9.23706 6.22 9.53L14.47 17.78C14.7629 18.073 15.2371 18.073 15.53 17.78L20.78 12.53C21.073 12.2371 21.073 11.7629 20.78 11.47L12.53 3.22Z' stroke='currentColor' stroke-width='2'/><path d='M5 19L9.5 14.5' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
        ["calendar"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><rect x='4' y='5' width='16' height='15' rx='2' stroke='currentColor' stroke-width='2'/><path d='M8 3V7' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M16 3V7' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M4 11H20' stroke='currentColor' stroke-width='2'/></svg>",
        ["eye"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M2 12C2 12 6 5 12 5C18 5 22 12 22 12C22 12 18 19 12 19C6 19 2 12 2 12Z' stroke='currentColor' stroke-width='2'/><circle cx='12' cy='12' r='3' stroke='currentColor' stroke-width='2'/></svg>",
        ["edit"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M11.586 3.586L16.414 8.414L7.828 17H3V12.172L11.586 3.586Z' stroke='currentColor' stroke-width='1.5' stroke-linejoin='round'/></svg>",
        ["sync"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M5 9C5 6.23858 7.23858 4 10 4C11.3868 4 12.6764 4.56183 13.6377 5.46243L12 7H16V3L14.5623 4.43766C13.2646 3.1785 11.4636 2.4 9.5 2.4C5.91015 2.4 3 5.31015 3 8.9V10H5V9Z' fill='currentColor'/><path d='M15 11C15 13.7614 12.7614 16 10 16C8.61325 16 7.3236 15.4382 6.36233 14.5376L8 13H4V17L5.43767 15.5623C6.73537 16.8215 8.53641 17.6 10.5 17.6C14.0898 17.6 17 14.6899 17 11.1V10H15V11Z' fill='currentColor'/></svg>",
        ["analytics"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M5 3V21H21' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><rect x='8' y='11' width='3' height='6' rx='1' fill='currentColor'/><rect x='13' y='7' width='3' height='10' rx='1' fill='currentColor'/><rect x='18' y='5' width='3' height='12' rx='1' fill='currentColor'/></svg>",
        ["team"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M12 12C14.7614 12 17 9.76142 17 7C17 4.23858 14.7614 2 12 2C9.23858 2 7 4.23858 7 7C7 9.76142 9.23858 12 12 12Z' stroke='currentColor' stroke-width='2'/><path d='M4 20C4 16.6863 6.68629 14 10 14H14C17.3137 14 20 16.6863 20 20V21H4V20Z' stroke='currentColor' stroke-width='2'/></svg>",
        ["chef-hat"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M7 19H17V21H7V19Z' fill='currentColor'/><path d='M5 10V15C5 16.1046 5.89543 17 7 17H17C18.1046 17 19 16.1046 19 15V10' stroke='currentColor' stroke-width='2'/><path d='M7 10C5.34315 10 4 8.65685 4 7C4 5.34315 5.34315 4 7 4C7.45249 4 7.88601 4.10038 8.28005 4.28338C9.03029 2.89763 10.4287 2 12 2C13.5713 2 14.9697 2.89763 15.7199 4.28338C16.114 4.10038 16.5475 4 17 4C18.6569 4 20 5.34315 20 7C20 8.65685 18.6569 10 17 10H7Z' stroke='currentColor' stroke-width='2'/></svg>",
        ["seasonal-leaf"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M4 11C4 7 8 3 12 3C14.7614 3 17 5.23858 17 8C17 12 13 16 9 16C6.23858 16 4 13.7614 4 11Z' stroke='currentColor' stroke-width='1.5'/><path d='M9 8L12 11' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/><path d='M7 10L10 13' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/></svg>",
        ["trend"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M3 13L7.5 8.5L10.5 11.5L15.5 6.5L18 9V4H13' stroke='currentColor' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'/></svg>",
        ["menu-flow"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><rect x='4' y='4' width='12' height='12' rx='2' stroke='currentColor' stroke-width='1.5'/><path d='M7 8H13' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/><path d='M7 11H11' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/></svg>",
        ["shield"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M12 3L5 6V11C5 16.52 8.84 21.74 12 23C15.16 21.74 19 16.52 19 11V6L12 3Z' stroke='currentColor' stroke-width='2' stroke-linejoin='round'/><path d='M9 11L11 13L15 9' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'/></svg>",
        ["document"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M7 3H14L19 8V19C19 20.1046 18.1046 21 17 21H7C5.89543 21 5 20.1046 5 19V5C5 3.89543 5.89543 3 7 3Z' stroke='currentColor' stroke-width='2'/><path d='M13 3V9H19' stroke='currentColor' stroke-width='2'/></svg>",
        ["team"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M12 12C14.7614 12 17 9.76142 17 7C17 4.23858 14.7614 2 12 2C9.23858 2 7 4.23858 7 7C7 9.76142 9.23858 12 12 12Z' stroke='currentColor' stroke-width='2'/><path d='M4 20C4 16.6863 6.68629 14 10 14H14C17.3137 14 20 16.6863 20 20V21H4V20Z' stroke='currentColor' stroke-width='2'/></svg>",
        ["eye-lite"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M2.5 10C2.5 10 5.5 5.5 10 5.5C14.5 5.5 17.5 10 17.5 10C17.5 10 14.5 14.5 10 14.5C5.5 14.5 2.5 10 2.5 10Z' stroke='currentColor' stroke-width='1.5'/><circle cx='10' cy='10' r='2.5' stroke='currentColor' stroke-width='1.5'/></svg>",
        ["spark-lite"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M10 2.5L11.7165 7.6165H17.0711L12.6773 10.8835L14.3938 16L10 12.7329L5.60623 16L7.32274 10.8835L2.92893 7.6165H8.28348L10 2.5Z' stroke='currentColor' stroke-width='1.5' stroke-linejoin='round'/></svg>",
        ["trash"] = "<svg viewBox='0 0 20 20' fill='none' xmlns='http://www.w3.org/2000/svg'><path d='M5 7H15L14 17H6L5 7Z' stroke='currentColor' stroke-width='1.5'/><path d='M8 7V5H12V7' stroke='currentColor' stroke-width='1.5'/><path d='M4 7H16' stroke='currentColor' stroke-width='1.5' stroke-linecap='round'/></svg>",
        ["category-manage"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><rect x='3' y='3' width='18' height='18' rx='4' stroke='currentColor' stroke-width='2'/><path d='M9 7H11V9H9V7Z' fill='currentColor'/><path d='M13 7H15V9H13V7Z' fill='currentColor'/><path d='M9 11H11V13H9V11Z' fill='currentColor'/><path d='M13 11H15V13H13V11Z' fill='currentColor'/><path d='M9 15H11V17H9V15Z' fill='currentColor'/><path d='M13 15H15V17H13V15Z' fill='currentColor'/></svg>",
        ["category-add"] = "<svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'><rect x='3' y='3' width='18' height='18' rx='4' stroke='currentColor' stroke-width='2'/><path d='M12 7V17' stroke='currentColor' stroke-width='2' stroke-linecap='round'/><path d='M7 12H17' stroke='currentColor' stroke-width='2' stroke-linecap='round'/></svg>",
    };

    private readonly IRecipeCategoryRepository _recipeCategoryRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IRecipePdfExporter _recipePdfExporter;
    private readonly IRecipeImageStorage _recipeImageStorage;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RecipesModel> _logger;

    private IReadOnlyDictionary<int, Ingredient> _ingredientLookup = new Dictionary<int, Ingredient>();
    private IReadOnlyDictionary<int, RecipeCategory> _categoryLookup = new Dictionary<int, RecipeCategory>();

    public RecipesModel(
        IRecipeCategoryRepository recipeCategoryRepository,
        IRecipeRepository recipeRepository,
        IIngredientRepository ingredientRepository,
        IRecipePdfExporter recipePdfExporter,
        IRecipeImageStorage recipeImageStorage,
        IHttpClientFactory httpClientFactory,
        ILogger<RecipesModel> logger)
    {
        _recipeCategoryRepository = recipeCategoryRepository ?? throw new ArgumentNullException(nameof(recipeCategoryRepository));
        _recipeRepository = recipeRepository ?? throw new ArgumentNullException(nameof(recipeRepository));
        _ingredientRepository = ingredientRepository ?? throw new ArgumentNullException(nameof(ingredientRepository));
        _recipePdfExporter = recipePdfExporter ?? throw new ArgumentNullException(nameof(recipePdfExporter));
        _recipeImageStorage = recipeImageStorage ?? throw new ArgumentNullException(nameof(recipeImageStorage));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public RecipeSummaryViewModel Summary { get; private set; } = RecipeSummaryViewModel.Empty;

    public IReadOnlyList<RecipeHighlightViewModel> HighlightTags { get; private set; } = Array.Empty<RecipeHighlightViewModel>();

    public IReadOnlyList<OptionViewModel> Categories { get; private set; } = Array.Empty<OptionViewModel>();

    public IReadOnlyList<IngredientOptionViewModel> Ingredients { get; private set; } = Array.Empty<IngredientOptionViewModel>();

    public IReadOnlyList<RecipeCardViewModel> Recipes { get; private set; } = Array.Empty<RecipeCardViewModel>();

    [BindProperty]
    public RecipeFormModel RecipeInput { get; set; } = new();

    [BindProperty]
    public RecipeCategoryFormModel CategoryInput { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? StatusMessageTarget { get; set; }

    [TempData]
    public string? StatusMessageEn { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        await LoadPageAsync(userId.Value, cancellationToken);
        EnsureIngredientInputSlot();

        return Page();
    }

    public async Task<IActionResult> OnPostCreateCategoryAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        await LoadPageAsync(userId.Value, cancellationToken);
        EnsureIngredientInputSlot();

        ModelState.Clear();

        if (!TryValidateModel(CategoryInput, nameof(CategoryInput)))
        {
            _logger.LogWarning("Recipe category creation aborted due to validation errors: {Errors}", ModelState.GetErrorMessages());
            StatusMessage = null;
            StatusMessageTarget = null;
            StatusMessageEn = null;
            return await RenderPageAsync(cancellationToken);
        }

        var category = new RecipeCategory
        {
            UserId = userId.Value,
            Name = CategoryInput.Name!,
            IconKey = "category",
            Description = CategoryInput.Description,
            IsActive = true,
        };

        try
        {
            var created = await _recipeCategoryRepository.CreateCategoryAsync(category, cancellationToken);
            StatusMessage = $"Categoria \"{created.Name}\" adicionada.";
            StatusMessageEn = $"Category \"{created.Name}\" added.";
            StatusMessageTarget = "Category";
            return RedirectToPage();
        }
        catch (DuplicateCategoryException ex)
        {
            _logger.LogWarning(ex, "Duplicate recipe category {CategoryName} for user {UserId}.", CategoryInput.Name, userId);
            ModelState.AddModelError("CategoryInput.Name", "Já existe uma categoria com esse nome.");
            StatusMessage = null;
            StatusMessageTarget = null;
            StatusMessageEn = null;
            return await RenderPageAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create recipe category {CategoryName} for user {UserId}.", CategoryInput.Name, userId);
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar a categoria. Tente novamente.");
            StatusMessage = null;
            StatusMessageTarget = null;
            StatusMessageEn = null;
            return await RenderPageAsync(cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostSaveRecipeAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        await LoadPageAsync(userId.Value, cancellationToken);
        NormalizeIngredientInputsFromForm();
        EnsureIngredientInputSlot();

        LogRecipeSubmission(userId.Value, "received");

        ValidateRecipeInput();
        SuppressCategoryValidationForRecipeSubmission();

        if (!ModelState.IsValid)
        {
            LogModelStateErrors(userId.Value, "initial validation");
            return await RenderPageAsync(cancellationToken);
        }

        if (RecipeInput.CategoryId is null)
        {
            ModelState.AddModelError("RecipeInput.CategoryId", "Selecione uma categoria.");
            return await RenderPageAsync(cancellationToken);
        }

        var ingredientsForRecipe = new List<RecipeIngredient>();
        decimal totalCost = 0m;

        for (var index = 0; index < RecipeInput.Ingredients.Count; index++)
        {
            var ingredientInput = RecipeInput.Ingredients[index];
            if (ingredientInput.IngredientId <= 0)
            {
                continue;
            }

            if (!_ingredientLookup.TryGetValue(ingredientInput.IngredientId, out var ingredient))
            {
                ModelState.AddModelError($"RecipeInput.Ingredients[{index}].IngredientId", "Ingrediente não encontrado.");
                continue;
            }

            if (ingredientInput.Quantity <= 0)
            {
                ModelState.AddModelError($"RecipeInput.Ingredients[{index}].Quantity", "Informe uma quantidade maior que zero.");
                continue;
            }

            var quantity = decimal.Round(ingredientInput.Quantity, 4, MidpointRounding.AwayFromZero);
            var costPerUnit = ingredient.CostPerUnit;
            var lineTotal = decimal.Round(quantity * costPerUnit, 2, MidpointRounding.AwayFromZero);

            ingredientsForRecipe.Add(new RecipeIngredient
            {
                IngredientId = ingredient.Id,
                IngredientName = ingredient.Name,
                Quantity = quantity,
                Unit = ingredient.Unit,
                CostPerUnit = costPerUnit,
                TotalCost = lineTotal,
            });

            totalCost += lineTotal;
        }

        if (!RecipeInput.Yield.HasValue || RecipeInput.Yield.Value <= 0)
        {
            ModelState.AddModelError("RecipeInput.Yield", "Informe o número de porções.");
        }

        var portionCount = RecipeInput.Yield ?? 0;
        var provisionalCost = portionCount > 0
            ? decimal.Round(totalCost / portionCount, 2, MidpointRounding.AwayFromZero)
            : decimal.Round(totalCost, 2, MidpointRounding.AwayFromZero);

        if (!ModelState.IsValid)
        {
            LogModelStateErrors(userId.Value, "post-cost validation");
            UpdateRecipeFormPricing(provisionalCost);
            return await RenderPageAsync(cancellationToken);
        }

        if (ingredientsForRecipe.Count == 0)
        {
            ModelState.AddModelError("RecipeInput.Ingredients", "Adicione ao menos um ingrediente válido.");
            UpdateRecipeFormPricing(0);
            return await RenderPageAsync(cancellationToken);
        }

        var ingredientCost = provisionalCost;
        var sellingPrice = RecipeInput.SuggestedPrice ?? 0m;
        sellingPrice = decimal.Round(sellingPrice, 2, MidpointRounding.AwayFromZero);

        var marginPercentage = CalculateMarginPercentage(ingredientCost, sellingPrice);
        RecipeInput.TargetMargin = marginPercentage;
        var marginFraction = decimal.Round(marginPercentage / 100m, 4, MidpointRounding.AwayFromZero);

        var originalImagePath = RecipeInput.ExistingImagePath;
        var desiredImagePath = RecipeInput.ImagePath;
        var removeImage = RecipeInput.RemoveImage;

        if (RecipeInput.ImageUpload is { Length: > 0 })
        {
            try
            {
                var storedPath = await _recipeImageStorage.SaveImageAsync(RecipeInput.ImageUpload, cancellationToken);

                if (!string.IsNullOrWhiteSpace(originalImagePath)
                    && !string.Equals(originalImagePath, storedPath, StringComparison.Ordinal))
                {
                    await _recipeImageStorage.DeleteImageAsync(originalImagePath, cancellationToken);
                }

                desiredImagePath = storedPath;
                removeImage = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store recipe image for user {UserId}.", userId);
                ModelState.AddModelError("RecipeInput.ImageUpload", "Não foi possível guardar a imagem. Tente novamente.");
                UpdateRecipeFormPricing(provisionalCost);
                return await RenderPageAsync(cancellationToken);
            }
        }
        else if (removeImage)
        {
            if (!string.IsNullOrWhiteSpace(originalImagePath))
            {
                await _recipeImageStorage.DeleteImageAsync(originalImagePath, cancellationToken);
            }

            desiredImagePath = null;
        }
        else if (string.IsNullOrWhiteSpace(desiredImagePath) && !string.IsNullOrWhiteSpace(originalImagePath))
        {
            desiredImagePath = originalImagePath;
        }

        RecipeInput.ImagePath = desiredImagePath;
        RecipeInput.ExistingImagePath = desiredImagePath;
        RecipeInput.RemoveImage = false;

        var normalizedDescription = string.IsNullOrWhiteSpace(RecipeInput.Description)
            ? null
            : RecipeInput.Description.Trim();

        var normalizedChefNotes = string.IsNullOrWhiteSpace(RecipeInput.ChefNotes)
            ? null
            : RecipeInput.ChefNotes.Trim();

        RecipeInput.Description = normalizedDescription;
        RecipeInput.ChefNotes = normalizedChefNotes;

        var recipe = new Recipe
        {
            UserId = userId.Value,
            Name = RecipeInput.Name!,
            CategoryId = RecipeInput.CategoryId.Value,
            Description = normalizedDescription,
            ChefNotes = normalizedChefNotes,
            PreparationTime = RecipeInput.PreparationTime.HasValue
                ? RecipeInput.PreparationTime.Value.ToString(CultureInfo.InvariantCulture)
                : null,
            Yield = RecipeInput.Yield?.ToString(CultureInfo.InvariantCulture),
            TargetMargin = marginFraction,
            IngredientCost = ingredientCost,
            SuggestedPrice = sellingPrice,
            Ingredients = ingredientsForRecipe,
            ImagePath = desiredImagePath,
        };

        try
        {
            if (RecipeInput.Id.HasValue && RecipeInput.Id.Value > 0)
            {
                recipe.Id = RecipeInput.Id.Value;
                await _recipeRepository.UpdateRecipeAsync(recipe, cancellationToken);
                StatusMessage = $"Receita \"{recipe.Name}\" atualizada com sucesso.";
                StatusMessageEn = $"Recipe \"{recipe.Name}\" updated successfully.";
            }
            else
            {
                await _recipeRepository.CreateRecipeAsync(recipe, cancellationToken);
                StatusMessage = $"Receita \"{recipe.Name}\" criada com sucesso.";
                StatusMessageEn = $"Recipe \"{recipe.Name}\" created successfully.";
            }

            StatusMessageTarget = "Recipe";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save recipe {RecipeName} for user {UserId}.", RecipeInput.Name, userId);
            ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar a receita. Tente novamente.");
            StatusMessageEn = null;
            UpdateRecipeFormPricing(ingredientCost);
            return await RenderPageAsync(cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostExportRecipeAsync(int id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Challenge();
        }

        if (id <= 0)
        {
            StatusMessage = "Selecione uma receita válida para exportar.";
            StatusMessageEn = "Select a valid recipe to export.";
            StatusMessageTarget = "Recipe";
            return RedirectToPage();
        }

        try
        {
            var recipe = await _recipeRepository.GetRecipeAsync(userId.Value, id, cancellationToken);
            if (recipe is null)
            {
                _logger.LogWarning("Recipe {RecipeId} not found for user {UserId} during export.", id, userId);
                StatusMessage = "Não encontramos a receita selecionada.";
                StatusMessageEn = "The selected recipe was not found.";
                StatusMessageTarget = "Recipe";
                return RedirectToPage();
            }

            var dishImageBytes = await TryLoadRecipeImageAsync(recipe, cancellationToken);
            var pdfBytes = _recipePdfExporter.Export(recipe, dishImageBytes);
            var fileName = BuildRecipeFileName(recipe.Name);

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export recipe {RecipeId} for user {UserId}.", id, userId);
            StatusMessage = "Não foi possível exportar a ficha técnica. Tente novamente.";
            StatusMessageEn = "We couldn't export the recipe sheet. Try again.";
            StatusMessageTarget = "Recipe";
            return RedirectToPage();
        }
    }

    private async Task<byte[]?> TryLoadRecipeImageAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        if (recipe is null)
        {
            return null;
        }

        var storedPath = string.IsNullOrWhiteSpace(recipe.ImagePath)
            ? null
            : recipe.ImagePath.Trim();

        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        try
        {
            var resolvedPath = await _recipeImageStorage.GetImageUrlAsync(storedPath, cancellationToken);
            var imageSource = string.IsNullOrWhiteSpace(resolvedPath) ? storedPath : resolvedPath;

            if (string.IsNullOrWhiteSpace(imageSource))
            {
                return null;
            }

            if (imageSource.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var commaIndex = imageSource.IndexOf(',', StringComparison.Ordinal);
                if (commaIndex < 0 || commaIndex + 1 >= imageSource.Length)
                {
                    return null;
                }

                var base64Data = imageSource[(commaIndex + 1)..].Trim();
                if (string.IsNullOrWhiteSpace(base64Data))
                {
                    return null;
                }

                try
                {
                    return Convert.FromBase64String(base64Data);
                }
                catch (FormatException ex)
                {
                    _logger.LogWarning(ex, "Failed to decode base64 recipe image for recipe {RecipeId}.", recipe.Id);
                    return null;
                }
            }

            if (Uri.TryCreate(imageSource, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                var httpClient = _httpClientFactory.CreateClient();
                using var response = await httpClient.GetAsync(uri, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Failed to download recipe image from {ImageUrl} for recipe {RecipeId}. Status code: {StatusCode}.",
                        imageSource,
                        recipe.Id,
                        response.StatusCode);
                    return null;
                }

                return await response.Content.ReadAsByteArrayAsync();
            }

            if (System.IO.File.Exists(imageSource))
            {
                return await System.IO.File.ReadAllBytesAsync(imageSource, cancellationToken);
            }

            _logger.LogDebug(
                "Recipe image path {ImagePath} for recipe {RecipeId} could not be resolved to a downloadable resource.",
                imageSource,
                recipe.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error while loading recipe image for recipe {RecipeId}.", recipe.Id);
        }

        return null;
    }

    private void LogRecipeSubmission(int userId, string stage)
    {
        if (!_logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        var snapshot = new
        {
            Stage = stage,
            Recipe = RecipeInput is null
                ? null
                : new
                {
                    RecipeInput.Name,
                    RecipeInput.CategoryId,
                    RecipeInput.Yield,
                    RecipeInput.PreparationTime,
                    RecipeInput.TargetMargin,
                    RecipeInput.Description,
                    RecipeInput.ChefNotes,
                    RecipeInput.ImagePath,
                    RecipeInput.ExistingImagePath,
                    RecipeInput.RemoveImage,
                    ImageUpload = RecipeInput.ImageUpload?.FileName,
                    Ingredients = RecipeInput.Ingredients?
                        .Select((ingredient, index) => new
                        {
                            Index = index,
                            ingredient?.IngredientId,
                            ingredient?.IngredientName,
                            ingredient?.Quantity,
                            ingredient?.Unit,
                            ingredient?.CostPerUnit,
                        })
                        .ToList(),
                },
            RawForm = Request.HasFormContentType
                ? Request.Form.Keys.ToDictionary(key => key, key => Request.Form[key].ToString())
                : null,
        };

        _logger.LogInformation(
            "Recipe submission snapshot for user {UserId}: {@Submission}",
            userId,
            snapshot);
    }

    private void LogModelStateErrors(int userId, string context)
    {
        var detailedErrors = ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? error.Exception?.Message ?? string.Empty
                        : error.ErrorMessage)
                    .ToList());

        _logger.LogWarning(
            "Recipe form validation failed during {Context} for user {UserId}: {@Errors}",
            context,
            userId,
            detailedErrors);
    }

    private void SuppressCategoryValidationForRecipeSubmission()
    {
        if (ModelState.IsValid)
        {
            return;
        }

        var categoryPrefix = $"{nameof(CategoryInput)}.";
        var orphanCategoryKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(RecipeCategoryFormModel.Name),
            nameof(RecipeCategoryFormModel.Description),
        };

        var keysToRemove = ModelState.Keys
            .Where(key => key.Equals(nameof(CategoryInput), StringComparison.Ordinal)
                || key.StartsWith(categoryPrefix, StringComparison.Ordinal)
                || orphanCategoryKeys.Contains(key))
            .ToList();

        if (keysToRemove.Count == 0)
        {
            return;
        }

        foreach (var key in keysToRemove)
        {
            ModelState.Remove(key);
        }
    }
    
    private async Task<IActionResult> RenderPageAsync(CancellationToken cancellationToken)
    {
        if (RecipeInput is not null)
        {
            var previewSource = string.IsNullOrWhiteSpace(RecipeInput.ImagePath)
                ? RecipeInput.ExistingImagePath
                : RecipeInput.ImagePath;

            RecipeInput.ImagePreviewUrl = string.IsNullOrWhiteSpace(previewSource)
                ? null
                : await _recipeImageStorage.GetImageUrlAsync(previewSource, cancellationToken);
        }

        return Page();
    }

    public HtmlString RenderIcon(string key)
    {
        if (IconLibrary.TryGetValue(key, out var svg))
        {
            return new HtmlString(svg);
        }

        return HtmlString.Empty;
    }

    private async Task LoadPageAsync(int userId, CancellationToken cancellationToken)
    {
        await LoadCategoriesAsync(userId, cancellationToken);
        await LoadIngredientsAsync(userId, cancellationToken);
        await LoadRecipesAsync(userId, cancellationToken);
        BuildSummaryAndHighlights();
    }

    private void ValidateRecipeInput()
    {
        if (RecipeInput is null)
        {
            ModelState.AddModelError(nameof(RecipeInput), "Preencha as informações da receita.");
            return;
        }

        RecipeInput.Ingredients ??= new List<RecipeIngredientFormModel>();

        var name = RecipeInput.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("RecipeInput.Name", "Informe o nome da receita.");
        }
        else if (name.Length > 200)
        {
            ModelState.AddModelError("RecipeInput.Name", "O nome pode ter no máximo 200 caracteres.");
        }
        else
        {
            RecipeInput.Name = name;
        }

        if (!RecipeInput.Yield.HasValue)
        {
            ModelState.AddModelError("RecipeInput.Yield", "Informe o número de porções.");
        }
        else if (RecipeInput.Yield.Value < 1 || RecipeInput.Yield.Value > 999)
        {
            ModelState.AddModelError("RecipeInput.Yield", "Informe um número de porções válido.");
        }

        if (RecipeInput.PreparationTime.HasValue)
        {
            if (RecipeInput.PreparationTime.Value < 0 || RecipeInput.PreparationTime.Value > 1440)
            {
                ModelState.AddModelError("RecipeInput.PreparationTime", "Informe um tempo de preparo válido.");
            }
        }

        if (RecipeInput.SuggestedPrice.HasValue)
        {
            var sellingPrice = decimal.Round(RecipeInput.SuggestedPrice.Value, 2, MidpointRounding.AwayFromZero);
            RecipeInput.SuggestedPrice = sellingPrice;

            if (sellingPrice <= 0m)
            {
                ModelState.AddModelError("RecipeInput.SuggestedPrice", "Informe um preço de venda válido.");
            }
        }
        else
        {
            ModelState.AddModelError("RecipeInput.SuggestedPrice", "Informe um preço de venda válido.");
        }

        RecipeInput.Description = string.IsNullOrWhiteSpace(RecipeInput.Description)
            ? null
            : RecipeInput.Description.Trim();

        if (!string.IsNullOrWhiteSpace(RecipeInput.ChefNotes))
        {
            var trimmedNotes = RecipeInput.ChefNotes.Trim();
            if (trimmedNotes.Length > RecipeFormModel.ChefNotesMaxLength)
            {
                ModelState.AddModelError("RecipeInput.ChefNotes", $"As notas do chef podem ter no máximo {RecipeFormModel.ChefNotesMaxLength} caracteres.");
            }
            else
            {
                RecipeInput.ChefNotes = trimmedNotes;
            }
        }
        else
        {
            RecipeInput.ChefNotes = null;
        }

        for (var index = 0; index < RecipeInput.Ingredients.Count; index++)
        {
            var ingredient = RecipeInput.Ingredients[index];
            if (ingredient is null)
            {
                RecipeInput.Ingredients[index] = new RecipeIngredientFormModel();
                continue;
            }

            if (ingredient.Quantity < 0m)
            {
                ModelState.AddModelError($"RecipeInput.Ingredients[{index}].Quantity", "Quantidade inválida.");
            }
        }

        RecipeInput.ImagePath = string.IsNullOrWhiteSpace(RecipeInput.ImagePath)
            ? null
            : RecipeInput.ImagePath.Trim();

        RecipeInput.ExistingImagePath = string.IsNullOrWhiteSpace(RecipeInput.ExistingImagePath)
            ? null
            : RecipeInput.ExistingImagePath.Trim();

        if (RecipeInput.ImageUpload is { Length: > 0 })
        {
            if (!AllowedImageContentTypes.Contains(RecipeInput.ImageUpload.ContentType))
            {
                ModelState.AddModelError("RecipeInput.ImageUpload", "Envie uma imagem nos formatos JPG, PNG, GIF ou WEBP.");
            }
            else if (RecipeInput.ImageUpload.Length > MaxImageSizeBytes)
            {
                ModelState.AddModelError("RecipeInput.ImageUpload", "A imagem deve ter no máximo 5 MB.");
            }
        }
    }

    private async Task LoadCategoriesAsync(int userId, CancellationToken cancellationToken)
    {
        var categories = await _recipeCategoryRepository.GetCategoriesAsync(userId, cancellationToken);
        _categoryLookup = categories.ToDictionary(c => c.Id);
        Categories = categories
            .Select(c => new OptionViewModel(
                c.Name,
                c.Id.ToString(CultureInfo.InvariantCulture),
                string.IsNullOrWhiteSpace(c.IconKey) ? "category" : c.IconKey,
                c.Name))
            .ToList();
    }

    private async Task LoadIngredientsAsync(int userId, CancellationToken cancellationToken)
    {
        var ingredients = await _ingredientRepository.GetIngredientsAsync(userId, cancellationToken);
        _ingredientLookup = ingredients.ToDictionary(i => i.Id);
        Ingredients = ingredients
            .Select(i => new IngredientOptionViewModel(
                i.Id,
                i.Name,
                i.Unit,
                i.CostPerUnit,
                FormatCurrency(i.CostPerUnit)))
            .OrderBy(i => i.Name)
            .ToList();
    }

    private async Task LoadRecipesAsync(int userId, CancellationToken cancellationToken)
    {
        var recipes = await _recipeRepository.GetRecipesAsync(userId, cancellationToken);
        var imageLookup = new Dictionary<string, string?>(StringComparer.Ordinal);

        var pathsToResolve = recipes
            .Select(r => r.ImagePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (pathsToResolve.Count > 0)
        {
            var resolutionTasks = pathsToResolve
                .Select(async path => (Path: path, Url: await _recipeImageStorage.GetImageUrlAsync(path, cancellationToken)))
                .ToArray();

            var resolved = await Task.WhenAll(resolutionTasks);
            foreach (var item in resolved)
            {
                imageLookup[item.Path] = item.Url;
            }
        }

        Recipes = recipes
            .Select(recipe =>
            {
                var categoryIcon = string.IsNullOrWhiteSpace(recipe.CategoryIconKey) ? "chef-hat" : recipe.CategoryIconKey!;
                var accentColor = !string.IsNullOrWhiteSpace(recipe.CategoryColor)
                    ? recipe.CategoryColor!
                    : "rgba(205, 205, 205, 0.55)";

                string? storedImagePath = null;
                string? imagePath = null;
                if (!string.IsNullOrWhiteSpace(recipe.ImagePath))
                {
                    storedImagePath = recipe.ImagePath.Trim();
                    if (imageLookup.TryGetValue(storedImagePath, out var resolvedUrl))
                    {
                        imagePath = resolvedUrl ?? storedImagePath;
                    }
                    else
                    {
                        imagePath = storedImagePath;
                    }
                }

                var topIngredientEntries = recipe.Ingredients
                    .OrderByDescending(i => i.TotalCost)
                    .Take(4)
                    .Select(i => new
                    {
                        Pt = $"{i.Quantity.ToString("0.##", Culture)} {i.Unit} · {i.IngredientName}",
                        En = $"{i.Quantity.ToString("0.##", EnglishCulture)} {i.Unit} · {i.IngredientName}"
                    })
                    .ToList();

                var topIngredients = topIngredientEntries
                    .Select(entry => entry.Pt)
                    .ToList();

                var topIngredientsEn = topIngredientEntries
                    .Select(entry => entry.En)
                    .ToList();

                var contributionValue = recipe.SuggestedPrice - recipe.IngredientCost;
                var complexity = recipe.Ingredients.Count switch
                {
                    >= 8 => "Avançado",
                    >= 5 => "Intermediário",
                    _ => "Fácil",
                };

                var complexityEn = recipe.Ingredients.Count switch
                {
                    >= 8 => "Advanced",
                    >= 5 => "Intermediate",
                    _ => "Easy",
                };

                var margin = recipe.TargetMargin;
                var lastUpdated = recipe.UpdatedAt ?? recipe.CreatedAt;
                var marginBand = margin switch
                {
                    >= 0.65m => "high",
                    >= 0.40m => "medium",
                    _ => "low",
                };

                int? preparationMinutes = null;
                if (int.TryParse(recipe.PreparationTime, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedMinutes))
                {
                    preparationMinutes = parsedMinutes;
                }

                int? yieldQuantity = null;
                if (int.TryParse(recipe.Yield, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedYield))
                {
                    yieldQuantity = parsedYield;
                }

                var ingredientDetails = recipe.Ingredients
                    .Select(i => new RecipeIngredientDetailViewModel(i.IngredientId, i.IngredientName, i.Quantity, i.Unit, i.CostPerUnit))
                    .ToList();

                var targetMarginPercentage = decimal.Round(margin * 100m, 0, MidpointRounding.AwayFromZero);

                var baseKeywords = string.Join(" ", new[]
                {
                    recipe.Name,
                    recipe.CategoryName,
                    recipe.Description,
                    recipe.ChefNotes,
                }.Where(value => !string.IsNullOrWhiteSpace(value)));

                var ingredientKeywords = string.Join(" ", ingredientDetails.Select(detail => detail.IngredientName));

                var searchKeywords = string.Join(
                    " ",
                    new[] { baseKeywords, ingredientKeywords }
                        .Where(value => !string.IsNullOrWhiteSpace(value)));

                var sanitizedDescription = SanitizeRichText(recipe.Description);
                var sanitizedChefNotes = SanitizeRichText(recipe.ChefNotes);

                return new RecipeCardViewModel(
                    recipe.Id,
                    recipe.Name,
                    recipe.CategoryId,
                    recipe.CategoryName ?? "Sem categoria",
                    FormatCurrency(recipe.IngredientCost),
                    recipe.IngredientCost,
                    FormatCurrency(recipe.SuggestedPrice),
                    recipe.SuggestedPrice,
                    FormatPercentage(margin),
                    margin,
                    marginBand,
                    FormatCurrency(contributionValue),
                    contributionValue,
                    FormatPreparationTime(recipe.PreparationTime),
                    preparationMinutes,
                    FormatYield(recipe.Yield),
                    yieldQuantity,
                    complexity,
                    complexityEn,
                    topIngredients,
                    topIngredientsEn,
                    ingredientDetails,
                    targetMarginPercentage,
                    accentColor,
                    categoryIcon,
                    imagePath,
                    storedImagePath,
                    lastUpdated.ToString("dd/MM/yyyy", Culture),
                    lastUpdated,
                    sanitizedDescription,
                    sanitizedChefNotes,
                    searchKeywords,
                    false,
                    margin < 0.35m);
            })
            .ToList();
    }

    private void BuildSummaryAndHighlights()
    {
        if (Recipes.Count == 0)
        {
            var now = DateTime.Now;
            Summary = RecipeSummaryViewModel.Empty with
            {
                LastUpdated = now.ToString("dd 'de' MMMM 'de' yyyy", Culture),
                LastUpdatedEn = now.ToString("MMMM dd, yyyy", EnglishCulture)
            };

            HighlightTags = new List<RecipeHighlightViewModel>
            {
                new("Receitas ativas", "Active recipes", "spark", "Organize fichas técnicas com custos atualizados.", "Organize recipe sheets with up-to-date costs.", "0", "0"),
                new("Custo médio", "Average food cost", "margin-up", "O CMV médio será exibido quando adicionar receitas.", "The average food cost will be displayed once recipes are added.", "€ 0,00", "€ 0.00"),
                new("Margem média", "Average margin", "analytics-mini", "Defina margens alvo para acompanhar rentabilidade.", "Set target margins to track profitability.", "0%", "0%"),
            };

            return;
        }

        var totalRecipes = Recipes.Count;
        var averageCost = Recipes.Average(r => r.FoodCostValue);
        var averageSuggestedPrice = Recipes.Average(r => r.SuggestedPriceValue);
        var averageMargin = Recipes.Average(r => r.MarginValue);
        var totalContribution = Recipes.Sum(r => r.ContributionValue);
        var lastUpdated = Recipes
            .Select(r => DateTime.ParseExact(r.LastUpdated, "dd/MM/yyyy", Culture))
            .OrderByDescending(d => d)
            .First();

        Summary = new RecipeSummaryViewModel
        {
            TotalRecipes = totalRecipes,
            AverageFoodCostValue = averageCost,
            AverageSuggestedPriceValue = averageSuggestedPrice,
            AverageMarginValue = averageMargin,
            MonthlyContributionValue = totalContribution,
            AverageFoodCost = FormatCurrency(averageCost),
            AverageSuggestedPrice = FormatCurrency(averageSuggestedPrice),
            AverageMargin = FormatPercentage(averageMargin),
            MonthlyContribution = FormatCurrency(totalContribution),
            LastUpdated = lastUpdated.ToString("dd 'de' MMMM 'de' yyyy", Culture),
            LastUpdatedEn = lastUpdated.ToString("MMMM dd, yyyy", EnglishCulture)
        };

        HighlightTags = new List<RecipeHighlightViewModel>
        {
            new("Receitas ativas", "Active recipes", "spark", "Número total de fichas técnicas prontas para produção.", "Total number of recipe sheets ready for production.", totalRecipes.ToString(Culture), totalRecipes.ToString(EnglishCulture)),
            new("CMV médio", "Average food cost", "margin-up", "Custo médio dos ingredientes por receita.", "Average ingredient cost per recipe.", FormatCurrency(averageCost), FormatCurrencyEnglishValue(averageCost)),
            new("Margem média", "Average margin", "analytics-mini", "Rentabilidade média considerando preço sugerido.", "Average profitability considering the suggested price.", FormatPercentage(averageMargin), FormatPercentageEnglishValue(averageMargin)),
        };
    }

    private void EnsureIngredientInputSlot()
    {
        RecipeInput ??= new RecipeFormModel();
        RecipeInput.Ingredients ??= new List<RecipeIngredientFormModel>();

        if (RecipeInput.Ingredients.Count == 0)
        {
            RecipeInput.Ingredients.Add(new RecipeIngredientFormModel());
        }

        UpdateRecipeFormPricing(RecipeInput.IngredientCost);
    }

    private void UpdateRecipeFormPricing(decimal ingredientCost)
    {
        RecipeInput!.IngredientCost = ingredientCost;

        if (RecipeInput.SuggestedPrice.HasValue)
        {
            var sellingPrice = decimal.Round(
                Math.Max(RecipeInput.SuggestedPrice.Value, 0m),
                2,
                MidpointRounding.AwayFromZero);

            RecipeInput.SuggestedPrice = sellingPrice;
        }

        RecipeInput.TargetMargin = CalculateMarginPercentage(ingredientCost, RecipeInput.SuggestedPrice);
    }

    private void NormalizeIngredientInputsFromForm()
    {
        if (!Request.HasFormContentType)
        {
            return;
        }

        RecipeInput ??= new RecipeFormModel();

        var parsed = new Dictionary<int, RecipeIngredientFormModel>();
        var invariantCulture = CultureInfo.InvariantCulture;
        var portugueseCulture = CultureInfo.GetCultureInfo("pt-PT");

        foreach (var key in Request.Form.Keys)
        {
            if (!key.StartsWith("RecipeInput.Ingredients[", StringComparison.Ordinal))
            {
                continue;
            }

            var start = "RecipeInput.Ingredients[".Length;
            var end = key.IndexOf(']', start);
            if (end <= start)
            {
                continue;
            }

            if (!int.TryParse(key.AsSpan(start, end - start), NumberStyles.Integer, invariantCulture, out var index))
            {
                continue;
            }

            if (!parsed.TryGetValue(index, out var ingredient))
            {
                ingredient = new RecipeIngredientFormModel();
                parsed[index] = ingredient;
            }

            var property = key.Substring(end + 2);
            var value = Request.Form[key].ToString();

            switch (property)
            {
                case nameof(RecipeIngredientFormModel.IngredientId):
                    if (int.TryParse(value, NumberStyles.Integer, invariantCulture, out var ingredientId))
                    {
                        ingredient.IngredientId = ingredientId;
                    }

                    break;
                case nameof(RecipeIngredientFormModel.IngredientName):
                    ingredient.IngredientName = value;
                    break;
                case nameof(RecipeIngredientFormModel.Quantity):
                    if (decimal.TryParse(value, NumberStyles.Number, invariantCulture, out var quantity) ||
                        decimal.TryParse(value, NumberStyles.Number, portugueseCulture, out quantity))
                    {
                        ingredient.Quantity = quantity;
                    }

                    break;
                case nameof(RecipeIngredientFormModel.Unit):
                    ingredient.Unit = value;
                    break;
                case nameof(RecipeIngredientFormModel.CostPerUnit):
                    if (decimal.TryParse(value, NumberStyles.Number, invariantCulture, out var cost) ||
                        decimal.TryParse(value, NumberStyles.Number, portugueseCulture, out cost))
                    {
                        ingredient.CostPerUnit = cost;
                    }

                    break;
            }
        }

        if (parsed.Count == 0)
        {
            return;
        }

        RecipeInput.Ingredients = parsed
            .OrderBy(entry => entry.Key)
            .Select(entry => entry.Value)
            .ToList();

        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("RecipeInput.Ingredients[", StringComparison.Ordinal)).ToList())
        {
            ModelState.Remove(key);
        }

        ModelState.Remove($"{nameof(RecipeInput)}.{nameof(RecipeFormModel.Ingredients)}");
    }

    private static decimal CalculateMarginPercentage(decimal ingredientCost, decimal? sellingPrice)
    {
        if (!sellingPrice.HasValue || sellingPrice.Value <= 0m)
        {
            return 0m;
        }

        if (ingredientCost <= 0m)
        {
            return 0m;
        }

        var margin = ((sellingPrice.Value - ingredientCost) / ingredientCost) * 100m;
        return decimal.Round(margin, 2, MidpointRounding.AwayFromZero);
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

    private static string BuildRecipeFileName(string? recipeName)
    {
        var normalized = (recipeName ?? string.Empty).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else if (char.IsWhiteSpace(character) || character is '-' or '_')
            {
                if (builder.Length > 0 && builder[^1] != '-')
                {
                    builder.Append('-');
                }
            }
        }

        var sanitized = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "ficha-tecnica";
        }

        if (sanitized.Length > 60)
        {
            sanitized = sanitized[..60].Trim('-');
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return $"{sanitized}-ficha-{timestamp}.pdf";
    }

    public string FormatCurrencyEnglish(decimal value) => FormatCurrencyEnglishValue(value);

    public string FormatPercentageEnglish(decimal value) => FormatPercentageEnglishValue(value);

    public string FormatPreparationTimeEnglish(int? minutes) => FormatPreparationTimeEnglishValue(minutes);

    public string FormatYieldEnglish(int? portions) => FormatYieldEnglishValue(portions);

    public string FormatDateEnglish(DateTime date) => FormatDateEnglishValue(date);

    private static string FormatCurrency(decimal value) => string.Format(Culture, "€ {0:N2}", value);

    private static string FormatCurrencyEnglishValue(decimal value) => string.Format(EnglishCulture, "€ {0:N2}", value);

    private static string FormatPercentage(decimal value) => string.Format(Culture, "{0:N0}%", value * 100m);

    private static string FormatPercentageEnglishValue(decimal value) => string.Format(EnglishCulture, "{0:N0}%", value * 100m);

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

    private static string FormatPreparationTimeEnglishValue(int? minutes)
    {
        if (!minutes.HasValue || minutes.Value <= 0)
        {
            return "Time not provided";
        }

        return minutes.Value == 1 ? "1 minute" : $"{minutes.Value} minutes";
    }

    private static string FormatYieldEnglishValue(int? portions)
    {
        if (!portions.HasValue || portions.Value <= 0)
        {
            return "Servings not provided";
        }

        return portions.Value == 1 ? "1 serving" : $"{portions.Value} servings";
    }

    private static string FormatDateEnglishValue(DateTime date) => date.ToString("MMMM dd, yyyy", EnglishCulture);

    private static readonly Regex HtmlCommentRegex = new("<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex ScriptTagRegex = new(
        "<script\\b[^<]*(?:(?!</script>)<[^<]*)*</script>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex HtmlTagRegex = new(
        "</?([a-z0-9]+)([^>]*)>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AttributeRegex = new(
        "([a-z0-9:-]+)(?:\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s>]+)))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SafeDataImageRegex = new(
        "^data:image/[a-z0-9.+-]+;base64,[a-z0-9+/=\\s]+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p",
        "br",
        "strong",
        "em",
        "ul",
        "ol",
        "li",
        "b",
        "i",
        "u",
        "a",
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
        "blockquote",
        "code",
        "pre",
        "table",
        "thead",
        "tbody",
        "tr",
        "td",
        "th",
        "img",
        "figure",
        "figcaption",
        "div",
        "span",
    };

    private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "br",
        "img",
    };

    private static readonly HashSet<string> AllowedGlobalAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "class",
        "id",
        "title",
        "lang",
        "dir",
    };

    private static readonly Dictionary<string, HashSet<string>> AllowedAttributeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "title", "target", "rel" },
        ["img"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "src", "alt", "title", "width", "height" },
        ["table"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "summary" },
        ["td"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "colspan", "rowspan" },
        ["th"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "colspan", "rowspan", "scope" },
        ["div"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "data-section" },
        ["span"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "data-label" },
    };

    private static readonly HashSet<string> AllowedLinkTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "_self",
        "_blank",
        "_parent",
        "_top",
    };

    private static string SanitizeRichText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = HtmlCommentRegex.Replace(value, string.Empty);
        sanitized = ScriptTagRegex.Replace(sanitized, string.Empty);

        sanitized = HtmlTagRegex.Replace(sanitized, static match =>
        {
            var tagName = match.Groups[1].Value;
            var isClosing = match.Value.AsSpan().StartsWith("</", StringComparison.Ordinal);

            if (!AllowedTags.Contains(tagName))
            {
                return string.Empty;
            }

            var normalizedTagName = tagName.ToLowerInvariant();

            if (isClosing)
            {
                return $"</{normalizedTagName}>";
            }

            var isSelfClosing = match.Value.EndsWith("/>", StringComparison.Ordinal);
            var attributeText = match.Groups[2].Value;
            var sanitizedAttributes = SanitizeTagAttributes(normalizedTagName, attributeText);

            if (isSelfClosing || VoidTags.Contains(normalizedTagName))
            {
                return $"<{normalizedTagName}{sanitizedAttributes} />";
            }

            return $"<{normalizedTagName}{sanitizedAttributes}>";
        });

        return sanitized;
    }

    private static string SanitizeTagAttributes(string tagName, string attributeText)
    {
        if (string.IsNullOrWhiteSpace(attributeText))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var allowedAttributesForTag = AllowedAttributeMap.TryGetValue(tagName, out var specific)
            ? specific
            : null;

        foreach (Match match in AttributeRegex.Matches(attributeText))
        {
            var attributeName = match.Groups[1].Value;

            if (string.IsNullOrWhiteSpace(attributeName))
            {
                continue;
            }

            if (attributeName.StartsWith("on", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isAriaAttribute = attributeName.StartsWith("aria-", StringComparison.OrdinalIgnoreCase);
            var isDataAttribute = attributeName.StartsWith("data-", StringComparison.OrdinalIgnoreCase);

            if (!(AllowedGlobalAttributes.Contains(attributeName)
                || (allowedAttributesForTag != null && allowedAttributesForTag.Contains(attributeName))
                || isAriaAttribute
                || isDataAttribute))
            {
                continue;
            }

            var value = match.Groups[2].Success
                ? match.Groups[2].Value
                : match.Groups[3].Success
                    ? match.Groups[3].Value
                    : match.Groups[4].Success
                        ? match.Groups[4].Value
                        : string.Empty;

            if (!TrySanitizeAttributeValue(tagName, attributeName, value, out var sanitizedValue))
            {
                continue;
            }

            builder.Append(' ').Append(attributeName.ToLowerInvariant());

            if (sanitizedValue is not null)
            {
                builder.Append("=\"").Append(HtmlEncoder.Default.Encode(sanitizedValue)).Append('\"');
            }
        }

        return builder.ToString();
    }

    private static bool TrySanitizeAttributeValue(
        string tagName,
        string attributeName,
        string rawValue,
        out string? sanitizedValue)
    {
        sanitizedValue = null;

        if (rawValue is null)
        {
            return true;
        }

        var value = rawValue.Trim();

        if (value.Length == 0)
        {
            sanitizedValue = string.Empty;
            return true;
        }

        if (attributeName.Equals("href", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsSafeUrl(value, allowData: false))
            {
                return false;
            }

            sanitizedValue = value;
            return true;
        }

        if (attributeName.Equals("src", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsSafeUrl(value, allowData: tagName.Equals("img", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            sanitizedValue = value;
            return true;
        }

        if (attributeName.Equals("target", StringComparison.OrdinalIgnoreCase))
        {
            if (!AllowedLinkTargets.Contains(value))
            {
                return false;
            }

            sanitizedValue = value;
            return true;
        }

        if (attributeName.Equals("rel", StringComparison.OrdinalIgnoreCase))
        {
            sanitizedValue = EnsureRelIsSafe(value);
            return true;
        }

        sanitizedValue = value;
        return true;
    }

    private static string EnsureRelIsSafe(string value)
    {
        var tokens = value
            .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        var uniqueTokens = new HashSet<string>(tokens, StringComparer.OrdinalIgnoreCase)
        {
            "noopener",
            "noreferrer",
        };

        return string.Join(" ", uniqueTokens);
    }

    private static bool IsSafeUrl(string value, bool allowData)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        if (trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.StartsWith("/", StringComparison.Ordinal))
        {
            return true;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absoluteUri))
        {
            if (absoluteUri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                || absoluteUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
                || absoluteUri.Scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (allowData && absoluteUri.Scheme.Equals("data", StringComparison.OrdinalIgnoreCase))
            {
                return IsSafeDataUri(trimmed);
            }

            return false;
        }

        if (allowData && trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return IsSafeDataUri(trimmed);
        }

        if (Uri.TryCreate(trimmed, UriKind.Relative, out _))
        {
            return true;
        }

        return false;
    }

    private static bool IsSafeDataUri(string value)
    {
        return SafeDataImageRegex.IsMatch(value);
    }

    public record RecipeSummaryViewModel
    {
        public int TotalRecipes { get; init; }

        public decimal AverageFoodCostValue { get; init; }

        public decimal AverageSuggestedPriceValue { get; init; }

        public decimal AverageMarginValue { get; init; }

        public decimal MonthlyContributionValue { get; init; }

        public string AverageFoodCost { get; init; } = "€ 0,00";

        public string AverageSuggestedPrice { get; init; } = "€ 0,00";

        public string AverageMargin { get; init; } = "0%";

        public string MonthlyContribution { get; init; } = "€ 0,00";

        public string LastUpdated { get; init; } = string.Empty;

        public string LastUpdatedEn { get; init; } = string.Empty;

        public static RecipeSummaryViewModel Empty { get; } = new();
    }

    public record RecipeHighlightViewModel(string Title, string TitleEn, string IconKey, string Description, string DescriptionEn, string Metric, string MetricEn);

    public record OptionViewModel(string Label, string Value, string IconKey, string? LabelEn = null);

    public record IngredientOptionViewModel(int Id, string Name, string Unit, decimal CostPerUnit, string DisplayCost);

    public sealed record RecipeCardViewModel(
        int Id,
        string Name,
        int CategoryId,
        string Category,
        string FoodCost,
        decimal FoodCostValue,
        string SuggestedPrice,
        decimal SuggestedPriceValue,
        string Margin,
        decimal MarginValue,
        string MarginBand,
        string Contribution,
        decimal ContributionValue,
        string PreparationTime,
        int? PreparationTimeMinutes,
        string Yield,
        int? YieldQuantity,
        string Complexity,
        string ComplexityEn,
        IReadOnlyList<string> TopIngredients,
        IReadOnlyList<string> TopIngredientsEn,
        IReadOnlyList<RecipeIngredientDetailViewModel> Ingredients,
        decimal TargetMarginPercentage,
        string AccentColor,
        string IconKey,
        string? ImagePath,
        string? ImageStoragePath,
        string LastUpdated,
        DateTime LastUpdatedDate,
        string? Description,
        string ChefNotes,
        string SearchKeywords,
        bool IsSeasonal,
        bool RequiresRevision);

    public record RecipeIngredientDetailViewModel(
        int IngredientId,
        string IngredientName,
        decimal Quantity,
        string Unit,
        decimal CostPerUnit);

    public class RecipeFormModel
    {
        public int? Id { get; set; }

        public string? Name { get; set; }

        public int? CategoryId { get; set; }

        public int? Yield { get; set; }

        public int? PreparationTime { get; set; }

        public decimal? TargetMargin { get; set; } = 55m;

        public string? Description { get; set; }

        public const int ChefNotesMaxLength = 2000;

        [Display(Name = "Notas do chef")]
        [StringLength(ChefNotesMaxLength, ErrorMessage = "As notas do chef podem ter no máximo 2000 caracteres.")]
        public string? ChefNotes { get; set; }

        [Display(Name = "Imagem da receita")]
        public IFormFile? ImageUpload { get; set; }

        public string? ImagePath { get; set; }

        public string? ExistingImagePath { get; set; }

        public string? ImagePreviewUrl { get; set; }

        public bool RemoveImage { get; set; }

        public List<RecipeIngredientFormModel> Ingredients { get; set; } = new();

        public decimal IngredientCost { get; set; }

        public decimal? SuggestedPrice { get; set; }

        public bool IsManualPrice { get; set; }
    }

    public class RecipeIngredientFormModel
    {
        public int IngredientId { get; set; }

        public string? IngredientName { get; set; }

        public decimal Quantity { get; set; }

        public string? Unit { get; set; }

        public decimal CostPerUnit { get; set; }
    }

    public class RecipeCategoryFormModel
    {
        [Required(ErrorMessage = "Informe o nome da categoria.")]
        [StringLength(150, ErrorMessage = "O nome pode ter no máximo 150 caracteres.")]
        public string? Name { get; set; }

        [StringLength(200, ErrorMessage = "A descrição pode ter no máximo 200 caracteres.")]
        public string? Description { get; set; }
    }
}
