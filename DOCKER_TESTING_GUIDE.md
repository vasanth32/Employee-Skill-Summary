# Docker Testing Guide

This guide walks you through testing the Dockerized Employee Skill Summary application.

## Step 1: Build and Start All Services

```bash
# Build and start all containers in detached mode
docker-compose up -d --build
```

This will:
- Build Docker images for all services
- Start SQL Server and RabbitMQ
- Start all application services
- Create Docker network and volumes

**Expected output:** You should see messages like:
```
Creating employeeskill-sqlserver ... done
Creating employeeskill-rabbitmq ... done
Creating employeeskill-authservice ... done
...
```

## Step 2: Verify All Services Are Running

```bash
# Check status of all containers
docker-compose ps
```

**Expected output:** All services should show `Up` status:
```
NAME                          STATUS
employeeskill-apigateway      Up
employeeskill-authservice     Up
employeeskill-employeeservice Up
employeeskill-rabbitmq        Up
employeeskill-searchservice   Up
employeeskill-skillservice    Up
employeeskill-sqlserver       Up
```

## Step 3: Check Service Logs

```bash
# View logs for all services
docker-compose logs -f

# Or view logs for a specific service
docker-compose logs -f sqlserver
docker-compose logs -f authservice
docker-compose logs -f apigateway
```

**What to look for:**
- SQL Server: "SQL Server is now ready for client connections"
- RabbitMQ: "Server startup complete"
- Services: No critical errors, application started successfully

## Step 4: Run Database Migrations

The services need their databases created. You have two options:

### Option A: Run Migrations from Host Machine (Recommended)

1. **Update connection strings temporarily** in your local `appsettings.json` files to point to Docker SQL Server:
   ```json
   "ConnectionStrings": {
     "AuthDatabase": "Server=localhost,1433;Database=AuthServiceDb;User Id=sa;Password=YourStrong@Passw0rd;Encrypt=True;TrustServerCertificate=True;"
   }
   ```

2. **Run migrations from project root:**
   ```bash
   # AuthService
   dotnet ef database update --project AuthService

   # EmployeeService
   dotnet ef database update --project EmployeeService

   # SkillService
   dotnet ef database update --project SkillService

   # SearchService
   dotnet ef database update --project SearchService
   ```

### Option B: Run Migrations Inside Container

```bash
# Note: This requires EF tools in the container, which may not be available
# You might need to install them first or use Option A

docker-compose exec authservice dotnet ef database update
```

## Step 5: Verify Database Creation

Connect to SQL Server using SQL Server Management Studio or Azure Data Studio:

- **Server:** `localhost,1433`
- **Authentication:** SQL Server Authentication
- **Username:** `sa`
- **Password:** `YourStrong@Passw0rd`

Verify these databases exist:
- `AuthServiceDb`
- `EmployeeServiceDb`
- `SkillServiceDb`
- `SearchServiceDb`

## Step 6: Test Services via Swagger UI

Open these URLs in your browser:

### API Gateway
- **URL:** http://localhost:5112/swagger
- **Purpose:** Main entry point for all API calls

### Individual Services (for direct testing)
- **AuthService:** http://localhost:5163/swagger
- **EmployeeService:** http://localhost:5110/swagger
- **SkillService:** http://localhost:5212/swagger
- **SearchService:** http://localhost:5234/swagger

### RabbitMQ Management UI
- **URL:** http://localhost:15672
- **Username:** `guest`
- **Password:** `guest`

## Step 7: Test API Endpoints

### 7.1 Register a New User

```bash
POST http://localhost:5112/auth/register
Content-Type: application/json

{
  "name": "Test User",
  "email": "test@example.com",
  "password": "Test@123456"
}
```

**Expected Response:** 200 OK with user details and token

### 7.2 Login

```bash
POST http://localhost:5112/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test@123456"
}
```

**Expected Response:** 200 OK with JWT token

**Save the token** for subsequent authenticated requests.

### 7.3 Create an Employee (Authenticated)

```bash
POST http://localhost:5112/employee/employees
Authorization: Bearer <your-jwt-token>
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "role": "Software Engineer"
}
```

**Expected Response:** 201 Created with employee details

### 7.4 Get All Employees

```bash
GET http://localhost:5112/employee/employees
Authorization: Bearer <your-jwt-token>
```

**Expected Response:** 200 OK with list of employees

### 7.5 Add a Skill Rating

```bash
POST http://localhost:5112/skills/employees/{employeeId}/skills
Authorization: Bearer <your-jwt-token>
Content-Type: application/json

{
  "skillName": "C#",
  "rating": 5
}
```

### 7.6 Search Employees

```bash
GET http://localhost:5112/search/employees?skillName=C#
Authorization: Bearer <your-jwt-token>
```

**Expected Response:** 200 OK with matching employees

## Step 8: Verify Event Flow (RabbitMQ)

1. **Open RabbitMQ Management UI:** http://localhost:15672
2. **Login** with `guest`/`guest`
3. **Navigate to Exchanges** tab
4. **Verify exchanges exist:**
   - `employee.events`
   - `skill.events`
5. **Create an employee** via API (Step 7.3)
6. **Check RabbitMQ** - you should see messages in the exchanges
7. **Verify SearchService** consumed the event by checking if the employee appears in search results

## Step 9: Test Service Health

```bash
# Check if services are responding
curl http://localhost:5112/swagger/index.html
curl http://localhost:5163/swagger/index.html
curl http://localhost:5110/swagger/index.html
curl http://localhost:5212/swagger/index.html
curl http://localhost:5234/swagger/index.html
```

## Step 10: Monitor Logs During Testing

Keep logs open in a separate terminal:

```bash
# Watch all logs
docker-compose logs -f

# Watch specific service
docker-compose logs -f apigateway
docker-compose logs -f employeeservice
docker-compose logs -f searchservice
```

## Common Issues and Solutions

### Issue 1: Services Won't Start

**Symptoms:** Containers exit immediately or show errors

**Solutions:**
```bash
# Check logs
docker-compose logs <service-name>

# Verify ports are not in use
netstat -an | findstr "5112 5163 5110 5212 5234 1433 5672 15672"

# Restart services
docker-compose down
docker-compose up -d --build
```

### Issue 2: Database Connection Errors

**Symptoms:** Services can't connect to SQL Server

**Solutions:**
```bash
# Wait for SQL Server to be fully ready (can take 30-60 seconds)
docker-compose logs sqlserver

# Verify SQL Server is accessible
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q "SELECT @@VERSION"

# Check connection string in appsettings.Docker.json
```

### Issue 3: RabbitMQ Connection Errors

**Symptoms:** Services can't connect to RabbitMQ

**Solutions:**
```bash
# Check RabbitMQ logs
docker-compose logs rabbitmq

# Verify RabbitMQ is running
docker-compose exec rabbitmq rabbitmq-diagnostics ping

# Access management UI: http://localhost:15672
```

### Issue 4: API Gateway Can't Reach Services

**Symptoms:** 502 Bad Gateway or connection refused

**Solutions:**
```bash
# Verify all services are running
docker-compose ps

# Check API Gateway logs
docker-compose logs apigateway

# Verify service names in ApiGateway/appsettings.Docker.json match docker-compose service names
```

### Issue 5: Database Migrations Fail

**Symptoms:** EF migrations fail to run

**Solutions:**
```bash
# Ensure SQL Server is ready
docker-compose logs sqlserver

# Verify connection string
# Use: Server=localhost,1433;Database=...;User Id=sa;Password=YourStrong@Passw0rd;Encrypt=True;TrustServerCertificate=True;

# Try running migrations one at a time
dotnet ef database update --project AuthService --verbose
```

## Cleanup and Restart

### Stop All Services
```bash
docker-compose down
```

### Stop and Remove All Data (Fresh Start)
```bash
docker-compose down -v
docker-compose up -d --build
```

### Rebuild Specific Service
```bash
docker-compose build authservice
docker-compose up -d authservice
```

## Performance Testing

### Check Resource Usage
```bash
# View container resource usage
docker stats

# View specific container
docker stats employeeskill-apigateway
```

### Load Testing
Use tools like:
- **Postman** - Collection runner
- **Apache JMeter**
- **k6**
- **Artillery**

## Next Steps

Once everything is working:
1. ✅ All services are running
2. ✅ Databases are created and migrated
3. ✅ API endpoints are accessible
4. ✅ Event flow through RabbitMQ is working
5. ✅ Search functionality is operational

You can now:
- Integrate with your CI/CD pipeline
- Set up monitoring and logging
- Configure production settings
- Scale services as needed

## Quick Test Script

Save this as `test-docker.ps1` (PowerShell) or `test-docker.sh` (Bash):

```powershell
# PowerShell Script
Write-Host "Testing Docker Services..." -ForegroundColor Green

# Check if containers are running
Write-Host "`nChecking container status..." -ForegroundColor Yellow
docker-compose ps

# Test API Gateway
Write-Host "`nTesting API Gateway..." -ForegroundColor Yellow
$response = Invoke-WebRequest -Uri "http://localhost:5112/swagger/index.html" -UseBasicParsing
if ($response.StatusCode -eq 200) {
    Write-Host "✓ API Gateway is responding" -ForegroundColor Green
} else {
    Write-Host "✗ API Gateway is not responding" -ForegroundColor Red
}

# Test Auth Service
Write-Host "`nTesting Auth Service..." -ForegroundColor Yellow
$response = Invoke-WebRequest -Uri "http://localhost:5163/swagger/index.html" -UseBasicParsing
if ($response.StatusCode -eq 200) {
    Write-Host "✓ Auth Service is responding" -ForegroundColor Green
} else {
    Write-Host "✗ Auth Service is not responding" -ForegroundColor Red
}

Write-Host "`nTest complete!" -ForegroundColor Green
```

Run it:
```powershell
.\test-docker.ps1
```

