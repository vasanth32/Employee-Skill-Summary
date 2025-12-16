# SkillService Endpoint Testing Script
# Run this after starting both EmployeeService and SkillService

$SkillServiceUrl = "http://localhost:5212"
$EmployeeServiceUrl = "http://localhost:5110"

Write-Host "=== SkillService Testing Script ===" -ForegroundColor Green
Write-Host ""

# Test 1: Get All Skills
Write-Host "Test 1: Get All Skills" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$SkillServiceUrl/skills" -Method Get
    Write-Host "✓ Success! Found $($response.Count) skills" -ForegroundColor Green
    Write-Host "First skill: $($response[0].skillName)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}
Write-Host ""

# Test 2: Create a New Skill
Write-Host "Test 2: Create New Skill" -ForegroundColor Yellow
try {
    $newSkill = @{
        skillName = "PowerShell"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$SkillServiceUrl/skills" -Method Post -Body $newSkill -ContentType "application/json"
    Write-Host "✓ Success! Created skill: $($response.skillName) (ID: $($response.skillId))" -ForegroundColor Green
    $newSkillId = $response.skillId
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    $newSkillId = $null
}
Write-Host ""

# Test 3: Create Employee (triggers event)
Write-Host "Test 3: Create Employee (triggers EmployeeCreatedEvent)" -ForegroundColor Yellow
try {
    $newEmployee = @{
        name = "Test User $(Get-Date -Format 'HHmmss')"
        email = "test$(Get-Date -Format 'HHmmss')@example.com"
        role = "Developer"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$EmployeeServiceUrl/employees" -Method Post -Body $newEmployee -ContentType "application/json"
    Write-Host "✓ Success! Created employee: $($response.name) (ID: $($response.employeeId))" -ForegroundColor Green
    Write-Host "  Check SkillService logs - should see EmployeeCreatedEvent consumed" -ForegroundColor Cyan
    $employeeId = $response.employeeId
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    $employeeId = $null
}
Write-Host ""

# Test 4: Rate a Skill (if employee was created)
if ($employeeId) {
    Write-Host "Test 4: Rate Skill for Employee" -ForegroundColor Yellow
    try {
        # Get first skill ID
        $skills = Invoke-RestMethod -Uri "$SkillServiceUrl/skills" -Method Get
        $firstSkillId = $skills[0].skillId
        
        $rating = @{
            skillId = $firstSkillId
            rating = 4
            trainingNeeded = $false
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$SkillServiceUrl/employees/$employeeId/skills" -Method Post -Body $rating -ContentType "application/json"
        Write-Host "✓ Success! Rated skill: $($response.skillName) with rating $($response.rating)" -ForegroundColor Green
        Write-Host "  Check RabbitMQ - SkillRatedEvent should be published" -ForegroundColor Cyan
    } catch {
        Write-Host "✗ Failed: $_" -ForegroundColor Red
    }
    Write-Host ""
}

# Test 5: Search Skills
Write-Host "Test 5: Search Skills" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$SkillServiceUrl/skills/search?rating=3" -Method Get
    Write-Host "✓ Success! Found $($response.Count) employee skills with rating >= 3" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}
Write-Host ""

Write-Host "=== Testing Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Check SkillService console logs for event consumption" -ForegroundColor White
Write-Host "2. Check RabbitMQ Management UI (http://localhost:15672) for published events" -ForegroundColor White
Write-Host "3. Verify data in database:" -ForegroundColor White
Write-Host "   SELECT * FROM EmployeeReferences" -ForegroundColor Gray
Write-Host "   SELECT * FROM EmployeeSkills" -ForegroundColor Gray

