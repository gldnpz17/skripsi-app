import { PinButton } from "./Common/PinButton"

const TeamListItem = ({ team: { name }, selected = false, onClick, pinned, togglePin }) => {
  return (
    <div
      {...{ onClick }}
      className='group/item relative border border-gray-700 rounded-md cursor-pointer overflow-hidden duration-150 hover:brightness-125 bg-dark-2'
    >
      <div className={`absolute top-0 bottom-0 left-0 w-1 ${selected && 'bg-secondary-dark'} duration-150`} />
      <span className='px-4 py-2 flex'>
        <span className='flex-grow font-semibold overflow-hidden whitespace-nowrap mr-2 overflow-ellipsis'>
          {name}
        </span>
        <PinButton {...{ pinned, togglePin }} />
      </span>
    </div>
  )
}

export { TeamListItem }