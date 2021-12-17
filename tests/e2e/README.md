# Umbraco e2e tests

## Assumptions

Has backoffice user with following credentials (can override see umbraco.config.ts).

```
username: user@example.com
password: Umbraco9Rocks!
```

## Running the tests

```bash
$ npm ci
$ npx playwright install # install browsers
$ npm test # Run tests in headless browser
$ npm run test:headed # Run tests in headed browser
```

## Why did my tests fail?

Not sure but checkout the trace viewer (imagine downloading this from ci artifacts).
Nice demo if you comment out users.spec.ts beforeEach and run twice, will fail second time.

```bash
$ npx playwright show-trace ./artifacts/test-output/{{failing-test-folder}}/trace.zip
```
