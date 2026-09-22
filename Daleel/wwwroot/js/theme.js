// ============================================================
// Daleel Theme Manager (FINAL CLEAN VERSION)
// ============================================================

(function () {
    'use strict';

    // ─────────────────────────────────────────────
    // 1. Fix old key + Apply theme BEFORE paint
    // ─────────────────────────────────────────────
    const oldTheme = localStorage.getItem('theme');
    if (oldTheme) {
        localStorage.setItem('daleel-theme', oldTheme);
        localStorage.removeItem('theme');
    }

    const saved = localStorage.getItem('daleel-theme');
    // Default is LIGHT — only go dark if user explicitly chose dark
    if (saved === 'dark') {
        document.documentElement.classList.add('dark');
    } else {
        document.documentElement.classList.remove('dark');
    }

    // ─────────────────────────────────────────────
    // 2. Private helper
    // ─────────────────────────────────────────────
    function syncIcon() {
        const icon = document.getElementById('theme-icon');
        if (!icon) return;

        icon.textContent = document.documentElement.classList.contains('dark')
            ? 'light_mode'
            : 'dark_mode';
    }

    // ─────────────────────────────────────────────
    // 3. Public API
    // ─────────────────────────────────────────────
    window.DaleelTheme = {
        toggle() {
            const isDark = document.documentElement.classList.toggle('dark');
            localStorage.setItem('daleel-theme', isDark ? 'dark' : 'light');

            // 🔥 أهم سطر في السيستم كله
            document.dispatchEvent(new Event('daleel:theme-changed'));

            syncIcon();
        },

        syncIcon,

        get isDark() {
            return document.documentElement.classList.contains('dark');
        }
    };

    // ─────────────────────────────────────────────
    // 4. Global functions (HTML buttons)
    // ─────────────────────────────────────────────
    window.toggleTheme = () => window.DaleelTheme.toggle();

    window.toggleDirection = function () {
        const html = document.documentElement;
        const isRTL = html.dir === 'rtl';

        html.dir = isRTL ? 'ltr' : 'rtl';
        html.lang = isRTL ? 'en' : 'ar';

        const btn = document.getElementById('lang-toggle');
        if (btn) btn.textContent = isRTL ? 'عربي' : 'EN';
    };

    // ─────────────────────────────────────────────
    // 5. Sync icon after header load
    // ─────────────────────────────────────────────
    document.addEventListener('daleel:header-ready', syncIcon);
    document.addEventListener('DOMContentLoaded', syncIcon);

})();