import { useCallback, useState } from "react"
import { useMutation, useQuery, useQueryClient } from "react-query"
import { readAllProjects, readAllProjects as readAllTeams } from "../api-requests/Projects"
import { readUntrackedTeams, trackTeam } from "../api-requests/Teams"
import { CheckSolid, Organization } from "../common/icons"
import { Button } from "../Components/Common/Button"
import { Spinner } from "../Components/Common/Spinner"
import { useSimpleMutation } from "../Hooks/useSimpleMutation"

const ProjectButton = ({ 
  project: {
    name: projectName,
    organization: {
      name: organizationName
    }
  },
  onClick
}) => (
  <div className={`flex items-center select-none bg-dark-2 py-3 px-6 rounded-md border border-gray-700 duration-150 ${onClick && 'cursor-pointer hover:brightness-125'}`} {...{ onClick }}>
    <span className='mr-4 font-bold flex-grow'>{projectName}</span>
    <Organization className='h-4 mr-2' />
    <span>{organizationName}</span>
  </div>
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

const ProjectSelectionSection = ({ selectProject }) => {
  const { 
    isLoading: projectsLoading,
    data: projects
  } = useQuery(['projects'], readAllProjects)

  return (
    <div>
      <div className='mb-4'>Select a project</div>
      {!projectsLoading && (
        <div className='flex flex-col gap-2'>
          {projects.map(project => (
            <ProjectButton key={project.id} {...{ project }} onClick={() => selectProject(project)} />
          ))}
        </div>
      )}
    </div>
  )
}

const TeamSelectionSection = ({ project, unselectProject }) => {
  const { data: teams, isLoading: teamsLoading } = useQuery(
    ['teams', 'untracked', project],
    async () => await readUntrackedTeams({ projectId: project.id })
  )

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
      <div className='mb-4'>Selected project</div>
      <div className='mb-6'>
        <ProjectButton {...{ project }} />
      </div>
      <div className='mb-4'>Select a team</div>
      {!teamsLoading && (
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
            <Button onClick={unselectProject}>
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

const TrackNewTeamPage = () => {
  const [project, setProject] = useState(null)

  const selectProject = useCallback((project) => {
    setProject(project)
  }, [])

  const unselectProject = useCallback(() => {
    setProject(null)
  })

  return (
    <div>
      <h1 className='mt-8 text-2xl mb-6'>Track New Team (Step {project ? '2' : '1'}/2)</h1>
      <div className='w-[36rem]'>
        {!project && (
          <ProjectSelectionSection {...{ selectProject }} />
        )}
        {project && (
          <TeamSelectionSection {...{ project, unselectProject }} />
        )}
      </div>
    </div>
  )
}

export { TrackNewTeamPage }