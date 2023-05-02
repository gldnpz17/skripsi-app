import axios from "axios";

const readProjectsByOrganization = async ({ organizationName }) => await (await axios.get(`/api/organizations/${organizationName}/projects`)).data

export {
  readProjectsByOrganization
}