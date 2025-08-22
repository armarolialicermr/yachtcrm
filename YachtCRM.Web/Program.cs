using Microsoft.AspNetCore.Identity;
using YachtCRM.Domain;
using Microsoft.EntityFrameworkCore;
using YachtCRM.Infrastructure;
using YachtCRM.Application.Interfaces;
using YachtCRM.Application.Services;
using YachtCRM.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// DB
var cs = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=YachtCrm.db";
builder.Services.AddDbContext<YachtCrmDbContext>(opts => opts.UseSqlite(cs));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
}).AddRoles<IdentityRole>()
  .AddEntityFrameworkStores<YachtCrmDbContext>();

// MVC
builder.Services.AddControllersWithViews();

// DI
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Apply migrations & seed roles/admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<YachtCrmDbContext>();
    db.Database.Migrate();

    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    async Task EnsureRole(string role)
    {
        if (!await roleMgr.RoleExistsAsync(role)) await roleMgr.CreateAsync(new IdentityRole(role));
    }
    await EnsureRole("Admin"); await EnsureRole("Sales"); await EnsureRole("Service");

    var adminEmail = "admin@yachtcrm.local";
    var admin = await userMgr.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        await userMgr.CreateAsync(admin, "Admin!123");
        await userMgr.AddToRoleAsync(admin, "Admin");
    }

    // --- SAMPLE DATA (for demo) ---  <<< PASTE YOUR BLOCK HERE
    if (!db.Customers.Any())
    {
        var cust = new Customer { Name = "Cantieri Demo S.p.A.", Email = "info@cantieri-demo.it", Country = "Italy" };
        db.Customers.Add(cust);
        db.SaveChanges();

        var proj = new Project
        {
            Name = "Project Aurora",
            CustomerID = cust.CustomerID,
            YachtModelID = 1, // Ocean Explorer 45
            TotalPrice = 1500000m,
            PlannedStart = DateTime.UtcNow.Date.AddDays(-30),
            PlannedEnd   = DateTime.UtcNow.Date.AddDays(120),
            Status = "InProgress"
        };
        db.Projects.Add(proj);
        db.SaveChanges();

        db.ChangeRequests.Add(new ChangeRequest {
            ProjectID = proj.ProjectID,
            Title = "Interior wood upgrade",
            CostImpact = 25000m,
            DaysImpact = 10,
            Approved = true,
            ApprovedOn = DateTime.UtcNow
        });
        db.Tasks.Add(new CrmTask { ProjectID = proj.ProjectID, Title = "Hull paint prep", DueDate = DateTime.UtcNow.AddDays(14) });
        db.Interactions.Add(new Interaction { CustomerID = cust.CustomerID, ProjectID = proj.ProjectID, Type = "Meeting", Notes = "Kickoff with PM" });

        db.SaveChanges();
    }
}
// keep this AFTER the scope:
app.Run();
