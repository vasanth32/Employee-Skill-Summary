# Quick Start Testing Guide

## Prerequisites Check

1. **SQL Server** running
2. **RabbitMQ** running (check: `docker ps` or Windows Services)
3. **Database created** (run migrations)

## Quick Setup (5 minutes)

### 1. Create Database
```powershell
cd SkillService
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 2. Start Services

**Terminal 1:**
```powershell
cd EmployeeService
dotnet run
```

**Terminal 2:**
```powershell
cd SkillService
dotnet run
```

### 3. Test in Browser

Open Swagger UI:
- EmployeeService: http://localhost:5110/swagger
- SkillService: http://localhost:5212/swagger

## Quick Test Sequence

### 1. Get Skills (Verify Seeded Data)
```
GET http://localhost:5212/skills
```
Should return 10 skills.

### 2. Create Employee (Triggers Event)
```
POST http://localhost:5110/employees
Body: {
  "name": "Test User",
  "email": "test@example.com",
  "role": "Developer"
}
```
**Check SkillService console** - should log event consumption.

### 3. Rate a Skill
Use the employeeId from step 2 and a skillId from step 1:
```
POST http://localhost:5212/employees/{employeeId}/skills
Body: {
  "skillId": "11111111-1111-1111-1111-111111111111",  // C# skill
  "rating": 4,
  "trainingNeeded": false
}
```

### 4. Search
```
GET http://localhost:5212/skills/search?skill=C%23&rating=3
```

## Verify Everything Works

✅ Skills endpoint returns data  
✅ Employee creation triggers event (check SkillService logs)  
✅ Skill rating saves to database  
✅ SkillRatedEvent published (check RabbitMQ UI)  
✅ Search returns results  

## Common Issues

**"Cannot connect to database"**
- Check SQL Server is running
- Verify connection string in appsettings.json

**"RabbitMQ connection failed"**
- Start RabbitMQ: `docker start rabbitmq` or check Windows Services
- Verify port 5672 is accessible

**"Event not consumed"**
- Ensure SkillService is running
- Check SkillService console logs for errors
- Verify RabbitMQ exchange/queue exists

## Full Testing Guide

See `SkillService/TESTING_GUIDE.md` for detailed testing instructions.

