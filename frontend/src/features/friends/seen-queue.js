// Account-local, DOM-observed dwell queue. No route-open/hover or optimistic count writes.
export function createSeenQueue({
  send,
  confirmed,
  failed,
  isCurrent,
  environment = window,
  doc = document,
}) {
  const rows = new Map()
  const pending = new Set()
  const completed = new Set()
  const inFlight = new Set()
  const abort = new AbortController()
  let stopped = false,
    sending = false,
    flushTimer
  const focused = () =>
    !stopped && isCurrent() && doc.visibilityState === 'visible' && doc.hasFocus()
  function visible(node) {
    if (!focused() || !node.isConnected) return false
    const r = node.getBoundingClientRect()
    const height = Math.max(0, Math.min(r.bottom, environment.innerHeight) - Math.max(0, r.top))
    const width = Math.max(0, Math.min(r.right, environment.innerWidth) - Math.max(0, r.left))
    return (
      r.height > 0 &&
      r.width > 0 &&
      width >= Math.min(r.width, environment.innerWidth) * 0.25 &&
      height >= Math.min(r.height, environment.innerHeight) * 0.25
    )
  }
  async function flush() {
    flushTimer = undefined
    if (sending || !focused()) return
    const ids = [...pending]
      .filter((id) => rows.has(id) && visible(rows.get(id).node))
      .slice(0, 100)
    if (!ids.length) return
    ids.forEach((id) => pending.delete(id))
    ids.forEach((id) => inFlight.add(id))
    sending = true
    try {
      await send(ids, abort.signal)
      if (!stopped && isCurrent()) {
        ids.forEach((id) => completed.add(id))
        confirmed()
      }
    } catch (error) {
      if (!stopped && isCurrent() && !abort.signal.aborted) {
        ids.forEach((id) => {
          if (rows.has(id)) rows.get(id).failed = true
        })
        failed(error)
      }
    } finally {
      sending = false
      ids.forEach((id) => inFlight.delete(id))
      if (!stopped && pending.size) flushTimer = setTimeout(flush, 0)
    }
  }
  function scan() {
    for (const [id, row] of rows) {
      if (!visible(row.node)) {
        clearTimeout(row.timer)
        row.timer = undefined
        pending.delete(id)
      } else if (
        !row.timer &&
        !row.failed &&
        !completed.has(id) &&
        !pending.has(id) &&
        !inFlight.has(id)
      ) {
        row.timer = setTimeout(() => {
          row.timer = undefined
          if (!stopped && visible(row.node) && !completed.has(id) && !row.failed) {
            pending.add(id)
            if (!flushTimer) flushTimer = setTimeout(flush, 0)
          }
        }, 500)
      }
    }
  }
  const observer =
    typeof environment.IntersectionObserver === 'function'
      ? new environment.IntersectionObserver(scan, { threshold: [0, 0.25, 1] })
      : null
  environment.addEventListener('scroll', scan, true)
  environment.addEventListener('resize', scan)
  environment.addEventListener('focus', scan)
  environment.addEventListener('blur', scan)
  doc.addEventListener('visibilitychange', scan)
  return {
    register(id, node) {
      const old = rows.get(id)
      if (old) {
        clearTimeout(old.timer)
        observer?.unobserve(old.node)
      }
      pending.delete(id)
      rows.delete(id)
      if (node && !stopped) {
        rows.set(id, { node })
        observer?.observe(node)
        scan()
      }
    },
    retry() {
      for (const row of rows.values()) row.failed = false
      scan()
    },
    stop() {
      stopped = true
      abort.abort()
      clearTimeout(flushTimer)
      for (const row of rows.values()) clearTimeout(row.timer)
      rows.clear()
      pending.clear()
      completed.clear()
      observer?.disconnect()
      environment.removeEventListener('scroll', scan, true)
      for (const event of ['resize', 'focus', 'blur']) environment.removeEventListener(event, scan)
      doc.removeEventListener('visibilitychange', scan)
    },
  }
}
