import { useMemo } from "react"
import { Line } from "react-chartjs-2"
import { Form, Link } from "react-router-dom"
import { ApplicationError } from "../common/ApplicationError"
import { Cash, Configuration, OpenInNew, Speedometer } from "../common/icons"
import { CategoryScale, Chart as ChartJS, Legend, LinearScale, LineElement, PointElement, Title, Tooltip } from 'chart.js'
import { Format } from "../common/Format"

const ExternalLink = ({ className, children }) => (
  <Link className={`text-sm flex gap-1 items-center underline cursor-pointer text-gray-400 hover:text-secondary-light duration-150 ${className}`}>
    <span>{children}</span>
    <OpenInNew className='h-4' />
  </Link>
)  

const ProjectListItem = ({ project: { title, status }, selected = false }) => {
  return (
    <div className='group relative border border-gray-700 rounded-md cursor-pointer overflow-hidden hover:-translate-x-1 duration-150 hover:brightness-125 bg-dark-2'>
      <div className={`absolute top-0 bottom-0 left-0 w-1 ${selected && 'bg-secondary-dark'} duration-150`} />
      <span className='px-4 py-2 flex'>
        <span className='flex-grow font-semibold'>
          {title}
        </span>
        <span className={`${Format.statusColor(status, 'text-')}`}>
          {Format.status(status)}
        </span>
      </span>
    </div>
  )
}

const projects = [
  { title: 'Project A', status: 'healthy' },
  { title: 'Project B', status: 'atRisk' },
  { title: 'Project C', status: 'critical' },
  { title: 'Project D', status: 'healthy' },
  { title: 'Project E', status: 'healthy' }
]

const ProjectListSection = () => (
  <div>
    <div className='mb-3 text-gray-300 font-semibold'>Projects</div>
    <div className='flex flex-col gap-4'>
      {projects.map((project, index) => (
        <ProjectListItem key={index} {...{ project }} selected={index == 1} />
      ))}
    </div>
  </div>
)

const HealthComponentStatus = ({ title, statusText, severity }) => {
  const color = ['green', 'orange', 'red'][severity]

  return (
    <div className='rounded-md border border-gray-700 p-4 bg-dark-2 shadow-lg'>
      <div className='text-gray-400 text-sm font-bold mb-1'>{title}</div>
      <div className={`text-${color}-300 text-xl`}>
        {statusText}
      </div>
    </div>
  )
}

const ProgressBar = ({ progress, className }) => (
  <div className={`h-2 rounded-lg overflow-hidden ${className} relative rounded`}>
    {/* Background */}
    <div className='bg-primary-light absolute inset-0' />
    {/* Progress */}
    <div className={`absolute left-0 rounded-lg top-0 bottom-0 bg-primary-dark`} style={{ right: `${(1 - progress) * 100}%` }} />
  </div>
)

const DATASET = {
  label: 'Project Health',
  data: [0.2, 0.6, 0.3, 0.2, 0.5, 0.9],
  borderColor: 'rgb(111, 134, 191)',
  tension: 0.3
}

ChartJS.register(LinearScale, CategoryScale, PointElement, LineElement, Title, Tooltip, Legend)

const IconButton = ({ children, onClick }) => (
  <div className='hover:text-secondary-light rounded-full hover:rotate-12 duration-150 cursor-pointer' {...{ onClick }}>
    {children}
  </div>
)

const HealthLineChart = () => (
  <div className='h-72 flex flex-col w-full rounded-md bg-dark-2 p-4 border border-gray-700'>
    <div className='w-full mb-4 flex items-center'>
      <div className='text-gray-400 text-sm font-bold flex-grow'>Project Health History</div>
      <IconButton onClick={() => alert('Hello')}>
        <Configuration className='h-5' />
      </IconButton>
    </div>
    <div className='flex-grow'>
      <Line
        style={{
          minHeight: "100%",
          height: "0",
          width: "100%"
        }}
        options={{
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: null,
          },
          scales: {
            x: {
              ticks: {
                color: 'white'
              },
              grid: {
                display: false
              }
            },
            y: {
              display: false,
              ticks: {
                color: 'white'
              },
              min: 0,
              max: 1,
              grid: {
                display: false
              }
            }
          }
        }}
        data={{
          labels: ['January', 'February', 'March', 'April', 'May', 'June'],
          datasets: [DATASET]
        }}
      />
    </div>
  </div>
)

const REPORTS = [
  { date: '6 January 2023', velocity: 33, expenditureRate: 3300000, status: 'critical' },
  { date: '9 February 2023', velocity: 23, expenditureRate: 3100000, status: 'atRisk' },
  { date: '5 March 2023', velocity: 12, expenditureRate: 3500000, status: 'critical' },
  { date: '3 April 2023', velocity: 36, expenditureRate: 3600000, status: 'critical' },
  { date: '5 May 2023', velocity: 38, expenditureRate: 3700000, status: 'atRisk' },
  { date: '6 June 2023', velocity: 45, expenditureRate: 3200000, status: 'healthy' }
]

const ReportItem = ({ report: { date, velocity, expenditureRate, status } }) => (
  <div className='flex items-center bg-dark-2 p-4 rounded-md border border-gray-700 shadow cursor-pointer hover:brightness-125 duration-150'>
    <span className='mr-2'>{date}</span>
    <span className={`${Format.statusColor(status, 'bg-')} text-black px-2 rounded text-sm`}>
      {Format.status(status)}
    </span>
    <span className='flex-grow'></span>
    <Speedometer className='h-4 mr-2' />
    <span className='mr-6 text-gray-400'>{velocity} efforts/sprint</span>
    <Cash className='h-4 mr-2' />
    <span className='text-gray-400'>{Format.currency(expenditureRate)}/sprint</span>
  </div>
)

const ReportsList = ({ reports }) => (
  <div>
    <div className='flex text-sm items-center mb-4'>
      <span className='flex-grow font-bold text-gray-400'>Recent Reports</span>
      <ExternalLink>
        See More
      </ExternalLink>
    </div>
    <div className='flex flex-col gap-4'>
      {reports.map(report => (
        <ReportItem key={report.date} {...{ report }} />
      ))}
    </div>
  </div>
)

const ProjectDetailSection = () => (
  <div className='grid grid-cols-12 gap-x-4 gap-y-6'>
    <div className='flex items-center col-span-12'>
      <div className='flex-grow'>
        <h1 className='text-2xl mb-1'>Project Name</h1>
        <ExternalLink>Project Settings</ExternalLink>
      </div>
      <div>
        <div className='text-sm text-gray-400 text-right mb-1'>Overall Project Health</div>
        <div className='flex gap-2 items-center'>
          <ProgressBar progress={0.8} className='w-32' />
          <span>
            80%
          </span>
        </div>
      </div>
    </div>
    <div className='col-span-4'>
      <HealthComponentStatus
        title="Timeliness"
        statusText="On Time"
        severity={0}
      />
    </div>
    <div className='col-span-4'>
      <HealthComponentStatus
        title="Budget"
        statusText="Under Budget"
        severity={0}
      />
    </div>
    <div className='col-span-4'>
      <HealthComponentStatus
        title="Scope Creep"
        statusText="Medium"
        severity={1}
      />
    </div>
    <div className='col-span-12'>
      <HealthLineChart />
    </div>
    <div className='col-span-12 mb-8'>
      <ReportsList reports={REPORTS} />
    </div>
  </div>
)

const DashboardPage = () => {
  return (
    <div className='h-full overflow-auto'>
      <div className='mr-80 mt-8'>
        <ProjectDetailSection />
      </div>
      <div className='w-64 top-8 bottom-0 right-12 fixed pl-4'>
        {/* Divider */}
        <div className='w-px absolute top-0 bottom-8 left-0 bg-gray-700' />
        <ProjectListSection />
      </div>
    </div>
  )
}

export { DashboardPage }