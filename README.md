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

## 🚨 Regras de Negócio - Alerta de Seca

**Condições para criar Alerta de Seca:**

1. Sensor Type = `SoilHumidity`
2. Valor da leitura < 30%
3. Verificar se nas **últimas 24 horas** outras leituras do mesmo FieldId também ficaram < 30%
4. Se todas as leituras estiverem abaixo de 30% (ou for a primeira leitura crítica) → Criar Alert

**Comportamento:**
- Não cria alertas duplicados para o mesmo talhão
- Verifica alertas ativos antes de criar um novo
- Logs detalhados de cada etapa do processamento

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

Use o `agro-solutions-sensor-ingestion` para publicar dados ou publique diretamente no RabbitMQ:

```bash
# Via sensor-ingestion API
POST https://localhost:7001/api/sensor-data
Authorization: Bearer {token}
Content-Type: application/json

{
  "fieldId": 1,
  "sensorType": "SoilHumidity",
  "value": 25.5,
  "timestamp": "2026-02-07T12:00:00Z"
}
```

### 2. Verificar Processamento

- Checar logs do Worker
- Consultar tabela `SensorReadings` no PostgreSQL
- Consultar tabela `Alerts` para ver alertas criados

### 3. Verificar Alerta de Seca

Publicar múltiplas leituras com umidade < 30% para o mesmo FieldId:

```json
{"fieldId": 1, "sensorType": "SoilHumidity", "value": 28.0, "timestamp": "2026-02-07T10:00:00Z"}
{"fieldId": 1, "sensorType": "SoilHumidity", "value": 25.0, "timestamp": "2026-02-07T11:00:00Z"}
{"fieldId": 1, "sensorType": "SoilHumidity", "value": 22.0, "timestamp": "2026-02-07T12:00:00Z"}
```

Resultado esperado: 1 alerta criado na tabela `Alerts`

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
