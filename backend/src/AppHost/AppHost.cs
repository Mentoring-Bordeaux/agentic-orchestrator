var builder = DistributedApplication.CreateBuilder(args);
var api = builder.AddProject<Projects.Api>("api");


builder.AddJavaScriptApp("frontend", "../../../frontend", "dev")
       .WithHttpEndpoint(env: "PORT")
       .WithEnvironment("NUXT_PUBLIC_API_BASE", api.GetEndpoint("https"))
       .WithReference(api)
       .WaitFor(api);

builder.Build().Run();
