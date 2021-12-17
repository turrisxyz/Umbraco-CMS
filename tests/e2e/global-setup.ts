import { chromium } from '@playwright/test';
import { umbracoConfig } from './umbraco.config';

async function globalSetup() {
  const browser = await chromium.launch();
  const page = await browser.newPage();

  await page.goto(`${umbracoConfig.environment.baseUrl}/umbraco`);
  await page.fill('#umb-username', umbracoConfig.user.login);
  await page.fill('#umb-passwordTwo', umbracoConfig.user.password);
  await page.click('text=Login');
  await page.waitForNavigation();

  // Save signed-in state for re-use.
  await page.context().storageState({ path: './artifacts/umbracoUserState.json' });
  await browser.close();
}

export default globalSetup;
