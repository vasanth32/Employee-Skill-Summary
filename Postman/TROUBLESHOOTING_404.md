# Troubleshooting 404 Errors

## Common 404 Error: Employee Not Found

### Issue
Getting `404 Not Found` when trying to GET `/employees/{id}`

### Possible Causes

1. **Employee doesn't exist in database**
   - The employee was never created
   - The employee was deleted
   - Database connection issue

2. **Postman variable not set**
   - `employeeId` variable is not set in environment
   - Variable name is misspelled
   - Environment is not selected

3. **Wrong GUID format**
   - GUID is malformed
   - Using literal `{{employeeId}}` instead of variable value

### Solutions

#### Solution 1: Verify Employee Was Created

1. **Check Create Employee Response:**
   - Run "Create Employee" request
   - Verify response is `201 Created`
   - Copy the `employeeId` from response body

2. **Check All Employees:**
   ```
   GET http://localhost:5110/employees
   ```
   - This will show all employees in database
   - Copy a valid `employeeId` from the response

#### Solution 2: Verify Postman Variable

1. **Check Environment is Selected:**
   - Top right corner: Ensure environment is selected
   - Should show: `SkillService Local` (or your environment name)

2. **Check Variable is Set:**
   - Click on **Environments** (left sidebar)
   - Select your environment
   - Verify `employeeId` variable exists and has a value
   - Value should be a GUID like: `4ad8e205-c47b-49c4-9389-8f3602d83aef`

3. **View Postman Console:**
   - View → Show Postman Console
   - Run "Create Employee" request
   - Check console output for: `Employee created: {guid}`
   - Verify variable was set

#### Solution 3: Manual Variable Setup

If automatic variable setting isn't working:

1. **Get employeeId from Create Response:**
   - Run "Create Employee" request
   - In response body, copy the `employeeId` value
   - Example: `"employeeId": "4ad8e205-c47b-49c4-9389-8f3602d83aef"`

2. **Set Variable Manually:**
   - Click **Environments** → Select your environment
   - Click **+** to add variable
   - Name: `employeeId`
   - Value: Paste the GUID you copied
   - Click **Save**

3. **Use Variable in Request:**
   - In URL, use: `{{employeeId}}`
   - Postman will replace it with the actual value

#### Solution 4: Test with Direct GUID

Temporarily test with a direct GUID:

1. **Get a valid employeeId:**
   ```
   GET http://localhost:5110/employees
   ```

2. **Use it directly in URL:**
   ```
   GET http://localhost:5110/employees/4ad8e205-c47b-49c4-9389-8f3602d83aef
   ```
   (Replace with actual GUID from step 1)

### Step-by-Step Debugging

1. **Verify Service is Running:**
   ```powershell
   # Check if EmployeeService is running
   # Should see: "Now listening on: http://localhost:5110"
   ```

2. **Test Base Endpoint:**
   ```
   GET http://localhost:5110/employees
   ```
   - Should return list of employees (may be empty)
   - If this fails, service is not running or database issue

3. **Create Employee:**
   ```
   POST http://localhost:5110/employees
   Body: {
     "name": "Test User",
     "email": "test@example.com",
     "role": "Developer"
   }
   ```
   - Should return `201 Created` with employee data
   - Copy the `employeeId` from response

4. **Get Employee by ID:**
   ```
   GET http://localhost:5110/employees/{paste-employeeId-here}
   ```
   - Should return `200 OK` with employee data

### Quick Fix Checklist

- [ ] EmployeeService is running on port 5110
- [ ] Database connection is working
- [ ] "Create Employee" request returned 201
- [ ] `employeeId` variable is set in environment
- [ ] Environment is selected in Postman
- [ ] URL uses `{{employeeId}}` (not literal text)
- [ ] GUID format is correct (8-4-4-4-12 hex digits)

### Common Mistakes

❌ **Wrong:**
```
GET http://localhost:5110/employees/{{4ad8e205-c47b-49c4-9389-8f3602d83aef}}
```
(Extra braces around GUID)

✅ **Correct:**
```
GET http://localhost:5110/employees/{{employeeId}}
```
(Variable name only)

❌ **Wrong:**
```
GET http://localhost:5110/employees/employeeId
```
(Literal text, not variable)

✅ **Correct:**
```
GET http://localhost:5110/employees/{{employeeId}}
```
(Variable with double braces)

### Database Verification

If you want to verify in database:

```sql
-- Check if employee exists
SELECT * FROM Employees WHERE EmployeeId = '4ad8e205-c47b-49c4-9389-8f3602d83aef'

-- List all employees
SELECT * FROM Employees
```

### Still Getting 404?

1. **Check EmployeeService logs:**
   - Look for errors in console
   - Check database connection errors

2. **Verify Database:**
   - Ensure migrations are applied
   - Check connection string in `appsettings.json`

3. **Test with Swagger:**
   - Open http://localhost:5110/swagger
   - Try the endpoint there
   - If it works in Swagger but not Postman, it's a Postman configuration issue

4. **Check Request Headers:**
   - Ensure no extra headers causing issues
   - Try with minimal headers

