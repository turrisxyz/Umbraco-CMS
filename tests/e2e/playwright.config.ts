import { PlaywrightTestConfig, devices } from '@playwright/test';
import { umbracoConfig } from './umbraco.config';

const config: PlaywrightTestConfig = {
  globalSetup: require.resolve('./global-setup'),
  forbidOnly: !!process.env.CI,
  retries: 1,
  outputDir: 'artifacts/test-output',
  use: {
    trace: 'on-first-retry',
    viewport: { width: 1920, height: 1080 },
    ignoreHTTPSErrors: true,
    video: 'on-first-retry',
    baseURL: umbracoConfig.environment.baseUrl,
    storageState: './artifacts/umbracoUserState.json'
  },
  reporter: [
    ['list'],
    ['junit', { outputFile: 'artifacts/results.xml' }]
  ],
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
  ]
};
export default config;
