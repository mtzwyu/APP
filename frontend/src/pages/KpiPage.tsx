import { useState, useEffect } from 'react'
import type { KpiDto, TrendDto, MeasureDto, DimensionDto } from '../types'
import { getKpi, getTrend, getMeasures, getDimensions } from '../services/api'
import { useApi } from '../hooks/useApi'
import KpiCard from '../components/KpiCard'
import LineChartCard from '../components/LineChartCard'
import BarChartCard from '../components/BarChartCard'

const ICONS = {
  value: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M12 2v20M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
    </svg>
  ),
  yoy: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <polyline points="23 6 13.5 15.5 8.5 10.5 1 18" />
    </svg>
  ),
  mom: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
    </svg>
  ),
  trend: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M18 20V10M12 20V4M6 20v-6" />
    </svg>
  ),
}

export default function KpiPage() {
  const [measure, setMeasure] = useState('')
  const [yearDimension, setYearDimension] = useState('')
  const [year, setYear] = useState<number | string>(2023)

  // Fetch Metadata dynamically
  const { data: measData, loading: measLoading } = useApi<MeasureDto[]>(() => getMeasures(), [])
  const { data: dimsData, loading: dimsLoading } = useApi<DimensionDto[]>(() => getDimensions(), [])

  useEffect(() => {
    if (measData && measData.length > 0 && !measure) {
      const defaultMeasure = measData.find(m => m.uniqueName.includes('Cost')) ?? measData[0]
      setMeasure(defaultMeasure.name || defaultMeasure.uniqueName.replace(/\[|\]/g, ''))
    }
  }, [measData, measure])

  useEffect(() => {
    if (dimsData && dimsData.length > 0 && !yearDimension) {
      // Find a Date dimension automatically
      const dateDim = dimsData.find(d => d.uniqueName.toLowerCase().includes('date') || d.uniqueName.toLowerCase().includes('time'))
      if (dateDim) {
        // Find Year level
        const yearLvl = dateDim.hierarchies.flatMap(h => h.levels).find(l => l.name.toLowerCase().includes('year'))
        if (yearLvl) setYearDimension(yearLvl.uniqueName)
        else setYearDimension(dateDim.hierarchies[0]?.levels[0]?.uniqueName || '')
      } else {
        setYearDimension(dimsData[0]?.hierarchies[0]?.levels[0]?.uniqueName || '')
      }
    }
  }, [dimsData, yearDimension])

  const { data: kpi, loading: kl } = useApi<KpiDto>(
    () => {
      if (!measure) return Promise.reject('No measure selected')
      return getKpi(measure, yearDimension, +year)
    },
    [measure, yearDimension, year]
  )
  
  const { data: monthTrend, loading: tl } = useApi<TrendDto>(
    () => {
      if (!measure) return Promise.reject('No measure selected')
      return getTrend(measure, 'Monthly', yearDimension, +year)
    },
    [measure, yearDimension, year]
  )
  
  const { data: yearTrend, loading: yl } = useApi<TrendDto>(
    () => {
      if (!measure) return Promise.reject('No measure selected')
      return getTrend(measure, 'Yearly')
    },
    [measure]
  )

  const monthPoints = monthTrend?.dataPoints ?? []
  const yearPoints  = yearTrend?.dataPoints ?? []

  const barData = yearPoints.map(p => ({
    name: p.period.slice(0, 10),
    value: Math.round(p.value),
  }))

  const isLoading = kl || measLoading || dimsLoading

  return (
    <div className="page-content">
      {/* Controls bar */}
      <div style={{
        display: 'flex', alignItems: 'center', gap: 10, marginBottom: 14,
        background: 'var(--bg-card)', border: '1px solid var(--border)',
        borderRadius: 10, padding: '10px 14px'
      }}>
        <span style={{ fontSize: 13, fontWeight: 700, color: 'var(--text-secondary)', marginRight: 4 }}>KPI</span>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginLeft: 'auto' }}>
          <label style={{ fontSize: 11, color: 'var(--text-muted)', whiteSpace: 'nowrap' }}>Measure</label>
          <select
            className="input"
            value={measure}
            onChange={e => setMeasure(e.target.value)}
            style={{ padding: '5px 8px', minWidth: 140, fontSize: 12 }}
          >
            {measData?.map(m => (
              <option key={m.uniqueName} value={m.name || m.uniqueName.replace(/\[|\]/g, '')}>
                {m.caption}
              </option>
            ))}
          </select>
          <label style={{ fontSize: 11, color: 'var(--text-muted)', whiteSpace: 'nowrap' }}>Thời gian</label>
          <select
            className="input"
            value={yearDimension}
            onChange={e => setYearDimension(e.target.value)}
            style={{ padding: '5px 8px', minWidth: 130, fontSize: 12 }}
          >
            {dimsData?.map(d => (
              <optgroup key={d.uniqueName} label={d.caption}>
                {d.hierarchies.map(h =>
                  h.levels.map(l => (
                    <option key={l.uniqueName} value={l.uniqueName}>{d.caption} - {l.name}</option>
                  ))
                )}
              </optgroup>
            ))}
          </select>
          <input type="number" className="input" value={year} onChange={e => setYear(e.target.value)}
            style={{ padding: '5px 8px', width: 72, fontSize: 12 }} />
        </div>
      </div>

      {/* KPI Cards - 3 cols */}
      <div className="kpi-grid" style={{ marginBottom: 14 }}>
        <KpiCard
          label="Giá Trị Hiện Tại"
          value={kpi?.formattedCurrentValue ?? '—'}
          rawValue={kpi?.currentValue}
          change={kpi?.growthRate ?? 0}
          color="#3b82f6"
          icon={ICONS.value}
          sub={kpi?.period}
          loading={isLoading}
        />
        <KpiCard
          label="YoY Growth"
          value={kpi ? `${kpi.yearOverYear.toFixed(2)}%` : '—'}
          change={kpi?.yearOverYear ?? 0}
          color="#8b5cf6"
          icon={ICONS.yoy}
          sub="Year-over-Year"
          loading={isLoading}
        />
        <KpiCard
          label="MoM Growth"
          value={kpi ? `${kpi.monthOverMonth.toFixed(2)}%` : '—'}
          change={kpi?.monthOverMonth ?? 0}
          color="#10b981"
          icon={ICONS.mom}
          sub="Month-over-Month"
          loading={isLoading}
        />
      </div>

      {/* Charts */}
      <div className="charts-row">
        <LineChartCard
          title={`Monthly Trend — ${measure || 'Đang tải...'}`}
          badge={String(year)}
          data={monthPoints}
          color="#3b82f6"
          loading={tl || isLoading}
        />
        <BarChartCard
          title="Yearly Comparison"
          badge={measure || 'Đang tải...'}
          data={barData}
          loading={yl || isLoading}
        />
      </div>
    </div>
  )
}
