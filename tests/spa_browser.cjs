const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const root = 'http://localhost:5081';
const screenshots = 'artifacts/previews/spa';
fs.mkdirSync(screenshots, { recursive: true });
const shot = async (page, name) => { await page.evaluate(() => scrollTo(0, 0)); await page.screenshot({ path: `${screenshots}/${name}.png`, fullPage: true }); };
const settle = async page => {
  await page.locator('main h1').first().waitFor();
  await page.waitForTimeout(180);
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 2), false, 'Horizontal overflow');
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await context.newPage();
  const errors = [];
  const documents = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('request', request => { if (request.resourceType() === 'document') documents.push(request.url()); });
  try {
    const missingBff = await context.request.get(root + '/spa-api/not-a-route');
    assert.equal(missingBff.status(), 404);
    assert.match(missingBff.headers()['content-type'] || '', /^application\/json/);
    assert.match((await missingBff.json()).message, /Không tìm thấy/);
    const missingAsset = await context.request.get(root + '/spa/assets/not-a-file.js', { headers: { Accept: 'text/html' } });
    assert.equal(missingAsset.status(), 404);
    assert.doesNotMatch(await missingAsset.text(), /<div id="root">/);
    const missingPage = await page.goto(root + '/Account/Not-A-Route');
    assert.equal(missingPage?.status(), 404);
    assert.equal(await page.locator('#root').count(), 0, 'Unknown server route must not receive the SPA document');
    await page.goto(root);
    await settle(page);
    assert.equal(await page.locator('#root').count(), 1);
    await shot(page, 'home-desktop');
    const initialDocuments = documents.length;
    await page.getByRole('link', { name: 'Khám phá tour', exact: true }).first().click();
    await settle(page);
    assert.equal(new URL(page.url()).pathname, '/Tours');
    assert.equal(documents.length, initialDocuments, 'React Router navigation fetched a new document');
    await shot(page, 'tours-desktop');
    await page.goto(root + '/Tours/Details/1');
    await settle(page);
    await shot(page, 'tour-detail-desktop');
    await page.goto(root + '/Destinations');
    await settle(page);
    await shot(page, 'destinations-desktop');
    await page.goto(root + '/Destinations/Details/1');
    await settle(page);
    await shot(page, 'destination-detail-desktop');
    await page.goto(root + '/Planner');
    await settle(page);
    await shot(page, 'planner-desktop');
    await page.goto(root + '/Trips');
    await page.waitForURL('**/Account/Login?returnUrl=%2FTrips');
    await settle(page);
    assert.equal(new URL(page.url()).searchParams.get('returnUrl'), '/Trips');
    await shot(page, 'login-desktop');

    const noCsrf = await context.request.post(root + '/spa-api/login', { data: { email: process.env.SPA_FIRST, password: 'ProfilePass2026' } });
    assert.equal(noCsrf.status(), 400, 'BFF mutation without antiforgery token must fail');
    await page.locator('#Email').fill(process.env.SPA_FIRST);
    await page.locator('#Password').fill('ProfilePass2026');
    await page.getByRole('button', { name: /Đăng nhập/ }).click();
    await page.waitForURL('**/Trips');
    await settle(page);
    const charsetProxy = await page.evaluate(async () => {
      const token = await fetch('/spa-api/csrf').then(response => response.json()).then(value => value.token);
      const response = await fetch('/spa-api/proxy/account/activity', {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json; charset=utf-8', RequestVerificationToken: token },
        body: JSON.stringify({ kind: 'search', itemId: null, query: 'kiểm tra charset' }),
      });
      return response.status;
    });
    assert.equal(charsetProxy, 204, 'BFF must forward JSON content types with charset');
    assert.equal(await page.getByRole('link', { name: 'Hồ sơ', exact: true }).count(), 1);
    await shot(page, 'trips-desktop');
    await page.getByRole('link', { name: 'Hồ sơ', exact: true }).click();
    await page.locator('#profile-name').waitFor();
    assert.equal(await page.locator('#profile-email').inputValue(), process.env.SPA_FIRST);
    await page.locator('#profile-name').fill('Đã hủy');
    await page.getByRole('button', { name: 'Hủy thay đổi' }).click();
    await page.waitForFunction(() => document.querySelector('#profile-name')?.value !== 'Đã hủy');
    await page.locator('#profile-phone').fill('012345678901234');
    await page.locator('#profile-name').fill('Khách SPA đã cập nhật');
    await page.getByRole('button', { name: 'Lưu thay đổi' }).click();
    await page.getByText('Đã lưu thay đổi hồ sơ tài khoản.').waitFor();
    await shot(page, 'profile-success-desktop');
    await page.reload();
    await page.locator('#profile-name').waitFor();
    assert.equal(await page.locator('#profile-phone').inputValue(), '012345678901234');
    await page.locator('#profile-email').fill(process.env.SPA_SECOND);
    await page.getByRole('button', { name: 'Lưu thay đổi' }).click();
    await page.getByText('Email đã được sử dụng.').first().waitFor();
    assert.equal(await page.locator('#profile-email').inputValue(), process.env.SPA_SECOND);
    await shot(page, 'profile-error-desktop');
    await page.getByRole('button', { name: 'Hủy thay đổi' }).click();
    await page.waitForFunction(email => document.querySelector('#profile-email')?.value === email, process.env.SPA_FIRST);
    await shot(page, 'profile-desktop');

    const routes = [
      ['/Account/Security', 'security'], ['/Account/Payments', 'payments'],
      ['/Account/Favorites', 'favorites'], ['/Account/Notices', 'notices'],
    ];
    for (const [route, name] of routes) {
      await page.goto(root + route);
      await settle(page);
      await shot(page, `${name}-desktop`);
    }
    await page.goto(root + '/Account/Payments');
    await page.getByRole('button', { name: 'Thêm phương thức' }).click();
    assert.equal(await page.locator('dialog').evaluate(dialog => dialog.open), true);
    assert.equal(await page.locator('#payment-method').evaluate(element => document.activeElement === element), true);
    await page.keyboard.press('Escape');
    assert.equal(await page.locator('dialog').evaluate(dialog => dialog.open), false);
    await page.getByRole('button', { name: 'Thêm phương thức' }).click();
    await page.getByRole('button', { name: 'Lưu', exact: true }).click();
    await page.getByText('Vui lòng chọn một phương thức ưu tiên.').waitFor();
    await page.locator('#payment-method').selectOption('bank_transfer');
    await shot(page, 'payment-dialog-desktop');
    await page.getByRole('button', { name: 'Lưu', exact: true }).click();
    await page.getByText(/Đã lưu lựa chọn ưu tiên/).waitFor();
    await page.reload();
    await page.getByRole('button', { name: 'Đổi', exact: true }).waitFor();
    assert.match(await page.locator('.account-payment-summary').innerText(), /chuyển khoản ngân hàng/i);
    assert.equal((await context.request.get(root + '/spa-api/proxy/account/payments')).status(), 200);

    await page.goto(root + '/Tours/Details/1');
    await page.getByRole('button', { name: 'Lưu tour yêu thích' }).click();
    await page.goto(root + '/Account/Favorites');
    await page.getByText(/Tour/).first().waitFor();
    await page.goto(root + '/Account/Favorites?tab=viewed');
    await settle(page);
    await page.goto(root + '/Account/Favorites?tab=search');
    await settle(page);
    await page.goto(root + '/Account/Notices');
    await page.getByRole('heading', { name: 'Thông báo và tùy chọn' }).waitFor();

    // The second account must not inherit the first account's SQL preference or collection.
    await page.getByRole('button', { name: 'Đăng xuất' }).click();
    await page.waitForURL(root + '/');
    assert.equal((await context.request.get(root + '/spa-api/proxy/account/payments')).status(), 401);
    await page.goto(root + '/Account/Login');
    await page.locator('#Email').fill(process.env.SPA_SECOND);
    await page.locator('#Password').fill('ProfilePass2026');
    await page.getByRole('button', { name: /Đăng nhập/ }).click();
    await page.waitForURL(root + '/');
    await page.goto(root + '/Account/Payments');
    await page.getByRole('button', { name: 'Thêm phương thức' }).waitFor();
    await page.goto(root + '/Account/Favorites');
    await page.getByText(/chưa lưu tour hoặc địa điểm/i).waitFor();
    assert.equal(await page.evaluate(() => Boolean(localStorage.getItem('token') || sessionStorage.getItem('token'))), false);

    // Registration through the SPA uses the same customer account API and cookie session.
    await page.getByRole('button', { name: 'Đăng xuất' }).click();
    await page.goto(root + '/Account/Register');
    await shot(page, 'register-desktop');
    await page.locator('#Name').fill('Khách SPA đăng ký');
    await page.locator('#Email').fill(process.env.SPA_REGISTERED);
    await page.locator('#PhoneNumber').fill('0901234567');
    await page.locator('#Password').fill('RegisterPass2026');
    await page.locator('#ConfirmPassword').fill('RegisterPass2026');
    await page.locator('#TermsAccepted').check();
    await page.getByRole('button', { name: /Đăng ký/ }).click();
    await page.waitForURL(root + '/');
    await page.goto(root + '/Account/Profile');
    await page.locator('#profile-email').waitFor();
    assert.equal(await page.locator('#profile-email').inputValue(), process.env.SPA_REGISTERED);
    await page.getByRole('button', { name: 'Đăng xuất' }).click();

    for (const width of [1024, 768, 390]) {
      await page.setViewportSize({ width, height: 900 });
      for (const [route, name] of [['/Tours', 'tours'], ['/Account/Login', 'login'], ['/Account/Register', 'register']]) {
        await page.goto(root + route); await settle(page); await shot(page, `${name}-${width}`);
      }
    }
    await page.setViewportSize({ width: 390, height: 900 });
    await page.goto(root + '/Account/Login');
    await page.locator('#Email').fill(process.env.SPA_FIRST);
    await page.locator('#Password').fill('ProfilePass2026');
    await page.getByRole('button', { name: /Đăng nhập/ }).click();
    await page.waitForURL(root + '/');
    await page.getByRole('button', { name: 'Mở menu' }).click();
    assert.equal(await page.locator('#navigation').evaluate(nav => nav.classList.contains('open')), true);
    await page.getByRole('button', { name: 'Mở menu' }).click();
    for (const width of [1440, 1024, 768, 390]) {
      await page.setViewportSize({ width, height: 900 });
      for (const [route, name] of [['/Account/Profile', 'profile'], ['/Account/Security', 'security'], ['/Account/Payments', 'payments'], ['/Account/Favorites', 'favorites'], ['/Account/Notices', 'notices'], ['/Trips', 'trips'], ['/Planner', 'planner']]) {
        await page.goto(root + route); await settle(page);
        if (route !== '/Planner') assert.equal(await page.locator('.account-sidebar nav a.active').count(), 1, 'Account menu active link');
        await shot(page, `${name}-${width}`);
      }
    }
    await page.setViewportSize({ width: 390, height: 900 });
    await page.goto(root + '/Account/Payments');
    await page.getByRole('button', { name: 'Đổi', exact: true }).click();
    await shot(page, 'payment-dialog-mobile');
    assert.deepEqual(errors, [], 'Browser runtime errors');
    console.log(`SPA browser PASS; ${documents.length} direct documents, client navigation confirmed; screenshots in ${screenshots}`);
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
