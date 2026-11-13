// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
  nitro: {
    preset: 'azure_swa',
    azure: { config: { platform: { apiRuntime: 'node:22' } } }
  },
  runtimeConfig: { backendApiBase: '' }
})
