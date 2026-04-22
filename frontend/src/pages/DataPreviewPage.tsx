import { useState, useEffect } from 'react'
import { getFileData, analyzeFile } from '../services/api'
import type { UploadResponse, PreviewRow } from '../types'

interface Props {
  upload: UploadResponse
  onAnalyze: () => void
  onBack: () => void
}

export default function DataPreviewPage({ upload, onAnalyze, onBack }: Props) {
  const [rows,       setRows]       = useState<PreviewRow[]>(upload.preview)
  const [loading,    setLoading]    = useState(false)
  const [processing, setProcessing] = useState(false)
  const [error,      setError]      = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getFileData(upload.fileId)
      .then(data => { if (!cancelled) setRows(data) })
      .catch(e   => { if (!cancelled) setError(e?.message ?? 'Lỗi tải dữ liệu') })
      .finally(  () => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [upload.fileId])

  const handleStartAnalyze = async () => {
    setProcessing(true)
    setError(null)
    try {
      await analyzeFile(upload.fileId)
      onAnalyze() // Navigate to dashboard after success
    } catch (e: any) {
      setError(e?.response?.data?.message ?? e?.message ?? 'Lỗi phân tích dữ liệu')
    } finally {
      setProcessing(false)
    }
  }

  const columns = upload.columns
  const displayRows = rows.slice(0, 50)

  return (
    <div style={{ padding: '0 0 32px' }}>

      {/* Header */}
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        marginBottom: 32, flexWrap: 'wrap', gap: 16,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <button
            className="btn btn-outline"
            onClick={onBack}
            disabled={processing}
            style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '10px 16px' }}
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
                 stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="15 18 9 12 15 6" />
            </svg>
            Tải lại File
          </button>

          <div>
            <h2 style={{
              fontSize: 22, fontWeight: 900, color: 'var(--text-primary)',
              display: 'flex', alignItems: 'center', gap: 10, letterSpacing: '-0.02em'
            }}>
              <div style={{
                width: 38, height: 38, borderRadius: 12,
                background: 'linear-gradient(135deg, rgba(16,185,129,0.2), rgba(16,185,129,0.05))',
                color: 'var(--success)', border: '1px solid rgba(16,185,129,0.2)',
                display: 'flex', alignItems: 'center', justifyContent: 'center'
              }}>
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
                     stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                  <polyline points="14 2 14 8 20 8" />
                  <line x1="16" y1="13" x2="8" y2="13" />
                  <line x1="16" y1="17" x2="8" y2="17" />
                  <polyline points="10 9 9 9 8 9" />
                </svg>
              </div>
              {upload.fileName}
            </h2>
          </div>
        </div>

        <button
          id="btn-start-analyze"
          className="btn btn-primary"
          onClick={handleStartAnalyze}
          disabled={processing}
          style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '10px 24px', fontSize: 14 }}
        >
          {processing ? (
            <><span className="spinner" style={{ width: 16, height: 16, borderWidth: 2 }} /> Đang xử lý AI...</>
          ) : (
            <>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
                   stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2" />
              </svg>
              Bắt đầu phân tích →
            </>
          )}
        </button>
      </div>

      {/* Stats bar */}
      <div style={{
        display: 'flex', gap: 16, marginBottom: 24, flexWrap: 'wrap',
      }}>
        {[
          { label: 'Tổng số hàng', value: upload.rowCount.toLocaleString('vi-VN'), color: '#6366f1' },
          { label: 'Số cột',       value: String(columns.length),                  color: '#10b981' },
          { label: 'Xem trước',    value: `${displayRows.length} hàng`,            color: '#f59e0b' },
        ].map(s => (
          <div key={s.label} style={{
            padding: '16px 24px',
            background: 'var(--bg-card)',
            backdropFilter: 'blur(20px)',
            border: '1px solid var(--border)',
            borderRadius: 16,
            minWidth: 160,
            flex: 1,
            boxShadow: '0 4px 20px rgba(0,0,0,0.1)',
            position: 'relative', overflow: 'hidden'
          }}>
            <div style={{
              position: 'absolute', top: 0, left: 0, width: '4px', height: '100%',
              background: s.color, opacity: 0.8
            }} />
            <p style={{ fontSize: 12, fontWeight: 700, color: 'var(--text-muted)', marginBottom: 6, textTransform: 'uppercase', letterSpacing: '0.05em' }}>{s.label}</p>
            <p style={{ fontSize: 28, fontWeight: 900, color: s.color, letterSpacing: '-0.02em' }}>{s.value}</p>
          </div>
        ))}
      </div>

      {/* Column badges */}
      <div style={{
        padding: '16px 20px',
        background: 'var(--bg-card)',
        backdropFilter: 'blur(20px)',
        border: '1px solid var(--border)',
        borderRadius: 16,
        marginBottom: 24,
        display: 'flex',
        alignItems: 'flex-start',
        gap: 12,
        flexWrap: 'wrap',
        boxShadow: '0 4px 20px rgba(0,0,0,0.1)'
      }}>
        <p style={{ fontSize: 13, fontWeight: 800, color: 'var(--text-primary)', flexShrink: 0, marginTop: 4 }}>
          Cấu trúc cột:
        </p>
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          {columns.map((col, i) => {
            const colors = ['#818cf8', '#34d399', '#fbbf24', '#f43f5e', '#38bdf8', '#c084fc']
            const c = colors[i % colors.length]
            return (
              <span key={col} style={{
                padding: '4px 12px', borderRadius: 20,
                fontSize: 12, fontWeight: 700,
                background: `rgba(${c === '#818cf8' ? '129,140,248' : c === '#34d399' ? '52,211,153' : c === '#fbbf24' ? '251,191,36' : c === '#f43f5e' ? '244,63,94' : c === '#38bdf8' ? '56,189,248' : '192,132,252'}, 0.15)`,
                color: c,
                border: `1px solid rgba(${c === '#818cf8' ? '129,140,248' : c === '#34d399' ? '52,211,153' : c === '#fbbf24' ? '251,191,36' : c === '#f43f5e' ? '244,63,94' : c === '#38bdf8' ? '56,189,248' : '192,132,252'}, 0.3)`,
              }}>
                {col}
              </span>
            )
          })}
        </div>
      </div>

      {/* Data Table */}
      <div style={{
        background: 'var(--bg-card)',
        backdropFilter: 'blur(20px)',
        border: '1px solid var(--border)',
        borderRadius: 16,
        overflow: 'hidden',
        boxShadow: '0 8px 30px rgba(0,0,0,0.15)'
      }}>
        <div style={{
          padding: '16px 20px',
          borderBottom: '1px solid var(--border)',
          background: 'rgba(255,255,255,0.02)',
          display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        }}>
          <p style={{ fontSize: 14, fontWeight: 800, color: 'var(--text-primary)' }}>
            Xem trước dữ liệu <span style={{ color: 'var(--text-muted)', fontWeight: 600 }}>(tối đa 50 hàng)</span>
          </p>
          {loading && <span className="spinner" style={{ width: 16, height: 16, borderWidth: 2 }} />}
          {error && <span style={{ fontSize: 12, color: 'var(--error)', fontWeight: 600 }}>{error}</span>}
        </div>

        <div style={{ overflowX: 'auto', overflowY: 'auto', maxHeight: 500 }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
            <thead style={{ position: 'sticky', top: 0, zIndex: 1 }}>
              <tr>
                <th style={{
                  padding: '12px 16px', background: 'var(--bg-card-hover)', backdropFilter: 'blur(10px)',
                  borderBottom: '2px solid var(--border)', borderRight: '1px solid var(--border)',
                  textAlign: 'center', color: 'var(--text-muted)', fontWeight: 800, fontSize: 12,
                  width: 50, textTransform: 'uppercase', letterSpacing: '0.05em'
                }}>
                  #
                </th>
                {columns.map(col => (
                  <th key={col} style={{
                    padding: '12px 16px', background: 'var(--bg-card-hover)', backdropFilter: 'blur(10px)',
                    borderBottom: '2px solid var(--border)', borderRight: '1px solid var(--border)',
                    textAlign: 'left', color: 'var(--text-primary)', fontWeight: 800, fontSize: 12,
                    whiteSpace: 'nowrap', textTransform: 'uppercase', letterSpacing: '0.05em'
                  }}>
                    {col}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {displayRows.map((row, ri) => (
                <tr key={ri} style={{
                  borderBottom: '1px solid var(--border)',
                  background: ri % 2 === 0 ? 'transparent' : 'rgba(255,255,255,0.015)',
                  transition: 'background 0.2s ease'
                }} className="table-row-hover">
                  <td style={{
                    padding: '10px 16px', textAlign: 'center',
                    color: 'var(--text-muted)', fontSize: 12, fontWeight: 600,
                    borderRight: '1px solid var(--border)',
                  }}>
                    {ri + 1}
                  </td>
                  {columns.map(col => (
                    <td key={col} style={{
                      padding: '10px 16px',
                      color: 'var(--text-secondary)',
                      borderRight: '1px solid var(--border)',
                      maxWidth: 240,
                      whiteSpace: 'nowrap',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                    }}>
                      {String(row[col] ?? '')}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

    </div>
  )
}
