import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import Groups from '../views/Groups.vue'
import GroupDetail from '../views/GroupDetail.vue'
import Disciplines from '../views/Disciplines.vue'
import Tasks from '../views/Tasks.vue'
import Courses from '../views/Courses.vue'
import CourseProgress from '../views/CourseProgress.vue'
import Login from '../views/Login.vue'

const routes = [
  { path: '/login', name: 'login', component: Login, meta: { requiresAuth: false } },
  { path: '/', component: Dashboard },
  { path: '/groups', component: Groups },
  { path: '/groups/:id', component: GroupDetail },
  { path: '/disciplines', component: Disciplines },
  { path: '/disciplines/:id/tasks', component: Tasks },
  { path: '/courses', name: 'courses', component: Courses },
  { path: '/courses/:id/progress', name: 'course-progress', component: CourseProgress },
  { path: '/admin/users', name: 'admin-users', component: () => import('../views/admin/Users.vue'), meta: { requiresAdmin: true } },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach((to) => {
  const token = localStorage.getItem('auth_token')
  const role = localStorage.getItem('auth_role')

  if (to.name !== 'login' && !token) {
    return { name: 'login' }
  }
  if (to.name === 'login' && token) {
    return { path: '/' }
  }
  if (to.meta.requiresAdmin && role !== 'Admin') {
    return { path: '/' }
  }
})

export default router
