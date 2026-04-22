import { useState } from 'react'
import {
  ResponsiveContainer,
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend,
  LineChart, Line,
  PieChart, Pie, Cell, Sector,
} from 'recharts'

interface DataPoint {
  name: string
  value: number
  [key: string]: string | number
}

interface Props {
  data: DataPoint[]
  title?: string
  color?: string
  multiSeries?: { key: string; color: string }[]
}

type ChartType = 'bar' | 'line' | 'pie'

const COLORS = ['#6366f1', '#10b981', '#f59e0b', '#ef4444', '#3b82f6', '#8b5cf6', '#ec4899']

// Custom tooltip
function CustomTooltip({ active, payload, label }: any) {
  if (!active || !payload?.length) return null
  return (
    <div style={{
      background: 'var(--card)',
      border: '1px solid var(--border)',
      borderRadius: 10,
      padding: '10px 14px',
      fontSize: 12,
      boxShadow: '0 8px 24px rgba(0,0,0,0.3)',
    }}>
      {label && <p style={{ fontWeight: 700, color: 'var(--text-primary)', marginBottom: 6 }}>{label}</p>}
      {payload.map((p: any, i: number) => (
        <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 3 }}>
          <div style={{ width: 8, height: 8, borderRadius: '50%', background: p.color ?? p.fill }} />
          <span style={{ color: 'var(--text-muted)' }}>{p.name ?? p.dataKey}:</span>
          <span style={{ fontWeight: 700, color: 'var(--text-primary)' }}>
            {typeof p.value === 'number' ? p.value.toLocaleString() : p.value}
          </span>
        </div>
      ))}
    </div>
  )
}

// Active pie sector (pop-out effect)
function renderActiveShape(props: any) {
  const { cx, cy, innerRadius, outerRadius, startAngle, endAngle, fill, payload, percent, value } = props
  return (
    <g>
      <text x={cx} y={cy - 12} textAnchor="middle" fill="var(--text-primary)" style={{ fontSize: 14, fontWeight: 700 }}>
        {payload.name}
      </text>
      <text x={cx} y={cy + 12} textAnchor="middle" fill="var(--text-muted)" style={{ fontSize: 12 }}>
        {value.toLocaleString()} ({(percent * 100).toFixed(1)}%)
      </text>
      <Sector cx={cx} cy={cy} innerRadius={innerRadius} outerRadius={outerRadius + 8}
              startAngle={startAngle} endAngle={endAngle} fill={fill} />
      <Sector cx={cx} cy={cy} innerRadius={outerRadius + 12} outerRadius={outerRadius + 15}
              startAngle={startAngle} endAngle={endAngle} fill={fill} />
    </g>
  )
}

export default function ChartComponent({ data, title, color = '#6366f1', multiSeries }: Props) {
  const [type, setType] = useState<ChartType>('bar')

  const btnStyle = (t: ChartType) => ({
    padding: '5px 14px',
    borderRadius: 8,
    border: 'none',
    fontSize: 12,
    fontWeight: 600,
    cursor: 'pointer',
    transition: 'all 0.18s',
    background: type === t ? color : 'var(--surface)',
    color: type === t ? '#fff' : 'var(--text-muted)',
    boxShadow: type === t ? `0 2px 8px ${color}55` : 'none',
  })

  const axisProps = {
    tick: { fill: 'var(--text-muted)', fontSize: 11 },
    axisLine: { stroke: 'var(--border)' },
    tickLine: false as const,
  }

  return (
    <div style={{
      background: 'var(--card)',
      borderRadius: 16,
      border: '1px solid var(--border)',
      padding: '20px 24px',
      height: '100%',
      display: 'flex',
      flexDirection: 'column',
    }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 16, flexWrap: 'wrap', gap: 8 }}>
        {title && (
          <p style={{ fontWeight: 700, fontSize: 15, color: 'var(--text-primary)' }}>{title}</p>
        )}
        <div style={{ display: 'flex', gap: 6 }}>
          {(['bar', 'line', 'pie'] as ChartType[]).map(t => (
            <button key={t} style={btnStyle(t)} onClick={() => setType(t)}>
              {t === 'bar' ? '📊 Bar' : t === 'line' ? '📈 Line' : '🥧 Pie'}
            </button>
          ))}
        </div>
      </div>

      {/* Chart */}
      <div style={{ flex: 1, minHeight: 0, minWidth: 0 }}>
        {data.length === 0 ? (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', color: 'var(--text-muted)', fontSize: 13 }}>
            Chưa có dữ liệu để hiển thị biểu đồ
          </div>
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            {type === 'bar' ? (
              <BarChart data={data} margin={{ top: 4, right: 12, bottom: 4, left: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                <XAxis dataKey="name" {...axisProps} />
                <YAxis {...axisProps} />
                <Tooltip content={<CustomTooltip />} />
                {multiSeries ? (
                  <>
                    <Legend wrapperStyle={{ fontSize: 11, color: 'var(--text-muted)' }} />
                    {multiSeries.map(s => (
                      <Bar key={s.key} dataKey={s.key} fill={s.color} radius={[4, 4, 0, 0]} />
                    ))}
                  </>
                ) : (
                  <Bar dataKey="value" radius={[4, 4, 0, 0]}>
                    {data.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
                  </Bar>
                )}
              </BarChart>
            ) : type === 'line' ? (
              <LineChart data={data} margin={{ top: 4, right: 12, bottom: 4, left: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                <XAxis dataKey="name" {...axisProps} />
                <YAxis {...axisProps} />
                <Tooltip content={<CustomTooltip />} />
                {multiSeries ? (
                  <>
                    <Legend wrapperStyle={{ fontSize: 11, color: 'var(--text-muted)' }} />
                    {multiSeries.map(s => (
                      <Line key={s.key} type="monotone" dataKey={s.key}
                            stroke={s.color} strokeWidth={2.5} dot={{ r: 4, fill: s.color }} />
                    ))}
                  </>
                ) : (
                  <Line type="monotone" dataKey="value"
                        stroke={color} strokeWidth={2.5}
                        dot={{ r: 4, fill: color }}
                        activeDot={{ r: 6 }} />
                )}
              </LineChart>
            ) : (
              <PieChart>
                <Pie
                  data={data}
                  dataKey="value"
                  nameKey="name"
                  cx="50%" cy="50%"
                  innerRadius="38%"
                  outerRadius="65%"
                  activeShape={renderActiveShape}
                >
                  {data.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
                </Pie>
                <Tooltip content={<CustomTooltip />} />
              </PieChart>
            )}
          </ResponsiveContainer>
        )}
      </div>
    </div>
  )
}
