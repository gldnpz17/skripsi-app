import { useMutation, useQuery } from "react-query"
import { IconButton } from "../Components/Common/IconButton"
import { BlankReportItem, ReportItem } from "../Components/Common/ReportItem"
import { Format } from "../common/Format"
import { Archive, Edit, Warning } from "../common/icons"
import { useParams } from "react-router-dom"
import { deleteTeam, readTeamDetails, updateTeam } from "../api-requests/Teams"
import { useSimpleMutation } from "../Hooks/useSimpleMutation"
import { useCallback, useEffect, useMemo, useReducer, useState } from "react"
import { DateTime } from "luxon"
import { FormInput } from "../Components/Common/FormInput"
import { readAvailableReports, readTeamReports } from "../api-requests/Reports"
import { ErrorPlaceholder } from "../Components/Common/ErrorPlaceholder"
import { Button } from "../Components/Common/Button"
import { withAuth } from "../HigherOrderComponents/withAuth"
import { MathJax } from "better-react-mathjax"

const SectionTitle = ({ children }) => (
  <div className='text-sm items-center font-bold text-gray-400 mb-4'>
    {children}
  </div>
)

const ReportsSection = ({ reportMetrics, availableReports, selectedTeam }) => (
  <div className='mb-10'>
    <SectionTitle>Monthly Reports</SectionTitle>
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
          {...{ reportMetric }} 
        />
      ))}
    </div>
  </div>
)

const DangerSection = ({ team, details }) => {
  const { 
    mutateAsync: updateTeamAsync
  } = useSimpleMutation(updateTeam, [['teams']])

  const { 
    mutateAsync: deleteTeamAsync
  } = useSimpleMutation(deleteTeam, [['teams']])

  const handleDelete = useCallback(async () => {
    const reply = window.prompt(`Type '${details.team.name}' to confirm deletion.`)
    if (!reply || reply !== details.team.name) return

    await deleteTeamAsync(() => ({
      organizationName: team.organization.name,
      projectId: team.project.id,
      teamId: team.id,
    }))()

    window.close()
  }, [team])

  return (
    <div className='w-full border-red-500 border rounded-md p-6 bg-dark-2 mb-10'>
      <div className='text-red-500 flex gap-2 items-center mb-4'>
        <Warning className='h-6' />
        <div className='font-bold'>Danger Zone</div>
      </div>
      <div className='flex gap-6 max-w-lg items-center mb-4'>
        <Button 
          onClick={updateTeamAsync(() => ({
            organizationName: team.organization.name,
            projectId: team.project.id,
            teamId: team.id,
            archived: true
          }))}
          className='w-20 bg-yellow-500 hover:bg-yellow-300'
        >
          Archive
        </Button>
        <div className='text-gray-300'>
          The team will be hidden from the dashboard and you can't view it unless it's restored.
        </div>
      </div>
      <div className='flex gap-6 max-w-lg items-center'>
        <Button onClick={handleDelete} className='w-20 bg-red-500 hover:bg-red-300'>Delete</Button>
        <div className='text-gray-300'>
          The team will be deleted from the database. This action is <b>irreversible</b>.
        </div>
      </div>
    </div>
  )
}

const RadioGroup = ({ label, options=[], value, onChange }) => {
  const getInputProps = useCallback((inputValue) => ({
    onChange: (e) => {
      const checked = e.target.checked
      if (checked) onChange(inputValue)
    },
    checked: value === inputValue
  }), [onChange, value])

  return (
    <div className='flex w-full'>
      <div className='w-60 mr-4'>{label}</div>
      <fieldset className='flex-grow'>
        {options.map(({ value, label, information }) => (
          <div key={value} className="">
            <label className='flex items-center'>
              <input type='radio' className='mr-1' {...getInputProps(value)} />
              {label}
            </label>
            {information}
          </div>
        ))}
      </fieldset>
    </div>
  )
}

const GeneralSection = ({ team }) => {
  const { 
    mutateAsync: updateTeamAsync
  } = useSimpleMutation(updateTeam, [['teams']])

  const [eac, setEac] = useState(team.eacFormula)

  useEffect(() => {
    updateTeamAsync(() => ({
      organizationName: team.organization.name,
      projectId: team.project.id,
      teamId: team.id,
      eacFormula: eac
    }))()
  }, [eac])

  return (
    <div className='mb-10'>
      <SectionTitle>General</SectionTitle>
      <div className='flex flex-col gap-4'>
        <FormInput
          label='Team project deadline'
          type='date'
          labelClassName='w-60'
          defaultValue={team.deadline?.toISODate()}
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
          labelClassName='w-60'
          defaultValue={team.costPerEffort}
          onChange={updateTeamAsync(({ target }) => ({
            organizationName: team.organization.name,
            projectId: team.project.id,
            teamId: team.id,
            costPerEffort: Number.parseInt(target.value)
          }))}
        />
        <RadioGroup
          label='EAC Formula'
          options={[
            {
              value: 'Typical',
              label: 'Typical',
              information: (
                <div className='border border-gray-700 rounded-md bg-dark-2 p-4 mt-2 mb-4'>
                  <div className='text-sm'>Use this when cost performance is relatively steady. (refer to the CPI chart in the dashboard)</div>
                  <MathJax>{`\\[EAC = \\frac{BAC}{CPI}\\]`}</MathJax>
                  <div className='flex'>
                    <div className='text-sm'>
                      <div><span className='text-primary-dark font-semibold'>EAC</span> = Estimate at Completion (Estimated cost to complete the project)</div>
                      <div><span className='text-primary-dark font-semibold'>BAC</span> = Budget at Completion (Total project budget)</div>
                      <div><span className='text-primary-dark font-semibold'>CPI</span> = Cost Performance Index (How efficient our spendings are)</div>
                    </div>
                  </div>
                </div>
              )
            },
            {
              value: 'Atypical',
              label: 'Atypical',
              information: (
                <div className='border border-gray-700 rounded-md bg-dark-2 p-4 mt-2 mb-4'>
                  <div className='text-sm'>Use this when there are financial issues. (refer to the CPI chart in the dashboard)</div>
                  <MathJax>{`\\[EAC = BAC + (AC - EV)\\]`}</MathJax>
                  <div className='flex'>
                    <div className='text-sm'>
                      <div><span className='text-primary-dark font-semibold'>EAC</span> = Estimate at Completion (Estimated cost to complete the project)</div>
                      <div><span className='text-primary-dark font-semibold'>AC</span> = Actual Cost (Total amount of money spent)</div>
                      <div><span className='text-primary-dark font-semibold'>BAC</span> = Budget at Completion (Total project budget)</div>
                      <div><span className='text-primary-dark font-semibold'>EV</span> = Earned Value (Value of work done)</div>
                    </div>
                  </div>
                </div>
              )
            }
          ]}
          value={eac}
          onChange={(data) => setEac(data)}
        />
      </div>
    </div>
  )
}

const ArchivedSection = ({ team }) => {
  const { 
    mutateAsync: updateTeamAsync
  } = useSimpleMutation(updateTeam, [['teams']])

  return (
    <div className='flex gap-2 border border-gray-700 rounded-md bg-dark-2 px-6 py-4 items-center'>
      <Archive className='h-6' />
      <div className='flex-grow'>This team is archived</div>
      <Button
        onClick={updateTeamAsync(() => ({
          organizationName: team.organization.name,
          projectId: team.project.id,
          teamId: team.id,
          archived: false
        }))}
      >
        Restore
      </Button>
    </div>
  )
}

const Page = () => {
  const { organizationName, projectId, teamId } = useParams()

  const { data: details, isLoading: detailsLoading } = useQuery(
    ['teams', organizationName, projectId, teamId],
    async () => await readTeamDetails({ organizationName, projectId, teamId })
  )

  const {
    isLoading: reportsLoading,
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
    error: availableReportsError,
  } = useQuery(
    ['teams', organizationName, projectId, teamId, 'available-reports'],
    async () => await readAvailableReports({ organizationName, projectId, teamId }),
    { retry: false }
  )

  const team = {
    id: teamId,
    organization: {
      name: organizationName,
    },
    project: {
      id: projectId
    }
  }

  const reportsError = useMemo(() => {
    return reportMetricsError ?? availableReportsError ?? null
  }, [reportMetricsError, availableReportsError])

  return (
    <div className='pr-80 pt-8 h-full overflow-auto'>
      {!detailsLoading && (
        <>
          <h1 className='text-2xl mb-6'>Team Settings - {details.team.name}</h1>
          {details.team.archived && (
            <ArchivedSection {...{ team }} />
          )}
          {!details.team.archived && (
            <GeneralSection team={details.team} />
          )}
        </>
      )}
      {!reportsLoading && !availableReportsLoading && (
        <>
          {reportsError && !details.team.archived && (
            <ErrorPlaceholder
              className='col-span-12 mb-10'
              message='Unable to display report list.'
              errorCode={reportsError.response.data}
              {...{ team }}
            />
          )}
          {!reportsError && !details.team.archived && (
            <section id='reports'>
              <ReportsSection selectedTeam={team} {...{ availableReports, reportMetrics }} />
            </section>
          )}
        </>
      )}
      {!detailsLoading && (
        <>
          {!details.team.archived && (
            <DangerSection {...{ team, details }} />
          )}
        </>
      )}
    </div>
  )
}

const TeamDetailsPage = withAuth(Page)

export { TeamDetailsPage }