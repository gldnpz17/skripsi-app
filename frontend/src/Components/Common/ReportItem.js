import { useCallback } from "react"
import { Format } from "../../common/Format"
import { Cash, Edit, Speedometer } from "../../common/icons"
import { IconButton } from "./IconButton"
import { CpiReason, SpiReason } from "../../pages/Dashboard"

const ReportItemContainer = ({ children, onClick, href }) => (
  <a {...{ onClick, href }} target='_blank' className='flex relative items-center bg-dark-2 p-4 rounded-md border border-gray-700 shadow cursor-pointer hover:brightness-125 duration-150 z-50'>
    {children}
  </a>
)

const ReportStatus = ({ status, tooltip }) => (
  <span className={`${Format.statusColor(status, 'bg-')} text-black px-2 rounded text-sm relative group`}>
    <div className='absolute bottom-8 bg-dark-2 px-2 py-1 text-white rounded border border-gray-700 left-1/2 -translate-x-1/2 shadow-md invisible group-hover:visible z-50'>
      {tooltip}
    </div>
    {Format.status(status)}
  </span>
)

const ReportItem = ({ 
  organizationName,
  projectId,
  teamId,
  reportMetric: {
    report: {
      id,
      startDate
    },
    schedulePerformanceIndex,
    costPerformanceIndex,
    errors,
    cpiCriteria,
    spiCriteria
  } 
}) => (
  <ReportItemContainer href={`/team/${organizationName}/${projectId}/${teamId}/reports/${id}/edit`}>
    <span className='mr-2'>{Format.month(startDate)}</span>
    <span className='flex-grow'></span>
    <Speedometer className='h-4 mr-2' />
    <ReportStatus
      status={Format.performanceIndex(schedulePerformanceIndex).status}
      tooltip={
        <div className='w-80'>
          <SpiReason criteria={spiCriteria} {...{ schedulePerformanceIndex }} />
        </div>
      }
    />
    <Cash className='h-4 mr-2 ml-4' />
    <ReportStatus
      status={errors.includes('ZERO_EXPENDITURE') ? 'Healthy' : Format.performanceIndex(costPerformanceIndex).status}
      tooltip={
        <div className='w-80'>
          <CpiReason criteria={cpiCriteria} {...{ costPerformanceIndex }} />
        </div>
      }
    />
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