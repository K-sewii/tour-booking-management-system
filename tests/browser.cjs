const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
 const browser = await chromium.launch({headless:true});
 const context = await browser.newContext({viewport:{width:1440,height:1000}}), page = await context.newPage();
 const errors=[]; page.on('pageerror',e=>errors.push(e.message));
 const root='http://localhost:5081';
 const screenshots='artifacts/previews'; fs.mkdirSync(screenshots,{recursive:true});
 await page.goto(root); await page.screenshot({path:screenshots+'/web-home-desktop.png',fullPage:true});
 assert.match(await page.locator('h1').innerText(),/Đi gần thôi/);
 await page.goto(root+'/Tours'); await page.locator('select[name=region]').selectOption({label:'Vũng Tàu'}); await page.getByRole('button',{name:'Áp dụng bộ lọc'}).click(); assert.equal(await page.locator('.tour-card').count(),1);
 await page.goto(root+'/Tours/Details/1'); await page.locator('#guest-count').fill('3'); assert.match(await page.locator('#estimate-value').innerText(),/2.070.000/);
 await page.getByRole('link',{name:'Tùy chỉnh lịch trình'}).click(); assert.equal(await page.locator('#selected-places li').count(),2);
 await page.getByRole('button',{name:'Đưa Chợ Bến Thành lên',exact:true}).click(); assert.match(await page.locator('#selected-places li').first().innerText(),/Chợ Bến Thành/);
 await page.locator('input[name=Name]').fill('Cuối tuần của Đức'); await page.getByRole('button',{name:'Lưu lịch trình'}).click(); await page.getByRole('link',{name:'Đăng nhập để lưu'}).click();
 await page.locator('[name=Email]').fill('khach@dongnamtrip.local'); await page.locator('[name=Password]').fill('wrong'); await page.getByRole('button',{name:'Đăng nhập',exact:true}).click(); assert.match(await page.locator('.validation').innerText(),/đăng nhập/);
 await page.locator('[name=Password]').fill('Demo@123'); await page.getByRole('button',{name:'Đăng nhập',exact:true}).click();
 await page.waitForURL('**/Planner'); assert.equal(await page.locator('input[name=Name]').inputValue(),'Cuối tuần của Đức'); assert.match(await page.locator('#selected-places li').first().innerText(),/Chợ Bến Thành/);
 await page.screenshot({path:screenshots+'/web-planner-desktop.png',fullPage:true});
 await page.getByRole('button',{name:'Lưu lịch trình'}).click(); await page.waitForURL('**/Trips'); assert.match(await page.locator('.saved-trip').last().innerText(),/Cuối tuần của Đức/);
 await page.getByRole('tab',{name:'Tour đã đặt',exact:true}).click(); assert.equal(await page.locator('#bookings').isVisible(),true);
 await page.getByRole('button',{name:/Đăng xuất/}).click(); await page.goto(root+'/Trips'); await page.waitForURL('**/Account/Login?**');
 await page.locator('[name=Email]').fill('khach2@dongnamtrip.local'); await page.locator('[name=Password]').fill('Demo@123'); await page.getByRole('button',{name:'Đăng nhập',exact:true}).click(); await page.waitForURL('**/Trips'); assert.equal(await page.locator('.saved-trip').count(),0);
 const mobile=await context.newPage(); await mobile.setViewportSize({width:390,height:844}); mobile.on('pageerror',e=>errors.push(e.message));
 for(const path of ['/','/Tours','/Tours/Details/1','/Destinations','/Destinations/Details/1','/Planner','/Trips','/Account/Login']){
   await mobile.goto(root+path); assert.equal(await mobile.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true,'horizontal overflow '+path);
 }
 await mobile.goto(root); await mobile.screenshot({path:screenshots+'/web-home-mobile.png',fullPage:true}); await mobile.getByRole('button',{name:'Mở menu'}).click(); assert.equal(await mobile.locator('#navigation').isVisible(),true);
 assert.deepEqual(errors,[]); await browser.close(); console.log('PASS: browser journey, filtering, estimate, reorder, draft preservation, login errors, save, account isolation, mobile menu and 8 mobile routes.');
})().catch(e=>{console.error(e);process.exit(1)});
