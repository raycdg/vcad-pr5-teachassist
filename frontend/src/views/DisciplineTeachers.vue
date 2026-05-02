<template>
  <div>
    <div class="d-flex align-center mb-4">
      <v-btn
        icon
        variant="text"
        :to="{ name: 'disciplines' }"
      >
        <v-icon>mdi-arrow-left</v-icon>
      </v-btn>
      <h1 class="text-h4 ml-2">
        Teachers: {{ disciplineName }}
      </h1>
      <v-spacer />
      <v-btn
        v-if="authStore.isManager"
        color="primary"
        @click="openAddDialog"
      >
        Add Teacher
      </v-btn>
    </div>

    <v-alert
      v-if="error"
      type="error"
      closable
      class="mb-4"
      @click:close="error = null"
    >
      {{ error }}
    </v-alert>

    <v-data-table
      :headers="headers"
      :items="teachers"
      :loading="loading"
      item-value="id"
    >
      <template #item.actions="{ item }">
        <v-btn
          v-if="authStore.isManager"
          icon
          size="small"
          variant="text"
          color="error"
          @click="confirmRemove(item)"
        >
          <v-icon>mdi-delete</v-icon>
        </v-btn>
      </template>
    </v-data-table>

    <v-dialog
      v-model="addDialog"
      max-width="500"
    >
      <v-card>
        <v-card-title>Add Teacher</v-card-title>
        <v-card-text>
          <v-select
            v-model="selectedTeacherId"
            :items="availableTeachers"
            item-title="email"
            item-value="id"
            label="Select Teacher"
            :rules="[v => !!v || 'Required']"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn
            variant="text"
            @click="addDialog = false"
          >
            Cancel
          </v-btn>
          <v-btn
            color="primary"
            :loading="saving"
            @click="addTeacher"
          >
            Add
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog
      v-model="removeDialog"
      max-width="400"
    >
      <v-card>
        <v-card-title>Remove Teacher</v-card-title>
        <v-card-text>
          Are you sure you want to remove teacher "{{ removeTarget?.email }}" from this discipline?
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn
            variant="text"
            @click="removeDialog = false"
          >
            Cancel
          </v-btn>
          <v-btn
            color="error"
            :loading="removing"
            @click="doRemove"
          >
            Remove
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const authStore = useAuthStore()

const disciplineId = route.params.id
const disciplineName = ref('')
const teachers = ref([])
const availableTeachers = ref([])
const loading = ref(false)
const error = ref(null)
const saving = ref(false)
const removing = ref(false)
const addDialog = ref(false)
const removeDialog = ref(false)
const selectedTeacherId = ref(null)
const removeTarget = ref(null)

const headers = [
  { title: 'Email', key: 'email' },
  { title: 'Actions', key: 'actions', sortable: false },
]

async function fetchDiscipline() {
  try {
    const res = await axios.get(`/api/disciplines/${disciplineId}`)
    disciplineName.value = res.data.name
  } catch {
    error.value = 'Failed to load discipline'
  }
}

async function fetchTeachers() {
  loading.value = true
  error.value = null
  try {
    const res = await axios.get(`/api/disciplines/${disciplineId}/teachers`)
    teachers.value = res.data
  } catch (err) {
    error.value = err.response?.data?.message || 'Failed to load teachers'
  } finally {
    loading.value = false
  }
}

async function fetchAvailableTeachers() {
  try {
    const res = await axios.get('/api/users/teachers')
    const assignedIds = teachers.value.map(t => t.id)
    availableTeachers.value = res.data.filter(t => !assignedIds.includes(t.id))
  } catch (err) {
    error.value = err.response?.data?.message || 'Failed to load available teachers'
  }
}

function openAddDialog() {
  selectedTeacherId.value = null
  fetchAvailableTeachers()
  addDialog.value = true
}

async function addTeacher() {
  if (!selectedTeacherId.value) return
  saving.value = true
  error.value = null
  try {
    await axios.post(`/api/disciplines/${disciplineId}/assign-teacher`, { teacherId: selectedTeacherId.value })
    addDialog.value = false
    await fetchTeachers()
  } catch (err) {
    error.value = err.response?.data?.message || 'Failed to add teacher'
  } finally {
    saving.value = false
  }
}

function confirmRemove(teacher) {
  removeTarget.value = teacher
  removeDialog.value = true
}

async function doRemove() {
  removing.value = true
  error.value = null
  try {
    await axios.delete(`/api/disciplines/${disciplineId}/teachers/${removeTarget.value.id}`)
    removeDialog.value = false
    await fetchTeachers()
  } catch (err) {
    error.value = err.response?.data?.message || 'Failed to remove teacher'
  } finally {
    removing.value = false
  }
}

onMounted(() => {
  fetchDiscipline()
  fetchTeachers()
})
</script>
