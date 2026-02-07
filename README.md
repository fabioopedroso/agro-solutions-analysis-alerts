# AgroSolutions Analysis Alerts - Worker Service

Worker Service que consome mensagens da fila RabbitMQ de dados de sensores, aplica regras de negócio de análise e persiste no PostgreSQL seguindo arquitetura DDD (Domain-Driven Design).

## 🏗️ Arquitetura DDD

O projeto segue Clean Architecture com as seguintes camadas:

- **Domain**: Entidades, Enums e Interfaces de Repositórios
- **Application**: DTOs, Services e lógica de aplicação
- **Infrastructure**: Implementações (EF Core, RabbitMQ Consumer)
- **Worker Host**: Configuração e execução do serviço

## 🔄 Fluxo de Processamento

```
RabbitMQ Queue (sensor-data-queue)
    ↓
RabbitMqConsumer (BackgroundService)
    ↓
SensorAnalysisService
    ↓ ↓
SensorReadingRepository    AlertRepository
    ↓ ↓
PostgreSQL Database
```

## 📊 Entidades

### SensorReading
Armazena todas as leituras recebidas dos sensores:
- FieldId (ID do Talhão)
- SensorType (SoilHumidity, Temperature, Rainfall)
- Value (Valor da leitura)
- Timestamp (Quando foi capturada)
- ProcessedAt (Quando foi processada)

### Alert
Armazena alertas gerados pelas regras de negócio:
- FieldId (ID do Talhão)
- Type (DroughtAlert)
- Status (Active, Resolved)
- Message (Descrição do alerta)
- CreatedAt / ResolvedAt

## 🚨 Regras de Negócio Implementadas

O sistema processa três tipos de sensores e aplica as seguintes regras:

### 1. Umidade do Solo (SoilHumidity)

| Condição | Tipo de Alerta | Severidade | Mensagem |
|----------|----------------|------------|----------|
| **< 20%** | `DROUGHT_CRITICAL` | 🔴 **Critical** | "PERIGO: Seca severa detectada (X%). Risco de perda da cultura." |
| **< 30%** | `DROUGHT_WARNING` | 🟠 **High** | "Alerta de Seca: Umidade abaixo do nível ideal (X% < 30%)." |
| **> 80%** | `SATURATION` | 🟡 **Medium** | "Solo Saturado: Risco de apodrecimento da raiz (X% > 80%)." |

**Comportamento:**
- Seca Crítica tem prioridade (se < 20%, não cria alerta de warning)
- Verifica alertas ativos do mesmo tipo antes de criar duplicados
- Logs detalhados em cada verificação

### 2. Temperatura (Temperature)

| Condição | Tipo de Alerta | Severidade | Mensagem |
|----------|----------------|------------|----------|
| **< 2°C** | `FROST_RISK` | 🔴 **Critical** | "ALERTA DE GEADA: Temperatura crítica para a planta (X°C < 2°C)." |
| **> 32°C** | `HEAT_STRESS` | 🟠 **High** | "Estresse Térmico: Calor excessivo detectado (X°C > 32°C)." |

**Comportamento:**
- Alertas críticos para temperaturas extremas
- Não cria alertas duplicados
- Temperatura entre 2°C e 32°C é considerada normal

### 3. Precipitação (Rainfall)

| Condição | Tipo de Alerta | Severidade | Mensagem |
|----------|----------------|------------|----------|
| **> 20mm/h** | `HEAVY_RAIN` | 🟡 **Medium** | "Chuva Intensa: Monitorar erosão do solo (Xmm/h > 20mm/h)." |

**Comportamento:**
- Alerta quando precipitação excede 20mm por hora
- Indica risco de erosão do solo
- Não cria alertas duplicados

### Tipos de Alerta (Enum)

```csharp
public enum AlertType
{
    DROUGHT_WARNING,        // Seca - Atenção (< 30%)
    DROUGHT_CRITICAL,       // Seca Severa (< 20%)
    SATURATION,            // Solo Saturado (> 80%)
    FROST_RISK,            // Risco de Geada (< 2°C)
    HEAT_STRESS,           // Estresse Térmico (> 32°C)
    HEAVY_RAIN             // Chuva Intensa (> 20mm/h)
}
```

### Níveis de Severidade (Enum)

```csharp
public enum AlertSeverity
{
    Low,        // 🟢 Baixa
    Medium,     // 🟡 Média
    High,       // 🟠 Alta
    Critical    // 🔴 Crítica
}
```

### Lógica de Prevenção de Duplicados

O sistema **não cria alertas duplicados** do mesmo tipo para o mesmo talhão:
- Verifica alertas ativos antes de criar um novo
- Compara por `FieldId` + `Type` + `Status = Active`
- Logs informativos quando detecta duplicatas

### Campos do Alert

```csharp
public class Alert
{
    public int Id { get; set; }
    public int FieldId { get; set; }
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }    // NOVO
    public AlertStatus Status { get; set; }
    public string Message { get; set; }
    public double TriggerValue { get; set; }        // NOVO - Valor que gerou o alerta
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
```

## ⚙️ Configuração

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=agro_analysis_alerts;Username=postgres;Password=postgres"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": "5672",
    "Username": "guest",
    "Password": "guest",
    "QueueName": "sensor-data-queue"
  }
}
```

### Variáveis de Ambiente (Produção)

```bash
ConnectionStrings__DefaultConnection="Host=prod-db;Database=agro_analysis_alerts;..."
RabbitMQ__Host="prod-rabbitmq"
RabbitMQ__Port="5672"
RabbitMQ__Username="user"
RabbitMQ__Password="pass"
RabbitMQ__QueueName="sensor-data-queue"
```

## 🗄️ Banco de Dados

### Criar Banco de Dados

```sql
CREATE DATABASE agro_analysis_alerts;
```

### Executar Migrations

```bash
# Na raiz do projeto
dotnet ef database update --project Infrastructure --startup-project AgroSolutions.Analysis.Alerts
```

### Tabelas Criadas

- **SensorReadings**: Histórico de todas as leituras
- **Alerts**: Alertas gerados pelo sistema

## 🚀 Como Executar

### Pré-requisitos
- .NET 8.0 SDK
- PostgreSQL
- RabbitMQ

### Docker Compose (Opcional)

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: agro_analysis_alerts
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
  
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
```

### Executar o Worker

```bash
cd AgroSolutions.Analysis.Alerts
dotnet run
```

## 📝 Logs

O Worker gera logs detalhados de:
- Conexão com RabbitMQ
- Mensagens recebidas
- Leituras processadas
- Alertas criados
- Erros e exceções

Exemplo:
```
[Information] RabbitMQ Consumer executando e aguardando mensagens na fila: sensor-data-queue
[Information] Mensagem recebida da fila: {"fieldId":1,"sensorType":"SoilHumidity","value":25.5,...}
[Information] Processando leitura do sensor: FieldId=1, SensorType=SoilHumidity, Value=25.5
[Warning] Umidade do solo crítica detectada: FieldId=1, Value=25.5%
[Warning] Alerta de Seca criado: AlertId=1, FieldId=1
```

## 🧪 Testando

### 1. Publicar Mensagem de Teste na Fila

Use o `agro-solutions-sensor-ingestion` para publicar dados ou publique diretamente no RabbitMQ.

#### Exemplo 1: Testar Seca Crítica (< 20%)
```json
POST https://localhost:7001/api/sensor-data
Authorization: Bearer {token}
Content-Type: application/json

{
  "fieldId": 1,
  "sensorType": "SoilHumidity",
  "value": 18.5,
  "timestamp": "2026-02-07T12:00:00Z"
}
```
**Resultado Esperado:** Alerta `DROUGHT_CRITICAL` com severidade `Critical`

#### Exemplo 2: Testar Alerta de Seca (< 30%)
```json
{
  "fieldId": 2,
  "sensorType": "SoilHumidity",
  "value": 25.0,
  "timestamp": "2026-02-07T12:00:00Z"
}
```
**Resultado Esperado:** Alerta `DROUGHT_WARNING` com severidade `High`

#### Exemplo 3: Testar Solo Saturado (> 80%)
```json
{
  "fieldId": 3,
  "sensorType": "SoilHumidity",
  "value": 85.0,
  "timestamp": "2026-02-07T12:00:00Z"
}
```
**Resultado Esperado:** Alerta `SATURATION` com severidade `Medium`

#### Exemplo 4: Testar Risco de Geada (< 2°C)
```json
{
  "fieldId": 1,
  "sensorType": "Temperature",
  "value": 0.5,
  "timestamp": "2026-02-07T06:00:00Z"
}
```
**Resultado Esperado:** Alerta `FROST_RISK` com severidade `Critical`

#### Exemplo 5: Testar Estresse Térmico (> 32°C)
```json
{
  "fieldId": 2,
  "sensorType": "Temperature",
  "value": 35.5,
  "timestamp": "2026-02-07T14:00:00Z"
}
```
**Resultado Esperado:** Alerta `HEAT_STRESS` com severidade `High`

#### Exemplo 6: Testar Chuva Intensa (> 20mm/h)
```json
{
  "fieldId": 1,
  "sensorType": "Rainfall",
  "value": 25.0,
  "timestamp": "2026-02-07T16:00:00Z"
}
```
**Resultado Esperado:** Alerta `HEAVY_RAIN` com severidade `Medium`

### 2. Verificar Processamento

**No Worker (Logs):**
```log
[Information] Mensagem recebida da fila: {"fieldId":1,"sensorType":"SoilHumidity","value":18.5,...}
[Information] Processando leitura do sensor: FieldId=1, SensorType=SoilHumidity, Value=18.5
[Information] Leitura salva com sucesso: Id=1
[Error] SECA CRÍTICA detectada! FieldId=1, Umidade=18.5%
[Warning] Alerta criado: Type=DROUGHT_CRITICAL, Severity=Critical, FieldId=1, AlertId=1
[Information] Mensagem processada e confirmada (ACK)
```

**No PostgreSQL:**
```sql
-- Verificar leituras processadas
SELECT * FROM "SensorReadings" ORDER BY "ProcessedAt" DESC LIMIT 10;

-- Verificar alertas gerados
SELECT 
    "Id",
    "FieldId",
    "Type",
    "Severity",
    "Status",
    "TriggerValue",
    "Message",
    "CreatedAt"
FROM "Alerts" 
WHERE "Status" = 'Active'
ORDER BY "Severity" DESC, "CreatedAt" DESC;

-- Contar alertas por tipo
SELECT 
    "Type",
    "Severity",
    COUNT(*) as Total
FROM "Alerts"
WHERE "Status" = 'Active'
GROUP BY "Type", "Severity"
ORDER BY Total DESC;
```

### 3. Testar Prevenção de Duplicados

Publicar a mesma mensagem duas vezes:
```json
// Primeira mensagem
{"fieldId": 1, "sensorType": "SoilHumidity", "value": 18.5, "timestamp": "2026-02-07T12:00:00Z"}

// Segunda mensagem (mesmo FieldId e valor crítico)
{"fieldId": 1, "sensorType": "SoilHumidity", "value": 19.0, "timestamp": "2026-02-07T12:05:00Z"}
```

**Resultado Esperado:**
- 1º Alerta criado com sucesso
- 2º Log: "Alerta DROUGHT_CRITICAL já existe para FieldId=1, não criando duplicado"

### 4. Cenário de Teste Completo

**Simular um dia de monitoramento:**

```json
// 06:00 - Madrugada fria - GEADA
{"fieldId": 1, "sensorType": "Temperature", "value": 1.0, "timestamp": "2026-02-07T06:00:00Z"}

// 08:00 - Solo seco após a noite
{"fieldId": 1, "sensorType": "SoilHumidity", "value": 22.0, "timestamp": "2026-02-07T08:00:00Z"}

// 14:00 - Calor intenso - ESTRESSE TÉRMICO
{"fieldId": 1, "sensorType": "Temperature", "value": 36.0, "timestamp": "2026-02-07T14:00:00Z"}

// 16:00 - Tempestade - CHUVA INTENSA
{"fieldId": 1, "sensorType": "Rainfall", "value": 28.0, "timestamp": "2026-02-07T16:00:00Z"}

// 17:00 - Solo encharcado após chuva - SATURAÇÃO
{"fieldId": 1, "sensorType": "SoilHumidity", "value": 85.0, "timestamp": "2026-02-07T17:00:00Z"}
```

**Resultado Esperado:** 5 alertas criados para o Field ID 1:
- `FROST_RISK` (Critical)
- `DROUGHT_CRITICAL` (Critical)
- `HEAT_STRESS` (High)
- `HEAVY_RAIN` (Medium)
- `SATURATION` (Medium)

## 🔧 Tecnologias

- **.NET 8.0 Worker Service**
- **Entity Framework Core 8.0**
- **PostgreSQL** (Npgsql.EntityFrameworkCore.PostgreSQL)
- **RabbitMQ.Client 6.8.1**
- **Arquitetura DDD/Clean Architecture**

## 📁 Estrutura de Pastas

```
agro-solutions-analysis-alerts/
├── Domain/                         # Camada de Domínio
│   ├── Entities/
│   │   ├── SensorReading.cs
│   │   └── Alert.cs
│   ├── Enums/
│   │   ├── SensorType.cs
│   │   ├── AlertType.cs
│   │   └── AlertStatus.cs
│   └── Interfaces/
│       ├── ISensorReadingRepository.cs
│       └── IAlertRepository.cs
├── Application/                    # Camada de Aplicação
│   ├── DTOs/
│   │   └── SensorDataDto.cs
│   ├── Interfaces/
│   │   └── ISensorAnalysisService.cs
│   └── Services/
│       └── SensorAnalysisService.cs
├── Infrastructure/                 # Camada de Infraestrutura
│   ├── Messaging/
│   │   ├── RabbitMqConsumer.cs
│   │   └── RabbitMqSettings.cs
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── SensorReadingConfiguration.cs
│   │   │   └── AlertConfiguration.cs
│   │   └── Repositories/
│   │       ├── SensorReadingRepository.cs
│   │       └── AlertRepository.cs
│   ├── Migrations/
│   └── DependencyInjection.cs
└── AgroSolutions.Analysis.Alerts/  # Worker Host
    ├── Program.cs
    ├── appsettings.json
    └── appsettings.Development.json
```

## 🔄 Integração com Outros Serviços

Este Worker faz parte do ecossistema AgroSolutions:

- **agro-solutions-sensor-ingestion**: Publica dados na fila `sensor-data-queue`
- **agro-solutions-properties-fields**: Gerencia dados de Talhões (Fields)
- **Consumidores futuros**: Podem criar outros workers para processar alertas

## 📈 Próximos Passos

- [ ] Implementar resolução automática de alertas
- [ ] Adicionar mais regras de negócio (ex: Alerta de Temperatura Alta)
- [ ] Criar API REST para consultar alertas
- [ ] Implementar notificações (email, SMS, push)
- [ ] Adicionar Health Checks
- [ ] Implementar métricas e monitoramento
- [ ] Adicionar testes unitários e de integração
- [ ] Dead Letter Queue para mensagens com erro

## ⚠️ Tratamento de Erros

- **NACK sem requeue**: Mensagens com erro não voltam para a fila (evita loops infinitos)
- **Logs detalhados**: Todos os erros são logados com stack trace
- **Transações**: SaveChanges só após processamento completo

## 🛡️ Resiliência

- **AutomaticRecoveryEnabled**: Reconexão automática ao RabbitMQ
- **Heartbeat**: 60 segundos
- **QoS**: Processa 1 mensagem por vez (prefetchCount: 1)
- **Reconnection**: Intervalo de 10 segundos

---

**Desenvolvido seguindo os princípios SOLID e Clean Architecture** 🚀
