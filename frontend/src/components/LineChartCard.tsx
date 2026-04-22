import {
  AreaChart, Area, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid
} from 'recharts'
import type { TrendPoint } from '../types'

interface Props {
  title: string
  badge?: string
  data: TrendPoint[]
  color?: string
  loading?: boolean
  onPointClick?: (point: TrendPoint) => void
}

const CustomTooltip = ({ active, payload, label }: any) => {
  if (!active || !payload?.length) return null
  return (
    <div className="custom-tooltip">
      <div className="ct-label">{label}</div>
      {payload.map((p: any, i: number) => (
        <div key={i} className="ct-value" style={{ color: p.color }}>
          {p.name}: <strong>{typeof p.value === 'number' ? p.value.toLocaleString() : p.value}</strong>
        </div>
      ))}
    </div>
  )
}

export default function LineChartCard({ title, badge, data, color = '#6366f1', loading, onPointClick }: Props) {
  const uid = title.replace(/\s/g, '')
  const gradFillId = `line-fill-${uid}`
  const gradStrokeId = `line-stroke-${uid}`
  const glowFilterId = `line-glow-${uid}`
  const dotGlowId = `dot-glow-${uid}`

  const chartData = data.map(d => ({
    period: d.period.length > 10 ? d.period.slice(0, 10) : d.period,
    value: +(d.value / 1000).toFixed(1),
    rawPoint: d,
  }))

  /* Derive neon palette from base color */
  const lighten = (c: string, amt: number) => {
    const num = parseInt(c.replace('#', ''), 16)
    const r = Math.min(255, (num >> 16) + amt)
    const g = Math.min(255, ((num >> 8) & 0x00FF) + amt)
    const b = Math.min(255, (num & 0x0000FF) + amt)
    return `rgb(${r},${g},${b})`
  }
  const neonLight = lighten(color, 80)

  return (
    <div className="chart-card fade-up">
      <div className="chart-header">
        <h3>{title}</h3>
        {badge && <span className="chart-badge">{badge}</span>}
      </div>
      {loading ? (
        <div className="chart-skeleton" />
      ) : (
        <div style={{ perspective: '900px', perspectiveOrigin: '50% 0%' }}>
          <div style={{ transform: 'rotateX(6deg)', transformOrigin: 'center bottom' }}>
            <ResponsiveContainer width="100%" height={245}>
              <AreaChart data={chartData} margin={{ top: 8, right: 8, left: -14, bottom: 0 }}
                onClick={(e: any) => {
                  if (e?.activePayload?.[0]?.payload?.rawPoint && onPointClick) {
                    onPointClick(e.activePayload[0].payload.rawPoint)
                  }
                }}
              >
                <defs>
                  {/* Multi-stop area fill for depth illusion */}
                  <linearGradient id={gradFillId} x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor={neonLight} stopOpacity={0.5} />
                    <stop offset="30%" stopColor={color} stopOpacity={0.25} />
                    <stop offset="70%" stopColor={color} stopOpacity={0.08} />
                    <stop offset="100%" stopColor={color} stopOpacity={0.01} />
                  </linearGradient>

                  {/* Neon stroke gradient */}
                  <linearGradient id={gradStrokeId} x1="0" y1="0" x2="1" y2="0">
                    <stop offset="0%" stopColor={neonLight} />
                    <stop offset="50%" stopColor={color} />
                    <stop offset="100%" stopColor={neonLight} />
                  </linearGradient>

                  {/* Strong glow filter for the line */}
                  <filter id={glowFilterId} x="-20%" y="-20%" width="140%" height="140%">
                    <feGaussianBlur stdDeviation="2.5" result="blur" />
                    <feFlood floodColor={color} floodOpacity="0.6" result="color" />
                    <feComposite in="color" in2="blur" operator="in" result="glow" />
                    <feMerge>
                      <feMergeNode in="glow" />
                      <feMergeNode in="glow" />
                      <feMergeNode in="SourceGraphic" />
                    </feMerge>
                  </filter>

                  {/* Dot glow */}
                  <filter id={dotGlowId}>
                    <feGaussianBlur stdDeviation="2.5" />
                  </filter>
                </defs>

                <CartesianGrid strokeDasharray="3 3" stroke="rgba(30,58,95,0.3)" />
                <XAxis dataKey="period" tick={{ fontSize: 10, fill: '#64748b' }} />
                <YAxis tick={{ fontSize: 10, fill: '#64748b' }} />
                <Tooltip content={<CustomTooltip />} />

                {/* Soft glow shadow layer underneath */}
                <Area
                  type="monotone"
                  dataKey="value"
                  stroke="none"
                  fill={color}
                  fillOpacity={0.08}
                  strokeWidth={0}
                  dot={false}
                  activeDot={false}
                  isAnimationActive={false}
                />

                {/* Main neon area */}
                <Area
                  type="monotone"
                  dataKey="value"
                  stroke={`url(#${gradStrokeId})`}
                  fill={`url(#${gradFillId})`}
                  strokeWidth={3}
                  name="Value (K)"
                  filter={`url(#${glowFilterId})`}
                  dot={{
                    r: 4,
                    fill: neonLight,
                    stroke: color,
                    strokeWidth: 2,
                  }}
                  activeDot={{
                    r: 8,
                    fill: neonLight,
                    stroke: color,
                    strokeWidth: 3,
                    cursor: 'pointer',
                    // @ts-ignore
                    filter: `drop-shadow(0 0 6px ${color})`,
                  }}
                />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>
      )}
    </div>
  )
}
