import { test as base, Page, Locator } from '@playwright/test';

class ButtonHelpers {
  page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  byLabelKey(key: string): Locator {
    return this.page.locator(`umb-button[label-key="${key}"] button:enabled`);
  }
}

class SectionHelpers {
  page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async open(section: string) {
    await this.page.click(`[data-element="section-${section}"]`);
  }
}

class ApiHelpers {
  page: Page;
  users: UsersApiHelpers;

  constructor(page: Page) {
    this.page = page;
    this.users = new UsersApiHelpers(this);
  }

  async getCsrfToken() {
    return (await this.page.context().cookies()).filter(x => x.name === 'UMB-XSRF-TOKEN')[0].value;
  }

  async get(url: string) {
    const csrf = await this.getCsrfToken();
    const options = {
      headers: {
        'X-UMB-XSRF-TOKEN': csrf
      },
    }
    return this.page.request.get(url, options);
  }

  async post(url: string, data?: object) {
    const csrf = await this.getCsrfToken();
    const options = {
      headers: {
        'X-UMB-XSRF-TOKEN': csrf
      },
      data: data
    }
    return this.page.request.post(url, options);
  }
}

class UsersApiHelpers {
  api: ApiHelpers;

  constructor(api: ApiHelpers) {
    this.api = api;
  }

  async deleteIfExists(email: string) {
    let response =  await this.api.get(`/umbraco/backoffice/UmbracoApi/Users/GetPagedUsers?pageNumber=1&pageSize=1&filter=${email}`);
    let content = await response.text();

    if (!content.length) {
      return null;
    }

    // https://github.com/umbraco/Umbraco-CMS/discussions/11186
    const junk = ")]}',";

    if (content.startsWith(junk)) {
      content = content.substr(junk.length);
    }

    const json =  JSON.parse(content);

    if(json.totalItems < 1) {
      return false;
    }

    const userId = json.items[0].id;

    response =  await this.api.post(`/umbraco/backoffice/UmbracoApi/Users/PostDeleteNonLoggedInUser?id=${userId}`);

    return response.ok();
  }
}

class Backoffice {
  page: Page;
  buttons: ButtonHelpers;
  sections: SectionHelpers;
  api: ApiHelpers;

  constructor(page: Page) {
    this.page = page;
    this.buttons = new ButtonHelpers(page);
    this.sections = new SectionHelpers(page);
    this.api = new ApiHelpers(page);
  }

  async openBackoffice() {
    await this.page.goto('/umbraco');
  }
}

const test = base.extend<{ umbraco: Backoffice }>({
  umbraco: async ({ page }, use) => {
    const umbraco = new Backoffice(page);
    await use(umbraco);
  },
});

export { test, Backoffice };
