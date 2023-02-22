import { Format } from "../../common/Format"
import { Cash, Edit, Speedometer } from "../../common/icons"
import { IconButton } from "./IconButton"

const ReportItemContainer = ({ children }) => (
  <div className='flex relative items-center bg-dark-2 p-4 rounded-md border border-gray-700 shadow cursor-pointer hover:brightness-125 duration-150'>
    {children}
  </div>
)

const ReportStatus = ({ status }) => (
  <span className={`${Format.statusColor(status, 'bg-')} text-black px-2 rounded text-sm`}>
    {Format.status(status)}
  </span>
)

const ReportItem = ({ report: { date, velocity, expenditureRate, status } }) => (
  <ReportItemContainer>
    <span className='mr-2'>{Format.month(date)}</span>
    <ReportStatus {...{ status }} />
    <span className='flex-grow'></span>
    <Speedometer className='h-4 mr-2' />
    <span className='mr-6 text-gray-400'>{velocity} efforts/sprint</span>
    <Cash className='h-4 mr-2' />
    <span className='text-gray-400'>{Format.currency(expenditureRate)}/sprint</span>
  </ReportItemContainer>
)

const Ping = () => (
  <span class="flex h-3 w-3 absolute -top-1 -right-1">
    <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-sky-400 opacity-75"></span>
    <span class="relative inline-flex rounded-full h-3 w-3 bg-sky-500"></span>
  </span>
)

const BlankReportItem = ({ date }) => (
  <ReportItemContainer>
    <span className='mr-2'>{Format.month(date)}</span>
    <ReportStatus status='NoData' />
    <span className='flex-grow' />
    <Ping />
  </ReportItemContainer>
)

export { ReportItem, BlankReportItem }