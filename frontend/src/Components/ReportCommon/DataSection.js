import { FormInput } from "../Common/FormInput"
import { Heading2 } from "../Common/Headings"

const DataSection = ({ expenditure, setExpenditure }) => (
  <>
    <Heading2>Report Data</Heading2>
    <FormInput
      value={expenditure}
      label='Expenditure (Rp)'
      type='number'
      onChange={({ target: { value } }) => setExpenditure(value ? Number.parseInt(value) : 0)}
    />
  </>
)

export { DataSection }