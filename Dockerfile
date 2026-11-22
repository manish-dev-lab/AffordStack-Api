# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

# Copy all csproj files first for layer caching
COPY ["AffordStack.API/AffordStack.API.csproj", "AffordStack.API/"]
COPY ["AffordStack.Application/AffordStack.Application.csproj", "AffordStack.Application/"]
COPY ["AffordStack.Domain/AffordStack.Domain.csproj", "AffordStack.Domain/"]
COPY ["AffordStack.Infrastructure/AffordStack.Infrastructure.csproj", "AffordStack.Infrastructure/"]

RUN dotnet restore "AffordStack.API/AffordStack.API.csproj"

# Copy source and publish
COPY . .
WORKDIR "/src/AffordStack.API"
RUN dotnet publish "AffordStack.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AffordStack.API.dll"]
