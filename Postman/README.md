# Postman Collection for SkillService

This folder contains a complete Postman collection for testing the full event-driven flow of SkillService.

## Files

- **SkillService_Collection.json** - Complete Postman collection with all endpoints
- **POSTMAN_TESTING_GUIDE.md** - Detailed step-by-step testing guide

## Quick Start

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

## Collection Structure

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

- ✅ Automatic variable management (employeeId, skillIds)
- ✅ Test scripts with console logging
- ✅ Complete event flow testing
- ✅ Validation and error testing
- ✅ Ready-to-use request bodies

## Prerequisites

- EmployeeService running on http://localhost:5110
- SkillService running on http://localhost:5212
- RabbitMQ running
- Database migrations applied

See **POSTMAN_TESTING_GUIDE.md** for detailed instructions.

