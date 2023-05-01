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
          The team will be hidden from the dashboard and you can't view or edit it unless it's restored.
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
      <div className='flex-grow'>{label}</div>
      <fieldset className='w-48'>
          {options.map(({ value, label }) => (
            <div key={value} >
              <label className='flex items-center'>
                <input type='radio' className='mr-1' {...getInputProps(value)} />
                {label}
              </label>
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

  const reducer = (state, { field, data }) => {
    const newState = {...state}

    if (field === 'eac' && data === 'Derived' && state.etc === 'Derived') {
      newState.etc = 'Typical'
    }
    if (field === 'etc' && data === 'Derived' && state.eac === 'Derived') {
      newState.eac = 'Basic'
    }
    newState[field] = data
    return newState
  }
  const [{ etc, eac }, dispatch] = useReducer(reducer, { eac: team.eacFormula, etc: team.etcFormula })

  useEffect(() => {
    updateTeamAsync(() => ({
      organizationName: team.organization.name,
      projectId: team.project.id,
      teamId: team.id,
      eacFormula: eac,
      etcFormula: etc
    }))()
  }, [eac, etc])

  return (
    <div className='mb-10'>
      <SectionTitle>General</SectionTitle>
      <div className='flex flex-col gap-4 max-w-md'>
        <FormInput
          label='Team project deadline'
          type='date'
          stretch
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
          stretch
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
            { value: 'Derived', label: 'Derived' },
            { value: 'Basic', label: 'Basic' },
            { value: 'Typical', label: 'Typical' },
            { value: 'Atypical', label: 'Atypical' }
          ]}
          value={eac}
          onChange={(data) => dispatch({ field: 'eac', data })}
        />
        <RadioGroup
          label='ETC Formula'
          options={[
            { value: 'Derived', label: 'Derived' },
            { value: 'Typical', label: 'Typical' },
            { value: 'Atypical', label: 'Atypical' }
          ]}
          value={etc}
          onChange={(data) => dispatch({ field: 'etc', data })}
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

const TeamDetailsPage = () => {
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
            <ReportsSection selectedTeam={team} {...{ availableReports, reportMetrics }} />
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

export { TeamDetailsPage }