using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class SensorAnalysisService : ISensorAnalysisService
{
    private readonly ISensorReadingRepository _sensorReadingRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly ILogger<SensorAnalysisService> _logger;

    public SensorAnalysisService(
        ISensorReadingRepository sensorReadingRepository,
        IAlertRepository alertRepository,
        ILogger<SensorAnalysisService> logger)
    {
        _sensorReadingRepository = sensorReadingRepository;
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task ProcessSensorReadingAsync(SensorDataDto sensorData)
    {
        _logger.LogInformation(
            "Processando leitura do sensor: FieldId={FieldId}, SensorType={SensorType}, Value={Value}",
            sensorData.FieldId, sensorData.SensorType, sensorData.Value);

        var reading = MapToEntity(sensorData);
        await _sensorReadingRepository.AddAsync(reading);
        await _sensorReadingRepository.SaveChangesAsync();

        _logger.LogInformation("Leitura salva com sucesso: Id={Id}", reading.Id);

        await ApplyBusinessRulesAsync(sensorData);
    }

    private async Task ApplyBusinessRulesAsync(SensorDataDto sensorData)
    {
        var sensorType = sensorData.SensorType.ToLower();

        switch (sensorType)
        {
            case "soilhumidity":
                await ProcessSoilHumidityRulesAsync(sensorData);
                break;

            case "temperature":
                await ProcessTemperatureRulesAsync(sensorData);
                break;

            case "rainfall":
                await ProcessRainfallRulesAsync(sensorData);
                break;

            default:
                _logger.LogWarning(
                    "Tipo de sensor não reconhecido para regras de negócio: {SensorType}",
                    sensorData.SensorType);
                break;
        }
    }

    #region Soil Humidity Rules

    private async Task ProcessSoilHumidityRulesAsync(SensorDataDto sensorData)
    {
        var value = sensorData.Value;

        if (value < 20)
        {
            _logger.LogError(
                "SECA CRÍTICA detectada! FieldId={FieldId}, Umidade={Value}%",
                sensorData.FieldId, value);

            await CreateAlertIfNotExistsAsync(
                fieldId: sensorData.FieldId,
                type: AlertType.DROUGHT_CRITICAL,
                severity: AlertSeverity.Critical,
                message: $"PERIGO: Seca severa detectada ({value:F1}%). Risco de perda da cultura.",
                triggerValue: value);
        }
        else if (value < 30)
        {
            _logger.LogWarning(
                "Alerta de Seca detectado! FieldId={FieldId}, Umidade={Value}%",
                sensorData.FieldId, value);

            await CreateAlertIfNotExistsAsync(
                fieldId: sensorData.FieldId,
                type: AlertType.DROUGHT_WARNING,
                severity: AlertSeverity.High,
                message: $"Alerta de Seca: Umidade abaixo do nível ideal ({value:F1}% < 30%).",
                triggerValue: value);
        }
        else if (value > 80)
        {
            _logger.LogWarning(
                "Solo Saturado detectado! FieldId={FieldId}, Umidade={Value}%",
                sensorData.FieldId, value);

            await CreateAlertIfNotExistsAsync(
                fieldId: sensorData.FieldId,
                type: AlertType.SATURATION,
                severity: AlertSeverity.Medium,
                message: $"Solo Saturado: Risco de apodrecimento da raiz ({value:F1}% > 80%).",
                triggerValue: value);
        }
        else
        {
            _logger.LogInformation(
                "Umidade do solo em níveis normais: FieldId={FieldId}, Umidade={Value}%",
                sensorData.FieldId, value);
        }
    }

    #endregion

    #region Temperature Rules

    private async Task ProcessTemperatureRulesAsync(SensorDataDto sensorData)
    {
        var value = sensorData.Value;

        if (value < 2)
        {
            _logger.LogError(
                "RISCO DE GEADA detectado! FieldId={FieldId}, Temperatura={Value}°C",
                sensorData.FieldId, value);

            await CreateAlertIfNotExistsAsync(
                fieldId: sensorData.FieldId,
                type: AlertType.FROST_RISK,
                severity: AlertSeverity.Critical,
                message: $"ALERTA DE GEADA: Temperatura crítica para a planta ({value:F1}°C < 2°C).",
                triggerValue: value);
        }
        else if (value > 32)
        {
            _logger.LogWarning(
                "Estresse Térmico detectado! FieldId={FieldId}, Temperatura={Value}°C",
                sensorData.FieldId, value);

            await CreateAlertIfNotExistsAsync(
                fieldId: sensorData.FieldId,
                type: AlertType.HEAT_STRESS,
                severity: AlertSeverity.High,
                message: $"Estresse Térmico: Calor excessivo detectado ({value:F1}°C > 32°C).",
                triggerValue: value);
        }
        else
        {
            _logger.LogInformation(
                "Temperatura em níveis normais: FieldId={FieldId}, Temperatura={Value}°C",
                sensorData.FieldId, value);
        }
    }

    #endregion

    #region Rainfall Rules

    private async Task ProcessRainfallRulesAsync(SensorDataDto sensorData)
    {
        var value = sensorData.Value;

        if (value > 20)
        {
            _logger.LogWarning(
                "Chuva Intensa detectada! FieldId={FieldId}, Precipitação={Value}mm/h",
                sensorData.FieldId, value);

            await CreateAlertIfNotExistsAsync(
                fieldId: sensorData.FieldId,
                type: AlertType.HEAVY_RAIN,
                severity: AlertSeverity.Medium,
                message: $"Chuva Intensa: Monitorar erosão do solo ({value:F1}mm/h > 20mm/h).",
                triggerValue: value);
        }
        else
        {
            _logger.LogInformation(
                "Precipitação em níveis normais: FieldId={FieldId}, Precipitação={Value}mm/h",
                sensorData.FieldId, value);
        }
    }

    #endregion

    #region Alert Creation

    private async Task CreateAlertIfNotExistsAsync(
        int fieldId,
        AlertType type,
        AlertSeverity severity,
        string message,
        double triggerValue)
    {
        var activeAlerts = await _alertRepository.GetActiveAlertsByFieldAsync(fieldId);
        var existingAlert = activeAlerts.FirstOrDefault(a => a.Type == type);

        if (existingAlert != null)
        {
            _logger.LogInformation(
                "Alerta {AlertType} já existe para FieldId={FieldId}, não criando duplicado. AlertId={AlertId}",
                type, fieldId, existingAlert.Id);
            return;
        }

        var alert = new Alert
        {
            FieldId = fieldId,
            Type = type,
            Severity = severity,
            Status = AlertStatus.Active,
            Message = message,
            TriggerValue = triggerValue,
            CreatedAt = DateTime.UtcNow
        };

        await _alertRepository.AddAsync(alert);
        await _alertRepository.SaveChangesAsync();

        _logger.LogWarning(
            "Alerta criado: Type={Type}, Severity={Severity}, FieldId={FieldId}, AlertId={AlertId}",
            type, severity, fieldId, alert.Id);
    }

    #endregion

    #region Mapping

    private SensorReading MapToEntity(SensorDataDto dto)
    {
        if (!Enum.TryParse<SensorType>(dto.SensorType, true, out var sensorType))
        {
            _logger.LogWarning(
                "Tipo de sensor desconhecido: {SensorType}, usando SoilHumidity como padrão",
                dto.SensorType);
            sensorType = SensorType.SoilHumidity;
        }

        return new SensorReading
        {
            FieldId = dto.FieldId,
            SensorType = sensorType,
            Value = dto.Value,
            Timestamp = dto.Timestamp,
            ProcessedAt = DateTime.UtcNow
        };
    }

    #endregion
}
