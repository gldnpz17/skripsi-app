const IconButton = ({ children, onClick }) => (
  <div className='relative rounded-full'>
    <div className='hover:text-secondary-light hover:rotate-12 duration-150 cursor-pointer' {...{ onClick }}>
      {children}
    </div>
  </div>
)

export { IconButton }