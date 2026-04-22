import { useState, useEffect, useCallback } from 'react'
import ChartComponent from '../components/ChartComponent'
import InsightPanel from '../components/InsightPanel'
import PivotTableComponent from '../components/PivotTableComponent'
import { processData, getInsight } from '../services/api'
import type { UploadResponse, PivotConfig, FilePivotResult, InsightDto } from '../types'

interface Props {
  upload: UploadResponse
  onBack: () => void
}

export default function FileDashboardPage({ upload, onBack }: Props) {
  const [pivotResult,  setPivotResult]  = useState<FilePivotResult | null>(null)
  const [pivotLoading, setPivotLoading] = useState(false)

  const [insights,        setInsights]        = useState<InsightDto[]>([])
  const [insightLoading,  setInsightLoading]  = useState(true)

  // chart data derived from pivot result (first column as series)
  const chartData = pivotResult
    ? pivotResult.rowLabels.map((label, i) => ({
        name: label,
        value: pivotResult.matrix[i].reduce((a, b) => a + b, 0),
      }))
    : []

  // Load insights once on mount
  useEffect(() => {
    let cancelled = false
    setInsightLoading(true)
    getInsight(upload.fileId)
      .then(ins => { if (!cancelled) setInsights(ins) })
      .catch(() => {})
      .finally(() => { if (!cancelled) setInsightLoading(false) })
    return () => { cancelled = true }
  }, [upload.fileId])

  const handleConfigChange = useCallback(async (config: PivotConfig) => {
    const hasData = config.rows.length > 0 || config.columns.length > 0 || config.values.length > 0
    if (!hasData) { setPivotResult(null); return }
    setPivotLoading(true)
    try {
      const result = await processData(upload.fileId, config)
      setPivotResult(result)
    } catch {
      // ignore
    } finally {
      setPivotLoading(false)
    }
  }, [upload.fileId])

  return (
    <div style={{ paddingBottom: 40 }}>

      {/* Header bar */}
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        marginBottom: 20, flexWrap: 'wrap', gap: 10,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <button
            className="btn btn-outline btn-sm"
            onClick={onBack}
            style={{ display: 'flex', alignItems: 'center', gap: 6 }}
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
                 stroke="currentColor" strokeWidth="2.5">
              <polyline points="15 18 9 12 15 6" />
            </svg>
            Xem lại Preview
          </button>
          <div>
            <h2 style={{ fontSize: 17, fontWeight: 800, color: 'var(--text-primary)' }}>
              📊 File Dashboard
            </h2>
            <p style={{ fontSize: 11, color: 'var(--text-muted)', marginTop: 1 }}>
              {upload.fileName} · {upload.rowCount.toLocaleString('vi-VN')} hàng · {upload.columns.length} cột
            </p>
          </div>
        </div>

        {/* Quick stats */}
        <div style={{ display: 'flex', gap: 8 }}>
          {[
            { label: 'Hàng', value: upload.rowCount.toLocaleString('vi-VN'), color: '#6366f1' },
            { label: 'Cột',  value: String(upload.columns.length),           color: '#10b981' },
          ].map(s => (
            <div key={s.label} style={{
              padding: '6px 14px',
              borderRadius: 10,
              background: 'var(--card)',
              border: '1px solid var(--border)',
              textAlign: 'center',
            }}>
              <p style={{ fontSize: 16, fontWeight: 800, color: s.color }}>{s.value}</p>
              <p style={{ fontSize: 10, color: 'var(--text-muted)' }}>{s.label}</p>
            </div>
          ))}
        </div>
      </div>

      {/* Insight Panel (top) */}
      <InsightPanel insights={insights} loading={insightLoading} />

      {/* Main Grid: Pivot Config + Chart */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: '1fr 1fr',
        gap: 20,
        marginBottom: 20,
      }}>

        {/* Left: Pivot Table Component (config + result) */}
        <div style={{ gridColumn: '1 / -1' }}>
          <PivotTableComponent
            columns={upload.columns}
            fileId={upload.fileId}
            onConfigChange={handleConfigChange}
            result={pivotResult}
            loading={pivotLoading}
          />
        </div>
      </div>

      {/* Chart area */}
      {chartData.length > 0 && (
        <div style={{
          background: 'var(--card)',
          border: '1px solid var(--border)',
          borderRadius: 16,
          padding: 20,
          height: 360,
        }}>
          <ChartComponent
            data={chartData}
            title="Biểu đồ từ Pivot Data"
            color="#6366f1"
          />
        </div>
      )}

      {chartData.length === 0 && (
        <div style={{
          background: 'var(--card)',
          border: '1px dashed var(--border)',
          borderRadius: 16,
          padding: '32px 20px',
          textAlign: 'center',
          color: 'var(--text-muted)',
          fontSize: 13,
        }}>
          <p style={{ fontSize: 24, marginBottom: 8 }}>📈</p>
          Kéo thả cột vào Pivot Table để tạo biểu đồ
        </div>
      )}

    </div>
  )
}
