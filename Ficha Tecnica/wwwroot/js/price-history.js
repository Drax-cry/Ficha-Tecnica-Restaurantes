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

const normalizeMessage = (value) => typeof value === 'string' ? value.trim() : '';

const VALIDATION_MESSAGES = [
    {
        pt: 'Selecione um ingrediente.',
        en: 'Select an ingredient.'
    },
    {
        pt: 'Informe um valor válido para o preço.',
        en: 'Enter a valid price.'
    },
    {
        pt: 'As observações devem ter no máximo 500 caracteres.',
        en: 'Notes can have up to 500 characters.'
    },
    {
        pt: 'Ingrediente selecionado não foi encontrado.',
        en: 'The selected ingredient was not found.'
    },
    {
        pt: 'Não foi possível atualizar o preço do ingrediente. Tente novamente.',
        en: 'Unable to update the ingredient price. Try again.'
    },
    {
        pt: 'O período selecionado é inválido.',
        en: 'The selected period is invalid.'
    }
];

const VALIDATION_LOOKUP = VALIDATION_MESSAGES.reduce((map, entry) => {
    const portuguese = normalizeMessage(entry.pt);
    const english = normalizeMessage(entry.en);

    if (portuguese) {
        map.set(portuguese, entry);
    }

    if (english) {
        map.set(english, entry);
    }

    return map;
}, new Map());

const setTextContent = (element, value) => {
    if (!element || value == null) {
        return;
    }

    element.textContent = value;
};

const updateValidationElementLanguage = (element, lang) => {
    if (!element) {
        return;
    }

    const rawText = element.textContent;
    const text = normalizeMessage(rawText);

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
    const elements = new Set();

    ['.validation-summary li', '.validation-error'].forEach((selector) => {
        root.querySelectorAll(selector).forEach((element) => {
            elements.add(element);
        });
    });

    elements.forEach((element) => updateValidationElementLanguage(element, lang));
};

(function () {
    const page = document.querySelector('.price-history-page');
    if (!page) {
        return;
    }

    const targets = [
        page,
        page.querySelector('.price-history-form'),
        page.querySelector('.validation-summary')
    ].filter(Boolean);

    if (targets.length === 0) {
        return;
    }

    const applyLanguage = (lang) => {
        targets.forEach((target) => applyValidationTranslations(lang, target));
    };

    const observerConfig = { childList: true, subtree: true, characterData: true };

    targets.forEach((target) => {
        const observer = new MutationObserver(() => {
            applyLanguage(getCurrentLanguage());
        });

        observer.observe(target, observerConfig);
    });

    applyLanguage(getCurrentLanguage());
    subscribeToLanguageChanges(applyLanguage);
})();
