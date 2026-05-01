import { defineStore } from 'pinia'
import axios from 'axios'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('auth_token') || null,
    email: localStorage.getItem('auth_email') || null,
    loading: false,
    error: null,
  }),
  getters: {
    isLoggedIn: (state) => !!state.token,
  },
  actions: {
    async login(email, password) {
      this.loading = true
      this.error = null
      try {
        const res = await axios.post('/api/auth/login', { email, password })
        this.token = res.data.token
        this.email = res.data.email
        localStorage.setItem('auth_token', res.data.token)
        localStorage.setItem('auth_email', res.data.email)
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
      localStorage.removeItem('auth_token')
      localStorage.removeItem('auth_email')
    },
  },
})
