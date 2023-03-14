import axios from "axios";
import { DateTime } from "luxon";

const mapAvailableReport = ({
  startDate,
  endDate
}) => ({
  startDate: DateTime.fromISO(startDate),
  endDate: DateTime.fromISO(endDate)
})

const readTeamReports = async ({ organizationName, projectId, teamId }) => 
  (await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/reports`)).data

const createReport = async ({ organizationName, projectId, teamId, report: { startDate, endDate } }) => 
  (await axios.post(`/api/teams/${organizationName}/${projectId}/${teamId}`, { startDate, endDate }))

const readAvailableReports = async ({ organizationName, projectId, teamId }) =>
  ((await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/reports/available`)).data)?.map(mapAvailableReport)

const readTimespanSprints = async ({ organizationName, projectId, teamId, start, end }) =>
  (await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/reports/timespan-sprints?start=${start.toISO()}&end=${end.toISO()}`)).data

export { 
  readTeamReports,
  createReport,
  readAvailableReports,
  readTimespanSprints
}