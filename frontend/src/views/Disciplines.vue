<template>
  <div>
    <div class="d-flex align-center mb-4">
      <h1 class="text-h4">
        Disciplines
      </h1>
      <v-spacer />
      <v-btn
        v-if="authStore.isTeacher"
        color="primary"
        @click="openCreate"
      >
        Add Discipline
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

    <v-text-field
      v-model="search"
      label="Search by name or abbreviation"
      prepend-inner-icon="mdi-magnify"
      clearable
      density="compact"
      class="mb-4"
    />

    <v-data-table
      :headers="headers"
      :items="filteredDisciplines"
      :loading="store.loading"
      item-value="id"
    >
      <template #item.actions="{ item }">
        <v-btn
          icon
          size="small"
          variant="text"
          :to="`/disciplines/${item.id}/tasks`"
          color="primary"
        >
          <v-icon>mdi-clipboard-text</v-icon>
        </v-btn>
        <v-btn
          v-if="authStore.isTeacher"
          icon
          size="small"
          variant="text"
          @click="openEdit(item)"
        >
          <v-icon>mdi-pencil</v-icon>
        </v-btn>
        <v-btn
          v-if="authStore.isTeacher"
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
              v-model="form.name"
              label="Name"
              :rules="[v => !!v || 'Required']"
              maxlength="200"
            />
            <v-text-field
              v-model="form.abbreviation"
              label="Abbreviation"
              :rules="[v => !!v || 'Required']"
              maxlength="50"
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
        <v-card-title>Delete Discipline</v-card-title>
        <v-card-text>Are you sure you want to delete "{{ deleteTarget?.name }}"?</v-card-text>
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
import { ref, computed } from 'vue'
import { useDisciplinesStore } from '../stores/disciplines'
import { useAuthStore } from '../stores/auth'

const store = useDisciplinesStore()
const authStore = useAuthStore()

const headers = [
  { title: 'Name', key: 'name' },
  { title: 'Abbreviation', key: 'abbreviation' },
  { title: 'Actions', key: 'actions', sortable: false },
]

store.fetchDisciplines()

const search = ref('')
const dialog = ref(false)
const deleteDialog = ref(false)
const saving = ref(false)
const deleting = ref(false)
const formRef = ref(null)
const editId = ref(null)
const deleteTarget = ref(null)

const form = ref({ name: '', abbreviation: '' })

const formTitle = computed(() => editId.value ? 'Edit Discipline' : 'Create Discipline')

const filteredDisciplines = computed(() => {
  if (!search.value) return store.disciplines
  const q = search.value.toLowerCase()
  return store.disciplines.filter(d =>
    d.name.toLowerCase().includes(q) || d.abbreviation.toLowerCase().includes(q)
  )
})

function openCreate() {
  editId.value = null
  form.value = { name: '', abbreviation: '' }
  dialog.value = true
}

function openEdit(item) {
  editId.value = item.id
  form.value = { name: item.name, abbreviation: item.abbreviation }
  dialog.value = true
}

async function save() {
  const { valid } = await formRef.value.validate()
  if (!valid) return
  saving.value = true
  try {
    if (editId.value) {
      await store.updateDiscipline(editId.value, { ...form.value })
    } else {
      await store.createDiscipline({ ...form.value })
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
    await store.deleteDiscipline(deleteTarget.value.id)
    deleteDialog.value = false
  } finally {
    deleting.value = false
  }
}
</script>
