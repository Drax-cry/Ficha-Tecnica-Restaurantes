(() => {
    const header = document.querySelector('[data-nav-root]');
    if (!header) {
        return;
    }

    const toggle = header.querySelector('.nav-toggle');
    const panel = header.querySelector('.nav-panel');

    if (!toggle || !panel) {
        return;
    }

    header.setAttribute('data-nav-enhanced', 'true');

    const closePanel = () => {
        panel.classList.remove('is-open');
        toggle.classList.remove('is-active');
        toggle.setAttribute('aria-expanded', 'false');
    };

    toggle.addEventListener('click', () => {
        const isOpen = panel.classList.toggle('is-open');
        toggle.classList.toggle('is-active', isOpen);
        toggle.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
    });

    panel.addEventListener('click', (event) => {
        const link = event.target.closest('a');
        if (link) {
            closePanel();
        }
    });

    header.addEventListener('keyup', (event) => {
        if (event.key === 'Escape') {
            closePanel();
            toggle.focus();
        }
    });

    window.addEventListener('resize', closePanel);
})();

(() => {
    const body = document.body;
    if (!body) {
        return;
    }

    const rawUrl = (body.dataset && body.dataset.serverHealthUrl) ? body.dataset.serverHealthUrl.trim() : '';
    if (!rawUrl) {
        return;
    }

    let resolvedUrl;
    try {
        resolvedUrl = new URL(rawUrl, window.location.origin).href;
    } catch (error) {
        console.warn('[Server Connection] Ignoring invalid URL:', rawUrl, error);
        return;
    }

    const logResult = (response, durationMs) => {
        const info = {
            ok: response.ok,
            status: response.status,
            statusText: response.statusText,
            url: response.url,
            durationMs
        };

        const message = `[Server Connection] ${response.ok ? 'Success' : 'Completed with status ' + response.status} in ${durationMs}ms`;
        (response.ok ? console.log : console.warn)(message, info);
    };

    const logError = (error, durationMs) => {
        console.error(`[Server Connection] Failed to reach ${resolvedUrl} after ${durationMs}ms`, error);
    };

    const probeServer = async () => {
        const start = performance.now();

        try {
            const response = await fetch(resolvedUrl, { cache: 'no-store' });
            const durationMs = Math.round(performance.now() - start);
            logResult(response, durationMs);
        } catch (error) {
            const durationMs = Math.round(performance.now() - start);
            logError(error, durationMs);
        }
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', probeServer, { once: true });
    } else {
        probeServer();
    }
})();
