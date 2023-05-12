import { useMemo } from "react"
import { Warning } from "../../common/icons"
import { ExternalLink } from "./ExternalLink"

const ErrorMessage = ({ message, reason, helpLink, helpText }) => (
  <div className='flex flex-col items-center'>
    <div className='flex gap-3 items-center'>
      <Warning className='h-8 text-orange-300' />
      <div>
        <div className=''>{message}</div>
        <div className='text-sm text-gray-300'>
          <b>Reason :</b> {reason}.&nbsp;
          {helpLink && helpText && (
            <ExternalLink className='inline-flex' to={helpLink}>{helpText}</ExternalLink>
          )}
        </div>
      </div>
    </div>
  </div>
)

const ErrorPlaceholder = ({ className, errorCode, ...props }) => {
  const errorProps = useMemo(() => {
    const errors = {
      'TEAM_NO_DEADLINE': () => ({
        reason: 'Deadline not set',
        helpText: 'Set deadline',
        helpLink: `/teams/${props.team.organization.name}/${props.team.project.id}/${props.team.id}`
      }),
      'TEAM_NO_SPRINTS': () => ({
        reason: 'No sprints',
        helpText: 'Add sprint',
        helpLink: `https://dev.azure.com/${props.team.organization.name}/${encodeURI(props.team.project.name)}`
      }),
      'TEAM_NO_EFFORT_COST': () => ({
        reason: 'Cost per effort not set',
        helpText: 'Set cost per effort',
        helpLink: `/teams/${props.team.organization.name}/${props.team.project.id}/${props.team.id}`
      }),
      'ZERO_EXPENDITURE': () => ({
        reason: 'Total expenditure is zero'
      }),
      'NO_REPORT': () => ({
        reason: 'No reports created yet'
      })
    }

    const error = errors[errorCode]

    if (!error) return ({ reason: `Unknown error` })

    return error()
  }, [errorCode, props])

  return (
    <div className={`bg-dark-2 p-6 border border-gray-700 flex items-center justify-center flex-col gap-2 rounded-md ${className}`}>
      <ErrorMessage {...errorProps} {...props} />
    </div>
  )
}

export { ErrorPlaceholder }