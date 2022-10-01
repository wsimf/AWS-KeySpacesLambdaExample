FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["MeterReadingAPI/MeterReadingAPI.csproj", "MeterReadingAPI/"]
RUN dotnet restore "MeterReadingAPI/MeterReadingAPI.csproj"  /property:Configuration=Release -v d

COPY . .

WORKDIR "/src/MeterReadingAPI"
RUN dotnet build "MeterReadingAPI.csproj" -c Release -o /app/build -v d 

FROM build AS publish
RUN dotnet publish "MeterReadingAPI.csproj" \
    --configuration Release \ 
    --runtime linux-x64 \
    --self-contained false \ 
    --output /app/publish \
    -p:PublishReadyToRun=true  

FROM public.ecr.aws/lambda/dotnet:6 AS final

WORKDIR /var/task
COPY --from=publish /app/publish .

CMD [ "MeterReading.Web.API::MeterReading.Web.API.Function::FunctionHandler" ]
