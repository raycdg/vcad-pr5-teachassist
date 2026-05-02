<template>
  <div>
    <div class="d-flex align-center mb-4">
      <v-btn
        variant="text"
        @click="router.push('/groups')"
      >
        <v-icon start>
          mdi-arrow-left
        </v-icon>
        Back to Groups
      </v-btn>
      <v-spacer />
      <h1 class="text-h4">
        {{ group?.name || 'Loading...' }}
      </h1>
      <v-spacer />
      <v-btn
        v-if="authStore.isManager"
        color="primary"
        @click="openCreate"
      >
        Add Student
      </v-btn>
    </div>

    <v-alert
      v-if="store.error"
      type="error"
      closable
      class="mb-4"
      @click:close="store.error = null"
    >
      {{ store.error }}
    </v-alert>

    <v-card
      v-if="group"
      class="mb-4"
    >
      <v-card-text>
        <div class="d-flex gap-4">
          <div><strong>Short Name:</strong> {{ group.shortName }}</div>
          <div><strong>Year Started:</strong> {{ group.yearStarted }}</div>
          <div><strong>Students:</strong> {{ store.students.length }}</div>
        </div>
      </v-card-text>
    </v-card>

    <v-data-table
      :headers="headers"
      :items="store.students"
      :loading="store.loading"
      item-value="id"
    >
      <template #item.actions="{ item }">
        <v-btn
          v-if="authStore.isManager"
          icon
          size="small"
          variant="text"
          @click="openEdit(item)"
        >
          <v-icon>mdi-pencil</v-icon>
        </v-btn>
        <v-btn
          v-if="authStore.isManager"
          icon
          size="small"
          variant="text"
          color="error"
          @click="confirmDelete(item)"
        >
          <v-icon>mdi-delete</v-icon>
        </v-btn>
      </template>
    </v-data-table>

    <v-dialog
      v-model="dialog"
      max-width="500"
    >
      <v-card>
        <v-card-title>{{ formTitle }}</v-card-title>
        <v-card-text>
          <v-form
            ref="formRef"
            @submit.prevent="save"
          >
            <v-text-field
              v-model="form.firstName"
              label="First Name"
              :rules="[v => !!v || 'Required']"
              maxlength="100"
            />
            <v-text-field
              v-model="form.lastName"
              label="Last Name"
              :rules="[v => !!v || 'Required']"
              maxlength="100"
            />
            <v-text-field
              v-model="form.email"
              label="Email (optional)"
              type="email"
              maxlength="200"
            />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn
            variant="text"
            @click="dialog = false"
          >
            Cancel
          </v-btn>
          <v-btn
            color="primary"
            :loading="saving"
            @click="save"
          >
            Save
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog
      v-model="deleteDialog"
      max-width="400"
    >
      <v-card>
        <v-card-title>Delete Student</v-card-title>
        <v-card-text>Are you sure you want to delete "{{ deleteTarget?.firstName }} {{ deleteTarget?.lastName }}"?</v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn
            variant="text"
            @click="deleteDialog = false"
          >
            Cancel
          </v-btn>
          <v-btn
            color="error"
            :loading="deleting"
            @click="doDelete"
          >
            Delete
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from 'axios'
import { useStudentsStore } from '../stores/students'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const store = useStudentsStore()
const authStore = useAuthStore()

const group = ref(null)
const groupError = ref(null)

const headers = [
  { title: 'First Name', key: 'firstName' },
  { title: 'Last Name', key: 'lastName' },
  { title: 'Email', key: 'email' },
  { title: 'Actions', key: 'actions', sortable: false },
]

const dialog = ref(false)
const deleteDialog = ref(false)
const saving = ref(false)
const deleting = ref(false)
const formRef = ref(null)
const editId = ref(null)
const deleteTarget = ref(null)

const form = ref({ firstName: '', lastName: '', email: '' })

const formTitle = computed(() => editId.value ? 'Edit Student' : 'Add Student')

onMounted(async () => {
  const groupId = parseInt(route.params.id)
  try {
    const res = await axios.get(`/api/groups/${groupId}`)
    group.value = res.data
  } catch (err) {
    groupError.value = err.response?.data?.message || 'Failed to load group'
  }
  store.fetchStudentsByGroup(groupId)
})

function openCreate() {
  editId.value = null
  form.value = { firstName: '', lastName: '', email: '' }
  dialog.value = true
}

function openEdit(item) {
  editId.value = item.id
  form.value = { firstName: item.firstName, lastName: item.lastName, email: item.email || '' }
  dialog.value = true
}

async function save() {
  const { valid } = await formRef.value.validate()
  if (!valid) return
  saving.value = true
  try {
    const groupId = parseInt(route.params.id)
    if (editId.value) {
      await store.updateStudent(editId.value, { ...form.value })
    } else {
      await store.createStudent({ ...form.value, groupId })
    }
    dialog.value = false
  } finally {
    saving.value = false
  }
}

function confirmDelete(item) {
  deleteTarget.value = item
  deleteDialog.value = true
}

async function doDelete() {
  deleting.value = true
  try {
    await store.deleteStudent(deleteTarget.value.id)
    deleteDialog.value = false
  } finally {
    deleting.value = false
  }
}
</script>
