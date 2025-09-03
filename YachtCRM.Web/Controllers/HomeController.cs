using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YachtCRM.Infrastructure;
using YachtCRM.Web.ViewModels;
using YachtCRM.Infrastructure.Services; // MlDelayPredictionService

namespace YachtCRM.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly YachtCrmDbContext _db;
        private readonly MlDelayPredictionService _ml;

        public HomeController(YachtCrmDbContext db, MlDelayPredictionService ml)
        {
            _db = db;
            _ml = ml;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel();

            // ---- Basic counts ----
            vm.TotalProjects        = await _db.Projects.CountAsync();
            vm.TotalCustomers       = await _db.Customers.CountAsync();
            vm.TotalChangeRequests  = await _db.ChangeRequests.CountAsync();
            vm.TotalInteractions    = await _db.Interactions.CountAsync();
            vm.AvgFeedbackScore     = await _db.CustomerFeedbacks.AnyAsync()
                                        ? await _db.CustomerFeedbacks.AverageAsync(f => (double)f.Score)
                                        : (double?)null;
            vm.OpenServiceRequests  = await _db.ServiceRequests.CountAsync(s => s.CompletedOn == null && s.Status != "Closed");

            // ---- Status chart ----
            var statusCounts = await _db.Projects
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .OrderBy(x => x.Status)
                .ToListAsync();

            var statusLabels = statusCounts.Select(x => x.Status ?? "Unknown").ToArray();
            var statusData   = statusCounts.Select(x => x.Count).ToArray();

            // ---- Tasks vs Length scatter ----
            var projectsForTL = await _db.Projects
                .Include(p => p.YachtModel)
                .Include(p => p.Tasks)
                .AsNoTracking()
                .Select(p => new {
                    p.ProjectID,
                    p.Name,
                    Length = p.YachtModel != null ? (double?)p.YachtModel.Length : null,
                    Tasks  = p.Tasks.Count
                })
                .ToListAsync();

            var tlPoints = projectsForTL
                .Where(p => p.Length.HasValue)
                .Select(p => new { x = p.Length!.Value, y = p.Tasks, label = p.Name, id = p.ProjectID })
                .ToList();

            // ---- CR vs Predicted Delay scatter ----
            var projectsForCR = await _db.Projects
                .Include(p => p.YachtModel)
                .Include(p => p.ChangeRequests)
                .Include(p => p.Interactions)
                .AsNoTracking()
                .Select(p => new {
                    p.ProjectID,
                    p.Name,
                    CustomerName = p.Customer != null ? p.Customer.Name : "",
                    Length       = p.YachtModel != null ? (float?)p.YachtModel.Length : null,
                    BasePrice    = p.YachtModel != null ? (float?)p.YachtModel.BasePrice : null,
                    Tasks        = p.Tasks.Count,
                    CRs          = p.ChangeRequests.Count,
                    Interactions = p.Interactions.Count
                })
                .ToListAsync();

            // Ensure model is trained at least once
            await _ml.TrainAsync();

            var crDelayPoints = new List<object>();
            foreach (var p in projectsForCR)
            {
                var lengthMeters = p.Length ?? 0f;
                var basePrice    = p.BasePrice ?? 0f;
                var pred         = _ml.PredictDelayDays(lengthMeters, basePrice, p.Tasks, p.CRs, p.Interactions);
                crDelayPoints.Add(new { x = p.CRs, y = pred, label = p.Name, id = p.ProjectID });
            }

            // ---- Top predicted delays table ----
            vm.TopPredictedDelays = projectsForCR
                .Select(p => new {
                    p.ProjectID,
                    p.Name,
                    p.CustomerName,
                    p.Length,
                    p.Tasks,
                    p.CRs,
                    Pred = _ml.PredictDelayDays(p.Length ?? 0f, p.BasePrice ?? 0f, p.Tasks, p.CRs, p.Interactions)
                })
                .OrderByDescending(x => x.Pred)
                .Take(15)
                .Select(x => new OffenderRow {
                    ProjectID = x.ProjectID,
                    ProjectName = x.Name,
                    CustomerName = x.CustomerName,
                    PredictedDelayDays = x.Pred,
                    ChangeRequests = x.CRs,
                    Tasks = x.Tasks,
                    Length = x.Length ?? 0f
                })
                .ToList();

            // ---- Serialize JSON strings the view expects ----
            var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = null };
            vm.StatusLabelsJson         = JsonSerializer.Serialize(statusLabels, jsonOpts);
            vm.StatusDataJson           = JsonSerializer.Serialize(statusData,   jsonOpts);
            vm.TasksVsLengthPointsJson  = JsonSerializer.Serialize(tlPoints,     jsonOpts);
            vm.CrVsDelayPointsJson      = JsonSerializer.Serialize(crDelayPoints,jsonOpts);

            return View(vm);
        }
    }
}


