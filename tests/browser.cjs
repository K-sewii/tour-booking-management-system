const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  const root = 'http://localhost:5081';
  const screenshots = 'artifacts/previews';
  fs.mkdirSync(screenshots, { recursive: true });

  await page.goto(root);
  await page.screenshot({ path: screenshots + '/web-home-desktop.png', fullPage: true });
  assert.match(await page.locator('h1').innerText(), /Đi gần/);

  await page.goto(root + '/Tours');
  await page.locator('select[name=region]').selectOption({ label: 'Vũng Tàu' });
  await page.getByRole('button', { name: 'Áp dụng bộ lọc' }).click();
  assert.equal(await page.locator('.tour-card').count(), 1);

  await page.goto(root + '/Tours/Details/1');
  await page.locator('#guest-count').fill('3');
  assert.match(await page.locator('#estimate-value').innerText(), /2.070.000/);
  await page.getByRole('link', { name: 'Tùy chỉnh lịch trình' }).click();
  assert.equal(await page.locator('#selected-places li').count(), 2);
  await page.getByRole('button', { name: 'Đưa Chợ Bến Thành lên', exact: true }).click();
  assert.match(await page.locator('#selected-places li').first().innerText(), /Chợ Bến Thành/);
  await page.locator('input[name=Name]').fill('Cuối tuần kiểm tra');
  await page.getByRole('button', { name: 'Lưu lịch trình' }).click();
  await page.getByRole('link', { name: 'Đăng nhập để lưu' }).click();
  await page.waitForURL('**/Account/Login?**');
  await page.screenshot({ path: screenshots + '/web-login-desktop.png', fullPage: true });
  assert.equal(await page.locator('.auth-form h1').innerText(), 'Chào bạn trở lại.');
  assert.equal(await page.locator('[name=Email]').count(), 1);
  assert.equal(await page.locator('[name=Password]').count(), 1);
  assert.equal(await page.locator('text=Demo@123').count(), 0);
  assert.equal(await page.locator('[name=Email]').evaluate(element => document.activeElement === element), true, 'Login must focus Email.');
  await page.keyboard.press('Tab');
  assert.equal(await page.locator('[name=Password]').evaluate(element => document.activeElement === element), true, 'Login tab order must put Password after Email.');
  await page.keyboard.press('Tab');
  assert.equal(await page.locator('[data-auth-submit]').evaluate(element => document.activeElement === element), true, 'Login tab order must put submit after Password.');
  await page.getByRole('button', { name: 'Đăng nhập' }).click();
  assert.equal(await page.locator('#Email-error').innerText(), 'Vui lòng nhập email');
  assert.equal(await page.locator('#Password-error').innerText(), 'Vui lòng nhập mật khẩu');
  assert.equal(await page.locator('[name=Email]').evaluate(element => document.activeElement === element), true, 'Login must focus its first invalid field.');

  await page.goto(root + '/Account/Login');
  await page.locator('[name=Email]').fill('khong-hop-le');
  await page.locator('[name=Password]').click();
  assert.equal(await page.locator('#Email-error').innerText(), 'Email không đúng định dạng.');

  await page.goto(root + '/Account/Register');
  assert.equal(await page.locator('[name=Name]').evaluate(element => document.activeElement === element), true, 'Register must focus Name.');
  assert.equal(await page.locator('[data-auth-submit]').isDisabled(), true, 'Register submit must be disabled until terms are accepted.');
  await page.locator('input[type="checkbox"][name="TermsAccepted"]').check();
  assert.equal(await page.locator('[data-auth-submit]').isDisabled(), false, 'Register submit must enable after accepting terms.');
  await page.locator('[name=PhoneNumber]').fill('090abc123456789012345');
  assert.equal(await page.locator('[name=PhoneNumber]').inputValue(), '090123456789012', 'Phone input must retain digits only and stop at 15 digits.');
  await page.locator('[name=Name]').fill('   ');
  await page.locator('[name=Email]').click();
  assert.equal(await page.locator('#Name-error').innerText(), 'Vui lòng nhập họ tên.');

  const mobile = await context.newPage();
  await mobile.setViewportSize({ width: 390, height: 844 });
  mobile.on('pageerror', error => errors.push(error.message));
  for (const path of ['/', '/Tours', '/Tours/Details/1', '/Destinations', '/Destinations/Details/1', '/Planner', '/Account/Login', '/Account/Register']) {
    await mobile.goto(root + path);
    assert.equal(await mobile.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true, 'horizontal overflow ' + path);
  }
  await mobile.goto(root);
  await mobile.screenshot({ path: screenshots + '/web-home-mobile.png', fullPage: true });
  await mobile.getByRole('button', { name: 'Mở menu' }).click();
  assert.equal(await mobile.locator('#navigation').isVisible(), true);
  assert.deepEqual(errors, []);
  await browser.close();
  console.log('PASS: browser public routes, filters, planner draft, SQL-auth login UI, and mobile menu.');
})().catch(error => { console.error(error); process.exit(1); });
