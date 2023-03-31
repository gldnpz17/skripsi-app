import { useCallback } from "react"
import { HealthMetric } from "./HealthMetric"
import { Format } from "../../common/Format"

const MetricGroup = ({ metricSpecs, data }) => {
  const getValue = useCallback((key) => data.cumulativeMetrics[metricSpecs.key][key], [data])
  const getDeltaValue = useCallback((key) => data.deltaMetrics[metricSpecs.key][key], [data])
  const getDeltaColor = useCallback((key) => {
    const higherIsBetter = metricSpecs.metrics[key].higherIsBetter
    const value = data.deltaMetrics[metricSpecs.key][key]
    if (higherIsBetter === undefined) {
      return 'text-white'
    }

    if ((higherIsBetter && value > 0) || (!higherIsBetter && value < 0)) {
      return 'text-green-400'
    } else {
      return 'text-red-400'
    }
  }, [data])

  const formatData = useCallback((data, key) => {
    switch (metricSpecs.metrics[key].type) {
      case 'currency':
        return Format.currency(data)
      case 'number':
        return Format.number(data, 2)
    }
  }, [data])

  return (
    <>
      {Object.keys(metricSpecs.metrics).map(key => (
        <HealthMetric
          key={key}
          label={metricSpecs.metrics[key].label}
          value={formatData(getValue(key), key)}
          additionalValue={
            <span className={getDeltaColor(key)}>
              &nbsp;{getDeltaValue(key) > 0 ? '+' : '-'} {formatData(Math.abs(getDeltaValue(key)), key)}
            </span>
          }
        />
      ))}
    </>
  )
}

export { MetricGroup }