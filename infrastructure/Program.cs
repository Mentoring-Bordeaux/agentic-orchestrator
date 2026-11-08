using Pulumi;
using Pulumi.AzureNative.Resources;
using System.Collections.Generic;

return await Pulumi.Deployment.RunAsync(() =>
{
    // Require an explicit team-approved region; never silently choose a default.
    var config = new Config();
    var location = config.Require("location");

    // First resource for issue #4. Other resources will be added only after
    // the backend image and deployment settings are confirmed.
    var resourceGroup = new ResourceGroup("agentic-orchestrator-rg", new ResourceGroupArgs
    {
        Location = location
    });

    return new Dictionary<string, object?>
    {
        ["resourceGroupName"] = resourceGroup.Name
    };
});
