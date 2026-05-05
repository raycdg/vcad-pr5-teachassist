<template>
  <div>
    <div class="d-flex align-center mb-4">
      <v-btn
        icon
        variant="text"
        :to="{ name: 'courses' }"
      >
        <v-icon>mdi-arrow-left</v-icon>
      </v-btn>
      <h1 class="text-h4 ml-2">
        {{ progress?.disciplineName }} — {{ progress?.groupName }}
      </h1>
      <v-spacer />
      <v-chip
        :color="progress?.isActive ? 'green' : 'grey'"
        class="mr-2"
      >
        {{ progress?.isActive ? 'Active' : 'Inactive' }}
      </v-chip>
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

    <div class="d-flex align-center mb-4">
      <v-text-field
        v-model="filter"
        label="Filter students..."
        prepend-inner-icon="mdi-magnify"
        variant="outlined"
        density="compact"
        hide-details
        style="max-width: 300px"
      />
      <v-spacer />
      <v-btn
        v-if="authStore.isTeacher"
        color="primary"
        :disabled="!progress?.isActive || !hasChanges"
        :loading="saving"
        @click="saveGrades"
      >
        Save
      </v-btn>
    </div>

    <div
      class="table-wrapper"
      style="overflow-x: auto; max-height: 70vh; overflow-y: auto;"
    >
      <table class="grade-table">
        <thead>
          <tr>
            <th class="sticky-col">
              Student
            </th>
            <th
              v-for="task in tasks"
              :key="task.id"
              class="text-center"
            >
              <div class="text-caption">
                {{ task.number }}
              </div>
              <div class="text-caption font-weight-bold">
                {{ task.name }}
              </div>
            </th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="student in filteredStudents"
            :key="student.id"
          >
            <td class="sticky-col">
              {{ student.lastName }} {{ student.firstName }}
            </td>
            <td
              v-for="task in tasks"
              :key="task.id"
              class="text-center"
            >
              <v-text-field
                v-model="grades[`${student.id}_${task.id}`]"
                :disabled="!progress?.isActive"
                :type="task.gradingType === 2 ? 'number' : 'text'"
                :rules="getGradeRules(task)"
                variant="outlined"
                density="compact"
                hide-details
                single-line
                style="max-width: 100px; margin: 0 auto;"
                @update:model-value="markChanged"
              />
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useCoursesStore } from '../stores/courses'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const store = useCoursesStore()
const authStore = useAuthStore()

const courseId = Number(route.params.id)
const progress = ref(null)
const filter = ref('')
const grades = ref({})
const originalGrades = ref({})
const hasChanges = ref(false)
const saving = ref(false)

const students = computed(() => progress.value?.students || [])
const tasks = computed(() => progress.value?.tasks || [])

const filteredStudents = computed(() => {
  if (!filter.value) return [...students.value].sort((a, b) => {
    const cmp = a.lastName.localeCompare(b.lastName)
    return cmp !== 0 ? cmp : a.firstName.localeCompare(b.firstName)
  })
  const q = filter.value.toLowerCase()
  return [...students.value].filter(s =>
    s.firstName.toLowerCase().includes(q) || s.lastName.toLowerCase().includes(q)
  ).sort((a, b) => {
    const cmp = a.lastName.localeCompare(b.lastName)
    return cmp !== 0 ? cmp : a.firstName.localeCompare(b.firstName)
  })
})

function markChanged() {
  hasChanges.value = JSON.stringify(grades.value) !== JSON.stringify(originalGrades.value)
}

function getGradeRules(task) {
  if (!task) return []
  if (task.gradingType === 1) {
    return [v => v === '' || v === '0' || v === '1' || v === 0 || v === 1 || 'Must be 0 or 1']
  }
  if (task.gradingType === 2) {
    const max = task.maxScore ?? 0
    return [
      v => v === '' || (Number.isInteger(Number(v)) && Number(v) >= 0 && Number(v) <= max) || `Must be 0-${max}`
    ]
  }
  return []
}

onMounted(async () => {
  const data = await store.fetchProgress(courseId)
  progress.value = data

  const gradeEntries = {}
  if (data.students && data.tasks) {
    for (const s of data.students) {
      for (const t of data.tasks) {
        const key = `${s.id}_${t.id}`
        gradeEntries[key] = data.grades?.[key] ?? ''
      }
    }
  }
  grades.value = { ...gradeEntries }
  originalGrades.value = { ...gradeEntries }
  hasChanges.value = false
})

async function saveGrades() {
  saving.value = true
  try {
    const entries = Object.entries(grades.value)
      .filter(([key, v]) => v !== '' && v !== originalGrades.value[key])
      .map(([key, value]) => {
        const [studentId, taskId] = key.split('_').map(Number)
        return { studentId, taskId, value }
      })
    if (entries.length === 0) return
    await store.saveGrades(courseId, entries)
    originalGrades.value = { ...grades.value }
    hasChanges.value = false
  } finally { saving.value = false }
}
</script>

<style scoped>
.grade-table {
  border-collapse: collapse;
  width: 100%;
  min-width: 600px;
}

.grade-table th,
.grade-table td {
  border: 1px solid #ddd;
  padding: 8px;
  background: white;
}

.grade-table th {
  background: #f5f5f5;
  position: sticky;
  top: 0;
  z-index: 2;
}

.sticky-col {
  position: sticky;
  left: 0;
  z-index: 3;
  font-weight: 500;
  min-width: 200px;
}

.grade-table th.sticky-col {
  z-index: 4;
}
</style>
