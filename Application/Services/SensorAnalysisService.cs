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

        // 1. Mapear e salvar a leitura no banco
        var reading = MapToEntity(sensorData);
        await _sensorReadingRepository.AddAsync(reading);
        await _sensorReadingRepository.SaveChangesAsync();

        _logger.LogInformation("Leitura salva com sucesso: Id={Id}", reading.Id);

        // 2. Aplicar regra de Alerta de Seca
        if (sensorData.SensorType.Equals("SoilHumidity", StringComparison.OrdinalIgnoreCase) 
            && sensorData.Value < 30)
        {
            _logger.LogInformation(
                "Umidade do solo crítica detectada: FieldId={FieldId}, Value={Value}%",
                sensorData.FieldId, sensorData.Value);

            await CheckAndCreateDroughtAlertAsync(sensorData.FieldId);
        }
    }

    private async Task CheckAndCreateDroughtAlertAsync(int fieldId)
    {
        // Buscar leituras de SoilHumidity das últimas 24 horas
        var last24HoursReadings = await _sensorReadingRepository
            .GetLast24HoursByFieldAsync(fieldId, SensorType.SoilHumidity);

        var readingsList = last24HoursReadings.ToList();

        // Verificar se todas as leituras estão abaixo de 30% ou se é a primeira leitura crítica
        var allBelowThreshold = readingsList.Any() && readingsList.All(r => r.Value < 30);
        var isFirstCriticalReading = !readingsList.Any();

        if (allBelowThreshold || isFirstCriticalReading)
        {
            _logger.LogWarning(
                "Condição de Alerta de Seca detectada para FieldId={FieldId}. " +
                "Leituras nas últimas 24h: {Count}, Todas abaixo de 30%: {AllBelow}",
                fieldId, readingsList.Count, allBelowThreshold);

            // Verificar se já não existe um alerta ativo para este talhão
            var activeAlerts = await _alertRepository.GetActiveAlertsByFieldAsync(fieldId);
            
            if (!activeAlerts.Any())
            {
                await CreateDroughtAlertAsync(fieldId);
            }
            else
            {
                _logger.LogInformation(
                    "Alerta de seca já existe para FieldId={FieldId}, não criando duplicado",
                    fieldId);
            }
        }
        else
        {
            _logger.LogInformation(
                "Condição de Alerta de Seca não atendida para FieldId={FieldId}. " +
                "Leituras nas últimas 24h acima do limite.",
                fieldId);
        }
    }

    private async Task CreateDroughtAlertAsync(int fieldId)
    {
        var alert = new Alert
        {
            FieldId = fieldId,
            Type = AlertType.DroughtAlert,
            Status = AlertStatus.Active,
            Message = $"Alerta de Seca: Umidade do solo abaixo de 30% nas últimas 24 horas no Talhão {fieldId}",
            CreatedAt = DateTime.UtcNow
        };

        await _alertRepository.AddAsync(alert);
        await _alertRepository.SaveChangesAsync();

        _logger.LogWarning(
            "Alerta de Seca criado: AlertId={AlertId}, FieldId={FieldId}",
            alert.Id, fieldId);
    }

    private SensorReading MapToEntity(SensorDataDto dto)
    {
        // Mapear string para enum
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
}
