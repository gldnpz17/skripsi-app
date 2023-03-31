import { Information } from "../../common/icons"

const HealthMetric = ({ label, value, additionalValue }) => (
  <div>
    <div className='flex gap-1 items-center'>
      <span className='text-sm font-bold text-gray-400'>{label}</span>
      <Information className='h-4 text-secondary-dark hover:text-secondary-light duration-300 cursor-pointer' />
    </div>
    <div className='flex items-center'>
      <span>{value}</span>
      {additionalValue}
    </div>
  </div>
)

export { HealthMetric }