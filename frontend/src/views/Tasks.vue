<template>
  <div>
    <div class="d-flex align-center mb-4">
      <h1 class="text-h4">Tasks for discipline #{{ disciplineId }}</h1>
      <v-spacer />
      <v-btn color="primary" @click="openCreate">Add Task</v-btn>
    </div>

    <v-alert v-if="store.error" type="error" closable class="mb-4" @click:close="store.error = null">
      {{ store.error }}
    </v-alert>

    <v-text-field
      v-model="search"
      label="Search by name"
      prepend-inner-icon="mdi-magnify"
      clearable
      density="compact"
      class="mb-4"
    />

    <v-data-table
      :headers="headers"
      :items="store.tasks"
      :loading="store.loading"
      item-value="id"
    >
      <template #item.gradingType="{ item }">
        {{ item.gradingType === 1 ? 'pass/fail' : 'score' }}
      </template>
      <template #item.maxScore="{ item }">
        {{ item.gradingType === 2 ? item.maxScore : '-' }}
      </template>
      <template #item.actions="{ item }">
        <v-btn icon size="small" variant="text" @click="movePriority(item, 'up')" :disabled="item.number <= 1">
          <v-icon>mdi-arrow-up</v-icon>
        </v-btn>
        <v-btn icon size="small" variant="text" @click="movePriority(item, 'down')">
          <v-icon>mdi-arrow-down</v-icon>
        </v-btn>
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
            <v-select
              v-model="form.gradingType"
              label="Grading Type"
              :items="gradingTypes"
              :rules="[v => !!v || 'Required']"
            />
            <v-text-field
              v-if="form.gradingType === 2"
              v-model.number="form.maxScore"
              label="Max Score"
              type="number"
              :rules="[v => !!v || 'Required for score type']"
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
        <v-card-title>Delete Task</v-card-title>
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
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useTasksStore } from '../stores/tasks'

const route = useRoute()
const store = useTasksStore()

const disciplineId = Number(route.params.id)

const headers = [
  { title: 'Number', key: 'number' },
  { title: 'Name', key: 'name' },
  { title: 'Grading Type', key: 'gradingType' },
  { title: 'Max Score', key: 'maxScore' },
  { title: 'Actions', key: 'actions', sortable: false },
]

const gradingTypes = [
  { title: 'pass/fail', value: 1 },
  { title: 'score', value: 2 },
]

onMounted(() => {
  store.fetchTasks(disciplineId)
})

const search = ref('')
const dialog = ref(false)
const deleteDialog = ref(false)
const saving = ref(false)
const deleting = ref(false)
const formRef = ref(null)
const editId = ref(null)
const deleteTarget = ref(null)

const form = ref({ name: '', gradingType: null, maxScore: null })

const formTitle = computed(() => editId.value ? 'Edit Task' : 'Create Task')

function openCreate() {
  editId.value = null
  form.value = { name: '', gradingType: null, maxScore: null }
  dialog.value = true
}

function openEdit(item) {
  editId.value = item.id
  form.value = { name: item.name, gradingType: item.gradingType, maxScore: item.maxScore }
  dialog.value = true
}

async function save() {
  const { valid } = await formRef.value.validate()
  if (!valid) return
  saving.value = true
  try {
    if (editId.value) {
      await store.updateTask(disciplineId, editId.value, { ...form.value })
    } else {
      await store.createTask(disciplineId, { ...form.value })
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
    await store.deleteTask(disciplineId, deleteTarget.value.id)
    deleteDialog.value = false
  } finally {
    deleting.value = false
  }
}

async function movePriority(item, direction) {
  await store.changePriority(disciplineId, item.id, direction)
}
</script>
