import { useState, useCallback } from 'react'
import type { PivotConfig, FilePivotResult } from '../types'

interface Props {
  columns: string[]
  fileId: string
  onConfigChange: (config: PivotConfig) => void
  result: FilePivotResult | null
  loading: boolean
}

type Zone = 'rows' | 'columns' | 'values'

const ZONE_COLORS: Record<Zone, { bg: string; border: string; text: string }> = {
  rows:    { bg: 'rgba(99,102,241,0.08)',  border: 'rgba(99,102,241,0.35)',  text: '#818cf8' },
  columns: { bg: 'rgba(16,185,129,0.08)', border: 'rgba(16,185,129,0.35)', text: '#34d399' },
  values:  { bg: 'rgba(245,158,11,0.08)', border: 'rgba(245,158,11,0.35)', text: '#fbbf24' },
}

const ZONE_LABELS: Record<Zone, string> = {
  rows: '↕ Hàng (Rows)',
  columns: '↔ Cột (Columns)',
  values: '∑ Giá trị (Values)',
}

function fmt(n: number) {
  return n >= 1000 ? n.toLocaleString('vi-VN') : String(n)
}

// Heatmap: giá trị max → đậm nhất
function heatColor(val: number, max: number) {
  if (max === 0) return 'transparent'
  const ratio = val / max
  return `rgba(99,102,241,${(ratio * 0.45).toFixed(3)})`
}

export default function PivotTableComponent({ columns, onConfigChange, result, loading }: Props) {
  const [config, setConfig] = useState<PivotConfig>({
    rows: [], columns: [], values: [], aggregation: 'Sum'
  })
  const [draggingCol, setDraggingCol] = useState<string | null>(null)
  const [dragOverZone, setDragOverZone] = useState<Zone | null>(null)

  // available = columns not in any zone
  const usedInZone = [...config.rows, ...config.columns, ...config.values]
  const available = columns.filter(c => !usedInZone.includes(c))

  const updateConfig = useCallback((newCfg: PivotConfig) => {
    setConfig(newCfg)
    onConfigChange(newCfg)
  }, [onConfigChange])

  // Remove from a zone
  const removeFromZone = (zone: Zone, col: string) => {
    updateConfig({
      ...config,
      [zone]: config[zone].filter(c => c !== col),
    })
  }

  // Drag from available fields into a zone
  const onDragStartAvail = (col: string) => setDraggingCol(col)

  const onDropZone = (zone: Zone) => {
    if (!draggingCol) return
    // Remove from other zones first
    const newCfg: PivotConfig = {
      rows:    config.rows.filter(c => c !== draggingCol),
      columns: config.columns.filter(c => c !== draggingCol),
      values:  config.values.filter(c => c !== draggingCol),
      aggregation: config.aggregation,
    }
    newCfg[zone] = [...newCfg[zone], draggingCol]
    setDraggingCol(null)
    setDragOverZone(null)
    updateConfig(newCfg)
  }

  // Drag a chip back to available
  const onDragStartChip = (col: string) => setDraggingCol(col)

  const onDropAvailable = () => {
    if (!draggingCol) return
    const newCfg: PivotConfig = {
      rows:    config.rows.filter(c => c !== draggingCol),
      columns: config.columns.filter(c => c !== draggingCol),
      values:  config.values.filter(c => c !== draggingCol),
      aggregation: config.aggregation,
    }
    setDraggingCol(null)
    updateConfig(newCfg)
  }

  const maxVal = result
    ? Math.max(...result.matrix.flat(), 1)
    : 1

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>

      {/* ── Config Panel ── */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: '1fr 1fr',
        gap: 16,
        background: 'var(--card)',
        borderRadius: 16,
        border: '1px solid var(--border)',
        padding: 20,
      }}>

        {/* Left: Available Fields */}
        <div>
          <p style={{ fontSize: 12, fontWeight: 700, color: 'var(--text-muted)', marginBottom: 10, letterSpacing: 0.5 }}>
            TRƯỜNG DỮ LIỆU
          </p>
          <div
            onDragOver={e => e.preventDefault()}
            onDrop={onDropAvailable}
            style={{
              minHeight: 80,
              padding: 10,
              borderRadius: 10,
              background: 'var(--surface)',
              border: '1px dashed var(--border)',
              display: 'flex',
              flexWrap: 'wrap',
              gap: 7,
              alignContent: 'flex-start',
            }}
          >
            {available.length === 0 ? (
              <p style={{ fontSize: 11, color: 'var(--text-muted)', padding: 4 }}>Tất cả đã được sử dụng</p>
            ) : (
              available.map(col => (
                <div
                  key={col}
                  draggable
                  onDragStart={() => onDragStartAvail(col)}
                  style={{
                    padding: '4px 10px',
                    borderRadius: 20,
                    fontSize: 12, fontWeight: 600,
                    background: 'var(--card)',
                    border: '1px solid var(--border)',
                    color: 'var(--text-secondary)',
                    cursor: 'grab',
                    userSelect: 'none',
                    transition: 'box-shadow 0.15s',
                  }}
                >
                  ⠿ {col}
                </div>
              ))
            )}
          </div>
        </div>

        {/* Right: Zone drops + Aggregation */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          {(['rows', 'columns', 'values'] as Zone[]).map(zone => {
            const zc = ZONE_COLORS[zone]
            const isOver = dragOverZone === zone
            return (
              <div key={zone}>
                <p style={{ fontSize: 11, fontWeight: 700, color: zc.text, marginBottom: 5, letterSpacing: 0.5 }}>
                  {ZONE_LABELS[zone]}
                </p>
                <div
                  onDragOver={e => { e.preventDefault(); setDragOverZone(zone) }}
                  onDragLeave={() => setDragOverZone(null)}
                  onDrop={() => onDropZone(zone)}
                  style={{
                    minHeight: 42,
                    padding: '6px 8px',
                    borderRadius: 10,
                    background: isOver ? zc.bg : 'var(--surface)',
                    border: `1.5px dashed ${isOver ? zc.border : 'var(--border)'}`,
                    display: 'flex',
                    flexWrap: 'wrap',
                    gap: 6,
                    transition: 'all 0.15s',
                    alignContent: 'flex-start',
                  }}
                >
                  {config[zone].map(col => (
                    <div
                      key={col}
                      draggable
                      onDragStart={() => onDragStartChip(col)}
                      style={{
                        padding: '3px 10px',
                        borderRadius: 20,
                        fontSize: 12, fontWeight: 600,
                        background: zc.bg,
                        color: zc.text,
                        border: `1px solid ${zc.border}`,
                        cursor: 'grab',
                        display: 'flex',
                        alignItems: 'center',
                        gap: 6,
                        userSelect: 'none',
                      }}
                    >
                      {col}
                      <span
                        onClick={() => removeFromZone(zone, col)}
                        style={{ cursor: 'pointer', opacity: 0.6, lineHeight: 1 }}
                        title="Xoá"
                      >×</span>
                    </div>
                  ))}
                  {config[zone].length === 0 && (
                    <p style={{ fontSize: 11, color: 'var(--text-muted)', padding: '2px 4px' }}>
                      Kéo trường vào đây
                    </p>
                  )}
                </div>
              </div>
            )
          })}

          {/* Aggregation */}
          <div style={{ display: 'flex', gap: 6, marginTop: 4 }}>
            {(['Sum', 'Count'] as const).map(agg => (
              <button
                key={agg}
                onClick={() => updateConfig({ ...config, aggregation: agg })}
                style={{
                  flex: 1, padding: '6px 0', borderRadius: 8,
                  border: 'none', cursor: 'pointer', fontSize: 12, fontWeight: 700,
                  transition: 'all 0.15s',
                  background: config.aggregation === agg ? '#6366f1' : 'var(--surface)',
                  color: config.aggregation === agg ? '#fff' : 'var(--text-muted)',
                  boxShadow: config.aggregation === agg ? '0 2px 8px rgba(99,102,241,0.4)' : 'none',
                }}
              >
                {agg === 'Sum' ? '∑ Sum' : '# Count'}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* ── Result Table ── */}
      <div style={{
        background: 'var(--card)',
        borderRadius: 16,
        border: '1px solid var(--border)',
        overflow: 'hidden',
      }}>
        <div style={{
          padding: '14px 20px',
          borderBottom: '1px solid var(--border)',
          display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          background: 'linear-gradient(135deg, rgba(99,102,241,0.06) 0%, rgba(139,92,246,0.06) 100%)',
        }}>
          <p style={{ fontWeight: 700, fontSize: 14, color: 'var(--text-primary)' }}>
            📊 Kết quả Pivot
          </p>
          {loading && <span className="spinner" style={{ width: 16, height: 16 }} />}
          {result && (
            <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>
              {result.rowLabels.length} hàng × {result.colLabels.length} cột
            </span>
          )}
        </div>

        <div style={{ overflowX: 'auto' }}>
          {!result && !loading ? (
            <div style={{ padding: '32px 20px', textAlign: 'center', color: 'var(--text-muted)', fontSize: 13 }}>
              Kéo thả cột vào Rows, Columns, Values để xem kết quả
            </div>
          ) : loading ? (
            <div style={{ padding: '32px 20px', textAlign: 'center', color: 'var(--text-muted)', fontSize: 13 }}>
              <span className="spinner" style={{ width: 20, height: 20, margin: '0 auto 8px', display: 'block' }} />
              Đang tính toán...
            </div>
          ) : result && (
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12 }}>
              <thead>
                <tr>
                  <th style={{
                    padding: '10px 16px', background: 'var(--surface)',
                    borderBottom: '1px solid var(--border)', borderRight: '1px solid var(--border)',
                    textAlign: 'left', color: 'var(--text-muted)', fontWeight: 700, fontSize: 11,
                    position: 'sticky', left: 0, zIndex: 1,
                  }}>
                    {config.rows[0] || '—'}
                  </th>
                  {result.colLabels.map(col => (
                    <th key={col} style={{
                      padding: '10px 16px', background: 'var(--surface)',
                      borderBottom: '1px solid var(--border)', borderRight: '1px solid var(--border)',
                      textAlign: 'right', color: 'var(--text-muted)', fontWeight: 700, fontSize: 11,
                      whiteSpace: 'nowrap',
                    }}>
                      {col}
                    </th>
                  ))}
                  <th style={{
                    padding: '10px 16px', background: 'var(--surface)',
                    borderBottom: '1px solid var(--border)',
                    textAlign: 'right', color: '#818cf8', fontWeight: 700, fontSize: 11,
                  }}>
                    Tổng
                  </th>
                </tr>
              </thead>
              <tbody>
                {result.rowLabels.map((row, ri) => {
                  const rowSum = result.matrix[ri].reduce((a, b) => a + b, 0)
                  return (
                    <tr key={row} style={{ borderBottom: '1px solid var(--border)' }}>
                      <td style={{
                        padding: '8px 16px',
                        fontWeight: 600, color: 'var(--text-primary)',
                        borderRight: '1px solid var(--border)',
                        background: 'var(--surface)',
                        position: 'sticky', left: 0, zIndex: 1,
                        whiteSpace: 'nowrap',
                      }}>
                        {row}
                      </td>
                      {result.matrix[ri].map((val, ci) => (
                        <td key={ci} style={{
                          padding: '8px 16px',
                          textAlign: 'right',
                          fontWeight: 500,
                          color: 'var(--text-primary)',
                          borderRight: '1px solid var(--border)',
                          background: heatColor(val, maxVal),
                          transition: 'background 0.2s',
                        }}>
                          {fmt(val)}
                        </td>
                      ))}
                      <td style={{
                        padding: '8px 16px', textAlign: 'right',
                        fontWeight: 700, color: '#818cf8',
                      }}>
                        {fmt(rowSum)}
                      </td>
                    </tr>
                  )
                })}
                {/* Total row */}
                <tr style={{ borderTop: '2px solid var(--border)', background: 'var(--surface)' }}>
                  <td style={{
                    padding: '9px 16px', fontWeight: 700,
                    color: '#818cf8', borderRight: '1px solid var(--border)',
                    position: 'sticky', left: 0, background: 'var(--surface)',
                  }}>
                    Tổng cộng
                  </td>
                  {result.colLabels.map((_, ci) => {
                    const colSum = result.matrix.reduce((acc, row) => acc + row[ci], 0)
                    return (
                      <td key={ci} style={{
                        padding: '9px 16px', textAlign: 'right',
                        fontWeight: 700, color: '#818cf8',
                        borderRight: '1px solid var(--border)',
                      }}>
                        {fmt(colSum)}
                      </td>
                    )
                  })}
                  <td style={{
                    padding: '9px 16px', textAlign: 'right',
                    fontWeight: 800, color: '#a78bfa',
                  }}>
                    {fmt(result.matrix.flat().reduce((a, b) => a + b, 0))}
                  </td>
                </tr>
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  )
}
