import { defineStore } from 'pinia'
import axios from 'axios'

export const useTasksStore = defineStore('tasks', {
  state: () => ({
    tasks: [],
    loading: false,
    error: null,
  }),
  actions: {
    async fetchTasks(disciplineId, search) {
      this.loading = true
      this.error = null
      try {
        const params = search ? { search } : {}
        const res = await axios.get(`/api/disciplines/${disciplineId}/tasks`, { params })
        this.tasks = res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to load tasks'
      } finally {
        this.loading = false
      }
    },
    async createTask(disciplineId, data) {
      this.error = null
      try {
        const res = await axios.post(`/api/disciplines/${disciplineId}/tasks`, data)
        this.tasks.push(res.data)
        this.tasks.sort((a, b) => a.number - b.number)
        return res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to create task'
        throw err
      }
    },
    async updateTask(disciplineId, id, data) {
      this.error = null
      try {
        const res = await axios.put(`/api/disciplines/${disciplineId}/tasks/${id}`, data)
        const idx = this.tasks.findIndex(t => t.id === id)
        if (idx !== -1) this.tasks[idx] = res.data
        return res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to update task'
        throw err
      }
    },
    async deleteTask(disciplineId, id) {
      this.error = null
      try {
        await axios.delete(`/api/disciplines/${disciplineId}/tasks/${id}`)
        this.tasks = this.tasks.filter(t => t.id !== id)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to delete task'
        throw err
      }
    },
    async changePriority(disciplineId, id, direction) {
      this.error = null
      try {
        await axios.patch(`/api/disciplines/${disciplineId}/tasks/${id}/priority`, null, { params: { direction } })
        await this.fetchTasks(disciplineId)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to change priority'
        throw err
      }
    },
  },
})
