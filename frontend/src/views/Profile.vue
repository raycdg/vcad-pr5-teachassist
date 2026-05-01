<template>
  <v-container
    class="fill-height align-start justify-center"
    fluid
  >
    <v-card
      max-width="600"
      width="100%"
      class="pa-6"
    >
      <v-card-title class="text-h5 text-center mb-4">
        My Profile
      </v-card-title>

      <v-alert
        v-if="successMessage"
        type="success"
        variant="tonal"
        class="mb-4"
      >
        {{ successMessage }}
      </v-alert>

      <v-alert
        v-if="errorMessage"
        type="error"
        variant="tonal"
        class="mb-4"
      >
        {{ errorMessage }}
      </v-alert>

      <!-- Profile Info -->
      <div
        v-if="profile"
        class="mb-6"
      >
        <p><strong>Email:</strong> {{ profile.email }}</p>
        <p><strong>Roles:</strong> {{ profile.roles.join(', ') }}</p>
      </div>

      <v-divider class="mb-6" />

      <!-- Change Email -->
      <h3
        class="text-h6 mb-4"
      >
        Change Email
      </h3>
      <v-form
        ref="emailForm"
        @submit.prevent="handleChangeEmail"
      >
        <v-text-field
          v-model="newEmail"
          label="New Email"
          type="email"
          :rules="emailRules"
          required
          variant="outlined"
          class="mb-4"
        />
        <v-text-field
          v-model="emailPassword"
          label="Current Password"
          type="password"
          :rules="passwordRequiredRules"
          required
          variant="outlined"
          class="mb-4"
        />
        <v-btn
          type="submit"
          color="primary"
          block
          :loading="loadingEmail"
        >
          Change Email
        </v-btn>
      </v-form>

      <v-divider class="my-6" />

      <!-- Change Password -->
      <h3
        class="text-h6 mb-4"
      >
        Change Password
      </h3>
      <v-form
        ref="passwordForm"
        @submit.prevent="handleChangePassword"
      >
        <v-text-field
          v-model="oldPassword"
          label="Old Password"
          type="password"
          :rules="passwordRequiredRules"
          required
          variant="outlined"
          class="mb-4"
        />
        <v-text-field
          v-model="newPassword"
          label="New Password"
          type="password"
          :rules="passwordRules"
          required
          variant="outlined"
          class="mb-4"
        />
        <v-text-field
          v-model="confirmPassword"
          label="Confirm New Password"
          type="password"
          :rules="confirmPasswordRules"
          required
          variant="outlined"
          class="mb-4"
        />
        <v-btn
          type="submit"
          color="primary"
          block
          :loading="loadingPassword"
        >
          Change Password
        </v-btn>
      </v-form>
    </v-card>
  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

const profile = ref(null)
const successMessage = ref('')
const errorMessage = ref('')

const newEmail = ref('')
const emailPassword = ref('')
const loadingEmail = ref(false)
const emailForm = ref(null)

const oldPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const loadingPassword = ref(false)
const passwordForm = ref(null)

const emailRules = [
  (v) => !!v || 'Email is required',
  (v) => /.+@.+\..+/.test(v) || 'Email must be valid',
]

const passwordRequiredRules = [
  (v) => !!v || 'Password is required',
]

const passwordRules = [
  (v) => !!v || 'Password is required',
  (v) => v.length >= 4 || 'Password must be at least 4 characters',
]

const confirmPasswordRules = [
  (v) => !!v || 'Please confirm password',
  (v) => v === newPassword.value || 'Passwords do not match',
]

async function fetchProfile() {
  try {
    const res = await axios.get('/api/account/profile')
    profile.value = res.data
  } catch (err) {
    errorMessage.value = err.response?.data?.message || 'Failed to load profile'
  }
}

async function handleChangeEmail() {
  const { valid } = await emailForm.value.validate()
  if (!valid) return

  loadingEmail.value = true
  successMessage.value = ''
  errorMessage.value = ''
  try {
    await axios.put('/api/account/email', {
      newEmail: newEmail.value,
      password: emailPassword.value,
    })
    successMessage.value = 'Email updated successfully'
    newEmail.value = ''
    emailPassword.value = ''
    emailForm.value.reset()
    await fetchProfile()
  } catch (err) {
    errorMessage.value = err.response?.data?.message || 'Failed to change email'
  } finally {
    loadingEmail.value = false
  }
}

async function handleChangePassword() {
  const { valid } = await passwordForm.value.validate()
  if (!valid) return

  loadingPassword.value = true
  successMessage.value = ''
  errorMessage.value = ''
  try {
    await axios.put('/api/account/password', {
      oldPassword: oldPassword.value,
      newPassword: newPassword.value,
    })
    successMessage.value = 'Password changed successfully'
    oldPassword.value = ''
    newPassword.value = ''
    confirmPassword.value = ''
    passwordForm.value.reset()
  } catch (err) {
    errorMessage.value = err.response?.data?.message || 'Failed to change password'
  } finally {
    loadingPassword.value = false
  }
}

onMounted(fetchProfile)
</script>
