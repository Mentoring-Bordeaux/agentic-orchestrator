interface HelloResponse {
  message: string
}

// Relaye la demande du frontend vers l'API backend.
export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig(event)
  return await $fetch<HelloResponse[]>(`${config.backendApiBase}/api/hello`)
})
