Clean Start: Yacht Shipyard CRM (demo-ready + regression hooks)

1) Create the solution and add projects (from this folder):
   dotnet new sln -n YachtCRM.Clean
   dotnet sln add YachtCRM.Domain/YachtCRM.Domain.csproj
   dotnet sln add YachtCRM.Application/YachtCRM.Application.csproj
   dotnet sln add YachtCRM.Infrastructure/YachtCRM.Infrastructure.csproj
   dotnet sln add YachtCRM.Web/YachtCRM.Web.csproj

2) Restore packages
   dotnet restore

3) Install EF CLI (if needed)
   dotnet tool update -g dotnet-ef

4) Create initial migration and update DB
   dotnet ef migrations add Init -p YachtCRM.Infrastructure/YachtCRM.Infrastructure.csproj -s YachtCRM.Web/YachtCRM.Web.csproj -o Migrations
   dotnet ef database update -p YachtCRM.Infrastructure/YachtCRM.Infrastructure.csproj -s YachtCRM.Web/YachtCRM.Web.csproj

5) Run
   dotnet run --project YachtCRM.Web/YachtCRM.Web.csproj

Login:
- Admin auto-created at first run:
  Email: admin@yachtcrm.local
  Password: Admin!123

Analytics:
- Export CSV: /analytics/export/projects.csv
Predictions:
- Project details call: /projects/{id}/prediction (heuristic now; swap with ML.NET)

Next steps:
- Add controllers for Customers/Interactions, or seed sample data.
- Replace PredictionService with ML.NET model loader for your dissertation.

Built on 2025-08-22.
