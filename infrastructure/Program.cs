using Pulumi;
using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
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
    var registryServer = config.Require("registryServer");
    var registryUsername = config.Require("registryUsername");
    var registryPassword = config.RequireSecret("registryPassword");

    var resourceGroup = new ResourceGroup($"{projectPrefix}-rg", new ResourceGroupArgs
    {
        Location = location
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
        Configuration = new ConfigurationArgs
        {
            Secrets =
            {
                new SecretArgs
                {
                    Name = "acr-password",
                    Value = registryPassword
                }
            },
            Registries =
            {
                new RegistryCredentialsArgs
                {
                    Server = registryServer,
                    Username = registryUsername,
                    PasswordSecretRef = "acr-password"
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

    return new Dictionary<string, object?>
    {
        ["resourceGroupName"] = resourceGroup.Name,
        ["frontendUrl"] = frontend.DefaultHostname.Apply(hostname => $"https://{hostname}"),
        ["backendUrl"] = backend.LatestRevisionFqdn.Apply(hostname => $"https://{hostname}")
    };
});
