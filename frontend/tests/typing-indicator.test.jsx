import { render, screen } from '@testing-library/react'
import { expect, it } from 'vitest'
import { TypingIndicator } from '@/components/chat/TypingIndicator'

it('keeps an empty polite status slot and announces the named participant', () => {
  const view = render(<TypingIndicator />)
  const slot = screen.getByRole('status')
  expect(slot).toBeEmptyDOMElement()
  expect(slot).toHaveAttribute('aria-live', 'polite')
  expect(slot).toHaveAttribute('aria-atomic', 'true')
  view.rerender(<TypingIndicator names={['tienAdmin']} />)
  expect(slot).toHaveTextContent('tienAdmin đang nhập')
  expect(slot.querySelector('.typing-dots')).toHaveAttribute('aria-hidden', 'true')
  expect(slot.querySelectorAll('.typing-dots i')).toHaveLength(3)
  view.rerender(<TypingIndicator />)
  expect(screen.getByRole('status')).toBe(slot)
  expect(slot).toBeEmptyDOMElement()
})

it('summarizes group participants without listing every name', () => {
  const view = render(<TypingIndicator names={['An', 'Bình']} />)
  expect(screen.getByRole('status')).toHaveTextContent('An, Bình đang nhập')
  view.rerender(<TypingIndicator names={['An', 'Bình', 'Chi', 'Dung']} />)
  expect(screen.getByRole('status')).toHaveTextContent('An, Bình và 2 người khác đang nhập')
  expect(screen.queryByText('Chi')).not.toBeInTheDocument()
})

it('renders user-provided names as text', () => {
  render(<TypingIndicator names={['<script>test-only</script>']} />)
  expect(screen.getByRole('status')).toHaveTextContent('<script>test-only</script> đang nhập')
  expect(document.querySelector('script')).toBeNull()
})
