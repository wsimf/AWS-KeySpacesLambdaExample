FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["MeterReadingProcessor/MeterReadingProcessor.csproj", "MeterReadingProcessor/"]
RUN dotnet restore "MeterReadingProcessor/MeterReadingProcessor.csproj" /property:Configuration=Release -v d

COPY . .

WORKDIR "/src/MeterReadingProcessor"
RUN dotnet build "MeterReadingProcessor.csproj" -c Release -o /app/build -v d 

FROM build AS publish
RUN dotnet publish "MeterReadingProcessor.csproj" \
    --configuration Release \ 
    --runtime linux-x64 \
    --self-contained false \ 
    --output /app/publish \
    -p:PublishReadyToRun=true  

FROM public.ecr.aws/lambda/dotnet:6 AS final

WORKDIR /var/task
COPY --from=publish /app/publish .

CMD [ "MeterReading.Processor::MeterReading.Processor.Function::FunctionHandler" ]
