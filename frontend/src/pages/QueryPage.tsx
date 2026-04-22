import { useState, useEffect } from 'react'
import type { QueryResult, DimensionDto, MeasureDto } from '../types'
import { queryMdx, getDimensions, getMeasures } from '../services/api'
import { useApi } from '../hooks/useApi'

export default function QueryPage() {
  const [measuresStr, setMeasuresStr] = useState('')
  const [dimension, setDimension] = useState('')
  const [yearDimension, setYearDimension] = useState('')
  const [year, setYear] = useState('')
  const [topN, setTopN] = useState('10')
  const [result, setResult] = useState<QueryResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  // Metadata states
  const { data: dimsData, loading: dimsLoading } = useApi<DimensionDto[]>(() => getDimensions(), [])
  const { data: measData, loading: measLoading } = useApi<MeasureDto[]>(() => getMeasures(), [])

  // Measures auto-select
  useEffect(() => {
    if (measData && measData.length > 0 && !measuresStr) {
      setMeasuresStr(measData[0]?.uniqueName)
    }
  }, [measData, measuresStr])

  const run = async () => {
    setLoading(true); setError(''); setResult(null)
    try {
      const measureList = measuresStr.split(',').map(m => m.trim()).filter(Boolean)
      const body: any = {
        measures: measureList,
        rowDimension: dimension,
        topN: topN ? parseInt(topN) : 0,
        filters: [],
      }
      if (yearDimension || year) {
        body.dateRange = {}
        if (yearDimension) body.dateRange.yearColumn = yearDimension
        if (year) body.dateRange.year = parseInt(year)
      }
      const res = await queryMdx(body)
      setResult(res)
    } catch (e: any) {
      setError(e?.response?.data?.error ?? e.message ?? 'Query failed')
    } finally {
      setLoading(false)
    }
  }

  const exportCSV = () => {
    if (!result) return
    const cols = ['Dimension', ...result.headers]
    const lines = [cols.join(',')]
    for (const row of result.rows) {
      lines.push([row.axisValues.join('/'), ...row.values.map(v => v?.toFixed(2) || '')].join(','))
    }
    const blob = new Blob([lines.join('\n')], { type: 'text/csv' })
    const a = document.createElement('a')
    a.href = URL.createObjectURL(blob)
    a.download = `query_${Date.now()}.csv`
    a.click()
  }

  return (
    <div className="page-content">
      <div style={{ fontSize: 20, fontWeight: 800, marginBottom: 20 }}>MDX Query Builder</div>

      <div className="chart-card fade-up" style={{ marginBottom: 16 }}>
        <div className="chart-header"><h3>Cấu hình Query</h3></div>
        
        {dimsLoading || measLoading ? (
          <div style={{ color: 'var(--text-muted)' }}>Đang tải metadata từ SSAS...</div>
        ) : (
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr', gap: 14, marginBottom: 16 }}>
            <div className="input-group" style={{ gridColumn: 'span 2' }}>
              <label>Measures (Độ đo phân tích)</label>
              
              <select 
                className="input" 
                style={{ marginBottom: '8px' }}
                value="" 
                onChange={e => {
                  const val = e.target.value;
                  if (!val) return;
                  let current = measuresStr.split(',').map(s => s.trim()).filter(Boolean);
                  if (!current.includes(val)) {
                    current.push(val);
                    setMeasuresStr(current.join(', '));
                  }
                }}
              >
                <option value="">-- Chọn thêm Measure cần đo --</option>
                {measData?.map(m => (
                  <option key={m.uniqueName} value={m.uniqueName}>{m.caption}</option>
                ))}
              </select>

              {/* Render selected measure chips beautifully */}
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', minHeight: '34px' }}>
                {measuresStr.split(',').map(s => s.trim()).filter(Boolean).map(selectedUniqueName => {
                  // Find caption
                  let label = selectedUniqueName;
                  const found = measData?.find(m => m.uniqueName === selectedUniqueName);
                  if (found) label = found.caption;

                  return (
                    <div 
                      key={selectedUniqueName}
                      onClick={() => {
                        let current = measuresStr.split(',').map(s => s.trim()).filter(Boolean);
                        setMeasuresStr(current.filter(x => x !== selectedUniqueName).join(', '));
                      }}
                      style={{
                        padding: '4px 10px',
                        borderRadius: '20px',
                        fontSize: '13px',
                        fontWeight: 500,
                        cursor: 'pointer',
                        background: 'var(--primary)',
                        color: '#fff',
                        border: '1px solid var(--primary)',
                        display: 'flex',
                        alignItems: 'center',
                        gap: '6px'
                      }}
                    >
                      {label}
                      <span style={{ fontSize: '10px', opacity: 0.7 }}>✕</span>
                    </div>
                  );
                })}
              </div>
            </div>
            <div className="input-group" style={{ gridColumn: 'span 2' }}>
              <label>Row Dimensions (CrossJoin đa chiều)</label>
              
              <select 
                className="input" 
                style={{ marginBottom: '8px' }}
                value="" 
                onChange={e => {
                  const val = e.target.value;
                  if (!val) return;
                  let current = dimension.split(',').map(s => s.trim()).filter(Boolean);
                  if (!current.includes(val)) {
                    current.push(val);
                    setDimension(current.join(', '));
                  }
                }}
              >
                <option value="">-- Chọn thêm Dimension để phân tích --</option>
                {dimsData?.map(d => (
                  <optgroup key={d.uniqueName} label={d.caption}>
                    {d.hierarchies.map(h => 
                      h.levels.map(l => (
                        <option key={l.uniqueName} value={l.uniqueName}>{l.name}</option>
                      ))
                    )}
                  </optgroup>
                ))}
              </select>

              {/* Render selected chips beautifully */}
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', minHeight: '34px' }}>
                {dimension.split(',').map(s => s.trim()).filter(Boolean).map(selectedUniqueName => {
                  // Find caption for the selected item
                  let label = selectedUniqueName;
                  dimsData?.forEach(d => d.hierarchies.forEach(h => h.levels.forEach(l => {
                    if (l.uniqueName === selectedUniqueName) label = `${d.caption} - ${l.name}`;
                  })));

                  return (
                    <div 
                      key={selectedUniqueName}
                      onClick={() => {
                        let current = dimension.split(',').map(s => s.trim()).filter(Boolean);
                        setDimension(current.filter(x => x !== selectedUniqueName).join(', '));
                      }}
                      style={{
                        padding: '4px 10px',
                        borderRadius: '20px',
                        fontSize: '13px',
                        fontWeight: 500,
                        cursor: 'pointer',
                        background: 'var(--secondary)',
                        color: '#fff',
                        border: '1px solid var(--secondary)',
                        display: 'flex',
                        alignItems: 'center',
                        gap: '6px'
                      }}
                    >
                      {label}
                      <span style={{ fontSize: '10px', opacity: 0.7 }}>✕</span>
                    </div>
                  );
                })}
              </div>
            </div>
            <div className="input-group">
              <label>Năm (tuỳ chọn)</label>
              <div style={{ display: 'flex', gap: 4 }}>
                <input
                  className="input"
                  style={{ width: '50%' }}
                  value={yearDimension}
                  onChange={e => setYearDimension(e.target.value)}
                  placeholder="Year Col"
                />
                <input
                  className="input"
                  style={{ width: '50%' }}
                  value={year}
                  onChange={e => setYear(e.target.value)}
                  placeholder="2023"
                  type="number"
                />
              </div>
            </div>
            <div className="input-group">
              <label>Top N (0 = tất cả)</label>
              <input
                className="input"
                value={topN}
                onChange={e => setTopN(e.target.value)}
                placeholder="10"
                type="number"
                min={0}
              />
            </div>
          </div>
        )}
        
        <div style={{ display: 'flex', gap: 10 }}>
          <button
            className="btn btn-primary"
            onClick={run}
            disabled={loading || dimsLoading}
            style={{ display: 'flex', alignItems: 'center', gap: 8 }}
          >
            {loading
              ? <><span className="spinner" style={{ width: 14, height: 14, borderWidth: 2 }} />Executing...</>
              : <>
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                    <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2" />
                  </svg>
                  Execute Query
                </>
            }
          </button>
          {result && (
            <button
              className="btn btn-outline"
              onClick={exportCSV}
              style={{ display: 'flex', alignItems: 'center', gap: 6 }}
            >
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                <polyline points="7 10 12 15 17 10" /><line x1="12" y1="15" x2="12" y2="3" />
              </svg>
              Export CSV
            </button>
          )}
        </div>
      </div>

      {error && <div className="error-msg" style={{ marginBottom: 12 }}>{error}</div>}

      {result && (
        <div className="chart-card fade-up">
          <div className="chart-header">
            <h3>Kết quả — {result.totalRows} dòng</h3>
            <div style={{ display: 'flex', gap: 8 }}>
              {result.fromCache && <span className="badge badge-yellow">Cached</span>}
              <span className="badge badge-blue">{result.executionTimeMs}ms</span>
            </div>
          </div>

          {result.executedMdx && (
            <details style={{ marginBottom: 14 }}>
              <summary style={{ fontSize: 11, color: 'var(--text-muted)', cursor: 'pointer', marginBottom: 6 }}>
                MDX được thực thi
              </summary>
              <div className="mdx-box">{result.executedMdx}</div>
            </details>
          )}

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Dimension</th>
                  {result.headers.map((h, idx) => <th key={idx}>{h}</th>)}
                </tr>
              </thead>
              <tbody>
                {result.rows.slice(0, 100).map((row, i) => (
                  <tr key={i}>
                    <td style={{ color: 'var(--text-muted)' }}>{i + 1}</td>
                    <td style={{ fontWeight: 500, color: 'var(--text-primary)' }}>
                      {row.axisValues.join(' / ')}
                    </td>
                    {row.formattedValues.map((v, j) => (
                      <td key={j}>{v || row.values[j]?.toLocaleString()}</td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
            {result.rows.length > 100 && (
              <p style={{ color: 'var(--text-muted)', fontSize: 12, padding: '10px 14px' }}>
                Showing 100/{result.totalRows} rows
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
