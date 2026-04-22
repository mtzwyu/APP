import { useState, useCallback } from 'react'
import type { DrillStep } from '../types'

export interface DrillDownState {
  stack: DrillStep[]           // breadcrumb history
  currentLevel: string | null
  currentMember: string | null
}

/**
 * Manages drill-down navigation state.
 * Provides drillTo(), drillUp(), reset() and a breadcrumb trail.
 */
export function useDrillDown() {
  const [stack, setStack] = useState<DrillStep[]>([])

  const drillTo = useCallback((step: DrillStep) => {
    setStack(prev => [...prev, step])
  }, [])

  const goUp = useCallback(() => {
    setStack(prev => prev.slice(0, -1))
  }, [])

  const reset = useCallback(() => {
    setStack([])
  }, [])

  const current = stack.length > 0 ? stack[stack.length - 1] : null

  return {
    stack,
    current,
    isAtRoot: stack.length === 0,
    drillTo,
    drillUp: goUp,
    reset,
  }
}
