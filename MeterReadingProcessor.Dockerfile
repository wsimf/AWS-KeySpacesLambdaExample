FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MeterReadingProcessor/MeterReadingProcessor.csproj", "MeterReadingProcessor/"]

RUN dotnet restore "MeterReadingProcessor/MeterReadingProcessor.csproj" /property:Configuration=Release -v d
COPY . .

WORKDIR "/src/MeterReadingProcessor"
RUN dotnet build "MeterReadingProcessor.csproj" -c Release -o /app/build -v d 

FROM build AS publish
RUN dotnet publish "MeterReadingProcessor.csproj" -c Release -o /app/publish -v d

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MeterReading.Processor.dll"]
