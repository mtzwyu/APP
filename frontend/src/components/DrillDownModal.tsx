import { useEffect, useRef } from 'react'
import type { QueryResult, DrillStep } from '../types'

interface Props {
  isOpen: boolean
  title?: string
  stack: DrillStep[]
  result: QueryResult | null
  loading: boolean
  error: string | null
  onClose: () => void
  onDrillUp: () => void
  onReset: () => void
}

const COLORS = ['#3b82f6', '#06b6d4', '#8b5cf6', '#10b981', '#f59e0b']

export default function DrillDownModal({
  isOpen, title = 'Drill-Down', stack, result, loading, error, onClose, onDrillUp, onReset
}: Props) {
  const overlayRef = useRef<HTMLDivElement>(null)

  // Close on backdrop click
  const handleBackdrop = (e: React.MouseEvent) => {
    if (e.target === overlayRef.current) onClose()
  }

  // Close on Escape
  useEffect(() => {
    if (!isOpen) return
    const fn = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
    window.addEventListener('keydown', fn)
    return () => window.removeEventListener('keydown', fn)
  }, [isOpen, onClose])

  if (!isOpen) return null

  return (
    <div className="modal-overlay" ref={overlayRef} onClick={handleBackdrop}>
      <div className="modal-panel slide-up">
        {/* Header */}
        <div className="modal-header">
          <div>
            <h2 style={{ fontSize: 16, fontWeight: 700 }}>{title}</h2>
            {/* Breadcrumb */}
            <div className="breadcrumb">
              <span className="bc-root" onClick={onReset}>Root</span>
              {stack.map((s, i) => (
                <span key={i}>
                  <span className="bc-sep">›</span>
                  <span
                    className={`bc-item ${i === stack.length - 1 ? 'active' : ''}`}
                    style={{ color: COLORS[i % COLORS.length] }}
                  >
                    {s.label}
                  </span>
                </span>
              ))}
            </div>
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            {stack.length > 0 && (
              <button className="btn btn-outline btn-sm" onClick={onDrillUp}>
                ↑ Drill Up
              </button>
            )}
            <button className="btn btn-outline btn-sm" onClick={onReset}>
              Reset
            </button>
            <button className="btn btn-icon btn-outline" onClick={onClose} title="Đóng">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            </button>
          </div>
        </div>

        {/* Body */}
        <div className="modal-body">
          {loading && (
            <div className="loading-overlay">
              <div className="spinner" style={{ width: 32, height: 32, borderWidth: 3 }} />
              <span>Đang drill-down...</span>
            </div>
          )}
          {error && !loading && (
            <div className="error-msg">{error}</div>
          )}
          {result && !loading && (
            <>
              <div style={{ display: 'flex', gap: 8, marginBottom: 12, alignItems: 'center' }}>
                <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>
                  {result.totalRows} kết quả · {result.executionTimeMs}ms
                </span>
                {result.fromCache && <span className="badge badge-yellow">Cached</span>}
              </div>
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>Member</th>
                      {result.headers.map(h => <th key={h}>{h}</th>)}
                    </tr>
                  </thead>
                  <tbody>
                    {result.rows.map((row, i) => (
                      <tr key={i}>
                        <td style={{ color: 'var(--text-muted)' }}>{i + 1}</td>
                        <td style={{ fontWeight: 500, color: 'var(--text-primary)' }}>
                          {row.axisValues.join(' / ')}
                        </td>
                        {row.formattedValues.map((v, j) => (
                          <td key={j}>{v || row.values[j]?.toLocaleString()}</td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {/* MDX */}
              {result.executedMdx && (
                <details style={{ marginTop: 12 }}>
                  <summary style={{ fontSize: 11, color: 'var(--text-muted)', cursor: 'pointer' }}>
                    MDX thực thi
                  </summary>
                  <div className="mdx-box" style={{ marginTop: 6 }}>{result.executedMdx}</div>
                </details>
              )}
            </>
          )}
          {!loading && !result && !error && (
            <div style={{ textAlign: 'center', color: 'var(--text-muted)', padding: 32, fontSize: 13 }}>
              Chọn một điểm dữ liệu trên chart để drill-down
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
