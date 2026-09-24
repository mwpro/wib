using Wib.Api.Auth;
using Wib.Api.Common;
using Wib.Api.Config;
using Wib.Api.Data;
using Wib.Api.Members;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("/run/secrets/appsettings.secret.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.Configure<JwtAuthOptions>(builder.Configuration.GetSection(JwtAuthOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddWibAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddWibDatabase(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapConfigEndpoints();
app.MapMemberEndpoints();

app.MapHealthChecks("/api/health");

app.MapFallbackToFile("index.html");

if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        app.ApplyDatabaseMigrations();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Could not apply database migrations on startup. If running without MySQL, ensure DB is running.");
    }
}

app.Run();

public partial class Program { }
