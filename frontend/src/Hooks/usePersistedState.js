import { useState } from "react"

const usePersistedValue = (key) => {
  const [value, setValue] = useState(JSON.parse(window.localStorage.getItem(key)))

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