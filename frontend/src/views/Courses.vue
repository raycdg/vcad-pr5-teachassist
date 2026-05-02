<template>
  <div>
    <div class="d-flex align-center mb-4">
      <h1 class="text-h4">
        Courses
      </h1>
      <v-spacer />
      <v-btn
        variant="text"
        @click="toggleShowAll"
      >
        {{ showAll ? 'Hide inactive' : 'Show all courses' }}
      </v-btn>
      <v-btn
        v-if="authStore.isTeacher"
        color="primary"
        @click="openCreate"
      >
        Add Course
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

    <v-data-table
      :headers="headers"
      :items="store.courses"
      :loading="store.loading"
      item-value="id"
    >
      <template #item.isActive="{ item }">
        <v-chip
          :color="item.isActive ? 'green' : 'grey'"
          size="small"
        >
          {{ item.isActive ? 'Active' : 'Inactive' }}
        </v-chip>
      </template>
      <template #item.actions="{ item }">
        <v-btn
          icon
          size="small"
          variant="text"
          :to="`/courses/${item.id}/progress`"
        >
          <v-icon>mdi-table</v-icon>
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
          @click="toggleCourseStatus(item)"
        >
          <v-icon>{{ item.isActive ? 'mdi-close' : 'mdi-check' }}</v-icon>
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
            <v-select
              v-model="form.disciplineId"
              :items="disciplines"
              item-title="name"
              item-value="id"
              label="Discipline"
              :rules="[v => !!v || 'Required']"
            />
            <v-select
              v-model="form.groupId"
              :items="groups"
              item-title="name"
              item-value="id"
              label="Group"
              :rules="[v => !!v || 'Required']"
            />
            <v-text-field
              v-model.number="form.year"
              label="Year"
              type="number"
              :rules="[v => !!v || 'Required']"
            />
            <v-checkbox
              v-if="editId"
              v-model="form.isActive"
              label="Active"
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
        <v-card-title>Delete Course</v-card-title>
        <v-card-text>Are you sure you want to delete this course?</v-card-text>
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
import { useCoursesStore } from '../stores/courses'
import { useDisciplinesStore } from '../stores/disciplines'
import { useGroupsStore } from '../stores/groups'
import { useAuthStore } from '../stores/auth'

const store = useCoursesStore()
const disciplinesStore = useDisciplinesStore()
const groupsStore = useGroupsStore()
const authStore = useAuthStore()

const showAll = ref(false)
const dialog = ref(false)
const deleteDialog = ref(false)
const saving = ref(false)
const deleting = ref(false)
const formRef = ref(null)
const editId = ref(null)
const deleteTarget = ref(null)

const form = ref({ disciplineId: null, groupId: null, year: new Date().getFullYear(), isActive: true })

const disciplines = computed(() => disciplinesStore.disciplines)
const groups = computed(() => groupsStore.groups)

const headers = [
  { title: 'Discipline', key: 'disciplineName' },
  { title: 'Group', key: 'groupName' },
  { title: 'Year', key: 'year' },
  { title: 'Status', key: 'isActive' },
  { title: 'Actions', key: 'actions', sortable: false },
]

const formTitle = computed(() => editId.value ? 'Edit Course' : 'Add Course')

onMounted(async () => {
  await Promise.all([
    store.fetchCourses(showAll.value),
    disciplinesStore.fetchDisciplines(),
    groupsStore.fetchGroups(),
  ])
})

function toggleShowAll() {
  showAll.value = !showAll.value
  store.fetchCourses(showAll.value)
}

function openCreate() {
  editId.value = null
  form.value = { disciplineId: null, groupId: null, year: new Date().getFullYear(), isActive: true }
  dialog.value = true
}

function openEdit(item) {
  editId.value = item.id
  form.value = { disciplineId: item.disciplineId, groupId: item.groupId, year: item.year, isActive: item.isActive }
  dialog.value = true
}

async function save() {
  const { valid } = await formRef.value.validate()
  if (!valid) return
  saving.value = true
  try {
    if (editId.value) {
      await store.updateCourse(editId.value, { ...form.value })
    } else {
      await store.createCourse({ ...form.value })
    }
    dialog.value = false
    await store.fetchCourses(showAll.value)
  } finally { saving.value = false }
}

async function toggleCourseStatus(item) {
  await store.toggleStatus(item.id)
  await store.fetchCourses(showAll.value)
}

function confirmDelete(item) {
  deleteTarget.value = item
  deleteDialog.value = true
}

async function doDelete() {
  deleting.value = true
  try {
    await store.deleteCourse(deleteTarget.value.id)
    deleteDialog.value = false
    await store.fetchCourses(showAll.value)
  } finally { deleting.value = false }
}
</script>
