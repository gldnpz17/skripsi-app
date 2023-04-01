import { useMemo, useState } from "react"
import { Line } from "react-chartjs-2"
import { Form, Link } from "react-router-dom"
import { ApplicationError } from "../common/ApplicationError"
import { CalendarCheck, Cash, CashStack, CheckeredFlag, Configuration, OpenInNew, PlusCircle, Speedometer, Warning } from "../common/icons"
import { CategoryScale, Chart as ChartJS, Legend, LinearScale, LineElement, PointElement, Title, Tooltip } from 'chart.js'
import { Format } from "../common/Format"
import { useQuery } from "react-query"
import { readTeamDetails, readTeamMetrics, readTeamMetricsTimeline, readTrackedTeams, readUntrackedTeams } from "../api-requests/Teams"
import { Button } from "../Components/Common/Button"
import { IconButton } from '../Components/Common/IconButton'
import { BlankReportItem, ReportItem } from "../Components/Common/ReportItem"
import { readAvailableReports, readTeamReports } from "../api-requests/Reports"
import { DateTime } from "luxon"

const ExternalLinkWrapper = ({ to, children, ...props }) => {
  const isAbsolute = /https:\/\//g.test(to)

  return isAbsolute ? <a href={to} {...props}>{children}</a> : <Link {...{ to, ...props }}>{children}</Link>
}

const ExternalLink = ({ className, children, to }) => (
  <ExternalLinkWrapper {...{ to }} target='_blank' className={`text-sm flex gap-1 items-center underline cursor-pointer text-gray-400 hover:text-secondary-light duration-150 ${className}`}>
    <span>{children}</span>
    <OpenInNew className='h-4' />
  </ExternalLinkWrapper>
)  

const TeamListItem = ({ team: { id, name }, selected = false, onClick }) => {
  return (
    <div {...{ onClick }} className='group relative border border-gray-700 rounded-md cursor-pointer overflow-hidden hover:-translate-x-1 duration-150 hover:brightness-125 bg-dark-2'>
      <div className={`absolute top-0 bottom-0 left-0 w-1 ${selected && 'bg-secondary-dark'} duration-150`} />
      <span className='px-4 py-2 flex'>
        <span className='flex-grow font-semibold overflow-hidden whitespace-nowrap mr-2 overflow-ellipsis'>
          {name}
        </span>
        <span className={`${Format.statusColor('Healthy', 'text-')}`}>
          {Format.status('Healthy')}
        </span>
      </span>
    </div>
  )
}

const TeamsListSection = ({ setSelectedTeam }) => {
  const {
    data: teams,
    isLoading: teamsLoading
  } = useQuery(['projects', 'tracked'], readTrackedTeams)

  return (
    <div>
      <div className='mb-3 text-gray-300 font-semibold'>Teams</div>
      {!teamsLoading && (
        <>
          <div className='flex flex-col gap-4 mb-6'>
            {teams.map(team => (
              <TeamListItem key={team.id} {...{ team }} selected={false} onClick={() => setSelectedTeam(team)} />
            ))}
          </div>
          <a href='/track-new' target='_blank'>
            <Button className='w-full'>
              <PlusCircle className='h-4' />
              <span>Track Team</span>
            </Button>
          </a>
        </>
      )}
    </div>
  )
}

const HealthComponentStatus = ({ title, content, severity, children }) => {
  const textColor = useMemo(() => {
    if (severity === undefined) {
      return 'text-white'
    }

    return `text-${Format.statusColor(severity)}`
  }, [severity])

  return (
    <div className='rounded-md border border-gray-700 p-4 bg-dark-2 shadow-lg'>
      <div className='text-gray-400 text-sm font-bold mb-1'>{title}</div>
      <div className={`${textColor} text-xl`}>
        {content}
      </div>
      {children && (
        <hr className='my-3 border-gray-700' />
      )}
      <div>
        {children}
      </div>
    </div>
  )
}

const ProgressBar = ({ progress, className }) => (
  <div className={`h-2 rounded-lg overflow-hidden ${className} relative rounded`}>
    {/* Background */}
    <div className='bg-primary-light absolute inset-0' />
    {/* Progress */}
    <div className={`absolute left-0 rounded-lg top-0 bottom-0 bg-primary-dark`} style={{ right: `${(1 - progress) * 100}%` }} />
  </div>
)

const DATASET = {
  label: 'Project Health',
  data: [0.2, 0.6, 0.3, 0.2, 0.5, 0.9],
  borderColor: 'rgb(111, 134, 191)',
  tension: 0.3
}

ChartJS.register(LinearScale, CategoryScale, PointElement, LineElement, Title, Tooltip, Legend)

const MetricButton = ({ children, onClick, active }) => (
  <button {...{ onClick }} className={`px-3 py-0 text-sm border duration-150 border-secondary-dark whitespace-nowrap rounded-md text-white ${active && 'bg-secondary-dark border text-black shadow-sm shadow-secondary-dark'}`}>
    {children}
  </button>
)

const HealthLineChart = ({ dataPoints }) => {
  const metrics = {
    PV: {
      label: 'Planned Value',
      map: (dataPoint) => dataPoint.basicMetrics.plannedValue
    },
    EV: {
      label: 'Earned Value',
      map: (dataPoint) => dataPoint.basicMetrics.earnedValue
    },
    AC: {
      label: 'Actual Cost',
      map: (dataPoint) => dataPoint.basicMetrics.actualCost
    },
    CPI: {
      label: 'Cost Performance Index',
      map: (dataPoint) => dataPoint.healthMetrics.costPerformanceIndex
    },
    CV: {
      label: 'Cost Variance',
      map: (dataPoint) => dataPoint.healthMetrics.costVariance
    },
    SPI: {
      label: 'Schedule Performance Index',
      map: (dataPoint) => dataPoint.healthMetrics.schedulePerformanceIndex
    },
    SV: {
      label: 'Schedule Variance',
      map: (dataPoint) => dataPoint.healthMetrics.scheduleVariance
    },
  }

  const [metric, setMetric] = useState(metrics.PV)

  const labels = useMemo(() => dataPoints.map(dataPoint => dataPoint.report.startDate.toFormat('MMM yyyy')), [dataPoints])

  const dataset = useMemo(() => ({
    label: metric.label,
    data: dataPoints.map(metric.map),
    borderColor: 'rgb(111, 134, 191)',
    tension: 0.3
  }), [metric, dataPoints])

  return (
    <div className='h-96 flex flex-col w-full rounded-md bg-dark-2 p-4 border border-gray-700'>
      <div className='w-full mb-4 flex items-center'>
        <div className='text-gray-400 text-sm font-bold flex-grow'>Project Health History</div>
        <IconButton onClick={() => alert('Hello')}>
          <Configuration className='h-5' />
        </IconButton>
      </div>
      <div className='flex-grow mb-3'>
        <Line
          style={{
            minHeight: "100%",
            height: "0",
            width: "100%"
          }}
          options={{
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: null,
            },
            scales: {
              x: {
                ticks: {
                  color: 'white'
                },
                grid: {
                  display: false
                }
              },
              y: {
                display: false,
                ticks: {
                  color: 'white'
                },
                grid: {
                  display: false
                }
              }
            }
          }}
          data={{
            labels,
            datasets: [dataset]
          }}
        />
      </div>
      <div className='flex gap-3 flex-wrap mb-4'>
        {Object.keys(metrics).map(key => (
          <MetricButton {...{ key }} active={metric.label === metrics[key].label} onClick={() => setMetric(metrics[key])}>
            {metrics[key].label}
          </MetricButton>
        ))}
      </div>
    </div>
  )
}

const ReportsList = ({ selectedTeam, availableReports, reportMetrics }) => (
  <div>
    <div className='flex text-sm items-center mb-4'>
      <span className='flex-grow font-bold text-gray-400'>Recent Reports</span>
      <ExternalLink>
        See More
      </ExternalLink>
    </div>
    <div className='flex flex-col gap-4'>
      {availableReports?.map(report => (
        <BlankReportItem key={Format.reportKey(report)} {...{ report, selectedTeam }} />
      ))}
      {reportMetrics.map(reportMetric => (
        <ReportItem
          key={reportMetric.report.startDate}
          organizationName={selectedTeam.organization.name}
          projectId={selectedTeam.project.id}
          teamId={selectedTeam.id}
          {...{ reportMetric }} />
      ))}
    </div>
  </div>
)

const HealthComponentInformation = ({ title, Icon, content }) => (
  <div className='flex flex-col'>
    <div className='flex gap-1 items-center'>
      <Icon className='h-4' />
      <div className='text-sm text-gray-400'>{title}</div>
    </div>
    <div>{content}</div>
  </div>
)

const HealthComponentError = ({ reason, helpLink, helpText }) => (
  <div>
    <div className='text-orange-300 flex gap-1 items-center'>
      <Warning className='h-4' />
      {reason}
    </div>
    <ExternalLink
      to={helpLink}
    >
      {helpText}
    </ExternalLink>
  </div>
)

const HealthError = ({ errorCode, team }) => {
  const errorProps = {
    'TEAM_NO_DEADLINE': {
      reason: 'Deadline not set',
      helpText: 'Set deadline',
      helpLink: `/teams/${team.organization.name}/${team.project.id}/${team.id}`
    },
    'TEAM_NO_SPRINTS': {
      reason: 'No sprints',
      helpText: 'Add sprint',
      helpLink: `https://dev.azure.com/${team.organization.name}/${encodeURI(team.project.name)}`
    }
  }

  return (
    <HealthComponentError {...errorProps[errorCode]} />
  )
}

const TeamDetailsSection = ({ selectedTeam }) => {
  const organizationName = selectedTeam.organization.name
  const projectId = selectedTeam.project.id
  const projectName = selectedTeam.project.name
  const teamId = selectedTeam.id
  const teamName = selectedTeam.name

  const {
    isLoading: metricsLoading,
    data: metrics
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'metrics'],
    async () => await readTeamMetrics({ organizationName, projectId, teamId })
  )

  const {
    isLoading: metricsTimelineLoading,
    data: metricsTimeline
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'metrics', 'timeline'],
    async () => await readTeamMetricsTimeline({ organizationName, projectId, teamId })
  )

  const {
    isLoading: reportsLoading,
    data: reportMetrics
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'reports'],
    async () => await readTeamReports({ organizationName, projectId, teamId })
  )

  const {
    isLoading: availableReportsLoading,
    data: availableReports
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'available-reports'],
    async () => await readAvailableReports({ organizationName, projectId, teamId })
  )

  if (reportsLoading || availableReportsLoading || metricsTimelineLoading) return <></>

  return (
    <div className='grid grid-cols-12 gap-x-4 gap-y-6'>
      <div className='flex items-center col-span-12'>
        <div className='flex-grow'>
          <h1 className='text-2xl mb-1'>Team Name</h1>
          <ExternalLink
            to={`/teams/${organizationName}/${projectId}/${teamId}`}
          >
            Team Settings
          </ExternalLink>
        </div>
        <div>
          <div className='text-sm text-gray-400 text-right mb-1'>Overall Project Health</div>
          <div className='flex gap-2 items-center'>
            <ProgressBar progress={0.8} className='w-32' />
            <span>
              80%
            </span>
          </div>
        </div>
      </div>
      {!metricsLoading && (
        <>
          <div className='col-span-4'>
            <HealthComponentStatus
              title="Estimate at Completion"
              content={Format.currency(metrics.forecastMetrics.estimateAtCompletion)}
            >
              <HealthComponentInformation
                Icon={CalendarCheck}
                title='Estimate to Completion'
                content={Format.currency(Math.max(0, metrics.forecastMetrics.estimateToCompletion))}
              />
            </HealthComponentStatus>
          </div>
          <div className='col-span-4'>
            <HealthComponentStatus
              title="Cost Performance Index"
              content={Format.number(metrics.healthMetrics.costPerformanceIndex, 2)}
              severity={Format.performanceIndex(metrics.healthMetrics.costPerformanceIndex).status}
            >
              <HealthComponentInformation
                Icon={CheckeredFlag}
                title='Cost Variance'
                content={Format.currency(metrics.healthMetrics.costVariance)}
              />
            </HealthComponentStatus>
          </div>
          <div className='col-span-4'>
            <HealthComponentStatus
              title='Schedule Performance Index'
              content={Format.number(metrics.healthMetrics.schedulePerformanceIndex, 2)}
              severity={Format.performanceIndex(metrics.healthMetrics.schedulePerformanceIndex).status}
            >
              <HealthComponentInformation
                Icon={CashStack}
                title='Schedule Variance'
                content={Format.currency(metrics.healthMetrics.scheduleVariance)}
              />
            </HealthComponentStatus>
          </div>
        </>
      )}
      <div className='col-span-12'>
        <HealthLineChart dataPoints={metricsTimeline} />
      </div>
      <div className='col-span-12 mb-8'>
        <ReportsList {...{ reportMetrics, availableReports, selectedTeam }} />
      </div>
    </div>
  )
}

const NoSelectedTeamPlaceholder = () => (
  <div className='flex flex-col items-center justify-center h-[90vh]'>
    <div className='text-gray-500 text-3xl mb-2'>
      No team selected
    </div>
    <div className='text-gray-600 text-lg'>
      Please select a team
    </div>
  </div>
)

const DashboardPage = () => {
  const [selectedTeam, setSelectedTeam] = useState(null)

  return (
    <div className='h-full overflow-auto'>
      <div className='mr-80 mt-8'>
        {selectedTeam && (
          <TeamDetailsSection {...{ selectedTeam }} />
        )}
        {!selectedTeam && (
          <NoSelectedTeamPlaceholder />
        )}
      </div>
      <div className='w-64 top-8 bottom-0 right-12 fixed pl-4'>
        {/* Divider */}
        <div className='w-px absolute top-0 bottom-8 left-0 bg-gray-700' />
        <TeamsListSection {...{ setSelectedTeam }} />
      </div>
    </div>
  )
}

export { DashboardPage }