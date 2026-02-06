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

    const modeKey = capitalize(mode);
    const datasetKey = `${prefix}${modeKey}`;
    const englishKey = `${datasetKey}En`;
    const dataset = element.dataset || {};

    if (lang === 'en' && dataset[englishKey]) {
        return dataset[englishKey];
    }

    if (dataset[datasetKey]) {
        return dataset[datasetKey];
    }

    return element.textContent?.trim() || '';
};

const setTextContent = (element, value) => {
    if (!element || value == null) {
        return;
    }

    element.textContent = value;
};

const VALIDATION_MESSAGES = [
    {
        pt: 'Informe o nome do ingrediente.',
        en: 'Enter the ingredient name.'
    },
    {
        pt: 'Informe o valor total do ingrediente.',
        en: 'Enter the ingredient total cost.'
    },
    {
        pt: 'Informe uma quantidade maior que zero.',
        en: 'Enter an amount greater than zero.'
    },
    {
        pt: 'Informe a quantidade adquirida.',
        en: 'Enter the purchased quantity.'
    },
    {
        pt: 'Selecione a unidade de medida.',
        en: 'Select a measurement unit.'
    },
    {
        pt: 'A unidade selecionada é inválida.',
        en: 'The selected unit is invalid.'
    },
    {
        pt: 'Selecione uma categoria válida.',
        en: 'Select a valid category.'
    },
    {
        pt: 'O nome do fornecedor pode ter no máximo 150 caracteres.',
        en: 'The supplier name can be up to 150 characters long.'
    },
    {
        pt: 'O nome pode ter no máximo 150 caracteres.',
        en: 'The name can be up to 150 characters long.'
    },
    {
        pt: 'As observações podem ter no máximo 2000 caracteres.',
        en: 'Notes can have up to 2000 characters.'
    },
    {
        pt: 'Informe o nome da categoria.',
        en: 'Enter the category name.'
    },
    {
        pt: 'O identificador do ícone é inválido.',
        en: 'The icon identifier is invalid.'
    },
    {
        pt: 'Informe o nome do fornecedor.',
        en: 'Enter the supplier name.'
    },
    {
        pt: 'Já existe uma categoria com esse nome.',
        en: 'A category with this name already exists.'
    },
    {
        pt: 'Ocorreu um erro ao salvar a categoria. Tente novamente.',
        en: 'An error occurred while saving the category. Try again.'
    },
    {
        pt: 'Já existe um ingrediente com esse nome.',
        en: 'An ingredient with this name already exists.'
    },
    {
        pt: 'O ingrediente selecionado não foi encontrado. Atualize a página e tente novamente.',
        en: 'The selected ingredient was not found. Refresh the page and try again.'
    },
    {
        pt: 'Ocorreu um erro ao salvar o ingrediente. Tente novamente.',
        en: 'An error occurred while saving the ingredient. Try again.'
    }
];

const VALIDATION_LOOKUP = VALIDATION_MESSAGES.reduce((map, entry) => {
    const normalize = (value) => typeof value === 'string' ? value.trim() : '';
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

    const { pt, en } = entry;

    if (!element.hasAttribute('data-i18n-pt')) {
        element.setAttribute('data-i18n-pt', pt);
    }

    element.setAttribute('data-i18n-en', en);

    const value = lang === 'en' ? en : pt;
    if (element.textContent !== value) {
        setTextContent(element, value);
    }
};

const applyValidationTranslations = (lang, scope) => {
    const root = scope || document;
    const targets = root.querySelectorAll('.form-error, .form-error li');

    targets.forEach((element) => {
        updateValidationElementLanguage(element, lang);
    });
};

(function () {
    const forms = Array.from(document.querySelectorAll('.ingredient-form, .category-form'));
    if (forms.length === 0) {
        return;
    }

    const applyLanguageToForms = (lang) => {
        forms.forEach((form) => {
            applyValidationTranslations(lang, form);
        });
    };

    const observerConfig = { childList: true, subtree: true, characterData: true };

    forms.forEach((form) => {
        applyValidationTranslations(getCurrentLanguage(), form);

        const observer = new MutationObserver(() => {
            applyValidationTranslations(getCurrentLanguage(), form);
        });

        observer.observe(form, observerConfig);
    });

    subscribeToLanguageChanges(applyLanguageToForms);
})();

(function () {
        const picker = document.querySelector('[data-icon-picker]');
        if (!picker) {
            return;
        }

        const trigger = picker.querySelector('[data-icon-picker-trigger]');
        const list = picker.querySelector('[data-icon-picker-list]');
        const preview = picker.querySelector('[data-icon-picker-preview]');
        const text = picker.querySelector('[data-icon-picker-text]');
        const hiddenInput = picker.querySelector('input[type="hidden"]');

        if (!trigger || !list || !preview || !text || !hiddenInput) {
            return;
        }

        const closeList = () => {
            list.hidden = true;
            picker.removeAttribute('data-open');
            trigger.setAttribute('aria-expanded', 'false');
        };

        const openList = () => {
            list.hidden = false;
            picker.setAttribute('data-open', 'true');
            trigger.setAttribute('aria-expanded', 'true');
        };

        const toggleList = () => {
            if (picker.getAttribute('data-open') === 'true') {
                closeList();
            } else {
                openList();
                const selected = list.querySelector('[data-selected] button');
                const firstOption = selected || list.querySelector('button');
                if (firstOption) {
                    firstOption.focus();
                }
            }
        };

        const setSelection = (optionItem) => {
            list.querySelectorAll('[data-selected]').forEach((item) => item.removeAttribute('data-selected'));
            list.querySelectorAll('[role="option"]').forEach((item) => item.removeAttribute('aria-selected'));
            optionItem.setAttribute('data-selected', 'true');
            optionItem.setAttribute('aria-selected', 'true');

            const optionButton = optionItem.querySelector('button');
            if (!optionButton) {
                return;
            }

            const optionPreview = optionButton.querySelector('.icon-picker-preview');
            const optionLabel = optionButton.querySelector('.icon-picker-option-label');

            if (optionPreview) {
                preview.innerHTML = optionPreview.innerHTML;
            }

            if (optionLabel) {
                text.textContent = optionLabel.textContent;
            }

            const value = optionItem.getAttribute('data-value') || '';
            hiddenInput.value = value;
            picker.setAttribute('data-has-selection', value ? 'true' : 'false');

            if (value) {
                trigger.setAttribute('aria-activedescendant', optionItem.id);
            } else {
                trigger.removeAttribute('aria-activedescendant');
            }
        };

        trigger.addEventListener('click', (event) => {
            event.preventDefault();
            toggleList();
        });

        trigger.addEventListener('keydown', (event) => {
            if (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                if (picker.getAttribute('data-open') === 'true') {
                    const selected = list.querySelector('[data-selected] button');
                    const targetButton = selected || list.querySelector('button');
                    if (targetButton) {
                        targetButton.focus();
                    }
                } else {
                    toggleList();
                }
            }
        });

        list.addEventListener('click', (event) => {
            const optionButton = event.target.closest('button');
            if (!optionButton) {
                return;
            }

            const optionItem = optionButton.closest('li');
            if (!optionItem) {
                return;
            }

            setSelection(optionItem);
            closeList();
            trigger.focus();
        });

        list.addEventListener('keydown', (event) => {
            const optionButton = event.target.closest('button');
            if (!optionButton) {
                return;
            }

            const optionItem = optionButton.closest('li');
            if (!optionItem) {
                return;
            }

            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                setSelection(optionItem);
                closeList();
                trigger.focus();
            }

            if (event.key === 'Escape') {
                event.preventDefault();
                closeList();
                trigger.focus();
            }

            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                event.preventDefault();
                const items = Array.from(list.querySelectorAll('li'));
                const currentIndex = items.indexOf(optionItem);
                if (currentIndex === -1) {
                    return;
                }

                const nextIndex = event.key === 'ArrowDown'
                    ? Math.min(items.length - 1, currentIndex + 1)
                    : Math.max(0, currentIndex - 1);
                const nextButton = items[nextIndex]?.querySelector('button');
                if (nextButton) {
                    nextButton.focus();
                }
            }
        });

        const initializeSelection = () => {
            const value = hiddenInput.value;
            if (!value) {
                return;
            }

            const optionItem = Array.from(list.querySelectorAll('[role="option"]'))
                .find((item) => item.getAttribute('data-value') === value);

            if (optionItem) {
                setSelection(optionItem);
            }
        };

        initializeSelection();

        document.addEventListener('click', (event) => {
            if (!picker.contains(event.target)) {
                closeList();
            }
        });

        document.addEventListener('keydown', (event) => {
            if (event.key === 'Escape' && picker.getAttribute('data-open') === 'true') {
                closeList();
                trigger.focus();
            }
        });
    })();

(function () {
        const categoryFilter = document.getElementById('filter-category');
        const supplierFilter = document.getElementById('filter-supplier');
        const unitFilter = document.getElementById('filter-unit');
        const searchInput = document.getElementById('filter-search');
        const list = document.querySelector('.ingredient-list');

        if (!list) {
            return;
        }

        const cards = Array.from(list.querySelectorAll('.ingredient-card[data-ingredient-id]'));
        if (cards.length === 0) {
            return;
        }

        let emptyState = list.querySelector('[data-filter-empty]');
        if (!emptyState) {
            emptyState = document.createElement('li');
            emptyState.className = 'ingredient-empty';
            emptyState.setAttribute('role', 'status');
            emptyState.dataset.filterEmpty = 'true';
            emptyState.textContent = 'Nenhum ingrediente corresponde aos filtros selecionados.';
            emptyState.hidden = true;
            list.appendChild(emptyState);
        }

        const emptyStateMessages = {
            pt: 'Nenhum ingrediente corresponde aos filtros selecionados.',
            en: 'No ingredients match the selected filters.'
        };

        emptyState.setAttribute('data-i18n-en', emptyStateMessages.en);

        const updateEmptyStateLanguage = (lang) => {
            const value = lang === 'en' ? emptyStateMessages.en : emptyStateMessages.pt;
            emptyState.textContent = value;
        };

        const lastUpdateElements = cards
            .map((card) => card.querySelector('[data-ingredient-last-update]'))
            .filter((element) => element);

        const updateLastUpdateLanguage = (lang) => {
            lastUpdateElements.forEach((element) => {
                const value = lang === 'en'
                    ? element.getAttribute('data-last-update-en')
                    : element.getAttribute('data-last-update-pt');
                if (value != null) {
                    element.textContent = value;
                }
            });
        };

        const sanitize = (value) => {
            if (!value) {
                return '';
            }

            return value
                .toString()
                .normalize('NFD')
                .replace(/[\u0300-\u036f]/g, '')
                .toLowerCase();
        };

        const applyFilters = () => {
            const categoryValue = sanitize(categoryFilter?.value || 'all');
            const supplierValue = sanitize(supplierFilter?.value || 'all');
            const unitValue = sanitize(unitFilter?.value || 'all');
            const searchValue = sanitize(searchInput?.value || '');

            let visibleCount = 0;

            cards.forEach((card) => {
                const data = card.dataset;

                const matchesCategory = categoryValue === 'all'
                    || sanitize(data.ingredientCategoryId) === categoryValue;

                const matchesSupplier = supplierValue === 'all'
                    || sanitize(data.ingredientSupplier) === supplierValue;

                const matchesUnit = unitValue === 'all'
                    || sanitize(data.ingredientUnit) === unitValue;

                const matchesSearch = !searchValue
                    || [
                        sanitize(data.ingredientName),
                        sanitize(data.ingredientSupplier),
                        sanitize(data.ingredientCategoryLabel),
                        sanitize(data.ingredientCategoryLabelEn),
                    ].some((field) => field.includes(searchValue));

                const isVisible = matchesCategory && matchesSupplier && matchesUnit && matchesSearch;

                card.hidden = !isVisible;

                if (isVisible) {
                    visibleCount += 1;
                }
            });

            emptyState.hidden = visibleCount !== 0;
        };

        [categoryFilter, supplierFilter, unitFilter].forEach((filter) => {
            if (filter) {
                filter.addEventListener('change', applyFilters);
            }
        });

        if (searchInput) {
            searchInput.addEventListener('input', applyFilters);
        }

        applyFilters();

        subscribeToLanguageChanges((lang) => {
            updateEmptyStateLanguage(lang);
            updateLastUpdateLanguage(lang);
        });
    })();

(function () {
        const ingredientForm = document.querySelector('.ingredient-form');
        if (!ingredientForm) {
            return;
        }

        const cards = Array.from(document.querySelectorAll('.ingredient-card[data-ingredient-id]'));
        const idInput = ingredientForm.querySelector('input[name="IngredientInput.Id"]');
        const nameInput = ingredientForm.querySelector('#ingredient-name');
        const categorySelect = ingredientForm.querySelector('#ingredient-category');
        const supplierSelect = ingredientForm.querySelector('#ingredient-supplier');
        const totalInput = ingredientForm.querySelector('#ingredient-total');
        const quantityInput = ingredientForm.querySelector('#ingredient-quantity');
        const notesInput = ingredientForm.querySelector('#ingredient-notes');
        const unitInputs = Array.from(ingredientForm.querySelectorAll('input[name="IngredientInput.Unit"]'));
        const formTitle = document.getElementById('ingredient-form-title');
        const submitLabel = ingredientForm.querySelector('[data-ingredient-submit-label]');
        const newButton = document.querySelector('[data-ingredient-new]');

        const clearEditingState = () => {
            cards.forEach(card => card.removeAttribute('data-editing'));
        };

        const applyFormModeLanguage = (lang) => {
            const mode = ingredientForm.dataset.mode || 'create';
            if (formTitle) {
                const titleText = getModeDatasetValue(formTitle, 'modeTitle', mode, lang);
                setTextContent(formTitle, titleText);
            }

            if (submitLabel) {
                const submitText = getModeDatasetValue(submitLabel, 'modeSubmit', mode, lang);
                setTextContent(submitLabel, submitText);
            }
        };

        const setFormMode = (mode) => {
            ingredientForm.dataset.mode = mode;
            applyFormModeLanguage(getCurrentLanguage());
        };

        const ensureOptionExists = (select, value, label) => {
            if (!select || !value) {
                return;
            }

            const exists = Array.from(select.options).some(option => option.value === value);
            if (!exists) {
                const option = new Option(label || value, value, true, true);
                select.add(option);
            }
            select.value = value;
        };

        const populateFormFromCard = (card) => {
            const data = card.dataset;

            if (idInput) {
                idInput.value = data.ingredientId || '';
            }

            if (nameInput) {
                nameInput.value = data.ingredientName || '';
            }

            if (categorySelect) {
                const categoryValue = data.ingredientCategoryId || '';
                if (categoryValue) {
                    ensureOptionExists(categorySelect, categoryValue, data.ingredientCategoryLabel || '');
                } else {
                    categorySelect.value = '';
                }
            }

            if (supplierSelect) {
                const supplierValue = data.ingredientSupplier || '';
                if (supplierValue) {
                    ensureOptionExists(supplierSelect, supplierValue, supplierValue);
                } else {
                    supplierSelect.value = '';
                }
            }

            if (totalInput) {
                totalInput.value = data.ingredientTotal || '';
            }

            if (quantityInput) {
                quantityInput.value = data.ingredientQuantity || '';
            }

            if (notesInput) {
                notesInput.value = data.ingredientNotes || '';
            }

            if (unitInputs.length > 0) {
                const unitValue = data.ingredientUnit;
                if (unitValue) {
                    let hasMatch = false;
                    unitInputs.forEach((input) => {
                        const match = input.value === unitValue;
                        input.checked = match;
                        if (match) {
                            hasMatch = true;
                        }
                    });

                    if (!hasMatch && unitInputs[0]) {
                        unitInputs[0].checked = true;
                    }
                }
            }
        };

        const focusAndScroll = (element) => {
            ingredientForm.scrollIntoView({ behavior: 'smooth', block: 'start' });
            if (element) {
                element.focus();
                if (typeof element.select === 'function') {
                    element.select();
                }
            }
        };

        cards.forEach((card) => {
            const editButton = card.querySelector('[data-ingredient-edit]');
            if (editButton) {
                editButton.addEventListener('click', () => {
                    populateFormFromCard(card);
                    clearEditingState();
                    card.setAttribute('data-editing', 'true');
                    setFormMode('edit');
                    focusAndScroll(nameInput);
                });
            }

            const priceButton = card.querySelector('[data-ingredient-price]');
            if (priceButton) {
                priceButton.addEventListener('click', () => {
                    populateFormFromCard(card);
                    clearEditingState();
                    card.setAttribute('data-editing', 'true');
                    setFormMode('price');
                    focusAndScroll(totalInput);
                });
            }
        });

        const resetState = () => {
            clearEditingState();
            setFormMode('create');
            if (idInput) {
                idInput.value = '';
            }
        };

        ingredientForm.addEventListener('reset', () => {
            window.setTimeout(() => {
                resetState();
            }, 0);
        });

        if (newButton) {
            newButton.addEventListener('click', () => {
                ingredientForm.reset();
                resetState();
                focusAndScroll(nameInput);
            });
        }

        if (idInput && idInput.value) {
            const activeCard = cards.find(card => card.dataset.ingredientId === idInput.value);
            if (activeCard) {
                clearEditingState();
                activeCard.setAttribute('data-editing', 'true');
            }
            setFormMode('edit');
        } else {
            setFormMode('create');
        }

        subscribeToLanguageChanges(applyFormModeLanguage);
    })();

    (function () {
        const suppliersSection = document.querySelector('[data-suppliers-section]');
        if (!suppliersSection) {
            return;
        }

        const form = suppliersSection.querySelector('[data-supplier-form]');
        const idInput = suppliersSection.querySelector('[data-supplier-id-input]');
        const nameInput = suppliersSection.querySelector('[data-supplier-name]');
        const infoInput = suppliersSection.querySelector('[data-supplier-info-input]');
        const formTitle = suppliersSection.querySelector('[data-supplier-form-title]');
        const submitLabel = suppliersSection.querySelector('[data-supplier-submit-label]');
        const successMessage = suppliersSection.querySelector('[data-supplier-success]');
        const errorMessage = suppliersSection.querySelector('[data-supplier-error]');
        const emptyState = suppliersSection.querySelector('[data-supplier-empty]');
        const list = suppliersSection.querySelector('[data-supplier-list]');
        const template = suppliersSection.querySelector('[data-supplier-template]');
        const newButton = suppliersSection.querySelector('[data-supplier-new]');
        const cancelButton = suppliersSection.querySelector('[data-supplier-cancel]');
        const supplierSelects = Array.from(document.querySelectorAll('#ingredient-supplier, #filter-supplier'));

        if (!form || !nameInput || !infoInput || !list || !template) {
            return;
        }

        const translations = {
            defaultInfo: {
                pt: 'Informações não registradas.',
                en: 'Information not provided.'
            },
            nameRequired: {
                pt: 'Informe o nome do fornecedor para continuar.',
                en: 'Enter the supplier name to continue.'
            },
            saveError: {
                pt: 'Não foi possível salvar o fornecedor. Tente novamente.',
                en: 'Unable to save the supplier. Please try again.'
            },
            cardError: {
                pt: 'Não foi possível criar o cartão do fornecedor.',
                en: 'Unable to create the supplier card.'
            },
            networkError: {
                pt: 'Não foi possível salvar o fornecedor. Verifique sua conexão e tente novamente.',
                en: 'Unable to save the supplier. Check your connection and try again.'
            },
            updateSuccess: {
                pt: 'Fornecedor "{name}" atualizado.',
                en: 'Supplier "{name}" updated.'
            },
            createSuccess: {
                pt: 'Fornecedor "{name}" adicionado.',
                en: 'Supplier "{name}" added.'
            }
        };

        let editingCard = null;

        const getTranslation = (key, lang, replacements) => {
            const entry = translations[key];
            if (!entry) {
                return '';
            }

            const template = entry[lang] || entry.pt;
            return formatTemplate(template, replacements);
        };

        const getDefaultInfo = (lang) => translations.defaultInfo[lang];

        const toggleEmptyState = () => {
            if (!emptyState) {
                return;
            }

            const hasItems = list.querySelectorAll('[data-supplier-card]').length > 0;
            if (hasItems) {
                emptyState.setAttribute('hidden', '');
                list.removeAttribute('hidden');
            } else {
                emptyState.removeAttribute('hidden');
                list.setAttribute('hidden', '');
            }
        };

        const clearFeedback = () => {
            if (successMessage) {
                successMessage.setAttribute('hidden', '');
                successMessage.textContent = '';
                delete successMessage.dataset.messageKey;
                delete successMessage.dataset.messageReplacements;
            }
            if (errorMessage) {
                errorMessage.setAttribute('hidden', '');
                errorMessage.textContent = '';
                delete errorMessage.dataset.messageKey;
                delete errorMessage.dataset.messageReplacements;
            }
        };

        const setFeedback = (element, key, replacements, fallback) => {
            if (!element) {
                return;
            }

            if (key) {
                const text = getTranslation(key, getCurrentLanguage(), replacements);
                setTextContent(element, text);
                element.dataset.messageKey = key;
                element.dataset.messageReplacements = JSON.stringify(replacements || {});
            } else {
                setTextContent(element, fallback || '');
                delete element.dataset.messageKey;
                delete element.dataset.messageReplacements;
            }

            element.removeAttribute('hidden');
        };

        const showSuccess = (key, replacements, fallback) => {
            setFeedback(successMessage, key, replacements, fallback);
            if (errorMessage) {
                errorMessage.setAttribute('hidden', '');
                errorMessage.textContent = '';
                delete errorMessage.dataset.messageKey;
                delete errorMessage.dataset.messageReplacements;
            }
        };

        const showError = (key, replacements, fallback) => {
            setFeedback(errorMessage, key, replacements, fallback);
            if (successMessage) {
                successMessage.setAttribute('hidden', '');
                successMessage.textContent = '';
                delete successMessage.dataset.messageKey;
                delete successMessage.dataset.messageReplacements;
            }
        };

        const updateFeedbackLanguage = (lang) => {
            const refresh = (element) => {
                if (!element || element.hasAttribute('hidden')) {
                    return;
                }

                const key = element.dataset.messageKey;
                if (!key) {
                    return;
                }

                let replacements = {};
                try {
                    replacements = JSON.parse(element.dataset.messageReplacements || '{}');
                } catch (error) {
                    replacements = {};
                }

                const text = getTranslation(key, lang, replacements);
                setTextContent(element, text);
            };

            refresh(successMessage);
            refresh(errorMessage);
        };

        const applySupplierFormMode = (lang) => {
            const mode = form.dataset.mode || 'create';
            if (formTitle) {
                const titleText = getModeDatasetValue(formTitle, 'supplierModeTitle', mode, lang);
                setTextContent(formTitle, titleText);
            }

            if (submitLabel) {
                const submitText = getModeDatasetValue(submitLabel, 'supplierModeSubmit', mode, lang);
                setTextContent(submitLabel, submitText);
            }
        };

        const setFormMode = (mode) => {
            form.dataset.mode = mode;
            applySupplierFormMode(getCurrentLanguage());
        };

        const upsertSelectOption = (previousValue, newValue, label) => {
            if (!newValue) {
                return;
            }

            supplierSelects.forEach((select) => {
                if (!select) {
                    return;
                }

                const options = Array.from(select.options);
                let option = null;

                if (previousValue) {
                    option = options.find(item => item.value === previousValue);
                }

                if (!option) {
                    option = options.find(item => item.value === newValue);
                }

                if (!option) {
                    option = new Option(label || newValue, newValue, false, false);
                    select.add(option);
                } else {
                    option.value = newValue;
                    option.textContent = label || newValue;
                }

                option.setAttribute('data-i18n-en', label || newValue);
            });
        };

        const resetEditingState = () => {
            list.querySelectorAll('[data-supplier-card]').forEach((card) => {
                card.removeAttribute('data-editing');
            });
            editingCard = null;
            setFormMode('create');
            if (idInput) {
                idInput.value = '';
            }
        };

        const populateForm = (card) => {
            const data = card.dataset;
            const infoPt = data.supplierInfo || '';
            const infoEn = data.supplierInfoEn || infoPt;
            const isDefault = data.supplierInfoDefault === 'true'
                || infoPt === translations.defaultInfo.pt
                || infoEn === translations.defaultInfo.en;

            nameInput.value = data.supplierName || '';
            infoInput.value = isDefault ? '' : infoPt;
            if (idInput) {
                idInput.value = data.supplierId || '';
            }
            editingCard = card;
            card.setAttribute('data-editing', 'true');
            setFormMode('edit');
            nameInput.focus();
            clearFeedback();
        };

        const buildCard = () => {
            const fragment = template.content.cloneNode(true);
            const card = fragment.querySelector('[data-supplier-card]');
            return card;
        };

        const updateCard = (card, value, label, infoPt, infoEn, isDefault) => {
            if (!card) {
                return;
            }

            card.dataset.supplierId = value;
            card.dataset.supplierName = label;
            card.dataset.supplierInfo = infoPt;
            card.dataset.supplierInfoEn = infoEn;

            if (isDefault) {
                card.dataset.supplierInfoDefault = 'true';
            } else {
                delete card.dataset.supplierInfoDefault;
            }

            const nameDisplay = card.querySelector('[data-supplier-name-display]');
            const infoDisplay = card.querySelector('[data-supplier-info-display]');

            if (nameDisplay) {
                nameDisplay.textContent = label;
            }

            if (infoDisplay) {
                if (isDefault) {
                    infoDisplay.dataset.defaultInfoEn = translations.defaultInfo.en;
                } else {
                    delete infoDisplay.dataset.defaultInfoEn;
                }

                const lang = getCurrentLanguage();
                infoDisplay.textContent = lang === 'en' ? (infoEn || infoPt) : infoPt;
            }
        };

        const updateSupplierCardsLanguage = (lang) => {
            list.querySelectorAll('[data-supplier-card]').forEach((card) => {
                const infoDisplay = card.querySelector('[data-supplier-info-display]');
                if (!infoDisplay) {
                    return;
                }

                const infoPt = card.dataset.supplierInfo || '';
                let infoEn = card.dataset.supplierInfoEn || infoPt;
                const isDefault = card.dataset.supplierInfoDefault === 'true'
                    || infoPt === translations.defaultInfo.pt
                    || infoEn === translations.defaultInfo.en;

                if (isDefault) {
                    card.dataset.supplierInfo = translations.defaultInfo.pt;
                    card.dataset.supplierInfoEn = translations.defaultInfo.en;
                    card.dataset.supplierInfoDefault = 'true';
                    infoDisplay.dataset.defaultInfoEn = translations.defaultInfo.en;
                    infoEn = translations.defaultInfo.en;
                } else {
                    delete infoDisplay.dataset.defaultInfoEn;
                }

                const text = lang === 'en' ? (infoEn || infoPt) : infoPt;
                setTextContent(infoDisplay, text);
            });
        };

        const handleSubmit = async (event) => {
            event.preventDefault();

            const name = nameInput.value.trim();
            const info = infoInput.value.trim();

            clearFeedback();

            if (!name) {
                showError('nameRequired');
                nameInput.focus();
                return;
            }

            const formData = new FormData(form);
            formData.set('SupplierInput.Name', name);
            formData.set('SupplierInput.Notes', info);

            const supplierId = editingCard ? (editingCard.dataset.supplierId || '') : '';
            formData.set('SupplierInput.Id', supplierId);

            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest',
                    },
                });

                const result = await response.json().catch(() => null);

                if (!response.ok || !result || result.success !== true) {
                    if (result && result.error) {
                        showError(null, null, result.error);
                    } else {
                        showError('saveError');
                    }
                    return;
                }

                const payload = result.supplier || {};
                const value = String(payload.id ?? supplierId ?? '');
                const supplierName = payload.name || name;
                const notesFromPayload = typeof payload.notes === 'string' ? payload.notes.trim() : '';
                const infoSource = notesFromPayload || info;
                const infoPt = infoSource || translations.defaultInfo.pt;
                const infoEn = infoSource || translations.defaultInfo.en;
                const isDefaultInfo = !infoSource;

                if (editingCard) {
                    const previousName = editingCard.dataset.supplierName || '';
                    updateCard(editingCard, value, supplierName, infoPt, infoEn, isDefaultInfo);
                    upsertSelectOption(previousName, supplierName, supplierName);
                    showSuccess('updateSuccess', { name: supplierName });
                } else {
                    const card = buildCard();
                    if (!card) {
                        showError('cardError');
                        return;
                    }

                    updateCard(card, value, supplierName, infoPt, infoEn, isDefaultInfo);
                    list.appendChild(card);
                    upsertSelectOption('', supplierName, supplierName);
                    toggleEmptyState();
                    showSuccess('createSuccess', { name: supplierName });
                }

                form.reset();
                if (idInput) {
                    idInput.value = '';
                }
                resetEditingState();
            } catch (error) {
                showError('networkError');
            }
        };

        const handleReset = () => {
            window.setTimeout(() => {
                clearFeedback();
                resetEditingState();
                toggleEmptyState();
            }, 0);
        };

        list.addEventListener('click', (event) => {
            const editButton = event.target.closest('[data-supplier-edit]');
            if (!editButton) {
                return;
            }

            const card = editButton.closest('[data-supplier-card]');
            if (!card) {
                return;
            }

            list.querySelectorAll('[data-supplier-card]').forEach((item) => {
                if (item !== card) {
                    item.removeAttribute('data-editing');
                }
            });

            populateForm(card);
        });

        if (newButton) {
            newButton.addEventListener('click', () => {
                form.reset();
                clearFeedback();
                resetEditingState();
                nameInput.focus();
            });
        }

        if (cancelButton) {
            cancelButton.addEventListener('click', () => {
                form.reset();
                if (idInput) {
                    idInput.value = '';
                }
                clearFeedback();
                resetEditingState();
            });
        }

        form.addEventListener('submit', handleSubmit);
        form.addEventListener('reset', handleReset);

        toggleEmptyState();
        subscribeToLanguageChanges((lang) => {
            applySupplierFormMode(lang);
            updateFeedbackLanguage(lang);
            updateSupplierCardsLanguage(lang);
        });
    })();
