import axios from "axios";

const readUntrackedTeams = async ({ projectId }) => await (await axios.get(`/api/teams/untracked?projectId=${projectId}`)).data

const readTrackedTeams = async () => await (await axios.get('/api/teams/tracked')).data

const trackTeam = async ({ organizationName, projectId, teamId }) => await axios.post(`/api/teams`, { organizationName, projectId, teamId })

export {
  readUntrackedTeams,
  readTrackedTeams,
  trackTeam
}
