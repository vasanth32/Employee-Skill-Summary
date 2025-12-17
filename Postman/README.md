# Postman Collections

This folder contains Postman collections for testing the Employee Skill Summary application.

## Files

### API Gateway Collection
- **ApiGateway_Collection.json** - Complete Postman collection for API Gateway testing
- **API_GATEWAY_TESTING_GUIDE.md** - Detailed guide for testing API Gateway with JWT authentication

### SkillService Collection
- **SkillService_Collection.json** - Complete Postman collection for SkillService event-driven flow
- **POSTMAN_TESTING_GUIDE.md** - Detailed step-by-step testing guide for SkillService

## Quick Start

### API Gateway Collection

1. **Import Collection:**
   - Open Postman
   - Click **Import**
   - Select `ApiGateway_Collection.json`

2. **Create Environment:**
   - Create new environment: `API Gateway Local`
   - Variables are auto-set by scripts:
     - `gatewayUrl`: `http://localhost:5112` (pre-set)
     - `authToken`: Auto-set by login/register
     - `employeeId`: Auto-set when creating/getting employees
     - `skillId`: Auto-set when getting skills

3. **Run Tests:**
   - Start with: `1. Authentication > Register User` or `Login`
   - Follow sequence: 1 → 2 → 3 → 4 → 5 → 6
   - Or use **Runner** to automate

**See API_GATEWAY_TESTING_GUIDE.md for detailed instructions.**

### SkillService Collection

1. **Import Collection:**
   - Open Postman
   - Click **Import**
   - Select `SkillService_Collection.json`

2. **Create Environment:**
   - Create new environment: `SkillService Local`
   - Variables are auto-set by scripts, but you can pre-set:
     - `csharpSkillId`: `11111111-1111-1111-1111-111111111111`
     - `javaSkillId`: `22222222-2222-2222-2222-222222222222`

3. **Run Tests:**
   - Start with folder: `1. Setup - Get Skills`
   - Follow sequence: 1 → 2 → 3 → 4 → 5 → 6
   - Or use **Runner** to automate

**See POSTMAN_TESTING_GUIDE.md for detailed instructions.**

## Collection Structures

### API Gateway Collection
```
API Gateway - Complete Testing
├── 1. Authentication (No Token Required)
├── 2. Employee Management (Token Required)
├── 3. Skills Management (Token Required)
├── 4. Employee Skills (Token Required)
├── 5. Search Service (Token Required)
└── 6. JWT Validation Tests
```

### SkillService Collection
```
SkillService - Full Flow Testing
├── 1. Setup - Get Skills
├── 2. Create Employee (Triggers Event)
├── 3. Skill Management
├── 4. Rate Employee Skills
├── 5. Search Skills
└── 6. Validation Tests
```

## Features

### API Gateway Collection
- ✅ JWT authentication testing
- ✅ Reverse proxy routing verification
- ✅ Claim forwarding validation
- ✅ Automatic token management
- ✅ Protected vs public route testing

### SkillService Collection
- ✅ Automatic variable management (employeeId, skillIds)
- ✅ Test scripts with console logging
- ✅ Complete event flow testing
- ✅ Validation and error testing
- ✅ Ready-to-use request bodies

## Prerequisites

### For API Gateway Collection
- ApiGateway running on http://localhost:5112
- AuthService running on http://localhost:5163
- EmployeeService running on http://localhost:5110
- SkillService running on http://localhost:5212
- SearchService running on http://localhost:5234
- RabbitMQ running
- Database migrations applied

### For SkillService Collection
- EmployeeService running on http://localhost:5110
- SkillService running on http://localhost:5212
- RabbitMQ running
- Database migrations applied

