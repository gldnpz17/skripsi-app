import { useQuery } from "react-query"
import { readTrackedTeams } from "../../api-requests/Teams"
import { useCallback, useEffect, useMemo, useState } from "react"
import { usePersistedState } from "../usePersistedState"

const useTeams = () => {
  const {
    data: teams,
    isLoading: teamsLoading
  } = useQuery(['projects', 'tracked'], readTrackedTeams)

  const [selectedTeam, setSelectedTeam] = useState(undefined)
  const [storedTeamId, setSelectedTeamId] = usePersistedState('defaultTeamId', null)
  const [pinnedTeamIds, setPinnedTeamIds] = usePersistedState('pinnedTeamIds', [])
  
  useEffect(() => {
    if (!teams) return

    const storedTeam = teams.find(team => team.id === storedTeamId)
    
    if (storedTeam) {
      setSelectedTeam(storedTeam)
    } else {
      if (teams.length > 0) {
        setSelectedTeam(teams[0])
      } else {
        setSelectedTeam(undefined)
      }
    }
  }, [teamsLoading, teams, storedTeamId])

  const teamPinned = useCallback((team) => Boolean(pinnedTeamIds.find(id => id === team.id)), [pinnedTeamIds])

  const togglePin = useCallback((team) => () => {
    const newList = [...pinnedTeamIds]
    if (newList.find(id => id === team.id)) {
      setPinnedTeamIds(newList.filter(id => id !== team.id))
    } else {
      setPinnedTeamIds([...newList, team.id])
    }
  }, [pinnedTeamIds])

  const sortedTeams = useMemo(() => {
    if (!teams) return []
    const pinnedTeams = teams.filter(team => Boolean(pinnedTeamIds.find(id => id === team.id)))
    const unpinnedTeams = teams.filter(team => !Boolean(pinnedTeamIds.find(id => id === team.id)))
    return [...pinnedTeams, ...unpinnedTeams]
  }, [teams, pinnedTeamIds])

  return {
    teamsLoading,
    setSelectedTeamId,
    selectedTeam,
    teams: sortedTeams,
    pin: {
      teamPinned,
      togglePin
    }
  }
}

export { useTeams }