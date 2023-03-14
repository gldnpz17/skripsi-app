const Format = {
  currency: (amount) => {
    if (Number.isNaN(amount)) return "Rp -"
    
    // TODO: You know, we shouldn't be using floating-point numbers when working with money right?
    return Intl.NumberFormat("id", { style: "currency", currency: "IDR" }).format(Math.round(amount))
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
  relativeTime: (time, unit) => {
    let timePosition = null
    if (time > 0) {
      timePosition = 'ahead'
    } else if (time < 0) {
      timePosition = 'behind'
    }

    if (timePosition) {
      return `${Math.abs(time)} ${unit}${Math.abs(time) > 1 ? 's' : ''} ${timePosition}`
    } else {
      return 'right on time'
    }
  },
  reportKey: ({ startDate, endDate }) => `${startDate.toISO()}_${endDate.toISO()}`
}

export { Format }