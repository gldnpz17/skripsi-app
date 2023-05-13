import { useQuery } from "react-query"
import { Button } from "../Components/Common/Button"
import { FormInput } from "../Components/Common/FormInput"
import { CalendarRange, HeartPulse, Information, Save } from "../common/icons"
import { useCallback, useEffect, useMemo, useState } from "react"
import { DateTime } from "luxon"
import { createReport, readReportById, readReportMetrics, readTimespanSprints } from "../api-requests/Reports"
import { useParams } from "react-router-dom"
import { useQueryParams } from "../Hooks/useQueryParams"
import { Format } from "../common/Format"
import { Spinner } from "../Components/Common/Spinner"
import { useSimpleMutation } from "../Hooks/useSimpleMutation"
import { TitleSection } from "../Components/ReportCommon/TitleSection"
import { SprintsSection } from "../Components/ReportCommon/SprintSection"
import { DataSection } from "../Components/ReportCommon/DataSection"
import { withAuth } from "../HigherOrderComponents/withAuth"

const ActionSection = ({ organizationName, projectId, teamId, start, end, expenditure }) => {
  const onSuccess = () => window.close()

  const { mutateAsync } = useSimpleMutation(createReport, [['teams']], { onSuccess })

  return (
    <div className='flex justify-end max-w-2xl'>
      <Button
        onClick={mutateAsync(() => ({
          organizationName,
          projectId,
          teamId,
          report: {
            startDate: start,
            endDate: end,
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
          <TitleSection title='Create New Report' {...{ start, end }} />
        </div>
        <div className='mb-6'>
          <SprintsSection {...{ timespanSprints }} />
        </div>
        <div>
          <DataSection {...{ expenditure, setExpenditure }} />
        </div>
        <div className='flex-grow' />
        <div>
          <ActionSection {...{ organizationName, projectId, teamId, start, end, expenditure }} />
        </div>
      </div>
    </div>
  )
}

const NewReportPage = withAuth(Page)

export { NewReportPage }