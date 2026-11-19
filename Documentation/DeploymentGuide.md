# Production Deployment & Hosting Guide
**Full-Stack Software Services Platform**

---

## 1. Production Architecture Overview

In production, the platform consists of:
1. **Frontend**: Static production build (`dist/`) served via Nginx or Cloudflare Pages / AWS S3 + CloudFront.
2. **Backend**: ASP.NET Core Kestrel process running in a Docker container or managed by `systemd` on Linux.
3. **Database**: Managed PostgreSQL instance (AWS RDS, Azure Database for PostgreSQL, DigitalOcean Managed DB) or hardened PostgreSQL on Ubuntu.
4. **Reverse Proxy & SSL**: Nginx with automated Let's Encrypt SSL certificates (Certbot).

---

## 2. Docker & Docker Compose Deployment

### 2.1 Backend Dockerfile (`Backend/Dockerfile`)
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SoftwareServices.Domain/SoftwareServices.Domain.csproj", "SoftwareServices.Domain/"]
COPY ["SoftwareServices.Application/SoftwareServices.Application.csproj", "SoftwareServices.Application/"]
COPY ["SoftwareServices.Infrastructure/SoftwareServices.Infrastructure.csproj", "SoftwareServices.Infrastructure/"]
COPY ["SoftwareServices.API/SoftwareServices.API.csproj", "SoftwareServices.API/"]
RUN dotnet restore "SoftwareServices.API/SoftwareServices.API.csproj"
COPY . .
WORKDIR "/src/SoftwareServices.API"
RUN dotnet publish "SoftwareServices.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000
ENTRYPOINT ["dotnet", "SoftwareServices.API.dll"]
```

### 2.2 Docker Compose (`docker-compose.yml`)
```yaml
version: '3.8'

services:
  db:
    image: postgres:16-alpine
    container_name: softwareservices_db
    environment:
      POSTGRES_DB: software_services_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${DB_PASSWORD:-StrongProductionPassword123!}
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./Database/02_CreateTables.sql:/docker-entrypoint-initdb.d/01_tables.sql
      - ./Database/03_SeedData.sql:/docker-entrypoint-initdb.d/02_seed.sql
      - ./Database/04_IndexesAndConstraints.sql:/docker-entrypoint-initdb.d/03_indexes.sql
    ports:
      - "5432:5432"
    restart: always

  api:
    build:
      context: ./Backend
      dockerfile: Dockerfile
    container_name: softwareservices_api
    depends_on:
      - db
    environment:
      ConnectionStrings__DefaultConnection: "Host=db;Port=5432;Database=software_services_db;Username=postgres;Password=${DB_PASSWORD:-StrongProductionPassword123!}"
      JwtSettings__Secret: "${JWT_SECRET:-YourStrong32ByteProductionSecretKeyHere!}"
      JwtSettings__Issuer: "SoftwareServicesAPI"
      JwtSettings__Audience: "SoftwareServicesUI"
    ports:
      - "5000:5000"
    restart: always

volumes:
  pgdata:
```

---

## 3. Nginx Reverse Proxy Configuration

Create an Nginx configuration file (e.g. `/etc/nginx/sites-available/softwareservices.conf`):

```nginx
server {
    listen 80;
    server_name yourdomain.com www.yourdomain.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name yourdomain.com www.yourdomain.com;

    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    # Frontend Static SPA
    root /var/www/softwareservices/frontend/dist;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # Backend API Proxy
    location /api/ {
        proxy_pass http://127.0.0.1:5000/api/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Uploads & File Size Limit
    client_max_body_size 25M;
}
```

---

## 4. Production Security Checklist

- [ ] Change `JwtSettings:Secret` in production to a secure 256-bit key.
- [ ] Change the default `admin` password immediately upon initial deployment.
- [ ] Store database passwords in environment variables or key vaults (AWS Secrets Manager, Azure Key Vault).
- [ ] Configure live SMTP settings for customer acknowledgement and quotation dispatch.
- [ ] Enable automated daily backups of PostgreSQL via `pg_dump` or cloud snapshots.
- [ ] Restrict database port `5432` to the internal application network only.
