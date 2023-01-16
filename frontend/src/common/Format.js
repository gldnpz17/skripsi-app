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
      'critical': 'Critical'
    }

    return dict[status]
  },
  statusColor: (status, prefix = '') => {
    // Make sure to add the relevant classes to the safelist in the tailwind config file.
    const dict = {
      'healthy': 'green-300',
      'atRisk': 'yellow-300',
      'critical': 'red-300'
    }

    return prefix + dict[status]
  }
}

export { Format }