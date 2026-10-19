using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers.Fleet;

/// <summary>
/// IA predictiva de flota (pantalla `/fleet/predictive`, gated por
/// `fleet.read`): las consultas exigen `fleet.read` y la marca manual de
/// anomalía de combustible `fleet.write`.
/// </summary>
[ApiController]
[Route("zorvian/v1/fleet/predictive")]
[Authorize]
public sealed class PredictiveMaintenanceController : ControllerBase
{
    private readonly PredictiveMaintenanceService _predictiveService;
    private readonly FuelAnomalyDetectionService _anomalyService;

    public PredictiveMaintenanceController(
        PredictiveMaintenanceService predictiveService,
        FuelAnomalyDetectionService anomalyService)
    {
        _predictiveService = predictiveService;
        _anomalyService = anomalyService;
    }

    // ── Predictive Maintenance ──

    /// <summary>
    /// Get maintenance forecasts for all vehicles (or one specific vehicle).
    /// </summary>
    [HttpGet("maintenance/forecasts")]
    [RequirePermission(Permissions.FleetRead)]
    [ProducesResponseType(typeof(List<VehicleMaintenanceForecast>), 200)]
    public async Task<IActionResult> GetForecasts([FromQuery] Guid? vehicleId)
    {
        var result = await _predictiveService.GetMaintenanceForecastsAsync(vehicleId);
        return Ok(result);
    }

    /// <summary>
    /// Get fleet-wide predictive maintenance summary.
    /// </summary>
    [HttpGet("maintenance/summary")]
    [RequirePermission(Permissions.FleetRead)]
    [ProducesResponseType(typeof(PredictiveMaintenanceSummary), 200)]
    public async Task<IActionResult> GetMaintenanceSummary()
    {
        var result = await _predictiveService.GetSummaryAsync();
        return Ok(result);
    }

    // ── Fuel Anomaly Detection ──

    /// <summary>
    /// Analyze fuel consumption and detect anomalies.
    /// </summary>
    [HttpGet("fuel/anomalies")]
    [RequirePermission(Permissions.FleetRead)]
    [ProducesResponseType(typeof(FuelAnomalySummary), 200)]
    public async Task<IActionResult> GetFuelAnomalies([FromQuery] AnalyzeFuelConsumptionRequest request)
    {
        var result = await _anomalyService.AnalyzeAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Get consumption trends per vehicle.
    /// </summary>
    [HttpGet("fuel/trends")]
    [RequirePermission(Permissions.FleetRead)]
    [ProducesResponseType(typeof(List<FuelConsumptionTrend>), 200)]
    public async Task<IActionResult> GetConsumptionTrends([FromQuery] Guid? vehicleId)
    {
        var result = await _anomalyService.GetConsumptionTrendsAsync(vehicleId);
        return Ok(result);
    }

    /// <summary>
    /// Mark a fuel refill as anomaly or not (manual override).
    /// </summary>
    [HttpPut("fuel/{refillId:guid}/anomaly")]
    [RequirePermission(Permissions.FleetWrite)]
    [ProducesResponseType(200)]
    public async Task<IActionResult> MarkAnomaly(Guid refillId, [FromBody] MarkFuelAnomalyRequest request)
    {
        var success = await _anomalyService.MarkAnomalyAsync(refillId, request);
        return success ? Ok(new { message = "Anomaly status updated" }) : NotFound();
    }
}
