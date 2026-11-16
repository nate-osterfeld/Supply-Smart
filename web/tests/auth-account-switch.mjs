import assert from 'node:assert/strict';

// Start Vite first. All API calls are intercepted; no real accounts or servers are used.
const { chromium } = await import(process.env.PLAYWRIGHT_MODULE ?? 'playwright');
const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH, headless: true });
const baseUrl = process.env.BASE_URL ?? 'http://127.0.0.1:5175';
try {
  const page = await browser.newPage();
  const errors = [];
  const usersRequests = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.route(url => url.pathname.startsWith('/api/'), async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname.replace(/\/$/, '');
    const account = request.headers().authorization?.endsWith('2') ? 2 : 1;
    let data = {};
    if (path === '/api/auth/login') {
      const number = request.postDataJSON().email === 'owner2@example.test' ? 2 : 1;
      data = { token: `local-session-${number}`, organizationId: number, fullName: `Owner ${number}`,
        role: 'Admin', email: `owner${number}@example.test`,
        locations: [{ id: number, name: `Clinic ${number}`, isPrimary: true }] };
    } else if (path === '/api/settings/users') {
      usersRequests.push(account);
      data = [{ id: account, fullName: `Clinic ${account} member`, role: 'Staff',
        email: `clinic-${account === 1 ? 'one' : 'two'}-member@example.test` }];
    } else if (path === '/api/auth/locations') {
      data = [{ id: account, name: `Clinic ${account}`, isPrimary: true }];
    } else if (path === '/api/settings/subscription') {
      data = { planName: 'Free', status: 'Active', billingInterval: 'Annual', renewalDateUtc: '2027-01-01',
        maxRoomsQuota: 2, usedRoomsCount: 0, maxItemsQuota: 50, usedItemsCount: 0, isStripeConfigured: false };
    } else if (path.includes('audit') || path === '/api/locations') data = [];
    await route.fulfill({ json: data });
  });

  const signIn = async number => {
    await page.getByPlaceholder('you@company.com').fill(`owner${number}@example.test`);
    await page.getByPlaceholder('Password', { exact: true }).fill('local-test-passphrase');
    await page.locator('form').getByRole('button', { name: 'Sign In', exact: true }).click();
    await page.getByRole('button', { name: 'Launch Workspace' }).click();
    await page.getByRole('button', { name: 'Workspace Settings', exact: true }).click();
    await page.getByRole('button', { name: 'Tenant User Permissions', exact: true }).click();
    await page.getByText(/clinic-(one|two)-member@example.test/).first().waitFor();
  };

  await page.goto(baseUrl);
  await signIn(1);
  assert.equal(await page.getByText('clinic-one-member@example.test', { exact: true }).count(), 1);
  await page.getByTitle('Click to sign out', { exact: true }).click();
  await page.getByPlaceholder('you@company.com').waitFor();
  // The old sign-out stays on /dashboard; browser Back returns to the login route.
  if (new URL(page.url()).pathname !== '/') await page.goBack();
  await signIn(2);
  console.log(JSON.stringify({ usersRequests, previousClinicVisible: await page.getByText('clinic-one-member@example.test', { exact: true }).count() }));
  assert.equal(await page.getByText('clinic-one-member@example.test', { exact: true }).count(), 0,
    'Signing into another clinic must discard cached data from the previous account.');
  assert.equal(await page.getByText('clinic-two-member@example.test', { exact: true }).count(), 1);
  assert.deepEqual(usersRequests, [1, 2]);
  assert.deepEqual(errors, []);
  console.log('PASS account switch: prior clinic data cleared and new clinic data fetched.');
} finally {
  await browser.close();
}
