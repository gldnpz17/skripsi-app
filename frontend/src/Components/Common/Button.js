const Button = ({ children, className, disabled, onClick }) => (
  <button 
    onClick={disabled ? () => {} : onClick}
    className={`py-2 px-4 bg-secondary-dark rounded-md text-black duration-150 flex items-center justify-center gap-1 select-none ${!disabled && 'hover:bg-secondary-light '} ${disabled && 'brightness-50'} ${className}`}
  >
    {children}
  </button>
)

export { Button }