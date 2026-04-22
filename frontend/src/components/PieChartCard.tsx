import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts'

interface PieItem {
  name: string
  value: number
}

interface Props {
  title: string
  badge?: string
  data: PieItem[]
  loading?: boolean
  onSliceClick?: (item: PieItem) => void
}

/* Neon gradient palette with glow colors */
const PALETTE = [
  { main: '#6366f1', light: '#a5b4fc', dark: '#4338ca', glow: 'rgba(99,102,241,0.6)' },
  { main: '#06b6d4', light: '#67e8f9', dark: '#0891b2', glow: 'rgba(6,182,212,0.6)' },
  { main: '#8b5cf6', light: '#c4b5fd', dark: '#7c3aed', glow: 'rgba(139,92,246,0.6)' },
  { main: '#10b981', light: '#6ee7b7', dark: '#059669', glow: 'rgba(16,185,129,0.6)' },
  { main: '#f59e0b', light: '#fde68a', dark: '#d97706', glow: 'rgba(245,158,11,0.6)' },
  { main: '#ef4444', light: '#fca5a5', dark: '#dc2626', glow: 'rgba(239,68,68,0.6)' },
  { main: '#ec4899', light: '#f9a8d4', dark: '#db2777', glow: 'rgba(236,72,153,0.6)' },
]

const CustomTooltip = ({ active, payload }: any) => {
  if (!active || !payload?.length) return null
  const p = payload[0]
  const pal = PALETTE[payload[0]?.payload?.idx % PALETTE.length] ?? PALETTE[0]
  return (
    <div className="custom-tooltip">
      <div className="ct-label">{p.name}</div>
      <div className="ct-value" style={{ color: pal.light }}>
        <strong>{p.value?.toLocaleString()}</strong>
        <span style={{ color: '#64748b', marginLeft: 6, fontSize: 11 }}>
          ({((p.percent ?? 0) * 100).toFixed(1)}%)
        </span>
      </div>
    </div>
  )
}

const renderLegend = (props: any) => {
  const { payload } = props
  return (
    <ul style={{ listStyle: 'none', padding: 0, margin: 0, fontSize: 11 }}>
      {payload.map((entry: any, i: number) => {
        const pal = PALETTE[i % PALETTE.length]
        return (
          <li key={i} style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 5 }}>
            <span style={{
              width: 10, height: 10, borderRadius: 3, flexShrink: 0,
              background: `linear-gradient(135deg, ${pal.light}, ${pal.main})`,
              boxShadow: `0 0 6px ${pal.glow}`,
            }} />
            <span style={{
              color: '#94a3b8', whiteSpace: 'nowrap', overflow: 'hidden',
              textOverflow: 'ellipsis', maxWidth: 110,
            }}>
              {entry.value}
            </span>
          </li>
        )
      })}
    </ul>
  )
}

export default function PieChartCard({ title, badge, data, loading, onSliceClick }: Props) {
  /* Add index to each item for palette lookup */
  const indexedData = data.map((d, i) => ({ ...d, idx: i }))

  return (
    <div className="chart-card fade-up">
      <div className="chart-header">
        <h3>{title}</h3>
        {badge && <span className="chart-badge">{badge}</span>}
      </div>
      {loading ? (
        <div className="chart-skeleton" />
      ) : (
        <div style={{ perspective: '700px', perspectiveOrigin: '50% 40%' }}>
          <div style={{ transform: 'rotateX(22deg)', transformStyle: 'preserve-3d' }}>
            <ResponsiveContainer width="100%" height={260}>
              <PieChart>
                <defs>
                  {PALETTE.map((p, i) => (
                    <linearGradient key={`pg-${i}`} id={`pie-grad-${i}`} x1="0" y1="0" x2="1" y2="1">
                      <stop offset="0%" stopColor={p.light} stopOpacity={0.95} />
                      <stop offset="100%" stopColor={p.dark} stopOpacity={1} />
                    </linearGradient>
                  ))}
                  {PALETTE.map((p, i) => (
                    <filter key={`pf-${i}`} id={`pie-glow-${i}`} x="-30%" y="-30%" width="160%" height="160%">
                      <feGaussianBlur stdDeviation="2.0" result="blur" />
                      <feFlood floodColor={p.glow} result="color" />
                      <feComposite in="color" in2="blur" operator="in" result="glow" />
                      <feMerge>
                        <feMergeNode in="glow" />
                        <feMergeNode in="SourceGraphic" />
                      </feMerge>
                    </filter>
                  ))}
                </defs>

                {/* Shadow/depth ring underneath */}
                <Pie
                  data={indexedData}
                  dataKey="value"
                  nameKey="name"
                  cx="45%"
                  cy="57%"
                  outerRadius={90}
                  innerRadius={42}
                  paddingAngle={2}
                  isAnimationActive={false}
                  stroke="none"
                >
                  {indexedData.map((_, i) => (
                    <Cell key={`shadow-${i}`} fill={PALETTE[i % PALETTE.length].dark} opacity={0.35} />
                  ))}
                </Pie>

                {/* Main donut with gradients & glow */}
                <Pie
                  data={indexedData}
                  dataKey="value"
                  nameKey="name"
                  cx="45%"
                  cy="50%"
                  outerRadius={87}
                  innerRadius={40}
                  paddingAngle={3}
                  isAnimationActive
                  animationDuration={800}
                  cursor="pointer"
                  onClick={(entry: any) => onSliceClick?.({ name: String(entry.name ?? ''), value: Number(entry.value ?? 0) })}
                >
                  {indexedData.map((_, i) => (
                    <Cell
                      key={i}
                      fill={`url(#pie-grad-${i % PALETTE.length})`}
                      stroke={PALETTE[i % PALETTE.length].light}
                      strokeWidth={1}
                      filter={`url(#pie-glow-${i % PALETTE.length})`}
                    />
                  ))}
                </Pie>

                <Tooltip content={<CustomTooltip />} />
                <Legend content={renderLegend} layout="vertical" align="right" verticalAlign="middle" />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </div>
      )}
    </div>
  )
}
