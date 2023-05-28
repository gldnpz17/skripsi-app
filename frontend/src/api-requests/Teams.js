import axios from "axios";
import { DateTime } from "luxon";
import { mapReport } from "./mappers/Report";

const mapTeam = ({
  deadline,
  ...team
}) => ({
  ...team,
  deadline: deadline ? DateTime.fromISO(deadline) : null
})

const mapTeamDetails = ({ 
  team,
  ...details 
}) => ({
  ...details,
  team: mapTeam(team)
})

const mapTeamTimeline = ({
  startDate,
  deadline,
  now,
  estimatedCompletionDate
}) => ({
  startDate: DateTime.fromISO(startDate),
  deadline: DateTime.fromISO(deadline),
  now: DateTime.fromISO(now),
  estimatedCompletionDate: DateTime.fromISO(estimatedCompletionDate)
})

const mapCpiChartItem = ({
  month,
  ...item
}) => ({
  ...item,
  month: DateTime.fromISO(month)
})

const mapWorkCostChartItem = ({
  month,
  ...item
}) => ({
  ...item,
  month: DateTime.fromISO(month)
})

const mapBurndownChartItem = ({
  date,
  ...item
}) => ({
  ...item,
  date: DateTime.fromISO(date)
})

const mapMilestoneChartItem = ({
  month,
  ...item
}) => ({
  ...item,
  month: DateTime.fromISO(month)
})

const mapBurndownChart = ({
  startDate,
  endDate,
  items,
  ...chart
}) => ({
  ...chart,
  startDate: DateTime.fromISO(startDate),
  endDate: DateTime.fromISO(endDate),
  items: items.map(mapBurndownChartItem)
})

const readUntrackedTeams = async ({ projectId }) => await (await axios.get(`/api/teams/untracked?projectId=${projectId}`)).data

const readTrackedTeams = async () => (await (await axios.get('/api/teams/tracked')).data).map(mapTeam)

const readTeamSpi = async ({ organizationName, projectId, teamId }) => (await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/metrics/spi`)).data

const readTeamCpi = async ({ organizationName, projectId, teamId }) => (await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/metrics/cpi`)).data

const readTeamFinances = async ({ organizationName, projectId, teamId }) => (await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/metrics/finances`)).data

const readTeamTimeline = async ({ organizationName, projectId, teamId }) => mapTeamTimeline((await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/metrics/timeline`)).data)

const readTeamCpiChart = async ({ organizationName, projectId, teamId }) => (await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/metrics/cpi-chart`)).data.map(mapCpiChartItem)

const readTeamBurndownChart = async ({ organizationName, projectId, teamId }) => mapBurndownChart((await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/metrics/burndown-chart`)).data)

const readWorkCostChart = async ({ organizationName, projectId, teamId }) => (await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/metrics/work-cost-chart`)).data.map(mapWorkCostChartItem)

const readTeamVelocityChart = async ({ organizationName, projectId, teamId }) => (await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/metrics/velocity-chart`)).data

const readTeamMilestoneChart = async ({ organizationName, projectId, teamId }) => (await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/metrics/milestone-chart`)).data.map(mapMilestoneChartItem)

const trackTeam = async ({ organizationName, projectId, teamId }) => await axios.post(`/api/teams`, { organizationName, projectId, teamId })

const readTeamDetails = async ({ organizationName, projectId, teamId }) => mapTeamDetails((await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}`)).data)

const updateTeam = async ({ organizationName, projectId, teamId, deadline, costPerEffort, eacFormula, etcFormula, archived }) => await axios.patch(`/api/teams/${organizationName}/${projectId}/${teamId}`, { deadline, costPerEffort, eacFormula, etcFormula, archived })

const deleteTeam = async ({ organizationName, projectId, teamId }) => await axios.delete(`/api/teams/${organizationName}/${projectId}/${teamId}`)

export {
  readUntrackedTeams,
  readTrackedTeams,
  readTeamSpi,
  readTeamCpi,
  readTeamFinances,
  readTeamTimeline,
  readTeamCpiChart,
  readTeamBurndownChart,
  readWorkCostChart,
  readTeamVelocityChart,
  readTeamMilestoneChart,
  trackTeam,
  readTeamDetails,
  updateTeam,
  deleteTeam
}
