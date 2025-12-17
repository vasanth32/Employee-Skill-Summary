# API Gateway Testing Guide

Complete guide for testing the API Gateway with JWT authentication, reverse proxy routing, and claim forwarding.

## Prerequisites

1. **All services running:**
   - ApiGateway: http://localhost:5112
   - AuthService: http://localhost:5163
   - EmployeeService: http://localhost:5110
   - SkillService: http://localhost:5212
   - SearchService: http://localhost:5234

2. **RabbitMQ** running (for event flow)
3. **SQL Server** running with databases migrated
4. **Postman** installed

## Import Collection

1. Open Postman
2. Click **Import** button
3. Select `Postman/ApiGateway_Collection.json`
4. The collection will appear in your workspace

## Setup Environment Variables

The collection uses environment variables. Create a Postman Environment:

1. Click **Environments** → **+** (Create Environment)
2. Name it: `API Gateway Local`
3. The collection has default variables:
   - `gatewayUrl`: `http://localhost:5112` (pre-set)
   - `authToken`: Will be auto-set by login/register scripts
   - `employeeId`: Will be auto-set when creating/getting employees
   - `skillId`: Will be auto-set when getting skills
4. Select this environment before running requests

## Testing Sequence

### Step 1: Authentication (No Token Required)

#### 1.1 Register User
**Request:** `1. Authentication > Register User`

- **Purpose:** Register a new user and get JWT token
- **Route:** `/auth/register` → AuthService (no authentication required)
- **Body:**
  ```json
  {
    "name": "John Doe",
    "email": "john.doe@example.com",
    "password": "Password123!",
    "role": "User"
  }
  ```
- **Expected:** 200 OK with `token` and `role`
- **Auto-Action:** Script saves token to `authToken` environment variable
- **Verify:** Check console - should see "✅ User registered successfully"

#### 1.2 Login
**Request:** `1. Authentication > Login`

- **Purpose:** Login with existing credentials and get JWT token
- **Route:** `/auth/login` → AuthService (no authentication required)
- **Body:**
  ```json
  {
    "email": "john.doe@example.com",
    "password": "Password123!"
  }
  ```
- **Expected:** 200 OK with `token` and `role`
- **Auto-Action:** Script saves token to `authToken` environment variable
- **Verify:** Check console - should see "✅ Login successful"

---

### Step 2: Employee Management (Token Required)

**All requests in this section require JWT token in Authorization header.**

#### 2.1 Get All Employees
**Request:** `2. Employee Management > Get All Employees`

- **Purpose:** Test gateway routing and JWT validation
- **Route:** `/employee/employees` → EmployeeService
- **Authentication:** Bearer token (auto-added from `authToken` variable)
- **Expected:** 200 OK with employee list
- **Claim Forwarding:** Gateway forwards `X-User-Id`, `X-User-Email`, `X-User-Role` headers to EmployeeService
- **Verify:** 
  - Check EmployeeService console logs - should see forwarded headers
  - Check Postman console - should see "✅ Employees retrieved"

#### 2.2 Create Employee
**Request:** `2. Employee Management > Create Employee`

- **Purpose:** Create employee through gateway
- **Route:** `/employee/employees` → EmployeeService
- **Authentication:** Required
- **Body:**
  ```json
  {
    "name": "Jane Smith",
    "email": "jane.smith@example.com",
    "role": "Senior Developer"
  }
  ```
- **Expected:** 201 Created
- **Auto-Action:** Saves `employeeId` to environment variable
- **Event:** Triggers `EmployeeCreatedEvent` (consumed by SkillService)
- **Verify:**
  - Check SkillService console - should see event consumed
  - Check Postman console - should see "✅ Employee created"

#### 2.3 Get Employee by ID
**Request:** `2. Employee Management > Get Employee by ID`

- **Purpose:** Get specific employee through gateway
- **Route:** `/employee/employees/{id}` → EmployeeService
- **Authentication:** Required
- **Expected:** 200 OK with employee details

#### 2.4 Update Employee
**Request:** `2. Employee Management > Update Employee`

- **Purpose:** Update employee through gateway
- **Route:** `/employee/employees/{id}` → EmployeeService
- **Authentication:** Required
- **Expected:** 200 OK with updated employee

#### 2.5 Delete Employee
**Request:** `2. Employee Management > Delete Employee`

- **Purpose:** Delete employee through gateway
- **Route:** `/employee/employees/{id}` → EmployeeService
- **Authentication:** Required
- **Expected:** 204 No Content

---

### Step 3: Skills Management (Token Required)

#### 3.1 Get All Skills
**Request:** `3. Skills Management > Get All Skills`

- **Purpose:** Get all skills through gateway
- **Route:** `/skills/skills` → SkillService
- **Authentication:** Required
- **Expected:** 200 OK with skills list (should have 10 seeded skills)
- **Auto-Action:** Saves first skill ID to `skillId` variable

#### 3.2 Create Skill
**Request:** `3. Skills Management > Create Skill`

- **Purpose:** Create new skill through gateway
- **Route:** `/skills/skills` → SkillService
- **Authentication:** Required
- **Body:**
  ```json
  {
    "skillName": "TypeScript"
  }
  ```
- **Expected:** 201 Created

#### 3.3 Search Skills
**Request:** `3. Skills Management > Search Skills`

- **Purpose:** Search skills through gateway
- **Route:** `/skills/skills/search` → SkillService
- **Authentication:** Required
- **Query Parameters:**
  - `skill`: Skill name (e.g., "C#")
  - `rating`: Minimum rating (e.g., 3)
- **Expected:** 200 OK with filtered results

---

### Step 4: Employee Skills (Token Required)

#### 4.1 Rate Employee Skill
**Request:** `4. Employee Skills > Rate Employee Skill`

- **Purpose:** Rate a skill for an employee through gateway
- **Route:** `/skills/employees/{employeeId}/skills` → SkillService
- **Authentication:** Required
- **Body:**
  ```json
  {
    "skillId": "{{skillId}}",
    "rating": 4,
    "trainingNeeded": false
  }
  ```
- **Expected:** 200 OK with rating details
- **Event:** Publishes `SkillRatedEvent` to RabbitMQ
- **Claim Forwarding:** User claims forwarded to SkillService
- **Verify:**
  - Check SkillService console - should see `X-User-Id` header
  - Check RabbitMQ Management UI - should see message in `skill.events` exchange

---

### Step 5: Search Service (Token Required)

#### 5.1 Search by Skill Name
**Request:** `5. Search Service > Search by Skill Name`

- **Purpose:** Search employees by skill name through gateway
- **Route:** `/search/search?skill={name}` → SearchService
- **Authentication:** Required (JWT token)
- **Query Parameters:**
  - `skill`: Skill name (e.g., "C#", "Java")
- **Expected:** 200 OK with list of employees who have the skill
- **Note:** This reads from SearchService's local read database (CQRS pattern)

#### 5.2 Search by Minimum Rating
**Request:** `5. Search Service > Search by Minimum Rating`

- **Purpose:** Search employees by minimum rating
- **Route:** `/search/search?minRating={rating}` → SearchService
- **Authentication:** Required (JWT token)
- **Query Parameters:**
  - `minRating`: Minimum rating (1-5)
- **Expected:** 200 OK with employees who have skills rated >= minRating

#### 5.3 Search by Skill and Rating
**Request:** `5. Search Service > Search by Skill and Rating`

- **Purpose:** Combined search with both skill and rating filters
- **Route:** `/search/search?skill={name}&minRating={rating}` → SearchService
- **Authentication:** Required (JWT token)
- **Expected:** 200 OK with employees matching both criteria

#### 5.4 Search All (No Filters)
**Request:** `5. Search Service > Search All (No Filters)`

- **Purpose:** Get all employee-skill combinations
- **Route:** `/search/search` → SearchService
- **Authentication:** Required (JWT token)
- **Expected:** 200 OK with all data from read database - this is a placeholder

---

### Step 6: JWT Validation Tests

#### 6.1 Access Protected Route Without Token
**Request:** `6. JWT Validation Tests > Access Protected Route Without Token`

- **Purpose:** Verify gateway rejects unauthenticated requests
- **Route:** `/employee/employees` (no Authorization header)
- **Expected:** 401 Unauthorized
- **Verify:** Console should show "✅ Correctly rejected - 401 Unauthorized"

#### 6.2 Access Protected Route With Invalid Token
**Request:** `6. JWT Validation Tests > Access Protected Route With Invalid Token`

- **Purpose:** Verify gateway rejects invalid tokens
- **Route:** `/employee/employees` (with invalid token)
- **Expected:** 401 Unauthorized
- **Verify:** Console should show "✅ Correctly rejected invalid token"

#### 6.3 Access Public Route Without Token
**Request:** `6. JWT Validation Tests > Access Public Route Without Token (Should Work)`

- **Purpose:** Verify public routes (/auth/*) are accessible without token
- **Route:** `/auth/login` (no Authorization header)
- **Expected:** 200 OK (if credentials valid) or 401 (if invalid)
- **Verify:** Console should show "✅ Public route accessible without token"

---

## Verifying Claim Forwarding

The gateway forwards user claims as HTTP headers to downstream services:

- `X-User-Id`: User ID from JWT `sub` claim
- `X-User-Email`: Email from JWT `email` claim
- `X-User-Role`: Role from JWT `role` claim
- `X-User-Claims`: All claims as JSON (for debugging)

### How to Verify:

1. **Check Service Logs:**
   - Start EmployeeService or SkillService
   - Make a request through the gateway
   - Check service console logs - you should see the forwarded headers

2. **Add Logging to Services (Optional):**
   ```csharp
   // In EmployeeService or SkillService controller
   var userId = Request.Headers["X-User-Id"].FirstOrDefault();
   var userEmail = Request.Headers["X-User-Email"].FirstOrDefault();
   var userRole = Request.Headers["X-User-Role"].FirstOrDefault();
   _logger.LogInformation("Received headers - UserId: {UserId}, Email: {Email}, Role: {Role}", 
       userId, userEmail, userRole);
   ```

---

## Testing Checklist

### Gateway Routing
- [ ] `/auth/*` routes to AuthService (no auth required)
- [ ] `/employee/*` routes to EmployeeService (auth required)
- [ ] `/skills/*` routes to SkillService (auth required)
- [ ] `/search/*` routes to SearchService (auth required)

### JWT Validation
- [ ] Protected routes reject requests without token (401)
- [ ] Protected routes reject requests with invalid token (401)
- [ ] Protected routes accept requests with valid token (200/201)
- [ ] Public routes (`/auth/*`) work without token

### Claim Forwarding
- [ ] `X-User-Id` header forwarded to downstream services
- [ ] `X-User-Email` header forwarded to downstream services
- [ ] `X-User-Role` header forwarded to downstream services
- [ ] Headers visible in downstream service logs

### End-to-End Flow
- [ ] Register user → Get token
- [ ] Login → Get token
- [ ] Create employee (with token) → Success
- [ ] Get employees (with token) → Success
- [ ] Rate skill (with token) → Success
- [ ] Search skills (with token) → Success

---

## Troubleshooting

### 401 Unauthorized on Protected Routes

**Possible Causes:**
1. Token not set in environment variable
   - **Solution:** Run Login or Register request first
   - **Verify:** Check environment variable `authToken` is set

2. Token expired
   - **Solution:** Run Login request again to get new token
   - **Default expiry:** 60 minutes (configurable in `appsettings.json`)

3. Invalid token format
   - **Solution:** Ensure token is complete (not truncated)
   - **Verify:** Token should start with `eyJ` (base64 encoded JWT)

### 404 Not Found

**Possible Causes:**
1. Service not running
   - **Solution:** Start the required service
   - **Check:** Verify service is running on correct port

2. Incorrect route path
   - **Solution:** Check route configuration in `appsettings.json`
   - **Verify:** Path should match exactly (case-sensitive)

### Claims Not Forwarded

**Possible Causes:**
1. User not authenticated
   - **Solution:** Ensure valid token is used
   - **Verify:** Check gateway logs for authentication errors

2. Transform not working
   - **Solution:** Check `Program.cs` - transform should be configured
   - **Verify:** Gateway logs should show transform execution

---

## Quick Reference

### Service URLs
- **ApiGateway:** http://localhost:5112
- **AuthService:** http://localhost:5163
- **EmployeeService:** http://localhost:5110
- **SkillService:** http://localhost:5212
- **SearchService:** http://localhost:5234

### Gateway Routes
- `/auth/*` → AuthService (public)
- `/employee/*` → EmployeeService (protected)
- `/skills/*` → SkillService (protected)
- `/search/*` → SearchService (protected)

### Key Endpoints
- `POST /auth/register` - Register user (public)
- `POST /auth/login` - Login (public)
- `GET /employee/employees` - Get all employees (protected)
- `POST /employee/employees` - Create employee (protected)
- `GET /skills/skills` - Get all skills (protected)
- `POST /skills/employees/{id}/skills` - Rate skill (protected)
- `GET /skills/skills/search` - Search skills (protected)

### Environment Variables
- `gatewayUrl`: http://localhost:5112
- `authToken`: Auto-set by login/register
- `employeeId`: Auto-set when creating/getting employees
- `skillId`: Auto-set when getting skills

---

## Tips

1. **Use Console:** Open Postman Console (View → Show Postman Console) to see script outputs and debug messages
2. **Save Responses:** Save important responses for reference
3. **Use Variables:** The collection automatically saves IDs and tokens to variables
4. **Check Logs:** Always check service console logs for claim forwarding verification
5. **Test Sequence:** Follow the numbered sequence for best results
6. **Token Refresh:** If you get 401 errors, re-run Login to get a fresh token

---

## Next Steps

After testing the gateway:

1. **Implement SearchService** - Add actual search functionality
2. **Add Rate Limiting** - Configure rate limiting at gateway level
3. **Add CORS** - Configure CORS if needed for frontend
4. **Add Monitoring** - Add logging and monitoring for gateway
5. **Add Health Checks** - Add health check endpoints for services

