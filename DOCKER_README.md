# Docker Setup Guide

This guide explains how to run the Employee Skill Summary application using Docker and Docker Compose.

## Prerequisites

- Docker Desktop (Windows/Mac) or Docker Engine + Docker Compose (Linux)
- At least 4GB of available RAM
- At least 10GB of free disk space

## Quick Start

1. **Build and start all services:**
   ```bash
   docker-compose up -d --build
   ```

2. **View logs:**
   ```bash
   # All services
   docker-compose logs -f
   
   # Specific service
   docker-compose logs -f apigateway
   ```

3. **Stop all services:**
   ```bash
   docker-compose down
   ```

4. **Stop and remove volumes (clears databases):**
   ```bash
   docker-compose down -v
   ```

## Services

The docker-compose.yml includes the following services:

### Infrastructure Services
- **sqlserver**: SQL Server 2019 (port 1433)
  - **Note**: SQL Server 2018 doesn't have a direct Docker image. Using SQL Server 2019 which is backward compatible with SQL Server 2018.
  - Username: `sa`
  - Password: `YourStrong@Passw0rd`
  - Databases: AuthServiceDb, EmployeeServiceDb, SkillServiceDb, SearchServiceDb

- **rabbitmq**: RabbitMQ with Management UI (ports 5672, 15672)
  - Username: `guest`
  - Password: `guest`
  - Management UI: http://localhost:15672

### Application Services
- **apigateway**: API Gateway (port 5112)
  - URL: http://localhost:5112
  - Swagger: http://localhost:5112/swagger

- **authservice**: Authentication Service (port 5163)
  - URL: http://localhost:5163
  - Swagger: http://localhost:5163/swagger

- **employeeservice**: Employee Service (port 5110)
  - URL: http://localhost:5110
  - Swagger: http://localhost:5110/swagger

- **skillservice**: Skill Service (port 5212)
  - URL: http://localhost:5212
  - Swagger: http://localhost:5212/swagger

- **searchservice**: Search Service (port 5234)
  - URL: http://localhost:5234
  - Swagger: http://localhost:5234/swagger

## Database Setup

After starting the services, you need to run Entity Framework migrations to create the database schemas:

```bash
# AuthService
docker-compose exec authservice dotnet ef database update --project /app

# EmployeeService
docker-compose exec employeeservice dotnet ef database update --project /app

# SkillService
docker-compose exec skillservice dotnet ef database update --project /app

# SearchService
docker-compose exec searchservice dotnet ef database update --project /app
```

**Note:** The EF tools might not be available in the runtime image. If the above commands don't work, you can:

1. Run migrations from your local machine pointing to the Docker SQL Server:
   ```bash
   # Update connection strings in appsettings.json to:
   # Server=localhost,1433;Database=AuthServiceDb;User Id=sa;Password=YourStrong@Passw0rd;Encrypt=True;TrustServerCertificate=True;
   
   dotnet ef database update --project AuthService
   dotnet ef database update --project EmployeeService
   dotnet ef database update --project SkillService
   dotnet ef database update --project SearchService
   ```

2. Or create a migration script and run it manually in SQL Server.

## Configuration

Each service uses Docker-specific configuration files:
- `ApiGateway/appsettings.Docker.json`
- `AuthService/appsettings.Docker.json`
- `EmployeeService/appsettings.Docker.json`
- `SkillService/appsettings.Docker.json`
- `SearchService/appsettings.Docker.json`

These files are automatically loaded when `ASPNETCORE_ENVIRONMENT=Docker` is set (which is configured in docker-compose.yml).

## Networking

All services are connected via a Docker bridge network (`employeeskill-network`), allowing them to communicate using service names:
- `sqlserver` - SQL Server hostname
- `rabbitmq` - RabbitMQ hostname
- `authservice`, `employeeservice`, `skillservice`, `searchservice`, `apigateway` - Service hostnames

## Volumes

Docker volumes are created to persist data:
- `sqlserver-data`: SQL Server database files
- `rabbitmq-data`: RabbitMQ data and configuration

To clear all data, use:
```bash
docker-compose down -v
```

## Troubleshooting

### Services won't start
1. Check logs: `docker-compose logs [service-name]`
2. Ensure ports are not already in use
3. Verify Docker has enough resources allocated

### Database connection issues
1. Wait for SQL Server to be fully ready (healthcheck passes)
2. Verify connection string in appsettings.Docker.json
3. Check SQL Server logs: `docker-compose logs sqlserver`

### RabbitMQ connection issues
1. Wait for RabbitMQ to be fully ready
2. Verify RabbitMQ configuration in appsettings.Docker.json
3. Check RabbitMQ logs: `docker-compose logs rabbitmq`
4. Access management UI: http://localhost:15672

### API Gateway can't reach services
1. Verify all backend services are running: `docker-compose ps`
2. Check API Gateway logs: `docker-compose logs apigateway`
3. Verify service names in ApiGateway/appsettings.Docker.json match docker-compose service names

## Building Individual Services

To build a specific service:
```bash
docker-compose build [service-name]
```

Example:
```bash
docker-compose build authservice
```

## Viewing Service Status

```bash
docker-compose ps
```

## Accessing Service Shells

To access a service container shell:
```bash
docker-compose exec [service-name] /bin/bash
```

Example:
```bash
docker-compose exec authservice /bin/bash
```

## Production Considerations

For production deployment, consider:

1. **Security:**
   - Change default SQL Server password
   - Change RabbitMQ credentials
   - Use strong JWT secrets
   - Enable HTTPS

2. **Performance:**
   - Use production-ready SQL Server configuration
   - Configure RabbitMQ for high availability
   - Set appropriate resource limits in docker-compose.yml

3. **Monitoring:**
   - Add health check endpoints
   - Integrate logging and monitoring solutions
   - Set up alerting

4. **Scaling:**
   - Use Docker Swarm or Kubernetes for orchestration
   - Configure load balancing
   - Use external database and message broker services

