using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using YachtCRM.Infrastructure;

namespace YachtCRM.Web.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly YachtCrmDbContext _db;
        public AnalyticsController(YachtCrmDbContext db) => _db = db;

        [HttpGet("/analytics/export/projects.csv")]
        public async Task<IActionResult> ExportProjects()
        {
            var rows = await _db.Projects
                .Include(p => p.Customer)
                .Include(p => p.YachtModel)
                .Include(p => p.Tasks)
                .Include(p => p.ChangeRequests)
                .Include(p => p.Interactions)
                .Select(p => new
                {
                    p.ProjectID,
                    p.Name,
                    Customer = p.Customer.Name,
                    ModelName = p.YachtModel.ModelName,
                    Length = p.YachtModel.Length,
                    BasePrice = p.YachtModel.BasePrice,
                    TotalPrice = p.TotalPrice,
                    PlannedStart = p.PlannedStart,
                    PlannedEnd = p.PlannedEnd,
                    ActualStart = p.ActualStart,
                    ActualEnd = p.ActualEnd,
                    PlannedDurationDays = (int)(p.PlannedEnd - p.PlannedStart).TotalDays,
                    ActualDurationDays = p.ActualEnd == null ? (int?)null : (int?)(p.ActualEnd.Value - (p.ActualStart ?? p.PlannedStart)).TotalDays,
                    Tasks = p.Tasks.Count,
                    ChangeRequests = p.ChangeRequests.Count,
                    Interactions = p.Interactions.Count,
                    DeliveredLate = p.ActualEnd != null && p.ActualEnd > p.PlannedEnd ? 1 : 0,
                    DaysLate = p.ActualEnd == null ? (int?)null : (int?)(p.ActualEnd.Value.Date - p.PlannedEnd.Date).TotalDays,
                    IsCustom = p.Name.Contains("Custom") ? 1 : 0
                })
                .ToListAsync();

            // CSV helper: quote strings and escape quotes
            static string Q(string? s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }

            var sb = new StringBuilder();

            // Header
            sb.AppendLine(string.Join(",",
                "ProjectID","Name","Customer","ModelName",
                "Length","BasePrice","TotalPrice",
                "PlannedStart","PlannedEnd","ActualStart","ActualEnd",
                "PlannedDurationDays","ActualDurationDays",
                "Tasks","ChangeRequests","Interactions",
                "DeliveredLate","DaysLate","IsCustom"));

            // Rows
            foreach (var r in rows)
            {
                var line = string.Join(",",
                    r.ProjectID.ToString(CultureInfo.InvariantCulture),
                    Q(r.Name),
                    Q(r.Customer),
                    Q(r.ModelName),
                    r.Length.ToString(CultureInfo.InvariantCulture),
                    r.BasePrice.ToString(CultureInfo.InvariantCulture),
                    r.TotalPrice.ToString(CultureInfo.InvariantCulture),
                    Q(r.PlannedStart.ToString("O", CultureInfo.InvariantCulture)),
                    Q(r.PlannedEnd.ToString("O", CultureInfo.InvariantCulture)),
                    Q(r.ActualStart?.ToString("O", CultureInfo.InvariantCulture)),
                    Q(r.ActualEnd?.ToString("O", CultureInfo.InvariantCulture)),
                    r.PlannedDurationDays.ToString(CultureInfo.InvariantCulture),
                    r.ActualDurationDays?.ToString(CultureInfo.InvariantCulture) ?? "",
                    r.Tasks.ToString(CultureInfo.InvariantCulture),
                    r.ChangeRequests.ToString(CultureInfo.InvariantCulture),
                    r.Interactions.ToString(CultureInfo.InvariantCulture),
                    r.DeliveredLate.ToString(CultureInfo.InvariantCulture),
                    r.DaysLate?.ToString(CultureInfo.InvariantCulture) ?? "",
                    r.IsCustom.ToString(CultureInfo.InvariantCulture)
                );
                sb.AppendLine(line);
            }

            // Excel-friendly BOM
            var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var bytes = utf8WithBom.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "projects.csv");
        }
    }
}



