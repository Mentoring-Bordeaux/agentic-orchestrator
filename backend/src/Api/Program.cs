var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Renvoie un message simple au frontend.
app.MapGet("/api/hello", () =>
{
    return new[]
    {
        new { message = "Hello from backend" }
    };
})
.WithName("GetHello");

app.Run();
