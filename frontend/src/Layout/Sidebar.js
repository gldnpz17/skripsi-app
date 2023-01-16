import { useQuery } from "react-query"
import { Link, Outlet, useMatch } from "react-router-dom"
import { getSelfProfile } from "../api-requests/Profile"
import { Configuration, Dashboard, LogOut, Project } from "../common/icons"

const SidebarItem = ({ icon, label, href }) => {
  const match = useMatch(href)

  return (
    <Link to={href} className='h-6 flex gap-2 group'>
      <span className={`h-6 aspect-square group-hover:text-secondary-light group-hover:rotate-12 duration-150 ${Boolean(match) ? 'text-secondary-dark' : 'text-gray-300'}`}>
        {icon}
      </span>
      <span className={`flex-grow group-hover:text-white ${Boolean(match) ? 'text-white' : 'text-gray-300'}`}>
        {label}
      </span>
      {Boolean(match) && (
        <span className='w-1 h-full bg-secondary-dark rounded-l-md' />
      )}
    </Link>
  )
}

const SidebarItems = [
  { icon: <Dashboard />, label: 'Dashboard', href: '/' },
  { icon: <Project />, label: 'Project Details', href: '/projects' },
  { icon: <Configuration />, label: 'Configuration', href: '/configuration' }
]

const LayoutSidebar = () => {
  const { 
    isLoading: profileLoading, 
    data: {
      name
    } = {}
  } = useQuery(['profile', 'self'], getSelfProfile)

  return (
    <div className='flex h-full bg-dark-2 text-white'>
      <div className='flex flex-col w-52 pl-6 pt-6'>
        <div className='mb-8'>
          App Logo Here
        </div>
        <div className='flex flex-col gap-6'>
          {SidebarItems.map(item => (
            <SidebarItem key={item.href} {...item} />
          ))}
        </div>
        <div className='flex-grow'></div>
        {!profileLoading && (
          <div className='mb-8'>
            <div className='text-xs font-semibold text-gray-400'>
              Logged in as
            </div>
            <div className='mr-6 break-all text-sm mb-1'>
              {name}
            </div>
            <a href='/api/auth/destroy-session' className='text-sm flex items-center text-gray-400 hover:underline hover:text-secondary-light cursor-pointer duration-150'>
              <LogOut className='h-3 inline mr-1' />
              logout
            </a>
          </div>
        )}
      </div>
      <div className='flex-grow pl-12 rounded-l-3xl bg-dark-1 shadow-2xl'>
        <Outlet />
      </div>
    </div>
  )
}

export { LayoutSidebar }