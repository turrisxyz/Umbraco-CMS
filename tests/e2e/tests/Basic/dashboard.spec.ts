import { expect } from '@playwright/test';

// TODO: use tsconfig paths i.e. @fixtures/umbracoTest
// See https://github.com/microsoft/playwright/pull/10525 presumably will be in 1.18?
import { test } from '../../fixtures/backofficeTest';

test.describe('Dashboards', () => {
  test('Welcome dashboard is shown', async ({ umbraco, page }) => {
    await umbraco.openBackoffice();
    const title = page.locator('text=Welcome to Umbraco');
    await expect(title).toBeVisible();
  });
});
