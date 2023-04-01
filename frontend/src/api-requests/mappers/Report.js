import { DateTime } from "luxon"

const mapReport = ({
  startDate,
  endDate,
  ...report
}) => ({
  ...report,
  startDate: DateTime.fromISO(startDate),
  endDate: DateTime.fromISO(endDate)
})

export { mapReport }