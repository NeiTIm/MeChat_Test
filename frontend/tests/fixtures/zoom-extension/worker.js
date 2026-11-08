/* global chrome */
// Test-only extension. Chrome tabs.setZoom changes browser zoom, not CSS zoom,
// viewport emulation or deviceScaleFactor. No production registration.
chrome.runtime.onInstalled.addListener(() => {})
