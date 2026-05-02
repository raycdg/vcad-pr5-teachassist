import { useAuthStore } from '../stores/auth'

export const roleDirective = {
  mounted(el, binding) {
    const authStore = useAuthStore()
    const userRole = authStore.role
    const allowedRoles = Array.isArray(binding.value) ? binding.value : [binding.value]

    if (!allowedRoles.includes(userRole)) {
      el.style.display = 'none'
    }
  },
  updated(el, binding) {
    const authStore = useAuthStore()
    const userRole = authStore.role
    const allowedRoles = Array.isArray(binding.value) ? binding.value : [binding.value]

    if (!allowedRoles.includes(userRole)) {
      el.style.display = 'none'
    } else {
      el.style.display = ''
    }
  },
}

export default roleDirective
