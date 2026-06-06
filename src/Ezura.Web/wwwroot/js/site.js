// ═══════════════════════════════════════════════════════════════
// EZURA FURNITURE — SITE JS
// ═══════════════════════════════════════════════════════════════

(function () {
  'use strict';

  // ── Header scroll effect ───────────────────────────────────────
  const header = document.getElementById('site-header');
  if (header) {
    window.addEventListener('scroll', () => {
      header.classList.toggle('scrolled', window.scrollY > 40);
    }, { passive: true });
  }

  // ── Search overlay ─────────────────────────────────────────────
  const searchToggle  = document.getElementById('search-toggle');
  const searchOverlay = document.getElementById('search-overlay');
  const searchClose   = document.getElementById('search-close');

  searchToggle?.addEventListener('click', () => {
    searchOverlay.classList.add('active');
    searchOverlay.querySelector('input')?.focus();
  });
  searchClose?.addEventListener('click', () => searchOverlay.classList.remove('active'));
  document.addEventListener('keydown', e => {
    if (e.key === 'Escape') searchOverlay?.classList.remove('active');
  });

  // ── Mobile menu ────────────────────────────────────────────────
  const mobileToggle = document.getElementById('mobile-toggle');
  const navMenu      = document.getElementById('nav-menu');
  mobileToggle?.addEventListener('click', () => {
    navMenu?.classList.toggle('mobile-open');
    mobileToggle.classList.toggle('active');
  });

  // ── Cart count ─────────────────────────────────────────────────
  async function refreshCartCount() {
    try {
      const res  = await fetch('/cart/count');
      const data = await res.json();
      const badge = document.getElementById('cart-badge');
      if (badge) {
        badge.textContent = data.count;
        badge.style.display = data.count > 0 ? 'flex' : 'none';
      }
    } catch (_) {}
  }
  refreshCartCount();

  // ── Add to cart (global handler) ──────────────────────────────
  document.addEventListener('click', async e => {
    const btn = e.target.closest('[data-add-to-cart]');
    if (!btn) return;

    const productId = parseInt(btn.dataset.addToCart);
    const quantity  = parseInt(btn.dataset.qty || '1');

    btn.disabled = true;
    btn.textContent = 'Adding…';

    try {
      const res  = await fetch('/cart/add', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ productId, quantity, notes: null })
      });
      const data = await res.json();

      if (data.success) {
        showToast('Added to cart', 'success');
        refreshCartCount();
        btn.textContent = 'Added ✓';
        setTimeout(() => {
          btn.textContent = btn.dataset.label || 'Add to Cart';
          btn.disabled = false;
        }, 2000);
      } else {
        showToast(data.message || 'Could not add to cart', 'error');
        btn.textContent = btn.dataset.label || 'Add to Cart';
        btn.disabled = false;
      }
    } catch (_) {
      showToast('Network error', 'error');
      btn.textContent = btn.dataset.label || 'Add to Cart';
      btn.disabled = false;
    }
  });

  // ── Wishlist toggle ────────────────────────────────────────────
  document.addEventListener('click', async e => {
    const btn = e.target.closest('[data-wishlist]');
    if (!btn) return;

    const productId = btn.dataset.wishlist;
    try {
      const res  = await fetch(`/wishlist/toggle/${productId}`, { method: 'POST' });
      const data = await res.json();
      if (data.success) {
        btn.classList.toggle('wished', data.added);
        showToast(data.added ? 'Added to wishlist' : 'Removed from wishlist', 'success');
      }
    } catch (_) {}
  });

  // ── Currency switcher ──────────────────────────────────────────
  window.setCurrency = function (code) {
    localStorage.setItem('ezura_currency', code);
    document.documentElement.dataset.currency = code;
    document.getElementById('current-currency').textContent = code;
    convertAllPrices(code);
  };

  async function convertAllPrices(toCurrency) {
    const elements = document.querySelectorAll('[data-price-egp]');
    if (!elements.length) return;

    try {
      const res   = await fetch(`/api/v1/currency?t=${Date.now()}`);
      const rates = await res.json();
      const rate  = rates.find(r => r.code === toCurrency)?.rate || 1;

      elements.forEach(el => {
        const egp       = parseFloat(el.dataset.priceEgp);
        const converted = (egp * rate).toFixed(2);
        el.textContent  = `${getSymbol(toCurrency)} ${Number(converted).toLocaleString()}`;
      });
    } catch (_) {}
  }

  function getSymbol(code) {
    const symbols = { EGP: 'ج.م', USD: '$', EUR: '€', GBP: '£', SAR: '﷼', AED: 'د.إ' };
    return symbols[code] || code;
  }

  // Restore saved currency
  const savedCurrency = localStorage.getItem('ezura_currency');
  if (savedCurrency && savedCurrency !== 'EGP') {
    setCurrency(savedCurrency);
  }

  // ── Cart quantity controls ─────────────────────────────────────
  document.addEventListener('click', e => {
    const btn = e.target.closest('.qty-btn');
    if (!btn) return;

    const input  = btn.parentElement.querySelector('.qty-input');
    const cartId = parseInt(input?.dataset.cartItemId || '0');
    let   qty    = parseInt(input?.value || '1');

    if (btn.dataset.action === 'inc') qty++;
    else qty = Math.max(1, qty - 1);

    if (input) input.value = qty;
    if (cartId) updateCartItem(cartId, qty);
  });

  async function updateCartItem(cartItemId, quantity) {
    try {
      await fetch('/cart/update', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ cartItemId, quantity })
      });
      refreshCartCount();
    } catch (_) {}
  }

  // ── Toast notification ─────────────────────────────────────────
  window.showToast = function (message, type = 'success') {
    const existing = document.getElementById('flash-toast');
    if (existing) existing.remove();

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.id        = 'flash-toast';
    toast.textContent = message;
    document.body.appendChild(toast);

    setTimeout(() => toast.remove(), 4500);
  };

  // ── Animate on scroll ──────────────────────────────────────────
  const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        entry.target.classList.add('visible');
        observer.unobserve(entry.target);
      }
    });
  }, { threshold: 0.12 });

  document.querySelectorAll('.animate-on-scroll').forEach(el => observer.observe(el));

  // ── Auto-dismiss flash toast ───────────────────────────────────
  const flashToast = document.getElementById('flash-toast');
  if (flashToast) setTimeout(() => flashToast.remove(), 4500);

  // ── Notification badge (SignalR) ───────────────────────────────
  if (typeof signalR !== 'undefined' && document.body.dataset.userId) {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications')
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveNotification', (notification) => {
      showToast(notification.title, 'success');
      const dot = document.getElementById('notif-dot');
      if (dot) dot.style.display = 'block';
    });

    connection.start().catch(() => {});
  }

  // ── Confirm dialogs ────────────────────────────────────────────
  document.addEventListener('click', e => {
    const btn = e.target.closest('[data-confirm]');
    if (!btn) return;
    if (!confirm(btn.dataset.confirm)) e.preventDefault();
  });

  // ── Image lazy loading ─────────────────────────────────────────
  if ('loading' in HTMLImageElement.prototype) {
    document.querySelectorAll('img[data-src]').forEach(img => {
      img.src = img.dataset.src;
    });
  }

})();
