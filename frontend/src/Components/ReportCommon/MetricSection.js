import { useQuery } from "react-query"
import { readReportMetrics } from "../../api-requests/Reports"
import { Heading2 } from "../Common/Headings"
import { HeartPulse } from "../../common/icons"
import { Spinner } from "../Common/Spinner"
import { MetricGroup } from "./MetricGroup"
import { Format } from "../../common/Format"

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

export { MetricsSection }