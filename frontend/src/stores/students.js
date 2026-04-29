import { defineStore } from 'pinia'
import axios from 'axios'

export const useStudentsStore = defineStore('students', {
  state: () => ({
    students: [],
    loading: false,
    error: null,
    currentGroupId: null,
  }),
  actions: {
    async fetchStudentsByGroup(groupId) {
      this.loading = true
      this.error = null
      this.currentGroupId = groupId
      try {
        const res = await axios.get(`/api/groups/${groupId}/students`)
        this.students = res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to load students'
      } finally {
        this.loading = false
      }
    },
    async createStudent(data) {
      this.error = null
      try {
        const res = await axios.post('/api/students', data)
        this.students.push(res.data)
        this.students.sort((a, b) => a.lastName.localeCompare(b.lastName) || a.firstName.localeCompare(b.firstName))
        return res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to create student'
        throw err
      }
    },
    async updateStudent(id, data) {
      this.error = null
      try {
        const res = await axios.put(`/api/students/${id}`, data)
        const idx = this.students.findIndex(s => s.id === id)
        if (idx !== -1) this.students[idx] = res.data
        return res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to update student'
        throw err
      }
    },
    async deleteStudent(id) {
      this.error = null
      try {
        await axios.delete(`/api/students/${id}`)
        this.students = this.students.filter(s => s.id !== id)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to delete student'
        throw err
      }
    },
  },
})
