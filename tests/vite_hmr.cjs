const { chromium } = require('playwright');
const assert = require('node:assert/strict');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await context.newPage();
  const documents = [];
  const sockets = [];
  page.on('request', request => { if (request.resourceType() === 'document') documents.push(request.url()); });
  page.on('websocket', socket => sockets.push(socket.url()));
  try {
    const response = await page.goto('http://127.0.0.1:5173/Tours/Details/1');
    assert.equal(response?.status(), 200, 'Vite must serve a nested SPA route directly');
    await page.getByRole('heading', { level: 1 }).waitFor();
    assert.match(await page.getByRole('heading', { level: 1 }).innerText(), /S\u00e0i G\u00f2n/);
    await page.waitForTimeout(300);
    assert.equal(sockets.some(url => url.startsWith('ws://127.0.0.1:5173')), true, 'Vite HMR websocket was not opened');
    const beforeNavigation = documents.length;
    await page.getByRole('link', { name: '\u0110i\u1ec3m \u0111\u1ebfn', exact: true }).first().click();
    await page.waitForURL('**/Destinations');
    assert.equal(documents.length, beforeNavigation, 'Vite client navigation loaded another document');
    const session = await context.request.get('http://127.0.0.1:5173/spa-api/session');
    assert.equal(session.status(), 200, 'Vite did not proxy BFF requests to Web');
    const reload = await page.reload();
    assert.equal(reload?.status(), 200, 'Vite did not refresh a nested SPA route');
    console.log('PASS: Vite dev server, nested-route fallback, BFF proxy, client routing and HMR websocket.');
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
