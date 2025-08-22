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
                .Include(p => p.YachtModel)
                .Include(p => p.Tasks)
                .Include(p => p.ChangeRequests)
                .Include(p => p.Interactions)
                .Select(p => new
                {
                    p.ProjectID,
                    p.TotalPrice,
                    p.PlannedStart,
                    p.PlannedEnd,
                    p.ActualStart,
                    p.ActualEnd,
                    Length = p.YachtModel.Length,
                    BasePrice = p.YachtModel.BasePrice,
                    ChangeRequests = p.ChangeRequests.Count,
                    Tasks = p.Tasks.Count,
                    Interactions = p.Interactions.Count,
                    DaysLate = p.ActualEnd == null
                        ? (int?)null
                        : (int?)(p.ActualEnd.Value.Date - p.PlannedEnd.Date).TotalDays,
                    DeliveredLate = p.ActualEnd != null && p.ActualEnd > p.PlannedEnd ? 1 : 0
                })
                .ToListAsync();

            // CSV builder (with quoting + invariant culture)
            static string Q(string? s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }

            var sb = new StringBuilder();

            // Header
            sb.AppendLine(string.Join(",",
                "ProjectID","TotalPrice","PlannedStart","PlannedEnd","ActualStart","ActualEnd",
                "Length","BasePrice","ChangeRequests","Tasks","Interactions","DaysLate","DeliveredLate"));

            foreach (var r in rows)
            {
                var line = string.Join(",",
                    r.ProjectID.ToString(CultureInfo.InvariantCulture),
                    r.TotalPrice.ToString(CultureInfo.InvariantCulture),
                    Q(r.PlannedStart.ToString("O", CultureInfo.InvariantCulture)),
                    Q(r.PlannedEnd.ToString("O", CultureInfo.InvariantCulture)),
                    Q(r.ActualStart?.ToString("O", CultureInfo.InvariantCulture)),
                    Q(r.ActualEnd?.ToString("O", CultureInfo.InvariantCulture)),
                    r.Length.ToString(CultureInfo.InvariantCulture),
                    r.BasePrice.ToString(CultureInfo.InvariantCulture),
                    r.ChangeRequests.ToString(CultureInfo.InvariantCulture),
                    r.Tasks.ToString(CultureInfo.InvariantCulture),
                    r.Interactions.ToString(CultureInfo.InvariantCulture),
                    r.DaysLate?.ToString(CultureInfo.InvariantCulture) ?? "",
                    r.DeliveredLate.ToString(CultureInfo.InvariantCulture)
                );
                sb.AppendLine(line);
            }

            // Prepend UTF-8 BOM for Excel on macOS/Windows
            var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var bytes = utf8WithBom.GetBytes(sb.ToString());

            return File(bytes, "text/csv", "projects.csv");
        }
    }
}


