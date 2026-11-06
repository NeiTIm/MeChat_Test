// Avoid passing real credentials to locator.fill(), whose failure call log may include arguments.
// Authenticated traces/screenshots remain disabled in the live configuration.
export async function setAuthInputs(page, identifier, password) {
  await page.evaluate((values) => {
    const set = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set
    for (const [id, value] of Object.entries(values)) {
      const input = document.getElementById(id)
      if (!input) throw new Error('AUTH_INPUT_UNAVAILABLE')
      set.call(input, value)
      input.dispatchEvent(new Event('input', { bubbles: true }))
      input.dispatchEvent(new Event('change', { bubbles: true }))
    }
  }, { identifier, password })
}
