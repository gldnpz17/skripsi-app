import { Format } from "../../common/Format";

const ReportSprint = ({ 
  timespanSprint: {
    effort: accountedEffort,
    endDate: accountedEndDate,
    startDate: accountedStartDate,
    sprint: {
      startDate,
      endDate
    }
  }
}) => (
  <div className='bg-dark-2 px-4 py-3 rounded-md border border-gray-700 flex items-center'>
    <span className='flex-grow font-semibold'>
      {!startDate.startOf('day').equals(accountedStartDate.startOf('day')) && (
        <span className='line-through text-gray-400'>{Format.briefDate(startDate)}</span>
      )}
      <span>&nbsp;</span>
      <span>{Format.briefDate(accountedStartDate)}</span>
      <span> - </span>
      {!endDate.startOf('day').equals(accountedEndDate.startOf('day')) && (
        <span className='line-through text-gray-400'>{Format.briefDate(endDate)}</span>
      )}
      <span>&nbsp;</span>
      <span>{Format.briefDate(accountedEndDate)}</span>
    </span>
    <span className='bg-slate-700 rounded-full px-4 py-1 text-sm'>{Format.number(accountedEffort, 2)} Effort</span>
  </div>
)

export { ReportSprint }