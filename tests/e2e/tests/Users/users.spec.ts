import { expect } from '@playwright/test';

// TODO: use tsconfig paths i.e. @fixtures/umbracoTest
// See https://github.com/microsoft/playwright/pull/10525 presumably will be in 1.18?
import { test, Backoffice } from '../../fixtures/backofficeTest';

function getTestUser(browserName: string) {
  const email = `alice-bobson@acceptancetest.${browserName}`;
  return {
    id: -1,
    parentId: -1,
    name: `Alice ${browserName}`,
    username:email,
    culture: "en-US",
    email: email,
    startContentIds: [],
    startMediaIds: [],
    userGroups: ["admin"],
    message: ""
  };
}

async function createUser(umbraco: Backoffice, user: object){
  return await umbraco.api.post('/umbraco/backoffice/umbracoapi/users/PostCreateUser', user);
}

test.describe('Users', () => {
  test.beforeEach(async ({ umbraco,  browserName }) => {
    const user = getTestUser(browserName);
    await umbraco.api.users.deleteIfExists(user.email);
  });

  test('Can create user', async ({ umbraco, page, browserName }) => {
    await umbraco.openBackoffice();
    await umbraco.sections.open('users');
    await umbraco.buttons.byLabelKey('user_createUser').click();

    const user = getTestUser(browserName);

    await page.fill('input[name="name"]', user.name);
    await page.fill('input[name="email"]', user.email);

    await page.click('.umb-node-preview-add');
    await page.click('.umb-user-group-picker-list-item:nth-child(1) > .umb-user-group-picker__action');
    await page.click('.umb-user-group-picker-list-item:nth-child(2) > .umb-user-group-picker__action');
    await page.click('.btn-success');

    await page.click('.umb-button > .btn > .umb-button__content');

    const profileButton = await umbraco.buttons.byLabelKey('user_goToProfile');
    await expect(profileButton).toBeVisible();
  });

  test('Error shown for attempt to create duplicate user', async ({ umbraco, page, browserName }) => {
    const user = getTestUser(browserName);
    await createUser(umbraco, user);

    await umbraco.openBackoffice();
    await umbraco.sections.open('users');
    await umbraco.buttons.byLabelKey('user_createUser').click();

    await page.fill('input[name="name"]', user.name);
    await page.fill('input[name="email"]', user.email);

    await page.click('.umb-node-preview-add');
    await page.click('.umb-user-group-picker-list-item:nth-child(1) > .umb-user-group-picker__action');
    await page.click('.umb-user-group-picker-list-item:nth-child(2) > .umb-user-group-picker__action');
    await page.click('.btn-success');

    await page.click('.umb-button > .btn > .umb-button__content');

    const errorMessage = await page.locator('text=A user with the email already exists')
    await expect(errorMessage).toBeVisible();
  });
});
