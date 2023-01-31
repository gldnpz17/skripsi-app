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

const queryClient = new QueryClient()

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path='/' element={<LayoutSidebar />}>
            <Route path='/' element={<DashboardPage />} />
            <Route path='configuration' element={<ConfigurationPage />} />
            <Route path='track-new' element={<TrackNewTeamPage />} />
          </Route>
          <Route path='login' element={<LoginPage />} />
          <Route path='oauth-success' element={<OAuthSuccessPage />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

export default App
