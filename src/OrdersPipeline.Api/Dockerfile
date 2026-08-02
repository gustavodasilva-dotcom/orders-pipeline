FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/OrdersPipeline.Api/OrdersPipeline.Api.csproj", "src/OrdersPipeline.Api/"]
RUN dotnet restore "src/OrdersPipeline.Api/OrdersPipeline.Api.csproj"

COPY . .
WORKDIR "/src/src/OrdersPipeline.Api"
RUN dotnet publish "OrdersPipeline.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrdersPipeline.Api.dll"]
