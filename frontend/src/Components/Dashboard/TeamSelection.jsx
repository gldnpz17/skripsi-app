import { PlusCircle } from "../../common/icons"
import { Button } from "../Common/Button"
import { TeamListItem } from "./TeamListItem"

const TeamSelection = ({ setSelectedTeamId, teams, teamsLoading, teamPinned, togglePin }) => {
  return (
    <div>
      <div className='mb-3 text-gray-300 font-semibold'>Teams</div>
      {!teamsLoading && (
        <>
          <div className='flex flex-col gap-4 mb-6'>
            {teams.filter(team => !team.archived).map(team => (
              <TeamListItem
                key={team.id}
                selected={false}
                onClick={() => setSelectedTeamId(team.id)}
                pinned={teamPinned(team)}
                togglePin={togglePin(team)}
                {...{ team }} 
              />
            ))}
          </div>
          <a href='/track-new' target='_blank'>
            <Button className='w-full'>
              <PlusCircle className='h-4' />
              <span>Track Team</span>
            </Button>
          </a>
        </>
      )}
    </div>
  )
}

export { TeamSelection }