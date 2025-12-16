# Postman Testing Guide - Full Flow

## Prerequisites

1. **SQL Server** running
2. **RabbitMQ** running
3. **Database migrations** applied
4. **Both services running:**
   - EmployeeService: http://localhost:5110
   - SkillService: http://localhost:5212

## Import Collection

1. Open Postman
2. Click **Import** button
3. Select `Postman/SkillService_Collection.json`
4. The collection will appear in your workspace

## Setup Environment Variables

The collection uses environment variables. Create a Postman Environment:

1. Click **Environments** → **+** (Create Environment)
2. Name it: `SkillService Local`
3. Add variables (optional - some are auto-set by scripts):
   - `employeeId` - Will be set automatically
   - `csharpSkillId` - Pre-set to: `11111111-1111-1111-1111-111111111111`
   - `javaSkillId` - Pre-set to: `22222222-2222-2222-2222-222222222222`
4. Select this environment before running requests

## Full Flow Testing Sequence

### Step 1: Setup - Get Skills

**Request:** `1. Setup - Get Skills > Get All Skills`

- **Purpose:** Verify skills are seeded
- **Expected:** 200 OK with 10 skills (C#, Java, Python, etc.)
- **Action:** Note the skill IDs for later use

---

### Step 2: Create Employee (Triggers Event)

**Request:** `2. Create Employee (Triggers Event) > Create Employee`

- **Purpose:** Create employee and trigger `EmployeeCreatedEvent`
- **Expected:** 201 Created with employee details
- **Auto-Action:** Script saves `employeeId` to environment variable
- **Manual Check:** 
  - Open SkillService console/logs
  - You should see: `Received EmployeeCreatedEvent: EmployeeId=..., Name=John Doe`
  - You should see: `Stored employee reference: EmployeeId=...`

**Verify in Database:**
```sql
SELECT * FROM EmployeeReferences WHERE Email = 'john.doe@example.com'
```

**Request:** `2. Create Employee (Triggers Event) > Get Employee by ID`

- **Purpose:** Verify employee was created
- **Expected:** 200 OK with employee details

---

### Step 3: Skill Management

**Request:** `3. Skill Management > Create New Skill`

- **Purpose:** Create a new skill
- **Body:** `{ "skillName": "TypeScript" }`
- **Expected:** 201 Created

**Request:** `3. Skill Management > Get Skill by ID (C#)`

- **Purpose:** Get all skills and extract C# skill ID
- **Auto-Action:** Script saves C# skill ID to `csharpSkillId` variable
- **Expected:** 200 OK with skills list

---

### Step 4: Rate Employee Skills

**Request:** `4. Rate Employee Skills > Rate C# Skill (Rating 4)`

- **Purpose:** Rate C# skill for the employee
- **Body:** Uses `{{employeeId}}` and `{{csharpSkillId}}` variables
- **Expected:** 200 OK with rating details
- **Auto-Action:** Script logs success message
- **Manual Check:**
  - Open RabbitMQ Management UI: http://localhost:15672
  - Go to **Exchanges** → **skill.events**
  - Check message count (should increase)
  - Click on exchange to see published messages

**Request:** `4. Rate Employee Skills > Rate Java Skill (Rating 5)`

- First request gets Java skill ID
- Second request rates Java skill

**Request:** `4. Rate Employee Skills > Update C# Rating (Rating 5)`

- **Purpose:** Update existing rating (should update, not create duplicate)
- **Expected:** 200 OK with updated rating

**Verify in Database:**
```sql
SELECT es.*, s.SkillName 
FROM EmployeeSkills es
JOIN Skills s ON es.SkillId = s.SkillId
WHERE es.EmployeeId = '{employeeId from environment}'
```

---

### Step 5: Search Skills

**Request:** `5. Search Skills > Search by Skill Name (C#)`

- **Purpose:** Find all employees with C# skill
- **Expected:** 200 OK with employee skills list

**Request:** `5. Search Skills > Search by Rating (>= 4)`

- **Purpose:** Find all skills with rating >= 4
- **Expected:** 200 OK with filtered results

**Request:** `5. Search Skills > Search by Skill and Rating`

- **Purpose:** Combined search
- **Expected:** 200 OK with filtered results

---

### Step 6: Validation Tests

**Request:** `6. Validation Tests > Invalid Skill Name (Empty)`

- **Expected:** 400 Bad Request with validation errors

**Request:** `6. Validation Tests > Invalid Rating (Out of Range)`

- **Expected:** 400 Bad Request (rating must be 1-5)

**Request:** `6. Validation Tests > Duplicate Skill Name`

- **Expected:** 409 Conflict

---

## Using Postman Runner (Automated Testing)

1. Click on the collection
2. Click **Run** button
3. Select requests to run (or run all)
4. Click **Run SkillService - Full Flow Testing**
5. Review results in the test results tab

## Event Flow Verification Checklist

### EmployeeCreatedEvent Flow
- [ ] Employee created in EmployeeService
- [ ] SkillService console shows event received
- [ ] EmployeeReferences table has new entry
- [ ] No errors in SkillService logs

### SkillRatedEvent Flow
- [ ] Skill rated successfully
- [ ] RabbitMQ Management UI shows message in `skill.events` exchange
- [ ] EmployeeSkills table has entry
- [ ] No errors in SkillService logs

## Troubleshooting

### Environment Variables Not Working
- Ensure environment is selected in Postman
- Check variable names match exactly (case-sensitive)
- Re-run the request that sets the variable

### Events Not Consumed
- Verify SkillService is running
- Check SkillService console for errors
- Verify RabbitMQ is running: `docker ps` or check services
- Check RabbitMQ connection in SkillService logs

### Events Not Published
- Verify RabbitMQ is running
- Check RabbitMQ Management UI for exchange existence
- Verify exchange name: `skill.events`
- Check SkillService logs for publishing errors

### Database Errors
- Verify SQL Server is running
- Check connection string in `appsettings.json`
- Ensure migrations are applied: `dotnet ef database update`

## Quick Reference

### Service URLs
- **EmployeeService:** http://localhost:5110
- **SkillService:** http://localhost:5212
- **RabbitMQ Management:** http://localhost:15672 (guest/guest)

### Key Endpoints
- `GET /skills` - Get all skills
- `POST /employees` - Create employee (EmployeeService)
- `POST /employees/{id}/skills` - Rate skill
- `GET /skills/search?skill={name}&rating={min}` - Search skills

### Database Queries
```sql
-- Check employee references
SELECT * FROM EmployeeReferences

-- Check employee skills
SELECT es.*, s.SkillName, er.Name as EmployeeName
FROM EmployeeSkills es
JOIN Skills s ON es.SkillId = s.SkillId
JOIN EmployeeReferences er ON es.EmployeeId = er.EmployeeId

-- Check skills
SELECT * FROM Skills
```

## Tips

1. **Use Console:** Open Postman Console (View → Show Postman Console) to see script outputs
2. **Save Responses:** Save important responses for reference
3. **Use Variables:** The collection automatically saves IDs to variables
4. **Check Logs:** Always check service console logs for event flow
5. **RabbitMQ UI:** Keep RabbitMQ Management UI open to monitor events

