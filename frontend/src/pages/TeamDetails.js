import { useMutation, useQuery } from "react-query"
import { IconButton } from "../Components/Common/IconButton"
import { BlankReportItem, ReportItem } from "../Components/Common/ReportItem"
import { Format } from "../common/Format"
import { Edit } from "../common/icons"
import { useParams } from "react-router-dom"
import { readTeamDetails, updateTeam } from "../api-requests/Teams"
import { useSimpleMutation } from "../Hooks/useSimpleMutation"
import { useCallback } from "react"
import { DateTime } from "luxon"

const REPORTS = [
  { date: new Date(), velocity: 33, expenditureRate: 3300000, status: 'critical' },
  { date: new Date(), velocity: 23, expenditureRate: 3100000, status: 'atRisk' },
  { date: new Date(), velocity: 36, expenditureRate: 3600000, status: 'critical' },
  { date: new Date(), velocity: 38, expenditureRate: 3700000, status: 'atRisk' },
  { date: new Date(), velocity: 45, expenditureRate: 3200000, status: 'healthy' }
]

const SectionTitle = ({ children }) => (
  <div className='text-sm items-center font-bold text-gray-400 mb-4'>
    {children}
  </div>
)

const ReportsSection = ({ reports }) => (
  <div>
    <SectionTitle>Monthly Reports</SectionTitle>
    <div className='flex flex-col gap-4'>
      <BlankReportItem date={new Date()} />
      {reports.map(report => (
        <ReportItem key={report.date} {...{ report }} />
      ))}
    </div>
  </div>
)

const GeneralSection = ({ team }) => {
  const { 
    mutateAsync: updateTeamAsync,
    isLoading: updateTeamLoading
  } = useSimpleMutation(updateTeam, [['teams']])

  return (
    <div className='mb-10'>
      <SectionTitle>General</SectionTitle>
      <div>
        <span className='mr-4'>Team project deadline</span>
        <input type='date' className='rounded-md border-2 text-black px-2 py-1 border-secondary-dark bg-purple-100 w-48'
          value={team.deadline.toISODate()}
          onChange={updateTeamAsync(({ target }) => ({
            organizationName: team.organization.name,
            projectId: team.project.id,
            teamId: team.id,
            deadline: DateTime.fromISO(target.value).endOf('day').toISO() 
          }))}
        />
      </div>
    </div>
  )
}

const TeamDetailsPage = () => {
  const { organizationName, projectId, teamId } = useParams()

  const { data: details, isLoading: detailsLoading } = useQuery(
    ['teams', organizationName, projectId, teamId],
    async () => await readTeamDetails({ organizationName, projectId, teamId })
  )

  return (
    <div className='pr-80 pt-8 h-full overflow-auto'>
      {!detailsLoading && (
        <>
          <GeneralSection team={details.team} />
          <ReportsSection reports={REPORTS} />
        </>
      )}
    </div>
  )
}

export { TeamDetailsPage }