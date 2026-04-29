<template>
  <div>
    <h1 class="text-h4 mb-4">Dashboard</h1>
    <v-row>
      <v-col cols="12" md="4">
        <v-card color="primary" variant="tonal">
          <v-card-title>Groups</v-card-title>
          <v-card-text class="text-h5">{{ groupCount }}</v-card-text>
          <v-card-subtitle>Total student groups</v-card-subtitle>
        </v-card>
      </v-col>
      <v-col cols="12" md="4">
        <v-card color="info" variant="tonal">
          <v-card-title>Disciplines</v-card-title>
          <v-card-text class="text-h5">{{ disciplineCount }}</v-card-text>
          <v-card-subtitle>Total disciplines</v-card-subtitle>
        </v-card>
      </v-col>
      <v-col cols="12" md="4">
        <v-card color="secondary" variant="tonal">
          <v-card-title>Current Year</v-card-title>
          <v-card-text class="text-h5">{{ currentYear }}</v-card-text>
          <v-card-subtitle>Active semester</v-card-subtitle>
        </v-card>
      </v-col>
      <v-col cols="12" md="4">
        <v-card color="success" variant="tonal">
          <v-card-title>System</v-card-title>
          <v-card-text class="text-h5">OK</v-card-text>
          <v-card-subtitle>API connected</v-card-subtitle>
        </v-card>
      </v-col>
    </v-row>
    <v-row class="mt-4">
      <v-col>
        <v-card>
          <v-card-title>Recent Groups</v-card-title>
          <v-card-text>
            <v-list v-if="recentGroups.length">
              <v-list-item v-for="g in recentGroups" :key="g.id">
                <template #prepend>
                  <v-icon>mdi-account-group</v-icon>
                </template>
                <v-list-item-title>{{ g.name }}</v-list-item-title>
                <v-list-item-subtitle>{{ g.shortName }} | {{ g.yearStarted }}</v-list-item-subtitle>
              </v-list-item>
            </v-list>
            <p v-else class="text-body-2 text-disabled">No groups yet.</p>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useGroupsStore } from '../stores/groups'
import { useDisciplinesStore } from '../stores/disciplines'

const groupsStore = useGroupsStore()
const disciplinesStore = useDisciplinesStore()

onMounted(() => {
  groupsStore.fetchGroups()
  disciplinesStore.fetchDisciplines()
})

const groupCount = computed(() => groupsStore.groups.length)
const disciplineCount = computed(() => disciplinesStore.disciplines.length)
const currentYear = computed(() => new Date().getFullYear())
const recentGroups = computed(() => groupsStore.groups.slice(0, 5))
</script>
