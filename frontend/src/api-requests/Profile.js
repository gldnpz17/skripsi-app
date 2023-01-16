import axios from "axios";

const getSelfProfile = async () => await (await axios.get('/api/profile/self')).data

export {
  getSelfProfile
}