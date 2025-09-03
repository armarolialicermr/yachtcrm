using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YachtCRM.Infrastructure;
using YachtCRM.Infrastructure.Services; // MlDelayPredictionService

namespace YachtCRM.Web.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly YachtCrmDbContext _db;
        public AnalyticsController(YachtCrmDbContext db) => _db = db;

        // -------- Small helper --------
        private static string Q(string? s) => string.IsNullOrEmpty(s) ? "" : "\"" + s.Replace("\"", "\"\"") + "\"";

        // ================= CSV EXPORTS =================
        [HttpGet("/analytics/export/projects.csv")]
        public async Task<IActionResult> ExportProjects()
        {
            var rows = await _db.Projects
                .Include(p => p.Customer)
                .Include(p => p.YachtModel)
                .Include(p => p.Tasks)
                .Include(p => p.ChangeRequests)
                .Include(p => p.Interactions)
                .AsNoTracking()
                .Select(p => new
                {
                    p.ProjectID,
                    p.Name,
                    Customer = p.Customer != null ? p.Customer.Name : "",
                    ModelName = p.YachtModel != null ? p.YachtModel.ModelName : "",
                    Length = p.YachtModel != null ? (double?)p.YachtModel.Length : null,
                    BasePrice = p.YachtModel != null ? (decimal?)p.YachtModel.BasePrice : null,
                    TotalPrice = p.TotalPrice,
                    PlannedStart = p.PlannedStart,
                    PlannedEnd = p.PlannedEnd,
                    ActualStart = p.ActualStart,
                    ActualEnd = p.ActualEnd,
                    PlannedDurationDays = (int)(p.PlannedEnd - p.PlannedStart).TotalDays,
                    ActualDurationDays = p.ActualEnd == null
                        ? (int?)null
                        : (int?)((p.ActualEnd.Value - (p.ActualStart ?? p.PlannedStart)).TotalDays),
                    Tasks = p.Tasks.Count,
                    ChangeRequests = p.ChangeRequests.Count,
                    Interactions = p.Interactions.Count,
                    DeliveredLate = p.ActualEnd != null && p.ActualEnd > p.PlannedEnd ? 1 : 0,
                    DaysLate = p.ActualEnd == null
                        ? (int?)null
                        : (int?)(p.ActualEnd.Value.Date - p.PlannedEnd.Date).TotalDays,
                    IsCustom = p.Name.Contains("Custom") ? 1 : 0
                })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",",
                "ProjectID", "Name", "Customer", "ModelName",
                "Length", "BasePrice", "TotalPrice",
                "PlannedStart", "PlannedEnd", "ActualStart", "ActualEnd",
                "PlannedDurationDays", "ActualDurationDays",
                "Tasks", "ChangeRequests", "Interactions",
                "DeliveredLate", "DaysLate", "IsCustom"));

            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",",
                    r.ProjectID.ToString(CultureInfo.InvariantCulture),
                    Q(r.Name),
                    Q(r.Customer),
                    Q(r.ModelName),
                    r.Length?.ToString(CultureInfo.InvariantCulture) ?? "",
                    r.BasePrice?.ToString(CultureInfo.InvariantCulture) ?? "",
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
                ));
            }

            var bytes = new UTF8Encoding(true).GetBytes(sb.ToString());
            return File(bytes, "text/csv", "projects.csv");
        }

        [HttpGet("/analytics/export/service.csv")]
        public async Task<IActionResult> ExportService()
        {
            var rows = await _db.ServiceRequests
                .Include(s => s.Project!)       // tell compiler it's not null
                .ThenInclude(p => p!.Customer)  // same here for Customer
                .ToListAsync();


            var sb = new StringBuilder();
            sb.AppendLine("ServiceRequestID,Project,Customer,Title,Type,Status,RequestDate,CompletedOn,AgeDays");

            foreach (var s in rows)
            {
                var projName = s.Project?.Name ?? "";
                var custName = s.Project?.Customer?.Name ?? "";
                var title = s.Title ?? "";
                var type = s.Type ?? "";
                var status = s.Status ?? "";

                int? ageDays = s.CompletedOn.HasValue
                    ? (int?)Math.Round((s.CompletedOn.Value - s.RequestDate).TotalDays)
                    : null;

                sb.AppendLine(string.Join(",",
                    s.ServiceRequestID.ToString(CultureInfo.InvariantCulture),
                    Q(projName),
                    Q(custName),
                    Q(title),
                    type,
                    status,
                    s.RequestDate.ToString("O", CultureInfo.InvariantCulture),
                    s.CompletedOn.HasValue ? s.CompletedOn.Value.ToString("O", CultureInfo.InvariantCulture) : "",
                    ageDays?.ToString(CultureInfo.InvariantCulture) ?? ""
                ));
            }

            var bytes = new UTF8Encoding(true).GetBytes(sb.ToString());
            return File(bytes, "text/csv", "service.csv");
        }

        [HttpGet("/analytics/export/feedback.csv")]
        public async Task<IActionResult> ExportFeedback()
        {
            var rows = await _db.CustomerFeedbacks
                .Include(f => f.Customer)
                .Include(f => f.Project)
                .AsNoTracking()
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("CustomerFeedbackID,Customer,Project,Score,SubmittedOn,Comments");

            foreach (var f in rows)
            {
                var cust = f.Customer?.Name ?? "";
                var proj = f.Project?.Name ?? "";
                var comments = (f.Comments ?? "").Replace("\"", "\"\"");
                sb.AppendLine(string.Join(",",
                    f.CustomerFeedbackID.ToString(CultureInfo.InvariantCulture),
                    Q(cust),
                    Q(proj),
                    f.Score.ToString(CultureInfo.InvariantCulture),
                    f.SubmittedOn.ToString("O", CultureInfo.InvariantCulture),
                    Q(comments)
                ));
            }

            var bytes = new UTF8Encoding(true).GetBytes(sb.ToString());
            return File(bytes, "text/csv", "feedback.csv");
        }

        // ================= ML ENDPOINTS (Dashboard) =================
        // Keep ONLY this one under /ml/* in this controller to avoid duplicates.

        // k-Means clusters (Delay, CRs, Feedback)
        [HttpGet("/ml/clusters")]
        public async Task<IActionResult> GetClusters([FromServices] MlDelayPredictionService svc, int k = 3, CancellationToken ct = default)
        {
            var clusters = await svc.RunKMeansAsync(k, ct);
            return Json(clusters.Select(c => new
            {
                cluster = c.Cluster,
                delay = c.Delay,
                crs = c.CRs,
                feedback = c.Feedback
            }));
        }
    }
}



