import axios from "axios";
import { DateTime } from "luxon";

const mapTeamDetails = ({ 
  team: {
    deadline,
    ...team
  }, 
  ...details 
}) => ({
  ...details,
  team: {
    ...team,
    deadline: deadline ? DateTime.fromISO(deadline) : null
  }
})

const readUntrackedTeams = async ({ projectId }) => await (await axios.get(`/api/teams/untracked?projectId=${projectId}`)).data

const readTrackedTeams = async () => await (await axios.get('/api/teams/tracked')).data

const trackTeam = async ({ organizationName, projectId, teamId }) => await axios.post(`/api/teams`, { organizationName, projectId, teamId })

const readTeamDetails = async ({ organizationName, projectId, teamId }) => mapTeamDetails((await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}`)).data)

const updateTeam = async ({ organizationName, projectId, teamId, deadline }) => await axios.patch(`/api/teams/${organizationName}/${projectId}/${teamId}`, { deadline })

export {
  readUntrackedTeams,
  readTrackedTeams,
  trackTeam,
  readTeamDetails,
  updateTeam
}
