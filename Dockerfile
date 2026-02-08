# 1ª Fase: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY Domain/*.csproj ./Domain/
COPY Application/*.csproj ./Application/
COPY Infrastructure/*.csproj ./Infrastructure/
COPY AgroSolutions.Analysis.Alerts/*.csproj ./AgroSolutions.Analysis.Alerts/

RUN dotnet restore ./AgroSolutions.Analysis.Alerts/AgroSolutions.Analysis.Alerts.csproj

COPY . .

RUN dotnet publish ./AgroSolutions.Analysis.Alerts/AgroSolutions.Analysis.Alerts.csproj -c Release -o /app --no-restore

# 2ª Fase: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app

RUN addgroup -g 1000 appgroup && adduser -u 1000 -G appgroup -s /bin/sh -D appuser
RUN chown -R appuser:appgroup /app
USER appuser

COPY --from=build /app ./

ENTRYPOINT ["dotnet", "AgroSolutions.Analysis.Alerts.dll"]
