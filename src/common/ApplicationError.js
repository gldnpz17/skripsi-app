class ApplicationError extends Error {
  constructor(message) {
    super(message)
  }
}

export { ApplicationError }