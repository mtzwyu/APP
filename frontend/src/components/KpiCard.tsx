import { useEffect, useRef } from 'react'

interface KpiCardProps {
  label: string
  value: string
  rawValue?: number
  change: number
  color: string
  icon: React.ReactNode
  sub?: string
  loading?: boolean
}

function AnimatedNumber({ target }: { target: number }) {
  const ref = useRef<HTMLSpanElement>(null)
  const frame = useRef<number>(0)

  useEffect(() => {
    if (!ref.current) return
    const start = 0
    const duration = 900
    const startTime = performance.now()

    const tick = (now: number) => {
      const elapsed = now - startTime
      const progress = Math.min(elapsed / duration, 1)
      // ease-out cubic
      const eased = 1 - Math.pow(1 - progress, 3)
      const current = Math.round(start + (target - start) * eased)
      if (ref.current) ref.current.textContent = current.toLocaleString()
      if (progress < 1) frame.current = requestAnimationFrame(tick)
    }

    frame.current = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(frame.current)
  }, [target])

  return <span ref={ref}>0</span>
}

export default function KpiCard({
  label, value, rawValue, change, color, icon, sub, loading
}: KpiCardProps) {
  const dir = change > 0 ? 'up' : change < 0 ? 'down' : 'flat'

  if (loading) {
    return (
      <div className="kpi-card skeleton-card">
        <div className="skeleton-line short" />
        <div className="skeleton-line tall" style={{ marginTop: 12 }} />
        <div className="skeleton-line mid" style={{ marginTop: 10 }} />
      </div>
    )
  }

  return (
    <div className="kpi-card fade-up" style={{ '--accent-color': color } as React.CSSProperties}>
      <div className="kpi-icon" style={{ color }}>{icon}</div>
      <div className="kpi-label">{label}</div>
      <div className="kpi-value" style={{ color }}>
        {rawValue !== undefined ? <AnimatedNumber target={rawValue} /> : value}
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 6 }}>
        <span className={`kpi-change ${dir}`}>
          {dir === 'up' ? '▲' : dir === 'down' ? '▼' : '—'}
          &nbsp;{Math.abs(change).toFixed(1)}%
        </span>
        {sub && <span className="kpi-sub">{sub}</span>}
      </div>
    </div>
  )
}
