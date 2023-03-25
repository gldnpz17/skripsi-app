import { useQuery } from "react-query"
import { Button } from "../Components/Common/Button"
import { FormInput } from "../Components/Common/FormInput"
import { CalendarRange, HeartPulse, Information, Save } from "../common/icons"
import { useCallback, useMemo, useState } from "react"
import { DateTime } from "luxon"
import { readReportMetrics, readTimespanSprints } from "../api-requests/Reports"
import { useParams } from "react-router-dom"
import { useQueryParams } from "../Hooks/useQueryParams"
import { Format } from "../common/Format"
import { Spinner } from "../Components/Common/Spinner"

const ReportSprint = ({ 
  timespanSprint: {
    accountedEffort,
    accountedEndDate,
    accountedStartDate,
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
    <span className='bg-slate-700 rounded-full px-4 py-1 text-sm'>{accountedEffort} Effort</span>
  </div>
)

const Heading2 = ({ className, children }) => (
  <div className={`text-sm text-gray-400 font-bold mb-2 flex items-center ${className}`}>{children}</div>
)

const SprintsSection = ({ timespanSprints }) => (
  <>
    <Heading2>Sprints</Heading2>
    <div className='max-w-2xl flex flex-col gap-3'>
      {timespanSprints.map(timespanSprint => (
        <ReportSprint
          key={`${timespanSprint.sprint.startDate.toISO()}-${timespanSprint.sprint.endDate.toISO()}`}
          {...{ timespanSprint }}
        />
      ))}
    </div>
  </>
)

const DataSection = ({ expenditure, setExpenditure }) => (
  <>
    <Heading2>Report Data</Heading2>
    <FormInput
      value={expenditure}
      label='Expenditure (Rp)'
      type='number'
      onChange={(e) => setExpenditure(Number.parseInt(e.target.value))}
    />
  </>
)

const TitleSection = ({ start, end }) => {
  const { startDate, endDate } = useMemo(() => {
    return ({
      startDate: DateTime.fromISO(start),
      endDate: DateTime.fromISO(end)
    })
  }, [start, end])

  return (
    <>
      <div className='text-2xl mb-1'>Create New Report</div>
      <div className='flex gap-1 items-center mb-4'>
        <CalendarRange className='h-4' />
        <span>{Format.briefDate(startDate)} - {Format.briefDate(endDate)}</span>
      </div>
    </>
  )
}

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

const MetricGroup = ({ metricSpecs, data }) => {
  const getValue = useCallback((key) => data.cumulativeMetrics[metricSpecs.key][key], [data])
  const getDeltaValue = useCallback((key) => data.deltaMetrics[metricSpecs.key][key], data)
  const getDeltaColor = useCallback((key) => {
    const higherIsBetter = metricSpecs.metrics[key].higherIsBetter
    const value = data.deltaMetrics[metricSpecs.key][key]
    if (higherIsBetter === undefined) {
      return 'text-white'
    }

    if ((higherIsBetter && value > 0) || (!higherIsBetter && value < 0)) {
      return 'text-green-400'
    } else {
      return 'text-red-400'
    }
  }, [data])

  const formatData = (data, key) => {
    switch (metricSpecs.metrics[key].type) {
      case 'currency':
        return Format.currency(data)
      case 'number':
        return Format.number(data, 2)
    }
  }

  return (
    <>
      {Object.keys(metricSpecs.metrics).map(key => (
        <HealthMetric
          key={key}
          label={metricSpecs.metrics[key].label}
          value={formatData(getValue(key), key)}
          additionalValue={
            <span className={getDeltaColor(key)}>
              &nbsp;{getDeltaValue(key) > 0 ? '+' : '-'} {formatData(Math.abs(getDeltaValue(key)), key)}
            </span>
          }
        />
      ))}
    </>
  )
}

const ActionSection = () => (
  <div className='flex justify-end max-w-2xl'>
    <Button>
      <Save className='h-4' />
      Save
    </Button>
  </div>
)

const MetricsSection = ({ organizationName, projectId, teamId, start, end, expenditure }) => {
  const {
    isLoading,
    isError,
    error,
    data
  } = useQuery(
    ['report', organizationName, projectId, teamId, 'new-report-metrics', start, end, expenditure],
    async () => await readReportMetrics({ organizationName, projectId, teamId, start, end, expenditure })
  )

  const basicMetrics = {
    key: 'basicMetrics',
    metrics: {
      'plannedValue': {
        label: 'Planned Value',
        type: 'currency'
      },
      'earnedValue': {
        label: 'Earned Value',
        type: 'currency'
      },
      'actualCost': {
        label: 'Actual Cost',
        type: 'currency'
      }
    }
  }

  const healthMetrics = {
    key: 'healthMetrics',
    metrics: {
      'costVariance': {
        label: 'Cost Variance',
        type: 'currency',
        higherIsBetter: true
      },
      'scheduleVariance': {
        label: 'Schedule Variance',
        type: 'currency',
        higherIsBetter: true
      },
      'costPerformanceIndex': {
        label: 'Cost Performance Index',
        type: 'number',
        higherIsBetter: true
      },
      'schedulePerformanceIndex': {
        label: 'Schedule Performance Index',
        type: 'number',
        higherIsBetter: true
      }
    }
  }

  const forecastMetrics = {
    key: 'forecastMetrics',
    metrics: {
      'estimateToCompletion': {
        label: 'Estimate to Completion',
        type: 'currency',
        higherIsBetter: false
      },
      'estimateAtCompletion': {
        label: 'Estimate at Completion',
        type: 'currency',
        higherIsBetter: false
      },
      'varianceAtCompletion': {
        label: 'Variance at Completion',
        type: 'currency',
        higherIsBetter: true
      }
    }
  }

  return (
    <div className='bg-dark-2 p-4 rounded-md border border-gray-700 w-72'>
      <Heading2 className='mb-3'>
        <HeartPulse className='h-3 mr-1 text-primary-light' />
        Project Health Metrics
      </Heading2>
      {isLoading && (
        <div className='w-full flex items-center justify-center mt-4'>
          <Spinner className='h-6 text-primary-dark mr-2' />
          <div>Calculating</div>
        </div>
      )}
      {isError && (
        <div>{Format.error(error.response.data).message}</div>
      )}
      {!isLoading && !isError && (
        <div className='flex flex-col gap-2'>
          <MetricGroup metricSpecs={basicMetrics} {...{ data }} />
          <hr className='border-gray-700 my-2' />
          <MetricGroup metricSpecs={healthMetrics} {...{ data }} />
          <hr className='border-gray-700 my-2' />
          <MetricGroup metricSpecs={forecastMetrics} {...{ data }} />
        </div>
      )}
    </div>
  )
}

const NewReportPage = () => {
  const { organizationName, projectId, teamId } = useParams()
  const { start, end } = useQueryParams()

  const [expenditure, setExpenditure] = useState(0)

  const {
    isLoading: sprintsLoading,
    data: timespanSprints
  } = useQuery(
    ['report', organizationName, projectId, teamId, 'timespan-sprints', start, end],
    async () => await readTimespanSprints({ organizationName, projectId, teamId, start, end })
  )

  if (sprintsLoading) return <></>

  return (
    <div className='py-8 pr-6 flex min-h-full'>
      <div className='flex-grow flex flex-col'>
        <div>
          <TitleSection {...{ start, end }} />
        </div>
        <div className='mb-6'>
          <SprintsSection {...{ timespanSprints }} />
        </div>
        <div>
          <DataSection {...{ expenditure, setExpenditure }} />
        </div>
        <div className='flex-grow' />
        <div>
          <ActionSection />
        </div>
      </div>
      <MetricsSection {...{ organizationName, projectId, teamId, start, end, expenditure }} />
    </div>
  )
}

export { NewReportPage }