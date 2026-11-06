import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { mkdir } from 'node:fs/promises';
import { settleMotion } from './review-helpers';
for (const width of [320, 390, 768, 1366, 1440]) for (const theme of ['light', 'dark']) for (const sample of ['login', 'private', "group"]) {
  test(`${sample} ${width}px ${theme}: layout, accessibility and screenshot`, async ({
    page
  }) => {
    await page.setViewportSize({
      width,
      height: width < 768 ? 844 : width === 768 ? 1024 : 1000
    });
    await page.addInitScript(theme => localStorage.setItem('mechat-theme', theme), theme);
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => {
      if (message.type() === 'error') errors.push(message.text());
    });
    await page.goto(`/design/${sample}`);
    await page.getByText("BẢN MẪU THIẾT KẾ", {
      exact: true
    }).waitFor();
    await page.evaluate(() => document.fonts.ready);
    if (sample === 'login') await expect(page.getByRole('heading', {
      name: "Chào mừng trở lại."
    })).toBeVisible();else await expect(page.getByRole('textbox', {
      name: "Tin nhắn",
      exact: true
    })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
    if (sample !== 'login') {
      const geometry = await page.locator('.composer').evaluate(element => ({
        bottom: element.getBoundingClientRect().bottom,
        height: innerHeight
      }));
      expect(geometry.bottom).toBeLessThanOrEqual(geometry.height + 1);
      expect(await page.locator('.thread-header').evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
      expect(await page.evaluate(() => document.documentElement.scrollHeight <= innerHeight + 1)).toBe(true);
    }
    await settleMotion(page);
    const accessibility = await new AxeBuilder({
      page
    }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze();
    expect(accessibility.violations.map(v => ({
      id: v.id,
      nodes: v.nodes.map(n => ({
        target: n.target,
        summary: n.failureSummary
      }))
    }))).toEqual([]);
    expect(errors).toEqual([]);
    await mkdir('../docs/frontend/screenshots', {
      recursive: true
    });
    await mkdir('../docs/frontend/screenshots/vi/samples', {
      recursive: true
    });
    await mkdir('../docs/frontend/screenshots/vi/states', {
      recursive: true
    });
    await page.screenshot({
      path: `../docs/frontend/screenshots/vi/samples/${sample}-${width}-${theme}.png`,
      fullPage: true
    });
    await page.screenshot({
      path: `../docs/frontend/screenshots/vi/samples/${sample}-${width}-${theme}.png`,
      fullPage: true
    });
    const state = page.getByLabel("Trạng thái mẫu");
    for (const value of sample === 'login' ? ['loading', 'error'] : ['loading', 'empty', 'error', 'permission', 'reconnecting']) {
      await state.selectOption(value);
      if (value === 'loading') await expect(page.getByText(sample === 'login' ? "Minh họa: Đang đăng nhập, vui lòng chờ." : "Đang tải dữ liệu…", {
        exact: true
      })).toBeAttached();
      if (value === 'empty') await expect(page.getByText("Bắt đầu cuộc trò chuyện")).toBeVisible();
      if (value === 'error') await expect(page.getByRole('alert')).toBeVisible();
      if (value === 'permission') await expect(page.getByText('Nội dung không khả dụng hoặc quyền truy cập của bạn đã thay đổi.')).toBeVisible();
      if (value === 'reconnecting') await expect(page.locator('.connection-notice')).toContainText("Đang kết nối lại");
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
      await page.screenshot({
        path: `../docs/frontend/screenshots/vi/states/${sample}-${width}-${theme}-${value}.png`,
        fullPage: true
      });
    }
    await state.selectOption('populated');
    if (sample === 'login') {
      await page.getByRole('button', {
        name: "Quên mật khẩu?"
      }).click();
      await expect(page.getByRole('dialog')).toBeVisible();
    } else {
      await page.getByRole('button', {
        name: "Thông tin cuộc trò chuyện",
        exact: true
      }).click();
      if (width >= 1280) await expect(page.locator('.details-panel')).toBeVisible();else await expect(page.getByRole('dialog')).toBeVisible();
      if (width >= 1280) expect(await page.locator('.thread-header').evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
    }
    await settleMotion(page);
    const detailsAccessibility = await new AxeBuilder({
      page
    }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze();
    expect(detailsAccessibility.violations.map(violation => ({
      id: violation.id,
      targets: violation.nodes.map(node => node.target)
    }))).toEqual([]);
    expect(errors).toEqual([]);
    await page.screenshot({
      path: `../docs/frontend/screenshots/vi/states/${sample}-${width}-${theme}-details.png`,
      fullPage: false
    });
  });
}
test('private sample interactions, keyboard dialog, search and mobile list', async ({
  page
}) => {
  await page.setViewportSize({
    width: 390,
    height: 844
  });
  await page.goto('/design/private');
  await page.getByRole('button', {
    name: "Thông tin cuộc trò chuyện",
    exact: true
  }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog')).not.toBeVisible();
  await expect(page.getByRole('button', {
    name: "Thông tin cuộc trò chuyện",
    exact: true
  })).toBeFocused();
  await page.getByRole('button', {
    name: "Tìm trong tin nhắn đã tải"
  }).click();
  await page.getByRole('textbox', {
    name: "Lọc tin nhắn đã tải"
  }).fill("tiệm sách");
  await expect(page.getByText("1 kết quả")).toBeVisible();
  await page.getByRole('button', {
    name: "Tìm trong tin nhắn đã tải"
  }).click();
  await page.getByRole('textbox', {
    name: "Tin nhắn",
    exact: true
  }).fill('A sample hello');
  await page.getByRole('button', {
    name: "Gửi tin nhắn mẫu"
  }).click();
  await expect(page.getByText('A sample hello', {
    exact: true
  })).toBeVisible();
  await page.getByRole('button', {
    name: "Quay lại tin nhắn"
  }).click();
  await expect(page.getByRole('navigation', {
    name: "Điều hướng trên điện thoại"
  })).toBeVisible();
  await page.getByRole('textbox', {
    name: "Lọc tin nhắn"
  }).fill("Gia Huy");
  await expect(page.locator('.conversation-row')).toHaveCount(1);
  await page.getByRole('button', {
    name: "Xóa bộ lọc cuộc trò chuyện"
  }).click();
  await expect(page.locator('.conversation-row')).toHaveCount(4);
});
test('design loading, empty, permission and error retry states', async ({
  page
}) => {
  await page.goto('/design/group');
  const select = page.getByLabel("Trạng thái mẫu");
  await select.selectOption('loading');
  await expect(page.getByText("Đang tải dữ liệu…")).toBeVisible();
  await select.selectOption('empty');
  await expect(page.getByText("Bắt đầu cuộc trò chuyện")).toBeVisible();
  await select.selectOption('permission');
  await expect(page.getByText('Nội dung không khả dụng hoặc quyền truy cập của bạn đã thay đổi.')).toBeVisible();
  await page.getByRole('button', {
    name: "Thử lại"
  }).click();
  await expect(page.getByRole('textbox', {
    name: "Tin nhắn",
    exact: true
  })).toBeVisible();
  await select.selectOption('error');
  await expect(page.getByText(/Không thể kết nối đến MeChat/)).toBeVisible();
  await page.getByRole('button', {
    name: "Thử lại"
  }).click();
  await expect(page.getByRole('textbox', {
    name: "Tin nhắn",
    exact: true
  })).toBeVisible();
});
test('320px boundary, reduced motion and theme persistence', async ({
  page
}) => {
  await page.setViewportSize({
    width: 320,
    height: 740
  });
  await page.emulateMedia({
    reducedMotion: 'reduce'
  });
  await page.goto('/design/login');
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.getByRole('button', {
    name: "Chuyển sang giao diện tối"
  }).click();
  await page.reload();
  await expect(page.locator('html')).toHaveClass('dark');
  await page.goto('/design/group');
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});
test('group details dialog, focus state and mobile list screenshots', async ({
  page
}) => {
  await page.setViewportSize({
    width: 390,
    height: 844
  });
  await page.goto('/design/group');
  await page.getByRole('button', {
    name: "Thông tin cuộc trò chuyện",
    exact: true
  }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await expect(page.getByText("Chủ nhóm", {
    exact: true
  })).toBeVisible();
  const a11y = await new AxeBuilder({
    page
  }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze();
  expect(a11y.violations).toEqual([]);
  await page.screenshot({
    path: '../docs/frontend/screenshots/group-details-mobile.png'
  });
  await page.keyboard.press('Escape');
  await page.getByRole('button', {
    name: "Quay lại nhóm"
  }).click();
  await page.screenshot({
    path: '../docs/frontend/screenshots/group-list-mobile.png'
  });
  await page.goto('/design/login');
  await page.getByLabel("Email hoặc tên đăng nhập").focus();
  await page.screenshot({
    path: '../docs/frontend/screenshots/login-focus-mobile.png'
  });
  await page.getByRole('button', {
    name: "Đăng nhập",
    exact: true
  }).click();
  await expect(page.getByText("Vui lòng nhập email hoặc tên đăng nhập.")).toBeVisible();
  await page.screenshot({
    path: '../docs/frontend/screenshots/login-validation-mobile.png'
  });
});
test('offline feedback and 200% root font scaling retain navigation and composer', async ({
  page,
  context
}) => {
  await page.setViewportSize({
    width: 390,
    height: 844
  });
  await page.goto('/design/private');
  await expect(page.getByRole('textbox', {
    name: "Tin nhắn",
    exact: true
  })).toBeVisible();
  await context.setOffline(true);
  await expect(page.locator('.offline-notice')).toContainText('Bạn đang ngoại tuyến');
  await context.setOffline(false);
  await expect(page.locator('.offline-notice')).not.toBeVisible();
  await page.evaluate(() => {
    document.documentElement.style.fontSize = '200%';
  });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await expect(page.getByRole('textbox', {
    name: "Tin nhắn",
    exact: true
  })).toBeVisible();
});
test('anonymous protected route returns live login', async ({
  page
}) => {
  await page.goto('/chat');
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', {
    name: "Chào mừng trở lại."
  })).toBeVisible();
});
