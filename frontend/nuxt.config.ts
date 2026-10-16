// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
  runtimeConfig: {
    // Adresse de l'API backend, qui sera utilisée dans le code du frontend pour effectuer des requêtes HTTP.
    backendApiBase: 'http://localhost:5149'
  }
})
