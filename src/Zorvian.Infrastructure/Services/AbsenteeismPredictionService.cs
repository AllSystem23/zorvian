using Microsoft.ML;
using Zorvian.Application.DTOs.ML;

namespace Zorvian.Infrastructure.Services;

public sealed class AbsenteeismPredictionService
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private const string ModelPath = "absenteeism_model.zip";

    public AbsenteeismPredictionService()
    {
        _mlContext = new MLContext(seed: 1);
    }

    public void Train(IEnumerable<AttendanceData> trainingData)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
        var pipeline = _mlContext.Transforms.Concatenate("Features", 
            nameof(AttendanceData.DayOfWeek), 
            nameof(AttendanceData.Month), 
            nameof(AttendanceData.IsHoliday),
            nameof(AttendanceData.PreviousAbsenceCount))
            .Append(_mlContext.BinaryClassification.Trainers.FastTree(labelColumnName: "Label"));

        _model = pipeline.Fit(dataView);
        _mlContext.Model.Save(_model, dataView.Schema, ModelPath);
    }

    public AttendancePrediction Predict(AttendanceData data)
    {
        if (_model == null && File.Exists(ModelPath))
            _model = _mlContext.Model.Load(ModelPath, out _);
        
        if (_model == null) return new AttendancePrediction();

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<AttendanceData, AttendancePrediction>(_model);
        return predictionEngine.Predict(data);
    }
}
