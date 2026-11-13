using Pulumi;
using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using System.Collections.Generic;

return await Pulumi.Deployment.RunAsync(() =>
{
    // Require an explicit team-approved region; never silently choose a default.
    var config = new Config();
    var location = config.Require("location");
    // Azure Container Apps must pull a published image; supply its full registry path
    // (including tag) in stack configuration instead of hard-coding a placeholder.
    var backendImage = config.Require("backendImage");

    // Shared Azure resource group that contains the project's hosting resources.
    var resourceGroup = new ResourceGroup("agentic-orchestrator-rg", new ResourceGroupArgs
    {
        Location = location
    });

    // Create the Azure Static Web App that will host the generated Nuxt frontend.
    // The frontend build and upload are separate deployment steps; this resource
    // only provisions the Azure hosting service.
    var frontend = new StaticSite("agentic-orchestrator-frontend", new StaticSiteArgs
    {
        Name = "agentic-orchestrator-frontend",
        ResourceGroupName = resourceGroup.Name,
        Location = location,

        // Start with the Free tier to avoid introducing hosting charges by default.
        // Change this deliberately if the project needs features from another tier.
        Sku = new SkuDescriptionArgs
        {
            Name = "Free",
            Tier = "Free"
        }
    });

    // A managed environment provides the shared hosting boundary for Container Apps.
    // It is placed in the same region and resource group as the rest of this project.
    var backendEnvironment = new ManagedEnvironment("agentic-orchestrator-backend-env", new ManagedEnvironmentArgs
    {
        EnvironmentName = "agentic-orchestrator-backend-env",
        ResourceGroupName = resourceGroup.Name,
        Location = location
    });

    // Run the backend API image as an externally reachable Container App.
    // The Dockerfile exposes port 8080, so ingress forwards HTTPS requests there.
    var backend = new ContainerApp("agentic-orchestrator-backend", new ContainerAppArgs
    {
        ContainerAppName = "agentic-orchestrator-backend",
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        EnvironmentId = backendEnvironment.Id,
        Configuration = new ConfigurationArgs
        {
            // The frontend needs to call this API over the public HTTPS endpoint.
            Ingress = new IngressArgs
            {
                External = true,
                TargetPort = 8080,
                AllowInsecure = false
            }
        },
        Template = new TemplateArgs
        {
            Containers =
            {
                new ContainerArgs
                {
                    Name = "api",
                    Image = backendImage,
                    Resources = new ContainerResourcesArgs
                    {
                        Cpu = 0.5,
                        Memory = "1Gi"
                    }
                }
            },
            Scale = new ScaleArgs
            {
                // Allow the app to scale to zero when idle and cap the initial setup at one replica.
                MinReplicas = 0,
                MaxReplicas = 1
            }
        }
    });

    return new Dictionary<string, object?>
    {
        ["resourceGroupName"] = resourceGroup.Name,
        // Pulumi prints this URL after deployment so it is easy to open the frontend.
        ["frontendUrl"] = frontend.DefaultHostname.Apply(hostname => $"https://{hostname}"),
        // This is the HTTPS address the frontend can use to reach the backend API.
        ["backendUrl"] = backend.LatestRevisionFqdn.Apply(hostname => $"https://{hostname}")
    };
});
