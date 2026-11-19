# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

# Copy all csproj files first for layer caching
COPY ["SoftwareServices.API/SoftwareServices.API.csproj", "SoftwareServices.API/"]
COPY ["SoftwareServices.Application/SoftwareServices.Application.csproj", "SoftwareServices.Application/"]
COPY ["SoftwareServices.Domain/SoftwareServices.Domain.csproj", "SoftwareServices.Domain/"]
COPY ["SoftwareServices.Infrastructure/SoftwareServices.Infrastructure.csproj", "SoftwareServices.Infrastructure/"]

RUN dotnet restore "SoftwareServices.API/SoftwareServices.API.csproj"

# Copy source and publish
COPY . .
WORKDIR "/src/SoftwareServices.API"
RUN dotnet publish "SoftwareServices.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SoftwareServices.API.dll"]
