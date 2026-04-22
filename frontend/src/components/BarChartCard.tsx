import {
  BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer,
  CartesianGrid, Cell
} from 'recharts'

interface BarItem {
  name: string
  value: number
}

interface Props {
  title: string
  badge?: string
  data: BarItem[]
  color?: string
  loading?: boolean
  onBarClick?: (item: BarItem) => void
}

/* ── Neon gradient palette ── */
const GRADIENTS = [
  { main: '#6366f1', light: '#818cf8', dark: '#4338ca', glow: 'rgba(99,102,241,0.5)' },
  { main: '#06b6d4', light: '#22d3ee', dark: '#0891b2', glow: 'rgba(6,182,212,0.5)' },
  { main: '#8b5cf6', light: '#a78bfa', dark: '#7c3aed', glow: 'rgba(139,92,246,0.5)' },
  { main: '#10b981', light: '#34d399', dark: '#059669', glow: 'rgba(16,185,129,0.5)' },
  { main: '#f59e0b', light: '#fbbf24', dark: '#d97706', glow: 'rgba(245,158,11,0.5)' },
  { main: '#ef4444', light: '#f87171', dark: '#dc2626', glow: 'rgba(239,68,68,0.5)' },
  { main: '#ec4899', light: '#f472b6', dark: '#db2777', glow: 'rgba(236,72,153,0.5)' },
  { main: '#14b8a6', light: '#2dd4bf', dark: '#0d9488', glow: 'rgba(20,184,166,0.5)' },
]

const CustomTooltip = ({ active, payload, label }: any) => {
  if (!active || !payload?.length) return null
  return (
    <div className="custom-tooltip">
      <div className="ct-label">{label}</div>
      <div className="ct-value" style={{ color: payload[0]?.fill }}>
        <strong>{payload[0]?.value?.toLocaleString()}</strong>
      </div>
    </div>
  )
}

/* 3D bar shape with neon glow, glass highlight & deep shadow */
const Bar3DShape = (props: any) => {
  const { x, y, width, height, index } = props
  if (!height || height <= 0) return null

  const depth = 10
  const g = GRADIENTS[index % GRADIENTS.length]
  const gradId = `bar3d-grad-${index}`
  const glowId = `bar3d-glow-${index}`
  const sideGradId = `bar3d-side-${index}`

  return (
    <g>
      <defs>
        {/* Front face gradient: light → main */}
        <linearGradient id={gradId} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={g.light} stopOpacity={1} />
          <stop offset="100%" stopColor={g.main} stopOpacity={0.85} />
        </linearGradient>
        {/* Side gradient: dark → darker */}
        <linearGradient id={sideGradId} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={g.main} stopOpacity={0.8} />
          <stop offset="100%" stopColor={g.dark} stopOpacity={0.95} />
        </linearGradient>
        {/* Glow filter */}
        <filter id={glowId} x="-50%" y="-50%" width="200%" height="200%">
          <feGaussianBlur stdDeviation="2.5" result="blur" />
          <feFlood floodColor={g.glow} result="color" />
          <feComposite in="color" in2="blur" operator="in" result="glow" />
          <feMerge>
            <feMergeNode in="glow" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>
      </defs>

      {/* Soft glow behind the bar */}
      <rect
        x={x - 3} y={y - 3}
        width={width + 6} height={height + 6}
        rx={6} fill={g.main} opacity={0.15}
        filter={`url(#${glowId})`}
      />

      {/* Front face with gradient */}
      <rect x={x} y={y} width={width} height={height}
        fill={`url(#${gradId})`} rx={3} ry={3} />

      {/* Right side face */}
      <path
        d={`M${x + width},${y} L${x + width + depth},${y - depth} L${x + width + depth},${y + height - depth} L${x + width},${y + height} Z`}
        fill={`url(#${sideGradId})`}
      />

      {/* Top face */}
      <path
        d={`M${x},${y} L${x + depth},${y - depth} L${x + width + depth},${y - depth} L${x + width},${y} Z`}
        fill={g.light}
        opacity={0.55}
      />

      {/* Glass reflection on front face */}
      <rect
        x={x + 2}
        y={y + 2}
        width={Math.max(width * 0.35, 4)}
        height={Math.min(height - 4, height * 0.8)}
        fill="rgba(255,255,255,0.12)"
        rx={2}
      />

      {/* Top edge highlight */}
      <line
        x1={x + 2} y1={y + 1}
        x2={x + width - 2} y2={y + 1}
        stroke="rgba(255,255,255,0.35)"
        strokeWidth={1}
        strokeLinecap="round"
      />
    </g>
  )
}

export default function BarChartCard({ title, badge, data, loading, onBarClick }: Props) {
  return (
    <div className="chart-card fade-up">
      <div className="chart-header">
        <h3>{title}</h3>
        {badge && <span className="chart-badge">{badge}</span>}
      </div>
      {loading ? (
        <div className="chart-skeleton" />
      ) : (
        <ResponsiveContainer width="100%" height={260}>
          <BarChart data={data} margin={{ top: 20, right: 20, left: -14, bottom: 40 }}
            onClick={(e: any) => {
              if (e?.activePayload?.[0] && onBarClick) {
                const item = e.activePayload[0].payload as BarItem
                onBarClick(item)
              }
            }}
          >
            <CartesianGrid strokeDasharray="3 3" stroke="rgba(30,58,95,0.3)" vertical={false} />
            <XAxis
              dataKey="name"
              tick={{ fontSize: 9, fill: '#64748b' }}
              angle={-35}
              textAnchor="end"
              interval={0}
            />
            <YAxis tick={{ fontSize: 10, fill: '#64748b' }} />
            <Tooltip content={<CustomTooltip />} cursor={{ fill: 'rgba(99,102,241,0.06)' }} />
            <Bar
              dataKey="value"
              shape={<Bar3DShape />}
              cursor="pointer"
              isAnimationActive
              animationDuration={800}
              animationEasing="ease-out"
            >
              {data.map((_, i) => (
                <Cell key={i} fill={GRADIENTS[i % GRADIENTS.length].main} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      )}
    </div>
  )
}
