const DashboardLabel = ({ icon, label, className, external }) => {
  const Icon = icon
  return (
    <div className={`text-gray-400 flex items-center ${external ? 'font-bold mb-4' : 'mb-2'} ${className}`}>
      {external && (
        <div className='h-5 w-1 bg-secondary-dark mr-2'>&nbsp;</div>
      )}
      {Icon && (
        <>
          <Icon className='h-4 inline' />&nbsp;
        </>
      )}
      {label}
    </div>
  )
}

export { DashboardLabel }