// UI regression against the explicitly isolated local preview; no real login or booking.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = 'http://127.0.0.1:5181';
const out = path.resolve(__dirname, '../../artifacts/site-review/screenshots');
const notice = 'Vui lòng đăng nhập để xem và quản lý các chuyến đi của bạn.';

(async () => {
  fs.mkdirSync(out, { recursive: true });
  const browser = await chromium.launch({ headless: true });
  try {
    const widths = process.env.SITE_WIDTHS ? process.env.SITE_WIDTHS.split(',').map(Number) : [1440, 768, 390];
    for (const width of widths) {
      const context = await browser.newContext({ viewport: { width, height: 900 } });
      const page = await context.newPage();
      const errors = [], badImages = [];
      page.on('pageerror', e => errors.push(e.message));
      page.on('response', r => { if (r.url().includes('/images/') && r.status() >= 400) badImages.push(`${r.status()} ${r.url()}`); });
      await page.goto(root + '/', { waitUntil: 'networkidle' });
      assert.match(await page.locator('.hero h1').innerText(), /Đi gần thôi\.\s*Thấy nhiều hơn\./);
      assert.equal(await page.locator('.region-tile').count(), 5);
      const regionPhotos = {
        'TP. Hồ Chí Minh': 'hcm04-cho-ben-thanh.webp', 'Tây Ninh': 'tn01-nui-ba-den.webp',
        'Vũng Tàu': 'vt01-bai-sau-vung-tau.webp', 'Đồng Nai': 'dn04-thac-giang-dien.webp',
        'Bình Dương': 'bd05-khu-du-lich-dai-nam.webp',
      };
      for (const [name, filename] of Object.entries(regionPhotos))
        assert.ok((await page.locator('.region-tile').filter({ hasText: name }).locator('img').getAttribute('src')).endsWith(filename));
      for (const img of await page.locator('.region-tile img').all()) {
        await img.scrollIntoViewIfNeeded(); await img.evaluate(e => e.decode());
        assert.ok(await img.evaluate(e => e.naturalWidth > 0 && e.currentSrc.endsWith('.webp')));
      }
      const headline = await page.locator('.hero h1').evaluate(e => ({ scroll: e.scrollWidth, width: e.clientWidth, font: getComputedStyle(e.querySelector('em')).fontFamily }));
      assert.ok(headline.scroll <= headline.width + 1 && headline.font.includes('Segoe UI'));
      await page.screenshot({ path: path.join(out, `home-${width}.png`), fullPage: true });
      if (width === 1440) {
        await page.locator('.region-tile').filter({ hasText: 'Tây Ninh' }).click();
        await page.waitForURL(url => url.pathname === '/Destinations' && url.searchParams.get('region') === 'Tây Ninh');
        await page.locator('.destination-card').first().waitFor();
        assert.equal(await page.locator('.destination-card').count(), 5);
      }
      await page.goto(root + '/Destinations', { waitUntil: 'networkidle' });
      assert.equal(await page.locator('.destination-card').count(), 12);
      await page.screenshot({ path: path.join(out, `destinations-${width}.png`), fullPage: true });
      await page.goto(root + '/Destinations/Details/4', { waitUntil: 'networkidle' });
      const hero = page.locator('img.detail-hero'); await hero.waitFor(); await hero.evaluate(e => e.decode());
      assert.ok(await hero.evaluate(e => e.naturalWidth > 0 && e.currentSrc.endsWith('hcm04-cho-ben-thanh.webp')));
      await page.screenshot({ path: path.join(out, `detail-${width}.png`), fullPage: true });
      await page.goto(root + '/Planner', { waitUntil: 'networkidle' });
      const firstPlace = page.locator('.place-row').first(); await firstPlace.waitFor();
      assert.ok(await firstPlace.locator('img').evaluate(e => e.currentSrc.endsWith('.webp')));
      await page.screenshot({ path: path.join(out, `planner-${width}.png`), fullPage: true });
      if (width === 1440) {
        await page.getByRole('button', { name: 'Thêm Dinh Độc Lập' }).click();
        await page.getByRole('button', { name: 'Thêm Chợ Bến Thành' }).click();
        await page.getByRole('button', { name: 'Đưa Chợ Bến Thành lên' }).waitFor();
        await page.getByRole('button', { name: 'Đưa Chợ Bến Thành lên' }).click();
        await page.waitForFunction(() => document.querySelector('#selected-places li')?.textContent.includes('Chợ Bến Thành'));
        assert.match(await page.locator('#selected-places li').first().innerText(), /Chợ Bến Thành/);
        await page.reload({ waitUntil: 'networkidle' });
        await page.waitForFunction(() => document.querySelector('#selected-places li')?.textContent.includes('Chợ Bến Thành'));
        assert.match(await page.locator('#selected-places li').first().innerText(), /Chợ Bến Thành/);
        await page.getByRole('button', { name: 'Xóa Chợ Bến Thành' }).click();
        await page.getByRole('button', { name: 'Đưa Chợ Bến Thành lên' }).waitFor({ state: 'detached' });
        assert.equal(await page.getByRole('button', { name: 'Đưa Chợ Bến Thành lên' }).count(), 0);
      }
      await page.goto(root + '/Trips', { waitUntil: 'networkidle' });
      assert.match(page.url(), /\/Account\/Login\?returnUrl=%2FTrips/i);
      assert.equal(await page.getByText(notice).count(), 1);
      assert.ok(await page.locator('.account-message--info').isVisible());
      await page.screenshot({ path: path.join(out, `login-${width}.png`), fullPage: true });
      await page.goto(root + '/Account/Login', { waitUntil: 'networkidle' });
      assert.equal(await page.getByText(notice).count(), 0);
      await page.goto(root + '/Tours', { waitUntil: 'networkidle' });
      try { await page.locator('.tour-card').first().waitFor(); }
      catch (error) { console.error('Tours page state:', (await page.locator('main').innerText()).slice(0, 1500)); throw error; }
      assert.ok(await page.locator('.tour-card').count() > 0);
      assert.ok(await page.getByText('Dữ liệu tham khảo — chưa công bố').count() > 0);
      await page.screenshot({ path: path.join(out, `tours-${width}.png`), fullPage: true });
      await page.locator('.tour-card a').first().click();
      await page.locator('.detail-heading').waitFor();
      assert.ok(await page.getByText('Dữ liệu tham khảo — chưa công bố', { exact: true }).isVisible());
      assert.equal(await page.getByRole('button', { name: /đặt tour/i }).count(), 0);
      assert.equal(await page.getByRole('link', { name: /đăng nhập để đặt tour/i }).count(), 0);
      assert.deepEqual(errors, []); assert.deepEqual(badImages, []);
      await context.close();
      console.log(`PASS: pages, images, login notice and reference tours at ${width}px`);
    }
    const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await context.newPage();
    await page.route('**/spa-api/login', route => route.fulfill({ json: { user: { id: 1, name: 'Fixture', email: 'fixture@example.test', roles: ['Customer'] } } }));
    await page.route('**/spa-api/proxy/itineraries', route => route.fulfill({ json: [] }));
    await page.goto(root + '/', { waitUntil: 'networkidle' });
    await page.getByRole('link', { name: 'Lịch trình đã lưu' }).click();
    await page.getByText(notice).waitFor();
    await page.locator('#Email').fill('fixture@example.test');
    await page.locator('#Password').fill('FixturePassword!');
    await page.getByRole('button', { name: /Đăng nhập →/ }).click();
    await page.waitForURL('**/Trips');
    await context.close();
    console.log('PASS: simulated login returns to /Trips');
    const unsafe = await browser.newContext();
    const unsafePage = await unsafe.newPage();
    await unsafePage.route('**/spa-api/login', route => route.fulfill({ json: { user: { id: 1, name: 'Fixture', email: 'fixture@example.test', roles: ['Customer'] } } }));
    await unsafePage.goto(root + '/Account/Login?returnUrl=%2F%2Fevil.example', { waitUntil: 'networkidle' });
    await unsafePage.locator('#Email').fill('fixture@example.test');
    await unsafePage.locator('#Password').fill('FixturePassword!');
    await unsafePage.getByRole('button', { name: /Đăng nhập →/ }).click();
    await unsafePage.waitForURL(root + '/');
    await unsafe.close();
    console.log('PASS: external returnUrl is rejected');
    const fallback = await browser.newContext();
    const fallbackPage = await fallback.newPage();
    await fallbackPage.route('**/images/destinations/hcm04-cho-ben-thanh.webp', route => route.fulfill({ status: 404 }));
    await fallbackPage.goto(root + '/', { waitUntil: 'networkidle' });
    await fallbackPage.waitForFunction(() => document.querySelector('.region-tile img')?.getAttribute('src') === '/images/city.svg');
    await fallbackPage.goto(root + '/Planner', { waitUntil: 'networkidle' });
    const missing = fallbackPage.locator('.place-row').filter({ hasText: 'Chợ Bến Thành' }).locator('img');
    await missing.scrollIntoViewIfNeeded();
    await fallbackPage.waitForFunction(() => [...document.querySelectorAll('.place-row')].find(x => x.textContent.includes('Chợ Bến Thành'))?.querySelector('img')?.getAttribute('src') === '/images/city.svg');
    await fallback.close();
    console.log('PASS: region and Planner images fall back to SVG on image error');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
