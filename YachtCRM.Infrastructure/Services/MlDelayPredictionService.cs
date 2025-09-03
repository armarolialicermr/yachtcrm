using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;              // KMeansTrainer
using Microsoft.ML.Trainers.FastTree;     // FastTreeRegressionTrainer
using YachtCRM.Application.Interfaces;
using YachtCRM.Infrastructure;             // YachtCrmDbContext

namespace YachtCRM.Infrastructure.Services
{
    /// <summary>
    /// ML.NET delay predictor using FastTree regression.
    /// - TrainAsync() fits the model from the Projects table (when labels exist).
    /// - PredictDelayDays() uses the trained model (lazy-trains on first call).
    /// - CrossValidateAsync() reports CV metrics (SDCA pipeline, metrics only).
    /// - GetFeatureImportanceAsync() reports |Pearson correlation| as a simple, robust importance proxy.
    /// - GetHighRiskProjectsAsync() returns projects with large predicted delay + many CRs, joining feedback avg from CustomerFeedbacks.
    /// - RunKMeansAsync() clusters projects by (PredictedDelay, CRs, AvgFeedback).
    /// </summary>
    public sealed class MlDelayPredictionService : IPredictionService, IDisposable
    {
        private readonly YachtCrmDbContext _db;
        private readonly MLContext _ml;
        private readonly object _lock = new();
        private ITransformer? _model;
        private PredictionEngine<DelayRow, DelayPrediction>? _engine;
        private bool _trained;

        private static readonly string[] featureColumns =
        {
            nameof(DelayRow.LengthMeters),
            nameof(DelayRow.ModelBasePrice),
            nameof(DelayRow.Tasks),
            nameof(DelayRow.ChangeRequests),
            nameof(DelayRow.Interactions),
            nameof(DelayRow.IsCustom),
            nameof(DelayRow.Seasonality)
        };

        public MlDelayPredictionService(YachtCrmDbContext db)
        {
            _db = db;
            _ml = new MLContext(seed: 42);
        }

        // ---------------- Prediction API ----------------
        public float PredictDelayDays(float lengthMeters, float basePrice, int tasks, int cr, int interactions)
        {
            EnsureTrained();

            var input = new DelayRow
            {
                LengthMeters   = lengthMeters,
                ModelBasePrice = basePrice,
                Tasks          = tasks,
                ChangeRequests = cr,
                Interactions   = interactions,
                // You can wire real flags if available:
                IsCustom       = 0f,
                Seasonality    = 0f,
                Label          = 0f
            };

            return _engine!.Predict(input).Score;
        }

        // ---------------- Training ----------------
        public async Task<int> TrainAsync(CancellationToken ct = default)
        {
            // 1) Pull labeled rows (ActualEnd present)
            var rows = await _db.Projects
                .Include(p => p.YachtModel)
                .Include(p => p.Tasks)
                .Include(p => p.ChangeRequests)
                .Include(p => p.Interactions)
                .AsNoTracking()
                .Where(p => p.ActualEnd != null)
                .Select(p => new DelayRow
                {
                    LengthMeters   = (float)(p.YachtModel != null ? p.YachtModel.Length : 0m),
                    ModelBasePrice = (float)(p.YachtModel != null ? p.YachtModel.BasePrice : 0m),
                    Tasks          = p.Tasks.Count,
                    ChangeRequests = p.ChangeRequests.Count,
                    Interactions   = p.Interactions.Count,
                    IsCustom       = p.Name.Contains("Custom", StringComparison.OrdinalIgnoreCase) ? 1f : 0f,
                    Seasonality    = (p.PlannedStart.Month >= 6 && p.PlannedStart.Month <= 9) ? 1f : 0f,
                    Label          = (float)((p.ActualEnd!.Value.Date - p.PlannedEnd.Date).TotalDays)
                })
                .ToListAsync(ct);

            lock (_lock)
            {
                if (rows.Count < 10)
                {
                    // Fallback: copy label to score (lets the app run with tiny data)
                    var tiny = rows.Any() ? rows : new List<DelayRow> { new DelayRow() };
                    var dataView = _ml.Data.LoadFromEnumerable<DelayRow>(tiny);

                    var fallback = _ml.Transforms.CopyColumns("Score", nameof(DelayRow.Label)).Fit(dataView);
                    _model   = fallback;
                    _engine  = _ml.Model.CreatePredictionEngine<DelayRow, DelayPrediction>(_model);
                    _trained = true;
                    return rows.Count;
                }

                var data = _ml.Data.LoadFromEnumerable<DelayRow>(rows);

                var pipeline =
                    _ml.Transforms.Concatenate("Features", featureColumns)
                      .Append(_ml.Regression.Trainers.FastTree(new FastTreeRegressionTrainer.Options
                      {
                          LabelColumnName            = "Label",
                          FeatureColumnName          = "Features",
                          NumberOfTrees              = 200,
                          NumberOfLeaves             = 32,
                          MinimumExampleCountPerLeaf = 10,
                          LearningRate               = 0.15f
                      }));

                _model   = pipeline.Fit(data);
                _engine  = _ml.Model.CreatePredictionEngine<DelayRow, DelayPrediction>(_model);
                _trained = true;
            }

            return rows.Count;
        }

        private void EnsureTrained()
        {
            if (_trained) return;
            TrainAsync().GetAwaiter().GetResult();
        }

        // ---------------- Cross-Validation Metrics ----------------
        public async Task<MlMetricsDto> CrossValidateAsync(int folds = 5, CancellationToken ct = default)
        {
            var rows = await _db.Projects
                .Include(p => p.YachtModel)
                .Include(p => p.Tasks)
                .Include(p => p.ChangeRequests)
                .Include(p => p.Interactions)
                .AsNoTracking()
                .Select(p => new DelayRow
                {
                    LengthMeters   = (float)(p.YachtModel != null ? p.YachtModel.Length : 0m),
                    ModelBasePrice = (float)(p.YachtModel != null ? p.YachtModel.BasePrice : 0m),
                    Tasks          = p.Tasks.Count,
                    ChangeRequests = p.ChangeRequests.Count,
                    Interactions   = p.Interactions.Count,
                    IsCustom       = p.Name.Contains("Custom", StringComparison.OrdinalIgnoreCase) ? 1f : 0f,
                    Seasonality    = (p.PlannedStart.Month >= 6 && p.PlannedStart.Month <= 9) ? 1f : 0f,
                    Label          = (p.ActualEnd.HasValue ?
                        (float)(p.ActualEnd.Value.Date - p.PlannedEnd.Date).TotalDays : 0f)
                })
                .ToListAsync(ct);

            var dto = new MlMetricsDto { TrainingRows = rows.Count };
            if (rows.Count < Math.Max(10, folds)) return dto;

            var data = _ml.Data.LoadFromEnumerable<DelayRow>(rows);

            var cvPipeline =
                _ml.Transforms.Concatenate("Features", featureColumns)
                  .Append(_ml.Regression.Trainers.Sdca(
                      labelColumnName: "Label", featureColumnName: "Features"));

            var cv = _ml.Regression.CrossValidate(data, cvPipeline, numberOfFolds: folds);

            foreach (var (fold, i) in cv.Select((c, i) => (c, i)))
            {
                dto.Folds.Add(new MlCvFold
                {
                    Fold = i + 1,
                    MAE  = fold.Metrics.MeanAbsoluteError,
                    RMSE = fold.Metrics.RootMeanSquaredError,
                    R2   = fold.Metrics.RSquared
                });
            }

            dto.MeanMAE  = dto.Folds.Average(f => f.MAE);
            dto.MeanRMSE = dto.Folds.Average(f => f.RMSE);
            dto.MeanR2   = dto.Folds.Average(f => f.R2);
            dto.Model    = "FastTree (serving), SDCA (CV)";
            return dto;
        }

        // ---------------- Feature “importance” via |corr| ----------------
        public async Task<FeatureImportanceDto> GetFeatureImportanceAsync(CancellationToken ct = default)
        {
            var rows = await _db.Projects
                .Include(p => p.YachtModel)
                .Include(p => p.Tasks)
                .Include(p => p.ChangeRequests)
                .Include(p => p.Interactions)
                .AsNoTracking()
                .Select(p => new DelayRow
                {
                    LengthMeters   = (float)(p.YachtModel != null ? p.YachtModel.Length : 0m),
                    ModelBasePrice = (float)(p.YachtModel != null ? p.YachtModel.BasePrice : 0m),
                    Tasks          = p.Tasks.Count,
                    ChangeRequests = p.ChangeRequests.Count,
                    Interactions   = p.Interactions.Count,
                    IsCustom       = p.Name.Contains("Custom", StringComparison.OrdinalIgnoreCase) ? 1f : 0f,
                    Seasonality    = (p.PlannedStart.Month >= 6 && p.PlannedStart.Month <= 9) ? 1f : 0f,
                    Label          = (p.ActualEnd.HasValue ?
                        (float)(p.ActualEnd.Value.Date - p.PlannedEnd.Date).TotalDays : 0f)
                })
                .ToListAsync(ct);

            var dto = new FeatureImportanceDto
            {
                TrainingRows = rows.Count,
                Method       = "Absolute Pearson correlation with label"
            };
            if (rows.Count < 10) return dto;

            static double AbsCorr(IEnumerable<float> xsF, IEnumerable<float> ysF)
            {
                var xs = xsF.Select(v => (double)v).ToArray();
                var ys = ysF.Select(v => (double)v).ToArray();
                if (xs.Length != ys.Length || xs.Length == 0) return 0;
                var mx = xs.Average(); var my = ys.Average();
                double sxy = 0, sxx = 0, syy = 0;
                for (int i = 0; i < xs.Length; i++)
                {
                    var dx = xs[i] - mx; var dy = ys[i] - my;
                    sxy += dx * dy; sxx += dx * dx; syy += dy * dy;
                }
                if (sxx == 0 || syy == 0) return 0;
                return Math.Abs(sxy / Math.Sqrt(sxx * syy));
            }

            var label = rows.Select(r => r.Label).ToArray();

            IEnumerable<float> series(string feat) => feat switch
            {
                nameof(DelayRow.LengthMeters)   => rows.Select(r => r.LengthMeters),
                nameof(DelayRow.ModelBasePrice) => rows.Select(r => r.ModelBasePrice),
                nameof(DelayRow.Tasks)          => rows.Select(r => r.Tasks),
                nameof(DelayRow.ChangeRequests) => rows.Select(r => r.ChangeRequests),
                nameof(DelayRow.Interactions)   => rows.Select(r => r.Interactions),
                nameof(DelayRow.IsCustom)       => rows.Select(r => r.IsCustom),
                nameof(DelayRow.Seasonality)    => rows.Select(r => r.Seasonality),
                _ => Enumerable.Repeat(0f, rows.Count)
            };

            dto.Items = featureColumns
                .Select(f => new FeatureImportanceItem
                {
                    Feature   = f,
                    CorrAbs   = AbsCorr(series(f), label),
                    WeightAbs = AbsCorr(series(f), label) // same value for plotting
                })
                .OrderByDescending(x => x.CorrAbs ?? 0)
                .ToList();

            return dto;
        }

        // ---------------- High-risk projects (safe feedback join) ----------------
        public async Task<List<ProjectRiskDto>> GetHighRiskProjectsAsync(CancellationToken ct = default)
        {
            // Precompute Avg feedback per project (avoids relying on Project.Feedbacks)
            var feedbackAvgByProject = await _db.CustomerFeedbacks
                .AsNoTracking()
                .Where(f => f.ProjectID != null)                  // ensure non-null
                .GroupBy(f => f.ProjectID!.Value)                 // group by int (not int?)
                .Select(g => new { ProjectID = g.Key, AvgScore = g.Average(x => x.Score) })
                .ToDictionaryAsync(x => x.ProjectID, x => x.AvgScore, ct);

            // Pull minimal features + predict
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
                    CustomerName = p.Customer != null ? p.Customer.Name : "",
                    Length = (float)(p.YachtModel != null ? p.YachtModel.Length : 0m),
                    BasePrice = (float)(p.YachtModel != null ? p.YachtModel.BasePrice : 0m),
                    Tasks = p.Tasks.Count,
                    CRs = p.ChangeRequests.Count,
                    Interactions = p.Interactions.Count
                })
                .ToListAsync(ct);

            EnsureTrained();

            var list = new List<ProjectRiskDto>(rows.Count);
            foreach (var r in rows)
            {
                var pred = PredictDelayDays(r.Length, r.BasePrice, r.Tasks, r.CRs, r.Interactions);
                var fb   = feedbackAvgByProject.TryGetValue(r.ProjectID, out var avg) ? avg : 10.0;

                list.Add(new ProjectRiskDto
                {
                    ProjectID      = r.ProjectID,
                    ProjectName    = r.Name,
                    CustomerName   = r.CustomerName,
                    PredictedDelay = pred,
                    ChangeRequests = r.CRs,
                    FeedbackScore  = fb
                });
            }

            // Example threshold (tune to your data scale)
            // inside MlDelayPredictionService.GetHighRiskProjectsAsync()
            return list
                .Where(p => p.PredictedDelay > 15f && p.ChangeRequests > 5) // relaxed thresholds
                .OrderByDescending(p => p.PredictedDelay)
                .ThenByDescending(p => p.ChangeRequests)
                .ToList();

        }

        // ---------------- K-Means clustering ----------------
        public async Task<List<ClusterResult>> RunKMeansAsync(int k = 3, CancellationToken ct = default)
        {
            // Precompute Avg feedback per project
            var feedbackAvgByProject = await _db.CustomerFeedbacks
                .AsNoTracking()
                .Where(f => f.ProjectID != null)                  // ensure non-null
                .GroupBy(f => f.ProjectID!.Value)                 // group by int (not int?)
                .Select(g => new { ProjectID = g.Key, AvgScore = g.Average(x => x.Score) })
                .ToDictionaryAsync(x => x.ProjectID, x => x.AvgScore, ct);

            var rows = await _db.Projects
                .Include(p => p.YachtModel)
                .Include(p => p.Tasks)
                .Include(p => p.ChangeRequests)
                .Include(p => p.Interactions)
                .AsNoTracking()
                .Select(p => new
                {
                    p.ProjectID,
                    p.Name,
                    Length      = (float)(p.YachtModel != null ? p.YachtModel.Length : 0m),
                    BasePrice   = (float)(p.YachtModel != null ? p.YachtModel.BasePrice : 0m),
                    Tasks       = p.Tasks.Count,
                    CRs         = p.ChangeRequests.Count,
                    Interactions= p.Interactions.Count
                })
                .ToListAsync(ct);

            EnsureTrained();

            var inputs = rows.Select(r => new ClusterInput
            {
                Delay    = PredictDelayDays(r.Length, r.BasePrice, r.Tasks, r.CRs, r.Interactions),
                CRs      = r.CRs,
                Feedback = (float)(feedbackAvgByProject.TryGetValue(r.ProjectID, out var avg) ? avg : 10.0)
            }).ToList();

            if (inputs.Count == 0) return new List<ClusterResult>();

            var dataView = _ml.Data.LoadFromEnumerable<ClusterInput>(inputs);

            var pipeline = _ml.Transforms
                .Concatenate("Features", nameof(ClusterInput.Delay), nameof(ClusterInput.CRs), nameof(ClusterInput.Feedback))
                .Append(_ml.Clustering.Trainers.KMeans(new KMeansTrainer.Options
                {
                    NumberOfClusters  = k,
                    FeatureColumnName = "Features"
                }));

            var model = pipeline.Fit(dataView);
            var predEngine = _ml.Model.CreatePredictionEngine<ClusterInput, ClusterPrediction>(model);

            var results = inputs.Select(inp =>
            {
                var p = predEngine.Predict(inp);
                return new ClusterResult
                {
                    Cluster  = (int)p.PredictedClusterId,
                    Delay    = inp.Delay,
                    CRs      = inp.CRs,
                    Feedback = inp.Feedback
                };
            }).ToList();

            return results;
        }

        public void Dispose() => _engine?.Dispose();

        // --------- ML.NET schema ---------
        private sealed class DelayRow
        {
            [LoadColumn(0)] public float LengthMeters { get; set; }
            [LoadColumn(1)] public float ModelBasePrice { get; set; }
            [LoadColumn(2)] public float Tasks { get; set; }
            [LoadColumn(3)] public float ChangeRequests { get; set; }
            [LoadColumn(4)] public float Interactions { get; set; }
            [LoadColumn(5)] public float IsCustom { get; set; }
            [LoadColumn(6)] public float Seasonality { get; set; }
            [LoadColumn(7)] public float Label { get; set; } // actual delay days
        }

        private sealed class DelayPrediction
        {
            public float Score { get; set; }
        }

        private sealed class ClusterInput
        {
            public float Delay { get; set; }
            public float CRs { get; set; }
            public float Feedback { get; set; }
        }

        private sealed class ClusterPrediction
        {
            [ColumnName("PredictedLabel")]
            public uint PredictedClusterId { get; set; }
        }

        // --------- DTOs used by controllers/views ---------
        public sealed class MlCvFold
        {
            public int Fold { get; set; }
            public double MAE { get; set; }
            public double RMSE { get; set; }
            public double R2 { get; set; }
        }

        public sealed class MlMetricsDto
        {
            public double MeanMAE { get; set; }
            public double MeanRMSE { get; set; }
            public double MeanR2 { get; set; }
            public List<MlCvFold> Folds { get; set; } = new();
            public int TrainingRows { get; set; }
            public string Model { get; set; } = "FastTree (serving), SDCA (CV)";
        }

        public sealed class FeatureImportanceItem
        {
            public string Feature { get; set; } = "";
            public double WeightAbs { get; set; }  // same as CorrAbs for display
            public double? CorrAbs { get; set; }
        }

        public sealed class FeatureImportanceDto
        {
            public List<FeatureImportanceItem> Items { get; set; } = new();
            public int TrainingRows { get; set; }
            public string Method { get; set; } = "Absolute Pearson correlation with label";
        }

        public sealed class ProjectRiskDto
        {
            public int ProjectID { get; set; }
            public string ProjectName { get; set; } = "";
            public string CustomerName { get; set; } = "";
            public float  PredictedDelay { get; set; }
            public int    ChangeRequests { get; set; }
            public double FeedbackScore { get; set; }
        }

        public sealed class ClusterResult
        {
            public int   Cluster  { get; set; }
            public float Delay    { get; set; }
            public float CRs      { get; set; }
            public float Feedback { get; set; }
        }
    }
}


