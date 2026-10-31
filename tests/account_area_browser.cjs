const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');

(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    const root = 'http://localhost:5081';
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.goto(root+'/Account/Login?returnUrl=%2FAccount%2FSecurity');
    await page.locator('#Email').fill(process.env.SCR21_RENAMED);
    await page.locator('#Password').fill('ProfilePass2026');
    await page.locator('[data-auth-submit]').click();
    await page.waitForURL('**/Account/Security');
    await page.locator('#account-title').waitFor();
    const dir = 'artifacts/previews';
    fs.mkdirSync(dir, { recursive: true });
    const paths = [
      ['/Account/Profile','account-refresh-profile'], ['/Trips','account-refresh-trips'],
      ['/Account/Security','scr22-security'], ['/Account/Payments','scr23-payments'],
      ['/Account/Favorites','scr24-favorites'], ['/Account/Notices','scr25-notices'],
    ];
    for (const [path, file] of paths) {
      await page.setViewportSize({ width: 1440, height: 1000 });
      await page.goto(root+path);
      assert.equal(await page.locator('#account-title').count(), 1, path+' heading');
      assert.equal(await page.locator('.account-sidebar nav a').count(), 6, path+' navigation');
      assert.equal(await page.locator('.account-sidebar nav a[aria-current="page"]').getAttribute('href'), path, path+' selected navigation');
      assert.equal((await page.locator('header a[href="/Account/Profile"]').innerText()).trim(), 'Hồ sơ');
      assert.equal(await page.getByRole('button', { name: 'Đăng xuất' }).count(), 1);
      for (const width of [1440, 1024, 768, 390]) {
        await page.setViewportSize({ width, height: width === 390 ? 844 : 1000 });
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true, path+' overflow at '+width);
        if (width === 1440 || width === 390) await page.screenshot({ path: `${dir}/${file}-${width}.png`, fullPage: true });
      }
    }
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.goto(root+'/Account/Profile');
    await page.locator('.account-sidebar nav a[href="/Trips"]').click();
    await page.waitForURL('**/Trips');
    assert.equal(await page.locator('.account-sidebar nav a[aria-current="page"]').getAttribute('href'), '/Trips');

    await page.goto(root+'/Account/Payments');
    const openChoice = page.locator('[data-payment-open]');
    const dialog = page.locator('#payment-choice-dialog');
    await openChoice.click();
    assert.equal(await dialog.evaluate(element => element.open), true);
    assert.equal(await page.locator('#methodCode').evaluate(element => element === document.activeElement), true);
    await page.locator('[data-payment-save]').click();
    assert.match(await page.locator('#payment-dialog-error').innerText(), /Vui lòng chọn/);
    await page.locator('[aria-label="Đóng hộp thoại"]').click();
    assert.equal(await dialog.evaluate(element => element.open), false);
    assert.equal(await openChoice.evaluate(element => element === document.activeElement), true);
    await openChoice.focus();
    await page.keyboard.press('Enter');
    assert.equal(await dialog.evaluate(element => element.open), true);
    await page.keyboard.press('Escape');
    assert.equal(await dialog.evaluate(element => element.open), false);
    assert.equal(await openChoice.evaluate(element => element === document.activeElement), true);
    await openChoice.click();
    await page.locator('#methodCode').selectOption('bank_transfer');
    await page.route('**/Account/Payments/Choice', route => route.abort());
    await page.locator('[data-payment-save]').click();
    await page.locator('#payment-dialog-error').filter({ hasText: 'Chưa kết nối' }).waitFor({ state: 'visible' });
    assert.match(await page.locator('#payment-dialog-error').innerText(), /Chưa kết nối/);
    await page.unroute('**/Account/Payments/Choice');
    await page.screenshot({ path: `${dir}/payment-choice-dialog-1440.png` });
    await page.setViewportSize({ width: 390, height: 844 });
    await page.locator('#payment-dialog-error').evaluate(element => { element.textContent = 'Lỗi kết nối '.repeat(80); });
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true, 'Long modal error overflow');
    await page.locator('#payment-dialog-error').evaluate(element => { element.textContent = 'Chưa kết nối được máy chủ. Vui lòng thử lại.'; });
    await page.screenshot({ path: `${dir}/payment-choice-dialog-390.png` });
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.locator('[data-payment-save]').click();
    await page.locator('#payment-choice-status').waitFor({ state: 'visible' });
    assert.match(await page.locator('#payment-choice-status').innerText(), /Chưa có khoản thanh toán nào/);
    await page.reload();
    assert.match(await page.locator('[data-payment-label]').innerText(), /chuyển khoản/);
    await page.screenshot({ path: `${dir}/payment-choice-saved-1440.png`, fullPage: true });
    assert.equal((await page.locator('[data-payment-open]').innerText()).trim(), 'Đổi');
    await page.locator('[data-payment-open]').click();
    await page.locator('#methodCode').selectOption('cash');
    await page.locator('[data-payment-save]').click();
    await page.locator('#payment-choice-status').waitFor({ state: 'visible' });
    await page.reload();
    assert.match(await page.locator('[data-payment-label]').innerText(), /tiền mặt/);
    await page.goto(root+'/Tours/Details/1');
    await page.getByRole('button', { name: 'Lưu tour yêu thích' }).click();
    await page.goto(root+'/Destinations/Details/2');
    await page.getByRole('button', { name: 'Lưu địa điểm yêu thích' }).click();
    await page.goto(root+'/Tours?q=núi');
    await page.goto(root+'/Account/Favorites');
    assert.equal(await page.locator('.account-list li').count(), 2);
    await page.screenshot({ path: `${dir}/scr24-favorites-saved.png`, fullPage: true });
    await page.getByRole('link', { name: 'Tìm kiếm gần đây' }).click();
    assert.match(await page.locator('.account-list').innerText(), /núi/);
    await page.getByRole('link', { name: 'Đã xem gần đây' }).click();
    assert.match(await page.locator('.account-list').innerText(), /Núi Bà Đen/);
    await page.goto(root+'/Account/Security');
    await page.locator('#CurrentPassword').fill('ProfilePass2026');
    await page.locator('#NewPassword').fill('AreaPass2026');
    await page.locator('#ConfirmNewPassword').fill('AreaPass2026');
    await page.getByRole('button', { name: 'Đổi mật khẩu' }).click();
    await page.waitForURL('**/Account/Login?**');
    assert.match(await page.locator('body').innerText(), /Đã đổi mật khẩu/);
    await page.locator('#Email').fill(process.env.SCR21_RENAMED);
    await page.locator('#Password').fill('AreaPass2026');
    await page.locator('[data-auth-submit]').click();
    await page.waitForURL('**/Account/Security');
    await page.goto(root+'/Account/Notices');
    assert.match(await page.locator('.account-list').innerText(), /Mật khẩu đã được đổi/);
    await page.screenshot({ path: `${dir}/scr25-notices-real-event.png`, fullPage: true });
    await page.getByRole('button', { name: 'Đánh dấu đã đọc' }).click();
    assert.match(await page.locator('.account-list').innerText(), /Đã đọc/);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.getByRole('button', { name: 'Mở menu' }).click();
    assert.equal(await page.locator('header a[href="/Account/Profile"]').isVisible(), true);
    await page.getByRole('button', { name: 'Đăng xuất' }).click();
    await page.waitForURL(root+'/');
    await page.getByRole('button', { name: 'Mở menu' }).click();
    assert.equal(await page.locator('header a[href="/Account/Profile"]').count(), 0);
    assert.equal(await page.locator('header a[href="/Account/Login"]').isVisible(), true);
    assert.deepEqual(errors, []);
    await context.close();
    console.log('PASS: account navigation/header at 1440/1024/768/390, payment choice modal keyboard/error/save/reload, favorites/history, password event and logout.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exit(1); });
