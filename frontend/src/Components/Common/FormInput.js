const FormInput = ({ label, className, ...props }) => (
  <div>
    <span className='mr-4'>{label}</span>
    <input
      className={`rounded-md border-2 text-black px-2 py-1 border-secondary-dark bg-purple-100 w-48 ${className}`}
      {...props}
    />
  </div>
)

export { FormInput }