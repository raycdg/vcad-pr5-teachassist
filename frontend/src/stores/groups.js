import { defineStore } from 'pinia'
import axios from 'axios'

const API = '/api/groups'

export const useGroupsStore = defineStore('groups', {
  state: () => ({
    groups: [],
    loading: false,
    error: null,
  }),
  actions: {
    async fetchGroups() {
      this.loading = true
      this.error = null
      try {
        const res = await axios.get(API)
        this.groups = res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to load groups'
      } finally {
        this.loading = false
      }
    },
    async createGroup(data) {
      this.error = null
      try {
        const res = await axios.post(API, data)
        this.groups.push(res.data)
        this.groups.sort((a, b) => a.yearStarted - b.yearStarted || a.name.localeCompare(b.name))
        return res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to create group'
        throw err
      }
    },
    async updateGroup(id, data) {
      this.error = null
      try {
        const res = await axios.put(`${API}/${id}`, data)
        const idx = this.groups.findIndex(g => g.id === id)
        if (idx !== -1) this.groups[idx] = res.data
        return res.data
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to update group'
        throw err
      }
    },
    async deleteGroup(id) {
      this.error = null
      try {
        await axios.delete(`${API}/${id}`)
        this.groups = this.groups.filter(g => g.id !== id)
      } catch (err) {
        this.error = err.response?.data?.message || 'Failed to delete group'
        throw err
      }
    },
  },
})
