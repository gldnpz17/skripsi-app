import { DateTime } from "luxon"
import { DashboardContainer } from "../Components/Dashboard/Common/DashboardContainer"
import { DashboardLabel } from "../Components/Dashboard/Common/DashboardLabel"
import { DashboardInsight } from "../Components/Dashboard/DashboardInsight"
import { DashboardLayout } from "../Components/Dashboard/DashboardLayout"
import { useTeams } from "../Hooks/Dashboard/useTeams"
import { Format } from "../common/Format"
import { Bug, CheckSolid, CrossSolid, File, Print, SideTriangle, StickerCheck, StickerCross, StickerList } from "../common/icons"
import { TeamName } from "../Components/Dashboard/TeamName"
import { Button } from "../Components/Common/Button"

const WorkItem = ({ label, resolved, azureDevopsUrl }) => (
  <DashboardContainer interactive>
    <a className='flex items-center' href={azureDevopsUrl} target='_blank'>
      <div className='flex-grow'>{label}</div>
      {resolved ? <CheckSolid className='h-6 text-green-400' /> : <CrossSolid className='h-6 text-red-400' />}
    </a>
  </DashboardContainer>
)

const Sprint = ({ label, startDate, endDate, selected }) => (
  <div className='flex items-center cursor-pointer hover:text-secondary-dark hover:bg-dark-2 duration-150 py-1 px-2'>
    {selected && (
      <SideTriangle className='h-4 text-secondary-dark mr-1' />
    )}
    <div className='flex-grow font-bold'>{label}</div>
    <div>{Format.briefDate(startDate)} - {Format.briefDate(endDate)}</div>
  </div>
)

const SummaryItem = ({ icon, label, info }) => {
  const Icon = icon

  return (
    <div className='flex items-center py-2'>
      <Icon className='h-4 text-secondary-dark mr-2' />
      <div className='flex-grow'>{label}</div>
      <div className='font-bold'>{info}</div>
    </div>
  )
}

const PageContent = () => (
  <div className='grid grid-cols-12 gap-6 mb-12'>
    <div className='col-span-6 flex flex-col'>
      <DashboardInsight icon={File} label='Summary'>
        <div className='flex flex-col justify-around flex-grow'>
          <SummaryItem icon={Bug} label='Resolved Bugs' info='12' />
          <SummaryItem icon={StickerCheck} label='Finished User Stories' info='34' />
          <SummaryItem icon={StickerCross} label='Unfinished User Stories' info='56' />
          <SummaryItem icon={StickerList} label='Total User Stories' info='78' />
        </div>
      </DashboardInsight>
      <Button className='w-full mt-4'>
        <Print className='h-4' />
        <span>Print Report</span>
      </Button>
    </div>
    <div className='col-span-6 flex flex-col'>
      <DashboardLabel label='Sprints' external />
      <div className='flex-grow relative'>
        <div className='flex flex-col gap-1 overflow-y-scroll min-h-full h-0'>
          <Sprint label='Sprint #5 (current)' startDate={DateTime.now()} endDate={DateTime.now()} selected />
          <Sprint label='Sprint #4' startDate={DateTime.now()} endDate={DateTime.now()} />
          <Sprint label='Sprint #3' startDate={DateTime.now()} endDate={DateTime.now()} />
          <Sprint label='Sprint #2' startDate={DateTime.now()} endDate={DateTime.now()} />
          <Sprint label='Sprint #1' startDate={DateTime.now()} endDate={DateTime.now()} />
          <Sprint label='Sprint #1' startDate={DateTime.now()} endDate={DateTime.now()} />
          <Sprint label='Sprint #1' startDate={DateTime.now()} endDate={DateTime.now()} />
          <Sprint label='Sprint #1' startDate={DateTime.now()} endDate={DateTime.now()} />
          <Sprint label='Sprint #1' startDate={DateTime.now()} endDate={DateTime.now()} />
          <Sprint label='Sprint #1' startDate={DateTime.now()} endDate={DateTime.now()} />
          <Sprint label='Sprint #1' startDate={DateTime.now()} endDate={DateTime.now()} />
          <Sprint label='Sprint #1' startDate={DateTime.now()} endDate={DateTime.now()} />
        </div>
      </div>
    </div>
    <div className='col-span-6'>
      <DashboardLabel label='Bugs' external />
      <div className='flex flex-col gap-4'>
        <WorkItem label='Lorem Ipsum Dolor sit Amet' resolved azureDevopsUrl='https://google.com' />
        <WorkItem label='Consectetur Adipiscing Elit' resolved azureDevopsUrl='https://google.com' />
        <WorkItem label='Integer Effictur leo Porta' resolved={false} />
      </div>
    </div>
    <div className='col-span-6'>
      <DashboardLabel label='User Stories' external />
      <div className='flex flex-col gap-4'>
        <WorkItem label='Integer eu Posuere' resolved azureDevopsUrl='https://google.com' />
        <WorkItem label='Orci Varius Natoque' resolved azureDevopsUrl='https://google.com' />
        <WorkItem label='Vestibulum Ante Ipsum' resolved azureDevopsUrl='https://google.com' />
        <WorkItem label='Donec Venenatis Maximus Felis' resolved azureDevopsUrl='https://google.com' />
        <WorkItem label='aliquet Erat ut Ipsum Scelerisque' resolved={false} />
      </div>
    </div>
  </div>
)

const ProgressReportPage = () => {
  const { teams, teamsLoading, setSelectedTeamId, selectedTeam, pin: { teamPinned, togglePin } } = useTeams()

  return (
    <DashboardLayout {...{ teams, teamsLoading, setSelectedTeamId, teamPinned, togglePin }}>
      {selectedTeam && (
        <>
          <TeamName team={selectedTeam} />
          <PageContent />
        </>
      )}
    </DashboardLayout>
  )
}

export { ProgressReportPage }