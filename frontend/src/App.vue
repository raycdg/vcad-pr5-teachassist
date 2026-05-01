<template>
  <v-app>
    <v-app-bar
      color="primary"
      density="compact"
    >
      <v-app-bar-title>TeachAssist</v-app-bar-title>
      <v-btn
        to="/"
        variant="text"
      >
        Dashboard
      </v-btn>
      <v-btn
        to="/groups"
        variant="text"
      >
        Groups
      </v-btn>
      <v-btn
        to="/disciplines"
        variant="text"
      >
        Disciplines
      </v-btn>
      <v-btn
        to="/courses"
        variant="text"
      >
        Courses
      </v-btn>
      <v-btn
        v-if="authStore.isAdmin"
        to="/admin/users"
        variant="text"
      >
        Users
      </v-btn>
      <v-spacer />
      <v-btn
        v-if="authStore.isLoggedIn"
        to="/profile"
        variant="text"
      >
        Profile
      </v-btn>
      <span
        v-if="authStore.email"
        class="text-subtitle-2 mr-2"
      >
        {{ authStore.email }} ({{ authStore.role }})
      </span>
      <v-btn
        v-if="authStore.isLoggedIn"
        variant="text"
        @click="handleLogout"
      >
        Logout
      </v-btn>
    </v-app-bar>
    <v-main>
      <v-container>
        <router-view />
      </v-container>
    </v-main>
  </v-app>
</template>

<script setup>
import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'

const router = useRouter()
const authStore = useAuthStore()

function handleLogout() {
  authStore.logout()
  router.push('/login')
}
</script>
