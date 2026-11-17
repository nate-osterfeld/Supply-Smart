import assert from 'node:assert/strict';

const { chromium } = await import(process.env.PLAYWRIGHT_MODULE ?? 'playwright');
const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH, headless: true });
try {
  const page = await browser.newPage();
  let navigations = 0;
  page.on('framenavigated', frame => { if (frame === page.mainFrame()) navigations++; });
  await page.route(url => url.pathname === '/api/auth/login', route => route.fulfill({ status: 401 }));
  await page.goto(process.env.BASE_URL ?? 'http://127.0.0.1:5175');
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
