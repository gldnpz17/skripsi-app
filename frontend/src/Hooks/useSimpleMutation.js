import { useMutation, useQueryClient } from "react-query"

const useSimpleMutation = (mutate, queryKeys = []) => {
  const queryClient = useQueryClient()

  const { 
    isLoading, 
    mutateAsync 
  } = useMutation(mutate, {
    onSuccess: () => {
      queryKeys.forEach(queryClient.refetchQueries)
    }
  })

  return {
    isLoading,
    mutateAsync: (getArgs) => async () => {
      await mutateAsync(getArgs())
    }
  }
}

export { useSimpleMutation }