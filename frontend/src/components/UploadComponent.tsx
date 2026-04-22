import { useState, useRef, useCallback } from 'react'

interface Props {
  onFileSelected: (file: File) => void
  loading?: boolean
  error?: string | null
}

const ACCEPTED = ['.csv', '.xlsx', '.xls']
const MAX_MB = 50

function formatFileSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

function getFileExt(name: string) {
  return name.split('.').pop()?.toLowerCase() ?? ''
}

export default function UploadComponent({ onFileSelected, loading, error }: Props) {
  const [dragging, setDragging] = useState(false)
  const [file,     setFile]     = useState<File | null>(null)
  const [valErr,   setValErr]   = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  const validate = (f: File): string | null => {
    const ext = getFileExt(f.name)
    if (!ACCEPTED.includes(`.${ext}`)) return `Định dạng ".${ext}" không hỗ trợ. Dùng CSV hoặc Excel.`
    if (f.size > MAX_MB * 1024 * 1024) return `File quá lớn (${formatFileSize(f.size)}). Tối đa ${MAX_MB}MB.`
    return null
  }

  const handleFile = useCallback((f: File) => {
    const err = validate(f)
    if (err) { setValErr(err); return }
    setValErr(null); setFile(f)
  }, [])

  const onDragOver  = (e: React.DragEvent) => { e.preventDefault(); setDragging(true) }
  const onDragLeave = () => setDragging(false)
  const onDrop      = (e: React.DragEvent) => {
    e.preventDefault(); setDragging(false)
    const f = e.dataTransfer.files[0]
    if (f) handleFile(f)
  }
  const onInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0]
    if (f) handleFile(f)
  }

  const handleSubmit = () => { if (file) onFileSelected(file) }
  const ext = file ? getFileExt(file.name) : ''

  return (
    <div style={{ width: '100%' }}>
      {/* Drop Zone */}
      <div
        onDragOver={onDragOver}
        onDragLeave={onDragLeave}
        onDrop={onDrop}
        onClick={() => !loading && inputRef.current?.click()}
        style={{
          border: `2px dashed ${dragging ? 'var(--accent)' : file ? 'var(--success)' : 'var(--border)'}`,
          borderRadius: 12,
          padding: '28px 24px',
          textAlign: 'center',
          cursor: loading ? 'not-allowed' : 'pointer',
          background: dragging
            ? 'rgba(59,130,246,0.07)'
            : file ? 'rgba(16,185,129,0.05)' : 'var(--bg-input)',
          transition: 'all 0.2s ease',
        }}
      >
        <input
          ref={inputRef}
          type="file"
          accept=".csv,.xlsx,.xls"
          style={{ display: 'none' }}
          onChange={onInputChange}
          disabled={loading}
        />

        {/* Icon */}
        <div style={{
          width: 48, height: 48,
          borderRadius: '50%',
          background: file
            ? 'rgba(16,185,129,0.15)'
            : 'rgba(59,130,246,0.12)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          margin: '0 auto 12px',
          transition: 'transform 0.2s',
          transform: dragging ? 'translateY(-4px)' : 'none',
        }}>
          {file ? (
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="var(--success)" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="20 6 9 17 4 12"/>
            </svg>
          ) : (
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="var(--accent)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
              <polyline points="17 8 12 3 7 8"/>
              <line x1="12" y1="3" x2="12" y2="15"/>
            </svg>
          )}
        </div>

        {file && !valErr ? (
          <>
            <p style={{ fontSize: 14, fontWeight: 700, color: 'var(--success)', marginBottom: 4 }}>
              {file.name}
            </p>
            <p style={{ fontSize: 11, color: 'var(--text-muted)', marginBottom: 8 }}>
              {formatFileSize(file.size)} · {ext.toUpperCase()}
            </p>
            <button
              className="btn btn-outline btn-sm"
              onClick={(e) => { e.stopPropagation(); setFile(null) }}
              style={{ fontSize: 11 }}
            >
              Chọn file khác
            </button>
          </>
        ) : (
          <>
            <p style={{ fontSize: 14, fontWeight: 700, color: 'var(--text-primary)', marginBottom: 4 }}>
              {dragging ? 'Thả file vào đây!' : 'Kéo & thả file'}
            </p>
            <p style={{ fontSize: 12, color: 'var(--text-muted)', marginBottom: 10 }}>
              hoặc <span style={{ color: 'var(--accent)', fontWeight: 600 }}>click để chọn</span>
            </p>
            <div style={{ display: 'flex', gap: 6, justifyContent: 'center' }}>
              {ACCEPTED.map(a => (
                <span key={a} className="badge badge-blue" style={{ fontSize: 10 }}>{a.toUpperCase()}</span>
              ))}
              <span className="badge" style={{ fontSize: 10, background: 'rgba(255,255,255,0.06)', color: 'var(--text-muted)' }}>
                ≤ {MAX_MB}MB
              </span>
            </div>
          </>
        )}
      </div>

      {/* Error */}
      {(valErr || error) && (
        <div className="error-msg" style={{ marginTop: 8 }}>
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
          </svg>
          {valErr || error}
        </div>
      )}

      {/* Submit */}
      <button
        id="btn-analyze"
        className="btn btn-primary"
        style={{ width: '100%', marginTop: 12, justifyContent: 'center', fontSize: 13, padding: '10px 0' }}
        disabled={!file || !!valErr || loading}
        onClick={handleSubmit}
      >
        {loading ? (
          <><span className="spinner" style={{ width: 16, height: 16 }} /> Đang xử lý...</>
        ) : (
          <>
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/>
            </svg>
            Phân tích ngay →
          </>
        )}
      </button>
    </div>
  )
}
