import { useState } from 'react'
import type { InsightDto } from '../types'

interface Props {
  insights: InsightDto[]
  loading: boolean
}

const BADGE_COLORS = [
  { bg: 'rgba(99,102,241,0.15)', color: '#818cf8' },
  { bg: 'rgba(16,185,129,0.15)', color: '#34d399' },
  { bg: 'rgba(245,158,11,0.15)', color: '#fbbf24' },
  { bg: 'rgba(239,68,68,0.15)', color: '#f87171' },
  { bg: 'rgba(59,130,246,0.15)', color: '#60a5fa' },
]

function SkeletonCard() {
  return (
    <div style={{
      padding: '16px 20px',
      borderRadius: 12,
      background: 'var(--surface)',
      border: '1px solid var(--border)',
      animation: 'pulse 1.4s ease-in-out infinite',
    }}>
      <div style={{ height: 16, borderRadius: 6, background: 'var(--border)', width: '55%', marginBottom: 10 }} />
      <div style={{ height: 12, borderRadius: 6, background: 'var(--border)', width: '80%', marginBottom: 6 }} />
      <div style={{ height: 12, borderRadius: 6, background: 'var(--border)', width: '65%' }} />
    </div>
  )
}

function InsightCard({ insight, index, defaultOpen }: { insight: InsightDto; index: number; defaultOpen: boolean }) {
  const [open, setOpen] = useState(defaultOpen)
  const badge = BADGE_COLORS[index % BADGE_COLORS.length]

  return (
    <div style={{
      borderRadius: 12,
      border: '1px solid var(--border)',
      background: 'var(--surface)',
      overflow: 'hidden',
      transition: 'box-shadow 0.2s',
    }}>
      {/* Header */}
      <div
        onClick={() => setOpen(p => !p)}
        style={{
          padding: '14px 18px',
          display: 'flex',
          alignItems: 'center',
          gap: 12,
          cursor: 'pointer',
          userSelect: 'none',
        }}
      >
        {/* Index badge */}
        <div style={{
          width: 28, height: 28, borderRadius: 8,
          background: badge.bg,
          color: badge.color,
          fontSize: 11, fontWeight: 800,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          flexShrink: 0,
        }}>
          {index + 1}
        </div>

        <div style={{ flex: 1, minWidth: 0 }}>
          <p style={{ fontWeight: 700, fontSize: 14, color: 'var(--text-primary)' }}>
            {insight.title}
          </p>
          {!open && (
            <p style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 2,
                        whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
              {insight.description}
            </p>
          )}
        </div>

        {/* Value badge */}
        {insight.value !== undefined && (
          <span style={{
            fontSize: 12, fontWeight: 700,
            padding: '2px 10px', borderRadius: 20,
            background: badge.bg, color: badge.color,
            flexShrink: 0,
          }}>
            {insight.value}%
          </span>
        )}

        {/* Chevron */}
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
             stroke="var(--text-muted)" strokeWidth="2.5"
             style={{ flexShrink: 0, transform: open ? 'rotate(180deg)' : 'none', transition: 'transform 0.2s' }}>
          <polyline points="6 9 12 15 18 9" />
        </svg>
      </div>

      {/* Body */}
      {open && (
        <div style={{
          padding: '0 18px 16px',
          borderTop: '1px solid var(--border)',
          paddingTop: 12,
        }}>
          <p style={{ fontSize: 13, color: 'var(--text-secondary)', fontWeight: 500, marginBottom: 8 }}>
            {insight.description}
          </p>
          <p style={{ fontSize: 12, color: 'var(--text-muted)', lineHeight: 1.7 }}>
            {insight.explanation}
          </p>
          {insight.metric && (
            <div style={{
              marginTop: 10,
              display: 'inline-flex',
              alignItems: 'center',
              gap: 6,
              padding: '4px 10px',
              borderRadius: 8,
              background: badge.bg,
              fontSize: 11, color: badge.color, fontWeight: 600,
            }}>
              <svg width="11" height="11" viewBox="0 0 24 24" fill="none"
                   stroke="currentColor" strokeWidth="2.5">
                <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
              </svg>
              {insight.metric}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

export default function InsightPanel({ insights, loading }: Props) {
  const [collapsed, setCollapsed] = useState(false)

  return (
    <div style={{
      borderRadius: 16,
      border: '1px solid var(--border)',
      background: 'var(--card)',
      overflow: 'hidden',
      marginBottom: 24,
    }}>
      {/* Panel Header */}
      <div
        onClick={() => setCollapsed(p => !p)}
        style={{
          padding: '14px 20px',
          display: 'flex', alignItems: 'center', gap: 10,
          cursor: 'pointer',
          borderBottom: collapsed ? 'none' : '1px solid var(--border)',
          background: 'linear-gradient(135deg, rgba(99,102,241,0.06) 0%, rgba(139,92,246,0.06) 100%)',
        }}
      >
        <div style={{
          width: 32, height: 32, borderRadius: 8,
          background: 'linear-gradient(135deg, #6366f1, #8b5cf6)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          flexShrink: 0,
        }}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
               stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="10" />
            <path d="M12 16v-4M12 8h.01" />
          </svg>
        </div>
        <div style={{ flex: 1 }}>
          <p style={{ fontWeight: 700, fontSize: 14, color: 'var(--text-primary)' }}>
            AI Insights
          </p>
          <p style={{ fontSize: 11, color: 'var(--text-muted)' }}>
            {loading ? 'Đang phân tích...' : `${insights.length} phát hiện quan trọng`}
          </p>
        </div>
        {loading && <span className="spinner" style={{ width: 16, height: 16 }} />}
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none"
             stroke="var(--text-muted)" strokeWidth="2.5"
             style={{ transform: collapsed ? 'rotate(180deg)' : 'none', transition: 'transform 0.2s' }}>
          <polyline points="6 9 12 15 18 9" />
        </svg>
      </div>

      {/* Body */}
      {!collapsed && (
        <div style={{ padding: '16px 20px', display: 'flex', flexDirection: 'column', gap: 10 }}>
          {loading
            ? [1, 2, 3].map(i => <SkeletonCard key={i} />)
            : insights.map((ins, i) => (
                <InsightCard key={i} insight={ins} index={i} defaultOpen={i === 0} />
              ))
          }
          {!loading && insights.length === 0 && (
            <div style={{ textAlign: 'center', padding: '20px 0', color: 'var(--text-muted)', fontSize: 13 }}>
              Chưa có insight nào.
            </div>
          )}
        </div>
      )}

      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }
      `}</style>
    </div>
  )
}
