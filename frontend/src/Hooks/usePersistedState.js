import { useEffect, useState } from "react"

const tryParseStoredValue = (valueStr) => {
  let value = valueStr
  try {
    value = JSON.parse(valueStr)
    return value
  } catch {
    return value
  }
}

const usePersistedState = (key, defaultValue = null) => {
  const storedValue = window.localStorage.getItem(key)
  const [value, setValue] = useState(storedValue ? tryParseStoredValue(storedValue) : defaultValue)

  useEffect(() => {
    if (!storedValue) {
      window.localStorage.setItem(key, defaultValue)
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

export { usePersistedState }