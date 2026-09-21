using Wib.Api.Common;
using Wib.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddWibDatabase(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "Healthy",
    timeZone = WarsawTimeZone.TimeZoneId,
    utcNow = DateTime.UtcNow
}));

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
