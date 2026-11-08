# Infrastructure as Code

This Pulumi C# project is the starting point for issue #4. It currently describes
only the Azure Resource Group. The Azure Static Web App, Azure Container Apps
environment, and backend Container App will be added after the deployment details
and image-pull permissions are confirmed.

## Backend image

The API Dockerfile is `backend/src/Api/Dockerfile`. From the `backend` directory,
build a local image with:

```powershell
docker build -f src/Api/Dockerfile -t agentic-orchestrator-api:dev .
```

This creates a local image only. It does not upload it to Azure. A container
registry and a pipeline or another approved process are needed before Azure
Container Apps can pull and run the image.

## Pulumi settings

The program requires a team-approved Azure region in the Pulumi stack setting
named `location`. Do not use an assumed default region. The current program has
not deployed anything to Azure.

Before any deployment, review the Pulumi preview with the team and confirm the
subscription, spending limit, Nuxt 4 deployment mode, registry, and permissions
for Container Apps to pull the backend image.
