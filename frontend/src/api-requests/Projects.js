import axios from "axios";

const readAllProjects = async () => await (await axios.get('/api/projects')).data

export {
  readAllProjects
}