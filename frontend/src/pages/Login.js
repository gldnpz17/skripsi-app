import { Button } from "../Components/Common/Button"
import { ExternalLink } from "../Components/Common/ExternalLink"
import { Logo } from "../Components/Common/Logo"
import { AzureDevops, GitHub } from "../common/icons"

const LoginPage = () => {
  return (
    <div className='h-full w-full flex'>
      <div className='flex-grow z-20 relative overflow-hidden'>
        <div className='absolute inset-0 z-0'>
          <img src='/scream.png' className='h-64 absolute -right-8 -rotate-12 -bottom-6 opacity-20' />
          <img src='/login-bg.png' className='object-cover h-full w-full' />
        </div>
        <div className='w-0 h-0 min-w-full min-h-full relative z-10'>
          <div className='w-full h-full py-32 pl-32 pr-24 flex flex-col justify-center'>
            <div className='mb-8'>
              <Logo size={1} />
            </div>
            <div className='mb-4 flex gap-2'>
              <div className='w-[4px] h-full bg-primary-dark'></div>
              <div>
                SCREⱯM is an application that helps you monitor your SCRUM project's health.
                Our application is integrated with Azure DevOps so you could easily incorporate it into your workflow.
              </div>
            </div>
            <div className='mb-8'>
              <div className='font-semibold'>Features</div>
              <ul>
                <li>- View EVM-based project health metrics</li>
                <li>- Forecast future costs</li>
                <li>- Monthly reports to track health over time</li>
                <li>- Customizable estimation formulas</li>
                <li>
                  - Open source! View the&nbsp;
                  <a href='https://github.com' className='text-secondary-dark font-bold hover:text-secondary-light duration-300'>
                    GitHub Repo <GitHub className='h-4 inline' />
                  </a>
                </li>
              </ul>
            </div>
            <a href='/api/auth/create-session'>
              <Button>
                <AzureDevops className='h-5 text-sky-900 mr-1' />
                Connect with Azure DevOps
              </Button>
            </a>
          </div>
        </div>
      </div>
      <div className='flex-grow relative z-20 bg-white'>
        <div className='w-0 h-0 min-w-full min-h-full'>
          <div className='h-full w-full p-16 flex items-center justify-center'>
            <img src='/app-screenshot-v3.png' className='shadow-2xl shadow-dark-2 rounded' />
          </div>
        </div>
      </div>
    </div>
  )
}

export { LoginPage }