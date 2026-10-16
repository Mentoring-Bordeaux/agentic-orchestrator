<template>
  <main>
    <NuxtRouteAnnouncer />

    <p v-if="pending" role="status">Chargement du message…</p>
    <p v-else-if="error" role="alert">Impossible de contacter le backend : {{ error.message }}</p>
    <ul v-else-if="messages?.length">
      <li v-for="(item, index) in messages" :key="index">{{ item.message }}</li>
    </ul>
    <p v-else>Aucun message reçu.</p>
  </main>
</template>


//C’est la partie qui récupère les données avant l’affichage.
<script setup lang="ts">
// Représente un élément du tableau renvoyé par l'API.
interface HelloResponse {
  message: string
}

// Récupère les messages via la route serveur Nuxt.
const { data: messages, pending, error } = await useFetch<HelloResponse[]>('/api/hello')
</script>
