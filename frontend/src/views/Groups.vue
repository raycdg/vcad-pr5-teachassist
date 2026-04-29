<template>
  <div>
    <div class="d-flex align-center mb-4">
      <h1 class="text-h4">Groups</h1>
      <v-spacer />
      <v-btn color="primary" @click="openCreate">Add Group</v-btn>
    </div>

    <v-alert v-if="store.error" type="error" closable class="mb-4" @click:close="store.error = null">
      {{ store.error }}
    </v-alert>

    <v-data-table
      :headers="headers"
      :items="store.groups"
      :loading="store.loading"
      item-value="id"
    >
      <template #item.students="{ item }">
        <v-btn variant="text" size="small" @click="router.push(`/groups/${item.id}`)">
          <v-icon start>mdi-account-multiple</v-icon>
          Students
        </v-btn>
      </template>
      <template #item.actions="{ item }">
        <v-btn icon size="small" variant="text" @click="openEdit(item)">
          <v-icon>mdi-pencil</v-icon>
        </v-btn>
        <v-btn icon size="small" variant="text" color="error" @click="confirmDelete(item)">
          <v-icon>mdi-delete</v-icon>
        </v-btn>
      </template>
    </v-data-table>

    <v-dialog v-model="dialog" max-width="500">
      <v-card>
        <v-card-title>{{ formTitle }}</v-card-title>
        <v-card-text>
          <v-form ref="formRef" @submit.prevent="save">
            <v-text-field
              v-model="form.name"
              label="Name"
              :rules="[v => !!v || 'Required']"
              maxlength="200"
            />
            <v-text-field
              v-model="form.shortName"
              label="Short Name"
              :rules="[v => !!v || 'Required']"
              maxlength="50"
            />
            <v-text-field
              v-model.number="form.yearStarted"
              label="Year Started"
              type="number"
              :rules="[v => !!v || 'Required', v => v >= 2000 && v <= 2100 || 'Must be between 2000 and 2100']"
            />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="dialog = false">Cancel</v-btn>
          <v-btn color="primary" :loading="saving" @click="save">Save</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="deleteDialog" max-width="400">
      <v-card>
        <v-card-title>Delete Group</v-card-title>
        <v-card-text>Are you sure you want to delete "{{ deleteTarget?.name }}"?</v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="deleteDialog = false">Cancel</v-btn>
          <v-btn color="error" :loading="deleting" @click="doDelete">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useGroupsStore } from '../stores/groups'

const router = useRouter()
const store = useGroupsStore()

const headers = [
  { title: 'Name', key: 'name' },
  { title: 'Short Name', key: 'shortName' },
  { title: 'Year', key: 'yearStarted' },
  { title: '', key: 'students', sortable: false },
  { title: 'Actions', key: 'actions', sortable: false },
]

store.fetchGroups()

const dialog = ref(false)
const deleteDialog = ref(false)
const saving = ref(false)
const deleting = ref(false)
const formRef = ref(null)
const editId = ref(null)
const deleteTarget = ref(null)

const form = ref({ name: '', shortName: '', yearStarted: new Date().getFullYear() })

const formTitle = computed(() => editId.value ? 'Edit Group' : 'Create Group')

function openCreate() {
  editId.value = null
  form.value = { name: '', shortName: '', yearStarted: new Date().getFullYear() }
  dialog.value = true
}

function openEdit(item) {
  editId.value = item.id
  form.value = { name: item.name, shortName: item.shortName, yearStarted: item.yearStarted }
  dialog.value = true
}

async function save() {
  const { valid } = await formRef.value.validate()
  if (!valid) return
  saving.value = true
  try {
    if (editId.value) {
      await store.updateGroup(editId.value, { ...form.value })
    } else {
      await store.createGroup({ ...form.value })
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
    await store.deleteGroup(deleteTarget.value.id)
    deleteDialog.value = false
  } finally {
    deleting.value = false
  }
}
</script>
