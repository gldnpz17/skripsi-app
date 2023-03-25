import { useMemo } from "react"
import { useLocation } from "react-router-dom"

const useQueryParams = () => {
  const { search } = useLocation()

  return useMemo(() => {
    const keyValues = {}
    new URLSearchParams(search).forEach((value, key) => {
      keyValues[key] = value
    })

    return keyValues
  }, [search])
}

export { useQueryParams }