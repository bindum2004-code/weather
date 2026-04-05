/* ══════════════════════════════════════════════
   Nimbus — Client-side JS for Blazor Server
   Canvas BG · Custom Cursor · Theme persistence
   ══════════════════════════════════════════════ */

// ── Canvas Background ─────────────────────────
(function () {
  const cv = document.getElementById('bgCanvas');
  if (!cv) return;
  const cx = cv.getContext('2d');
  let W, H;
  let dark = document.documentElement.getAttribute('data-theme') !== 'light';

  const resize = () => { W = cv.width = window.innerWidth; H = cv.height = window.innerHeight; };
  resize();
  window.addEventListener('resize', resize);

  // Grid dots
  const COLS = 28, ROWS = 18;
  const dots = [];
  for (let r = 0; r < ROWS; r++)
    for (let c = 0; c < COLS; c++)
      dots.push({ c, r, phase: Math.random() * Math.PI * 2, speed: .008 + Math.random() * .012, lit: Math.random() < .06 });

  // Meteors
  class Meteor {
    constructor() { this.reset(true); }
    reset(init = false) {
      this.x = Math.random() * W * 1.4;
      this.y = init ? Math.random() * H : -20;
      this.vx = -2 - Math.random() * 4;
      this.vy = 3 + Math.random() * 6;
      this.len = 60 + Math.random() * 120;
      this.a = .5 + Math.random() * .5;
      this.w = .5 + Math.random() * 1.2;
    }
    tick() { this.x += this.vx; this.y += this.vy; if (this.y > H + 40) this.reset(); }
    draw() {
      const ang = Math.atan2(this.vy, this.vx);
      const tx = this.x - Math.cos(ang) * this.len;
      const ty = this.y - Math.sin(ang) * this.len;
      const g = cx.createLinearGradient(tx, ty, this.x, this.y);
      g.addColorStop(0, 'rgba(245,166,35,0)');
      g.addColorStop(.7, `rgba(245,166,35,${this.a * .35})`);
      g.addColorStop(1, `rgba(255,220,140,${this.a})`);
      cx.beginPath(); cx.moveTo(tx, ty); cx.lineTo(this.x, this.y);
      cx.strokeStyle = g; cx.lineWidth = this.w; cx.lineCap = 'round'; cx.stroke();
    }
  }
  const meteors = Array.from({ length: 10 }, () => new Meteor());

  // Amber pulses
  class Pulse {
    constructor() { this.reset(); }
    reset() {
      this.x = Math.random() * W; this.y = Math.random() * H;
      this.r = 0; this.maxR = 40 + Math.random() * 80;
      this.a = .3 + Math.random() * .3; this.speed = .4 + Math.random() * .5;
      this.delay = Math.random() * 200;
    }
    tick() { if (this.delay > 0) { this.delay--; return; } this.r += this.speed; if (this.r > this.maxR) this.reset(); }
    draw() {
      if (this.delay > 0 || this.r <= 0) return;
      const alpha = this.a * (1 - this.r / this.maxR);
      cx.beginPath(); cx.arc(this.x, this.y, this.r, 0, Math.PI * 2);
      cx.strokeStyle = `rgba(245,166,35,${alpha * .5})`; cx.lineWidth = 1; cx.stroke();
    }
  }
  const pulses = Array.from({ length: 8 }, () => new Pulse());

  // Weather glyphs
  const GLYPHS = ['⛅', '☁️', '🌧️', '❄️', '☀️', '⛈️', '🌫️', '🌤️'];
  class Glyph {
    constructor() { this.reset(); }
    reset() {
      this.e = GLYPHS[Math.floor(Math.random() * GLYPHS.length)];
      this.x = Math.random() * W; this.y = H + 50;
      this.s = 12 + Math.random() * 20;
      this.vy = -.2 - .4 * Math.random(); this.vx = (Math.random() - .5) * .3;
      this.a = .03 + Math.random() * .05; this.wb = Math.random() * Math.PI * 2;
    }
    tick() { this.wb += .018; this.x += this.vx + Math.sin(this.wb) * .25; this.y += this.vy; if (this.y < -50) this.reset(); }
    draw() {
      cx.save(); cx.globalAlpha = this.a;
      cx.font = `${this.s}px serif`; cx.textAlign = 'center'; cx.textBaseline = 'middle';
      cx.fillText(this.e, this.x, this.y); cx.restore();
    }
  }
  const glyphs = Array.from({ length: 16 }, () => { const g = new Glyph(); g.y = Math.random() * H; return g; });

  function loop() {
    cx.clearRect(0, 0, W, H);
    const cw = W / COLS, rh = H / ROWS;

    // Grid dots
    dots.forEach(d => {
      d.phase += d.speed;
      const fade = .5 + .5 * Math.sin(d.phase);
      const base = dark ? `rgba(58,58,69,${fade * .4})` : `rgba(176,165,144,${fade * .3})`;
      const lit  = dark ? `rgba(245,166,35,${fade * .8})` : `rgba(180,100,0,${fade * .6})`;
      cx.beginPath(); cx.arc(d.c * cw + cw / 2, d.r * rh + rh / 2, d.lit ? 1.8 : .9, 0, Math.PI * 2);
      cx.fillStyle = d.lit ? lit : base; cx.fill();
    });

    // Grid lines
    if (dark) {
      cx.strokeStyle = 'rgba(42,42,50,0.2)'; cx.lineWidth = .4;
      for (let c = 0; c <= COLS; c++) { cx.beginPath(); cx.moveTo(c * cw, 0); cx.lineTo(c * cw, H); cx.stroke(); }
      for (let r = 0; r <= ROWS; r++) { cx.beginPath(); cx.moveTo(0, r * rh); cx.lineTo(W, r * rh); cx.stroke(); }
    }

    if (dark) {
      pulses.forEach(p => { p.tick(); p.draw(); });
      meteors.forEach(m => { m.tick(); m.draw(); });
    }

    glyphs.forEach(g => { g.tick(); g.draw(); });
    requestAnimationFrame(loop);
  }
  loop();

  new MutationObserver(() => { dark = document.documentElement.getAttribute('data-theme') !== 'light'; })
    .observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
})();

// ── Custom Cursor ─────────────────────────────
(function () {
  const dot  = document.querySelector('.c-dot');
  const ring = document.querySelector('.c-ring');
  if (!dot || !ring) return;
  let mx = 0, my = 0, rx = 0, ry = 0;
  document.addEventListener('mousemove', e => { mx = e.clientX; my = e.clientY; });
  (function move() {
    rx += (mx - rx) * .18; ry += (my - ry) * .18;
    dot.style.left  = mx + 'px'; dot.style.top  = my + 'px';
    ring.style.left = rx + 'px'; ring.style.top = ry + 'px';
    requestAnimationFrame(move);
  })();
  document.querySelectorAll('a, button, .sb-chip, .pill, .wc-card, .qchip, .qb-btn, .followup-chip, .fday, .metric-tile').forEach(el => {
    el.addEventListener('mouseenter', () => { ring.style.width = '44px'; ring.style.height = '44px'; ring.style.borderColor = 'rgba(245,166,35,.8)'; });
    el.addEventListener('mouseleave', () => { ring.style.width = '28px'; ring.style.height = '28px'; ring.style.borderColor = 'rgba(245,166,35,.5)'; });
  });
})();

// ── Theme Toggle (called from Blazor) ─────────
window.nimbusSetTheme = (dark) => {
  var theme = dark ? 'dark' : 'light';
  document.documentElement.setAttribute('data-theme', theme);
  document.documentElement.classList.toggle('dark', dark);
  document.documentElement.classList.toggle('light', !dark);
  localStorage.setItem('nim-theme', theme);
};

window.nimbusGetTheme = () => {
  var stored = localStorage.getItem('nim-theme');
  if (stored === 'light' || stored === 'dark') return stored;
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
};

// ── Scroll chat to bottom ─────────────────────
window.scrollToBottom = (id) => {
  const el = document.getElementById(id);
  if (el) el.scrollTop = el.scrollHeight;
};


// View count persistence
window.nimbusViewCount = parseInt(localStorage.getItem('nimbusViews') || '0') || 0;

window.incrementViewCount = () => {
  window.nimbusViewCount++;
  localStorage.setItem('nimbusViews', window.nimbusViewCount.toString());
};

window.getViewCount = () => window.nimbusViewCount;

window.getGeolocation = () => new Promise((resolve, reject) => {
  if (!navigator.geolocation) return reject('Geolocation is not supported by this browser');
  navigator.geolocation.getCurrentPosition(
    pos => resolve({ lat: pos.coords.latitude, lon: pos.coords.longitude }),
    err => {
      const msgs = {
        1: 'Location permission was denied — please allow location access in your browser settings and try again',
        2: 'Location information is currently unavailable — please try again or enter your city manually',
        3: 'Location request timed out — please check your connection and try again'
      };
      reject(msgs[err.code] || err.message || 'Could not determine your location');
    },
    { timeout: 10000, maximumAge: 300000, enableHighAccuracy: false }
  );
});

// ── Mobile: scroll input into view on focus ───────────────
(function () {
  document.addEventListener('focusin', e => {
    if (e.target.matches('.message-input, .chat-ta')) {
      setTimeout(() => {
        e.target.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
      }, 320);
    }
  });
})();

// ── Auto-resize textarea ──────────────────────────────────
window.autoResizeTextarea = (el) => {
  if (!el) return;
  el.style.height = 'auto';
  el.style.height = Math.min(el.scrollHeight, 120) + 'px';
};

// ── Toast helper (callable from Blazor) ──────────────────
window.showToast = (msg, type = 'info') => {
  const existing = document.getElementById('nim-toast');
  if (existing) existing.remove();
  const toast = document.createElement('div');
  toast.id = 'nim-toast';
  toast.style.cssText = `
    position:fixed; bottom:3rem; left:50%; transform:translateX(-50%) translateY(20px);
    background:var(--bg2); border:1px solid var(--border2);
    color:var(--text); font-family:var(--f-mono); font-size:.72rem;
    letter-spacing:.06em; padding:.6rem 1.2rem; border-radius:999px;
    box-shadow:0 8px 32px rgba(0,0,0,.25); z-index:9999;
    opacity:0; transition:all .3s cubic-bezier(.22,1,.36,1);
    pointer-events:none; white-space:nowrap;
    border-left: 3px solid ${type === 'error' ? 'var(--rose)' : type === 'success' ? 'var(--teal)' : 'var(--amber)'};
  `;
  toast.textContent = msg;
  document.body.appendChild(toast);
  requestAnimationFrame(() => {
    toast.style.opacity = '1';
    toast.style.transform = 'translateX(-50%) translateY(0)';
  });
  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateX(-50%) translateY(10px)';
    setTimeout(() => toast.remove(), 400);
  }, 3500);
};
