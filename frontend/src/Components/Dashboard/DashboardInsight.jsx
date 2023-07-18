import { DashboardContainer } from "./Common/DashboardContainer";
import { DashboardLabel } from "./Common/DashboardLabel";

const DashboardInsight = ({ icon, label, children }) => (
  <DashboardContainer>
    <DashboardLabel {...{ icon, label }} />
    {children}
  </DashboardContainer>
)

export { DashboardInsight }