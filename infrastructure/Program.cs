using Pulumi;
using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.ContainerRegistry.Inputs;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using System.Collections.Generic;

return await Pulumi.Deployment.RunAsync(() =>
{
    var config = new Config();
    var location = config.Require("location");
    var projectPrefix = config.Require("projectPrefix");
    var frontendName = config.Require("frontendName");
    var backendImage = config.Require("backendImage");
    var registryName = config.Require("registryName");
    var subscriptionId = new Config("azure-native").Require("subscriptionId");

    var resourceGroup = new ResourceGroup($"{projectPrefix}-rg", new ResourceGroupArgs
    {
        Location = location
    });

    var registry = new Pulumi.AzureNative.ContainerRegistry.Registry("backend-registry", new Pulumi.AzureNative.ContainerRegistry.RegistryArgs
    {
        RegistryName = registryName,
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        Sku = new Pulumi.AzureNative.ContainerRegistry.Inputs.SkuArgs
        {
            Name = "Basic"
        },
        AdminUserEnabled = false,
        AnonymousPullEnabled = false
    });

    var pullIdentityName = $"{projectPrefix}-backend-pull";
    var pullIdentity = new Pulumi.AzureNative.ManagedIdentity.UserAssignedIdentity("backend-pull-identity", new Pulumi.AzureNative.ManagedIdentity.UserAssignedIdentityArgs
    {
        ResourceName = pullIdentityName,
        ResourceGroupName = resourceGroup.Name,
        Location = location
    });

    var acrPullRoleAssignment = new RoleAssignment("backend-acr-pull", new RoleAssignmentArgs
    {
        RoleAssignmentName = "1f5d6020-9d70-4fd2-9bab-6d498cc2713b",
        PrincipalId = pullIdentity.PrincipalId,
        PrincipalType = "ServicePrincipal",
        RoleDefinitionId = $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/7f951dda-4ed3-4680-a7ca-43fe172d538d",
        Scope = registry.Id
    });

    var frontend = new StaticSite("frontend", new StaticSiteArgs
    {
        Name = frontendName,
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        Sku = new SkuDescriptionArgs
        {
            Name = "Free",
            Tier = "Free"
        }
    });

    var backendEnvironmentName = $"{projectPrefix}-backend-env";
    var backendEnvironment = new ManagedEnvironment("backend-environment", new ManagedEnvironmentArgs
    {
        EnvironmentName = backendEnvironmentName,
        ResourceGroupName = resourceGroup.Name,
        Location = location
    });

    var backend = new ContainerApp("backend", new ContainerAppArgs
    {
        ContainerAppName = $"{projectPrefix}-backend",
        ResourceGroupName = resourceGroup.Name,
        Location = location,
        EnvironmentId = backendEnvironment.Id,
        Identity = new Pulumi.AzureNative.App.Inputs.ManagedServiceIdentityArgs
        {
            Type = "UserAssigned",
            UserAssignedIdentities =
            {
                pullIdentity.Id
            }
        },
        Configuration = new ConfigurationArgs
        {
            Registries =
            {
                new RegistryCredentialsArgs
                {
                    Server = registry.LoginServer,
                    Identity = pullIdentity.Id
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
    }, new CustomResourceOptions
    {
        DependsOn = { acrPullRoleAssignment }
    });

    return new Dictionary<string, object?>
    {
        ["resourceGroupName"] = resourceGroup.Name,
        ["registryLoginServer"] = registry.LoginServer,
        ["acrImage"] = registry.LoginServer.Apply(server => $"{server}/agentic-orchestrator-api:dev"),
        ["frontendUrl"] = frontend.DefaultHostname.Apply(hostname => $"https://{hostname}"),
        ["backendUrl"] = backend.LatestRevisionFqdn.Apply(hostname => $"https://{hostname}")
    };
});
