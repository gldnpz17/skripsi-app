import axios from "axios";

const readAllOrganizations = async () => (await axios.get(`/api/organizations`)).data

export {
  readAllOrganizations
}
