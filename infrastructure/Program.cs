using Pulumi;
using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
using Pulumi.AzureNative.ManagedIdentity;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using System.Collections.Generic;

return await Pulumi.Deployment.RunAsync(() =>
{
    var config = new Config();
    var location = config.Require("location");
    var frontendLocation = config.Get("frontendLocation") ?? location;
    var projectPrefix = config.Require("projectPrefix");
    var resourceGroupName = config.Get("resourceGroupName") ?? $"{projectPrefix}-rg";
    var frontendName = config.Require("frontendName");
    var frontendSku = config.Get("frontendSku") ?? "Free";
    var backendImage = config.Get("backendImage");

    var resourceGroup = new ResourceGroup(resourceGroupName, new ResourceGroupArgs
    {
        Location = location
    });

    var frontend = new StaticSite("frontend", new StaticSiteArgs
    {
        Name = frontendName,
        ResourceGroupName = resourceGroup.Name,
        Location = frontendLocation,
        Sku = new SkuDescriptionArgs
        {
            Name = frontendSku,
            Tier = frontendSku
        }
    });

    var backendEnvironmentName = $"{projectPrefix}-backend-env";
    var backendEnvironment = new ManagedEnvironment("backend-environment", new ManagedEnvironmentArgs
    {
        EnvironmentName = backendEnvironmentName,
        ResourceGroupName = resourceGroup.Name,
        Location = location
    });

    // Use a user-assigned identity so an administrator can grant it AcrPull
    // before the Container App tries to download its private image.
    var imagePullIdentityName = $"{projectPrefix}-backend-pull";
    var imagePullIdentity = new UserAssignedIdentity("backend-pull-identity", new UserAssignedIdentityArgs
    {
        ResourceName = imagePullIdentityName,
        ResourceGroupName = resourceGroup.Name,
        Location = location
    });

    var outputs = new Dictionary<string, object?>
    {
        ["resourceGroupName"] = resourceGroup.Name,
        ["frontendUrl"] = frontend.DefaultHostname.Apply(hostname => $"https://{hostname}"),
        ["imagePullIdentityPrincipalId"] = imagePullIdentity.PrincipalId,
        ["backendEnvironmentName"] = backendEnvironmentName
    };

    // The image must already exist in the team's ACR and AcrPull must be
    // granted to the identity before this resource is created. This allows the
    // first Pulumi deployment to create the identity without a fake image.
    if (!string.IsNullOrWhiteSpace(backendImage))
    {
        var registryServer = config.Require("registryServer");
        var backendName = $"{projectPrefix}-backend";

        var backend = new ContainerApp("backend", new ContainerAppArgs
        {
            ContainerAppName = backendName,
            ResourceGroupName = resourceGroup.Name,
            Location = location,
            EnvironmentId = backendEnvironment.Id,
            Identity = new Pulumi.AzureNative.App.Inputs.ManagedServiceIdentityArgs
            {
                Type = "UserAssigned",
                UserAssignedIdentities =
                {
                    imagePullIdentity.Id
                }
            },
            Configuration = new ConfigurationArgs
            {
                IdentitySettings =
                {
                    new IdentitySettingsArgs
                    {
                        Identity = imagePullIdentity.Id,
                        Lifecycle = "None"
                    }
                },
                Registries =
                {
                    new RegistryCredentialsArgs
                    {
                        Server = registryServer,
                        Identity = imagePullIdentity.Id
                    }
                },
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
                    MinReplicas = 0,
                    MaxReplicas = 1
                }
            }
        });

        outputs["backendUrl"] = backend.LatestRevisionFqdn.Apply(hostname => $"https://{hostname}");
    }

    return outputs;
});
