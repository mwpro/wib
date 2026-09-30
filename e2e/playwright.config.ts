import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  globalSetup: './global-setup.ts',
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: [
    ['list'],
    ['html', { open: 'never' }]
  ],
  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:8080',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: process.env.BASE_URL ? undefined : {
    command: 'dotnet run --project ../backend/Wib.Api/Wib.Api.csproj --no-launch-profile',
    url: 'http://localhost:8080/api/health',
    reuseExistingServer: false,
    timeout: 60000,
    env: {
      ASPNETCORE_URLS: 'http://localhost:8080',
      ASPNETCORE_ENVIRONMENT: 'Development',
      JwtAuth__BypassAuth: 'true',
      ConnectionStrings__DefaultConnection:
        process.env.ConnectionStrings__DefaultConnection ||
        'Server=localhost;Port=3306;Database=wib_test;User=root;Password=secret;',
    },
  },
})
