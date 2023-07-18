const DashboardContainer = ({ children, interactive }) => (
  <div className={`rounded-md border border-gray-700 p-4 bg-dark-2 shadow-lg h-full flex flex-col ${interactive && 'shadow cursor-pointer hover:brightness-125 duration-150'}`}>
    {children}
  </div>
)

export { DashboardContainer } 