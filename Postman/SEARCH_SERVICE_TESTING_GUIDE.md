# SearchService Testing Guide

Complete guide for testing the SearchService with CQRS read model pattern.

## Overview

SearchService implements a **CQRS (Command Query Responsibility Segregation) Read Model**. It:
- ✅ Consumes events to maintain a local read database
- ✅ Provides fast search queries without calling other services
- ✅ Is fully decoupled and highly available
- ✅ Reads only from its own normalized database

## Prerequisites

1. **All services running:**
   - ApiGateway: http://localhost:5112
   - AuthService: http://localhost:5163
   - EmployeeService: http://localhost:5110
   - SkillService: http://localhost:5212
   - **SearchService: http://localhost:5234** ✅

2. **RabbitMQ** running (for event consumption)
3. **SQL Server** running with SearchServiceDb database
4. **Database migrations applied** for SearchService

## Database Schema

SearchService uses a normalized read model:

### EmployeeSummary Table
- `SummaryId` (PK, Guid)
- `EmployeeId` (Unique, Guid)
- `Name` (string)
- `Role` (string)

### EmployeeSummarySkill Table
- `SummarySkillId` (PK, Guid)
- `SummaryId` (FK → EmployeeSummary)
- `SkillName` (string)
- `Rating` (int, 1-5)

## Event Flow

### 1. EmployeeCreatedEvent
When an employee is created:
- **Published by:** EmployeeService
- **Consumed by:** SearchService
- **Action:** Inserts into `EmployeeSummary` table

### 2. SkillRatedEvent
When a skill is rated:
- **Published by:** SkillService
- **Consumed by:** SearchService
- **Action:** Inserts or Updates `EmployeeSummarySkill` table

## Testing Sequence

### Step 1: Setup - Ensure Data Exists

Before testing search, you need data in the read database. This happens automatically through events:

1. **Create an Employee** (through API Gateway):
   - `POST /employee/employees` with JWT token
   - This triggers `EmployeeCreatedEvent`
   - SearchService consumes it and creates `EmployeeSummary`

2. **Rate a Skill** (through API Gateway):
   - `POST /skills/employees/{employeeId}/skills` with JWT token
   - This triggers `SkillRatedEvent`
   - SearchService consumes it and creates/updates `EmployeeSummarySkill`

### Step 2: Test Search Endpoints

#### 2.1 Search by Skill Name
**Request:** `5. Search Service > Search by Skill Name`

- **URL:** `GET {{gatewayUrl}}/search/search?skill=C%23`
- **Authentication:** Required (JWT token)
- **Expected:** 200 OK with list of employees who have C# skill
- **SQL Equivalent:**
  ```sql
  SELECT e.EmployeeId, e.Name, e.Role, s.SkillName, s.Rating
  FROM EmployeeSummary e
  JOIN EmployeeSummarySkill s ON e.SummaryId = s.SummaryId
  WHERE s.SkillName = 'C#'
  ```

#### 2.2 Search by Minimum Rating
**Request:** `5. Search Service > Search by Minimum Rating`

- **URL:** `GET {{gatewayUrl}}/search/search?minRating=4`
- **Authentication:** Required (JWT token)
- **Expected:** 200 OK with all employees who have skills rated >= 4
- **SQL Equivalent:**
  ```sql
  SELECT e.EmployeeId, e.Name, e.Role, s.SkillName, s.Rating
  FROM EmployeeSummary e
  JOIN EmployeeSummarySkill s ON e.SummaryId = s.SummaryId
  WHERE s.Rating >= 4
  ```

#### 2.3 Search by Skill and Rating
**Request:** `5. Search Service > Search by Skill and Rating`

- **URL:** `GET {{gatewayUrl}}/search/search?skill=C%23&minRating=3`
- **Authentication:** Required (JWT token)
- **Expected:** 200 OK with employees who have C# skill rated >= 3
- **SQL Equivalent:**
  ```sql
  SELECT e.EmployeeId, e.Name, e.Role, s.SkillName, s.Rating
  FROM EmployeeSummary e
  JOIN EmployeeSummarySkill s ON e.SummaryId = s.SummaryId
  WHERE s.SkillName = 'C#' AND s.Rating >= 3
  ```

#### 2.4 Search All (No Filters)
**Request:** `5. Search Service > Search All (No Filters)`

- **URL:** `GET {{gatewayUrl}}/search/search`
- **Authentication:** Required (JWT token)
- **Expected:** 200 OK with all employee-skill combinations
- **SQL Equivalent:**
  ```sql
  SELECT e.EmployeeId, e.Name, e.Role, s.SkillName, s.Rating
  FROM EmployeeSummary e
  JOIN EmployeeSummarySkill s ON e.SummaryId = s.SummaryId
  ```

## Direct SearchService Testing (Without Gateway)

You can also test SearchService directly:

### Base URL
- **SearchService:** http://localhost:5234

### Endpoints
- `GET /search?skill=C#` - Search by skill
- `GET /search?minRating=3` - Search by rating
- `GET /search?skill=C#&minRating=3` - Combined search
- `GET /search` - Get all

**Note:** Direct testing doesn't require JWT authentication (unless you add it).

## Verifying Event Consumption

### Check SearchService Logs

When you create an employee or rate a skill, check SearchService console logs:

**For EmployeeCreatedEvent:**
```
Received EmployeeCreatedEvent: EmployeeId=..., Name=..., Role=...
Stored employee summary: EmployeeId=..., Name=...
```

**For SkillRatedEvent:**
```
Received SkillRatedEvent: EmployeeId=..., SkillName=..., Rating=...
Inserted new skill: EmployeeId=..., SkillName=..., Rating=...
```

### Verify Database

Query the SearchService database directly:

```sql
-- Check employee summaries
SELECT * FROM EmployeeSummaries;

-- Check employee skills
SELECT * FROM EmployeeSummarySkills;

-- Check combined data
SELECT e.EmployeeId, e.Name, e.Role, s.SkillName, s.Rating
FROM EmployeeSummaries e
JOIN EmployeeSummarySkills s ON e.SummaryId = s.SummaryId;
```

## Complete Testing Flow

### Full End-to-End Test

1. **Register/Login** to get JWT token
   - `POST /auth/register` or `POST /auth/login`

2. **Create Employee** (triggers EmployeeCreatedEvent)
   - `POST /employee/employees` with token
   - Verify: SearchService logs show event consumed
   - Verify: `EmployeeSummaries` table has new entry

3. **Get Skills** to find skill IDs
   - `GET /skills/skills` with token

4. **Rate Skill** (triggers SkillRatedEvent)
   - `POST /skills/employees/{employeeId}/skills` with token
   - Verify: SearchService logs show event consumed
   - Verify: `EmployeeSummarySkills` table has new entry

5. **Search** to verify read model
   - `GET /search/search?skill=C#` with token
   - Verify: Returns employee with C# skill

## Troubleshooting

### No Results in Search

**Possible Causes:**
1. **No data in read database**
   - **Solution:** Create employees and rate skills first
   - **Verify:** Check `EmployeeSummaries` and `EmployeeSummarySkills` tables

2. **Events not consumed**
   - **Solution:** Check SearchService logs for event consumption
   - **Verify:** Ensure RabbitMQ is running
   - **Verify:** Check SearchService console for errors

3. **Skill name mismatch**
   - **Solution:** Use exact skill name (case-sensitive)
   - **Verify:** Check what skill names exist in database

### 401 Unauthorized

**Possible Causes:**
1. **Token not set**
   - **Solution:** Run Login or Register request first
   - **Verify:** Check `authToken` environment variable

2. **Token expired**
   - **Solution:** Run Login request again
   - **Default expiry:** 60 minutes

### Events Not Consumed

**Possible Causes:**
1. **SearchService not running**
   - **Solution:** Start SearchService
   - **Verify:** Check if port 5234 is listening

2. **RabbitMQ not running**
   - **Solution:** Start RabbitMQ
   - **Verify:** Check RabbitMQ Management UI (http://localhost:15672)

3. **Queue not bound**
   - **Solution:** Restart SearchService to rebind queues
   - **Verify:** Check RabbitMQ Management UI for queue existence

4. **Database connection error**
   - **Solution:** Check connection string in `appsettings.json`
   - **Verify:** Ensure SQL Server is running
   - **Verify:** Ensure database exists and migrations are applied

## Key Features

### ✅ CQRS Pattern
- **Write Model:** EmployeeService and SkillService (source of truth)
- **Read Model:** SearchService (optimized for queries)
- **Synchronization:** Event-driven (asynchronous)

### ✅ Decoupled Architecture
- SearchService doesn't call EmployeeService or SkillService
- Independent and highly available
- Can scale separately

### ✅ Fast Queries
- Reads from local database (no network calls)
- Normalized schema for efficient joins
- Optimized for search scenarios

## Quick Reference

### Service URLs
- **ApiGateway:** http://localhost:5112
- **SearchService (Direct):** http://localhost:5234

### Search Endpoints (Through Gateway)
- `GET /search/search?skill={name}` - Search by skill
- `GET /search/search?minRating={rating}` - Search by rating
- `GET /search/search?skill={name}&minRating={rating}` - Combined
- `GET /search/search` - Get all

### Search Endpoints (Direct)
- `GET /search?skill={name}` - Search by skill
- `GET /search?minRating={rating}` - Search by rating
- `GET /search?skill={name}&minRating={rating}` - Combined
- `GET /search` - Get all

### Database Tables
- `EmployeeSummaries` - Employee data
- `EmployeeSummarySkills` - Employee skill ratings

### Events
- `EmployeeCreatedEvent` - Published by EmployeeService
- `SkillRatedEvent` - Published by SkillService

## Tips

1. **Use Console:** Open Postman Console to see script outputs
2. **Check Logs:** Always check SearchService console logs for event consumption
3. **Verify Database:** Query database directly to verify data
4. **Test Events:** Create employees and rate skills to populate read database
5. **Test Search:** Use various search combinations to test query logic



