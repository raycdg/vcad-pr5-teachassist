<template>
  <v-container>
    <v-row align="center">
      <v-col>
        <h1 class="text-h4">
          User Management
        </h1>
      </v-col>
      <v-col cols="auto">
        <v-btn
          color="primary"
          @click="showCreateDialog = true"
        >
          Add User
        </v-btn>
      </v-col>
    </v-row>

    <v-data-table
      :headers="headers"
      :items="users"
      :loading="loading"
      class="mt-4"
    >
      <template #item.role="{ item }">
        <v-chip
          :color="roleColor(item.role)"
          size="small"
        >
          {{ item.role }}
        </v-chip>
      </template>

      <template #item.status="{ item }">
        <v-chip
          :color="item.isDeleted ? 'error' : 'success'"
          size="small"
        >
          {{ item.isDeleted ? 'Deleted' : 'Active' }}
        </v-chip>
      </template>

      <template #item.actions="{ item }">
        <v-btn
          v-if="!item.isDeleted"
          variant="text"
          icon="mdi-pencil"
          size="small"
          @click="openRoleDialog(item)"
        />
        <v-btn
          v-if="!item.isDeleted"
          variant="text"
          icon="mdi-lock-reset"
          size="small"
          @click="openResetPasswordDialog(item)"
        />
        <v-btn
          v-if="!item.isDeleted"
          variant="text"
          icon="mdi-delete"
          size="small"
          color="error"
          @click="deleteUser(item)"
        />
        <v-btn
          v-else
          variant="text"
          icon="mdi-restore"
          size="small"
          color="success"
          @click="restoreUser(item)"
        />
      </template>
    </v-data-table>

    <v-dialog
      v-model="showCreateDialog"
      max-width="500"
    >
      <v-card>
        <v-card-title>Create User</v-card-title>
        <v-card-text>
          <v-form
            ref="createForm"
            @submit.prevent="createUser"
          >
            <v-text-field
              v-model="newUser.email"
              label="Email"
              type="email"
              :rules="[v => !!v || 'Required']"
              required
            />
            <v-text-field
              v-model="newUser.password"
              label="Password"
              type="password"
              :rules="[v => !!v || 'Required']"
              required
            />
            <v-select
              v-model="newUser.role"
              :items="availableRoles"
              label="Role"
              :rules="[v => !!v || 'Required']"
              required
            />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn
            variant="text"
            @click="showCreateDialog = false"
          >
            Cancel
          </v-btn>
          <v-btn
            color="primary"
            @click="createUser"
          >
            Create
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog
      v-model="showRoleDialog"
      max-width="400"
    >
      <v-card>
        <v-card-title>Change Role</v-card-title>
        <v-card-text>
          <v-select
            v-model="editRole.role"
            :items="availableRoles"
            label="Role"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn
            variant="text"
            @click="showRoleDialog = false"
          >
            Cancel
          </v-btn>
          <v-btn
            color="primary"
            @click="saveRole"
          >
            Save
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog
      v-model="showResetPasswordDialog"
      max-width="400"
    >
      <v-card>
        <v-card-title>Reset Password</v-card-title>
        <v-card-text>
          <v-text-field
            v-model="newPassword"
            label="New Password"
            type="password"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn
            variant="text"
            @click="showResetPasswordDialog = false"
          >
            Cancel
          </v-btn>
          <v-btn
            color="primary"
            @click="resetPassword"
          >
            Reset
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-snackbar
      v-model="snackbar"
      :color="snackbarColor"
    >
      {{ snackbarText }}
    </v-snackbar>
  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

const headers = [
  { title: 'Email', key: 'email' },
  { title: 'Role', key: 'role' },
  { title: 'Status', key: 'isDeleted' },
  { title: 'Created', key: 'createdAt' },
  { title: 'Actions', key: 'actions', sortable: false },
]

const users = ref([])
const loading = ref(false)
const showCreateDialog = ref(false)
const showRoleDialog = ref(false)
const showResetPasswordDialog = ref(false)

const newUser = ref({ email: '', password: '', role: '' })
const editRole = ref({ id: '', role: '' })
const newPassword = ref('')
const resetUserId = ref('')

const availableRoles = ['Admin', 'Manager', 'Teacher']

const snackbar = ref(false)
const snackbarText = ref('')
const snackbarColor = ref('success')

function roleColor(role) {
  switch (role) {
    case 'Admin': return 'red'
    case 'Manager': return 'blue'
    case 'Teacher': return 'green'
    default: return 'grey'
  }
}

function showSnackbar(text, color = 'success') {
  snackbarText.value = text
  snackbarColor.value = color
  snackbar.value = true
}

async function fetchUsers() {
  loading.value = true
  try {
    const res = await axios.get('/api/users', { params: { includeDeleted: true } })
    users.value = res.data
  } catch {
    showSnackbar('Failed to load users', 'error')
  } finally {
    loading.value = false
  }
}

async function createUser() {
  try {
    await axios.post('/api/users', newUser.value)
    showSnackbar('User created')
    showCreateDialog.value = false
    newUser.value = { email: '', password: '', role: '' }
    await fetchUsers()
  } catch (err) {
    showSnackbar(err.response?.data?.message || 'Failed to create user', 'error')
  }
}

function openRoleDialog(user) {
  editRole.value = { id: user.id, role: user.role }
  showRoleDialog.value = true
}

async function saveRole() {
  try {
    await axios.put(`/api/users/${editRole.value.id}/role`, { role: editRole.value.role })
    showSnackbar('Role updated')
    showRoleDialog.value = false
    await fetchUsers()
  } catch (err) {
    showSnackbar(err.response?.data?.message || 'Failed to update role', 'error')
  }
}

function openResetPasswordDialog(user) {
  resetUserId.value = user.id
  newPassword.value = ''
  showResetPasswordDialog.value = true
}

async function resetPassword() {
  try {
    await axios.put(`/api/users/${resetUserId.value}/reset-password`, { newPassword: newPassword.value })
    showSnackbar('Password reset')
    showResetPasswordDialog.value = false
  } catch (err) {
    showSnackbar(err.response?.data?.message || 'Failed to reset password', 'error')
  }
}

async function deleteUser(user) {
  if (!confirm(`Delete user ${user.email}?`)) return
  try {
    await axios.delete(`/api/users/${user.id}`)
    showSnackbar('User deleted')
    await fetchUsers()
  } catch {
    showSnackbar('Failed to delete user', 'error')
  }
}

async function restoreUser(user) {
  try {
    await axios.post(`/api/users/${user.id}/restore`)
    showSnackbar('User restored')
    await fetchUsers()
  } catch {
    showSnackbar('Failed to restore user', 'error')
  }
}

onMounted(fetchUsers)
</script>
