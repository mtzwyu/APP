import { useState, useMemo } from 'react'
import type { QueryResult } from '../types'

interface Props {
  data: QueryResult | null
  loading?: boolean
  onDrillDown?: (member: string, period: string) => void
}

// ─── Utility: build pivot rows from QueryResult ───────────────────────────────
function buildPivot(result: QueryResult) {
  // Rows = time periods from axisValues[0]
  // Cols = headers (measures)
  const rowMap = new Map<string, number[]>()

  for (const row of result.rows) {
    const key = row.axisValues[0] ?? '(unknown)'
    rowMap.set(key, row.values)
  }

  return { headers: result.headers, rowMap }
}

// ─── Heatmap colour for a value relative to min/max ──────────────────────────
function heatmapColor(value: number, min: number, max: number): string {
  if (max === min) return 'rgba(59,130,246,0.15)'
  const t = (value - min) / (max - min) // 0 = min, 1 = max
  if (t < 0.33) return `rgba(16,185,129,${0.1 + t * 0.4})`   // green low
  if (t < 0.66) return `rgba(245,158,11,${0.1 + t * 0.4})`   // amber mid
  return `rgba(239,68,68,${0.1 + t * 0.4})`                   // red high
}

// ─── Export CSV util ──────────────────────────────────────────────────────────
function exportCSV(headers: string[], rowMap: Map<string, number[]>) {
  const cols = ['Period', ...headers]
  const lines = [cols.join(',')]
  for (const [period, vals] of rowMap) {
    lines.push([period, ...vals.map(v => v.toFixed(2))].join(','))
  }
  const blob = new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `pivot_${Date.now()}.csv`
  a.click()
  URL.revokeObjectURL(url)
}

export default function PivotTable({ data, loading, onDrillDown }: Props) {
  const [sortAsc, setSortAsc] = useState(true)
  const [expanded, setExpanded] = useState<Set<string>>(new Set())

  const { headers, rowMap } = useMemo(
    () => (data ? buildPivot(data) : { headers: [], rowMap: new Map<string, number[]>() }),
    [data]
  )

  // Compute heatmap range per column
  const colRanges = useMemo<{ min: number; max: number }[]>(() => {
    return headers.map((_, ci) => {
      const vals = [...rowMap.values()].map(v => v[ci] ?? 0)
      return { min: Math.min(...vals), max: Math.max(...vals) }
    })
  }, [headers, rowMap])

  const rows = useMemo(() => {
    const arr = [...rowMap.entries()]
    arr.sort(([a], [b]) => {
      const numA = parseFloat(a)
      const numB = parseFloat(b)
      if (!isNaN(numA) && !isNaN(numB)) {
        return sortAsc ? numA - numB : numB - numA
      }
      return sortAsc ? a.localeCompare(b) : b.localeCompare(a)
    })
    return arr
  }, [rowMap, sortAsc])

  const totals = useMemo(() => {
    return headers.map((_, ci) =>
      rows.reduce((sum, [, vals]) => sum + (vals[ci] ?? 0), 0)
    )
  }, [headers, rows])

  if (loading) {
    return (
      <div className="chart-card">
        <div className="chart-header">
          <h3>Pivot Table</h3>
        </div>
        <div className="skeleton-line" style={{ height: 200 }} />
      </div>
    )
  }

  if (!data || !rows.length) {
    return (
      <div className="chart-card">
        <div className="chart-header"><h3>Pivot Table</h3></div>
        <div style={{ padding: '32px', textAlign: 'center', color: 'var(--text-muted)', fontSize: 13 }}>
          Chưa có dữ liệu
        </div>
      </div>
    )
  }

  return (
    <div className="chart-card fade-up">
      <div className="chart-header">
        <h3>Pivot Table</h3>
        <div style={{ display: 'flex', gap: 8 }}>
          <span className="chart-badge">{data.totalRows} dòng · {data.executionTimeMs}ms</span>
          {data.fromCache && <span className="badge badge-yellow">Cached</span>}
          <button
            className="btn btn-outline btn-sm"
            style={{ display: 'flex', alignItems: 'center', gap: 5 }}
            onClick={() => exportCSV(headers, rowMap)}
          >
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" /><polyline points="7 10 12 15 17 10" /><line x1="12" y1="15" x2="12" y2="3" />
            </svg>
            Export CSV
          </button>
        </div>
      </div>

      <div className="table-wrap pivot-wrap">
        <table className="pivot-table">
          <thead>
            <tr>
              <th
                className="pivot-th-row"
                onClick={() => setSortAsc(p => !p)}
                title="Click để sắp xếp"
                style={{ cursor: 'pointer', userSelect: 'none' }}
              >
                Kỳ {sortAsc ? '▲' : '▼'}
              </th>
              {headers.map(h => (
                <th key={h} className="pivot-th-col">{h}</th>
              ))}
              <th className="pivot-th-col" style={{ color: 'var(--accent)' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {rows.map(([period, vals]) => {
              const isExpanded = expanded.has(period)
              return (
                <tr key={period} className="pivot-row">
                  <td className="pivot-cell-row">
                    <button
                      className="expand-btn"
                      onClick={() => setExpanded(prev => {
                        const next = new Set(prev)
                        next.has(period) ? next.delete(period) : next.add(period)
                        return next
                      })}
                      title={isExpanded ? 'Thu gọn' : 'Mở rộng'}
                    >
                      {isExpanded ? '▾' : '▸'}
                    </button>
                    <span style={{ fontWeight: 500, color: 'var(--text-primary)' }}>{period}</span>
                  </td>
                  {vals.map((v, ci) => (
                    <td
                      key={ci}
                      className="pivot-cell-val"
                      style={{ background: heatmapColor(v, colRanges[ci].min, colRanges[ci].max) }}
                    >
                      {v.toLocaleString(undefined, { maximumFractionDigits: 0 })}
                    </td>
                  ))}
                  <td className="pivot-cell-val">
                    <button
                      className="btn btn-outline btn-sm"
                      style={{ fontSize: 11, padding: '3px 8px' }}
                      onClick={() => onDrillDown?.(period, period)}
                    >
                      Drill ↓
                    </button>
                  </td>
                </tr>
              )
            })}
          </tbody>
          <tfoot>
            <tr className="pivot-total-row">
              <td className="pivot-cell-row" style={{ fontWeight: 700, color: 'var(--text-primary)' }}>
                Tổng
              </td>
              {totals.map((t, i) => (
                <td key={i} className="pivot-cell-val" style={{ fontWeight: 700, color: 'var(--accent)' }}>
                  {t.toLocaleString(undefined, { maximumFractionDigits: 0 })}
                </td>
              ))}
              <td />
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  )
}
