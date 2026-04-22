import { useCallback } from 'react'
import type { AllConnectionStatus } from '../types'
import { getAllConnections } from '../services/api'
import { useApi } from '../hooks/useApi'

interface ConnStatus {
  connected: boolean
  source: string
  reason?: string | null
}

function ConnCard({
  label, status, color
}: { label: string; status: ConnStatus | null; color: string }) {
  const notConfigured = status?.reason?.toLowerCase().includes('not configured') ?? false

  return (
    <div className="kpi-card fade-up" style={{ '--accent-color': color } as React.CSSProperties}>
      <div className="kpi-icon" style={{ color }}>
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <ellipse cx="12" cy="5" rx="9" ry="3" />
          <path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3" />
          <path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5" />
        </svg>
      </div>
      <div className="kpi-label">{label}</div>

      {status === null ? (
        <div className="skeleton-line" style={{ marginTop: 12, height: 24 }} />
      ) : notConfigured ? (
        <>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6 }}>
            <span className="status-dot" style={{ background: 'var(--text-muted)' }} />
            <span style={{ fontWeight: 700, fontSize: 18, color: 'var(--text-muted)' }}>
              Not Configured
            </span>
          </div>
          <p style={{ fontSize: 12, color: 'var(--text-muted)' }}>
            Go to <strong>Settings</strong> to add your connection string
          </p>
        </>
      ) : (
        <>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 6 }}>
            <span className={`status-dot ${status.connected ? 'green' : 'red'}`} />
            <span style={{
              fontWeight: 700, fontSize: 20,
              color: status.connected ? 'var(--success)' : 'var(--error)'
            }}>
              {status.connected ? 'Connected' : 'Disconnected'}
            </span>
          </div>
          {status.source && (
            <p style={{ fontSize: 12, color: 'var(--text-muted)' }}>{status.source}</p>
          )}
        </>
      )}
    </div>
  )
}

export default function ConnectionPage() {
  const { data, loading, error, refetch } = useApi<AllConnectionStatus>(
    () => getAllConnections(),
    []
  )

  const handleRefetch = useCallback(() => refetch(), [refetch])

  const ssas = data
    ? ((data as any).ssas ?? (data as any).SSAS) as ConnStatus | null
    : null
  const sql = data
    ? ((data as any).sqlServer ?? (data as any).SqlServer) as ConnStatus | null
    : null

  return (
    <div className="page-content">
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 24 }}>
        <span style={{ fontSize: 20, fontWeight: 800 }}>Connection Status</span>
        <button
          className="btn btn-outline btn-sm"
          style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 6 }}
          onClick={handleRefetch}
          disabled={loading}
        >
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <polyline points="23 4 23 10 17 10" /><polyline points="1 20 1 14 7 14" />
            <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15" />
          </svg>
          {loading ? 'Testing...' : 'Test Now'}
        </button>
      </div>

      {error && <div className="error-msg" style={{ marginBottom: 16 }}>{error}</div>}

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <ConnCard label="SSAS (Analysis Services)" status={ssas} color="#8b5cf6" />
        <ConnCard label="SQL Server (Data Warehouse)" status={sql} color="#3b82f6" />
      </div>

      {data?.testedAt && (
        <p style={{ color: 'var(--text-muted)', fontSize: 12, marginTop: 16, textAlign: 'center' }}>
          Last tested: {new Date(data.testedAt).toLocaleString()}
        </p>
      )}
    </div>
  )
}
