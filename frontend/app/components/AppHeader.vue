<template>
  <header class="header">
    <NuxtLink to="/" aria-label="Retour à l'accueil">
      <img src="~/assets/images/avanade-logo.png" alt="Avanade" class="logo" />
    </NuxtLink>

    <nav class="nav">
      <NuxtLink to="/" class="nav-link">Home</NuxtLink>
      <NuxtLink to="/about" class="nav-link">About</NuxtLink>
    </nav>
  </header>
</template>

<style scoped>
.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1.5rem;
  border-bottom: 1px solid var(--color-border);
}
.logo { height: 36px; }
.nav { display: flex; gap: 1.5rem; }
.nav-link { text-decoration: none; font-weight: 500; }
.nav-link.router-link-exact-active {
  color: var(--color-primary);
  border-bottom: 2px solid var(--color-primary);
}
</style>
