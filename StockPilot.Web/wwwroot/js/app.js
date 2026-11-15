const $ = (s, r = document) => r.querySelector(s);
const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const money = n => '₹' + Number(n).toLocaleString('en-IN');
const when = s => new Date(s.endsWith('Z') ? s : s + 'Z').toLocaleString();
const palette = ['#14213d', '#2a6f97', '#b5651d', '#4f772d', '#7b2cbf', '#c1121f'];

async function api(url, opts = {}) {
    const res = await fetch(url, {
        method: opts.method || 'GET', credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json' },
        body: opts.body ? JSON.stringify(opts.body) : undefined
    });
    if (res.status === 401) { location.href = '/Account/Login'; return; }
    const data = res.status === 204 ? null : await res.json().catch(() => null);
    if (!res.ok) throw new Error(data?.error || data?.title || 'Request failed (' + res.status + ')');
    return data;
}

// Coloured initials; a photo at /images/products/<SKU>.jpg covers them when it exists
function pimg(p) {
    return `<div class="pimg" style="background:${palette[p.id % palette.length]}">${esc(p.name.slice(0, 2).toUpperCase())}` +
        `<img src="/images/products/${encodeURIComponent(p.sku)}.jpg" alt="${esc(p.name)}" onerror="this.remove()"></div>`;
}

function toast(msg, ok = true) {
    const t = document.createElement('div');
    t.className = 'toast-x ' + (ok ? 'ok' : 'err'); t.textContent = msg;
    document.body.appendChild(t); setTimeout(() => t.remove(), 3500);
}