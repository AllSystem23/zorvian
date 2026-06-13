using Microsoft.ML;
using Microsoft.ML.Data;
using Zorvian.Application.DTOs.ML;

namespace Zorvian.Infrastructure.Services;

public sealed class SalesPredictionService
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private const string ModelPath = "sales_prediction_model.zip";

    public SalesPredictionService()
    {
        _mlContext = new MLContext(seed: 1);
    }

    public void Train(IEnumerable<SalesData> trainingData)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
        var pipeline = _mlContext.Transforms.Concatenate("Features",
            nameof(SalesData.DayOfWeek),
            nameof(SalesData.Month),
            nameof(SalesData.DayOfMonth),
            nameof(SalesData.IsWeekend),
            nameof(SalesData.IsHoliday),
            nameof(SalesData.PreviousDaySales),
            nameof(SalesData.PreviousWeekSales),
            nameof(SalesData.DayOfYear))
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: "Label",
                numberOfLeaves: 20,
                numberOfTrees: 100,
                minimumExampleCountPerLeaf: 5));

        _model = pipeline.Fit(dataView);
        _mlContext.Model.Save(_model, dataView.Schema, ModelPath);
    }

    public SalesPrediction Predict(SalesData data)
    {
        if (_model == null && File.Exists(ModelPath))
            _model = _mlContext.Model.Load(ModelPath, out _);

        if (_model == null)
            return new SalesPrediction();

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<SalesData, SalesPrediction>(_model);
        return predictionEngine.Predict(data);
    }

    public List<SalesPrediction> PredictNextDays(IEnumerable<SalesData> nextDaysData)
    {
        if (_model == null && File.Exists(ModelPath))
            _model = _mlContext.Model.Load(ModelPath, out _);

        if (_model == null)
            return new List<SalesPrediction>();

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<SalesData, SalesPrediction>(_model);
        return nextDaysData.Select(d => predictionEngine.Predict(d)).ToList();
    }
}
