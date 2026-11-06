import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { LoginPage } from '@/features/auth/LoginPage';
import { ChatWorkspace } from '@/components/chat/ChatWorkspace';
import { Composer } from '@/components/chat/Composer';
describe('login design sample', () => {
  it('validates fields, exposes password toggle and announces the fixture submission', async () => {
    const user = userEvent.setup();
    render(<MemoryRouter><LoginPage preview /></MemoryRouter>);
    await user.click(screen.getByRole('button', {
      name: "Đăng nhập"
    }));
    expect(await screen.findByText("Vui lòng nhập email hoặc tên đăng nhập.")).toBeVisible();
    expect(screen.getByLabelText("Email hoặc tên đăng nhập")).toHaveAttribute('aria-invalid', 'true');
    await user.type(screen.getByLabelText("Email hoặc tên đăng nhập"), 'sample-name');
    await user.type(screen.getByLabelText("Mật khẩu", {
      exact: true
    }), 'sample-password');
    await user.click(screen.getByRole('button', {
      name: "Hiện mật khẩu"
    }));
    expect(screen.getByLabelText("Mật khẩu", {
      exact: true
    })).toHaveAttribute('type', 'text');
    await user.click(screen.getByRole('button', {
      name: "Đăng nhập"
    }));
    expect(await screen.findByText(/Không có yêu cầu đăng nhập thật nào được gửi/)).toBeVisible();
  });
});
it('keeps the composer focused and readOnly during an in-flight send', async () => {
  const user = userEvent.setup();
  let resolveSend;
  const onSend = vi.fn(() => new Promise(resolve => {
    resolveSend = resolve;
  }));
  render(<Composer onSend={onSend} />);
  const field = screen.getByRole('textbox', {
    name: "Tin nhắn",
    exact: true
  });
  await user.type(field, 'In-flight draft');
  await user.keyboard('{Enter}');
  expect(field).toHaveAttribute('readonly');
  expect(field).not.toBeDisabled();
  expect(field).toHaveFocus();
  expect(screen.getByRole('button', {
    name: "Đang gửi tin nhắn"
  })).toBeDisabled();
  resolveSend();
  await screen.findByRole('button', {
    name: "Gửi tin nhắn"
  });
  expect(field).toHaveValue('');
});
it('announces login pending and error preview states without sending requests', () => {
  const {
    rerender
  } = render(<MemoryRouter><LoginPage preview previewState="loading" /></MemoryRouter>);
  expect(screen.getByRole('button', {
    name: /Đang đăng nhập/
  })).toBeDisabled();
  expect(screen.getByLabelText("Email hoặc tên đăng nhập")).toBeDisabled();
  rerender(<MemoryRouter><LoginPage preview previewState="error" /></MemoryRouter>);
  expect(screen.getByRole('alert')).toHaveTextContent("Minh họa");
});
it('keeps failed drafts and reuses the client ID on retry; renders text safely', async () => {
  const user = userEvent.setup();
  const onSend = vi.fn().mockRejectedValueOnce(new Error('network')).mockResolvedValueOnce({});
  render(<MemoryRouter><ChatWorkspace user={{
      id: 'me',
      name: 'Test User'
    }} selectedId="thread" selected={{
      name: 'Friend',
      otherUserId: 'other',
      status: 'Active'
    }} messages={[{
      id: 'message',
      senderUserId: 'other',
      content: '<img src=x onerror=alert(1)>',
      createdAt: '2026-11-06T03:00:00Z'
    }]} onSend={onSend} /></MemoryRouter>);
  expect(screen.getByText('<img src=x onerror=alert(1)>')).toBeVisible();
  expect(document.querySelector('.message-bubble img')).toBeNull();
  await user.type(screen.getByRole('textbox', {
    name: "Tin nhắn",
    exact: true
  }), 'hello');
  await user.click(screen.getByRole('button', {
    name: "Gửi tin nhắn"
  }));
  expect(await screen.findByRole('alert')).toHaveTextContent("Bản nháp vẫn được giữ");
  expect(screen.getByRole('textbox', {
    name: "Tin nhắn",
    exact: true
  })).toHaveValue('hello');
  await user.click(screen.getByRole('button', {
    name: "Gửi tin nhắn"
  }));
  expect(onSend.mock.calls[0][0].clientMessageId).toBe(onSend.mock.calls[1][0].clientMessageId);
  expect(screen.getByRole('textbox', {
    name: "Tin nhắn",
    exact: true
  })).toHaveValue('');
});
