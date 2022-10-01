FROM public.ecr.aws/lambda/dotnet:6 AS base
WORKDIR /app

EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MeterReadingAPI/MeterReadingAPI.csproj", "MeterReadingAPI/"]
RUN dotnet restore "MeterReadingAPI/MeterReadingAPI.csproj"  /property:Configuration=Release -v d
COPY . .

WORKDIR "/src/MeterReadingAPI"
RUN dotnet build "MeterReadingAPI.csproj" -c Release -o /app/build -v d 

FROM build AS publish
RUN dotnet publish "MeterReadingAPI.csproj" -c Release -o /app/publish -v d 

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MeterReading.Web.API.dll"]
