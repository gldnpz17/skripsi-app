import { createContext, useContext } from "react";
import { getSelfProfile } from "../api-requests/Profile";
import { useNavigate } from "react-router-dom";
import { useQuery } from "react-query";

const AuthContext = createContext({
  profile: null,
  isLoading: true
})

const AuthProvider = ({ children }) => {
  const { isLoading, data: { name } = {} } = useQuery(['profile', 'self'], getSelfProfile)

  return (
    <AuthContext.Provider value={{ isLoading, profile: name ? { name } : null }}>
      {children}
    </AuthContext.Provider>
  )
}

const withAuth = (Page) => {
  const Component = (props) => {
    const { isLoading, profile } = useContext(AuthContext)
    const navigate = useNavigate()

    if (!isLoading && !profile) {
      navigate('/login')
    }

    return <Page {...props} />
  }

  return Component
}

export { AuthProvider, withAuth }