import { useState, useEffect, useRef } from 'react'
import { updateSettings, getSettings, discoverSqlDatabases, discoverSsasCatalogs, discoverSqlTables, discoverSsasCubes } from '../services/api'

// ── Icons ──────────────────────────────────────────────────────────────────────

const IcoServer = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="2" y="2" width="20" height="8" rx="2"/><rect x="2" y="14" width="20" height="8" rx="2"/>
    <line x1="6" y1="6" x2="6.01" y2="6"/><line x1="6" y1="18" x2="6.01" y2="18"/>
  </svg>
)
const IcoCube = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/>
    <polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/>
  </svg>
)
const IcoGemini = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="m12 3-1.912 5.813a2 2 0 0 1-1.275 1.275L3 12l5.813 1.912a2 2 0 0 1 1.275 1.275L12 21l1.912-5.813a2 2 0 0 1 1.275-1.275L21 12l-5.813-1.912a2 2 0 0 1-1.275-1.275L12 3Z"/>
  </svg>
)
const IcoSave = () => (
  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/>
    <polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/>
  </svg>
)
const IcoEye = ({ show }: { show: boolean }) =>
  show ? (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94"/>
      <path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19"/>
      <line x1="1" y1="1" x2="23" y2="23"/>
    </svg>
  ) : (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>
    </svg>
  )

// ── Save button helper ─────────────────────────────────────────────────────────
function SaveBtn({ loading, disabled, onClick }: { loading: boolean; disabled?: boolean; onClick: () => void }) {
  return (
    <button
      className="btn btn-primary"
      onClick={onClick}
      disabled={loading || disabled}
      style={{ gap: 6, fontSize: 12, padding: '7px 14px', height: 32 }}
    >
      {loading ? <span className="spinner" style={{ width: 12, height: 12 }} /> : <IcoSave />}
      {loading ? 'Đang lưu...' : 'Lưu'}
    </button>
  )
}

// ── Status message helper ──────────────────────────────────────────────────────
function StatusMsg({ msg }: { msg: string }) {
  if (!msg) return null
  const ok = msg.startsWith('✓')
  return (
    <span style={{
      fontSize: 11, padding: '4px 10px', borderRadius: 6, fontWeight: 600,
      background: ok ? 'rgba(16,185,129,0.12)' : 'rgba(239,68,68,0.12)',
      color: ok ? 'var(--success)' : 'var(--error)',
    }}>{msg}</span>
  )
}

// ── Section header ─────────────────────────────────────────────────────────────
function SectionHeader({
  icon, label, accent, badge,
  loading, disabled, msg, onSave
}: {
  icon: React.ReactNode; label: string; accent: string; badge?: React.ReactNode
  loading: boolean; disabled?: boolean; msg: string; onSave: () => void
}) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 7 }}>
        <span style={{ color: accent }}>{icon}</span>
        <span style={{ fontSize: 13, fontWeight: 700, color: 'var(--text-primary)' }}>{label}</span>
        {badge}
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        <StatusMsg msg={msg} />
        <SaveBtn loading={loading} disabled={disabled} onClick={onSave} />
      </div>
    </div>
  )
}

// ── Main page ──────────────────────────────────────────────────────────────────
interface SettingsPageProps {
  onSetupDone?: () => void
  needsSetup?: boolean
}

export default function SettingsPage({ onSetupDone }: SettingsPageProps) {
  const [mode, setMode] = useState<'create' | 'existing'>('create')
  const [loadingInit, setLoadingInit] = useState(true)

  // SQL
  const [sqlServer, setSqlServer] = useState('')
  const [sqlDb,     setSqlDb]     = useState('')
  const [sqlUser,   setSqlUser]   = useState('')
  const [sqlPass,   setSqlPass]   = useState('')
  const [loadingSql, setLoadingSql] = useState(false)
  const [msgSql,     setMsgSql]     = useState('')

  // SSAS
  const [ssasServer,  setSsasServer]  = useState('')
  const [ssasCatalog, setSsasCatalog] = useState('')
  const [ssasUser,    setSsasUser]    = useState('')
  const [ssasPass,    setSsasPass]    = useState('')
  const [loadingSsas, setLoadingSsas] = useState(false)
  const [msgSsas,     setMsgSsas]     = useState('')

  // Gemini
  const [geminiApiKey,  setGeminiApiKey]  = useState('')
  const [showKey,       setShowKey]       = useState(false)
  const [geminiSavedHint, setGeminiSavedHint] = useState('')
  const [loadingGemini, setLoadingGemini] = useState(false)
  const [msgGemini,     setMsgGemini]     = useState('')

  // Track which sections saved in this session (for setup unlock)
  const [sqlSaved,  setSqlSaved]  = useState(false)
  const [ssasSaved, setSsasSaved] = useState(false)

  // Discover
  const [sqlDbs,       setSqlDbs]       = useState<string[]>([])
  const [sqlTables,    setSqlTables]    = useState<string[]>([])
  const [ssasCatalogs, setSsasCatalogs] = useState<string[]>([])
  const [ssasCubes,    setSsasCubes]    = useState<string[]>([])
  
  const [discoveringSql,  setDiscoveringSql]  = useState(false)
  const [discoveringSsas, setDiscoveringSsas] = useState(false)
  const [showSqlDbs,       setShowSqlDbs]       = useState(false)
  const [showSsasCatalogs, setShowSsasCatalogs] = useState(false)
  const sqlDropRef  = useRef<HTMLDivElement>(null)
  const ssasDropRef = useRef<HTMLDivElement>(null)
  const sqlDebounceRef  = useRef<ReturnType<typeof setTimeout> | null>(null)
  const ssasDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Close dropdown on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (sqlDropRef.current && !sqlDropRef.current.contains(e.target as Node)) setShowSqlDbs(false)
      if (ssasDropRef.current && !ssasDropRef.current.contains(e.target as Node)) setShowSsasCatalogs(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  // ── Parse connection string helpers ─────────────────────────────────────────
  const parseKV = (conn: string) => {
    const map: Record<string, string> = {}
    conn.split(';').forEach(part => {
      const eq = part.indexOf('=')
      if (eq > 0) map[part.slice(0, eq).trim().toLowerCase()] = part.slice(eq + 1).trim()
    })
    return map
  }

  // ── Load saved settings on mount ─────────────────────────────────────────────
  useEffect(() => {
    let cancelled = false
    getSettings()
      .then(s => {
        if (cancelled) return
        // Parse SQL
        if (s.sqlConnectionString) {
          const kv = parseKV(s.sqlConnectionString)
          setSqlServer(kv['server'] ?? kv['data source'] ?? '')
          setSqlDb(kv['database'] ?? kv['initial catalog'] ?? '')
          setSqlUser(kv['user id'] ?? kv['uid'] ?? '')
          if (kv['database'] || kv['initial catalog']) setMode('existing')
          setSqlSaved(true)
        }
        // Parse SSAS
        if (s.ssasConnectionString) {
          const kv = parseKV(s.ssasConnectionString)
          setSsasServer(kv['data source'] ?? kv['server'] ?? '')
          setSsasCatalog(kv['catalog'] ?? kv['initial catalog'] ?? '')
          setSsasUser(kv['user id'] ?? kv['uid'] ?? '')
          if (kv['catalog'] || kv['initial catalog']) setMode('existing')
          setSsasSaved(true)
        }
        // Gemini
        if (s.hasGeminiKey) setGeminiSavedHint(s.geminiApiKeyMasked)
      })
      .catch(() => { /* ignore — user might not have saved yet */ })
      .finally(() => { if (!cancelled) setLoadingInit(false) })
    return () => { cancelled = true }
  }, [])

  // ── Handlers ────────────────────────────────────────────────────────────────
  const handleSaveSql = async () => {
    try {
      setLoadingSql(true); setMsgSql('')
      const parts = [`Server=${sqlServer}`, 'TrustServerCertificate=True']
      if (mode === 'existing' && sqlDb) parts.push(`Database=${sqlDb}`)
      if (sqlUser) parts.push(`User Id=${sqlUser}`)
      if (sqlPass) parts.push(`Password=${sqlPass}`)
      await updateSettings({ sqlConnectionString: parts.join(';') + ';' })
      setMsgSql('✓ Đã lưu!')
      setSqlSaved(true)
      if (ssasSaved) onSetupDone?.()   // unlock chỉ khi cả 2 đã lưu
    } catch (err: any) {
      setMsgSql('✗ ' + (err?.response?.data?.message ?? err.message))
    } finally { setLoadingSql(false) }
  }

  const handleSaveSsas = async () => {
    try {
      setLoadingSsas(true); setMsgSsas('')
      const parts = [`Data Source=${ssasServer}`]
      if (mode === 'existing' && ssasCatalog) parts.push(`Catalog=${ssasCatalog}`)
      if (ssasUser) parts.push(`User Id=${ssasUser}`)
      if (ssasPass) parts.push(`Password=${ssasPass}`)
      await updateSettings({ ssasConnectionString: parts.join(';') + ';' })
      setMsgSsas('✓ Đã lưu!')
      setSsasSaved(true)
      if (sqlSaved) onSetupDone?.()   // unlock chỉ khi cả 2 đã lưu
    } catch (err: any) {
      setMsgSsas('✗ ' + (err?.response?.data?.message ?? err.message))
    } finally { setLoadingSsas(false) }
  }

  // ── Auto-discover helpers (debounced 800ms) ─────────────────────────────────
  const buildSqlConnStr = (server = sqlServer) => {
    const parts = [`Server=${server}`, 'TrustServerCertificate=True']
    if (sqlUser) parts.push(`User Id=${sqlUser}`)
    if (sqlPass) parts.push(`Password=${sqlPass}`)
    return parts.join(';') + ';'
  }

  const buildSsasConnStr = (server = ssasServer) => {
    const parts = [`Data Source=${server}`]
    if (ssasUser) parts.push(`User Id=${ssasUser}`)
    if (ssasPass) parts.push(`Password=${ssasPass}`)
    return parts.join(';') + ';'
  }

  // ── Auto-discovery logic (triggered on Server/User/Pass changes) ──────────
  useEffect(() => {
    if (!sqlServer.trim()) { setSqlDbs([]); return }
    if (sqlDebounceRef.current) clearTimeout(sqlDebounceRef.current)
    sqlDebounceRef.current = setTimeout(async () => {
      try {
        setDiscoveringSql(true)
        const dbs = await discoverSqlDatabases(buildSqlConnStr())
        setSqlDbs(dbs)
        if (dbs.length > 0) setShowSqlDbs(true)
      } catch { /* silent */ }
      finally { setDiscoveringSql(false) }
    }, 800)
    return () => { if (sqlDebounceRef.current) clearTimeout(sqlDebounceRef.current) }
  }, [sqlServer, sqlUser, sqlPass])

  useEffect(() => {
    if (!ssasServer.trim()) { setSsasCatalogs([]); return }
    if (ssasDebounceRef.current) clearTimeout(ssasDebounceRef.current)
    ssasDebounceRef.current = setTimeout(async () => {
      try {
        setDiscoveringSsas(true)
        const cats = await discoverSsasCatalogs(buildSsasConnStr())
        setSsasCatalogs(cats)
        if (cats.length > 0) setShowSsasCatalogs(true)
      } catch { /* silent */ }
      finally { setDiscoveringSsas(false) }
    }, 800)
    return () => { if (ssasDebounceRef.current) clearTimeout(ssasDebounceRef.current) }
  }, [ssasServer, ssasUser, ssasPass])

  // Discover Tables when DB changes
  useEffect(() => {
    if (!sqlDb || !sqlServer) { setSqlTables([]); return }
    discoverSqlTables(buildSqlConnStr() + `Initial Catalog=${sqlDb};`)
      .then(setSqlTables)
      .catch(() => setSqlTables([]))
  }, [sqlDb, sqlServer, sqlUser, sqlPass])

  // Discover Cubes when Catalog changes
  useEffect(() => {
    if (!ssasCatalog || !ssasServer) { setSsasCubes([]); return }
    discoverSsasCubes(buildSsasConnStr() + `Initial Catalog=${ssasCatalog};`)
      .then(setSsasCubes)
      .catch(() => setSsasCubes([]))
  }, [ssasCatalog, ssasServer, ssasUser, ssasPass])

  const onSqlServerChange = (val: string) => {
    setSqlServer(val)
    setSqlDbs([]); setShowSqlDbs(false); setSqlTables([])
  }

  const onSsasServerChange = (val: string) => {
    setSsasServer(val)
    setSsasCatalogs([]); setShowSsasCatalogs(false); setSsasCubes([])
  }

  const handleSaveGemini = async () => {
    try {
      setLoadingGemini(true); setMsgGemini('')
      await updateSettings({ geminiApiKey })
      setMsgGemini('✓ Đã lưu!')
      if (geminiApiKey) setGeminiSavedHint(geminiApiKey.slice(0, 8) + '••••••••••••')
      setGeminiApiKey('') // clear field after save
    } catch (err: any) {
      setMsgGemini('✗ ' + (err?.response?.data?.message ?? err.message))
    } finally { setLoadingGemini(false) }
  }

  const cols = mode === 'existing' ? 'repeat(4,1fr)' : 'repeat(3,1fr)'

  if (loadingInit) return (
    <div className="page-content" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 10, color: 'var(--text-muted)', fontSize: 13 }}>
      <span className="spinner" style={{ width: 16, height: 16 }} /> Đang tải cấu hình...
    </div>
  )

  return (
    <div className="page-content" style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>

      {/* Header */}
      <div>
        <h2 style={{ fontSize: 17, fontWeight: 800, margin: '0 0 3px 0' }}>Cấu hình Kết nối</h2>
        <p style={{ fontSize: 12, color: 'var(--text-muted)', margin: 0 }}>
          Mỗi mục có nút lưu riêng — thay đổi độc lập, không ảnh hưởng các mục còn lại.
        </p>
      </div>

      {/* Mode selector */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
        {(['create', 'existing'] as const).map(m => (
          <div
            key={m}
            onClick={() => setMode(m)}
            style={{
              padding: '11px 14px', borderRadius: 10, cursor: 'pointer',
              border: `1.5px solid ${mode === m ? 'var(--accent)' : 'var(--border)'}`,
              background: mode === m ? 'rgba(59,130,246,0.08)' : 'var(--bg-card)',
              transition: 'all 0.2s', display: 'flex', alignItems: 'flex-start', gap: 10,
            }}
          >
            <div style={{
              width: 26, height: 26, borderRadius: 7, flexShrink: 0, marginTop: 1,
              background: mode === m ? 'var(--accent)' : 'var(--bg-input)',
              color: mode === m ? '#fff' : 'var(--text-muted)',
              display: 'flex', alignItems: 'center', justifyContent: 'center'
            }}>
              {m === 'create'
                ? <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><path d="m12 3-1.912 5.813a2 2 0 0 1-1.275 1.275L3 12l5.813 1.912a2 2 0 0 1 1.275 1.275L12 21l1.912-5.813a2 2 0 0 1 1.275-1.275L21 12l-5.813-1.912a2 2 0 0 1-1.275-1.275L12 3Z"/></svg>
                : <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/><path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/></svg>}
            </div>
            <div>
              <div style={{ fontSize: 12, fontWeight: 700, color: mode === m ? 'var(--accent)' : 'var(--text-primary)', marginBottom: 2 }}>
                {m === 'create' ? 'Tự động Thiết kế' : 'Kết nối Có sẵn'}
              </div>
              <div style={{ fontSize: 11, color: 'var(--text-muted)', lineHeight: 1.4 }}>
                {m === 'create' ? 'AI tự tạo DW + Cube khi bạn nạp file' : 'Kết nối vào DW/Cube đã có sẵn'}
              </div>
            </div>
            {mode === m && (
              <div style={{ marginLeft: 'auto', color: 'var(--accent)', flexShrink: 0 }}>
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3"><polyline points="20 6 9 17 4 12"/></svg>
              </div>
            )}
          </div>
        ))}
      </div>

      {/* ── SQL Server ──────────────────────────────────────────────────────── */}
      <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border)', borderRadius: 12, padding: '14px 16px' }}>
        <SectionHeader
          icon={<IcoServer />} label="Máy chủ SQL Server" accent="var(--accent)"
          loading={loadingSql} disabled={!sqlServer} msg={msgSql} onSave={handleSaveSql}
        />
        <div style={{ display: 'grid', gridTemplateColumns: cols, gap: 10 }}>
          {/* Server — auto-discover on typing */}
          <div className="input-group" style={{ position: 'relative' }} ref={sqlDropRef}>
            <label style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              Tên Server
              {discoveringSql && <span className="spinner" style={{ width: 9, height: 9 }} />}
              {!discoveringSql && sqlDbs.length > 0 && (
                <span style={{ fontSize: 10, color: 'var(--accent)', fontWeight: 600 }}>
                  {sqlDbs.length} DB
                </span>
              )}
            </label>
            <input
              className="input"
              value={sqlServer}
              onChange={e => onSqlServerChange(e.target.value)}
              placeholder="localhost\SQLEXPRESS"
            />
            {showSqlDbs && sqlDbs.length > 0 && (
              <div style={{
                position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 50,
                background: 'var(--bg-card)', border: '1px solid var(--accent)',
                borderRadius: 8, boxShadow: '0 8px 24px rgba(0,0,0,0.3)',
                maxHeight: 180, overflowY: 'auto', marginTop: 4,
              }}>
                <div style={{ padding: '6px 10px 4px', fontSize: 10, fontWeight: 700, color: 'var(--accent)', borderBottom: '1px solid var(--border)', textTransform: 'uppercase', letterSpacing: 0.5 }}>
                  {sqlDbs.length} database tìm thấy
                </div>
                {sqlDbs.map(db => (
                  <div
                    key={db}
                    onClick={() => { setSqlDb(db); setMode('existing'); setShowSqlDbs(false) }}
                    style={{
                      padding: '8px 12px', fontSize: 12, cursor: 'pointer',
                      color: 'var(--text-primary)', transition: 'background 0.15s',
                      display: 'flex', alignItems: 'center', gap: 8,
                    }}
                    onMouseEnter={e => (e.currentTarget.style.background = 'rgba(59,130,246,0.1)')}
                    onMouseLeave={e => (e.currentTarget.style.background = '')}
                  >
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/><path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/></svg>
                    {db}
                  </div>
                ))}
              </div>
            )}
          </div>
          {mode === 'existing' && (
            <div className="input-group">
              <label style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                Database
                {sqlTables.length > 0 && (
                  <span style={{ fontSize: 10, color: 'var(--text-muted)', fontWeight: 600 }}>
                    ({sqlTables.length} bảng)
                  </span>
                )}
              </label>
              <input className="input" value={sqlDb} onChange={e => setSqlDb(e.target.value)} placeholder="DW_Travel" />
            </div>
          )}
          <div className="input-group">
            <label>Tài khoản</label>
            <input className="input" value={sqlUser} onChange={e => setSqlUser(e.target.value)} placeholder="sa (nếu có)" />
          </div>
          <div className="input-group">
            <label>Mật khẩu</label>
            <input type="password" className="input" value={sqlPass} onChange={e => setSqlPass(e.target.value)} placeholder="••••••••" />
          </div>
        </div>
      </div>

      {/* ── SSAS ────────────────────────────────────────────────────────────── */}
      <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border)', borderRadius: 12, padding: '14px 16px' }}>
        <SectionHeader
          icon={<IcoCube />} label="Trạm Phân tích (SSAS)" accent="var(--accent-3)"
          loading={loadingSsas} disabled={!ssasServer} msg={msgSsas} onSave={handleSaveSsas}
        />
        <div style={{ display: 'grid', gridTemplateColumns: cols, gap: 10 }}>
          {/* SSAS Server — auto-discover on typing */}
          <div className="input-group" style={{ position: 'relative' }} ref={ssasDropRef}>
            <label style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              Tên Server
              {discoveringSsas && <span className="spinner" style={{ width: 9, height: 9 }} />}
              {!discoveringSsas && ssasCatalogs.length > 0 && (
                <span style={{ fontSize: 10, color: 'var(--accent-3)', fontWeight: 600 }}>
                  {ssasCatalogs.length} Catalog
                </span>
              )}
            </label>
            <input
              className="input"
              value={ssasServer}
              onChange={e => onSsasServerChange(e.target.value)}
              placeholder="localhost\MSSQLSERVER"
            />
            {showSsasCatalogs && ssasCatalogs.length > 0 && (
              <div style={{
                position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 50,
                background: 'var(--bg-card)', border: '1px solid var(--accent-3)',
                borderRadius: 8, boxShadow: '0 8px 24px rgba(0,0,0,0.3)',
                maxHeight: 180, overflowY: 'auto', marginTop: 4,
              }}>
                <div style={{ padding: '6px 10px 4px', fontSize: 10, fontWeight: 700, color: 'var(--accent-3)', borderBottom: '1px solid var(--border)', textTransform: 'uppercase', letterSpacing: 0.5 }}>
                  {ssasCatalogs.length} catalog tìm thấy
                </div>
                {ssasCatalogs.map(cat => (
                  <div
                    key={cat}
                    onClick={() => { setSsasCatalog(cat); setMode('existing'); setShowSsasCatalogs(false) }}
                    style={{
                      padding: '8px 12px', fontSize: 12, cursor: 'pointer',
                      color: 'var(--text-primary)', transition: 'background 0.15s',
                      display: 'flex', alignItems: 'center', gap: 8,
                    }}
                    onMouseEnter={e => (e.currentTarget.style.background = 'rgba(16,185,129,0.1)')}
                    onMouseLeave={e => (e.currentTarget.style.background = '')}
                  >
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/></svg>
                    {cat}
                  </div>
                ))}
              </div>
            )}
          </div>
          {mode === 'existing' && (
            <div className="input-group">
              <label style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                Catalog
                {ssasCubes.length > 0 && (
                  <span style={{ fontSize: 10, color: 'var(--text-muted)', fontWeight: 600 }}>
                    ({ssasCubes.length} Cube)
                  </span>
                )}
              </label>
              <input className="input" value={ssasCatalog} onChange={e => setSsasCatalog(e.target.value)} placeholder="Travel_Cube" />
            </div>
          )}
          <div className="input-group">
            <label>Tài khoản</label>
            <input className="input" value={ssasUser} onChange={e => setSsasUser(e.target.value)} placeholder="Trống nếu Win Auth" />
          </div>
          <div className="input-group">
            <label>Mật khẩu</label>
            <input type="password" className="input" value={ssasPass} onChange={e => setSsasPass(e.target.value)} placeholder="••••••••" />
          </div>
        </div>
      </div>

      {/* ── Gemini API Key ──────────────────────────────────────────────────── */}
      <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border)', borderRadius: 12, padding: '14px 16px' }}>
        <SectionHeader
          icon={<IcoGemini />}
          label="Gemini API Key"
          accent="#8b5cf6"
          badge={
            <span style={{
              fontSize: 10, fontWeight: 600, padding: '2px 7px', borderRadius: 20,
              background: 'rgba(139,92,246,0.12)', color: '#8b5cf6'
            }}>AI</span>
          }
          loading={loadingGemini}
          disabled={!geminiApiKey}
          msg={msgGemini}
          onSave={handleSaveGemini}
        />
        <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: 8, alignItems: 'end' }}>
          <div className="input-group" style={{ margin: 0 }}>
            <label>API Key (Google AI Studio)</label>
            <input
              id="gemini-api-key-input"
              type={showKey ? 'text' : 'password'}
              className="input"
              value={geminiApiKey}
              onChange={e => setGeminiApiKey(e.target.value)}
              placeholder="AIzaSy..."
              style={{ letterSpacing: showKey ? 'normal' : 2 }}
            />
          </div>
          <button
            id="gemini-toggle-visibility"
            onClick={() => setShowKey(v => !v)}
            title={showKey ? 'Ẩn key' : 'Hiện key'}
            style={{
              height: 36, width: 36, borderRadius: 8, border: '1px solid var(--border)',
              background: 'var(--bg-input)', color: 'var(--text-muted)',
              cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center',
              flexShrink: 0, transition: 'all 0.2s',
            }}
          >
            <IcoEye show={showKey} />
          </button>
        </div>
        <p style={{ fontSize: 11, color: 'var(--text-muted)', margin: '8px 0 0 0', lineHeight: 1.5 }}>
          {geminiSavedHint && (
            <span style={{
              display: 'inline-flex', alignItems: 'center', gap: 5,
              background: 'rgba(139,92,246,0.1)', color: '#8b5cf6',
              borderRadius: 6, padding: '3px 9px', fontSize: 11, fontWeight: 600,
              marginBottom: 6, marginRight: 8,
            }}>
              ✓ Key đã lưu: {geminiSavedHint}
            </span>
          )}
          Lấy key tại{' '}
          <a href="https://aistudio.google.com/app/apikey" target="_blank" rel="noreferrer"
            style={{ color: '#8b5cf6', textDecoration: 'none' }}>
            aistudio.google.com
          </a>
          {' '}— dùng để AI tự động thiết kế DW & Cube từ file bạn tải lên.
        </p>
      </div>


    </div>
  )
}
