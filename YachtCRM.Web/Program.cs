using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YachtCRM.Infrastructure;
using YachtCRM.Application.Interfaces;
using YachtCRM.Application.Services;
using YachtCRM.Infrastructure.Services;
using YachtCRM.Web.Seed; // <-- needed for BigSeeder

var builder = WebApplication.CreateBuilder(args);

// SQLite connection (Render overrides via env var)
builder.Services.AddDbContext<YachtCrmDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=app_dev.db"));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<YachtCrmDbContext>();

builder.Services.AddControllersWithViews();

// DI
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<IPredictionService, MlDelayPredictionService>();
builder.Services.AddScoped<MlDelayPredictionService>();

var app = builder.Build();

// --- Migrate & optional seed ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<YachtCrmDbContext>();
    db.Database.Migrate();

    // SEED ON BOOT (optional): if env SEED_ON_BOOT is set, and DB empty, seed N rows
    var seedEnv = Environment.GetEnvironmentVariable("SEED_ON_BOOT");
    if (!string.IsNullOrWhiteSpace(seedEnv))
    {
        var hasAny = db.Projects.Any();
        if (!hasAny)
        {
            if (!int.TryParse(seedEnv, out var n)) n = 500;
            try
            {
                // Create realistic synthetic dataset so the charts/ML have data
                BigSeeder.GenerateAsync(db, count: n, startYear: 2022).GetAwaiter().GetResult();
            }
            catch { /* ignore seeding errors to avoid blocking boot */ }
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/healthz", () => Results.Ok(new { ok = true, env = app.Environment.EnvironmentName }));

app.Run();

