import assert from 'node:assert/strict';

const { chromium } = await import(process.env.PLAYWRIGHT_MODULE ?? 'playwright');
const origin = process.env.BASE_URL ?? 'http://127.0.0.1:5175';
const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH, headless: true });
try {
  for (const scenario of ['checkout', 'expired-session', 'canceled', 'portal', 'staff']) {
    const context = await browser.newContext();
    const page = await context.newPage();
    let signedIn = scenario !== 'expired-session';
    let verifications = 0;
    let billingReads = 0;
    const role = scenario === 'staff' ? 'Staff' : 'Admin';
    const user = { role, organizationId: 1, fullName: 'Billing tester', email: 'clinic@example.test', locations: [{ id: 1, isPrimary: true, name: 'Clinic' }] };
    const details = { planName: 'Starter', status: 'Active', billingInterval: 'Annual', renewalDateUtc: '2030-01-01',
      maxRoomsQuota: 5, maxItemsQuota: 250, usedItemsCount: 1, usedRoomsCount: 1 };
    await page.route('**/api/**', async route => {
      const request = route.request();
      const path = new URL(request.url()).pathname;
      if (path === '/api/auth/session') return route.fulfill({ json: { user: signedIn ? user : null, csrfToken: signedIn ? 'fixture' : null } });
      if (path === '/api/auth/session/login') {
        signedIn = true;
        return route.fulfill({ json: user });
      }
      if (path === '/api/settings/subscription/verify-session') {
        verifications++;
        assert.equal(request.postDataJSON().sessionId, 'cs_fixture');
        return route.fulfill({ json: details });
      }
      if (path === '/api/settings/subscription') { billingReads++; return route.fulfill({ json: details }); }
      return route.fulfill({ json: path.includes('audit') || path === '/api/locations' ? [] : {} });
    });
    const query = new URLSearchParams({ tab: 'settings' });
    if (scenario === 'canceled') query.set('billing_canceled', 'true');
    else if (scenario !== 'portal') {
      query.set('billing_success', 'true');
      query.set('session_id', 'cs_fixture');
      query.set('plan', 'Enterprise');
    }
    await page.goto(origin + '/dashboard?' + query);
    if (!signedIn) {
      await page.getByPlaceholder('you@company.com').fill(user.email);
      await page.getByPlaceholder('Password', { exact: true }).fill('local browser fixture');
      await page.locator('form').getByRole('button', { name: 'Sign In', exact: true }).click();
      await page.getByRole('button', { name: 'Launch Workspace' }).click();
    }
    if (role === 'Staff') {
      await page.getByRole('heading', { name: 'Dashboard Overview' }).waitFor();
      assert.equal(new URL(page.url()).searchParams.has('session_id'), false);
      assert.equal(billingReads, 0);
      assert.equal(verifications, 0);
    } else {
      await page.getByText('Current plan', { exact: true }).waitFor();
      if (scenario === 'checkout' || scenario === 'expired-session') {
        await page.getByText('Your workspace is on the Starter plan.', { exact: true }).waitFor();
        assert.equal(new URL(page.url()).searchParams.has('session_id'), false);
        await page.reload();
        await page.getByText('Current plan', { exact: true }).waitFor();
        assert.equal(verifications, 1);
      } else {
        if (scenario === 'canceled') await page.getByText('Checkout Canceled', { exact: true }).waitFor();
        assert.equal(verifications, 0);
      }
    }
    await context.close();
    console.log('PASS billing return ' + scenario);
  }
} finally {
  await browser.close();
}
