import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import Groups from '../views/Groups.vue'
import GroupDetail from '../views/GroupDetail.vue'
import Disciplines from '../views/Disciplines.vue'
import Tasks from '../views/Tasks.vue'
import Courses from '../views/Courses.vue'
import CourseProgress from '../views/CourseProgress.vue'
import Login from '../views/Login.vue'
import Profile from '../views/Profile.vue'
import Forbidden from '../views/Forbidden.vue'

const routes = [
  { path: '/login', name: 'login', component: Login, meta: { requiresAuth: false } },
  { path: '/', component: Dashboard },
  { path: '/groups', name: 'groups', component: Groups, meta: { requiresManager: true } },
  { path: '/groups/:id', name: 'group-detail', component: GroupDetail, meta: { requiresManager: true } },
  { path: '/disciplines', name: 'disciplines', component: Disciplines },
  { path: '/disciplines/:id/tasks', name: 'tasks', component: Tasks },
  { path: '/courses', name: 'courses', component: Courses },
  { path: '/courses/:id/progress', name: 'course-progress', component: CourseProgress },
  { path: '/admin/users', name: 'admin-users', component: () => import('../views/admin/Users.vue'), meta: { requiresAdmin: true } },
  { path: '/profile', name: 'profile', component: Profile },
  { path: '/forbidden', name: 'forbidden', component: Forbidden },
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
    return { name: 'forbidden' }
  }
  if (to.meta.requiresManager && role === 'Teacher') {
    return { name: 'forbidden' }
  }
})

export default router
