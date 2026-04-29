import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import Groups from '../views/Groups.vue'
import GroupDetail from '../views/GroupDetail.vue'

const routes = [
  { path: '/', component: Dashboard },
  { path: '/groups', component: Groups },
  { path: '/groups/:id', component: GroupDetail },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

export default router
