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

const ReportItem = ({ 
  reportMetric: {
    report: {
      startDate
    },
    healthMetrics: {
      schedulePerformanceIndex,
      costPerformanceIndex
    }
  } 
}) => (
  <ReportItemContainer>
    <span className='mr-2'>{Format.month(startDate)}</span>
    <span className='flex-grow'></span>
    <Speedometer className='h-4 mr-2' />
    <ReportStatus status={Format.performanceIndex(schedulePerformanceIndex).status} />
    <Cash className='h-4 mr-2 ml-4' />
    <ReportStatus status={Format.performanceIndex(costPerformanceIndex).status} />
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
      <span className='flex-grow' />
      <ReportStatus status='NoData' />
      <Ping />
    </ReportItemContainer>
  )
}

export { ReportItem, BlankReportItem }