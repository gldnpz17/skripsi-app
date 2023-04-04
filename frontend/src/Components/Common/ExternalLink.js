import { Link } from "react-router-dom"
import { OpenInNew } from "../../common/icons"

const ExternalLinkWrapper = ({ to, children, ...props }) => {
  const isAbsolute = /https:\/\//g.test(to)

  return isAbsolute ? <a href={to} {...props}>{children}</a> : <Link {...{ to, ...props }}>{children}</Link>
}

const ExternalLink = ({ className, children, to }) => (
  <ExternalLinkWrapper {...{ to }} target='_blank' className={`text-sm flex gap-1 items-center underline cursor-pointer text-gray-400 hover:text-secondary-light duration-150 ${className}`}>
    <span>{children}</span>
    <OpenInNew className='h-4' />
  </ExternalLinkWrapper>
)  

export { ExternalLink }