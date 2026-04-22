import axios from 'axios'
import type {
  KpiDto, TrendDto, QueryResult, QueryRequestDto, AllConnectionStatus,
  DimensionDto, MeasureDto
} from '../types'

// ─────────────────────────────────────────────────────────────────────────────
// Axios instance
// ─────────────────────────────────────────────────────────────────────────────
const BASE_URL = 'http://localhost:5105'

export const apiClient = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 120_000,
})

// Attach JWT on every request from localStorage
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers['Authorization'] = `Bearer ${token}`
  return config
})


// ─────────────────────────────────────────────────────────────────────────────
// Response unwrapper — backend wraps everything in ApiResponse<T>
// ─────────────────────────────────────────────────────────────────────────────
function unwrap<T>(data: any): T {
  if (typeof data === 'object' && data !== null && 'success' in data) {
    if (!data.success) throw new Error(data.error ?? 'API error')
    return data.data as T
  }
  return data as T
}

// ─────────────────────────────────────────────────────────────────────────────
// Auth
// ─────────────────────────────────────────────────────────────────────────────
export async function login(username: string, password: string) {
  const res = await axios.post(`${BASE_URL}/api/auth/login`, { email: username, password })
  const d = res.data
  return {
    token:      d.Token      ?? d.token      ?? '',
    email:      d.Email      ?? d.email      ?? '',
    username:   d.Username   ?? d.username   ?? '',
    role:       d.Role       ?? d.role       ?? '',
    needsSetup: d.NeedsSetup ?? d.needsSetup ?? false,
  }
}

export async function register(body: any) {
  const res = await axios.post(`${BASE_URL}/api/auth/register`, body)
  const d = res.data
  return {
    token:      d.Token      ?? d.token      ?? '',
    email:      d.Email      ?? d.email      ?? '',
    username:   d.Username   ?? d.username   ?? '',
    role:       d.Role       ?? d.role       ?? '',
    needsSetup: d.NeedsSetup ?? d.needsSetup ?? true,
  }
}

export async function updateSettings(body: any) {
  const res = await apiClient.put(`/api/auth/settings`, body)
  return res.data
}

export async function getSettings(): Promise<{
  sqlConnectionString: string
  ssasConnectionString: string
  geminiApiKeyMasked: string
  hasGeminiKey: boolean
}> {
  const res = await apiClient.get('/api/auth/settings')
  const d = res.data
  return {
    sqlConnectionString:  d.SqlConnectionString  ?? d.sqlConnectionString  ?? '',
    ssasConnectionString: d.SsasConnectionString ?? d.ssasConnectionString ?? '',
    geminiApiKeyMasked:   d.GeminiApiKeyMasked   ?? d.geminiApiKeyMasked   ?? '',
    hasGeminiKey:         d.HasGeminiKey         ?? d.hasGeminiKey         ?? false,
  }
}


// ─────────────────────────────────────────────────────────────────────────────
// KPI
// ─────────────────────────────────────────────────────────────────────────────
export async function getKpi(
  measure: string,
  yearDimension?: string,
  year?: number,
  timeSlicers?: { dimension: string; member: string }[]
): Promise<KpiDto> {
  const params: Record<string, any> = { measure }
  if (yearDimension) params.yearColumn = yearDimension
  if (year) params.year = year
  // Encode extra slicers as JSON string so backend can parse
  if (timeSlicers && timeSlicers.length > 0) params.slicers = JSON.stringify(timeSlicers)
  const res = await apiClient.get('/api/kpi', { params })
  return unwrap<KpiDto>(res.data)
}

/** Alias: Year-over-Year KPI */
export async function getKpiYoY(measure: string, yearDimension: string, year: number): Promise<KpiDto> {
  return getKpi(measure, yearDimension, year)
}

/** Alias: Month-over-Month KPI (last month of year) */
export async function getKpiMoM(measure: string, yearDimension: string, year: number, monthDimension: string): Promise<KpiDto> {
  return getKpi(measure, yearDimension, year, [{ dimension: monthDimension, member: '12' }])
}

// ─────────────────────────────────────────────────────────────────────────────
// Trend
// ─────────────────────────────────────────────────────────────────────────────
export async function getTrend(
  measure: string,
  granularity: 'Monthly' | 'Quarterly' | 'Yearly' = 'Monthly',
  yearDimension?: string,
  year?: number,
  timeSlicers?: { dimension: string; member: string }[]
): Promise<TrendDto> {
  const params: Record<string, any> = { measure, granularity }
  if (yearDimension) params.yearColumn = yearDimension
  if (year) params.year = year
  if (timeSlicers && timeSlicers.length > 0) params.slicers = JSON.stringify(timeSlicers)
  const res = await apiClient.get('/api/trend', { params })
  return unwrap<TrendDto>(res.data)
}

// ─────────────────────────────────────────────────────────────────────────────
// Top N
// ─────────────────────────────────────────────────────────────────────────────
export async function getTopProducts(
  measure = 'Trip Cost',
  dimension = 'destination',
  n = 10,
  yearDimension?: string,
  year?: number,
  timeSlicers?: { dimension: string; member: string }[]
): Promise<QueryResult> {
  const params: Record<string, any> = { measure, dimension, n }
  if (yearDimension) params.yearColumn = yearDimension
  if (year) params.year = year
  if (timeSlicers && timeSlicers.length > 0) params.slicers = JSON.stringify(timeSlicers)
  const res = await apiClient.get('/api/trend/topn', { params })
  return unwrap<QueryResult>(res.data)
}

// ─────────────────────────────────────────────────────────────────────────────
// MDX Query
// ─────────────────────────────────────────────────────────────────────────────
export async function queryMdx(body: QueryRequestDto): Promise<QueryResult> {
  const res = await apiClient.post('/api/query', body)
  return unwrap<QueryResult>(res.data)
}

// ─────────────────────────────────────────────────────────────────────────────
// Drill-Down / Drill-Up
// ─────────────────────────────────────────────────────────────────────────────
export async function drillDown(body: QueryRequestDto): Promise<QueryResult> {
  const res = await apiClient.post('/api/drilldown/down', body)
  return unwrap<QueryResult>(res.data)
}

export async function drillUp(body: QueryRequestDto): Promise<QueryResult> {
  const res = await apiClient.post('/api/drilldown/up', body)
  return unwrap<QueryResult>(res.data)
}

export async function getChildren(
  measure: string,
  dimension: string,
  parentMember: string,
  level: string
): Promise<QueryResult> {
  const params = { measure, dimension, parentMember, level }
  const res = await apiClient.get('/api/drilldown/children', { params })
  return unwrap<QueryResult>(res.data)
}

// ─────────────────────────────────────────────────────────────────────────────
// Connection
// ─────────────────────────────────────────────────────────────────────────────
export async function getAllConnections(): Promise<AllConnectionStatus> {
  const res = await apiClient.get('/api/connection/all')
  return unwrap<AllConnectionStatus>(res.data)
}

// ─────────────────────────────────────────────────────────────────────────────
// Metadata
// ─────────────────────────────────────────────────────────────────────────────
export async function getDimensions(): Promise<DimensionDto[]> {
  const res = await apiClient.get('/api/dimensions')
  return unwrap<DimensionDto[]>(res.data)
}

export async function getMeasures(): Promise<MeasureDto[]> {
  const res = await apiClient.get('/api/measures')
  return unwrap<MeasureDto[]>(res.data)
}

// ─────────────────────────────────────────────────────────────────────────────
// File Upload Flow (mock khi backend chưa sẵn sàng)
// ─────────────────────────────────────────────────────────────────────────────
import type { UploadResponse, PreviewRow, InsightDto, PivotConfig, FilePivotResult } from '../types'



export async function uploadFile(file: File): Promise<UploadResponse> {
  const formData = new FormData()
  formData.append('file', file)
  
  const res = await apiClient.post('/api/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
  
  const d = res.data
  return {
    fileId: d.datasetId?.toString() ?? d.DatasetId?.toString() ?? '',
    fileName: d.fileName ?? d.FileName ?? '',
    rowCount: d.rowCount ?? d.RowCount ?? 0,
    columns: d.columns ?? d.Columns ?? [],
    preview: d.preview ?? d.Preview ?? [],
  }
}

export async function getFileData(_fileId: string): Promise<PreviewRow[]> {
  // Ideally, this hits /api/data to get saved rows, 
  // but currently we don't store raw rows in SQL, so we return empty or fetch from blob in future
  return [] 
}

export async function analyzeFile(_fileId: string): Promise<any> {
  const datasetId = parseInt(_fileId, 10)
  const res = await apiClient.post('/api/process', { datasetId })
  return res.data
}

export async function processData(_fileId: string, config: PivotConfig): Promise<FilePivotResult> {
  // Pivot structure parsing for simplistic mock fallback
  const destinations = ['Paris', 'Tokyo', 'New York', 'London', 'Sydney', 'Dubai', 'Bangkok'] // mock
  const years = ['2022', '2023', '2024']
  const rowLabels = config.rows.length > 0 ? destinations.slice(0, 6) : ['(no rows)']
  const colLabels = config.columns.length > 0 ? years : ['Total']
  const matrix = rowLabels.map(() => colLabels.map(() => Math.round(50 + Math.random() * 450)))

  return { rowLabels, colLabels, matrix }
}

export async function getInsight(_fileId: string): Promise<InsightDto[]> {
  const fileIdInt = parseInt(_fileId, 10)
  const res = await apiClient.get('/api/insight', { params: { fileId: fileIdInt } })
  return unwrap<InsightDto[]>(res.data)
}

