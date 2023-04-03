import { useMutation, useQuery } from "react-query"
import { IconButton } from "../Components/Common/IconButton"
import { BlankReportItem, ReportItem } from "../Components/Common/ReportItem"
import { Format } from "../common/Format"
import { Edit } from "../common/icons"
import { useParams } from "react-router-dom"
import { readTeamDetails, updateTeam } from "../api-requests/Teams"
import { useSimpleMutation } from "../Hooks/useSimpleMutation"
import { useCallback, useEffect, useReducer, useState } from "react"
import { DateTime } from "luxon"
import { FormInput } from "../Components/Common/FormInput"
import { readAvailableReports, readTeamReports } from "../api-requests/Reports"

const SectionTitle = ({ children }) => (
  <div className='text-sm items-center font-bold text-gray-400 mb-4'>
    {children}
  </div>
)

const ReportsSection = ({ reportMetrics, availableReports, selectedTeam }) => (
  <div>
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
    mutateAsync: updateTeamAsync,
    isLoading: updateTeamLoading
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

const TeamDetailsPage = () => {
  const { organizationName, projectId, teamId } = useParams()

  const { data: details, isLoading: detailsLoading } = useQuery(
    ['teams', organizationName, projectId, teamId],
    async () => await readTeamDetails({ organizationName, projectId, teamId })
  )

  const {
    isLoading: reportsLoading,
    data: reportMetrics
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

  const team = {
    id: teamId,
    organization: {
      name: organizationName,
    },
    project: {
      id: projectId
    }
  }

  return (
    <div className='pr-80 pt-8 h-full overflow-auto'>
      {!detailsLoading && !reportsLoading && !availableReportsLoading && (
        <>
          <GeneralSection team={details.team} />
          <ReportsSection selectedTeam={team} {...{ availableReports, reportMetrics }} />
        </>
      )}
    </div>
  )
}

export { TeamDetailsPage }