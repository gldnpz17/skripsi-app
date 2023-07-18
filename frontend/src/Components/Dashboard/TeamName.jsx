import { useMemo } from "react"
import { AzureDevops } from "../../common/icons"
import { Button } from "../Common/Button"
import { ExternalLink } from "../Common/ExternalLink"

const TeamName = ({ team }) => {
  const organizationName = team.organization.name
  const projectId = team.project.id
  const projectName = team.project.name
  const teamId = team.id
  const teamName = team.name

  const azureDevopsUrl = useMemo(() => {
    return `https://dev.azure.com/${encodeURIComponent(organizationName)}/${encodeURIComponent(projectName)}/_boards/board/t/${encodeURIComponent(teamName)}/Backlog%20items`
  }, [organizationName, projectName, teamName])

  return (
    <div className='flex items-center mb-4'>
      <div className='flex-grow'>
        <div className='text-2xl'>{teamName} - {projectName}</div>
        <ExternalLink to={`/teams/${organizationName}/${projectId}/${teamId}`}>
          Team Settings
        </ExternalLink>
      </div>
      <a href={azureDevopsUrl} target='_blank'>
        <Button className='shadow-xl'>
          <AzureDevops className='h-4' />
          Azure DevOps Board
        </Button>
      </a>
    </div>
  )
}

export { TeamName }