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
import { FormInput } from "../Components/Common/FormInput"
import { readAvailableReports, readTeamReports } from "../api-requests/Reports"

const SectionTitle = ({ children }) => (
  <div className='text-sm items-center font-bold text-gray-400 mb-4'>
    {children}
  </div>
)

const ReportsSection = ({ reports, availableReports, selectedTeam }) => (
  <div>
    <SectionTitle>Monthly Reports</SectionTitle>
    <div className='flex flex-col gap-4'>
      {availableReports?.map(report => (
        <BlankReportItem key={Format.reportKey(report)} {...{ report, selectedTeam }} />
      ))}
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
      <div className='flex flex-col gap-4'>
        <FormInput
          label='Team project deadline'
          type='date'
          value={team.deadline?.toISODate()}
          onChange={updateTeamAsync(({ target }) => ({
            organizationName: team.organization.name,
            projectId: team.project.id,
            teamId: team.id,
            deadline: DateTime.fromISO(target.value).endOf('day').toISO() 
          }))}
        />
        <FormInput
          label='Cost per Effort'
          type='number'
          value={team.costPerEffort}
          onChange={updateTeamAsync(({ target }) => ({
            organizationName: team.organization.name,
            projectId: team.project.id,
            teamId: team.id,
            costPerEffort: Number.parseInt(target.value)
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

  const {
    isLoading: reportsLoading,
    data: reports
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

  return (
    <div className='pr-80 pt-8 h-full overflow-auto'>
      {!detailsLoading && !reportsLoading && !availableReportsLoading && (
        <>
          <GeneralSection team={details.team} />
          <ReportsSection selectedTeam={{ organizationName, projectId, teamId }} {...{ availableReports, reports }} />
        </>
      )}
    </div>
  )
}

export { TeamDetailsPage }