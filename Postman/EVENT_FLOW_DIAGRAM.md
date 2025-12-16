# Event Flow Diagram - Full Testing Flow

## Complete Flow Visualization

```
┌─────────────────────────────────────────────────────────────────┐
│                    POSTMAN TESTING FLOW                         │
└─────────────────────────────────────────────────────────────────┘

STEP 1: Setup
┌─────────────────┐
│  Get All Skills  │ → Returns 10 seeded skills
└─────────────────┘

STEP 2: Create Employee (Event Trigger)
┌──────────────────────────────────────────────────────────────┐
│  POST /employees (EmployeeService)                           │
│  Body: { name, email, role }                                  │
└──────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────────────┐
│  EmployeeService saves to DB                                  │
└──────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────────────┐
│  Publishes: EmployeeCreatedEvent                              │
│  Exchange: employee.events                                    │
│  Message: { EmployeeId, Name, Email, Role }                  │
└──────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────────────┐
│  RabbitMQ Exchange: employee.events                           │
│  (Fanout - broadcasts to all queues)                          │
└──────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────────────┐
│  SkillService Consumer receives event                         │
│  Queue: skillservice_employee_created                        │
└──────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────────────┐
│  SkillService saves to EmployeeReferences table               │
│  Logs: "Received EmployeeCreatedEvent..."                    │
└──────────────────────────────────────────────────────────────┘

STEP 3: Rate Skill (Event Publishing)
┌──────────────────────────────────────────────────────────────┐
│  POST /employees/{id}/skills (SkillService)                  │
│  Body: { skillId, rating, trainingNeeded }                    │
└──────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────────────┐
│  SkillService saves/updates EmployeeSkills table              │
└──────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────────────┐
│  Publishes: SkillRatedEvent                                    │
│  Exchange: skill.events                                       │
│  Message: { EmployeeId, SkillName, Rating }                  │
└──────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────────────────────┐
│  RabbitMQ Exchange: skill.events                              │
│  (Available for SearchService or other consumers)             │
└──────────────────────────────────────────────────────────────┘

STEP 4: Search
┌──────────────────────────────────────────────────────────────┐
│  GET /skills/search?skill=C#&rating=3                        │
│  Returns: Employee skills matching criteria                   │
└──────────────────────────────────────────────────────────────┘
```

## Testing Checklist

### ✅ EmployeeCreatedEvent Flow
1. [ ] Create employee via Postman
2. [ ] Check SkillService console: "Received EmployeeCreatedEvent"
3. [ ] Verify EmployeeReferences table has entry
4. [ ] Check RabbitMQ UI: Exchange `employee.events` has message

### ✅ SkillRatedEvent Flow
1. [ ] Rate skill via Postman
2. [ ] Verify EmployeeSkills table has entry
3. [ ] Check RabbitMQ UI: Exchange `skill.events` has message
4. [ ] Verify event message contains: EmployeeId, SkillName, Rating

### ✅ Database Verification
```sql
-- Check employee was stored
SELECT * FROM EmployeeReferences;

-- Check skills were rated
SELECT es.*, s.SkillName, er.Name as EmployeeName
FROM EmployeeSkills es
JOIN Skills s ON es.SkillId = s.SkillId
JOIN EmployeeReferences er ON es.EmployeeId = er.EmployeeId;
```

### ✅ RabbitMQ Verification
1. Open http://localhost:15672
2. Go to **Exchanges**
3. Check:
   - `employee.events` - Should have messages
   - `skill.events` - Should have messages
4. Click on exchange → **Bindings** to see queues

## Request Sequence in Postman

```
1. Get All Skills
   ↓
2. Create Employee
   ↓ (Check SkillService logs)
3. Get Skills (to get skill IDs)
   ↓
4. Rate C# Skill
   ↓ (Check RabbitMQ UI)
5. Rate Java Skill
   ↓
6. Update C# Rating
   ↓
7. Search by Skill Name
   ↓
8. Search by Rating
   ↓
9. Search Combined
```

## Environment Variables (Auto-Set)

| Variable | Set By | Used In |
|----------|--------|---------|
| `employeeId` | Create Employee request | Rate skill requests |
| `csharpSkillId` | Get Skills request (script) | Rate C# skill |
| `javaSkillId` | Get Skills request (script) | Rate Java skill |

## Key Endpoints

### EmployeeService
- `POST /employees` - Create employee (triggers event)

### SkillService
- `GET /skills` - Get all skills
- `POST /skills` - Create skill
- `POST /employees/{id}/skills` - Rate skill (publishes event)
- `GET /skills/search` - Search skills

## RabbitMQ Exchanges

| Exchange | Type | Purpose | Consumers |
|----------|------|---------|-----------|
| `employee.events` | fanout | Employee lifecycle events | SkillService, SearchService |
| `skill.events` | fanout | Skill rating events | SearchService (future) |

## Troubleshooting Flow

```
Event not consumed?
├─ Check SkillService is running
├─ Check SkillService console logs
├─ Verify RabbitMQ is running
└─ Check queue binding in RabbitMQ UI

Event not published?
├─ Check SkillService logs
├─ Verify RabbitMQ connection
└─ Check exchange exists in RabbitMQ UI

Database not updated?
├─ Check connection string
├─ Verify migrations applied
└─ Check SQL Server is running
```

