import { defineStore } from 'pinia'
import axios from 'axios'

const API = '/api/disciplines'

export const useDisciplinesStore = defineStore('disciplines', {
  state: () => ({
    disciplines: [],
    loading: false,
    error: null,
  }),
  actions: {
    async fetchDisciplines() {
      this.loading = true
      this.error = null
      try {
        const res = await axios.get(API)
        this.disciplines = res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to load disciplines'
      } finally {
        this.loading = false
      }
    },
    async createDiscipline(data) {
      this.error = null
      try {
        const res = await axios.post(API, data)
        this.disciplines.push(res.data)
        this.disciplines.sort((a, b) => a.name.localeCompare(b.name))
        return res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to create discipline'
        throw err
      }
    },
    async updateDiscipline(id, data) {
      this.error = null
      try {
        const res = await axios.put(`${API}/${id}`, data)
        const idx = this.disciplines.findIndex(d => d.id === id)
        if (idx !== -1) this.disciplines[idx] = res.data
        return res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to update discipline'
        throw err
      }
    },
    async deleteDiscipline(id) {
      this.error = null
      try {
        await axios.delete(`${API}/${id}`)
        this.disciplines = this.disciplines.filter(d => d.id !== id)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to delete discipline'
        throw err
      }
    },
  },
})
