import { useMemo } from "react"
import { CalendarRange } from "../../common/icons"
import { Format } from "../../common/Format"
import { DateTime } from "luxon"

const TitleSection = ({ start, end, title }) => {
  const { startDate, endDate } = useMemo(() => {
    return ({
      startDate: DateTime.fromISO(start),
      endDate: DateTime.fromISO(end)
    })
  }, [start, end])

  return (
    <>
      <div className='text-2xl mb-1'>{title}</div>
      <div className='flex gap-1 items-center mb-4'>
        <CalendarRange className='h-4' />
        <span>{Format.briefDate(startDate)} - {Format.briefDate(endDate)}</span>
      </div>
    </>
  )
}

export { TitleSection }