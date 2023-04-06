import { useEffect, useState } from "react"

const usePersistedValue = (key, defaltValue = null) => {
  const storedValue = window.localStorage.getItem(key)

  const [value, setValue] = useState(storedValue ? JSON.parse(storedValue) : defaltValue)

  useEffect(() => {
    if (!storedValue) {
      window.localStorage.setItem(key, defaltValue)
    } 
  }, [])

  const setAndPersistValue = (newValue) => {
    if (newValue == null) {
      window.localStorage.removeItem(key)
      setValue(null)
      return
    }

    window.localStorage.setItem(key, JSON.stringify(newValue))
    setValue(newValue)
  }

  return [
    value,
    setAndPersistValue
  ]
}

export { usePersistedValue }