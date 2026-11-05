using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Show the XML summaries / response codes written on every endpoint in Swagger UI
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});

builder.Services.AddScoped<BlobStorageService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDistributedMemoryCache();

// Session management keeps the user's id and role between requests
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.API.Session";
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Create / update the database automatically so the API works on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (SqlException ex) when (ex.Number == 1801)
    {
        // Another API instance may have created the database after this instance checked.
        app.Logger.LogWarning(ex, "Database creation raced with another API instance; retrying migrations.");
        try
        {
            db.Database.Migrate();
        }
        catch (Exception retryEx)
        {
            app.Logger.LogError(retryEx, "Database migration retry failed. Check the DefaultConnection string.");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database migration failed. Check the DefaultConnection string.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseSession();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Needed so the test project can reference the entry point type
public partial class Program { }
