import { Heading2 } from "../Common/Headings";
import { ReportSprint } from "./ReportSprint";

const SprintsSection = ({ timespanSprints }) => (
  <>
    <Heading2>Sprints</Heading2>
    <div className='max-w-2xl flex flex-col gap-3'>
      {timespanSprints.map(timespanSprint => (
        <ReportSprint
          key={`${timespanSprint.sprint.startDate.toISO()}-${timespanSprint.sprint.endDate.toISO()}`}
          {...{ timespanSprint }}
        />
      ))}
    </div>
  </>
)

export { SprintsSection }