import { Button } from "../Components/Common/Button"
import { Logo } from "../Components/Common/Logo"
import { AzureDevops } from "../common/icons"

const OAuthSuccessPage = () => (
  <div className='h-full w-full flex flex-col p-20 items-center justify-center'>
    <div className='absolute inset-0 z-0'>
      <img src='/login-bg.png' className='object-cover h-full w-full' />
    </div>
    <div className='absolute top-20 left-auto right-auto z-10'>
      <Logo size={1} />
    </div>
    <div className='flex flex-col items-center relative z-10'>
      <div className='font-bold text-xl text-secondary-light underline mb-1'>Success!</div>
      <div className='mb-8 flex'>
        The application has beeen connected to&nbsp;
        <b className='text-sky-500 flex items-center'><AzureDevops className='h-4' />&nbsp;Azure DevOps</b>
      </div>
      <a href='/'>
        <Button>To Dashboard</Button>
      </a>
    </div>
  </div>
)

export { OAuthSuccessPage }