import { useCallback, useEffect, useMemo, useState } from "react"
import { Line, Bar } from "react-chartjs-2"
import { Form, Link } from "react-router-dom"
import { ApplicationError } from "../common/ApplicationError"
import { AzureDevops, CalendarCheck, Cash, CashStack, CheckeredFlag, Configuration, DatabaseAlert, OpenInNew, PinFilled, PinFilledOff, PinOutline, PlusCircle, Speedometer, TimelineClock, Triangle, Warning } from "../common/icons"
import { CategoryScale, BarController, BarElement, Chart as ChartJS, Legend, LinearScale, LineElement, PointElement, Title, Tooltip, TimeScale } from 'chart.js'
import { Format } from "../common/Format"
import { useQuery } from "react-query"
import { readTeamBurndownChart, readTeamCpi, readTeamCpiChart, readTeamFinances, readTeamMilestoneChart, readTeamSpi, readTeamTimeline, readTeamVelocityChart, readTrackedTeams, readWorkCostChart } from "../api-requests/Teams"
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

ChartJS.register(LinearScale, CategoryScale, PointElement, LineElement, BarController, BarElement, Title, Tooltip, Legend, TimeScale)

const ReportsList = ({ selectedTeam, availableReports, reportMetrics, itemLimit = 3 }) => {
  const { blankReports, reports } = useMemo(() => {
    if (!itemLimit) {
      return ({
        blankReports: availableReports,
        reports: reportMetrics
      })
    }

    const blankReports = availableReports?.slice(0, itemLimit) ?? []

    if (blankReports.length >= itemLimit) {
      return ({ blankReports, reports: [] })
    }

    const reports = reportMetrics.slice(0, itemLimit - blankReports.length)

    return ({ blankReports, reports })
  }, [availableReports, reportMetrics, itemLimit])

  return (
    <div>
      <div className='flex text-sm items-center mb-4'>
        <span className='flex-grow font-bold text-gray-400'>Recent Reports</span>
        <ExternalLink to={`/teams/${selectedTeam.organization.name}/${selectedTeam.project.id}/${selectedTeam.id}#reports`}>
          See More
        </ExternalLink>
      </div>
      <div className='flex flex-col gap-4'>
        {blankReports?.map(report => (
          <BlankReportItem key={Format.reportKey(report)} {...{ report, selectedTeam }} />
        ))}
        {reports.map(report => (
          <ReportItem
            key={report.report.startDate}
            organizationName={selectedTeam.organization.name}
            projectId={selectedTeam.project.id}
            teamId={selectedTeam.id}
            reportMetric={report}
          />
        ))}
      </div>
    </div>
  )
}

const TeamDetailsSkeleton = () => (
  <div className='grid grid-cols-12 gap-x-4 gap-y-6 overflow-hidden'>
    <Skeleton className='col-span-5 h-14' />
    <div className='col-span-4' />
    <Skeleton className='col-span-3 h-14' />

    <Skeleton className='col-span-6 h-32' />
    <Skeleton className='col-span-6 h-32' />

    <Skeleton className='col-span-12 h-36' />

    <Skeleton className='col-span-6 h-36' />
    <div className='col-span-6'>
      <div className='flex flex-col gap-3 col-span-12'>
        <Skeleton className='col-span-12 h-10' />
        <Skeleton className='col-span-12 h-10' />
        <Skeleton className='col-span-12 h-10' />
      </div>
    </div>

    <Skeleton className='col-span-12 h-80' />
    <Skeleton className='col-span-12 h-80' />
    <Skeleton className='col-span-12 h-80' />
  </div>
)

const TooltipWrapper = ({ children, tooltip }) => (
  <div className='relative group z-50'>
    <div className='absolute bottom-full left-1/2 -translate-x-1/2 -translate-y-2 shadow-lg bg-dark-2 px-2 py-1 border border-gray-700 group-hover:visible invisible whitespace-nowrap rounded'>
      {tooltip}
    </div>
    {children}
  </div>
)

const PerformanceIndexReason = ({ reason, conclusion }) => (
  <div className='flex flex-col flex-grow'>
    {reason}
    <div className='flex items-center text-sm'>
      <div className='bg-primary-dark h-4 w-1 mr-1 inline-block'></div>
      {conclusion}
    </div>
  </div>
)

const CpiReason = ({
  criteria: {
    budget,
    effort,
    expenditure
  },
  costPerformanceIndex
}) => (
  <PerformanceIndexReason
    reason={
      <div className='mb-1 text-gray-400 text-sm flex-grow'>
        Having finished <b className='text-gray-200'>{Format.number(effort, 2)}</b> units of work,
        your allocated budget is <b className='text-gray-200'>{Format.currency(budget)}</b>.
        Meanwhile, you've spent <b className='text-gray-200'>{Format.currency(expenditure)}</b>.
      </div>
    }
    conclusion={
      <>
        Therefore, your CPI is&nbsp;
        {costPerformanceIndex >= 1 
          ? <b className='text-green-400'>healthy</b> 
          : <b className='text-red-400'>critical</b>}
      </>
    }
  />
)

const SpiReason = ({
  criteria: {
    actualDuration,
    effortQuota,
    completedEffort
  },
  schedulePerformanceIndex
}) => (
  <PerformanceIndexReason
    reason={
      <div className='mb-1 text-gray-400 text-sm flex-grow'>
        In <b className='text-gray-200'>{Math.round(actualDuration)}</b> days,
        you're expected to have finished <b className='text-gray-200'>{Format.number(effortQuota, 2)}</b> units of work.
        Meanwhile, you've finished <b className='text-gray-200'>{Format.number(completedEffort, 2)}</b> units of work.
      </div>
    }
    conclusion={
      <>
        Therefore, your SPI is&nbsp;
        {schedulePerformanceIndex >= 1 
          ? <b className='text-green-400'>healthy</b> 
          : <b className='text-red-400'>critical</b>}
      </>
    }
  />
)

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
  },
  warning,
  detail
}) => {
  const Icon = icon
  const SeverityColor = {
    2: 'text-red-400',
    1: 'text-yellow-400',
    0: 'text-green-400'
  }
  const position = useMemo(() => Math.round((Math.max(0, Math.min(2, Format.performanceIndexPercent(value) + 1)) / 2) * 100), [value])

  return (
    <div className='rounded-md border border-gray-700 p-4 bg-dark-2 shadow-lg h-full flex flex-col'>
      <div className='text-gray-400 flex items-center'>
        <Icon className='h-4 inline' />&nbsp;
        {label}
      </div>
      <div className='flex items-center mb-4 '>
        <div className={`text-xl font-bold ${SeverityColor[severity]} flex-grow`}>
          {status}
        </div>
        {warning && (
          <TooltipWrapper tooltip={warning}>
            <Warning className='h-6 text-yellow-200' />
          </TooltipWrapper>
        )}
      </div>
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
      {detail && (
        <div className='flex-grow flex flex-col'>
          <div className='h-[1px] bg-gray-500 w-full my-2'></div>
          {detail}
        </div>
      )}
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

const CostSection = ({ 
  data: {
    actualCost,
    budgetAtCompletion,
    costPerEffort,
    estimateAtCompletion,
    estimateToCompletion,
    remainingBudget
  }
}) => {
  const labels = ['AC', 'BAC', 'EAC']
  const dataset = {
    data: [actualCost, budgetAtCompletion, estimateAtCompletion],
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
            value: actualCost
          }}
          secondary={{
            label: 'Remaining budget',
            value: remainingBudget
          }}
        />
        <CostInfo
          primary={{
            label: 'Budget at Completion (BAC)',
            description: 'The total budget of the project',
            value: budgetAtCompletion
          }}
          secondary={{
            label: 'Cost per effort',
            value: costPerEffort
          }}
        />
        <CostInfo
          primary={{
            label: 'Estimate at Completion (EAC)',
            description: 'The estimated final cost of the project',
            value: estimateAtCompletion
          }}
          secondary={{
            label: 'Remainder to complete',
            value: estimateToCompletion
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

const CpiChartSection = ({ data }) => {
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

  const { labels, dataset } = useMemo(() => {
    const labels = data.map(datum => Format.month(datum.month))
    const dataset = {
      label: 'Cost Performance Index',
      data: data.map(datum => datum.costPerformanceIndex),
      borderColor: 'rgb(179, 136, 235)'
    }

    return { labels, dataset }
  }, [data])

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

const WorkCostChartSection = ({ data }) => {
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

  const { dataset, requiredAverage } = useMemo(() => {
    const dataset = {
      label: 'Work Cost',
      data: data.map(datum => ({ y: datum.workCost, x: Format.month(datum.month) })),
      backgroundColor: 'rgb(111, 134, 191)',
      order: 1
    }

    const requiredAverage = {
      label: 'Maximum Average Work Cost',
      type: 'line',
      data: data.map(datum => ({ y: datum.maximumAverageWorkCost, x: Format.month(datum.month) })),
      borderColor: 'rgb(248, 113, 113)',
      borderDash: [5, 5],
      order: 0
    }

    return { dataset, requiredAverage }
  }, [data])

  return (
    <div className='rounded-md border border-gray-700 p-4 bg-dark-2 shadow-lg'>
      <div className='mb-2'>Work Cost Chart</div>
      <div className='mb-4 text-sm text-gray-400'>The amount of money spent per work done (lower is better)</div>
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
          { color: '#b388eb', label: 'Cost per unit of work (rupiah/effort)' },
          { color: '#f87171', label: 'Maximum average cost of work to complete the project on budget (rupiah/effort)' }
        ]}
      />
    </div>
  )
}

const BurndownChartSection = ({
  data: {
    startDate,
    endDate,
    items,
    totalEffort
  }
}) => {
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

  const dataset = useMemo(() => {
    return ({
      label: 'Remaining Effort',
      data: [
        { y: totalEffort, x: startDate },
        ...items.map(item => ({
          y: item.remainingEffort,
          x: item.date
        }))
      ],
      borderColor: 'rgb(179, 136, 235)'
    })
  }, [items])

  const baseline = {
    label: 'Planned',
    data: [
      { y: totalEffort, x: startDate },
      { y: 0, x: endDate }
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
          { color: '#b388eb', label: 'Actual remaining effort' },
          { color: '#f87171', label: 'Ideal remaining effort' }
        ]}
      />
    </div>
  )
}

const MilestoneChartSection = ({ data }) => {
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

  const borderDash = (ctx) => ctx.p1.raw.forecast ? [5, 5] : undefined

  const { remainingWork, remainingBudget, idealRemaining } = useMemo(() => {
    const remainingWork = {
      label: 'Remaining Work (%)',
      type: 'line',
      data: data.map(datum => ({ 
        y: Format.number(datum.remainingWorkPercentage, 3),
        x: Format.month(datum.month),
        forecast: datum.isForecast 
      })),
      borderColor: 'rgb(252, 169, 3)',
      segment: {
        borderDash
      },
      order: 1
    }

    const remainingBudget = {
      label: 'Remaining Budget (%)',
      type: 'line',
      data: data.map(datum => ({ 
        y: Format.number(datum.remainingBudgetPercentage, 3), 
        x: Format.month(datum.month),
        forecast: datum.isForecast 
      })),
      borderColor: 'rgb(2, 181, 76)',
      segment: {
        borderDash
      },
      order: 1
    }

    const idealRemaining = {
      label: 'Ideal Remaining Work (%)',
      type: 'line',
      data: data.map(datum => ({ 
        y: Format.number(datum.idealRemainingPercentage, 3), 
        x: Format.month(datum.month),
        forecast: datum.isForecast  
      })),
      borderColor: 'rgb(255, 233, 92)',
      segment: {
        borderDash
      },
      order: 0
    }

    return { remainingWork, remainingBudget, idealRemaining }
  }, [data])

  return (
    <div className='rounded-md border border-gray-700 p-4 bg-dark-2 shadow-lg'>
      <div className='mb-2'>Remaining Work and Budget</div>
      <div className='mb-4 text-sm text-gray-400'>Ideal order : (remaining budget {`>=`} remaining work) & (remaining work {`<=`} ideal remaining work)</div>
      <div className='mb-4'>
        <Bar
          style={{ width: '100%', height: '18rem' }}
          data={{
            datasets: [remainingWork, remainingBudget, idealRemaining]
          }}
          {...{ options }}
        />
      </div>
      <ChartLegend
        items={[
          { color: '#02b54c', label: 'Remaining budget (%)' },
          { color: '#ffe95c', label: 'Ideal remaining work (%)' },
          { color: '#fca903', label: 'Remaining work (%)' },
        ]}
      />
    </div>
  )
}

const VelocityChartSection = ({ data }) => {
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

  const { dataset, requiredAverage } = useMemo(() => {
    const dataset = {
      label: 'Velocity',
      data: data.map(datum => ({ y: datum.velocity, x: `Sprint #${datum.index + 1}` })),
      backgroundColor: 'rgb(111, 134, 191)',
      order: 1
    }

    const requiredAverage = {
      label: 'Minimum Average Velocity',
      type: 'line',
      data: data.map(datum => ({ y: datum.minimumAverageVelocity, x: `Sprint #${datum.index + 1}` })),
      borderColor: 'rgb(248, 113, 113)',
      borderDash: [5, 5],
      order: 0
    }

    return { dataset, requiredAverage }
  }, [data])

  return (
    <div className='rounded-md border border-gray-700 p-4 bg-dark-2 shadow-lg'>
      <div className='mb-2'>Velocity Chart</div>
      <div className='mb-4 text-sm text-gray-400'>The amount of work done per day (higher is better)</div>
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

const PunctualitySection = ({ 
  spi: {
    schedulePerformanceIndex,
    criteria
  }
}) => (
  <IndexMeter
    label='Punctuality'
    icon={CheckeredFlag}
    status={schedulePerformanceIndex >= 1 ? 'Ahead of Schedule' : 'Behind Schedule'}
    severity={
      Format.severity(schedulePerformanceIndex, [
        value => value > 1.05,
        value => value >= 0.95 && value <= 1.05,
        value => value < 0.95
      ])
    }
    meter={{
      minLabel: '100% early',
      midLabel: 'On time',
      maxLabel: '100% late'
    }}
    index={{
      name: 'Schedule Performance Index (SPI)',
      description: 'How punctual we are',
      value: Format.number(schedulePerformanceIndex, 2),
      hint: schedulePerformanceIndex >= 1 ? `${Math.abs(Math.round(Format.performanceIndexPercent(schedulePerformanceIndex) * 100))}% early` : `${Math.abs(Math.round(Format.performanceIndexPercent(schedulePerformanceIndex) * 100))}% late`
    }}
    detail={<SpiReason {...{ criteria, schedulePerformanceIndex }} />}
  />
)

const BudgetSection = ({ 
  cpi: { 
    costPerformanceIndex,
    criteria
  },
  selectedTeam
}) => (
  <IndexMeter
    label='Budget'
    icon={CashStack}
    status={costPerformanceIndex >= 1 ? 'Under Budget' : 'Over Budget'}
    severity={
      Format.severity(costPerformanceIndex, [
        value => value > 1.05,
        value => value >= 0.95 && value <= 1.05,
        value => value < 0.95
      ])
    }
    meter={{
      minLabel: '100% under budget',
      midLabel: 'On budget',
      maxLabel: '100% over budget'
    }}
    index={{
      name: 'Cost Performance Index (CPI)',
      description: 'How well we\'re doing financially',
      value: Format.number(costPerformanceIndex, 2),
      hint: costPerformanceIndex >= 1 ? `${Math.abs(Math.round(Format.performanceIndexPercent(costPerformanceIndex) * 100))}% under budget` : `${Math.abs(Math.round(Format.performanceIndexPercent(costPerformanceIndex) * 100))}% over budget`
    }}
    warning={selectedTeam.eacFormula === 'Atypical' && <div className='w-96 whitespace-pre-wrap'>Cost estimation formula is atypical. Cost performance may not be reflected in cost estimates.</div>}
    detail={<CpiReason {...{ criteria, costPerformanceIndex }} />}
  />
)

const TimelineSection = ({ 
  data: {
    startDate,
    deadline,
    now,
    estimatedCompletionDate
  }
}) => (
  <Timeline
    timelineItems={[
      {
        date: startDate,
        label: 'Start date',
        info: Format.relativeTime(Math.round(startDate.diff(now.startOf('day'), 'days').days), 'day', { ahead: 'from now', behind: 'ago' })
      },
      {
        date: now,
        label: 'Today',
        info: null
      },
      {
        date: deadline,
        label: 'Deadline',
        info: Format.relativeTime(Math.round(deadline.diff(now.startOf('day'), 'days').days), 'day', { ahead: 'from now', behind: 'ago' })
      },
      {
        date: estimatedCompletionDate,
        label: 'Estimated completion',
        // 13 days behind schedule, 20 days from now
        info: `${Format.relativeTime(Math.round(deadline.diff(estimatedCompletionDate, 'days').days), 'day', { ahead: 'ahead of schedule', behind: 'behind schedule' })}, ${Format.relativeTime(Math.round(estimatedCompletionDate.diff(now.startOf('day'),'days').days), 'day', { ahead: 'from now', behind: 'ago' })}`
      }
    ].sort((a, b) => a.date.diff(b.date, 'days'))}
  />
)

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

  const {
    isLoading: spiLoading,
    data: spi,
    error: spiError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'spi'],
    async () => readTeamSpi({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const {
    isLoading: cpiLoading,
    data: cpi,
    error: cpiError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'cpi'],
    async () => readTeamCpi({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const {
    isLoading: milestoneChartLoading,
    data: milestoneChart,
    error: milestoneChartError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'milestone-chart'],
    async () => readTeamMilestoneChart({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const {
    isLoading: financesLoading,
    data: finances,
    error: financeError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'finances'],
    async () => readTeamFinances({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const {
    isLoading: timelineLoading,
    data: timeline,
    error: timelineError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'timeline'],
    async () => readTeamTimeline({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const {
    isLoading: workCostChartLoading,
    data: workCostChart,
    error: workCostChartError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'work-cost-chart'],
    async () => readWorkCostChart({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const {
    isLoading: cpiChartLoading,
    data: cpiChart,
    error: cpiChartError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'cpi-chart'],
    async () => readTeamCpiChart({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const {
    isLoading: burndownChartLoading,
    data: burndownChart,
    error: burndownChartError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'burndown-chart'],
    async () => readTeamBurndownChart({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const {
    isLoading: velocityChartLoading,
    data: velocityChart,
    error: velocityChartError
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'velocity-chart'],
    async () => readTeamVelocityChart({ organizationName, projectId, teamId }),
    { retry: false }
  )

  if ([
    reportMetricsLoading,
    availableReportsLoading,
    spiLoading,
    cpiLoading,
    financesLoading,
    timelineLoading,
    cpiChartLoading,
    burndownChartLoading,
    velocityChartLoading,
    workCostChartLoading,
    milestoneChartLoading
  ].some(loading => loading === true)) {
    return <TeamDetailsSkeleton />
  }

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
        {spiError && (
          <ErrorPlaceholder
            message='Unable to display punctuality status.'
            errorCode={spiError.response.data}
            team={selectedTeam}
          />
        )}
        {!spiError && (
          <PunctualitySection {...{ spi }} />
        )}
      </div>
      {/* CPI */}
      <div className='col-span-6'>
        {cpiError && (
          <ErrorPlaceholder
            message='Unable to display budget status.'
            errorCode={cpiError.response.data}
            team={selectedTeam}
          />
        )}
        {!cpiError && (
          <BudgetSection {...{ cpi, selectedTeam }} />
        )}
      </div>
      {/* Cost Breakdown */}
      <div className='col-span-12'>
        {financeError && (
          <ErrorPlaceholder
            message='Unable to display project finance status.'
            errorCode={financeError.response.data}
            team={selectedTeam}
          />
        )}
        {!financeError && (
          <CostSection data={finances} />
        )}
      </div>
      {/* Timeline */}
      <div className='col-span-6'>
        {timelineError && (
          <ErrorPlaceholder
            message='Unable to display project timeline.'
            errorCode={timelineError.response.data}
            team={selectedTeam}
          />
        )}
        {!timelineError && (
          <TimelineSection data={timeline} />
        )}
      </div>
      {/* Recent Reports */}
      <div className='col-span-6 pt-2'>
        <ReportsList {...{ selectedTeam, availableReports, reportMetrics }} />
      </div>
      {/* Milestone Chart */}
      <div className='col-span-12'>
        {milestoneChartError && (
          <ErrorPlaceholder
            message='Unable to display remaining work/budget chart.'
            errorCode={milestoneChartError.response.data}
            team={selectedTeam}
          />
        )}
        {!milestoneChartError && (
          <MilestoneChartSection data={milestoneChart} />
        )}
      </div>
      {/* Work Cost Chart */}
      <div className='col-span-12'>
        {workCostChartError && (
          <ErrorPlaceholder
            message='Unable to display work cost chart.'
            errorCode={workCostChartError.response.data}
            team={selectedTeam}
          />
        )}
        {!workCostChartError && (
          <WorkCostChartSection data={workCostChart} />
        )}
      </div>
      {/* Burndown Chart */}
      <div className='col-span-12'>
        {burndownChartError && (
          <ErrorPlaceholder
            message='Unable to display burndown chart.'
            errorCode={burndownChartError.response.data}
            team={selectedTeam}
          />
        )}
        {!burndownChartError && (
          <BurndownChartSection data={burndownChart} />
        )}
      </div>
      {/* Velocity Chart */}
      <div className='col-span-12'>
        {velocityChartError && (
          <ErrorPlaceholder
            message='Unable to display velocity chart.'
            errorCode={velocityChartError.response.data}
            team={selectedTeam}
          />
        )}
        {!velocityChartError && (
          <VelocityChartSection data={velocityChart} />
        )}
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
    const storedTeam = teams.find(team => team.id === storedTeamId)
    
    if (storedTeam) {
      setSelectedTeam(storedTeam)
    } else {
      if (teams.length > 0) {
        setSelectedTeam(teams[0])
      } else {
        setSelectedTeam(undefined)
      }
    }
  }, [teamsLoading, teams])

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

export { DashboardPage, CpiReason, SpiReason }