// PH2/PH4 UI smoke: UC05–UC10 on the customer Web, with UC17/UC18 prepared through staff API.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');

const web = 'http://localhost:5081';
const api = 'http://localhost:5080';
const email = process.env.NAM_UI_CUSTOMER_EMAIL;
const staffToken = process.env.NAM_UI_STAFF_TOKEN;
assert.ok(email && staffToken, 'browser_runner.py must provide isolated test accounts.');

(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    const screenshots = 'artifacts/previews';
    fs.mkdirSync(screenshots, { recursive: true });

    await page.goto(web + '/Account/Register');
    await page.locator('[name=Name]').fill('Khách kiểm thử PH2 PH4');
    await page.locator('[name=Email]').fill(email);
    await page.locator('[name=Password]').fill('NamUiPass!2026');
    await page.locator('[name=ConfirmPassword]').fill('NamUiPass!2026');
    await page.locator('input[type=checkbox][name=TermsAccepted]').check();
    await Promise.all([
      page.waitForURL(url => !url.pathname.startsWith('/Account/Register')),
      page.locator('[data-auth-submit]').click(),
    ]);

    await page.goto(web + '/Planner');
    const places = page.locator('.place-row');
    assert.ok(await places.count() >= 1, 'Planner needs a published SQL destination.');
    const placeName = await places.first().getAttribute('data-name');
    await places.first().locator('button.add-place').click();
    assert.equal(await page.locator('#selected-places li').count(), 1);
    await page.locator('input[name=Name]').fill('Smoke lịch trình Nam');
    await page.locator('input[name=Guests]').fill('3');
    await page.locator('.planner-stop-details input[type=text]').fill('Điểm dừng theo PH2');
    await page.locator('.planner-stop-details input[type=text]').dispatchEvent('change');
    await page.screenshot({ path: screenshots + '/nam-planner-draft.png', fullPage: true });
    await Promise.all([
      page.waitForURL('**/Trips'),
      page.locator('#save-itinerary').click(),
    ]);
    const draft = page.locator('#drafts .saved-trip').filter({ hasText: 'Smoke lịch trình Nam' });
    assert.equal(await draft.count(), 1, 'The saved draft should appear in My Trips.');
    assert.ok((await draft.innerText()).includes(placeName));
    await page.screenshot({ path: screenshots + '/nam-itinerary-saved.png', fullPage: true });

    await draft.getByRole('link', { name: /Mở và sửa lịch trình/ }).click();
    assert.equal(await page.locator('#selected-places li').count(), 1);
    assert.equal(await page.locator('.planner-stop-details input[type=text]').inputValue(), 'Điểm dừng theo PH2');
    await page.locator('input[name=Name]').fill('Smoke lịch trình Nam đã sửa');
    await Promise.all([
      page.waitForURL('**/Trips'),
      page.locator('#save-itinerary').click(),
    ]);
    const updatedDraft = page.locator('#drafts .saved-trip').filter({ hasText: 'Smoke lịch trình Nam đã sửa' });
    assert.equal(await updatedDraft.count(), 1, 'The edited itinerary should be persisted.');
    await updatedDraft.locator('[name=ServiceNeeds]').fill('Hướng dẫn viên và xe riêng');
    await Promise.all([
      page.waitForURL('**/Trips#quotes'),
      updatedDraft.getByRole('button', { name: 'Gửi yêu cầu báo giá' }).click(),
    ]);
    assert.equal(await page.locator('#quotes').isVisible(), true);
    assert.match(await page.locator('#quotes').innerText(), /Smoke lịch trình Nam đã sửa/);
    await page.screenshot({ path: screenshots + '/nam-private-request.png', fullPage: true });

    const headers = { Authorization: `Bearer ${staffToken}` };
    const requestsResponse = await page.request.get(api + '/api/staff/private-tour-requests', { headers });
    assert.equal(requestsResponse.status(), 200);
    const requests = await requestsResponse.json();
    const request = requests.find(item => item.contact === email && item.snapshot.name === 'Smoke lịch trình Nam đã sửa');
    assert.ok(request, 'Staff should see the submitted private tour request.');
    const verifyResponse = await page.request.post(api + `/api/staff/private-tour-requests/${request.id}/verifications`, {
      headers, data: { item: 'Hướng dẫn viên', supplier: 'Đối tác kiểm thử', result: 'Available' },
    });
    assert.equal(verifyResponse.status(), 200);
    const quoteResponse = await page.request.post(api + `/api/staff/private-tour-requests/${request.id}/quotes`, {
      headers, data: {
        expiresAt: new Date(Date.now() + 2 * 86400000).toISOString(),
        amountDue: 300000,
        cancellationTerms: 'Hủy trước 48 giờ.',
        lines: [{ description: 'Tour riêng', unit: 'khách', quantity: 3, unitPrice: 500000 }],
      },
    });
    assert.equal(quoteResponse.status(), 200);
    const issued = await quoteResponse.json();
    const quoteId = issued.quotes.at(-1).id;

    await page.reload();
    assert.equal(await page.locator('#quotes').isVisible(), true);
    await page.screenshot({ path: screenshots + '/nam-quote-issued.png', fullPage: true });
    const quote = page.locator('#quotes .quote-summary').filter({ has: page.locator(`input[name=quoteId][value="${quoteId}"]`) });
    assert.equal(await quote.count(), 1, 'The issued quote should be visible to its customer.');
    await Promise.all([
      page.waitForNavigation(),
      quote.getByRole('button', { name: 'Chấp nhận báo giá' }).click(),
    ]);
    assert.equal(new URL(page.url()).hash, '#quotes');
    assert.match(await page.locator('[role=status]').innerText(), /Đã chấp nhận báo giá và tạo đơn/);
    await page.getByRole('tab', { name: /Tour đã đặt/ }).click();
    assert.match(await page.locator('#bookings').innerText(), /1[.,]500[.,]000/);
    await page.screenshot({ path: screenshots + '/nam-private-order.png', fullPage: true });
    assert.deepEqual(errors, [], 'PH2/PH4 customer pages should have no uncaught JavaScript errors.');
    console.log('PASS: PH2/PH4 UI register, plan, edit, request, quote and accept flow.');
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
