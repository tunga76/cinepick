import { expect, test } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test('catalog and recommendation critical path works', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Bu akşam ne izlemek istersin?' })).toBeVisible();
  await expect(page.locator('.movie-card')).not.toHaveCount(0);
  await page.getByRole('button', { name: 'Önerileri bul' }).click();
  await expect(page.getByRole('heading', { name: 'Sana uygun seçenekler' })).toBeVisible();
  const recommendationCount = await page.locator('.recommendation-card').count();
  expect(recommendationCount).toBeGreaterThan(0);
  expect(recommendationCount).toBeLessThanOrEqual(3);
});

test('protected profile redirects to account', async ({ page }) => {
  await page.goto('/profile');
  await expect(page).toHaveURL(/\/account\?returnUrl=%2Fprofile$/);
  await expect(page.getByRole('heading', { name: 'Tekrar hoş geldin' })).toBeVisible();
});

test('location denial keeps the city fallback available', async ({ context, page }) => {
  await context.clearPermissions();
  await page.goto('/cinemas');
  await page.getByRole('button', { name: 'Konumumu kullan' }).click();
  await expect(page.getByText('Konum izni verilmedi. Şehir seçerek devam edebilirsiniz.'))
    .toBeVisible({ timeout: 10000 });
  await expect(page.getByRole('combobox', { name: 'Şehir' })).toBeVisible();
});

test('mobile layout has no horizontal overflow', async ({ page }) => {
  await page.goto('/');
  await expect(page.locator('.movie-card').first()).toBeVisible();
  const dimensions = await page.evaluate(() => ({
    viewport: document.documentElement.clientWidth,
    content: document.documentElement.scrollWidth,
  }));
  expect(dimensions.content).toBeLessThanOrEqual(dimensions.viewport);
});

test('keyboard users can skip repeated navigation', async ({ page }) => {
  await page.goto('/');
  await page.keyboard.press('Tab');
  const skipLink = page.getByRole('link', { name: 'Ana içeriğe geç' });
  await expect(skipLink).toBeFocused();
  await expect(skipLink).toBeVisible();
  await page.keyboard.press('Enter');
  await expect(page.locator('#main-content')).toBeFocused();
});

for (const route of ['/', '/cinemas', '/account']) {
  test(`${route} has no serious or critical accessibility violations`, async ({ page }) => {
    await page.goto(route);
    await expect(page.locator('main')).toBeVisible();
    const result = await new AxeBuilder({ page }).analyze();
    const blocking = result.violations.filter(violation =>
      violation.impact === 'serious' || violation.impact === 'critical');
    expect(blocking).toEqual([]);
  });
}

test('administrator can open management and run a movie sync', async ({ page }) => {
  const email = process.env['CINEPICK_E2E_ADMIN_EMAIL'];
  const password = process.env['CINEPICK_E2E_ADMIN_PASSWORD'];
  test.skip(!email || !password, 'Admin bootstrap credentials are required for this test.');

  await page.goto('/account?returnUrl=%2Fadmin');
  await page.getByLabel('E-posta').fill(email!);
  await page.getByLabel('Şifre').fill(password!);
  await page.getByRole('button', { name: 'Giriş yap', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'CinePick yönetimi' })).toBeVisible();
  await page.getByRole('button', { name: 'Film kataloğunu eşitle' }).click();
  await expect(page.getByRole('status')).toContainText('mock:');
});
