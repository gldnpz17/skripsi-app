import logo from './logo.svg';
import './App.css';
import { BrowserRouter, createBrowserRouter, Route, Routes } from 'react-router-dom';
import { DashboardPage } from './pages/Dashboard';
import { LoginPage } from './pages/Login';
import { ConfigurationPage } from './pages/Configuration';

function App() {
  return (
    <BrowserRouter >
      <Routes>
        <Route path='/' element={<DashboardPage />} />
        <Route path='login' element={<LoginPage />} />
        <Route path='configuration' element={<ConfigurationPage />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
