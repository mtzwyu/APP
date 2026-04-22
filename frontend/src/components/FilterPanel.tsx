import { useState, useEffect, useRef } from 'react'
import type { FilterState, MeasureDto, DimensionDto } from '../types'
import { getMeasures, getDimensions } from '../services/api'

const TOP_NS = [5, 8, 10, 15, 20]

interface Props {
  value: FilterState
  onChange: (f: FilterState) => void
  onApply: () => void
  loading?: boolean
}

export default function FilterPanel({ value, onChange, onApply, loading }: Props) {
  const [collapsed, setCollapsed] = useState(false)
  const [measures, setMeasures] = useState<MeasureDto[]>([])
  const [dimensions, setDimensions] = useState<DimensionDto[]>([])
  const [metaLoading, setMetaLoading] = useState(true)
  const autoApplied = useRef(false)

  useEffect(() => {
    async function loadMeta() {
      try {
        const [meas, dims] = await Promise.all([getMeasures(), getDimensions()])
        // Keep all measures (don't filter out Count - user may want them)
        setMeasures(meas)
        setDimensions(dims.filter(d => d.name !== 'Measures'))

        const patch: Partial<FilterState> = {}

        // Auto-detect Year dimension
        if (!value.yearDimension && dims.length > 0) {
          const dateDim = dims.find(d =>
            d.uniqueName.toLowerCase().includes('date') ||
            d.uniqueName.toLowerCase().includes('time')
          )
          if (dateDim) {
            const yearLvl = dateDim.hierarchies.flatMap(h => h.levels)
              .find(l => l.name.toLowerCase().includes('year'))
            patch.yearDimension = yearLvl?.uniqueName || dateDim.hierarchies[0]?.levels[0]?.uniqueName || ''
          }
        }

        // Auto-detect Measure
        if (!value.measure && meas.length > 0) {
          // Prefer a non-count, non-key measure
          const preferred = meas.find(m =>
            !m.name.toLowerCase().includes('count') &&
            !m.name.toLowerCase().includes('key')
          )
          patch.measure = preferred?.name || meas[0].name
        }

        // Auto-detect analysis Dimension → use uniqueName of first non-time level
        if (!value.dimension && dims.length > 0) {
          const nonTimeDim = dims.find(d =>
            d.name !== 'Measures' &&
            !d.name.toLowerCase().includes('time') &&
            !d.name.toLowerCase().includes('date')
          )
          if (nonTimeDim) {
            const firstLevel = nonTimeDim.hierarchies[0]?.levels[0]
            patch.dimension = firstLevel?.uniqueName || nonTimeDim.name
          } else {
            patch.dimension = dims[0].name
          }
        }

        if (Object.keys(patch).length > 0) {
          onChange({ ...value, ...patch })
        }
      } catch (err) {
        console.error('Failed to load dimensions/measures', err)
      } finally {
        setMetaLoading(false)
      }
    }
    loadMeta()
  }, [])

  // Auto-apply once after metadata loads and filter values are populated
  useEffect(() => {
    if (!metaLoading && !autoApplied.current && value.measure && value.yearDimension && value.dimension) {
      autoApplied.current = true
      // Small delay to ensure state is committed
      setTimeout(() => onApply(), 100)
    }
  }, [metaLoading, value.measure, value.yearDimension, value.dimension])

  const set = (patch: Partial<FilterState>) => onChange({ ...value, ...patch })

  // Build flat level list for dimensions dropdown
  const dimLevelOptions = dimensions.flatMap(d =>
    d.hierarchies.flatMap(h =>
      h.levels.map(l => ({
        key: l.uniqueName,
        label: `${d.caption} → ${l.name}`,
        group: d.caption,
      }))
    )
  )

  // Find display name for current dimension
  const currentDimLabel = dimLevelOptions.find(o => o.key === value.dimension)?.label || value.dimension

  return (
    <div className={`filter-panel ${collapsed ? 'collapsed' : ''}`}>
      <div className="filter-header" style={{ cursor: 'pointer' }} onClick={() => setCollapsed(!collapsed)}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <div style={{
            width: 32, height: 32, borderRadius: 10,
            background: 'linear-gradient(135deg, rgba(99,102,241,0.15), rgba(99,102,241,0.05))',
            color: 'var(--accent)', border: '1px solid rgba(99,102,241,0.2)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            boxShadow: '0 4px 12px rgba(99,102,241,0.1)'
          }}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3" />
            </svg>
          </div>
          <span style={{ fontWeight: 800, fontSize: 14, color: 'var(--text-primary)', letterSpacing: '-0.01em' }}>Bộ lọc phân tích</span>

          {/* Active tags */}
          <div style={{ display: 'flex', gap: 8, marginLeft: 12, flexWrap: 'wrap' }}>
            {value.measure && <span className="filter-tag">{value.measure}</span>}
            {value.dimension && <span className="filter-tag">{currentDimLabel}</span>}
            {value.year && <span className="filter-tag">Năm {value.year}</span>}
            <span className="filter-tag">Top {value.topN}</span>
          </div>
        </div>

        <div style={{ display: 'flex', gap: 10 }} onClick={e => e.stopPropagation()}>
          <button
            className="btn btn-primary btn-sm"
            onClick={onApply}
            disabled={loading}
            style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '8px 16px', borderRadius: '8px' }}
          >
            {loading
              ? <><span className="spinner" style={{ width: 14, height: 14, borderWidth: 2 }} />Đang xử lý...</>
              : <>
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="23 4 23 10 17 10" /><polyline points="1 20 1 14 7 14" />
                    <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15" />
                  </svg>
                  Cập nhật biểu đồ
                </>
            }
          </button>
          <button
            className="btn btn-outline btn-sm btn-icon"
            onClick={() => setCollapsed(!collapsed)}
            title={collapsed ? 'Mở rộng' : 'Thu gọn'}
          >
            {collapsed
              ? <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="6 9 12 15 18 9" /></svg>
              : <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="18 15 12 9 6 15" /></svg>
            }
          </button>
        </div>
      </div>

      {!collapsed && (
        <div className="filter-body">
          {/* ── Measure ── */}
          <div className="filter-field">
            <label>Chỉ tiêu (Measure)</label>
            <select className="input" value={value.measure} onChange={e => set({ measure: e.target.value })} disabled={metaLoading}>
              {metaLoading ? <option>Đang tải...</option> : measures.map(m => <option key={m.name} value={m.name}>{m.caption || m.name}</option>)}
            </select>
          </div>

          {/* ── Dimension with Level ── */}
          <div className="filter-field">
            <label>Chiều phân tích</label>
            <select className="input" value={value.dimension} onChange={e => set({ dimension: e.target.value })} disabled={metaLoading}>
              {metaLoading ? <option>Đang tải...</option> : (
                dimensions
                  .filter(d => !d.name.toLowerCase().includes('time') && !d.name.toLowerCase().includes('date'))
                  .map(d => (
                    <optgroup key={d.uniqueName} label={d.caption}>
                      {d.hierarchies.map(h =>
                        h.levels.map(l => (
                          <option key={l.uniqueName} value={l.uniqueName}>{l.name}</option>
                        ))
                      )}
                    </optgroup>
                  ))
              )}
            </select>
          </div>

          {/* ── Lọc theo thời gian ── */}
          <div className="filter-field">
            <label>Lọc theo thời gian <span style={{ fontWeight: 400, fontSize: 11, opacity: 0.6 }}>(để trống = lấy tất cả)</span></label>

            {/* Row 1: Chiều thời gian + Năm */}
            <div style={{ display: 'flex', gap: 6, marginBottom: 6 }}>
              <select
                className="input"
                style={{ flex: 1 }}
                value={value.yearDimension}
                onChange={e => set({ yearDimension: e.target.value })}
                disabled={metaLoading}
              >
                {metaLoading ? <option>Đang tải...</option> : (
                  dimensions
                    .filter(d => d.name.toLowerCase().includes('time') || d.name.toLowerCase().includes('date'))
                    .map(d => (
                      <optgroup key={d.uniqueName} label={d.caption}>
                        {d.hierarchies.map(h =>
                          h.levels.map(l => (
                            <option key={l.uniqueName} value={l.uniqueName}>{l.name}</option>
                          ))
                        )}
                      </optgroup>
                    ))
                )}
              </select>
              <input
                type="number"
                className="input"
                style={{ width: 74 }}
                value={value.year || ''}
                onChange={e => set({ year: +e.target.value })}
                placeholder="Năm"
              />
            </div>

            {/* Row 2: Tháng / Ngày / Thứ — dùng đúng thuộc tính từ Dim Time */}
            {(() => {
              const timeLevels = dimensions
                .filter(d => d.name.toLowerCase().includes('time') || d.name.toLowerCase().includes('date'))
                .flatMap(d => d.hierarchies.flatMap(h => h.levels))

              const monthLevels   = timeLevels.filter(l => l.name.toLowerCase().includes('month'))
              const quarterLevels = timeLevels.filter(l => l.name.toLowerCase().includes('quarter'))
              const dayLevels     = timeLevels.filter(l => l.name.toLowerCase().includes('day') && !l.name.toLowerCase().includes('week'))
              const weekLevels    = timeLevels.filter(l => l.name.toLowerCase().includes('week') || l.name.toLowerCase().includes('weekday'))

              const TimeRow = ({
                levels, dimKey, valKey, placeholder
              }: {
                levels: typeof timeLevels
                dimKey: 'quarterDimension' | 'monthDimension' | 'dayDimension' | 'weekdayDimension'
                valKey: 'quarterValue' | 'monthValue' | 'dayValue' | 'weekdayValue'
                placeholder: string
              }) => (
                <div style={{ display: 'flex', gap: 4 }}>
                  <select
                    className="input"
                    style={{ flex: 1 }}
                    value={(value as any)[dimKey] ?? ''}
                    onChange={e => set({ [dimKey]: e.target.value || undefined, [valKey]: undefined } as any)}
                  >
                    <option value="">-- {placeholder} --</option>
                    {levels.map(l => <option key={l.uniqueName} value={l.uniqueName}>{l.name}</option>)}
                  </select>
                  {(value as any)[dimKey] && (
                    <input
                      className="input"
                      style={{ width: 70 }}
                      value={(value as any)[valKey] ?? ''}
                      onChange={e => set({ [valKey]: e.target.value || undefined } as any)}
                      placeholder="Giá trị"
                    />
                  )}
                </div>
              )

              return (
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 6 }}>
                  <TimeRow levels={quarterLevels} dimKey="quarterDimension" valKey="quarterValue" placeholder="Quý" />
                  <TimeRow levels={monthLevels}   dimKey="monthDimension"   valKey="monthValue"   placeholder="Tháng" />
                  <TimeRow levels={dayLevels}     dimKey="dayDimension"     valKey="dayValue"     placeholder="Ngày" />
                  <TimeRow levels={weekLevels}    dimKey="weekdayDimension" valKey="weekdayValue" placeholder="Thứ" />
                </div>
              )
            })()}
          </div>

          {/* ── Top N ── */}
          <div className="filter-field">
            <label>Top N</label>
            <select className="input" value={value.topN} onChange={e => set({ topN: +e.target.value })}>
              {TOP_NS.map(n => <option key={n}>{n}</option>)}
            </select>
          </div>
        </div>
      )}
    </div>
  )
}
