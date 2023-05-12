import { LinearProgress, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow } from "@mui/material"
import { readTeamCpi, readTrackedTeams } from "../api-requests/Teams"
import { Format } from "../common/Format"
import { Button } from "../Components/Common/Button"
import { useQuery } from "react-query"
import { Skeleton } from "../Components/Common/Skeleton"
import { useEffect, useState } from "react"
import { DataGrid } from "@mui/x-data-grid"
import { Archive } from "../common/icons"
import { withAuth } from "../HigherOrderComponents/withAuth"

const Loading = () => (
  <LinearProgress color='inherit' className='text-primary-dark' />
)

const Page = () => {
  const {
    data: teams,
    isLoading: teamsLoading
  } = useQuery(['projects', 'tracked'], readTrackedTeams)

  const columns = [
    { 
      field: 'teamName', 
      headerName: 'Team', 
      width: 200,
      renderCell: ({ row: { team } }) => (
        <div className='flex gap-2 items-center'>
          <div>{team.name}</div>
          {team.archived && (
            <Archive className='h-4 text-primary-dark' />
          )}
        </div>
      )
    },
    {
      field: 'projectName',
      headerName: 'Project',
      width: 200
    },
    {
      field: 'organizationName',
      headerName: 'Organization',
      width: 200
    },
    {
      field: 'cpi',
      headerName: 'CPI',
      width: 100
    },
    {
      field: 'spi',
      headerName: 'SPI',
      width: 100,
    },
    {
      field: 'deadline',
      headerName: 'Deadline',
      width: 150,
      valueGetter: ({ value }) => value ? Format.fullDate(value) : 'N/A'
    },
    {
      headerName: 'Actions',
      width: 225,
      renderCell: ({ row: { team } }) => (
        <div className='flex gap-3'>
          <a target='_blank' href={`https://dev.azure.com/${team.organization.name}/${encodeURIComponent(team.project.name)}/_backlogs/backlog/${encodeURIComponent(team.name)}/Issues`}>
            <Button>Backlog</Button>
          </a>
          <a target='_blank' href={`/teams/${team.organization.name}/${team.project.id}/${team.id}`}>
            <Button>Details</Button>
          </a>
        </div>
      )
    },
  ]

  const [rows, setRows] = useState(null)

  useEffect(() => {
    if (!teams) return

    (async () => {
      const rows = await Promise.all(
        teams
          .sort((a, b) => (a.archived === b.archived) ? 0 : a.archived ? 1 : -1)
          .map(async (team) => {
            const teamId = team.id
            const projectId = team.project.id
            const organizationName = team.organization.name
            let cpi = 'Error!'
            let spi = 'Error!'
            try {
              cpi = Format.number((await readTeamCpi({ teamId, projectId, organizationName })).CostPerformanceIndex, 2)
              spi = Format.number((await readTeamCpi({ teamId, projectId, organizationName })).SchedulePerformanceIndex, 2)
            } catch (err) {
              // TODO: Display error message?
            }

            return ({
              id: team.id,
              team,
              teamName: team.name,
              projectName: team.project.name,
              organizationName: organizationName,
              cpi,
              spi,
              deadline: team.deadline
            })
          })
      )

      setRows(rows)
    })()
  }, [teams, teamsLoading])

  return (
    <div className='pr-12 py-8 h-full overflow-auto'>
      <h1 className='text-2xl mb-6'>My Teams</h1>
      <div className='min-w-full w-0'>
        <DataGrid
          rowSelection={false}
          slots={{
            loadingOverlay: Loading
          }}
          loading={!Boolean(rows)}
          sx={{ height: '36rem' }}
          {...{ rows: rows ?? [], columns }}
        />
      </div>
    </div>
  )
}

const TeamsListPage = withAuth(Page)

export { TeamsListPage }