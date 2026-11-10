const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');

(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    const page = await context.newPage();
    const root = 'http://localhost:5081';
    const images = 'artifacts/previews';
    fs.mkdirSync(images, { recursive: true });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    async function login(target, email) {
      await target.goto(root + '/Account/Login?returnUrl=%2FAccount%2FProfile');
      await target.locator('#Email').fill(email);
      await target.locator('#Password').fill('ProfilePass2026');
      await target.locator('[data-auth-submit]').click();
      await target.waitForURL('**/Account/Profile');
    }
    await login(page, process.env.SCR21_EMAIL);
    assert.equal(await page.locator('h1').innerText(), 'Hồ sơ tài khoản');
    assert.equal(await page.locator('.auth-art').count(), 0);
    assert.equal(await page.locator('.account-sidebar nav a').count(), 6);
    for (const width of [1440, 1024, 768, 390]) {
      await page.setViewportSize({ width, height: width === 390 ? 844 : 1000 });
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true, 'Overflow at '+width);
      const regions = await page.locator('.account-layout').evaluate(element => {
        const a = element.children[0].getBoundingClientRect();
        const b = element.children[1].getBoundingClientRect();
        return { sideBySide: a.right <= b.left, stacked: a.bottom <= b.top };
      });
      assert.equal(width > 640 ? regions.sideBySide : regions.stacked, true, 'Responsive regions at '+width);
      await page.screenshot({ path: `${images}/scr21-profile-${width}.png`, fullPage: true });
    }
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.locator('#Name').focus();
    await page.keyboard.press('Tab');
    assert.equal(await page.locator('#Email').evaluate(el => el === document.activeElement), true);
    await page.keyboard.press('Tab');
    assert.equal(await page.locator('#PhoneNumber').evaluate(el => el === document.activeElement), true);
    await page.keyboard.press('Tab');
    assert.equal(await page.locator('[data-profile-submit]').evaluate(el => el === document.activeElement), true);
    await page.keyboard.press('Tab');
    assert.equal(await page.locator('[data-profile-cancel]').evaluate(el => el === document.activeElement), true);

    await page.locator('#Name').fill(' ');
    await page.locator('#PhoneNumber').fill('09abc');
    await page.getByRole('button', { name: 'Lưu thay đổi' }).click();
    assert.equal(await page.locator('#Name-error').innerText(), 'Vui lòng nhập họ tên.');
    assert.equal(await page.evaluate(() => document.activeElement.id), 'Name', 'Submit must focus the first invalid field.');
    assert.match(await page.locator('#PhoneNumber-error').innerText(), /chữ số/);
    await page.screenshot({ path: `${images}/scr21-validation-error.png`, fullPage: true });

    await page.getByRole('link', { name: 'Hủy thay đổi' }).click();
    assert.equal(await page.locator('#Name').inputValue(), 'Khách kiểm thử SCR21');
    assert.equal(await page.locator('#PhoneNumber').inputValue(), '');
    await page.locator('#Name').fill('Giữ thông tin đã nhập');
    await page.locator('#Email').fill(process.env.SCR21_DUPLICATE);
    await page.locator('#PhoneNumber').fill('0901234567');
    await page.getByRole('button', { name: 'Lưu thay đổi' }).click();
    await page.waitForLoadState('networkidle');
    assert.match(await page.locator('#Email-error').innerText(), /Email đã được sử dụng/);
    assert.equal(await page.locator('#Name').inputValue(), 'Giữ thông tin đã nhập');
    assert.equal(await page.locator('#PhoneNumber').inputValue(), '0901234567');
    assert.equal(await page.locator('#Email').evaluate(el => el === document.activeElement), true);
    await page.screenshot({ path: `${images}/scr21-duplicate-email-desktop.png`, fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    await page.screenshot({ path: `${images}/scr21-error-mobile.png`, fullPage: true });
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.locator('#Name').fill('Nguyễn Minh Anh');
    await page.locator('#Email').fill(process.env.SCR21_RENAMED);
    await page.locator('#PhoneNumber').fill('012345678901234');
    // Pause native navigation after the real submit handler updates the UI.
    // Resume with the same native MVC form (including antiforgery and load proof).
    await page.evaluate(() => {
      document.querySelector('[data-profile-form]').addEventListener('submit', event => event.preventDefault(), { once: true });
    });
    await page.getByRole('button', { name: 'Lưu thay đổi' }).click();
    await page.getByRole('button', { name: 'Đang lưu…' }).waitFor();
    assert.equal(await page.locator('[data-profile-submit]').isDisabled(), true);
    assert.equal(await page.locator('[data-profile-form]').getAttribute('aria-busy'), 'true');
    assert.equal(await page.locator('.validation-summary-errors').isVisible(), false, 'Retry must clear the previous error while saving.');
    await page.screenshot({ path: `${images}/scr21-saving.png`, fullPage: true });
    await page.evaluate(() => HTMLFormElement.prototype.submit.call(document.querySelector('[data-profile-form]')));
    await page.locator('.account-message--success').waitFor();
    assert.equal(await page.locator('#Name').inputValue(), 'Nguyễn Minh Anh');
    const header = page.locator('header a[href="/Account/Profile"]');
    assert.equal((await header.innerText()).trim(), 'Hồ sơ');
    await page.screenshot({ path: `${images}/scr21-success-desktop.png`, fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    await page.screenshot({ path: `${images}/scr21-success-mobile.png`, fullPage: true });
    await page.reload();
    assert.equal(await page.locator('#PhoneNumber').inputValue(), '012345678901234');
    await page.locator('#PhoneNumber').fill('');
    await page.getByRole('button', { name: 'Lưu thay đổi' }).click();
    await page.locator('.account-message--success').waitFor();
    await page.reload();
    assert.equal(await page.locator('#PhoneNumber').inputValue(), '');
    // Cancel must fetch API data, including changes made since the form was opened.
    const auth = await context.request.post('http://localhost:5080/api/auth/login', { data: { email: process.env.SCR21_RENAMED, password: 'ProfilePass2026' } });
    const { token } = await auth.json();
    await context.request.put('http://localhost:5080/api/auth/profile', { headers: { Authorization: 'Bearer '+token }, data: { name: 'Dữ liệu mới từ API', email: process.env.SCR21_RENAMED, phoneNumber: '00012' } });
    await page.locator('#Name').fill('Chưa lưu');
    const writes = [];
    page.on('request', request => { if (request.method() === 'POST' && request.url().endsWith('/Account/Profile')) writes.push(request.url()); });
    await page.getByRole('link', { name: 'Hủy thay đổi' }).click();
    assert.equal(await page.locator('#Name').inputValue(), 'Dữ liệu mới từ API');
    assert.equal(await page.locator('#PhoneNumber').inputValue(), '00012');
    assert.equal(writes.length, 0);
    // A separate no-JS browser logs in again with the changed email.
    const noJs = await browser.newContext({ javaScriptEnabled: false, viewport: { width: 1024, height: 900 } });
    const plain = await noJs.newPage();
    await login(plain, process.env.SCR21_RENAMED);
    assert.equal(await plain.locator('#Name').inputValue(), 'Dữ liệu mới từ API');
    await plain.locator('#Name').fill('A');
    await plain.getByRole('button', { name: 'Lưu thay đổi' }).click();
    assert.match(await plain.locator('#Name-error').innerText(), /2 đến 100/);
    await plain.locator('#Name').fill('Lưu khi tắt JavaScript');
    await plain.getByRole('button', { name: 'Lưu thay đổi' }).click();
    await plain.locator('.account-message--success').waitFor();
    assert.equal(await plain.locator('#Name').inputValue(), 'Lưu khi tắt JavaScript');
    assert.deepEqual(errors, []);
    await noJs.close();
    await context.close();
    console.log('PASS: SCR21 browser; 1440/1024/768/390, keyboard, validation, 409, saving/success, reload, clear phone, API cancel, claims, changed-email login, no-JS validation/save.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exit(1); });
