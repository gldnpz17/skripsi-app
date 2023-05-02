import { useCallback, useMemo, useState } from "react"
import { useMutation, useQuery, useQueryClient } from "react-query"
import { readProjectsByOrganization } from "../api-requests/Projects"
import { readUntrackedTeams, trackTeam } from "../api-requests/Teams"
import { CheckSolid, Organization } from "../common/icons"
import { Button } from "../Components/Common/Button"
import { Spinner } from "../Components/Common/Spinner"
import { useSimpleMutation } from "../Hooks/useSimpleMutation"
import { withAuth } from "../HigherOrderComponents/withAuth"
import { readAllOrganizations } from "../api-requests/Organizations"
import { FormInput, FormInputSimple } from "../Components/Common/FormInput"

const useSearch = (data, getProperty) => {
  const [keyword, setKeyword] = useState('')

  const filteredData = useMemo(() => {
    if (!data) return null

    return data.filter(datum => getProperty(datum).includes(keyword))
  }, [data, getProperty, keyword])

  return [filteredData, setKeyword]
}

const OptionButton = ({ label, onClick }) => (
  <div className={`flex items-center select-none bg-dark-2 py-3 px-6 rounded-md border border-gray-700 duration-150 ${onClick && 'cursor-pointer hover:brightness-125'}`} {...{ onClick }}>
    <span className='mr-4 font-bold flex-grow'>{label}</span>
  </div>
)

const OrganizationButton = ({ organization: { name }, onClick }) => (
  <OptionButton label={name} {...{ onClick }} />
)

const ProjectButton = ({ project: { name }, onClick }) => (
  <OptionButton label={name} {...{ onClick }} />
)

const TeamButton = ({ team: { name }, selected, onClick }) => (
  <div 
    className={`flex items-center select-none bg-dark-2 py-3 px-6 rounded-md border border-gray-700 duration-150 cursor-pointer hover:brightness-125 ${selected && 'brightness-125'}`}
    {...{ onClick }}
  >
    <span className='flex-grow'>{name}</span>
    <CheckSolid className={`h-6 text-primary-dark opacity-0 duration-150 ${selected && 'opacity-100'}`} />
  </div>
)

const OrganizationSelectionSection = ({ setOrganization }) => {
  const { isLoading, data } = useQuery(['organizations'], readAllOrganizations)
  const [organizations, setKeyword] = useSearch(data, (organization) => organization.name)

  return (
    <div>
      <div className='flex mb-4 items-center'>
        <div className='flex-grow'>Select an organization</div>
        <FormInputSimple
          type='text'
          label='Search'
          className='w-full'
          onChange={(e) => setKeyword(e.target.value)}
        />
      </div>
      {!isLoading && organizations.length === 0 && (
        <div className='text-gray-500 text-center'>No organizations to select from</div>
      )}
      {!isLoading && (
        <div className='flex flex-col gap-2'>
          {organizations.map(organization => (
            <OrganizationButton
              key={organization.name}
              onClick={() => setOrganization(organization)}
              {...{ organization }}
            />
          ))}
        </div>
      )}
    </div>
  )
}

const ProjectSelectionSection = ({ organization, setProject, setOrganization }) => {
  const { isLoading, data } = useQuery(['projects'], async () => readProjectsByOrganization({ organizationName: organization.name }))
  const [projects, setKeyword] = useSearch(data, (project) => project.name)

  return (
    <div>
      {!isLoading && (
        <div>
          <div className='mb-4'>Select a project</div>
          <div className='mb-2 text-sm'>Selected organization</div>
          <div className='mb-6'>
            <OrganizationButton {...{ organization }} />
          </div>
          <div className='flex mb-4 items-center'>
            <div className='flex-grow'>Select a project</div>
            <FormInputSimple
              type='text'
              label='Search'
              className='w-full'
              onChange={(e) => setKeyword(e.target.value)}
            />
          </div>
          {projects.length === 0 && (
            <div className='text-gray-500 text-center'>No projects to select from</div>
          )}
          <div className='flex flex-col gap-2 mb-8'>
            {projects.map(project => (
              <ProjectButton
                key={project.id}
                onClick={() => setProject(project)}
                {...{ project }} 
              />
            ))}
          </div>
          <div>
            <Button onClick={() => setOrganization(null)}>Back</Button>
          </div>
        </div>
      )}
    </div>
  )
}

const TeamSelectionSection = ({ organization, project, setProject }) => {
  const { data, isLoading } = useQuery(
    ['teams', 'untracked', project],
    async () => await readUntrackedTeams({ projectId: project.id })
  )
  const [teams, setKeyword] = useSearch(data, (project) => project.name)

  const [selectedTeam, setSelectedTeam] = useState(null)

  const {
    isLoading: trackTeamLoading,
    mutateAsync: trackTeamMutation
  } = useSimpleMutation(trackTeam, [['teams']])

  const selectTeam = useCallback((buttonTeam) => () => {
    if (buttonTeam.id === selectedTeam?.id) {
      setSelectedTeam(null)
      return
    }

    setSelectedTeam(buttonTeam)
  }, [selectedTeam, setSelectedTeam])

  return (
    <div>
      <div className='mb-2 text-sm'>Selected organization</div>
      <div className='mb-3'>
        <OrganizationButton {...{ organization }} />
      </div>
      <div className='mb-2 text-sm'>Selected project</div>
      <div className='mb-6'>
        <ProjectButton {...{ project }} />
      </div>
      <div className='flex mb-4 items-center'>
        <div className='flex-grow'>Select a team</div>
        <FormInputSimple
          type='text'
          label='Search'
          className='w-full'
          onChange={(e) => setKeyword(e.target.value)}
        />
      </div>
      {!isLoading && teams.length === 0 && (
        <div className='text-gray-500 text-center'>No teams to select from</div>
      )}
      {!isLoading && (
        <>
          <div className='mb-8 flex flex-col gap-2'>
            {teams.map(team => (
              <TeamButton
                key={team.id}
                selected={team.id === selectedTeam?.id}
                onClick={selectTeam(team)}
                {...{ team }}
              />
            ))}
          </div>
          <div className='flex justify-between'>
            <Button onClick={() => setProject(null)}>
              Back
            </Button>
            <Button
              disabled={!selectedTeam}
              onClick={trackTeamMutation(() => ({
                organizationName: selectedTeam.organization.name,
                projectId: selectedTeam.project.id,
                teamId: selectedTeam.id
              }))}
            >
              {trackTeamLoading && (
                <Spinner className='h-4' />
              )}
              Track Team
            </Button>
          </div>
        </>
      )}
    </div>
  )
}

const Page = () => {
  const [organization, setOrganization] = useState(null)
  const [project, setProject] = useState(null)

  return (
    <div>
      <h1 className='mt-8 text-2xl mb-6'>Track New Team (Step {!organization ? '1' : !project ? '2' : '3'}/3)</h1>
      <div className='w-[36rem]'>
        {!organization && (
          <OrganizationSelectionSection {...{ setOrganization }} />
        )}
        {!project && organization && (
          <ProjectSelectionSection {...{ organization, setProject, setOrganization }} />
        )}
        {project && (
          <TeamSelectionSection {...{ organization, project, setProject }} />
        )}
      </div>
    </div>
  )
}

const TrackNewTeamPage = withAuth(Page)

export { TrackNewTeamPage }