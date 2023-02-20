import { useMutation, useQueryClient } from "react-query"

const useSimpleMutation = (mutate, queryKeys = []) => {
  const queryClient = useQueryClient()

  const { 
    isLoading, 
    mutateAsync 
  } = useMutation(mutate, {
    onSuccess: () => {
      queryKeys.forEach(key => queryClient.refetchQueries(key))
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