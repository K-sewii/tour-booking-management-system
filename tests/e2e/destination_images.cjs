// SCR06/SCR07 browser regression, using a local Web server and mocked BFF data.
// No API or database is contacted. Run after building Web and the SPA.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '../..');
const url = 'http://127.0.0.1:15387';
const webDir = path.join(root, 'src/Frontend/DongNamTrip.Web');
const executable = path.join(webDir, 'bin/Release/net10.0/DongNamTrip.Web.exe');
const output = path.join(root, 'artifacts/destination-photo-tests');
const base = { region: 'TP. Hồ Chí Minh', category: 'Văn hóa', address: 'Thử nghiệm', description: 'Fixture', visitMinutes: 60, illustration: 'city', status: 'Published' };
const fixtures = [
  { ...base, id: 1, name: 'Chợ Bến Thành', imageUrl: '/images/destinations/hcm04-cho-ben-thanh.webp' },
  { ...base, id: 2, name: 'Ảnh bị thiếu', imageUrl: '/images/destinations/missing.webp' },
  { ...base, id: 3, name: 'Chưa có ảnh', imageUrl: null },
];

(async () => {
  fs.mkdirSync(output, { recursive: true });
  // Refuse to use an existing listener, which might belong to another application.
  try { await fetch(url); throw new Error('Port 15387 already has a server'); }
  catch (error) { if (error.message === 'Port 15387 already has a server') throw error; }
  const web = spawn(executable, [], { cwd: webDir, env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development', ASPNETCORE_URLS: url, ApiBaseUrl: 'http://127.0.0.1:1/' }, stdio: 'ignore', windowsHide: true });
  let browser;
  try {
    for (let attempt = 0; attempt < 80; attempt++) {
      if (web.exitCode !== null) throw new Error('Web exited before becoming ready');
      try { if ((await fetch(url + '/images/city.svg')).ok) break; } catch { /* startup */ }
      if (attempt === 79) throw new Error('Web failed to start');
      await new Promise(resolve => setTimeout(resolve, 250));
    }
    browser = await chromium.launch({ headless: true });
    const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
    await page.route('**/spa-api/**', route => {
      const pathname = new URL(route.request().url()).pathname;
      let body;
      if (pathname === '/spa-api/session') body = { user: null };
      else if (pathname === '/spa-api/proxy/meta') body = { regions: [], categories: [] };
      else if (pathname === '/spa-api/proxy/destinations/search') body = { items: fixtures, total: fixtures.length, page: 1, pageSize: 20 };
      else if (/^\/spa-api\/proxy\/destinations\/[123]$/.test(pathname)) {
        const item = fixtures[Number(pathname.at(-1)) - 1];
        body = { ...item, openTime: null, closeTime: null, latitude: null, longitude: null, activities: [] };
      } else throw new Error('Unexpected BFF request: ' + pathname);
      return route.fulfill({ json: body });
    });
    async function assertImage(locator, expected) {
      await locator.waitFor();
      await page.waitForFunction(([selector, suffix]) => {
        const image = document.querySelector(selector);
        return image && image.complete && image.naturalWidth > 0 && image.getAttribute('src') === suffix;
      }, [await locator.getAttribute('alt').then(value => `img[alt="${value}"]`), expected]);
    }
    await page.goto(url + '/Destinations', { waitUntil: 'networkidle' });
    for (const [item, expected] of [[fixtures[0], fixtures[0].imageUrl], [fixtures[1], '/images/city.svg'], [fixtures[2], '/images/city.svg']])
      await assertImage(page.getByAltText('Ảnh ' + item.name), expected);
    await page.screenshot({ path: path.join(output, 'scr06.png'), fullPage: true });
    for (const [item, expected] of [[fixtures[0], fixtures[0].imageUrl], [fixtures[1], '/images/city.svg'], [fixtures[2], '/images/city.svg']]) {
      await page.goto(url + '/Destinations/Details/' + item.id, { waitUntil: 'networkidle' });
      await assertImage(page.locator('img.detail-hero'), expected);
      if (item.id === 1) await page.screenshot({ path: path.join(output, 'scr07.png'), fullPage: true });
    }
    const image = await page.request.get(url + fixtures[0].imageUrl);
    assert.equal(image.status(), 200);
    assert.match(image.headers()['content-type'], /image\/webp/i);
    console.log('PASS: SCR06 and SCR07 show WebP and fall back to SVG for absent or broken images; WebP HTTP 200.');
  } finally {
    if (browser) await browser.close();
    web.kill();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
