using Microsoft.ML;
using Microsoft.ML.Data;
using Zorvian.Application.DTOs.ML;

namespace Zorvian.Infrastructure.Services;

public sealed class ExpenseClassificationService
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private Dictionary<uint, AccountInfo>? _labelMap;
    private const string ModelPath = "expense_classification_model.zip";

    public sealed record AccountInfo(Guid AccountId, string Code, string Name);

    public ExpenseClassificationService()
    {
        _mlContext = new MLContext(seed: 1);
    }

    public void Train(IEnumerable<ExpenseClassificationData> trainingData, Dictionary<uint, AccountInfo> labelMap)
    {
        _labelMap = labelMap;
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = _mlContext.Transforms.Text.FeaturizeText("DescriptionFeatures", nameof(ExpenseClassificationData.Description))
            .Append(_mlContext.Transforms.Concatenate("Features", "DescriptionFeatures", nameof(ExpenseClassificationData.Amount)))
            .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(ExpenseClassificationData.AccountId)))
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "Label",
                featureColumnName: "Features"))
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel", "PredictedLabel"));

        _model = pipeline.Fit(dataView);
        _mlContext.Model.Save(_model, dataView.Schema, ModelPath);
    }

    public ExpenseClassificationResponseDto Predict(string description, decimal amount)
    {
        if (_model == null && File.Exists(ModelPath))
            _model = _mlContext.Model.Load(ModelPath, out _);

        if (_model == null)
            throw new InvalidOperationException("Model not trained. Train the model first.");

        if (_labelMap == null)
            throw new InvalidOperationException("Label map not loaded. Train or load the model first.");

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<ExpenseClassificationData, PredictionOut>(_model);
        var input = new ExpenseClassificationData
        {
            Description = description,
            Amount = (float)amount,
            AccountId = ""
        };

        var prediction = predictionEngine.Predict(input);
        var scores = prediction.Score ?? [];

        var scored = scores.Select((s, i) => new { Index = (uint)i, Score = s })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(3)
            .ToList();

        return new ExpenseClassificationResponseDto
        {
            Description = description,
            Amount = amount,
            Suggestions = scored.Select(s =>
            {
                _labelMap.TryGetValue(s.Index, out var info);
                return new ExpenseClassificationResultDto
                {
                    AccountId = info?.AccountId ?? Guid.Empty,
                    AccountCode = info?.Code ?? "",
                    AccountName = info?.Name ?? "",
                    Confidence = s.Score
                };
            }).ToList()
        };
    }

    private sealed class PredictionOut
    {
        [ColumnName("PredictedLabel")]
        public string PredictedAccountId { get; set; } = string.Empty;

        [ColumnName("Score")]
        public float[]? Score { get; set; }
    }
}
