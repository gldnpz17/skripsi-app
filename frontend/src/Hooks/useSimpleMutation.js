import { useMemo } from "react"
import { useMutation, useQueryClient } from "react-query"
const defaultOptions = {
  onSuccess: () => {}
}

const useSimpleMutation = (mutate, queryKeys = [], options) => {
  const queryClient = useQueryClient()

  const appliedOptions = useMemo(() => ({
    ...defaultOptions,
    ...options
  }), [options])

  const { 
    isLoading, 
    mutateAsync 
  } = useMutation(mutate, {
    onSuccess: () => {
      queryKeys.forEach(key => queryClient.refetchQueries(key))
      appliedOptions.onSuccess()
    }
  })

  return {
    isLoading,
    mutateAsync: (getArgs) => {
      return async function() {
        await mutateAsync(getArgs.apply(null, arguments))
      }
    }
  }
}

export { useSimpleMutation }