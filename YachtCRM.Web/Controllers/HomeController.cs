using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YachtCRM.Application.Interfaces;
using YachtCRM.Infrastructure;
using YachtCRM.Web.ViewModels;

namespace YachtCRM.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly YachtCrmDbContext _db;
        private readonly IPredictionService _pred;

        public HomeController(YachtCrmDbContext db, IPredictionService pred)
        {
            _db = db;
            _pred = pred;
        }

        // Add optional single-yard scope
        public async Task<IActionResult> Index(int? customerId = null)
        {
            // Base query pulls only what we need (fast for big datasets)
            var query = _db.Projects.AsNoTracking();

            if (customerId.HasValue)
            {
                query = query.Where(p => p.CustomerID == customerId.Value);
            }

            var rows = await query
                .Select(p => new
                {
                    p.ProjectID,
                    p.Name,
                    p.CustomerID,
                    CustomerName = p.Customer != null ? p.Customer.Name : "-",
                    Status = string.IsNullOrWhiteSpace(p.Status) ? "Unknown" : p.Status,
                    YachtModelLength = p.YachtModel != null ? (float?)p.YachtModel.Length : null,
                    YachtModelBasePrice = p.YachtModel != null ? (float?)p.YachtModel.BasePrice : null,
                    Tasks = p.Tasks.Count,
                    ChangeRequests = p.ChangeRequests.Count,
                    Interactions = p.Interactions.Count
                })
                .ToListAsync();

            var vm = new DashboardViewModel
            {
                TotalProjects       = rows.Count,
                TotalCustomers      = await _db.Customers.AsNoTracking().CountAsync(),
                TotalChangeRequests = await _db.ChangeRequests.AsNoTracking().CountAsync(),
                TotalInteractions   = await _db.Interactions.AsNoTracking().CountAsync(),
                NeedsSeed           = rows.Count == 0 // optional: show seed CTA only when empty
            };

            // Status chart
            var statusCounts = rows
                .GroupBy(r => r.Status)
                .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            vm.StatusCounts     = statusCounts;
            vm.StatusLabelsJson = JsonSerializer.Serialize(statusCounts.Select(s => s.Status));
            vm.StatusDataJson   = JsonSerializer.Serialize(statusCounts.Select(s => s.Count));

            // Scatter: Tasks vs Length (clickable points)
            var tvlPoints = rows
                .Where(r => r.YachtModelLength.HasValue)
                .Select(r => new
                {
                    x = r.YachtModelLength!.Value,
                    y = (float)r.Tasks,
                    label = r.Name,
                    id = r.ProjectID
                }).ToList();
            vm.TasksVsLengthPointsJson = JsonSerializer.Serialize(tvlPoints);

            // Scatter: Change Requests vs Predicted Delay (clickable points)
            var crdPoints = rows
                .Where(r => r.YachtModelLength.HasValue && r.YachtModelBasePrice.HasValue)
                .Select(r =>
                {
                    var pred = _pred.PredictDelayDays(
                        r.YachtModelLength!.Value,
                        r.YachtModelBasePrice!.Value,
                        r.Tasks, r.ChangeRequests, r.Interactions
                    );
                    return new { x = (float)r.ChangeRequests, y = (float)pred, label = r.Name, id = r.ProjectID };
                })
                .ToList();
            vm.CrVsDelayPointsJson = JsonSerializer.Serialize(crdPoints);

            // Top 5 by predicted delay
            vm.TopPredictedDelays = rows
                .Where(r => r.YachtModelLength.HasValue && r.YachtModelBasePrice.HasValue)
                .Select(r =>
                {
                    var pred = _pred.PredictDelayDays(
                        r.YachtModelLength!.Value,
                        r.YachtModelBasePrice!.Value,
                        r.Tasks, r.ChangeRequests, r.Interactions
                    );
                    return new OffenderRow
                    {
                        ProjectID          = r.ProjectID,
                        ProjectName        = r.Name,
                        CustomerName       = r.CustomerName,
                        PredictedDelayDays = pred,
                        ChangeRequests     = r.ChangeRequests,
                        Tasks              = r.Tasks,
                        Length             = r.YachtModelLength!.Value
                    };
                })
                .OrderByDescending(r => r.PredictedDelayDays)
                .ThenByDescending(r => r.ChangeRequests)
                .Take(5)
                .ToList();

            // Populate customers for the yard dropdown (full list; you can scope if you prefer)
            ViewBag.Customers = await _db.Customers.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new { c.CustomerID, c.Name })
                .ToListAsync();
            ViewBag.SelectedCustomerId = customerId;

            return View(vm);
        }
    }
}

