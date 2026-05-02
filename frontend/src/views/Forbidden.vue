<template>
  <v-container
    class="d-flex flex-column align-center justify-center"
    style="min-height: 60vh"
  >
    <v-icon
      size="120"
      color="error"
      class="mb-6"
    >
      mdi-lock-alert
    </v-icon>
    <h1 class="text-h3 mb-4">
      403 - Access Forbidden
    </h1>
    <p class="text-h6 text-medium-emphasis mb-8">
      You don't have permission to access this page.
    </p>
    <v-btn
      color="primary"
      size="large"
      to="/"
    >
      Back to Dashboard
    </v-btn>
  </v-container>
</template>

<script setup>
</script>
