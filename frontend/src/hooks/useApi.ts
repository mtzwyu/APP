import { useState, useEffect, useCallback, useRef } from 'react'

interface ApiState<T> {
  data: T | null
  loading: boolean
  error: string | null
}

/**
 * Generic hook for data fetching.
 * Re-runs whenever `deps` change (similar to useEffect deps).
 * Returns { data, loading, error, refetch }
 */
export function useApi<T>(
  fetcher: () => Promise<T>,
  deps: React.DependencyList = []
): ApiState<T> & { refetch: () => void } {
  const [state, setState] = useState<ApiState<T>>({
    data: null,
    loading: true,
    error: null,
  })
  // Keep a stable reference to the fetcher to avoid re-subscription
  const fetcherRef = useRef(fetcher)
  fetcherRef.current = fetcher

  const run = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true, error: null }))
    try {
      const data = await fetcherRef.current()
      setState({ data, loading: false, error: null })
    } catch (err: any) {
      const msg =
        err?.response?.data?.error ?? err?.message ?? 'Unknown error'
      setState(prev => ({ ...prev, loading: false, error: msg }))
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps)

  useEffect(() => {
    run()
  }, [run])

  return { ...state, refetch: run }
}
