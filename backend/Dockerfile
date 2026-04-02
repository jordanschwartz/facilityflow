FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore
COPY FacilityFlow.Core/FacilityFlow.Core.csproj FacilityFlow.Core/
COPY FacilityFlow.Application/FacilityFlow.Application.csproj FacilityFlow.Application/
COPY FacilityFlow.Infrastructure/FacilityFlow.Infrastructure.csproj FacilityFlow.Infrastructure/
COPY FacilityFlow.Api/FacilityFlow.Api.csproj FacilityFlow.Api/
RUN dotnet restore FacilityFlow.Api/FacilityFlow.Api.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish FacilityFlow.Api/FacilityFlow.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Create uploads directory
RUN mkdir -p /app/wwwroot/uploads

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "FacilityFlow.Api.dll"]
