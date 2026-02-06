(function () {
    const STORAGE_KEY = 'preferredLanguage';
    const SUPPORTED = new Set(['pt', 'en']);
    let currentLanguage = 'pt';

    function ensurePortugueseSnapshots(root) {
        const scope = root || document;

        if (scope !== document && scope instanceof Element && scope.hasAttribute('data-i18n-en')) {
            processElement(scope);
        }

        const elements = typeof scope.querySelectorAll === 'function'
            ? scope.querySelectorAll('[data-i18n-en]')
            : [];

        elements.forEach(processElement);
    }

    function processElement(element) {
        if (!element.hasAttribute('data-i18n-en') || element.hasAttribute('data-i18n-pt')) {
            return;
        }

        const target = element.getAttribute('data-i18n-target');
        let value = '';

        switch (target) {
            case 'html':
                value = element.innerHTML;
                break;
            case 'text':
                value = element.textContent;
                break;
            case null:
            case undefined:
                value = element.textContent;
                break;
            default:
                value = element.getAttribute(target) ?? '';
                break;
        }

        element.setAttribute('data-i18n-pt', value);
    }

    function setElementValue(element, value) {
        const target = element.getAttribute('data-i18n-target');

        if (target && target.includes(',')) {
            target.split(',').map(part => part.trim()).filter(Boolean).forEach((attribute) => {
                switch (attribute) {
                    case 'html':
                        element.innerHTML = value ?? '';
                        break;
                    case 'text':
                        element.textContent = value ?? '';
                        break;
                    default:
                        if (value == null) {
                            element.removeAttribute(attribute);
                        } else {
                            element.setAttribute(attribute, value);
                        }
                        break;
                }
            });
            return;
        }

        switch (target) {
            case 'html':
                element.innerHTML = value ?? '';
                break;
            case 'text':
                element.textContent = value ?? '';
                break;
            case null:
            case undefined:
                element.textContent = value ?? '';
                break;
            default:
                if (value == null) {
                    element.removeAttribute(target);
                } else {
                    element.setAttribute(target, value);
                }
                break;
        }
    }

    function updateLanguageSwitchers(lang) {
        document.querySelectorAll('[data-language-switcher]').forEach(switcher => {
            ensurePortugueseSnapshots(switcher);
            switcher.querySelectorAll('[data-language-option]').forEach(button => {
                const option = button.getAttribute('data-language-option');
                const isActive = option === lang;
                button.classList.toggle('is-active', isActive);
                button.setAttribute('aria-pressed', isActive ? 'true' : 'false');
            });
        });
    }

    function applyLanguage(lang) {
        if (!SUPPORTED.has(lang)) {
            lang = 'pt';
        }

        currentLanguage = lang;
        const html = document.documentElement;
        html.lang = lang === 'en' ? 'en' : 'pt-BR';
        html.setAttribute('data-language', lang);

        document.querySelectorAll('[data-i18n-en]').forEach(element => {
            const attributeName = lang === 'en' ? 'data-i18n-en' : 'data-i18n-pt';
            const value = element.getAttribute(attributeName) ?? element.getAttribute('data-i18n-pt');
            setElementValue(element, value);
        });

        updateLanguageSwitchers(lang);
        document.dispatchEvent(new CustomEvent('app:languagechange', {
            detail: { language: lang }
        }));
    }

    function setLanguage(lang) {
        if (!SUPPORTED.has(lang)) {
            lang = 'pt';
        }

        localStorage.setItem(STORAGE_KEY, lang);
        applyLanguage(lang);
    }

    function sync() {
        ensurePortugueseSnapshots();
        updateLanguageSwitchers(currentLanguage);
    }

    document.addEventListener('click', event => {
        const button = event.target.closest('[data-language-option]');
        if (!button) {
            return;
        }

        const lang = button.getAttribute('data-language-option');
        setLanguage(lang);
    });

    document.addEventListener('DOMContentLoaded', () => {
        ensurePortugueseSnapshots();
        const stored = localStorage.getItem(STORAGE_KEY);
        const lang = stored && SUPPORTED.has(stored) ? stored : 'pt';
        applyLanguage(lang);
    });

    window.appLanguage = {
        setLanguage,
        sync,
        get current() {
            return currentLanguage;
        }
    };
})();
