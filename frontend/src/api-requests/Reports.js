import axios from "axios";
import { DateTime } from "luxon";

const mapAvailableReport = ({
  startDate,
  endDate
}) => ({
  startDate: DateTime.fromISO(startDate),
  endDate: DateTime.fromISO(endDate)
})

const mapTimespanSprint = ({
  accountedStartDate,
  accountedEndDate,
  sprint: {
    startDate,
    endDate,
    ...sprint
  },
  ...timespanSprint
}) => ({
  ...timespanSprint,
  sprint: {
    ...sprint,
    startDate: DateTime.fromISO(startDate),
    endDate: DateTime.fromISO(endDate)
  },
  accountedStartDate: DateTime.fromISO(accountedStartDate),
  accountedEndDate: DateTime.fromISO(accountedEndDate)
})

const readTeamReports = async ({ organizationName, projectId, teamId }) => 
  (await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/reports`)).data

const createReport = async ({ organizationName, projectId, teamId, report: { startDate, endDate } }) => 
  (await axios.post(`/api/teams/${organizationName}/${projectId}/${teamId}`, { startDate, endDate }))

const readAvailableReports = async ({ organizationName, projectId, teamId }) =>
  ((await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/reports/available`)).data)?.map(mapAvailableReport)

const readTimespanSprints = async ({ organizationName, projectId, teamId, start, end }) =>
  ((await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/reports/timespan-sprints?start=${encodeURIComponent(start)}&end=${encodeURIComponent(end)}`)).data)?.map(mapTimespanSprint)

const readReportMetrics = async ({ organizationName, projectId, teamId, start, end, expenditure }) =>
  (await axios.get(`/api/teams/${organizationName}/${projectId}/${teamId}/reports/new-report-metrics?start=${encodeURIComponent(start)}&end=${encodeURIComponent(end)}&expenditure=${expenditure}`)).data

export { 
  readTeamReports,
  createReport,
  readAvailableReports,
  readTimespanSprints,
  readReportMetrics
}