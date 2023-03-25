import { useCallback } from "react"
import { Format } from "../../common/Format"
import { Cash, Edit, Speedometer } from "../../common/icons"
import { IconButton } from "./IconButton"

const ReportItemContainer = ({ children, onClick }) => (
  <div {...{ onClick }} className='flex relative items-center bg-dark-2 p-4 rounded-md border border-gray-700 shadow cursor-pointer hover:brightness-125 duration-150'>
    {children}
  </div>
)

const ReportStatus = ({ status }) => (
  <span className={`${Format.statusColor(status, 'bg-')} text-black px-2 rounded text-sm`}>
    {Format.status(status)}
  </span>
)

const ReportItem = ({ report: { date, velocity, expenditureRate, status } }) => (
  <ReportItemContainer>
    <span className='mr-2'>{Format.month(date)}</span>
    <ReportStatus {...{ status }} />
    <span className='flex-grow'></span>
    <Speedometer className='h-4 mr-2' />
    <span className='mr-6 text-gray-400'>{velocity} efforts/sprint</span>
    <Cash className='h-4 mr-2' />
    <span className='text-gray-400'>{Format.currency(expenditureRate)}/sprint</span>
  </ReportItemContainer>
)

const Ping = () => (
  <span className="flex h-3 w-3 absolute -top-1 -right-1">
    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-sky-400 opacity-75"></span>
    <span className="relative inline-flex rounded-full h-3 w-3 bg-sky-500"></span>
  </span>
)

const BlankReportItem = ({ 
  selectedTeam,
  report: { startDate, endDate } 
}) => {
  const createReport = useCallback(() => {
    const organizationName = selectedTeam.organization.name
    const projectId = selectedTeam.project.id
    const teamId = selectedTeam.id

    window.open(`/teams/${organizationName}/${projectId}/${teamId}/reports/create?start=${encodeURIComponent(startDate.toISO())}&end=${encodeURIComponent(endDate.toISO())}`, '_blank')
  }, [startDate, endDate, selectedTeam])

  return (
    <ReportItemContainer onClick={createReport}>
      <span className='mr-2'>{Format.month(startDate)}</span>
      <ReportStatus status='NoData' />
      <span className='flex-grow' />
      <Ping />
    </ReportItemContainer>
  )
}

export { ReportItem, BlankReportItem }