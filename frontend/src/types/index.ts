// ─────────────────────────────────────────────────────────────────────────────
// KPI
// ─────────────────────────────────────────────────────────────────────────────
export interface KpiDto {
  measureName: string
  currentValue: number
  previousValue: number
  growthRate: number
  yearOverYear: number
  monthOverMonth: number
  trendDirection: 'Up' | 'Down' | 'Flat' | string
  period: string
  formattedCurrentValue: string
  formattedGrowthRate: string
}

export interface KpiRequestDto {
  measure: string
  year?: number
  previousYear?: number
  month?: number
}

// ─────────────────────────────────────────────────────────────────────────────
// Trend
// ─────────────────────────────────────────────────────────────────────────────
export interface TrendPoint {
  period: string
  value: number
  formattedValue: string
  growthRate?: number
}

export interface TrendDto {
  measure: string
  granularity: string
  dataPoints: TrendPoint[]
}

// ─────────────────────────────────────────────────────────────────────────────
// Query / MDX
// ─────────────────────────────────────────────────────────────────────────────
export interface QueryRow {
  axisValues: string[]
  values: number[]
  formattedValues: string[]
}

export interface QueryResult {
  headers: string[]
  rows: QueryRow[]
  executedMdx: string
  executionTimeMs: number
  fromCache: boolean
  totalRows: number
}

export interface DrillPathDto {
  dimensionName: string
  fromLevel: string
  toLevel: string
  memberValue: string
  isDrillUp?: boolean
}

export interface QueryRequestDto {
  measures: string[]
  rowDimension: string
  topN?: number
  filters?: FilterItem[]
  dateRange?: {
    yearColumn?: string
    year?: number
    month?: number
    day?: number
    weekday?: number
  }
  drillDown?: DrillPathDto
}

export interface FilterItem {
  dimensionName: string
  memberValues: string[]
}

// ─────────────────────────────────────────────────────────────────────────────
// Metadata Helpers Let's add them here
// ─────────────────────────────────────────────────────────────────────────────

export interface LevelDto {
  name: string
  uniqueName: string
  levelNumber: number
}

export interface HierarchyDto {
  name: string
  uniqueName: string
  levels: LevelDto[]
}

export interface DimensionDto {
  name: string
  uniqueName: string
  caption: string
  hierarchies: HierarchyDto[]
}

export interface MeasureDto {
  name: string
  uniqueName: string
  caption: string
  aggregateFunction: string
  formatString: string
}

// ─────────────────────────────────────────────────────────────────────────────
// UI State
// ─────────────────────────────────────────────────────────────────────────────
export interface FilterState {
  yearDimension: string
  year: number
  quarterDimension?: string
  quarterValue?: string
  monthDimension?: string  // Level uniqueName, e.g. [Dim Time].[Month Start].[Month Start]
  monthValue?: string      // Member value, e.g. "1" or "January"
  dayDimension?: string
  dayValue?: string
  weekdayDimension?: string
  weekdayValue?: string
  measure: string
  dimension: string
  topN: number
}

export interface DrillStep {
  label: string
  level: string
  member: string
  dimension: string
}

// ─────────────────────────────────────────────────────────────────────────────
// Auth
// ─────────────────────────────────────────────────────────────────────────────
export interface AuthResponse {
  token: string
  username: string
  role: string
}

// ─────────────────────────────────────────────────────────────────────────────
// Connection
// ─────────────────────────────────────────────────────────────────────────────
export interface ConnectionStatus {
  connected: boolean
  source: string
  testedAt?: string
}

export interface AllConnectionStatus {
  ssas: ConnectionStatus
  sqlServer: ConnectionStatus
  testedAt: string
}

// ─────────────────────────────────────────────────────────────────────────────
// Pivot Table
// ─────────────────────────────────────────────────────────────────────────────
export interface PivotRow {
  label: string
  level: number        // 0 = year, 1 = quarter, 2 = month
  isExpanded?: boolean
  values: Record<string, number>
  children?: PivotRow[]
}

// ─────────────────────────────────────────────────────────────────────────────
// File Upload Flow
// ─────────────────────────────────────────────────────────────────────────────
export interface UploadResponse {
  fileId: string
  fileName: string
  rowCount: number
  columns: string[]
  preview: PreviewRow[]
}

export interface PreviewRow {
  [column: string]: string | number
}

export interface InsightDto {
  title: string
  description: string
  explanation: string
  metric?: string
  value?: number
}

// ─────────────────────────────────────────────────────────────────────────────
// Pivot Config (drag-and-drop)
// ─────────────────────────────────────────────────────────────────────────────
export interface PivotConfig {
  rows: string[]
  columns: string[]
  values: string[]
  aggregation: 'Sum' | 'Count'
}

export interface FilePivotResult {
  rowLabels: string[]
  colLabels: string[]
  matrix: number[][]
}
