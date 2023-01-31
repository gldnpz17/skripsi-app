import styles from './Spinner.module.css'

// Source : https://github.com/n3r4zzurr0/svg-spinners/blob/main/svg-css/ring-resize.svg?short_path=951875e

const Spinner = ({ className }) => (
  <svg stroke="#000" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" {...{ className }}>
    <g className={styles.spinner}>
      <circle cx="12" cy="12" r="9.5" fill="none" stroke-width="3" className={styles.spinnerCircle}>
      </circle >
    </g>
  </svg>
)

export { Spinner }