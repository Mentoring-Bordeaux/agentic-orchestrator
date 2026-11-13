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
named `location` and a published backend image reference in `backendImage`. For
example, after the image has been pushed to a registry:

```powershell
pulumi config set location <team-approved-region>
pulumi config set backendImage <registry>/<repository>:<tag>
```

Do not use an assumed default region or a local-only Docker tag: Azure must be
able to pull the image from the configured registry. The current program has not
deployed anything to Azure. Private registry credentials or managed identity
permissions still need to be configured before the backend can start.

Before any deployment, review the Pulumi preview with the team and confirm the
subscription, spending limit, Nuxt 4 deployment mode, registry, and permissions
for Container Apps to pull the backend image.
