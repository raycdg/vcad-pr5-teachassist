import { defineStore } from 'pinia'
import axios from 'axios'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('auth_token') || null,
    email: localStorage.getItem('auth_email') || null,
    role: localStorage.getItem('auth_role') || null,
    userId: localStorage.getItem('auth_userId') || null,
    loading: false,
    error: null,
  }),
  getters: {
    isLoggedIn: (state) => !!state.token,
    isAdmin: (state) => state.role === 'Admin',
    isManager: (state) => state.role === 'Manager' || state.role === 'Admin',
    isTeacher: (state) => state.role === 'Teacher' || state.role === 'Manager' || state.role === 'Admin',
  },
  actions: {
    async login(email, password) {
      this.loading = true
      this.error = null
      try {
        const res = await axios.post('/api/auth/login', { email, password })
        this.token = res.data.token
        this.email = res.data.email
        this.role = res.data.role
        this.userId = res.data.userId
        localStorage.setItem('auth_token', res.data.token)
        localStorage.setItem('auth_email', res.data.email)
        localStorage.setItem('auth_role', res.data.role)
        localStorage.setItem('auth_userId', res.data.userId)
      } catch (err) {
        this.error = err.response?.data?.message || 'Login failed'
        throw err
      } finally {
        this.loading = false
      }
    },
    logout() {
      this.token = null
      this.email = null
      this.role = null
      this.userId = null
      localStorage.removeItem('auth_token')
      localStorage.removeItem('auth_email')
      localStorage.removeItem('auth_role')
      localStorage.removeItem('auth_userId')
    },
  },
})
