const Format = {
  currency: (amount) => {
    if (Number.isNaN(amount)) return "Rp -"
    
    // TODO: You know, we shouldn't be using floating-point numbers when working with money right?
    return Intl.NumberFormat("id", { style: "currency", currency: "IDR" }).format(Math.round(amount))
  },
  status: (status) => {
    const dict = {
      'healthy': 'Healthy',
      'atRisk': 'At Risk',
      'critical': 'Critical',
      'noData': 'No Data'
    }

    return dict[status]
  },
  statusColor: (status, prefix = '') => {
    // Make sure to add the relevant classes to the safelist in the tailwind config file.
    const dict = {
      'healthy': 'green-300',
      'atRisk': 'yellow-300',
      'critical': 'red-300',
      'noData': 'gray-300'
    }

    return prefix + dict[status]
  },
  month: (date) => {
    return Intl.DateTimeFormat('en-US', { month: 'long', year: 'numeric' }).format(date)
  },
  metricSeverity: (score) => {
    if (typeof(score) != 'number') return undefined

    if (score >= -1 && score < -0.5) {
      return 
    } else if (score >= -0.5 && score < 0) {
      return 
    } else if (score >= 0 && score <= 1) {
      return 
    } else {
      return undefined
    }
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
  timeliness: (score) => {
    if (typeof(score) != 'number') return ['No Value', undefined]

    if (score >= -1 && score < 0) {
      return ['Likely Late', 'critical']
    } else if (score >= 0 && score < 0.1) {
      return ['On Time', 'atRisk']
    } else if (score >= 0.1 && score <= 1) {
      return ['Ahead of Time', 'healthy']
    } else {
      return ['No Value', undefined]
    }
  }
}

export { Format }