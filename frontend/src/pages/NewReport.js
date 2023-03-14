import { Button } from "../Components/Common/Button"
import { FormInput } from "../Components/Common/FormInput"
import { CalendarRange, HeartPulse, Information, Save } from "../common/icons"

const ReportSprint = () => (
  <div className='bg-dark-2 px-4 py-3 rounded-md border border-gray-700 flex items-center'>
    <span className='flex-grow font-semibold'>
      <span className='line-through text-gray-400'>25 Feb 2023</span>
      <span>&nbsp;</span>
      <span>1 Mar 2023</span>
      <span> - </span>
      <span>9 Mar 2023</span>
    </span>
    <span className='bg-slate-700 rounded-full px-4 py-1 text-sm'>30 Effort</span>
  </div>
)

const Heading2 = ({ className, children }) => (
  <div className={`text-sm text-gray-400 font-bold mb-2 flex items-center ${className}`}>{children}</div>
)

const SprintsSection = () => (
  <>
    <Heading2>Sprints</Heading2>
    <div className='max-w-2xl flex flex-col gap-3'>
      <ReportSprint />
      <ReportSprint />
      <ReportSprint />
    </div>
  </>
)

const DataSection = () => (
  <>
    <Heading2>Report Data</Heading2>
    <FormInput label='Expenditure (Rp)' type='number' />
  </>
)

const TitleSection = () => (
  <>
    <div className='text-2xl mb-1'>Create New Report</div>
    <div className='flex gap-1 items-center mb-4'>
      <CalendarRange className='h-4' />
      <span>1 March 2023 - 31 March 2023</span>
    </div>
  </>
)

const HealthMetric = ({ label, value, additionalValue }) => (
  <div>
    <div className='flex gap-1 items-center'>
      <span className='text-sm font-bold text-gray-400'>{label}</span>
      <Information className='h-4 text-secondary-dark hover:text-secondary-light duration-300 cursor-pointer' />
    </div>
    <div className='flex items-center'>
      <span>{value}</span>
      {additionalValue}
    </div>
  </div>
)

const ActionSection = () => (
  <div className='flex justify-end max-w-2xl'>
    <Button>
      <Save className='h-4' />
      Save
    </Button>
  </div>
)

const NewReportPage = () => (
  <div className='py-8 pr-6 flex min-h-full'>
    <div className='flex-grow flex flex-col'>
      <div>
        <TitleSection />
      </div>
      <div className='mb-6'>
        <SprintsSection />
      </div>
      <div>
        <DataSection />
      </div>
      <div className='flex-grow' />
      <div>
        <ActionSection />
      </div>
    </div>
    <div className='bg-dark-2 p-4 rounded-md border border-gray-700'>
      <Heading2 className='mb-3'>
        <HeartPulse className='h-3 mr-1 text-primary-light' />
        Project Health Metrics
      </Heading2>
      <div className='flex flex-col gap-2'>
        <HealthMetric label='Planned Value' value='Rp. 10.000.000,00' />
        <HealthMetric label='Earned Value' value='Rp. 5.000.000,00' additionalValue={<span className='text-green-400'>&nbsp;+ Rp. 500.000,00</span>} />
        <HealthMetric label='Actual Cost' value='Rp. 6.000.000,00' additionalValue={<span className='text-green-400'>&nbsp;+ Rp. 500.000,00</span>} />
        <hr className='border-gray-700 my-2' />
        <HealthMetric label='Cost Variance' value='Rp. 10.000.000,00' />
        <HealthMetric label='Schedule Variance' value='Rp. 10.000.000,00' />
        <HealthMetric label='Cost Performance Index' value='0.667' />
        <HealthMetric label='Schedule Performance Index' value='1.5' />
        <hr className='border-gray-700 my-2' />
        <HealthMetric label='Estimate to Completion' value='Rp. 10.000.000,00' />
        <HealthMetric label='Estimate at Completion' value='Rp. 10.000.000,00' />
        <HealthMetric label='Variance at Completion' value='Rp. 10.000.000,00' />
      </div>
    </div>
  </div>
)

export { NewReportPage }