const Format = {
  currency: (amount) => {
    if (Number.isNaN(amount)) return "Rp -"
    
    // TODO: You know, we shouldn't be using floating-point numbers when working with money right?
    return Intl.NumberFormat("id", { style: "currency", currency: "IDR" }).format(Math.round(amount))
  },
  severity: (value, ranges) => {
    return ranges.findIndex(predicate => predicate(value))
  },
  performanceIndex: (value) => {
    let status = 'Healthy'
    if (value < 1) {
      status = 'Critical'
    }

    return ({ status })
  },
  performanceIndexPercent: (value) => {
    return (1 / value) - 1
  },
  number: (amount, precision) => {
    return Math.round(amount * Math.pow(10, precision)) / Math.pow(10, precision)
  },
  status: (status) => {
    const dict = {
      'Healthy': 'Healthy',
      'AtRisk': 'At Risk',
      'Critical': 'Critical',
      'NoData': 'No Data'
    }

    return dict[status] ?? 'Can\'t Calculate'
  },
  statusColor: (status, prefix = '') => {
    // Make sure to add the relevant classes to the safelist in the tailwind config file.
    const dict = {
      'Healthy': 'green-300',
      'AtRisk': 'yellow-300',
      'Critical': 'red-300',
      'NoData': 'gray-300'
    }

    return prefix + dict[status]
  },
  month: (date) => {
    return Intl.DateTimeFormat('en-US', { month: 'long', year: 'numeric' }).format(date)
  },
  relativeTime: (timeDiff, unit, { ahead, behind }) => {
    return `${Math.abs(timeDiff)} ${unit}${Math.abs(timeDiff) > 1 ? 's' : ''} ${timeDiff >= 0 ? ahead : behind}`
  },
  reportKey: ({ startDate, endDate }) => `${startDate.toISO()}_${endDate.toISO()}`,
  briefDate: (date) => date.toFormat('dd MMM yyyy'),
  fullDate: (date) => date.toFormat('dd MMMM yyyy'),
  month: (date) => date.toFormat('MMMM yyyy'),
  error: (code) => {
    const dict = {
      'TEAM_NO_EFFORT_COST': {
        message: "The Cost/Effort value not set."
      }
    }

    return dict[code] ?? { message: code }
  }
}

export { Format }