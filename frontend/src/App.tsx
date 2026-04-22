import { useState } from 'react'
import DashboardPage    from './pages/DashboardPage'
import KpiPage          from './pages/KpiPage'
import QueryPage        from './pages/QueryPage'
import ConnectionPage   from './pages/ConnectionPage'
import UploadPage       from './pages/UploadPage'
import DataPreviewPage  from './pages/DataPreviewPage'
import FileDashboardPage from './pages/FileDashboardPage'
import SettingsPage     from './pages/SettingsPage'
import { login, register }        from './services/api'
import type { UploadResponse } from './types'
import './index.css'

// ── Icons (inline SVG, no deps) ─────────────────────────────────────────────
const Ico = ({ d, d2, size = 16 }: { d: string; d2?: string; size?: number }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none"
       stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
       style={{ flexShrink: 0 }}>
    <path d={d} />
    {d2 && <path d={d2} />}
  </svg>
)
const IcoDash   = () => <Ico d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
const IcoTrend  = () => <Ico d="M23 6l-9.5 9.5-5-5L1 18" />
const IcoQuery  = () => <Ico d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" />
const IcoDB     = () => <Ico d="M12 2C6.5 2 2 4 2 7v10c0 3 4.5 5 10 5s10-2 10-5V7c0-3-4.5-5-10-5z" d2="M2 12c0 3 4.5 5 10 5s10-2 10-5M2 7c0 3 4.5 5 10 5s10-2 10-5" />
const IcoLogout = () => <Ico d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" d2="M16 17l5-5-5-5M21 12H9" />
const IcoMoon   = () => <Ico d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
const IcoSun    = () => <Ico d="M12 3v1m0 16v1m-9-9h1m16 0h1m-2.64-6.36l-.71.71M5.35 18.65l-.71.71M5.35 5.35l-.71-.71M18.65 18.65l-.71-.71M12 7a5 5 0 1 0 0 10A5 5 0 0 0 12 7z" />
const IcoBar    = () => <Ico d="M18 20V10M12 20V4M6 20v-6" />
const IcoAlert  = () => <Ico d="M12 2a10 10 0 1 0 0 20A10 10 0 0 0 12 2z" d2="M12 8v4M12 16h.01" size={14} />
const IcoUpload = () => <Ico d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" d2="M17 8l-5-5-5 5M12 3v12" />
const IcoGear   = () => <Ico d="M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z" d2="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" />
const IcoLock   = () => (
  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor"
       strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ flexShrink: 0 }}>
    <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
    <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
  </svg>
)

// ── Trang nào cần kết nối SQL/SSAS ──────────────────────────────────────────
const DATA_PAGES = new Set(['dashboard', 'kpi', 'query', 'connection', 'fileupload'])

// ── Nav pages ───────────────────────────────────────────────────────────────
const PAGES = [
  { id: 'dashboard',  label: 'Dashboard',    Icon: IcoDash },
  { id: 'kpi',        label: 'KPI Analysis', Icon: IcoTrend },
  { id: 'query',      label: 'Query',        Icon: IcoQuery },
  { id: 'connection', label: 'Connections',  Icon: IcoDB },
  { id: 'fileupload', label: 'Import Data',  Icon: IcoUpload },
  { id: 'settings',   label: 'Settings',     Icon: IcoGear },
]

// ── Setup Required Screen ────────────────────────────────────────────────────
function SetupRequiredPage({ onGoSettings }: { onGoSettings: () => void }) {
  return (
    <div style={{
      display: 'flex', flexDirection: 'column', alignItems: 'center',
      justifyContent: 'center', height: '100%', gap: 24, padding: 40,
    }}>
      <div style={{
        width: 72, height: 72, borderRadius: 20,
        background: 'linear-gradient(135deg, rgba(59,130,246,0.15), rgba(139,92,246,0.15))',
        border: '1.5px solid rgba(59,130,246,0.3)',
        display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--accent)',
      }}>
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
          <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
          <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
        </svg>
      </div>

      <div style={{ textAlign: 'center', maxWidth: 420 }}>
        <h2 style={{ fontSize: 20, fontWeight: 800, margin: '0 0 10px 0', color: 'var(--text-primary)' }}>
          Chưa cấu hình kết nối
        </h2>
        <p style={{ fontSize: 13, color: 'var(--text-muted)', lineHeight: 1.75, margin: 0 }}>
          Bạn cần nhập thông tin <strong style={{ color: 'var(--text-primary)' }}>SQL Server</strong> và{' '}
          <strong style={{ color: 'var(--text-primary)' }}>SSAS</strong> trong trang{' '}
          <strong style={{ color: 'var(--accent)' }}>Settings</strong> trước khi hệ thống có thể truy vấn dữ liệu.
        </p>
      </div>

      <div style={{
        background: 'var(--bg-card)', border: '1px solid var(--border)',
        borderRadius: 12, padding: '16px 20px', width: '100%', maxWidth: 380,
        display: 'flex', flexDirection: 'column', gap: 12,
      }}>
        {[
          'Vào trang Settings',
          'Nhập thông tin SQL Server → Lưu',
          'Nhập thông tin SSAS → Lưu',
          'Quay lại Dashboard để xem dữ liệu',
        ].map((text, i) => (
          <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <div style={{
              width: 24, height: 24, borderRadius: '50%', flexShrink: 0,
              background: 'var(--accent)', color: '#fff',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontSize: 11, fontWeight: 800,
            }}>{i + 1}</div>
            <span style={{ fontSize: 13, color: 'var(--text-primary)' }}>{text}</span>
          </div>
        ))}
      </div>

      <button
        className="btn btn-primary"
        onClick={onGoSettings}
        style={{ gap: 8, fontSize: 13, padding: '10px 24px' }}
      >
        <IcoGear />
        Đi tới Settings
      </button>
    </div>
  )
}

function IcoGoogle() { return <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor"><path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/><path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/><path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05"/><path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/></svg> }
function IcoMicrosoft() { return <svg viewBox="0 0 23 23" width="18" height="18"><path fill="#f3f3f3" d="M0 0h23v23H0z"/><path fill="#f35325" d="M1 1h10v10H1z"/><path fill="#81bc06" d="M12 1h10v10H12z"/><path fill="#05a6f0" d="M1 12h10v10H1z"/><path fill="#ffba08" d="M12 12h10v10H12z"/></svg> }

// ── Login Page ───────────────────────────────────────────────────────────────
function LoginPage({ onLogin }: { onLogin: (tok: string, user: string, role: string, needsSetup?: boolean) => void }) {
  const [isRegister, setIsRegister] = useState(false)
  const [user,    setUser]    = useState('')
  const [email,   setEmail]   = useState('')
  const [pass,    setPass]    = useState('')
  const [loading, setLoading] = useState(false)
  const [error,   setError]   = useState('')

  const submit = async (e: React.FormEvent) => {
    e.preventDefault(); setLoading(true); setError('')
    try {
      if (isRegister) {
        const data = await register({ email, username: user, password: pass })
        onLogin(data.token, data.username || data.email, data.role, data.needsSetup ?? true)
      } else {
        const data = await login(user, pass)
        onLogin(data.token, data.username || data.email, data.role, data.needsSetup ?? false)
      }
    } catch (err: any) {
      if (err?.response?.data?.errors) {
        const errors = Object.values(err.response.data.errors).flat()
        setError(errors.join(', ') as string)
      } else {
        setError(err?.response?.data?.message ?? err.message ?? 'Lỗi xác thực')
      }
    } finally { setLoading(false) }
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-logo">
          <div className="login-logo-icon"><IcoBar /></div>
          <div>
            <div style={{ fontWeight: 800, fontSize: 16, color: 'var(--text-primary)' }}>OLAP Analytics</div>
            <div style={{ fontSize: 11, color: 'var(--text-muted)' }}>Smart Data Platform</div>
          </div>
        </div>

        <div className={`login-slider ${isRegister ? 'is-register' : ''}`}>
          {/* ── Login Pane ── */}
          <div className="login-pane">
            <h1>Đăng nhập</h1>
            <p className="subtitle">Truy cập hệ thống phân tích dữ liệu</p>
            {error && !isRegister && (
              <div className="error-msg" style={{ marginBottom: 16, display: 'flex', alignItems: 'center', gap: 8 }}>
                <IcoAlert />{error}
              </div>
            )}
            <form className="login-form" onSubmit={submit}>
              <div className="input-group">
                <label>Tên đăng nhập / Email</label>
                <input 
                  id="login-user" 
                  className="input" 
                  placeholder="admin / example@gmail.com"
                  value={user} 
                  onChange={e => setUser(e.target.value)} 
                  required 
                />
              </div>
              <div className="input-group">
                <label>Mật khẩu</label>
                <input 
                  id="login-pass" 
                  className="input" 
                  type="password" 
                  placeholder="••••••••"
                  value={pass} 
                  onChange={e => setPass(e.target.value)} 
                  required 
                />
              </div>
              <div style={{ marginTop: 4, textAlign: 'right' }}>
                <a href="#" style={{ fontSize: 12, color: 'var(--text-muted)' }} onClick={e => e.preventDefault()}>Quên mật khẩu?</a>
              </div>
              <button id="login-submit" className="btn btn-primary" style={{ width: '100%', justifyContent: 'center', marginTop: 20 }} disabled={loading}>
                {loading && !isRegister ? <span className="spinner" /> : 'Đăng nhập →'}
              </button>

              <div className="login-divider"><span>Hoặc</span></div>
              <div className="social-login">
                <button type="button" className="social-btn"><IcoGoogle /> Google</button>
                <button type="button" className="social-btn"><IcoMicrosoft /> Microsoft</button>
              </div>
            </form>
          </div>

          {/* ── Register Pane ── */}
          <div className="login-pane">
            <h1>Đăng ký</h1>
            <p className="subtitle">Thiết lập kho dữ liệu riêng</p>
            {error && isRegister && (
              <div className="error-msg" style={{ marginBottom: 16, display: 'flex', alignItems: 'center', gap: 8 }}>
                <IcoAlert />{error}
              </div>
            )}
            <form className="login-form" onSubmit={submit}>
              <div className="input-group">
                <label>Địa chỉ Email</label>
                <input 
                  className="input" 
                  type="email"
                  placeholder="example@gmail.com"
                  value={email} 
                  onChange={e => setEmail(e.target.value)} 
                  required 
                />
              </div>
              <div className="input-group">
                <label>Tên đăng nhập</label>
                <input 
                  className="input" 
                  placeholder="Sử dụng để đăng nhập"
                  value={user} 
                  onChange={e => setUser(e.target.value)} 
                  required 
                />
              </div>
              <div className="input-group">
                <label>Mật khẩu</label>
                <input 
                  className="input" 
                  type="password" 
                  placeholder="••••••••"
                  value={pass} 
                  onChange={e => setPass(e.target.value)} 
                  required 
                />
              </div>
              <button className="btn btn-primary" style={{ width: '100%', justifyContent: 'center', marginTop: 16 }} disabled={loading}>
                {loading && isRegister ? <span className="spinner" /> : 'Đăng ký ngay →'}
              </button>

              <div className="login-divider"><span>Hoặc đăng ký bằng</span></div>
              <div className="social-login">
                <button type="button" className="social-btn"><IcoGoogle /></button>
                <button type="button" className="social-btn"><IcoMicrosoft /></button>
              </div>
            </form>
          </div>
        </div>

        <div className="login-footer">
          {isRegister ? 'Đã có tài khoản? ' : 'Chưa có tài khoản? '}
          <a href="#" style={{ fontWeight: 700, color: 'var(--accent)' }} onClick={(e) => { e.preventDefault(); setIsRegister(!isRegister); setError('') }}>
            {isRegister ? 'Đăng nhập' : 'Đăng ký ngay'}
          </a>
        </div>
      </div>
    </div>
  )
}

export default function App() {
  const [token,      setToken]      = useState(() => localStorage.getItem('token') ?? '')
  const [username,   setUsername]   = useState(() => localStorage.getItem('user') ?? '')
  const [role,       setRole]       = useState(() => localStorage.getItem('role') ?? '')
  const [needsSetup, setNeedsSetup] = useState(() => localStorage.getItem('needsSetup') === 'true')
  const [page,       setPage]       = useState(() =>
    localStorage.getItem('needsSetup') === 'true' ? 'settings' : 'dashboard'
  )
  const [darkMode, setDarkMode] = useState(true)

  const [uploadedFile, setUploadedFile] = useState<UploadResponse | null>(null)
  const [uploadView,   setUploadView]   = useState<'upload' | 'preview' | 'dashboard'>('upload')

  const handleLogin = (t: string, u: string, r: string, ns = false) => {
    setToken(t); setUsername(u); setRole(r); setNeedsSetup(ns)
    localStorage.setItem('token', t)
    localStorage.setItem('user', u)
    localStorage.setItem('role', r)
    localStorage.setItem('needsSetup', String(ns))
    setPage(ns ? 'settings' : 'dashboard')
  }

  const logout = () => { setToken(''); localStorage.clear() }

  // Gọi từ SettingsPage sau khi lưu SQL hoặc SSAS thành công
  const handleSetupDone = () => {
    setNeedsSetup(false)
    localStorage.setItem('needsSetup', 'false')
  }

  const handleNavChange = (id: string) => {
    if (needsSetup && DATA_PAGES.has(id)) return   // block locked pages
    setPage(id)
    if (id === 'fileupload') setUploadView('upload')
  }

  if (!token) return <LoginPage onLogin={handleLogin} />

  const roleClass = role === 'Admin' ? 'badge-red' : role === 'Analyst' ? 'badge-blue' : 'badge-green'

  const renderPage = () => {
    if (needsSetup && DATA_PAGES.has(page))
      return <SetupRequiredPage onGoSettings={() => setPage('settings')} />

    if (page === 'settings')
      return <SettingsPage onSetupDone={handleSetupDone} needsSetup={needsSetup} />
    if (page === 'dashboard')  return <DashboardPage />
    if (page === 'kpi')        return <KpiPage />
    if (page === 'query')      return <QueryPage />
    if (page === 'connection') return <ConnectionPage />

    if (page === 'fileupload') {
      if (uploadView === 'upload')
        return <UploadPage onDone={resp => { setUploadedFile(resp); setUploadView('preview') }} />
      if (uploadView === 'preview' && uploadedFile)
        return <DataPreviewPage upload={uploadedFile} onAnalyze={() => setUploadView('dashboard')} onBack={() => setUploadView('upload')} />
      if (uploadView === 'dashboard' && uploadedFile)
        return <FileDashboardPage upload={uploadedFile} onBack={() => setUploadView('preview')} />
    }
    return null
  }

  return (
    <div className="app-layout" data-theme={darkMode ? 'dark' : 'light'}>
      {/* ── Sidebar ── */}
      <aside className="sidebar">
        <div className="sidebar-logo">
          <div className="sidebar-logo-icon"><IcoBar /></div>
          <div className="sidebar-logo-text"><h2>OLAP Analytics</h2><p>Smart Data Platform</p></div>
        </div>

        <div className="nav-section">Navigation</div>
        {PAGES.map(({ id, label, Icon }) => {
          const locked = needsSetup && DATA_PAGES.has(id)
          return (
            <div
              key={id}
              id={`nav-${id}`}
              className={`nav-item ${page === id ? 'active' : ''}`}
              onClick={() => handleNavChange(id)}
              title={locked ? 'Cần cấu hình kết nối trước' : label}
              style={{
                opacity: locked ? 0.38 : 1,
                cursor: locked ? 'not-allowed' : 'pointer',
              }}
            >
              <Icon /><span className="nav-label">{label}</span>
              {locked && <span className="nav-lock" style={{ marginLeft: 'auto' }}><IcoLock /></span>}
            </div>
          )
        })}

        {/* Setup nudge banner */}
        {needsSetup && (
          <div className="setup-nudge" style={{
            margin: '12px 10px 0', padding: '10px 12px', borderRadius: 10,
            background: 'rgba(59,130,246,0.1)', border: '1px solid rgba(59,130,246,0.25)',
            fontSize: 11, color: 'var(--accent)', lineHeight: 1.6,
          }}>
            <div style={{ fontWeight: 700, marginBottom: 3 }}>⚙ Cần thiết lập</div>
            Nhập kết nối SQL & SSAS trong Settings để mở khoá.
          </div>
        )}

        <div className="sidebar-footer">
          <div className="user-badge">
            <div className="user-avatar">{(username || '?')[0]?.toUpperCase()}</div>
            <div className="user-info">
              <p>{username || 'User'}</p>
              <span className={`badge ${roleClass}`} style={{ fontSize: 10 }}>{role}</span>
            </div>
            <button className="btn btn-icon btn-outline" style={{ marginLeft: 'auto' }}
              onClick={logout} title="Logout" id="btn-logout">
              <IcoLogout />
            </button>
          </div>
        </div>
      </aside>

      {/* ── Main ── */}
      <div className="main-content">
        <div className="topbar">
          <span className="topbar-title">{PAGES.find(p => p.id === page)?.label}</span>
          <div className="topbar-actions">
            {needsSetup && (
              <span style={{
                fontSize: 11, padding: '4px 10px', borderRadius: 6, fontWeight: 600,
                background: 'rgba(245,158,11,0.12)', color: '#f59e0b',
                display: 'flex', alignItems: 'center', gap: 5,
              }}>
                <IcoLock /> Chưa kết nối
              </span>
            )}
            <span className={`status-dot ${needsSetup ? 'red' : 'green'}`} />
            <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>localhost:5105</span>
            <button className="btn btn-outline btn-sm" style={{ padding: '5px 10px', gap: 5 }}
              onClick={() => setDarkMode(p => !p)}>
              {darkMode ? <IcoSun /> : <IcoMoon />}
            </button>
          </div>
        </div>

        {renderPage()}
      </div>
    </div>
  )
}
