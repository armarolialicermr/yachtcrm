using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YachtCRM.Infrastructure;
using YachtCRM.Application.Interfaces;

namespace YachtCRM.Web.Controllers
{
    [ApiController]
    public class PredictionsController : ControllerBase
    {
        private readonly YachtCrmDbContext _db;
        private readonly IPredictionService _predict;
        public PredictionsController(YachtCrmDbContext db, IPredictionService predict)
        {
            _db = db; _predict = predict;
        }

        [HttpGet("/projects/{id}/prediction")]
        public async Task<IActionResult> Predict(int id)
        {
            var p = await _db.Projects
                .Include(p => p.YachtModel)
                .Include(p => p.Tasks)
                .Include(p => p.ChangeRequests)
                .Include(p => p.Interactions)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (p == null) return NotFound();

            var predicted = _predict.PredictDelayDays(
                length: (float)p.YachtModel.Length,
                basePrice: (float)p.YachtModel.BasePrice,
                numTasks: p.Tasks.Count,
                changeRequests: p.ChangeRequests.Count,
                interactions: p.Interactions.Count
            );

            return Ok(new { projectId = id, predictedDelayDays = Math.Round(predicted, 1) });
        }
    }
}
