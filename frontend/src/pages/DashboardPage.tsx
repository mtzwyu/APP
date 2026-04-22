import { useState, useCallback } from 'react'
import type { FilterState, KpiDto, TrendDto, QueryResult, DrillStep } from '../types'
import { getKpi, getTrend, getTopProducts, drillDown } from '../services/api'
import { useApi } from '../hooks/useApi'
import { useDrillDown } from '../hooks/useDrillDown'
import KpiCard from '../components/KpiCard'
import LineChartCard from '../components/LineChartCard'
import BarChartCard from '../components/BarChartCard'
import PieChartCard from '../components/PieChartCard'
import FilterPanel from '../components/FilterPanel'
import PivotTable from '../components/PivotTable'
import DrillDownModal from '../components/DrillDownModal'

const ICON_TREND = (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <polyline points="23 6 13.5 15.5 8.5 10.5 1 18" /><polyline points="17 6 23 6 23 12" />
  </svg>
)
const ICON_GLOBE = (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <circle cx="12" cy="12" r="10" /><line x1="2" y1="12" x2="22" y2="12" />
    <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" />
  </svg>
)

const ICON_ACT = (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
  </svg>
)

const DEFAULT_FILTER: FilterState = {
  yearDimension: '',
  year: 2023,
  measure: '',
  dimension: '',
  topN: 8,
}

export default function DashboardPage() {
  const [filter, setFilter]           = useState<FilterState>(DEFAULT_FILTER)
  const [appliedFilter, setApplied]   = useState<FilterState>(DEFAULT_FILTER)
  const [ddOpen, setDdOpen]           = useState(false)
  const [ddResult, setDdResult]       = useState<QueryResult | null>(null)
  const [ddLoading, setDdLoading]     = useState(false)
  const [ddError, setDdError]         = useState<string | null>(null)

  const drill = useDrillDown()

  // ── Data fetching ────────────────────────────────────────────────────────────
  const deps = [
    appliedFilter.measure, appliedFilter.yearDimension, appliedFilter.year,
    appliedFilter.quarterDimension, appliedFilter.quarterValue,
    appliedFilter.monthDimension, appliedFilter.monthValue,
    appliedFilter.dayDimension, appliedFilter.dayValue,
    appliedFilter.weekdayDimension, appliedFilter.weekdayValue,
    appliedFilter.dimension, appliedFilter.topN
  ]

  // Build extra time slicers from user-selected Dim Time attributes
  const timeSlicers: { dimension: string; member: string }[] = [
    appliedFilter.quarterDimension && appliedFilter.quarterValue
      ? { dimension: appliedFilter.quarterDimension, member: appliedFilter.quarterValue } : null,
    appliedFilter.monthDimension && appliedFilter.monthValue
      ? { dimension: appliedFilter.monthDimension, member: appliedFilter.monthValue } : null,
    appliedFilter.dayDimension && appliedFilter.dayValue
      ? { dimension: appliedFilter.dayDimension, member: appliedFilter.dayValue } : null,
    appliedFilter.weekdayDimension && appliedFilter.weekdayValue
      ? { dimension: appliedFilter.weekdayDimension, member: appliedFilter.weekdayValue } : null,
  ].filter(Boolean) as { dimension: string; member: string }[]

  const { data: kpiCost, loading: l1, error: e1 } = useApi<KpiDto>(
    () => {
      if (!appliedFilter.measure) return Promise.resolve(null as any)
      return getKpi(appliedFilter.measure, appliedFilter.yearDimension, appliedFilter.year, timeSlicers)
    },
    deps
  )
  const { data: kpiCount, loading: l2 } = useApi<KpiDto>(
    () => {
      if (!appliedFilter.yearDimension) return Promise.resolve(null as any)
      return getKpi('Fact Travel Count', appliedFilter.yearDimension, appliedFilter.year, timeSlicers)
    },
    deps
  )
  const { data: trend, loading: l3 } = useApi<TrendDto>(
    () => {
      if (!appliedFilter.measure) return Promise.resolve(null as any)
      return getTrend(appliedFilter.measure, 'Monthly', appliedFilter.yearDimension, appliedFilter.year, timeSlicers)
    },
    deps
  )
  const { data: topN, loading: l4 } = useApi<QueryResult>(
    () => {
      if (!appliedFilter.measure || !appliedFilter.dimension) return Promise.resolve({ rows: [], columns: [] } as any)
      return getTopProducts(appliedFilter.measure, appliedFilter.dimension, appliedFilter.topN,
        appliedFilter.yearDimension, appliedFilter.year, timeSlicers)
    },
    deps
  )

  const anyLoading = l1 || l2 || l3 || l4

  const apply = () => {
    setApplied({ ...filter })
    // refetch will happen automatically because deps changed
  }

  // ── Chart data transforms ────────────────────────────────────────────────────
  const trendPoints = trend?.dataPoints ?? []

  const barData = topN?.rows.map(r => ({
    name: r.axisValues[0] ?? '—',
    value: Math.round(r.values[0] ?? 0),
  })) ?? []

  // Pie: same top data
  const pieData = barData.slice(0, 6)

  // Pivot: convert topN result with time dimension
  const pivotResult = trend
    ? {
        headers: [appliedFilter.measure],
        rows: (trend.dataPoints ?? []).map(p => ({
          axisValues: [p.period.slice(0, 10)],
          values: [p.value],
          formattedValues: [p.formattedValue],
        })),
        executedMdx: '',
        executionTimeMs: 0,
        fromCache: false,
        totalRows: trend.dataPoints?.length ?? 0,
      }
    : null

  // ── Drill-Down ───────────────────────────────────────────────────────────────
  const handleDrillDown = useCallback(async (member: string) => {
    const step: DrillStep = {
      label: member,
      level: `Chi tiết ${appliedFilter.dimension}`,
      member,
      dimension: appliedFilter.dimension,
    }
    drill.drillTo(step)
    setDdOpen(true)
    setDdLoading(true)
    setDdError(null)
    setDdResult(null)
    try {
      const res = await drillDown({
        measures: [appliedFilter.measure],
        rowDimension: appliedFilter.dimension,
        filters: [{ dimensionName: appliedFilter.dimension, memberValues: [member] }],
        dateRange: { year: appliedFilter.year, yearColumn: appliedFilter.yearDimension },
        topN: appliedFilter.topN,
        drillDown: {
          dimensionName: appliedFilter.dimension,
          fromLevel: '', // Let backend resolve
          toLevel: '',   // Let backend resolve
          memberValue: member,
        },
      })
      setDdResult(res)
    } catch (err: any) {
      setDdError(err.message ?? 'Drill-down failed')
    } finally {
      setDdLoading(false)
    }
  }, [appliedFilter, drill])

  const handleDrillUp = useCallback(async () => {
    drill.drillUp()
    if (drill.stack.length <= 1) {
      setDdResult(null)
      return
    }
    const parent = drill.stack[drill.stack.length - 2]
    setDdLoading(true)
    setDdError(null)
    try {
      const res = await drillDown({
        measures: [appliedFilter.measure],
        rowDimension: appliedFilter.dimension,
        drillDown: {
          dimensionName: appliedFilter.dimension,
          fromLevel: '',
          toLevel: '',
          memberValue: parent.member,
          isDrillUp: true,
        },
      })
      setDdResult(res)
    } catch (err: any) {
      setDdError(err.message ?? 'Drill-up failed')
    } finally {
      setDdLoading(false)
    }
  }, [appliedFilter, drill])

  return (
    <div className="page-content">
      {/* Filter Panel */}
      <FilterPanel value={filter} onChange={setFilter} onApply={apply} loading={anyLoading} />

      {/* Error */}
      {(e1) && <div className="error-msg" style={{ marginBottom: 16 }}>{e1}</div>}

      {/* KPI Cards - 3 columns */}
      <div className="kpi-grid">
        <KpiCard
          label="Tổng Revenue"
          value={kpiCost?.formattedCurrentValue ?? '—'}
          rawValue={kpiCost?.currentValue}
          change={kpiCost?.growthRate ?? 0}
          color="#3b82f6"
          icon={ICON_TREND}
          sub={kpiCost?.period}
          loading={l1}
        />
        <KpiCard
          label="Số Khách Du Lịch"
          value={(kpiCount?.currentValue ?? 0).toLocaleString()}
          rawValue={kpiCount?.currentValue}
          change={kpiCount?.growthRate ?? 0}
          color="#06b6d4"
          icon={ICON_GLOBE}
          sub={kpiCount?.period}
          loading={l2}
        />
        <KpiCard
          label="YoY Growth"
          value={`${(kpiCost?.yearOverYear ?? 0).toFixed(1)}%`}
          change={kpiCost?.yearOverYear ?? 0}
          color="#8b5cf6"
          icon={ICON_ACT}
          sub="So với năm trước"
          loading={l1}
        />
      </div>

      {/* Charts Row 1: Line + Pie */}
      <div className="charts-row" style={{ marginBottom: 14 }}>
        <LineChartCard
          title={`Trend — ${appliedFilter.measure}`}
          badge={`${appliedFilter.yearDimension}: ${appliedFilter.year} · Monthly`}
          data={trendPoints}
          color="#3b82f6"
          loading={l3}
          onPointClick={pt => handleDrillDown(pt.period)}
        />
        <PieChartCard
          title="Phân bố Top Destinations"
          badge="By Revenue"
          data={pieData}
          loading={l4}
          onSliceClick={item => handleDrillDown(item.name)}
        />
      </div>

      {/* Charts Row 2: Bar */}
      <div style={{ marginBottom: 14 }}>
        <BarChartCard
          title={`Top ${appliedFilter.topN} — ${appliedFilter.dimension}`}
          badge={appliedFilter.measure}
          data={barData}
          loading={l4}
          onBarClick={item => handleDrillDown(item.name)}
        />
      </div>

      {/* Pivot Table */}
      <PivotTable
        data={pivotResult}
        loading={l3}
        onDrillDown={handleDrillDown}
      />

      {/* Drill-Down Modal */}
      <DrillDownModal
        isOpen={ddOpen}
        title={`Drill-Down: ${drill.current?.label ?? ''}`}
        stack={drill.stack}
        result={ddResult}
        loading={ddLoading}
        error={ddError}
        onClose={() => setDdOpen(false)}
        onDrillUp={handleDrillUp}
        onReset={() => { drill.reset(); setDdOpen(false); setDdResult(null) }}
      />
    </div>
  )
}
