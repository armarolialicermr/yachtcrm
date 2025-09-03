// YachtCRM.Web/Controllers/MlController.cs
using Microsoft.AspNetCore.Mvc;
using YachtCRM.Infrastructure.Services;

namespace YachtCRM.Web.Controllers
{
    [ApiController]
    [Route("ml")]
    public class MlController : ControllerBase
    {
        private readonly MlDelayPredictionService _svc;
        public MlController(MlDelayPredictionService svc) => _svc = svc;

        [HttpGet("high-risk-projects")]
        public async Task<IActionResult> HighRisk(CancellationToken ct) =>
            Ok(await _svc.GetHighRiskProjectsAsync(ct));

        [HttpGet("metrics")]
        public async Task<IActionResult> Metrics(CancellationToken ct) =>
            Ok(await _svc.CrossValidateAsync(5, ct));

        [HttpGet("feature-importance")]
        public async Task<IActionResult> FeatureImportance(CancellationToken ct) =>
            Ok(await _svc.GetFeatureImportanceAsync(ct));
    }
}

