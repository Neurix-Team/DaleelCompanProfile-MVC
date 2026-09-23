// ═══════════════════════════════════════════
// PUBLIC SITE HEADER
// Scroll state, hide-on-scroll, progress line, sliding nav
// indicator, mobile menu and back-to-top.
// ═══════════════════════════════════════════
(function () {
    'use strict';

    const header = document.getElementById('main-header');
    if (!header) return;

    const nav = header.querySelector('.site-nav');
    const indicator = header.querySelector('.site-nav__indicator');
    const menuBtn = document.getElementById('mobile-menu-btn');
    const mobileNav = document.getElementById('mobile-nav');
    const progressBar = document.getElementById('scroll-progress-bar');
    const toTop = document.getElementById('to-top');
    const isRtl = document.documentElement.dir === 'rtl';

    // ── Scroll state ──────────────────────────
    let lastY = window.scrollY;
    let ticking = false;

    function onScroll() {
        const y = window.scrollY;
        const scrolled = y > 12 || header.hasAttribute('data-solid');
        header.dataset.scrolled = scrolled ? 'true' : 'false';

        // Hide when scrolling down past the hero, show when scrolling up
        // lastY only moves once the threshold is crossed, so slow drags still register.
        const menuOpen = mobileNav && mobileNav.classList.contains('is-open');
        if (y <= 320) {
            header.dataset.hidden = 'false';
            lastY = y;
        } else if (y > lastY + 6) {
            if (!menuOpen) header.dataset.hidden = 'true';
            lastY = y;
        } else if (y < lastY - 6) {
            header.dataset.hidden = 'false';
            lastY = y;
        }

        if (progressBar) {
            const max = document.documentElement.scrollHeight - window.innerHeight;
            progressBar.style.transform = 'scaleX(' + (max > 0 ? Math.min(y / max, 1) : 0) + ')';
        }

        if (toTop) toTop.classList.toggle('is-visible', y > 700);
        ticking = false;
    }

    window.addEventListener('scroll', function () {
        if (!ticking) {
            ticking = true;
            requestAnimationFrame(onScroll);
        }
    }, { passive: true });
    onScroll();

    // Reveal the header when keyboard focus moves into it
    header.addEventListener('focusin', function () { header.dataset.hidden = 'false'; });

    // ── Sliding nav indicator ─────────────────
    if (nav && indicator) {
        const links = Array.from(nav.querySelectorAll('.site-nav__link'));
        const active = nav.querySelector('.site-nav__link.is-active');

        function moveTo(link, instant) {
            if (!link) {
                indicator.style.opacity = '0';
                return;
            }
            if (instant) indicator.style.transition = 'none';
            indicator.style.width = link.offsetWidth + 'px';
            indicator.style.transform = 'translateX(' + link.offsetLeft + 'px)';
            indicator.style.opacity = '1';
            if (instant) {
                indicator.getBoundingClientRect();
                indicator.style.transition = '';
            }
        }

        links.forEach(function (link) {
            link.addEventListener('mouseenter', function () { moveTo(link); });
            link.addEventListener('focus', function () { moveTo(link); });
        });
        nav.addEventListener('mouseleave', function () { moveTo(active); });

        function reset() { moveTo(active, true); }
        // Fonts change link widths, so measure once they are ready
        if (document.fonts && document.fonts.ready) document.fonts.ready.then(reset);
        window.addEventListener('resize', reset);
        reset();
        nav.classList.add('is-ready');
    }

    // ── Mobile menu ───────────────────────────
    if (menuBtn && mobileNav) {
        const labelOpen = isRtl ? 'فتح القائمة' : 'Open menu';
        const labelClose = isRtl ? 'إغلاق القائمة' : 'Close menu';

        function setMenu(open) {
            mobileNav.classList.toggle('is-open', open);
            mobileNav.setAttribute('aria-hidden', open ? 'false' : 'true');
            menuBtn.setAttribute('aria-expanded', open ? 'true' : 'false');
            menuBtn.setAttribute('aria-label', open ? labelClose : labelOpen);
            header.classList.toggle('menu-open', open);
            if (open) header.dataset.hidden = 'false';
        }

        menuBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            setMenu(!mobileNav.classList.contains('is-open'));
        });

        mobileNav.addEventListener('click', function (e) {
            if (e.target.closest('a')) setMenu(false);
        });

        document.addEventListener('click', function (e) {
            if (mobileNav.classList.contains('is-open') && !header.contains(e.target)) setMenu(false);
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && mobileNav.classList.contains('is-open')) {
                setMenu(false);
                menuBtn.focus();
            }
        });

        window.matchMedia('(min-width: 1200px)').addEventListener('change', function (mq) {
            if (mq.matches) setMenu(false);
        });
    }

    // ── Card spotlight ────────────────────────
    // Tags the large content cards on public pages and feeds them the cursor position.
    if (window.matchMedia('(hover: hover) and (prefers-reduced-motion: no-preference)').matches) {
        const selector = '.glass-card, [class*="rounded-3xl"], [class*="rounded-[2rem]"], [class*="rounded-2xl"][class*="p-6"]';
        const main = document.querySelector('.site-main');
        const cards = main ? Array.from(main.querySelectorAll(selector)).filter(function (el) {
            if (el.matches('input, select, textarea, button, form') || el.querySelector('form, input, textarea')) return false;
            if (el.parentElement && el.parentElement.closest('.fx-card')) return false;
            const r = el.getBoundingClientRect();
            return r.width >= 200 && r.height >= 120;
        }) : [];

        cards.forEach(function (card) {
            if (getComputedStyle(card).position === 'static') card.style.position = 'relative';
            card.classList.add('fx-card');
            card.addEventListener('pointermove', function (e) {
                const r = card.getBoundingClientRect();
                card.style.setProperty('--mx', (e.clientX - r.left) + 'px');
                card.style.setProperty('--my', (e.clientY - r.top) + 'px');
            });
        });
    }

    // ── Back to top ───────────────────────────
    if (toTop) {
        toTop.addEventListener('click', function () {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }
})();
