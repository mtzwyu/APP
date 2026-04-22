import { useState } from 'react'
import UploadComponent from '../components/UploadComponent'
import { uploadFile } from '../services/api'
import type { UploadResponse } from '../types'

interface Props {
  onDone: (resp: UploadResponse) => void
}

const FEATURES = [
  { icon: '📊', label: 'Pivot Table' },
  { icon: '📈', label: 'Interactive Charts' },
  { icon: '🤖', label: 'AI Star Schema' },
  { icon: '⚡', label: 'Instant Analytics' },
]

export default function UploadPage({ onDone }: Props) {
  const [loading, setLoading] = useState(false)
  const [error,   setError]   = useState<string | null>(null)

  const handleFile = async (file: File) => {
    setLoading(true); setError(null)
    try {
      const resp = await uploadFile(file)
      onDone(resp)
    } catch (e: any) {
      setError(e?.response?.data?.message ?? e?.message ?? 'Upload thất bại.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="page-content" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '100%' }}>
      <div style={{
        maxWidth: 540, width: '100%',
        background: 'var(--bg-card)',
        borderRadius: 24,
        border: '1px solid var(--border)',
        boxShadow: '0 24px 50px rgba(0,0,0,0.2), 0 0 0 1px rgba(255,255,255,0.02) inset',
        padding: '36px',
        display: 'flex', flexDirection: 'column', gap: 28,
        position: 'relative', overflow: 'hidden'
      }}>
        {/* Subtle background glow */}
        <div style={{ position: 'absolute', top: -80, left: -80, width: 250, height: 250, background: 'var(--accent)', filter: 'blur(90px)', opacity: 0.15, borderRadius: '50%', pointerEvents: 'none' }} />

        {/* Header */}
        <div style={{ textAlign: 'center', position: 'relative', zIndex: 1 }}>
          <div style={{
            width: 56, height: 56, borderRadius: 16, margin: '0 auto 16px',
            background: 'linear-gradient(135deg, #6366f1, #8b5cf6)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            boxShadow: '0 8px 24px rgba(99,102,241,0.3)',
          }}>
            <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
              <polyline points="17 8 12 3 7 8"/>
              <line x1="12" y1="3" x2="12" y2="15"/>
            </svg>
          </div>
          <h2 style={{ fontSize: 24, fontWeight: 800, margin: '0 0 10px 0', color: 'var(--text-primary)' }}>Data Import</h2>
          <p style={{ fontSize: 13, color: 'var(--text-muted)', margin: 0, lineHeight: 1.6 }}>
            Tải lên file CSV hoặc Excel của bạn.<br />
            Hệ thống AI sẽ tự động phân tích và xây dựng Data Warehouse & SSAS Cube.
          </p>
        </div>

        {/* Upload Dropzone */}
        <div style={{ position: 'relative', zIndex: 1 }}>
          <UploadComponent onFileSelected={handleFile} loading={loading} error={error} />
        </div>

        {/* Features pills */}
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', justifyContent: 'center', position: 'relative', zIndex: 1 }}>
          {FEATURES.map(f => (
            <div key={f.label} style={{
              display: 'flex', alignItems: 'center', gap: 6,
              padding: '6px 12px', borderRadius: 20,
              background: 'rgba(255,255,255,0.03)', border: '1px solid var(--border)',
              fontSize: 11, fontWeight: 600, color: 'var(--text-secondary)'
            }}>
              <span style={{ fontSize: 14 }}>{f.icon}</span> {f.label}
            </div>
          ))}
        </div>

      </div>
    </div>
  )
}
