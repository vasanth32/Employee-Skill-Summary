# SkillService Testing Guide

## Prerequisites

1. **SQL Server** - Running and accessible
2. **RabbitMQ** - Running on localhost (default port 5672)
3. **Database Migration** - Run migrations first

## Step 1: Setup Database

```powershell
cd SkillService
dotnet ef migrations add InitialCreate
dotnet ef database update
```

This will:
- Create the `SkillServiceDb` database
- Create tables: `Skills`, `EmployeeSkills`, `EmployeeReferences`
- Seed 10 initial skills (C#, Java, Python, JavaScript, SQL, React, Angular, Node.js, Docker, Kubernetes)

## Step 2: Start RabbitMQ

Make sure RabbitMQ is running:
```powershell
# If using Docker:
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Access management UI at: http://localhost:15672 (guest/guest)
```

## Step 3: Start Services

### Terminal 1: Start EmployeeService
```powershell
cd EmployeeService
dotnet run
```
- Runs on: http://localhost:5110
- Swagger: http://localhost:5110/swagger

### Terminal 2: Start SkillService
```powershell
cd SkillService
dotnet run
```
- Runs on: http://localhost:5212
- Swagger: http://localhost:5212/swagger

## Step 4: Test Endpoints

### Test 1: Get All Skills (Should show seeded skills)
```http
GET http://localhost:5212/skills
```

Expected: Returns 10 skills (C#, Java, Python, etc.)

### Test 2: Create a New Skill
```http
POST http://localhost:5212/skills
Content-Type: application/json

{
  "skillName": "TypeScript"
}
```

Expected: Returns created skill with SkillId

### Test 3: Create an Employee (in EmployeeService)
This will trigger the `EmployeeCreatedEvent` which SkillService consumes.

```http
POST http://localhost:5110/employees
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "role": "Software Engineer"
}
```

**Check SkillService logs** - You should see:
```
Received EmployeeCreatedEvent: EmployeeId=..., Name=John Doe, Role=Software Engineer
Stored employee reference: EmployeeId=...
```

**Verify in Database:**
```sql
SELECT * FROM EmployeeReferences WHERE Email = 'john.doe@example.com'
```

### Test 4: Rate a Skill for an Employee
First, get a SkillId from the skills list, then:

```http
POST http://localhost:5212/employees/{employeeId}/skills
Content-Type: application/json

{
  "skillId": "11111111-1111-1111-1111-111111111111",  // C# skill ID
  "rating": 4,
  "trainingNeeded": false
}
```

**Check RabbitMQ** - A `SkillRatedEvent` should be published to `skill.events` exchange.

**Verify in Database:**
```sql
SELECT es.*, s.SkillName 
FROM EmployeeSkills es
JOIN Skills s ON es.SkillId = s.SkillId
WHERE es.EmployeeId = '{employeeId}'
```

### Test 5: Update Skill Rating
Rate the same skill again with different rating:

```http
POST http://localhost:5212/employees/{employeeId}/skills
Content-Type: application/json

{
  "skillId": "11111111-1111-1111-1111-111111111111",
  "rating": 5,
  "trainingNeeded": false
}
```

Expected: Updates existing rating (doesn't create duplicate)

### Test 6: Search Skills
```http
GET http://localhost:5212/skills/search?skill=C%23&rating=3
```

This searches for employees with C# skill rated 3 or higher.

### Test 7: Search by Rating Only
```http
GET http://localhost:5212/skills/search?rating=4
```

Returns all employee skills with rating >= 4

## Step 5: Verify Event Flow

### Verify EmployeeCreatedEvent Consumption

1. Create an employee in EmployeeService
2. Check SkillService console logs for consumption message
3. Verify `EmployeeReferences` table has the new employee

### Verify SkillRatedEvent Publishing

1. Rate a skill for an employee
2. Check RabbitMQ Management UI (http://localhost:15672)
   - Go to Exchanges → `skill.events`
   - Check message count
3. Or use RabbitMQ CLI:
```powershell
rabbitmqctl list_exchanges
rabbitmqctl list_bindings
```

## Step 6: Test Validation

### Test Invalid Skill Creation
```http
POST http://localhost:5212/skills
Content-Type: application/json

{
  "skillName": ""  // Empty - should fail validation
}
```

Expected: 400 Bad Request with validation errors

### Test Invalid Rating
```http
POST http://localhost:5212/employees/{employeeId}/skills
Content-Type: application/json

{
  "skillId": "11111111-1111-1111-1111-111111111111",
  "rating": 6,  // Invalid - should be 1-5
  "trainingNeeded": false
}
```

Expected: 400 Bad Request

### Test Duplicate Skill Name
```http
POST http://localhost:5212/skills
Content-Type: application/json

{
  "skillName": "C#"  // Already exists
}
```

Expected: 409 Conflict

## Troubleshooting

### Database Connection Issues
- Verify SQL Server is running
- Check connection string in `appsettings.json`
- Ensure database exists or migrations run successfully

### RabbitMQ Connection Issues
- Verify RabbitMQ is running: `docker ps` or check Windows services
- Test connection: `telnet localhost 5672`
- Check RabbitMQ logs

### Event Not Consumed
- Verify SkillService is running
- Check SkillService logs for errors
- Verify queue exists in RabbitMQ: `rabbitmqctl list_queues`
- Check exchange binding: `rabbitmqctl list_bindings`

### Migration Issues
- Ensure EF Core tools are installed: `dotnet tool install --global dotnet-ef`
- Check connection string is correct
- Verify SQL Server permissions

## Quick Test Script

Use the provided `SkillService.http` file in VS Code with REST Client extension, or use curl:

```bash
# Get all skills
curl http://localhost:5212/skills

# Create skill
curl -X POST http://localhost:5212/skills \
  -H "Content-Type: application/json" \
  -d '{"skillName": "Go"}'

# Rate skill (replace {employeeId} and {skillId})
curl -X POST http://localhost:5212/employees/{employeeId}/skills \
  -H "Content-Type: application/json" \
  -d '{"skillId": "11111111-1111-1111-1111-111111111111", "rating": 4, "trainingNeeded": false}'

# Search
curl "http://localhost:5212/skills/search?skill=Java&rating=3"
```

