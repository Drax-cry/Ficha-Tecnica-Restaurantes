(() => {
    const ingredientDataElement = document.getElementById('recipe-ingredient-data');
    if (!ingredientDataElement) {
        return;
    }

    const ingredients = JSON.parse(ingredientDataElement.textContent || '[]');
    const ingredientMap = new Map(ingredients.map((item) => [item.id, item]));

    const getCurrentLanguage = () => document.documentElement.getAttribute('data-language') === 'en' ? 'en' : 'pt';

    const subscribeToLanguageChanges = (callback) => {
        if (typeof callback !== 'function') {
            return;
        }

        callback(getCurrentLanguage());

        document.addEventListener('app:languagechange', (event) => {
            const lang = event?.detail?.language === 'en' ? 'en' : 'pt';
            callback(lang);
        });
    };

    const formatTemplate = (template, replacements = {}) => {
        if (!template) {
            return '';
        }

        return template.replace(/\{(\w+)\}/g, (_, key) => {
            const value = replacements[key];
            return value == null ? '' : String(value);
        });
    };

    const setTextContent = (element, value) => {
        if (!element || value == null) {
            return;
        }

        element.textContent = value;
    };

    const getLocale = (lang) => (lang === 'en' ? 'en-US' : 'pt-PT');

    const capitalize = (value) => {
        if (!value) {
            return '';
        }

        return value.charAt(0).toUpperCase() + value.slice(1);
    };

    const getModeDatasetValue = (element, prefix, mode, lang) => {
        if (!element) {
            return '';
        }

        const dataset = element.dataset || {};
        const modeKey = capitalize(mode);
        const datasetKey = `${prefix}${modeKey}`;
        const englishKey = `${datasetKey}En`;

        if (lang === 'en' && dataset[englishKey]) {
            return dataset[englishKey];
        }

        if (dataset[datasetKey]) {
            return dataset[datasetKey];
        }

        return element.textContent?.trim() || '';
    };

    const translations = {
        ingredientPlaceholder: {
            pt: 'Nenhum ingrediente adicionado ainda.',
            en: 'No ingredients added yet.'
        },
        ingredientMeta: {
            pt: '{quantity} {unit} • custo por unidade {cost}',
            en: '{quantity} {unit} • unit cost {cost}'
        },
        removeIngredient: {
            pt: 'Remover {name}',
            en: 'Remove {name}'
        },
        selectIngredient: {
            pt: 'Selecione um ingrediente cadastrado.',
            en: 'Select a registered ingredient.'
        },
        quantityGreaterThanZero: {
            pt: 'Informe uma quantidade maior que zero.',
            en: 'Enter an amount greater than zero.'
        },
        dialogRecipe: {
            pt: 'Receita',
            en: 'Recipe'
        },
        dialogImageAlt: {
            pt: 'Imagem da receita {name}',
            en: 'Recipe image {name}'
        },
        dialogMetrics: {
            cost: { pt: 'Custo', en: 'Cost' },
            suggestedPrice: { pt: 'Preço de venda', en: 'Selling price' },
            margin: { pt: 'Margem', en: 'Margin' },
            contribution: { pt: 'Contribuição', en: 'Contribution' },
            preparationTime: { pt: 'Tempo de preparo', en: 'Preparation time' },
            yield: { pt: 'Porções', en: 'Servings' }
        },
        dialogNoIngredients: {
            pt: 'Nenhum ingrediente cadastrado.',
            en: 'No ingredients registered.'
        },
        consoleLoadError: {
            pt: 'Não foi possível carregar a receita selecionada.',
            en: 'Unable to load the selected recipe.'
        },
        consoleParseError: {
            pt: 'Não foi possível interpretar os detalhes da receita.',
            en: 'Unable to parse recipe details.'
        }
    };

    const VALIDATION_MESSAGES = [
        { pt: 'Selecione uma categoria.', en: 'Select a category.' },
        { pt: 'Ingrediente não encontrado.', en: 'Ingredient not found.' },
        { pt: 'Informe uma quantidade maior que zero.', en: 'Enter an amount greater than zero.' },
        { pt: 'Informe o número de porções.', en: 'Enter the number of servings.' },
        { pt: 'Adicione ao menos um ingrediente válido.', en: 'Add at least one valid ingredient.' },
        { pt: 'Informe uma margem válida.', en: 'Enter a valid margin.' },
        { pt: 'Informe um preço de venda válido.', en: 'Enter a valid selling price.' },
        { pt: 'Não foi possível guardar a imagem. Tente novamente.', en: 'Unable to save the image. Try again.' },
        { pt: 'Ocorreu um erro ao salvar a receita. Tente novamente.', en: 'An error occurred while saving the recipe. Try again.' },
        { pt: 'Preencha as informações da receita.', en: 'Fill in the recipe information.' },
        { pt: 'Informe o nome da receita.', en: 'Enter the recipe name.' },
        { pt: 'O nome pode ter no máximo 200 caracteres.', en: 'The name can be up to 200 characters long.' },
        { pt: 'Informe um número de porções válido.', en: 'Enter a valid number of servings.' },
        { pt: 'Informe um tempo de preparo válido.', en: 'Enter a valid preparation time.' },
        { pt: 'As notas do chef podem ter no máximo 2000 caracteres.', en: 'Chef notes can be up to 2000 characters long.' },
        { pt: 'Quantidade inválida.', en: 'Invalid quantity.' },
        { pt: 'Envie uma imagem nos formatos JPG, PNG, GIF ou WEBP.', en: 'Upload an image in JPG, PNG, GIF, or WEBP format.' },
        { pt: 'A imagem deve ter no máximo 5 MB.', en: 'The image must be up to 5 MB.' },
        { pt: 'Informe o nome da categoria.', en: 'Enter the category name.' },
        { pt: 'O nome pode ter no máximo 150 caracteres.', en: 'The name can be up to 150 characters long.' },
        { pt: 'A descrição pode ter no máximo 200 caracteres.', en: 'The description can be up to 200 characters long.' },
        { pt: 'Já existe uma categoria com esse nome.', en: 'A category with this name already exists.' },
        { pt: 'Ocorreu um erro ao salvar a categoria. Tente novamente.', en: 'An error occurred while saving the category. Try again.' }
    ];

    const VALIDATION_LOOKUP = VALIDATION_MESSAGES.reduce((map, entry) => {
        const normalize = (value) => (typeof value === 'string' ? value.trim() : '');
        const portuguese = normalize(entry.pt);
        const english = normalize(entry.en);

        if (portuguese) {
            map.set(portuguese, entry);
        }

        if (english) {
            map.set(english, entry);
        }

        return map;
    }, new Map());

    const updateValidationElementLanguage = (element, lang) => {
        if (!element) {
            return;
        }

        const rawText = element.textContent;
        const text = typeof rawText === 'string' ? rawText.trim() : '';

        if (!text) {
            return;
        }

        const entry = VALIDATION_LOOKUP.get(text);
        if (!entry) {
            return;
        }

        if (!element.hasAttribute('data-i18n-pt')) {
            element.setAttribute('data-i18n-pt', entry.pt);
        }

        element.setAttribute('data-i18n-en', entry.en);

        const value = lang === 'en' ? entry.en : entry.pt;
        if (element.textContent !== value) {
            setTextContent(element, value);
        }
    };

    const applyValidationTranslations = (lang, scope = document) => {
        const targets = scope.querySelectorAll('.validation-message, .validation-summary, .validation-summary li');
        targets.forEach((element) => {
            updateValidationElementLanguage(element, lang);
        });
    };

    const searchInput = document.querySelector('[data-ingredient-search]');
    const quantityInput = document.querySelector('[data-ingredient-quantity]');
    const costInput = document.querySelector('[data-ingredient-cost]');
    const unitLabel = document.querySelector('[data-ingredient-unit]');
    const addButton = document.querySelector('[data-ingredient-add]');
    const ingredientList = document.querySelector('[data-ingredient-list]');
    const hiddenContainer = document.getElementById('ingredient-hidden-fields');
    const totalCostInput = document.getElementById('recipe-cost');
    const suggestedPriceInput = document.getElementById('RecipeInput_SuggestedPrice');
    const priceManualFlagInput = document.getElementById('RecipeInput_IsManualPrice');
    const marginInput = document.getElementById('RecipeInput_TargetMargin');
    const marginOutput = document.getElementById('margin-output');
    const portionsInput = document.getElementById('RecipeInput_Yield');

    if (!searchInput || !quantityInput || !costInput || !unitLabel || !addButton || !ingredientList || !hiddenContainer || !totalCostInput) {
        return;
    }

    let currentLanguage = getCurrentLanguage();
    let currencyFormatter = new Intl.NumberFormat(getLocale(currentLanguage), { style: 'currency', currency: 'EUR' });

    const updateCurrencyFormatter = (lang) => {
        currencyFormatter = new Intl.NumberFormat(getLocale(lang), { style: 'currency', currency: 'EUR' });
    };
    const removeIconTemplate = document.querySelector('[data-ingredient-remove] span') ?? null;

    const state = new Map();
    let selectedIngredient = null;
    const markPriceAsManual = (active) => {
        if (priceManualFlagInput) {
            priceManualFlagInput.value = active ? 'true' : 'false';
        }
    };

    const formatPriceValue = (value) => {
        if (!Number.isFinite(value)) {
            return '';
        }

        return normalizeCurrency(value).toFixed(2);
    };

    const parseLocalizedCurrency = (value) => {
        if (typeof value !== 'string') {
            return null;
        }

        const cleaned = value.replace(/[^0-9.,-]/g, '').trim();
        if (!cleaned) {
            return null;
        }

        const lastComma = cleaned.lastIndexOf(',');
        const lastDot = cleaned.lastIndexOf('.');
        let normalized = cleaned;

        if (lastComma > -1 && lastDot > -1) {
            if (lastComma > lastDot) {
                normalized = cleaned.replace(/\./g, '').replace(',', '.');
            } else {
                normalized = cleaned.replace(/,/g, '');
            }
        } else if (lastComma > -1) {
            normalized = cleaned.replace(',', '.');
        } else if (lastDot > -1) {
            const segments = cleaned.split('.');
            if (segments.length > 2) {
                const decimalPart = segments.pop();
                normalized = `${segments.join('')}.${decimalPart}`;
            }
        }

        const parsed = Number.parseFloat(normalized);
        return Number.isFinite(parsed) ? parsed : null;
    };

    const setSuggestedPriceValue = (value, options = {}) => {
        if (!suggestedPriceInput) {
            return;
        }

        const { manual = false } = options;

        if (typeof value === 'string') {
            suggestedPriceInput.value = value;
        } else {
            suggestedPriceInput.value = formatPriceValue(value);
        }

        const hasValue = suggestedPriceInput.value.trim().length > 0;
        markPriceAsManual(manual && hasValue);
        updateMarginDisplay();
    };

    const recipeGrid = document.querySelector('.recipes-grid');
    const recipeCards = recipeGrid ? Array.from(recipeGrid.querySelectorAll('[data-recipe-card]')) : [];
    const emptyFilterCard = recipeGrid?.querySelector('[data-empty-filter]') ?? null;
    const filterForm = document.querySelector('.recipes-filters');
    const recipeSearchField = document.getElementById('recipe-search');
    const recipeCategoryField = document.getElementById('recipe-category');
    const recipeMarginField = document.getElementById('recipe-margin');

    const recipeDialog = document.querySelector('[data-recipe-dialog]');
    const dialogTitle = recipeDialog?.querySelector('[data-dialog-title]') ?? null;
    const dialogCategory = recipeDialog?.querySelector('[data-dialog-category]') ?? null;
    const dialogMetrics = recipeDialog?.querySelector('[data-dialog-metrics]') ?? null;
    const dialogIngredients = recipeDialog?.querySelector('[data-dialog-ingredients]') ?? null;
    const dialogClose = recipeDialog?.querySelector('[data-dialog-close]') ?? null;
    const dialogImageWrapper = recipeDialog?.querySelector('[data-dialog-image-wrapper]') ?? null;
    const dialogImage = recipeDialog?.querySelector('[data-dialog-image]') ?? null;

    const recipeForm = document.querySelector('[data-recipe-form]');
    const editingBanner = recipeForm?.querySelector('[data-editing-banner]') ?? null;
    const editingNameTarget = recipeForm?.querySelector('[data-editing-name]') ?? null;
    const editingCancelButton = recipeForm?.querySelector('[data-editing-cancel]') ?? null;
    const formTitle = document.querySelector('[data-form-title]');
    const formDescription = document.querySelector('[data-form-description]');
    const submitText = document.querySelector('[data-submit-text]');
    const submitAction = document.querySelector('[data-submit-action]');
    const hiddenIdInput = document.getElementById('RecipeInput_Id');
    const nameInputField = document.getElementById('RecipeInput_Name');
    const categorySelectField = document.getElementById('RecipeInput_CategoryId');
    const preparationInputField = document.getElementById('RecipeInput_PreparationTime');
    const descriptionInputField = document.getElementById('RecipeInput_Description');
    const imageInputField = recipeForm?.querySelector('[data-image-input]') ?? null;
    const imagePathField = recipeForm?.querySelector('[data-image-path]') ?? null;
    const imageExistingField = recipeForm?.querySelector('[data-image-existing]') ?? null;
    const imagePreviewWrapper = recipeForm?.querySelector('[data-image-preview]') ?? null;
    const imagePreviewElement = imagePreviewWrapper?.querySelector('[data-image-preview-img]') ?? null;
    const imagePlaceholder = recipeForm?.querySelector('[data-image-placeholder]') ?? null;
    const imageRemoveButton = recipeForm?.querySelector('[data-image-remove]') ?? null;
    const imageUploader = recipeForm?.querySelector('[data-image-control]') ?? null;
    const imageRemoveFlagField = recipeForm?.querySelector('[data-image-remove-flag]') ?? null;

    let previewObjectUrl = null;
    let formMode = 'create';
    let currentDialogData = null;

    function setPreviewUrl(value) {
        if (!imagePathField) {
            return;
        }

        if (value) {
            imagePathField.dataset.previewUrl = value;
        } else {
            delete imagePathField.dataset.previewUrl;
        }
    }

    function getPreviewSource() {
        if (!imagePathField) {
            return '';
        }

        const datasetPreview = imagePathField.dataset.previewUrl?.trim() ?? '';
        if (datasetPreview) {
            return datasetPreview;
        }

        return imagePathField.value?.trim() ?? '';
    }

    function setRemoveFlag(value) {
        if (imageRemoveFlagField) {
            imageRemoveFlagField.value = value ? 'true' : 'false';
        }
    }

    function isRemovalActive() {
        if (!imageRemoveFlagField) {
            return false;
        }

        return imageRemoveFlagField.value.toLowerCase() === 'true';
    }

    function setImageState(hasImage) {
        if (!imageUploader) {
            return;
        }

        if (hasImage) {
            imageUploader.dataset.hasImage = 'true';
            setRemoveFlag(false);
        } else {
            delete imageUploader.dataset.hasImage;
        }

        if (imagePlaceholder) {
            imagePlaceholder.hidden = Boolean(hasImage);
        }

        if (imageRemoveButton) {
            imageRemoveButton.toggleAttribute('disabled', !hasImage);
        }
    }

    function clearPreviewUrl() {
        if (previewObjectUrl) {
            URL.revokeObjectURL(previewObjectUrl);
            previewObjectUrl = null;
        }
    }

    function clearImageSelection(options = {}) {
        const { clearStoredPath = true, markRemoval = false } = options;

        clearPreviewUrl();

        if (imageInputField) {
            imageInputField.value = '';
        }

        if (imagePreviewWrapper) {
            imagePreviewWrapper.hidden = true;
        }

        if (imagePreviewElement) {
            imagePreviewElement.removeAttribute('src');
        }

        if (clearStoredPath && imagePathField) {
            imagePathField.value = '';
        }

        setPreviewUrl('');

        if (markRemoval) {
            setRemoveFlag(true);
        } else {
            setRemoveFlag(false);
        }

        setImageState(false);
    }

    function renderImageFromPath(path) {
        if (!imagePreviewWrapper || !imagePreviewElement) {
            return;
        }

        clearPreviewUrl();

        const normalized = typeof path === 'string' ? path.trim() : '';

        if (!normalized) {
            imagePreviewWrapper.hidden = true;
            imagePreviewElement.removeAttribute('src');
            setImageState(false);
            return;
        }

        imagePreviewElement.src = normalized;
        imagePreviewWrapper.hidden = false;
        setImageState(true);
        setRemoveFlag(false);
    }

    function renderImageFromFile(file) {
        if (!file || !imagePreviewWrapper || !imagePreviewElement) {
            return;
        }

        clearPreviewUrl();

        const objectUrl = URL.createObjectURL(file);
        previewObjectUrl = objectUrl;
        imagePreviewElement.src = objectUrl;
        imagePreviewWrapper.hidden = false;
        setImageState(true);
        setRemoveFlag(false);
    }

    function parseNumber(value) {
        if (typeof value !== 'string') {
            return Number(value) || 0;
        }

        const trimmedValue = value.trim();
        if (trimmedValue === '') {
            return 0;
        }

        const hasComma = trimmedValue.includes(',');
        const hasDot = trimmedValue.includes('.');

        let normalized = trimmedValue;
        if (hasComma && hasDot) {
            normalized = normalized.replace(/\./g, '').replace(',', '.');
        } else if (hasComma) {
            normalized = normalized.replace(/\./g, '').replace(',', '.');
        } else {
            normalized = normalized.replace(/,/g, '.');
        }

        return Number(normalized) || 0;
    }

    const formatQuantity = (value, lang = currentLanguage) => Number(value).toLocaleString(getLocale(lang), {
        minimumFractionDigits: 0,
        maximumFractionDigits: 2,
    });

    const formatCurrencyValue = (value, lang = currentLanguage) => {
        const formatter = lang === currentLanguage
            ? currencyFormatter
            : new Intl.NumberFormat(getLocale(lang), { style: 'currency', currency: 'EUR' });

        return formatter.format(Number(value) || 0);
    };

    const formatPercentageValue = (value, lang = currentLanguage) => {
        const normalized = Number(value);
        if (!Number.isFinite(normalized)) {
            return lang === 'en' ? '0%' : '0%';
        }

        const formatter = new Intl.NumberFormat(getLocale(lang), { maximumFractionDigits: 0 });
        return `${formatter.format(normalized * 100)}%`;
    };

    const formatPreparationLabel = (minutes, fallback, lang = currentLanguage) => {
        const value = Number(minutes);
        if (!Number.isFinite(value) || value <= 0) {
            return lang === 'en' ? 'Time not provided' : (fallback || 'Tempo não informado');
        }

        if (lang === 'en') {
            return value === 1 ? '1 minute' : `${value} minutes`;
        }

        return value === 1 ? '1 minuto' : `${value} minutos`;
    };

    const formatYieldLabel = (quantity, fallback, lang = currentLanguage) => {
        const value = Number(quantity);
        if (!Number.isFinite(value) || value <= 0) {
            return lang === 'en' ? 'Servings not provided' : (fallback || 'Porções não informadas');
        }

        if (lang === 'en') {
            return value === 1 ? '1 serving' : `${value} servings`;
        }

        return value === 1 ? '1 porção' : `${value} porções`;
    };

    function getPortionCount() {
        if (!portionsInput) {
            return 0;
        }

        const value = Number.parseInt(portionsInput.value, 10);
        return Number.isFinite(value) && value > 0 ? value : 0;
    }

    function normalizeCurrency(value) {
        if (!Number.isFinite(value)) {
            return 0;
        }

        return Math.round(value * 100) / 100;
    }

    function calculatePerPortionCost() {
        const total = Array.from(state.values()).reduce((acc, item) => acc + item.quantity * item.costPerUnit, 0);
        const portions = getPortionCount();
        const perPortion = portions > 0 ? total / portions : total;
        return normalizeCurrency(perPortion);
    }

    const getSellingPriceValue = () => {
        if (!suggestedPriceInput) {
            return 0;
        }

        const priceValue = Number.parseFloat(suggestedPriceInput.value);
        return Number.isFinite(priceValue) && priceValue > 0 ? priceValue : 0;
    };

    function calculateMarginPercentage(costPerPortion, sellingPrice) {
        if (!Number.isFinite(costPerPortion) || costPerPortion <= 0) {
            return 0;
        }

        if (!Number.isFinite(sellingPrice) || sellingPrice <= 0) {
            return 0;
        }

        const rawMargin = ((sellingPrice - costPerPortion) / costPerPortion) * 100;
        return rawMargin;
    }

    function updateMarginDisplay(options = {}) {
        if (!marginInput) {
            return;
        }

        const customCost = Number.parseFloat(options.costValue);
        const costPerPortion = Number.isFinite(customCost)
            ? customCost
            : calculatePerPortionCost();
        const sellingPrice = getSellingPriceValue();
        const marginValue = calculateMarginPercentage(costPerPortion, sellingPrice);
        const normalizedMargin = Math.abs(marginValue) < 0.005 ? 0 : marginValue;
        const formattedMargin = normalizedMargin.toFixed(2);

        marginInput.value = formattedMargin;

        if (marginOutput) {
            marginOutput.textContent = `${formattedMargin}%`;
        }
    }

    function updateTotalCostDisplay() {
        const perPortion = calculatePerPortionCost();
        totalCostInput.value = currencyFormatter.format(perPortion);
        updateMarginDisplay({ costValue: perPortion });
    }

    function renderHiddenFields() {
        hiddenContainer.innerHTML = '';
        let index = 0;
        for (const item of state.values()) {
            const wrapper = document.createElement('div');
            wrapper.dataset.hiddenItem = '';
            wrapper.dataset.id = String(item.id);

            const idInput = document.createElement('input');
            idInput.type = 'hidden';
            idInput.name = `RecipeInput.Ingredients[${index}].IngredientId`;
            idInput.value = String(item.id);
            wrapper.appendChild(idInput);

            const nameInput = document.createElement('input');
            nameInput.type = 'hidden';
            nameInput.name = `RecipeInput.Ingredients[${index}].IngredientName`;
            nameInput.value = item.name;
            wrapper.appendChild(nameInput);

            const quantityInputHidden = document.createElement('input');
            quantityInputHidden.type = 'hidden';
            quantityInputHidden.name = `RecipeInput.Ingredients[${index}].Quantity`;
            quantityInputHidden.value = item.quantity.toLocaleString('en-US', {
                minimumFractionDigits: 0,
                maximumFractionDigits: 4,
                useGrouping: false,
            });
            wrapper.appendChild(quantityInputHidden);

            const unitInputHidden = document.createElement('input');
            unitInputHidden.type = 'hidden';
            unitInputHidden.name = `RecipeInput.Ingredients[${index}].Unit`;
            unitInputHidden.value = item.unit;
            wrapper.appendChild(unitInputHidden);

            const costInputHidden = document.createElement('input');
            costInputHidden.type = 'hidden';
            costInputHidden.name = `RecipeInput.Ingredients[${index}].CostPerUnit`;
            costInputHidden.value = item.costPerUnit.toLocaleString('en-US', {
                minimumFractionDigits: 0,
                maximumFractionDigits: 4,
                useGrouping: false,
            });
            wrapper.appendChild(costInputHidden);

            hiddenContainer.appendChild(wrapper);
            index += 1;
        }
    }

    function renderIngredientList() {
        ingredientList.innerHTML = '';

        if (state.size === 0) {
            const placeholder = document.createElement('li');
            placeholder.className = 'ingredient-placeholder';
            placeholder.textContent = translations.ingredientPlaceholder[currentLanguage];
            ingredientList.appendChild(placeholder);
            return;
        }

        for (const item of state.values()) {
            const listItem = document.createElement('li');
            listItem.className = 'ingredient-item';
            listItem.dataset.ingredientItem = '';
            listItem.dataset.id = String(item.id);

            const content = document.createElement('div');

            const nameElement = document.createElement('strong');
            nameElement.textContent = item.name;
            content.appendChild(nameElement);

            const metaElement = document.createElement('span');
            metaElement.className = 'ingredient-meta';
            metaElement.textContent = formatTemplate(translations.ingredientMeta[currentLanguage], {
                quantity: formatQuantity(item.quantity),
                unit: item.unit,
                cost: currencyFormatter.format(item.costPerUnit)
            });
            content.appendChild(metaElement);

            const removeButton = document.createElement('button');
            removeButton.type = 'button';
            removeButton.className = 'ingredient-remove';
            removeButton.dataset.ingredientRemove = '';
            removeButton.setAttribute('aria-label', formatTemplate(translations.removeIngredient[currentLanguage], { name: item.name }));

            if (removeIconTemplate) {
                removeButton.appendChild(removeIconTemplate.cloneNode(true));
            } else {
                const fallbackIcon = document.createElement('span');
                fallbackIcon.setAttribute('aria-hidden', 'true');
                fallbackIcon.textContent = '×';
                removeButton.appendChild(fallbackIcon);
            }

            removeButton.addEventListener('click', () => {
                state.delete(item.id);
                renderState();
            });

            listItem.appendChild(content);
            listItem.appendChild(removeButton);
            ingredientList.appendChild(listItem);
        }
    }

    function renderState() {
        renderHiddenFields();
        renderIngredientList();
        updateTotalCostDisplay();
    }

    function normalizeText(value) {
        if (!value) {
            return '';
        }

        return value
            .toString()
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .toLowerCase();
    }

    function applyRecipeFilters() {
        if (!recipeCards.length) {
            if (emptyFilterCard) {
                emptyFilterCard.hidden = true;
            }

            return;
        }

        const searchValue = normalizeText(recipeSearchField?.value?.trim() ?? '');
        const categoryValue = recipeCategoryField?.value ?? 'all';
        const marginValue = recipeMarginField?.value ?? 'all';

        let visibleCount = 0;

        recipeCards.forEach((card) => {
            const dataset = card.dataset;
            const keywords = normalizeText(dataset.search ?? dataset.name ?? '');
            const matchesSearch = !searchValue || keywords.includes(searchValue);
            const matchesCategory = categoryValue === 'all' || (dataset.categoryId ?? '') === categoryValue;
            const matchesMargin = marginValue === 'all' || (dataset.marginBand ?? '') === marginValue;

            const shouldShow = matchesSearch && matchesCategory && matchesMargin;
            card.hidden = !shouldShow;

            if (shouldShow) {
                visibleCount += 1;
            }
        });

        if (emptyFilterCard) {
            emptyFilterCard.hidden = visibleCount !== 0;
        }
    }

    const applyFormModeLanguage = (lang) => {
        if (formTitle) {
            setTextContent(formTitle, getModeDatasetValue(formTitle, 'modeTitle', formMode, lang));
        }

        if (formDescription) {
            setTextContent(formDescription, getModeDatasetValue(formDescription, 'modeDescription', formMode, lang));
        }

        if (submitText) {
            setTextContent(submitText, getModeDatasetValue(submitText, 'modeSubmit', formMode, lang));
        }
    };

    function setEditingMode(isEditing, recipeName) {
        formMode = isEditing ? 'edit' : 'create';

        if (editingBanner) {
            editingBanner.hidden = !isEditing;
        }

        if (editingNameTarget) {
            setTextContent(editingNameTarget, recipeName ?? '');
        }

        if (submitAction) {
            submitAction.dataset.mode = formMode;
        }

        applyFormModeLanguage(currentLanguage);
    }

    function clearEditingMode() {
        setEditingMode(false, '');

        if (hiddenIdInput) {
            hiddenIdInput.value = '';
        }

        if (imageExistingField) {
            imageExistingField.value = '';
        }

        clearImageSelection();
    }

    function populateRecipeFormFromData(data) {
        if (!recipeForm) {
            return;
        }

        if (hiddenIdInput) {
            hiddenIdInput.value = data?.id ? String(data.id) : '';
        }

        if (nameInputField) {
            nameInputField.value = data?.name ?? '';
        }

        if (categorySelectField) {
            categorySelectField.value = data?.categoryId ? String(data.categoryId) : '';
        }

        if (portionsInput) {
            portionsInput.value = Number.isFinite(Number(data?.yieldQuantity))
                ? String(Math.max(Number(data.yieldQuantity), 0))
                : '';
        }

        if (preparationInputField) {
            preparationInputField.value = Number.isFinite(Number(data?.preparationTimeMinutes))
                ? String(Math.max(Number(data.preparationTimeMinutes), 0))
                : '';
        }

        if (marginInput) {
            const targetMarginValue = Number.parseFloat(data?.targetMargin);
            const normalizedMargin = Number.isFinite(targetMarginValue)
                ? targetMarginValue
                : Number.parseFloat(marginInput.value) || 0;
            const formattedMargin = normalizedMargin.toFixed(2);
            marginInput.value = formattedMargin;

            if (marginOutput) {
                marginOutput.textContent = `${formattedMargin}%`;
            }
        }

        if (suggestedPriceInput) {
            const priceValue = Number(data?.suggestedPriceValue);
            if (Number.isFinite(priceValue)) {
                setSuggestedPriceValue(priceValue, { manual: true });
            } else {
                const fallbackPrice = parseLocalizedCurrency(data?.suggestedPrice);
                if (fallbackPrice !== null) {
                    setSuggestedPriceValue(fallbackPrice, { manual: true });
                } else {
                    setSuggestedPriceValue('', { manual: false });
                }
            }
        }

        if (descriptionInputField) {
            descriptionInputField.value = data?.description ?? '';
        }


        clearImageSelection({ clearStoredPath: true });

        if (imagePathField) {
            imagePathField.value = data?.imageStoragePath ?? '';
            setPreviewUrl(data?.imageUrl ?? '');
        }

        if (imageExistingField) {
            imageExistingField.value = data?.imageStoragePath ?? '';
        }

        const previewSource = data?.imageUrl ?? data?.imageStoragePath ?? '';
        if (previewSource) {
            renderImageFromPath(previewSource);
        }

        searchInput.value = '';
        quantityInput.value = '';
        costInput.value = '';
        unitLabel.textContent = 'un';
        selectedIngredient = null;

        state.clear();

        if (Array.isArray(data?.ingredients)) {
            data.ingredients.forEach((ingredient) => {
                const id = Number(ingredient.ingredientId);
                const quantity = Number(ingredient.quantity);

                if (!Number.isFinite(id) || id <= 0 || !Number.isFinite(quantity) || quantity <= 0) {
                    return;
                }

                const costPerUnit = Number(ingredient.costPerUnit) || 0;

                state.set(id, {
                    id,
                    name: ingredient.ingredientName || `Ingrediente ${id}`,
                    unit: ingredient.unit || 'un',
                    costPerUnit,
                    quantity,
                });
            });
        }

        renderState();
        setEditingMode(true, data?.name ?? '');
        recipeForm.scrollIntoView({ behavior: 'smooth', block: 'start' });
        nameInputField?.focus();
    }

    function handleRecipeEdit(card) {
        if (!card) {
            return;
        }

        const payload = card.dataset.recipe;

        if (!payload) {
            return;
        }

        try {
            const data = JSON.parse(payload);
            populateRecipeFormFromData(data);
        } catch (error) {
            // eslint-disable-next-line no-console
        console.error(translations.consoleLoadError[currentLanguage], error);
        }
    }

    const renderDialog = (data, lang) => {
        if (!recipeDialog) {
            return;
        }

        const formatter = lang === currentLanguage
            ? currencyFormatter
            : new Intl.NumberFormat(getLocale(lang), { style: 'currency', currency: 'EUR' });

        const safeName = data.name?.trim();

        if (dialogTitle) {
            setTextContent(dialogTitle, safeName || translations.dialogRecipe[lang]);
        }

        if (dialogCategory) {
            setTextContent(dialogCategory, data.category ?? '');
        }

        if (dialogMetrics) {
            dialogMetrics.innerHTML = '';

            const metrics = [
                {
                    key: 'cost',
                    value: lang === 'en'
                        ? formatter.format(Number(data.foodCostValue) || 0)
                        : (data.foodCost ?? formatter.format(Number(data.foodCostValue) || 0))
                },
                {
                    key: 'suggestedPrice',
                    value: lang === 'en'
                        ? formatter.format(Number(data.suggestedPriceValue) || 0)
                        : (data.suggestedPrice ?? formatter.format(Number(data.suggestedPriceValue) || 0))
                },
                {
                    key: 'margin',
                    value: lang === 'en'
                        ? formatPercentageValue(data.marginValue, lang)
                        : (data.margin ?? formatPercentageValue(data.marginValue, lang))
                },
                {
                    key: 'contribution',
                    value: lang === 'en'
                        ? formatter.format(Number(data.contributionValue) || 0)
                        : (data.contribution ?? formatter.format(Number(data.contributionValue) || 0))
                },
                {
                    key: 'preparationTime',
                    value: lang === 'en'
                        ? formatPreparationLabel(data.preparationTimeMinutes, data.preparationTime, lang)
                        : (data.preparationTime ?? formatPreparationLabel(data.preparationTimeMinutes, data.preparationTime, lang))
                },
                {
                    key: 'yield',
                    value: lang === 'en'
                        ? formatYieldLabel(data.yieldQuantity, data.yield, lang)
                        : (data.yield ?? formatYieldLabel(data.yieldQuantity, data.yield, lang))
                }
            ];

            metrics
                .filter((metric) => Boolean(metric.value))
                .forEach((metric) => {
                    const wrapper = document.createElement('div');
                    const term = document.createElement('dt');
                    term.textContent = translations.dialogMetrics[metric.key][lang];
                    const value = document.createElement('dd');
                    value.textContent = metric.value;
                    wrapper.appendChild(term);
                    wrapper.appendChild(value);
                    dialogMetrics.appendChild(wrapper);
                });
        }

        if (dialogImage && dialogImageWrapper) {
            const imageUrl = typeof data.imageUrl === 'string' ? data.imageUrl.trim() : '';
            const fallbackPath = typeof data.imagePath === 'string' ? data.imagePath.trim() : '';
            const finalPath = imageUrl || fallbackPath;

            if (finalPath) {
                dialogImage.src = finalPath;
                dialogImage.alt = formatTemplate(translations.dialogImageAlt[lang], { name: safeName || '' }).trim();
                dialogImageWrapper.hidden = false;
            } else {
                dialogImage.removeAttribute('src');
                dialogImage.alt = '';
                dialogImageWrapper.hidden = true;
            }
        }

        if (dialogIngredients) {
            dialogIngredients.innerHTML = '';

            if (Array.isArray(data.ingredients) && data.ingredients.length > 0) {
                data.ingredients.forEach((ingredient) => {
                    const item = document.createElement('li');
                    const name = document.createElement('strong');
                    name.textContent = ingredient.ingredientName || (lang === 'en' ? 'Ingredient' : 'Ingrediente');
                    const meta = document.createElement('span');

                    const quantity = Number(ingredient.quantity) || 0;
                    const unit = ingredient.unit || 'un';
                    const costPerUnit = Number(ingredient.costPerUnit) || 0;

                    meta.textContent = formatTemplate(translations.ingredientMeta[lang], {
                        quantity: formatQuantity(quantity, lang),
                        unit,
                        cost: formatter.format(costPerUnit)
                    });

                    item.appendChild(name);
                    item.appendChild(meta);
                    dialogIngredients.appendChild(item);
                });
            } else {
                const emptyItem = document.createElement('li');
                emptyItem.textContent = translations.dialogNoIngredients[lang];
                dialogIngredients.appendChild(emptyItem);
            }
        }
    };

    function openRecipeDialog(card) {
        if (!recipeDialog || !card) {
            return;
        }

        const payload = card.dataset.recipe;

        if (!payload) {
            return;
        }

        try {
            currentDialogData = JSON.parse(payload);
        } catch (error) {
            // eslint-disable-next-line no-console
            console.error(translations.consoleParseError[currentLanguage], error);
            return;
        }

        if (!currentDialogData) {
            return;
        }

        renderDialog(currentDialogData, currentLanguage);
        recipeDialog.showModal();
    }

    function setSelectedIngredientByName(name) {
        if (!name) {
            selectedIngredient = null;
            unitLabel.textContent = 'un';
            costInput.value = '';
            return;
        }

        const match = ingredients.find((item) => item.name.toLowerCase() === name.toLowerCase());
        if (match) {
            selectedIngredient = match;
            unitLabel.textContent = match.unit;
            updateEstimatedCost();
        } else {
            selectedIngredient = null;
            unitLabel.textContent = 'un';
            costInput.value = '';
        }
    }

    function updateEstimatedCost() {
        if (!selectedIngredient) {
            costInput.value = '';
            return;
        }

        const quantity = parseNumber(quantityInput.value);
        const estimatedTotal = quantity * selectedIngredient.costPerUnit;
        const costDisplay = estimatedTotal
            ? currencyFormatter.format(estimatedTotal)
            : currencyFormatter.format(selectedIngredient.costPerUnit);

        costInput.value = costDisplay;
    }

    function addIngredientToState() {
        if (!selectedIngredient) {
            searchInput.focus();
            searchInput.setCustomValidity(translations.selectIngredient[currentLanguage]);
            searchInput.reportValidity();
            return;
        }

        const quantity = parseNumber(quantityInput.value);
        if (!quantity || quantity <= 0) {
            quantityInput.focus();
            quantityInput.setCustomValidity(translations.quantityGreaterThanZero[currentLanguage]);
            quantityInput.reportValidity();
            return;
        }

        searchInput.setCustomValidity('');
        quantityInput.setCustomValidity('');

        const existing = state.get(selectedIngredient.id);
        if (existing) {
            existing.quantity = Number((existing.quantity + quantity).toFixed(4));
        } else {
            state.set(selectedIngredient.id, {
                id: selectedIngredient.id,
                name: selectedIngredient.name,
                unit: selectedIngredient.unit,
                costPerUnit: selectedIngredient.costPerUnit,
                quantity: Number(quantity.toFixed(4)),
            });
        }

        renderState();
        searchInput.value = '';
        quantityInput.value = '';
        unitLabel.textContent = 'un';
        costInput.value = '';
        selectedIngredient = null;
        searchInput.focus();
    }

    function loadInitialState() {
        const hiddenItems = hiddenContainer.querySelectorAll('[data-hidden-item]');
        hiddenItems.forEach((wrapper) => {
            const id = Number(wrapper.dataset.id);
            if (!id) {
                return;
            }

            const quantityField = wrapper.querySelector('[name$=".Quantity"]');
            const unitField = wrapper.querySelector('[name$=".Unit"]');
            const costField = wrapper.querySelector('[name$=".CostPerUnit"]');
            const nameField = wrapper.querySelector('[name$=".IngredientName"]');

            const quantity = parseNumber(quantityField?.value ?? '0');
            const cost = parseNumber(costField?.value ?? '0');

            if (quantity <= 0) {
                return;
            }

            const fallback = ingredientMap.get(id);
            state.set(id, {
                id,
                name: nameField?.value || fallback?.name || (currentLanguage === 'en' ? 'Ingredient' : 'Ingrediente'),
                unit: unitField?.value || fallback?.unit || 'un',
                costPerUnit: cost || fallback?.costPerUnit || 0,
                quantity,
            });
        });

        renderState();
    }

    searchInput.addEventListener('input', () => {
        searchInput.setCustomValidity('');
        setSelectedIngredientByName(searchInput.value.trim());
        updateEstimatedCost();
    });

    searchInput.addEventListener('change', () => {
        setSelectedIngredientByName(searchInput.value.trim());
        updateEstimatedCost();
    });

    quantityInput.addEventListener('input', () => {
        quantityInput.setCustomValidity('');
        updateEstimatedCost();
    });

    if (imageInputField) {
        imageInputField.addEventListener('change', () => {
            const file = imageInputField.files?.[0] ?? null;
            if (file) {
                renderImageFromFile(file);
                return;
            }

            clearPreviewUrl();

            const previewSource = getPreviewSource();
            if (previewSource) {
                renderImageFromPath(previewSource);
                return;
            }

            const removalActive = isRemovalActive();
            clearImageSelection({ clearStoredPath: false, markRemoval: removalActive });
        });
    }

    if (imageRemoveButton) {
        imageRemoveButton.addEventListener('click', () => {
            clearImageSelection({ markRemoval: true });
        });
    }

    if (portionsInput) {
        portionsInput.addEventListener('input', () => {
            updateTotalCostDisplay();
        });
    }

    if (suggestedPriceInput) {
        suggestedPriceInput.addEventListener('input', () => {
            const hasValue = suggestedPriceInput.value.trim().length > 0;
            markPriceAsManual(hasValue);
            updateMarginDisplay();
        });

        suggestedPriceInput.addEventListener('blur', () => {
            const parsed = Number.parseFloat(suggestedPriceInput.value);
            if (!Number.isFinite(parsed)) {
                return;
            }

            setSuggestedPriceValue(parsed, { manual: true });
        });
    }

    addButton.addEventListener('click', (event) => {
        event.preventDefault();
        addIngredientToState();
    });

    if (recipeCards.length > 0) {
        recipeCards.forEach((card) => {
            const viewButton = card.querySelector('[data-recipe-view]');
            if (viewButton) {
                viewButton.addEventListener('click', () => {
                    openRecipeDialog(card);
                });
            }

            const editButton = card.querySelector('[data-recipe-edit]');
            if (editButton) {
                editButton.addEventListener('click', () => {
                    handleRecipeEdit(card);
                });
            }
        });
    }

    if (recipeDialog && dialogClose) {
        dialogClose.addEventListener('click', () => {
            recipeDialog.close();
        });
    }

    if (recipeDialog) {
        recipeDialog.addEventListener('cancel', (event) => {
            event.preventDefault();
            recipeDialog.close();
        });

        recipeDialog.addEventListener('close', () => {
            currentDialogData = null;
        });
    }

    if (filterForm) {
        filterForm.addEventListener('reset', () => {
            window.setTimeout(() => {
                applyRecipeFilters();
            }, 0);
        });
    }

    if (recipeSearchField) {
        recipeSearchField.addEventListener('input', () => {
            applyRecipeFilters();
        });
    }

    if (recipeCategoryField) {
        recipeCategoryField.addEventListener('change', () => {
            applyRecipeFilters();
        });
    }

    if (recipeMarginField) {
        recipeMarginField.addEventListener('change', () => {
            applyRecipeFilters();
        });
    }

    if (editingCancelButton && recipeForm) {
        editingCancelButton.addEventListener('click', () => {
            recipeForm.reset();
        });
    }

    if (recipeForm) {
        recipeForm.addEventListener('submit', () => {
            renderState();
        });

        recipeForm.addEventListener('reset', () => {
            window.setTimeout(() => {
                state.clear();
                markPriceAsManual(false);
                renderState();
                updateMarginDisplay();
                clearEditingMode();
            }, 0);
        });
    }

    window.addEventListener('beforeunload', () => {
        clearPreviewUrl();
    });

    applyFormModeLanguage(currentLanguage);
    applyValidationTranslations(currentLanguage, recipeForm ?? document);
    loadInitialState();
    applyRecipeFilters();

    if (imagePathField && (imagePathField.value || imagePathField.dataset.previewUrl)) {
        setRemoveFlag(false);
        const previewSource = getPreviewSource();
        if (previewSource) {
            renderImageFromPath(previewSource);
        }

        if (imageExistingField && !imageExistingField.value) {
            imageExistingField.value = imagePathField.value;
        }
    } else {
        const removalActive = isRemovalActive();
        clearImageSelection({ clearStoredPath: false, markRemoval: removalActive });
    }

    if (marginInput) {
        updateMarginDisplay();
    }

    subscribeToLanguageChanges((lang) => {
        currentLanguage = lang;
        updateCurrencyFormatter(lang);
        renderState();
        applyFormModeLanguage(lang);
        applyValidationTranslations(lang, recipeForm ?? document);

        updateMarginDisplay();

        if (recipeDialog?.open && currentDialogData) {
            renderDialog(currentDialogData, lang);
        }
    });
})();
