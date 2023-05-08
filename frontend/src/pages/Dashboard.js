import { useCallback, useEffect, useMemo, useState } from "react"
import { Line, Bar } from "react-chartjs-2"
import { Form, Link } from "react-router-dom"
import { ApplicationError } from "../common/ApplicationError"
import { AzureDevops, CalendarCheck, Cash, CashStack, CheckeredFlag, Configuration, DatabaseAlert, OpenInNew, PinFilled, PinFilledOff, PinOutline, PlusCircle, Speedometer, TimelineClock, Triangle, Warning } from "../common/icons"
import { CategoryScale, BarController, BarElement, Chart as ChartJS, Legend, LinearScale, LineElement, PointElement, Title, Tooltip, TimeScale } from 'chart.js'
import { Format } from "../common/Format"
import { useQuery } from "react-query"
import { readTeamDetails, readTeamMetrics, readTeamMetricsTimeline, readTrackedTeams, readUntrackedTeams } from "../api-requests/Teams"
import { Button } from "../Components/Common/Button"
import { IconButton } from '../Components/Common/IconButton'
import { BlankReportItem, ReportItem } from "../Components/Common/ReportItem"
import { readAvailableReports, readTeamReports } from "../api-requests/Reports"
import { DateTime } from "luxon"
import { ExternalLink } from "../Components/Common/ExternalLink"
import { ErrorPlaceholder } from "../Components/Common/ErrorPlaceholder"
import { Skeleton } from "../Components/Common/Skeleton"
import { usePersistedValue } from "../Hooks/usePersistedState"
import { withAuth } from "../HigherOrderComponents/withAuth"
import 'chartjs-adapter-luxon';

const PinButton = ({ pinned, togglePin }) => {
  const onClick = (e) => {
    e.stopPropagation()
    togglePin()
  }

  return (
    <button
      className={`${pinned ? 'block' : 'hidden group-hover/item:block'}`}
      {...{ onClick }}
    >
      {pinned && (
        <div className='group/button'>
          <PinFilled className='h-4 block group-hover/button:hidden' />
          <PinFilledOff className='h-4 hidden group-hover/button:block' />
        </div>
      )}
      {!pinned && (
        <div className='group/button'>
          <PinOutline className='h-4 block group-hover/button:hidden' />
          <PinFilled className='h-4 hidden group-hover/button:block' />
        </div>
      )}
    </button>
  )
}

const TeamListItem = ({ team: { name }, selected = false, onClick, pinned, togglePin }) => {
  return (
    <div
      {...{ onClick }}
      className='group/item relative border border-gray-700 rounded-md cursor-pointer overflow-hidden duration-150 hover:brightness-125 bg-dark-2'
    >
      <div className={`absolute top-0 bottom-0 left-0 w-1 ${selected && 'bg-secondary-dark'} duration-150`} />
      <span className='px-4 py-2 flex'>
        <span className='flex-grow font-semibold overflow-hidden whitespace-nowrap mr-2 overflow-ellipsis'>
          {name}
        </span>
        <PinButton {...{ pinned, togglePin }} />
        {/* TODO: Implement health status icons. */}
        {/* <span className={`${Format.statusColor('Healthy', 'text-')}`}>
          {Format.status('Healthy')}
        </span> */}
      </span>
    </div>
  )
}

const TeamsListSection = ({ setSelectedTeam, teams, teamsLoading, teamPinned, togglePin }) => {
  return (
    <div>
      <div className='mb-3 text-gray-300 font-semibold'>Teams</div>
      {!teamsLoading && (
        <>
          <div className='flex flex-col gap-4 mb-6'>
            {teams.filter(team => !team.archived).map(team => (
              <TeamListItem
                key={team.id}
                selected={false}
                onClick={() => setSelectedTeam(team)}
                pinned={teamPinned(team)}
                togglePin={togglePin(team)}
                {...{ team }} 
              />
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

ChartJS.register(LinearScale, CategoryScale, PointElement, LineElement, BarController, BarElement, Title, Tooltip, Legend, TimeScale)

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
      {dataPoints.length === 0 && (
        <div className='text-gray-500 text-xl h-full font-bold flex flex-col items-center justify-center flex-grow'>
          <DatabaseAlert className='h-32 mb-4' />
          <span>No data to display</span>
        </div>
      )}
      {dataPoints.length > 0 && (
        <>
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
        </>
      )}
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

const TeamDetailsSkeleton = () => (
  <div className='grid grid-cols-12 gap-x-4 gap-y-6 overflow-hidden'>
    <Skeleton className='col-span-5 h-14' />
    <div className='col-span-4' />
    <Skeleton className='col-span-3 h-14' />

    <Skeleton className='col-span-4 h-28' />
    <Skeleton className='col-span-4 h-28' />
    <Skeleton className='col-span-4 h-28' />

    <Skeleton className='col-span-12 h-80' />

    <div className='flex flex-col gap-3 col-span-12'>
      <Skeleton className='col-span-12 h-10' />
      <Skeleton className='col-span-12 h-10' />
      <Skeleton className='col-span-12 h-10' />
    </div>
  </div>
)

const TeamDetailsSection = ({ selectedTeam }) => {
  const organizationName = selectedTeam.organization.name
  const projectId = selectedTeam.project.id
  const projectName = selectedTeam.project.name
  const teamId = selectedTeam.id
  const teamName = selectedTeam.name

  const {
    isLoading: metricsLoading,
    data: metrics,
    error: metricsError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'metrics'],
    async () => await readTeamMetrics({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const {
    isLoading: metricsTimelineLoading,
    data: metricsTimeline
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'metrics', 'timeline'],
    async () => await readTeamMetricsTimeline({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const {
    isLoading: reportMetricsLoading,
    data: reportMetrics,
    error: reportMetricsError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'reports'],
    async () => await readTeamReports({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const {
    isLoading: availableReportsLoading,
    data: availableReports,
    error: availableReportsError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'available-reports'],
    async () => await readAvailableReports({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const reportsError = useMemo(() => {
    return reportMetricsError ?? availableReportsError ?? null
  }, [reportMetricsError, availableReportsError])

  if (metricsLoading || 
    reportMetricsLoading || 
    availableReportsLoading || 
    metricsTimelineLoading
  ) return <TeamDetailsSkeleton />

  return (
    <div className='grid grid-cols-12 gap-x-4 gap-y-6'>
      <div className='flex items-center col-span-12'>
        <div className='flex-grow'>
          <h1 className='text-2xl mb-1'>{teamName} - {projectName}</h1>
          <ExternalLink
            to={`/teams/${organizationName}/${projectId}/${teamId}`}
          >
            Team Settings
          </ExternalLink>
        </div>
        {!metricsError && (
          <div>
            <div className='text-sm text-gray-400 text-right mb-1'>Budget Usage</div>
            <div className='flex gap-2 items-center'>
              <ProgressBar progress={metrics.basicMetrics.actualCost / metrics.forecastMetrics.budgetAtCompletion} className='w-32' />
              <span>
                {Format.number(metrics.basicMetrics.actualCost / metrics.forecastMetrics.budgetAtCompletion * 100, 0)} %
              </span>
            </div>
          </div>
        )}
      </div>
      {metricsError && (
        <ErrorPlaceholder
          className='col-span-12'
          message='Unable to display health metrics.'
          errorCode={metricsError.response.data}
          team={selectedTeam}
        />
      )}
      {!metricsError && (
        <>
          <div className='col-span-4'>
            <HealthComponentStatus
              title="Estimate at Completion"
              content={Format.currency(metrics.forecastMetrics.estimateAtCompletion)}
            >
              <HealthComponentInformation
                Icon={CashStack}
                title='Estimate to Completion'
                content={Format.currency(Math.max(0, metrics.forecastMetrics.estimateToCompletion))}
              />
            </HealthComponentStatus>
          </div>
          <div className='col-span-4'>
            <HealthComponentStatus
              title="Cost Performance Index"
              content={`${Format.number(metrics.healthMetrics.costPerformanceIndex, 2)} (${metrics.healthMetrics.costPerformanceIndex < 1 ? 'Over Budget' : 'Under Budget'})`}
              severity={Format.performanceIndex(metrics.healthMetrics.costPerformanceIndex).status}
            >
              <HealthComponentInformation
                Icon={CashStack}
                title='Cost Variance'
                content={Format.currency(metrics.healthMetrics.costVariance)}
              />
            </HealthComponentStatus>
          </div>
          <div className='col-span-4'>
            <HealthComponentStatus
              title='Schedule Performance Index'
              content={`${Format.number(metrics.healthMetrics.schedulePerformanceIndex, 2)} (${Format.number(metrics.healthMetrics.schedulePerformanceIndex, 2) < 1 ? 'Behind Schedule' : 'Ahead of schedule'})`}
              severity={Format.performanceIndex(metrics.healthMetrics.schedulePerformanceIndex).status}
            >
              <HealthComponentInformation
                Icon={CheckeredFlag}
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
      {reportsError && (
        <ErrorPlaceholder
          className='col-span-12 mb-8'
          message='Unable to display report list.'
          errorCode={reportsError.response.data}
          team={selectedTeam}
        />
      )}
      {!reportsError && (
        <div className='col-span-12 mb-8'>
          <ReportsList {...{ reportMetrics, availableReports, selectedTeam }} />
        </div>
      )}
    </div>
  )
}

const IndexMeter = ({
  label,
  icon,
  status,
  severity,
  meter: {
    minLabel,
    midLabel,
    maxLabel
  },
  index: {
    name,
    description,
    value,
    hint
  }
}) => {
  const Icon = icon
  const SeverityColor = {
    2: 'text-red-400',
    1: 'text-yellow-400',
    0: 'text-green-400'
  }
  const position = useMemo(() => Math.round((Math.max(0, Math.min(2, value)) / 2) * 100), [value])

  return (
    <div className='rounded-md border border-gray-700 p-4 bg-dark-2 shadow-lg'>
      <div className='text-gray-400 flex items-center'>
        <Icon className='h-4 inline' />&nbsp;
        {label}
      </div>
      <div className={`text-xl font-bold ${SeverityColor[severity]} mb-4`}>{status}</div>
      <div className='mb-4'>
        {/* Meter */}
        <div className='relative mb-2'>
          <div className='h-2 bg-gray-700 rounded-full z-0'></div>
          <div className='h-4 w-1 absolute -translate-x-1/2 left-1/2 z-10 bg-primary-dark top-1/2 -translate-y-1/2'></div>
          <Triangle style={{ left: `${position}%` }} className='h-3 rotate-180 text-primary-light absolute bottom-1/2 z-20 -translate-x-1/2' />
        </div>
        <div className='flex justify-between text-sm text-gray-400'>
          {/* Label */}
          <div>{minLabel}</div>
          <div>{midLabel}</div>
          <div>{maxLabel}</div>
        </div>
      </div>
      {/* Index */}
      <div className='flex items-center'>
        <div className='flex-grow'>
          <div className='text-sm'>{name}</div>
          <div className='text-xs text-gray-400'>{description}</div>
        </div>
        <div className='flex items-center gap-2'>
          <div className={`text-xl font-bold ${SeverityColor[severity]}`}>{value}</div>
          <div className='text-xs font-semibold px-2 py-1 rounded-full bg-gray-700'>{hint}</div>
        </div>
      </div>
    </div>
  )
}

const TimelineItem = ({ 
  timelineItem: {
    date,
    label,
    info
  }
}) => (
  <div className='flex gap-2'>
    <div className='h-6 w-6 flex items-center justify-center'>
      <div className='rounded-full h-3 w-3 ring-2 ring-gray-500 bg-primary-dark'></div>
    </div>
    <div>
      <div>{label} ({Format.briefDate(date)})</div>
      {info && (
        <div className='text-sm text-gray-400'>{info}</div>
      )}
    </div>
  </div>
)

const Timeline = ({ timelineItems }) => {
  return (
    <div className='rounded-md border border-gray-700 p-4 bg-dark-2 shadow-lg'>
      <div className='text-gray-400 mb-2 flex items-center gap-1'>
        <TimelineClock className='h-4' />
        Timeline
      </div>
      <div className='relative'>
        <div className='absolute w-[2px] bg-gray-600 left-3 top-0 bottom-0 z-0 -translate-x-1/2'></div>
        <div className='flex flex-col gap-4 z-10 relative py-2'>
          {timelineItems.map(timelineItem => (
            <TimelineItem key={timelineItem.label} {...{ timelineItem }} />
          ))}
        </div>
      </div>
    </div>
  )
}

const CostInfo = ({ 
  primary: {
    label: primaryLabel,
    description: primaryDescription,
    value: primaryValue
  },
  secondary: {
    label: secondaryLabel,
    value: secondaryValue
  }
}) => (
  <div className='flex-grow'>
    <div className='text-sm flex items-center'>
      <div className='h-4 w-[2px] bg-primary-dark mr-1'></div>
      {primaryLabel}
    </div>
    <div className='text-xs text-gray-400'>{primaryDescription}</div>
    <div className='font-bold'>{Format.currency(primaryValue)}</div>
    <hr className='border-gray-600 my-1' />
    <div className='text-xs text-gray-400 font-bold'>{secondaryLabel}</div>
    <div className='text-sm'>{Format.currency(secondaryValue)}</div>        
  </div>
)

const CostSection = () => {
  const labels = ['AC', 'BAC', 'EAC']
  const dataset = {
    data: [10000000, 30000000, 35000000],
    backgroundColor: 'rgb(111, 134, 191)'
  }
  const options = {
    indexAxis: 'y',
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: null,
    },
    scales: {
      x: {
        ticks: {
          color: '#9ca3af'
        }
      },
      y: {
        ticks: {
          color: '#9ca3af'
        }
      }
    }
  }

  return (
    <div className='rounded-md border border-gray-700 p-4 bg-dark-2 shadow-lg flex gap-2 flex-col'>
      <div className='flex gap-6'>
        <CostInfo
          primary={{
            label: 'Actual Cost (AC)',
            description: 'The amount of money you\'ve spent',
            value: 10000000
          }}
          secondary={{
            label: 'Remaining budget',
            value: 20000000
          }}
        />
        <CostInfo
          primary={{
            label: 'Budget at Completion (BAC)',
            description: 'The total budget of the project',
            value: 10000000
          }}
          secondary={{
            label: 'Cost per effort',
            value: 1000
          }}
        />
        <CostInfo
          primary={{
            label: 'Estimate at Completion (EAC)',
            description: 'The estimated final cost of the project',
            value: 12000000
          }}
          secondary={{
            label: 'Remainder to complete',
            value: 70000000
          }}
        />
      </div>
      <div className='h-32 w-full relative'>
        <Bar
          className='absolute inset-0'
          data={{ labels, datasets: [dataset] }}
          {...{ options }}
        />
      </div>
    </div>
  )
}

const ChartLegend = ({ items = [] }) => (
  <div>
    {items.map((item, index) => (
      <div key={index} className='flex items-center gap-2 text-gray-300 text-sm mb-1'>
        <span style={{ backgroundColor: item.color }} className='w-3 h-3 rounded'></span>
        <span>{item.label}</span>
      </div>
    ))}
  </div>
)

const CpiChartSection = () => {
  const options = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: {
        ticks: {
          color: '#9ca3af'
        }
      },
      y: {
        beginAtZero: true,
        ticks: {
          color: '#9ca3af'
        }
      }
    },
    plugins: {
      legend: null,
      tooltip: {
        filter: ({ datasetIndex }) => datasetIndex === 0
      }
    }
  }

  const labels = ['January 2023', 'February 2023', 'March 2023', 'April 2023', 'May 2023']
  const dataset = {
    label: 'Cost Performance Index',
    data: [1.3, 1.1, 0.8, 1.2, 1.1],
    borderColor: 'rgb(179, 136, 235)'
  }
  const baseline = {
    label: 'Baseline',
    data: new Array(labels.length).fill(1),
    borderColor: 'rgb(248, 113, 113)',
    borderDash: [5, 5],
    pointStyle: false
  }

  return (
    <div className='rounded-md border border-gray-700 p-4 bg-dark-2 shadow-lg'>
      <div>Cost Performance Index</div>
      <div className='text-sm text-gray-400 mb-4'>
        A cost performance index below 1 indicates overbudgeting (<span className='underline'>Learn more</span>)
      </div>
      <div className='mb-4'>
        <Line
          style={{ width: '100%', height: '18rem' }}
          data={{
            labels,
            datasets: [dataset, baseline]
          }}
          {...{ options }}
        />
      </div>
      <ChartLegend
        items={[
          { color: '#b388eb', label: 'Cost Performance Index (CPI)' },
          { color: '#f87171', label: 'Baseline CPI' }
        ]}
      />
    </div>
  )
}

const BurndownChartSection = () => {
  const options = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: {
        type: 'time',
        ticks: {
          color: '#9ca3af'
        }
      },
      y: {
        beginAtZero: true,
        ticks: {
          color: '#9ca3af'
        }
      }
    },
    plugins: {
      legend: null,
      tooltip: {
        filter: ({ datasetIndex }) => datasetIndex === 0
      }
    }
  }

  const dataset = {
    label: 'Remaining Effort',
    data: [
      { y: 120, x: DateTime.fromObject({ year: 2023, month: 1, day: 1 }).toJSDate() },
      { y: 100, x: DateTime.fromObject({ year: 2023, month: 1, day: 10 }).toJSDate() },
      { y: 100, x: DateTime.fromObject({ year: 2023, month: 1, day: 18 }).toJSDate() },
      { y: 70, x: DateTime.fromObject({ year: 2023, month: 2, day: 5 }).toJSDate() },
      { y: 40, x: DateTime.fromObject({ year: 2023, month: 2, day: 13 }).toJSDate() },
      { y: 30, x: DateTime.fromObject({ year: 2023, month: 2, day: 23 }).toJSDate() }
    ],
    borderColor: 'rgb(179, 136, 235)'
  }
  const baseline = {
    label: 'Planned',
    data: [
      { y: 120, x: DateTime.fromObject({ year: 2023, month: 1, day: 1 }).toJSDate() },
      { y: 0, x: DateTime.fromObject({ year: 2023, month: 3, day: 15 }).toJSDate() }
    ],
    borderColor: 'rgb(248, 113, 113)',
    borderDash: [5, 5],
    pointStyle: false
  }

  return (
    <div className='rounded-md border border-gray-700 p-4 bg-dark-2 shadow-lg'>
      <div className='mb-4'>Burndown Chart</div>
      <div className='mb-4'>
        <Line
          style={{ width: '100%', height: '18rem' }}
          data={{
            datasets: [dataset, baseline]
          }}
          {...{ options }}
        />
      </div>
      <ChartLegend
        items={[
          { color: '#b388eb', label: 'Remaining effort' },
          { color: '#f87171', label: 'Ideal effort' }
        ]}
      />
    </div>
  )
}

const VelocityChartSection = () => {
  const options = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: {
        ticks: {
          color: '#9ca3af'
        }
      },
      y: {
        beginAtZero: true,
        ticks: {
          color: '#9ca3af'
        }
      }
    },
    plugins: {
      legend: null,
    }
  }

  const dataset = {
    label: 'Velocity',
    data: [
      { y: 120, x: 'Sprint #1' },
      { y: 100, x: 'Sprint #2' },
      { y: 100, x: 'Sprint #3' },
      { y: 70, x: 'Sprint #4' },
      { y: 40, x: 'Sprint #5' },
      { y: 30, x: 'Sprint #6' }
    ],
    backgroundColor: 'rgb(111, 134, 191)',
    order: 1
  }

  const requiredAverage = {
    label: 'Minimum Average Velocity',
    type: 'line',
    data: [
      { y: 95, x: 'Sprint #1' },
      { y: 80, x: 'Sprint #2' },
      { y: 70, x: 'Sprint #3' },
      { y: 85, x: 'Sprint #4' },
      { y: 70, x: 'Sprint #5' },
      { y: 90, x: 'Sprint #6' }
    ],
    borderColor: 'rgb(248, 113, 113)',
    borderDash: [5, 5],
    order: 0
  }

  return (
    <div className='rounded-md border border-gray-700 p-4 bg-dark-2 shadow-lg'>
      <div className='mb-4'>Velocity Chart</div>
      <div className='mb-4'>
        <Bar
          style={{ width: '100%', height: '18rem' }}
          data={{
            datasets: [dataset, requiredAverage]
          }}
          {...{ options }}
        />
      </div>
      <ChartLegend
        items={[
          { color: '#b388eb', label: 'Sprint velocity (efforts/day)' },
          { color: '#f87171', label: 'Minimum average velocity to complete the project in time (efforts/day)' }
        ]}
      />
    </div>
  )
}

const NewTeamDetailsSection = ({ selectedTeam }) => {
  const organizationName = selectedTeam.organization.name
  const projectId = selectedTeam.project.id
  const projectName = selectedTeam.project.name
  const teamId = selectedTeam.id
  const teamName = selectedTeam.name

  const azureDevopsUrl = useMemo(() => {
    return `https://dev.azure.com/${encodeURIComponent(organizationName)}/${encodeURIComponent(projectName)}/_boards/board/t/${encodeURIComponent(teamName)}/Backlog%20items`
  }, [organizationName, projectName, teamName])

  const {
    isLoading: reportMetricsLoading,
    data: reportMetrics,
    error: reportMetricsError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'reports'],
    async () => await readTeamReports({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const {
    isLoading: availableReportsLoading,
    data: availableReports,
    error: availableReportsError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'available-reports'],
    async () => await readAvailableReports({ organizationName, projectId, teamId }),
    { retry: false }
  )

  return (
    <div className='grid grid-cols-12 gap-4 mb-12'>
      {/* Team Name */}
      <div className='col-span-12 flex items-center'>
        <div className='flex-grow'>
          <div className='text-2xl'>{teamName} - {projectName}</div>
          <ExternalLink to={`/teams/${organizationName}/${projectId}/${teamId}`}>
            Team Settings
          </ExternalLink>
        </div>
        <a href={azureDevopsUrl} target='_blank'>
          <Button className='shadow-xl'>
            <AzureDevops className='h-4' />
            Azure DevOps Board
          </Button>
        </a>
      </div>
      {/* SPI */}
      <div className='col-span-6'>
        <IndexMeter
          label='Punctuality'
          icon={CheckeredFlag}
          status='Behind Schedule'
          severity={2}
          meter={{
            minLabel: '100% late',
            midLabel: 'On time',
            maxLabel: '100% early'
          }}
          index={{
            name: 'Schedule Performance Index (SPI)',
            description: 'How punctual we are',
            value: 0.6,
            hint: '40% late'
          }}
        />
      </div>
      {/* CPI */}
      <div className='col-span-6'>
        <IndexMeter
          label='Budget'
          icon={CashStack}
          status='Over Budget'
          severity={2}
          meter={{
            minLabel: '100% over budget',
            midLabel: 'On budget',
            maxLabel: '100% under budget'
          }}
          index={{
            name: 'Cost Performance Index (CPI)',
            description: 'How well we\'re doing financially',
            value: 0.8,
            hint: '20% over budget'
          }}
        />
      </div>
      {/* Cost Breakdown */}
      <div className='col-span-12'>
        <CostSection />
      </div>
      {/* Timeline */}
      <div className='col-span-6'>
        <Timeline
          timelineItems={[
            {
              date: DateTime.fromObject({ year: 2023, month: 3, day: 1 }),
              label: 'Start date',
              info: '2 days ago'
            },
            {
              date: DateTime.fromObject({ year: 2023, month: 3, day: 3 }),
              label: 'Today',
              info: null
            },
            {
              date: DateTime.fromObject({ year: 2023, month: 3, day: 10 }),
              label: 'Deadline',
              info: '7 days from now'
            },
            {
              date: DateTime.fromObject({ year: 2023, month: 3, day: 23 }),
              label: 'Estimated completion',
              info: '13 days behind schedule, 20 days from now'
            }
          ]}
        />
      </div>
      {/* Recent Reports */}
      <div className='col-span-6 pt-2'>
        {!reportMetricsLoading && !availableReportsLoading && (
          <ReportsList {...{ selectedTeam, availableReports, reportMetrics }} />
        )}
      </div>
      {/* CPI Chart */}
      <div className='col-span-12'>
        <CpiChartSection />
      </div>
      {/* Burndown Chart */}
      <div className='col-span-12'>
        <BurndownChartSection />
      </div>
      {/*  */}
      <div className='col-span-12'>
        <VelocityChartSection />
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

const Page = () => {
  const {
    data: teams,
    isLoading: teamsLoading
  } = useQuery(['projects', 'tracked'], readTrackedTeams)

  const [selectedTeam, setSelectedTeam] = useState(undefined)
  
  useEffect(() => {
    if (!teams) return

    const storedTeamId = window.localStorage.getItem('defaultTeamId')
    const team = teams.find(team => team.id === storedTeamId)
    setSelectedTeam(team)
  }, [teamsLoading])

  const setSelectedTeamProxy = (team) => {
    window.localStorage.setItem('defaultTeamId', team.id)
    setSelectedTeam(team)
  }

  const [pinnedTeamIds, setPinnedTeamIds] = usePersistedValue('pinnedTeamIds', [])

  const teamPinned = useCallback((team) => Boolean(pinnedTeamIds.find(id => id === team.id)), [pinnedTeamIds])

  const togglePin = useCallback((team) => () => {
    const newList = [...pinnedTeamIds]
    if (newList.find(id => id === team.id)) {
      setPinnedTeamIds(newList.filter(id => id !== team.id))
    } else {
      setPinnedTeamIds([...newList, team.id])
    }
  }, [pinnedTeamIds])

  const sortedTeams = useMemo(() => {
    if (!teams) return []
    const pinnedTeams = teams.filter(team => Boolean(pinnedTeamIds.find(id => id === team.id)))
    const unpinnedTeams = teams.filter(team => !Boolean(pinnedTeamIds.find(id => id === team.id)))
    return [...pinnedTeams, ...unpinnedTeams]
  }, [teams, pinnedTeamIds])

  return (
    <div className='h-full overflow-auto'>
      <div className='mr-80 mt-8'>
        {selectedTeam && (
          <NewTeamDetailsSection {...{ selectedTeam }}  />
        )}
        {selectedTeam === null && (
          <NoSelectedTeamPlaceholder />
        )}
      </div>
      <div className='w-64 top-8 bottom-0 right-12 fixed pl-4'>
        {/* Divider */}
        <div className='w-px absolute top-0 bottom-8 left-0 bg-gray-700' />
        <TeamsListSection
          setSelectedTeam={setSelectedTeamProxy}
          teams={sortedTeams}
          {...{ teamsLoading, teamPinned, togglePin }}
        />
      </div>
    </div>
  )
}

const DashboardPage = withAuth(Page)

export { DashboardPage }