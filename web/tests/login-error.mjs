import assert from 'node:assert/strict';

const { chromium } = await import(process.env.PLAYWRIGHT_MODULE ?? 'playwright');
const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH, headless: true });
try {
  const page = await browser.newPage();
  await page.addInitScript(() => {
    localStorage.setItem('supplysmart_token', 'legacy-session');
    localStorage.setItem('supplysmart_location_id', '1');
    localStorage.setItem('other-preference', 'keep');
  });
  let navigations = 0;
  page.on('framenavigated', frame => { if (frame === page.mainFrame()) navigations++; });
  await page.route(url => url.pathname === '/api/auth/session', route =>
    route.fulfill({ json: { user: null, csrfToken: null } }));
  await page.route(url => url.pathname === '/api/auth/session/login', route => route.fulfill({ status: 401 }));
  await page.goto((process.env.BASE_URL ?? 'http://127.0.0.1:5175') + '/dashboard');
  await page.getByPlaceholder('you@company.com').waitFor();
  assert.equal(new URL(page.url()).pathname, '/');
  assert.equal(await page.evaluate(() => localStorage.getItem('supplysmart_token')), null);
  assert.equal(await page.evaluate(() => localStorage.getItem('other-preference')), 'keep');
  const initialNavigations = navigations;
  await page.getByPlaceholder('you@company.com').fill('owner@example.test');
  await page.getByPlaceholder('Password', { exact: true }).fill('incorrect-test-password');
  await page.locator('form').getByRole('button', { name: 'Sign In', exact: true }).click();
  const message = await page.getByRole('alert').textContent();
  await page.waitForLoadState('networkidle');
  console.log(JSON.stringify({ message, navigations, initialNavigations }));
  assert.equal(message, 'Email or password is incorrect.');
  assert.equal(navigations, initialNavigations, 'A rejected login must not reload the form.');
  assert.equal(await page.getByPlaceholder('you@company.com').inputValue(), 'owner@example.test');
  console.log('PASS login error: clear message and form retained without a reload.');
} finally {
  await browser.close();
}
