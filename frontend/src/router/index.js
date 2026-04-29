import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import Groups from '../views/Groups.vue'
import GroupDetail from '../views/GroupDetail.vue'
import Disciplines from '../views/Disciplines.vue'
import Tasks from '../views/Tasks.vue'
import Courses from '../views/Courses.vue'
import CourseProgress from '../views/CourseProgress.vue'

const routes = [
  { path: '/', component: Dashboard },
  { path: '/groups', component: Groups },
  { path: '/groups/:id', component: GroupDetail },
  { path: '/disciplines', component: Disciplines },
  { path: '/disciplines/:id/tasks', component: Tasks },
  { path: '/courses', name: 'courses', component: Courses },
  { path: '/courses/:id/progress', name: 'course-progress', component: CourseProgress },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

export default router
