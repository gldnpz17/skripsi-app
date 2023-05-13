const FormInput = ({ label, className, stretch, labelClassName, ...props }) => (
  <div className={`flex items-center ${stretch ? 'w-full' : ''}`}>
    <span className={`mr-4 ${labelClassName}`}>{label}</span>
    {stretch && (<span className='flex-grow' />)}
    <input
      className={`rounded-md border-2 text-black px-2 py-1 border-secondary-dark bg-purple-100 w-48 ${className}`}
      {...props}
    />
  </div>
)

const FormInputSimple = ({ label, className, ...props }) => (
  <input
    placeholder={label}
    className={`rounded-md border-2 text-black px-2 py-1 border-secondary-dark bg-purple-100 w-48 ${className}`}
    {...props}
  />
)

export { FormInput, FormInputSimple }