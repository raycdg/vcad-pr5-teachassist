import { defineStore } from 'pinia'
import axios from 'axios'

const API = '/api/courses'

export const useCoursesStore = defineStore('courses', {
  state: () => ({
    courses: [],
    loading: false,
    error: null,
  }),
  actions: {
    async fetchCourses(showAll = false) {
      this.loading = true
      this.error = null
      try {
        const res = await axios.get(API, { params: { showAll } })
        this.courses = res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to load courses'
      } finally {
        this.loading = false
      }
    },
    async createCourse(data) {
      this.error = null
      try {
        const res = await axios.post(API, data)
        this.courses.push(res.data)
        return res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to create course'
        throw err
      }
    },
    async updateCourse(id, data) {
      this.error = null
      try {
        await axios.put(`${API}/${id}`, data)
        const idx = this.courses.findIndex(c => c.id === id)
        if (idx !== -1) this.courses[idx] = { ...this.courses[idx], ...data }
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to update course'
        throw err
      }
    },
    async toggleStatus(id) {
      this.error = null
      try {
        await axios.patch(`${API}/${id}/toggle-status`)
        const course = this.courses.find(c => c.id === id)
        if (course) course.isActive = !course.isActive
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to toggle status'
        throw err
      }
    },
    async deleteCourse(id) {
      this.error = null
      try {
        await axios.delete(`${API}/${id}`)
        this.courses = this.courses.filter(c => c.id !== id)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to delete course'
        throw err
      }
    },
    async fetchProgress(id) {
      this.error = null
      try {
        const res = await axios.get(`${API}/${id}/progress`)
        return res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to load progress'
        throw err
      }
    },
    async saveGrades(id, grades) {
      this.error = null
      try {
        await axios.post(`${API}/${id}/grades`, { grades })
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to save grades'
        throw err
      }
    },
  },
})
