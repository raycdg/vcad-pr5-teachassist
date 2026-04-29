import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import Groups from '../views/Groups.vue'
import GroupDetail from '../views/GroupDetail.vue'
import Disciplines from '../views/Disciplines.vue'

const routes = [
  { path: '/', component: Dashboard },
  { path: '/groups', component: Groups },
  { path: '/groups/:id', component: GroupDetail },
  { path: '/disciplines', component: Disciplines },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

export default router
