import axios from "axios";
import { DateTime } from "luxon";
import { mapReport } from "./mappers/Report";

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

const mapTimelineMetric = ({
  report,
  ...timelineMetric
}) => ({
  ...timelineMetric,
  report: mapReport(report),
})

const readUntrackedTeams = async ({ projectId }) => await (await axios.get(`/api/teams/untracked?projectId=${projectId}`)).data

const readTrackedTeams = async () => await (await axios.get('/api/teams/tracked')).data

const readTeamMetrics = async ({ organizationName, projectId, teamId }) => 
  (await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/metrics`)).data

const readTeamMetricsTimeline = async ({ organizationName, projectId, teamId, startDate, endDate }) => {
  const params = new URLSearchParams()
  if (startDate) params.append('startDate', startDate)
  if (endDate) params.append('endDate', endDate)
  
  const response = (await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/metrics/timeline?${params.toString()}`)).data
  return response.map(mapTimelineMetric)
}

const trackTeam = async ({ organizationName, projectId, teamId }) => await axios.post(`/api/teams`, { organizationName, projectId, teamId })

const readTeamDetails = async ({ organizationName, projectId, teamId }) => mapTeamDetails((await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}`)).data)

const updateTeam = async ({ organizationName, projectId, teamId, deadline, costPerEffort }) => await axios.patch(`/api/teams/${organizationName}/${projectId}/${teamId}`, { deadline, costPerEffort })

export {
  readUntrackedTeams,
  readTrackedTeams,
  readTeamMetrics,
  readTeamMetricsTimeline,
  trackTeam,
  readTeamDetails,
  updateTeam
}
