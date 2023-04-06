import logo from './logo.svg';
import './App.css';
import { BrowserRouter, createBrowserRouter, Route, Routes } from 'react-router-dom';
import { DashboardPage } from './pages/Dashboard';
import { LoginPage } from './pages/Login';
import { ConfigurationPage } from './pages/Configuration';
import { LayoutSidebar } from './Layout/Sidebar';
import { OAuthSuccessPage } from './pages/OAuthSuccess';
import { QueryClient, QueryClientProvider } from 'react-query';
import { TrackNewTeamPage } from './pages/TrackNewTeamProject';
import { TeamDetailsPage } from './pages/TeamDetails';
import { TeamsListPage } from './pages/TeamsList';
import { NewReportPage } from './pages/NewReport';
import { EditReportPage } from './pages/EditReport';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';

const queryClient = new QueryClient()
const darkTheme = createTheme({
  palette: {
    mode: 'dark',
  },
});

function App() {
  return (
    <ThemeProvider theme={darkTheme}>
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <Routes>
            <Route path='/' element={<LayoutSidebar />}>
              <Route path='/' element={<DashboardPage />} />
              <Route path='configuration' element={<ConfigurationPage />} />
              <Route path='track-new' element={<TrackNewTeamPage />} />
              <Route path='/teams' element={<TeamsListPage />} />
              <Route path='teams/:organizationName/:projectId/:teamId' element={<TeamDetailsPage />} />
              <Route path='teams/:organizationName/:projectId/:teamId/reports/create' element={<NewReportPage />} />
              <Route path='team/:organizationName/:projectId/:teamId/reports/:reportId/edit' element={<EditReportPage />} />
            </Route>
            <Route path='login' element={<LoginPage />} />
            <Route path='oauth-success' element={<OAuthSuccessPage />} />
          </Routes>
        </BrowserRouter>
      </QueryClientProvider>
    </ThemeProvider>
  )
}

export default App
