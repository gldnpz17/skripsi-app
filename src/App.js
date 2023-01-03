import logo from './logo.svg';
import './App.css';
import { BrowserRouter, createBrowserRouter, Route, Routes } from 'react-router-dom';
import { DashboardPage } from './pages/Dashboard';
import { LoginPage } from './pages/Login';
import { ConfigurationPage } from './pages/Configuration';
import { LayoutSidebar } from './Layout/Sidebar';

function App() {
  return (
    <BrowserRouter >
      <Routes>
        <Route path='/' element={<LayoutSidebar />}>
          <Route path='/' element={<DashboardPage />} />
          <Route path='configuration' element={<ConfigurationPage />} />
        </Route>
        <Route path='login' element={<LoginPage />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
