(() => {
    const overlay = document.querySelector('[data-loading-overlay]');
    if (!overlay) {
        return;
    }

    const body = document.body;
    const showOverlay = () => {
        if (overlay.getAttribute('data-visible') === 'true') {
            return false;
        }

        overlay.hidden = false;
        overlay.setAttribute('aria-busy', 'true');
        overlay.setAttribute('data-visible', 'true');
        if (body) {
            body.setAttribute('data-loading', 'true');
        }

        return true;
    };

    const hideOverlay = () => {
        overlay.hidden = true;
        overlay.setAttribute('aria-busy', 'false');
        overlay.removeAttribute('data-visible');
        if (body) {
            body.removeAttribute('data-loading');
        }
    };

    const disableSubmitButtons = (form) => {
        const submitters = form.querySelectorAll('button[type="submit"], input[type="submit"]');
        submitters.forEach((submitter) => {
            submitter.disabled = true;
            submitter.setAttribute('aria-disabled', 'true');
        });
    };

    const isModifiedClick = (event) => event.button !== 0 || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey;

    const isNavigationLink = (element) => {
        if (!(element instanceof HTMLAnchorElement)) {
            return false;
        }

        if (element.dataset.loadingOverlay === 'disabled') {
            return false;
        }

        if (element.hasAttribute('download')) {
            return false;
        }

        const href = (element.getAttribute('href') || '').trim();
        if (!href || href.startsWith('#') || href.toLowerCase().startsWith('javascript:')) {
            return false;
        }

        const target = (element.getAttribute('target') || '').toLowerCase();
        if (target === '_blank') {
            return false;
        }

        return true;
    };

    const preventInteractionDuringLoading = (event) => {
        if (!body || body.getAttribute('data-loading') !== 'true') {
            return;
        }

        const target = event.target instanceof Element
            ? event.target.closest('a, button, input, textarea, select, [role="button"], [data-loading-interactive]')
            : null;

        if (!target) {
            return;
        }

        event.preventDefault();
        event.stopImmediatePropagation();
    };

    document.addEventListener('click', preventInteractionDuringLoading, true);

    document.addEventListener('submit', (event) => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement)) {
            return;
        }

        if (form.dataset.loadingOverlay === 'disabled') {
            return;
        }

        if (form.dataset.loadingSubmitted === 'true') {
            event.preventDefault();
            return;
        }

        form.dataset.loadingSubmitted = 'true';
        disableSubmitButtons(form);
        showOverlay();
    }, true);

    document.addEventListener('click', (event) => {
        if (isModifiedClick(event)) {
            return;
        }

        const link = event.target instanceof Element ? event.target.closest('a[href]') : null;
        if (!isNavigationLink(link)) {
            return;
        }

        showOverlay();
    });

    window.addEventListener('pageshow', () => {
        hideOverlay();
        document.querySelectorAll('form[data-loading-submitted="true"]').forEach((form) => {
            form.removeAttribute('data-loading-submitted');
            const submitters = form.querySelectorAll('button[type="submit"], input[type="submit"]');
            submitters.forEach((submitter) => {
                submitter.disabled = false;
                submitter.removeAttribute('aria-disabled');
            });
        });
    });
})();
