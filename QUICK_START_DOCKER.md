# Quick Start Guide - Running APIs with Docker and Testing in Postman

## Step 1: Start All Services with Docker

### Prerequisites
- Docker Desktop installed and running
- At least 4GB RAM available
- Ports 1433, 5672, 15672, 5112, 5163, 5110, 5212, 5234 available

### Start Services

1. **Open PowerShell/Terminal in the project root directory**

2. **Build and start all services:**
   ```powershell
   docker-compose up -d --build
   ```

   This will:
   - Build Docker images for all services
   - Start SQL Server (port 1433)
   - Start RabbitMQ (ports 5672, 15672)
   - Start all microservices (AuthService, EmployeeService, SkillService, SearchService)
   - Start API Gateway (port 5112)

3. **Wait for services to be ready** (about 1-2 minutes):
   ```powershell
   docker-compose ps
   ```
   
   All services should show "Up" status. Wait until SQL Server and RabbitMQ health checks pass.

4. **Check service logs** (optional, to verify everything is running):
   ```powershell
   # View all logs
   docker-compose logs -f
   
   # View specific service logs
   docker-compose logs -f apigateway
   docker-compose logs -f authservice
   ```

## Step 2: Run Database Migrations

After services are running, you need to create the database schemas:

### Option 1: Run migrations from your local machine (Recommended)

1. **Update connection strings** in each service's `appsettings.json` to point to Docker SQL Server:
   ```
   Server=localhost,1433;Database=AuthServiceDb;User Id=sa;Password=YourStrong@Passw0rd;Encrypt=True;TrustServerCertificate=True;
   ```

2. **Run migrations:**
   ```powershell
   dotnet ef database update --project AuthService
   dotnet ef database update --project EmployeeService
   dotnet ef database update --project SkillService
   dotnet ef database update --project SearchService
   ```

### Option 2: Run migrations inside containers (if EF tools are available)

```powershell
docker-compose exec authservice dotnet ef database update --project /app
docker-compose exec employeeservice dotnet ef database update --project /app
docker-compose exec skillservice dotnet ef database update --project /app
docker-compose exec searchservice dotnet ef database update --project /app
```

## Step 3: Verify Services Are Running

### Check Service URLs

Open these URLs in your browser to verify services are running:

- **API Gateway:** http://localhost:5112/swagger
- **AuthService:** http://localhost:5163/swagger
- **EmployeeService:** http://localhost:5110/swagger
- **SkillService:** http://localhost:5212/swagger
- **SearchService:** http://localhost:5234/swagger
- **RabbitMQ Management UI:** http://localhost:15672 (username: `guest`, password: `guest`)

### Check Docker Containers

```powershell
docker-compose ps
```

All services should show "Up" status.

## Step 4: Test APIs in Postman

### Import Postman Collection

1. **Open Postman**

2. **Import the API Gateway collection:**
   - Click **Import** button
   - Select `Postman/ApiGateway_Collection.json`
   - The collection will appear in your workspace

3. **Create Postman Environment:**
   - Click **Environments** → **+** (Create Environment)
   - Name it: `API Gateway Local`
   - Add variable:
     - Variable: `gatewayUrl`
     - Initial Value: `http://localhost:5112`
     - Current Value: `http://localhost:5112`
   - Click **Save**
   - **Select this environment** from the dropdown (top right)

### Testing Sequence

#### 1. Register a User (No Authentication Required)

**Request:** `1. Authentication (No Token Required) > Register User`

- **Method:** POST
- **URL:** `http://localhost:5112/auth/register`
- **Headers:**
  ```
  Content-Type: application/json
  ```
- **Body (JSON):**
  ```json
  {
    "name": "John Doe",
    "email": "john.doe@example.com",
    "password": "Password123!",
    "role": "User"
  }
  ```
- **Expected Response:** 200 OK
- **Response Body:**
  ```json
  {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "role": "User"
  }
  ```
- **Note:** The Postman script automatically saves the token to `authToken` environment variable

#### 2. Login (Alternative to Register)

**Request:** `1. Authentication (No Token Required) > Login`

- **Method:** POST
- **URL:** `http://localhost:5112/auth/login`
- **Body (JSON):**
  ```json
  {
    "email": "john.doe@example.com",
    "password": "Password123!"
  }
  ```
- **Expected Response:** 200 OK with token

#### 3. Get All Employees (Requires Authentication)

**Request:** `2. Employee Management > Get All Employees`

- **Method:** GET
- **URL:** `http://localhost:5112/employee/employees`
- **Headers:**
  ```
  Authorization: Bearer {{authToken}}
  ```
- **Expected Response:** 200 OK with employee list (may be empty initially)

#### 4. Create Employee (Requires Authentication)

**Request:** `2. Employee Management > Create Employee`

- **Method:** POST
- **URL:** `http://localhost:5112/employee/employees`
- **Headers:**
  ```
  Authorization: Bearer {{authToken}}
  Content-Type: application/json
  ```
- **Body (JSON):**
  ```json
  {
    "name": "Jane Smith",
    "email": "jane.smith@example.com",
    "role": "Senior Developer"
  }
  ```
- **Expected Response:** 201 Created
- **Note:** This triggers an `EmployeeCreatedEvent` that SkillService will consume

#### 5. Get All Skills (Requires Authentication)

**Request:** `3. Skills Management > Get All Skills`

- **Method:** GET
- **URL:** `http://localhost:5112/skills/skills`
- **Headers:**
  ```
  Authorization: Bearer {{authToken}}
  ```
- **Expected Response:** 200 OK with skills list

#### 6. Create a Skill (Requires Authentication)

**Request:** `3. Skills Management > Create Skill`

- **Method:** POST
- **URL:** `http://localhost:5112/skills/skills`
- **Headers:**
  ```
  Authorization: Bearer {{authToken}}
  Content-Type: application/json
  ```
- **Body (JSON):**
  ```json
  {
    "skillName": "TypeScript"
  }
  ```
- **Expected Response:** 201 Created

#### 7. Rate Employee Skill (Requires Authentication)

**Request:** `3. Skills Management > Rate Employee Skill`

- **Method:** POST
- **URL:** `http://localhost:5112/skills/employees/{employeeId}/skills`
  - Replace `{employeeId}` with the ID from step 4
- **Headers:**
  ```
  Authorization: Bearer {{authToken}}
  Content-Type: application/json
  ```
- **Body (JSON):**
  ```json
  {
    "skillId": "11111111-1111-1111-1111-111111111111",
    "rating": 4,
    "trainingNeeded": false
  }
  ```
- **Expected Response:** 200 OK

#### 8. Search Skills (Requires Authentication)

**Request:** `4. Search > Search by Skill Name`

- **Method:** GET
- **URL:** `http://localhost:5112/search/search?skill=C%23`
- **Headers:**
  ```
  Authorization: Bearer {{authToken}}
  ```
- **Expected Response:** 200 OK with search results

## Service URLs Reference

### Through API Gateway (Port 5112)
- **Auth:** `http://localhost:5112/auth/*`
- **Employees:** `http://localhost:5112/employee/*`
- **Skills:** `http://localhost:5112/skills/*`
- **Search:** `http://localhost:5112/search/*`

### Direct Service URLs (for testing)
- **API Gateway:** http://localhost:5112
- **AuthService:** http://localhost:5163
- **EmployeeService:** http://localhost:5110
- **SkillService:** http://localhost:5212
- **SearchService:** http://localhost:5234
- **RabbitMQ Management:** http://localhost:15672

## Troubleshooting

### Services Won't Start

1. **Check if ports are in use:**
   ```powershell
   netstat -ano | findstr :5112
   ```

2. **Check Docker logs:**
   ```powershell
   docker-compose logs [service-name]
   ```

3. **Restart services:**
   ```powershell
   docker-compose down
   docker-compose up -d --build
   ```

### Database Connection Errors

1. **Wait for SQL Server to be ready** (check health status)
2. **Verify connection strings** in `appsettings.Docker.json` files
3. **Check SQL Server logs:**
   ```powershell
   docker-compose logs sqlserver
   ```

### Authentication Errors (401 Unauthorized)

1. **Verify token is set** in Postman environment
2. **Check token format:** Should start with `eyJ`
3. **Token may have expired** (default: 60 minutes) - Login again to get new token

### 404 Not Found

1. **Verify service is running:** `docker-compose ps`
2. **Check route path** - must match exactly (case-sensitive)
3. **Verify API Gateway routing** in `ApiGateway/appsettings.Docker.json`

### RabbitMQ Connection Issues

1. **Check RabbitMQ is running:**
   ```powershell
   docker-compose ps rabbitmq
   ```

2. **Access RabbitMQ Management UI:** http://localhost:15672
   - Username: `guest`
   - Password: `guest`

3. **Check RabbitMQ logs:**
   ```powershell
   docker-compose logs rabbitmq
   ```

## Stop Services

When you're done testing:

```powershell
# Stop all services (keeps data)
docker-compose down

# Stop and remove all data (fresh start)
docker-compose down -v
```

## Next Steps

- Explore the full Postman collection for more test scenarios
- Check `Postman/API_GATEWAY_TESTING_GUIDE.md` for detailed testing guide
- Monitor RabbitMQ Management UI to see event flow
- Check service logs to understand event-driven architecture

