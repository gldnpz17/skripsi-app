import { Divider } from "./Common/Divider"
import { TeamSelection } from "./TeamSelection"

const DashboardLayout = ({ children, teamsLoading, teams, setSelectedTeamId, teamPinned, togglePin }) => (
  <div className='h-full overflow-auto overflow-y-scroll'>
    <div className='mr-80 mt-8'>
      {children}
    </div>
    <div className='w-64 top-8 bottom-0 right-12 fixed pl-4'>
      <Divider />
      <TeamSelection
        {...{ teamsLoading, teams, setSelectedTeamId, teamPinned, togglePin }}
      />
    </div>
  </div>
)

export { DashboardLayout }