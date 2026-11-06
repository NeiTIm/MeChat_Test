import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { settleMotion } from './review-helpers';
for (const width of [390, 1440]) for (const theme of ['light', 'dark']) {
  test(`Hybrid message actions ${width} ${theme}: reply/edit/react/recall, keyboard and focus`, async ({
    page
  }) => {
    await page.setViewportSize({
      width,
      height: 1000
    });
    await page.addInitScript(theme => localStorage.setItem('mechat-theme', theme), theme);
    const apiCalls = [];
    page.on('request', request => {
      if (/\/api\/|\/hubs\//.test(request.url())) apiCalls.push(request.url());
    });
    await page.goto('/design/private');
    const options = page.getByRole('button', {
      name: "Tùy chọn tin nhắn mẫu số 6",
      exact: true
    });
    await options.focus();
    await options.press('Enter');
    await page.getByRole('menuitem', {
      name: "Trả lời",
      exact: true
    }).click();
    await expect(page.locator('.compose-context')).toContainText('Đang trả lời');
    const field = page.getByRole('textbox', {
      name: "Tin nhắn",
      exact: true
    });
    await field.fill('A thoughtful sample reply');
    await page.getByRole('button', {
      name: "Gửi tin nhắn mẫu"
    }).click();
    await expect(page.locator('.reply-preview')).toContainText('Cà phê và sách');
    await expect(field).toBeFocused();
    await options.focus();
    await options.press('Enter');
    await page.getByRole('menuitem', {
      name: "Sửa tin nhắn mẫu"
    }).click();
    await expect(field).toHaveValue("Cà phê và sách thì mình đi ngay. Thứ Bảy lúc 10 giờ nhé?");
    await field.fill('Saturday at eleven — updated sample.');
    await page.getByRole('button', {
      name: "Lưu chỉnh sửa mẫu"
    }).click();
    await expect(page.getByText('Saturday at eleven — updated sample.', {
      exact: true
    })).toBeVisible();
    await expect(field).toBeFocused();
    await options.focus();
    await options.press('Enter');
    await page.getByRole('menuitem', {
      name: "Thả cảm xúc trái tim"
    }).click();
    await expect(page.locator('.reaction-preview')).toHaveCount(1);
    await options.focus();
    await options.press('Enter');
    await page.getByRole('menuitem', {
      name: "Thu hồi tin nhắn mẫu"
    }).click();
    await expect(page.getByRole('dialog')).toContainText('không thay đổi tin nhắn thật');
    await page.screenshot({
      path: `../docs/frontend/screenshots/vi/states/private-${width}-${theme}-recall.png`
    });
    await page.getByRole('button', {
      name: "Giữ tin nhắn"
    }).click();
    await expect(page.getByText('Saturday at eleven — updated sample.', {
      exact: true
    })).toBeVisible();
    await options.focus();
    await options.press('Enter');
    await page.getByRole('menuitem', {
      name: "Thu hồi tin nhắn mẫu"
    }).click();
    await page.getByRole('button', {
      name: "Thu hồi tin mẫu",
      exact: true
    }).click();
    await expect(page.getByText("Tin nhắn đã được thu hồi.")).toBeVisible();
    await expect(page.locator('.reaction-preview')).toHaveCount(0);
    await page.getByLabel("Trạng thái mẫu").selectOption('delivered');
    await expect(page.locator('.receipt-preview').first()).toContainText("Minh họa · Đã nhận");
    expect(apiCalls).toEqual([]);
  });
  test(`Hybrid group ${width} ${theme}: pins, invitation, roles and dialog accessibility`, async ({
    page
  }) => {
    await page.setViewportSize({
      width,
      height: 1000
    });
    await page.addInitScript(theme => localStorage.setItem('mechat-theme', theme), theme);
    await page.goto('/design/group');
    await page.getByRole('button', {
      name: /Tin nhắn đã ghim/
    }).click();
    await expect(page.getByRole('dialog')).toContainText('không phải kênh');
    await page.keyboard.press('Escape');
    await page.getByRole('button', {
      name: "Thông tin cuộc trò chuyện",
      exact: true
    }).click();
    await page.getByRole('button', {
      name: "Lời mời",
      exact: true
    }).click();
    await page.getByLabel("Tên đăng nhập minh họa").fill('sample-guest');
    await page.getByRole('button', {
      name: "Xem thử lời mời",
      exact: true
    }).click();
    await expect(page.getByText("Không có lời mời thật nào được gửi.")).toBeVisible();
    await page.keyboard.press('Escape');
    await page.getByRole('button', {
      name: "Thao tác nhóm",
      exact: true
    }).click();
    await page.getByLabel("Xem mẫu với vai trò").selectOption('Owner');
    await settleMotion(page);
    const accessibility = await new AxeBuilder({
      page
    }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze();
    expect(accessibility.violations.map(violation => violation.id)).toEqual([]);
    await page.screenshot({
      path: `../docs/frontend/screenshots/vi/states/group-${width}-${theme}-roles.png`
    });
    await page.keyboard.press('Escape');
    await expect(page.locator('.member-row').getByText("Chủ nhóm", {
      exact: true
    })).toHaveCount(1);
    if (width >= 1280) {
      await page.getByRole('button', {
        name: "Đóng thông tin cuộc trò chuyện"
      }).click();
      await expect(page.getByRole('button', {
        name: "Thông tin cuộc trò chuyện",
        exact: true
      })).toBeFocused();
    } else await page.keyboard.press('Escape');
    await expect(page.locator('.thread-heading')).toContainText("Chủ nhóm");
  });
}
test('scroll anchoring, incoming messages and history prepend preserve reading position', async ({
  page
}) => {
  await page.setViewportSize({
    width: 390,
    height: 844
  });
  await page.goto('/design/private');
  await page.getByLabel("Trạng thái mẫu").selectOption('history');
  const history = page.locator('.message-scroll');
  await expect(page.getByRole('button', {
    name: "Tải tin nhắn trước"
  })).toBeAttached();
  await history.evaluate(element => {
    element.scrollTop = 0;
    element.dispatchEvent(new Event('scroll'));
  });
  await page.getByRole('button', {
    name: "Mô phỏng tin nhắn mẫu đến"
  }).click();
  await expect(page.getByRole('button', {
    name: "Có tin nhắn mới · xem mới nhất"
  })).toBeVisible();
  expect(await history.evaluate(element => element.scrollTop)).toBe(0);
  const anchor = page.locator('[data-message-id="design-message-history-30"] .message-bubble');
  const before = await anchor.evaluate(element => element.getBoundingClientRect().top);
  await page.getByRole('button', {
    name: "Tải tin nhắn trước"
  }).click();
  const after = await anchor.evaluate(element => element.getBoundingClientRect().top);
  expect(Math.abs(after - before)).toBeLessThanOrEqual(3);
  await page.getByRole('button', {
    name: "Có tin nhắn mới · xem mới nhất"
  }).click();
  await expect(page.getByText("Có tin nhắn minh họa mới. Vị trí bạn đang đọc vẫn được giữ nguyên.")).toBeVisible();
  expect(await history.evaluate(element => element.scrollHeight - element.clientHeight - element.scrollTop)).toBeLessThan(3);
});
test('composer multiline, IME protection and shrunken mobile viewport remain usable', async ({
  page
}) => {
  await page.setViewportSize({
    width: 390,
    height: 844
  });
  await page.goto('/design/private');
  const field = page.getByRole('textbox', {
    name: "Tin nhắn",
    exact: true
  });
  await field.fill('First line');
  await field.press('Shift+Enter');
  await field.press('End');
  await field.type('Second line');
  expect((await field.inputValue()).split('\n')).toHaveLength(2);
  await field.dispatchEvent('keydown', {
    key: 'Enter',
    code: 'Enter',
    isComposing: true
  });
  await expect(field).not.toHaveValue('');
  await page.setViewportSize({
    width: 390,
    height: 480
  });
  await expect(field).toBeVisible();
  const geometry = await page.locator('.composer').evaluate(element => ({
    bottom: element.getBoundingClientRect().bottom,
    viewport: innerHeight
  }));
  expect(geometry.bottom).toBeLessThanOrEqual(geometry.viewport + 1);
  await page.screenshot({
    path: '../docs/frontend/screenshots/vi/states/private-390-keyboard-viewport.png'
  });
  await field.press('Enter');
  await expect(field).toHaveValue('');
  await expect(field).toBeFocused();
});
test('login keyboard sequence, autofill attributes and explicit preview recovery/register', async ({
  page
}) => {
  await page.goto('/design/login');
  const identifier = page.getByLabel("Email hoặc tên đăng nhập");
  await expect(identifier).toHaveAttribute('autocomplete', 'username');
  await expect(identifier).toHaveAttribute('autocapitalize', 'none');
  await expect(page.getByLabel("Mật khẩu", {
    exact: true
  })).toHaveAttribute('autocomplete', 'current-password');
  await identifier.focus();
  await page.keyboard.press('Tab');
  await expect(page.getByLabel("Mật khẩu", {
    exact: true
  })).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(page.getByRole('button', {
    name: "Hiện mật khẩu"
  })).toBeFocused();
  await page.getByRole('button', {
    name: "Quên mật khẩu?"
  }).click();
  await page.getByLabel("Địa chỉ email").fill('sample@example.invalid');
  await page.getByRole('button', {
    name: "Xem thử hướng dẫn"
  }).click();
  await expect(page.getByText('không có email nào được gửi.', {
    exact: false
  })).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('button', {
    name: "Quên mật khẩu?"
  })).toBeFocused();
  await page.getByRole('button', {
    name: "Tạo tài khoản",
    exact: false
  }).click();
  await expect(page.getByRole('dialog')).toContainText('12 ký tự');
  await page.keyboard.press('Escape');
});
test('320px long unbroken message wraps and growing composer never covers jump control', async ({
  page
}) => {
  await page.setViewportSize({
    width: 320,
    height: 844
  });
  await page.goto('/design/private');
  await page.getByLabel("Trạng thái mẫu").selectOption('history');
  await page.locator('.message-scroll').evaluate(element => {
    element.scrollTop = 0;
    element.dispatchEvent(new Event('scroll'));
  });
  await page.getByRole('button', {
    name: "Mô phỏng tin nhắn mẫu đến"
  }).click();
  const field = page.getByRole('textbox', {
    name: "Tin nhắn",
    exact: true
  });
  await field.fill('A long draft\n'.repeat(12));
  const jump = await page.getByRole('button', {
    name: "Có tin nhắn mới · xem mới nhất"
  }).boundingBox();
  const composer = await page.locator('.composer').boundingBox();
  expect(jump.y + jump.height).toBeLessThanOrEqual(composer.y + 1);
  const longText = `https://example.invalid/${'long-sample-path'.repeat(80)}`;
  await field.fill(longText);
  await field.press('Enter');
  await expect(page.getByText(longText, {
    exact: true
  })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  expect(await page.locator('.message-scroll').evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
});
test('purposeful panel motion is bounded and reduced-motion removes movement', async ({
  page
}) => {
  await page.setViewportSize({
    width: 1440,
    height: 1000
  });
  await page.goto('/design/private');
  await page.getByRole('button', {
    name: "Thông tin cuộc trò chuyện",
    exact: true
  }).click();
  const normal = await page.locator('.details-panel').evaluate(element => parseFloat(getComputedStyle(element).animationDuration));
  expect(normal).toBeGreaterThan(0);
  expect(normal).toBeLessThanOrEqual(.25);
  await page.getByRole('button', {
    name: "Đóng thông tin cuộc trò chuyện"
  }).click();
  await page.emulateMedia({
    reducedMotion: 'reduce'
  });
  await page.getByRole('button', {
    name: "Thông tin cuộc trò chuyện",
    exact: true
  }).click();
  const reduced = await page.locator('.details-panel').evaluate(element => parseFloat(getComputedStyle(element).animationDuration));
  expect(reduced).toBeLessThan(.001);
});
