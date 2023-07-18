import { PinFilled, PinFilledOff, PinOutline } from "../../../common/icons"

const PinButton = ({ pinned, togglePin }) => {
  const onClick = (e) => {
    e.stopPropagation()
    togglePin()
  }

  return (
    <button
      className={`${pinned ? 'block' : 'hidden group-hover/item:block'}`}
      {...{ onClick }}
    >
      {pinned && (
        <div className='group/button'>
          <PinFilled className='h-4 block group-hover/button:hidden' />
          <PinFilledOff className='h-4 hidden group-hover/button:block' />
        </div>
      )}
      {!pinned && (
        <div className='group/button'>
          <PinOutline className='h-4 block group-hover/button:hidden' />
          <PinFilled className='h-4 hidden group-hover/button:block' />
        </div>
      )}
    </button>
  )
}

export { PinButton }