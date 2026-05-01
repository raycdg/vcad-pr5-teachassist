<template>
  <v-container
    class="fill-height d-flex align-center justify-center"
    fluid
  >
    <v-card
      max-width="400"
      width="100%"
      class="pa-6"
    >
      <v-card-title class="text-h5 text-center">
        TeachAssist Login
      </v-card-title>

      <v-form
        ref="form"
        @submit.prevent="handleLogin"
      >
        <v-text-field
          v-model="email"
          label="Email"
          type="email"
          :rules="emailRules"
          required
          variant="outlined"
          class="mt-4"
        />

        <v-text-field
          v-model="password"
          label="Password"
          type="password"
          :rules="passwordRules"
          required
          variant="outlined"
        />

        <v-alert
          v-if="authStore.error"
          type="error"
          variant="tonal"
          class="mb-4"
        >
          {{ authStore.error }}
        </v-alert>

        <v-btn
          type="submit"
          color="primary"
          block
          size="large"
          :loading="authStore.loading"
        >
          Login
        </v-btn>
      </v-form>
    </v-card>
  </v-container>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const email = ref('')
const password = ref('')
const form = ref(null)

const emailRules = [
  (v) => !!v || 'Email is required',
  (v) => /.+@.+\..+/.test(v) || 'Email must be valid',
]

const passwordRules = [
  (v) => !!v || 'Password is required',
]

async function handleLogin() {
  const { valid } = await form.value.validate()
  if (!valid) return

  try {
    await authStore.login(email.value, password.value)
    router.push('/')
  } catch {
    // error handled by store
  }
}
</script>
