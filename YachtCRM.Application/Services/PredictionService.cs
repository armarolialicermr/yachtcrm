using Microsoft.ML;
using Microsoft.ML.Data;
using YachtCRM.Application.Interfaces;

namespace YachtCRM.Application.Services
{
    public class PredictionService : IPredictionService, IDisposable
    {
        private readonly MLContext _ml = new MLContext();
        private ITransformer? _model;
        private PredictionEngine<ModelInput, ModelOutput>? _engine;
        private readonly object _lock = new();

        // Adjust if your features change
        private class ModelInput
        {
            public float Length { get; set; }
            public float BasePrice { get; set; }
            public float NumTasks { get; set; }
            public float ChangeRequests { get; set; }
            public float Interactions { get; set; }
        }

        private class ModelOutput
        {
            [ColumnName("Score")]
            public float PredictedDaysLate { get; set; }
        }

        public PredictionService()
        {
            // Try to load a pre-trained model file from the working directory
            // Put delayModel.zip next to the running Web app (YachtCRM.Web/bin/Debug/...).
            try
            {
                var modelPath = Path.Combine(AppContext.BaseDirectory, "delayModel.zip");
                if (File.Exists(modelPath))
                {
                    using var fs = File.OpenRead(modelPath);
                    _model = _ml.Model.Load(fs, out _);
                    _engine = _ml.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_model);
                }
            }
            catch
            {
                // If loading fails, we'll just use the heuristic fallback.
            }
        }

        public float PredictDelayDays(float length, float basePrice, int numTasks, int changeRequests, int interactions)
        {
            if (_engine != null)
            {
                lock (_lock)
                {
                    var result = _engine.Predict(new ModelInput
                    {
                        Length = length,
                        BasePrice = basePrice,
                        NumTasks = numTasks,
                        ChangeRequests = changeRequests,
                        Interactions = interactions
                    });
                    return MathF.Max(0, result.PredictedDaysLate);
                }
            }

            // Fallback heuristic until you drop in a real model
            var score = 0.08f * length
                        + 0.0000015f * basePrice
                        + 0.06f * numTasks
                        + 4.5f * changeRequests
                        - 0.25f * interactions;
            return MathF.Max(0, score);
        }

        public void Dispose()
        {
            // nothing to dispose in this simple example
        }
    }
}

