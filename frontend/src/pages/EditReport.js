import { useQuery } from "react-query"
import { Button } from "../Components/Common/Button"
import { Save } from "../common/icons"
import { useCallback, useEffect, useState } from "react"
import { createReport, deleteReport, readReportById, readTimespanSprints, updateReport } from "../api-requests/Reports"
import { useParams } from "react-router-dom"
import { useQueryParams } from "../Hooks/useQueryParams"
import { useSimpleMutation } from "../Hooks/useSimpleMutation"
import { TitleSection } from "../Components/ReportCommon/TitleSection"
import { SprintsSection } from "../Components/ReportCommon/SprintSection"
import { DataSection } from "../Components/ReportCommon/DataSection"
import { withAuth } from "../HigherOrderComponents/withAuth"

const ActionSection = ({ reportId, expenditure }) => {
  const onSuccess = () => window.close()

  const { mutateAsync: updateAsync } = useSimpleMutation(updateReport, [['teams', 'report']], { onSuccess })
  const { mutateAsync: deleteAsync } = useSimpleMutation(deleteReport, [['teams', 'report']], { onSuccess })

  const handleDelete = () => {
    const confirmed = window.confirm('Delete this report?')

    if (confirmed) {
      deleteAsync(() => ({ reportId }))()
    }
  }

  return (
    <div className='flex max-w-2xl'>
      <Button overrideColor onClick={handleDelete} className='bg-red-500 hover:bg-red-300'>
        Delete
      </Button>
      <div className='flex-grow'></div>
      <Button
        onClick={updateAsync(() => ({
          id: reportId,
          report: {
            expenditure
          }
        }))}
      >
        <Save className='h-4' />
        Save
      </Button>
    </div>
  )
}

const Page = () => {
  const { organizationName, projectId, teamId, reportId } = useParams()

  const [{ start, end, expenditure }, setState] = useState({
    start: null,
    end: null,
    expenditure: 0
  })

  const setExpenditure = useCallback((expenditure) => setState({ start, end, expenditure }), [start, end])

  const {
    isLoading: reportLoading, 
    data: report
  } = useQuery(['report', reportId], async () => await readReportById({ id: reportId }))

  useEffect(() => {
    if (!reportLoading && report) {
      setState({
        start: report.startDate.toISO(),
        end: report.endDate.toISO(),
        expenditure: report.expenditure
      })
    }
  }, [reportLoading, report])

  const {
    isLoading: sprintsLoading,
    data: timespanSprints
  } = useQuery(
    ['report', organizationName, projectId, teamId, 'timespan-sprints', start, end],
    async () => await readTimespanSprints({ organizationName, projectId, teamId, start, end })
  )

  if (sprintsLoading) return <></>

  return (
    <div className='py-8 pr-6 flex min-h-full relative'>
      <div className='flex-grow flex flex-col'>
        <div>
          <TitleSection title='Edit Report' {...{ start, end }} />
        </div>
        <div className='mb-6'>
          <SprintsSection {...{ timespanSprints }} />
        </div>
        <div>
          <DataSection {...{ expenditure, setExpenditure }} />
        </div>
        <div className='flex-grow' />
        <div>
          <ActionSection {...{ reportId, expenditure }} />
        </div>
      </div>
    </div>
  )
}

const EditReportPage = withAuth(Page)

export { EditReportPage }