# Test RabbitMQ Consumer for SkillService
Write-Host "=== Testing SkillService RabbitMQ Consumer ===" -ForegroundColor Green
Write-Host ""

# 1. Check if RabbitMQ is running
Write-Host "1. Checking RabbitMQ..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:15672/api/overview" -UseBasicParsing -ErrorAction Stop
    Write-Host "   ✓ RabbitMQ is running" -ForegroundColor Green
} catch {
    Write-Host "   ✗ RabbitMQ is not running or not accessible" -ForegroundColor Red
    Write-Host "   Start RabbitMQ: docker start rabbitmq" -ForegroundColor Yellow
    Write-Host "   Or check if it's running on port 15672" -ForegroundColor Yellow
    exit
}

# 2. Check if queue exists and has consumer
Write-Host "`n2. Checking queue..." -ForegroundColor Yellow
try {
    $queues = Invoke-RestMethod -Uri "http://localhost:15672/api/queues" -UseBasicParsing
    $skillQueue = $queues | Where-Object { $_.name -eq "skillservice_employee_created" }
    if ($skillQueue) {
        Write-Host "   ✓ Queue exists: skillservice_employee_created" -ForegroundColor Green
        Write-Host "   Consumers attached: $($skillQueue.consumers)" -ForegroundColor Cyan
        if ($skillQueue.consumers -gt 0) {
            Write-Host "   ✓ Consumer is attached! SkillService is listening" -ForegroundColor Green
        } else {
            Write-Host "   ✗ No consumer attached" -ForegroundColor Red
            Write-Host "   → Start SkillService: cd SkillService; dotnet run" -ForegroundColor Yellow
        }
        Write-Host "   Messages ready: $($skillQueue.messages_ready)" -ForegroundColor Cyan
        Write-Host "   Messages unacknowledged: $($skillQueue.messages_unacknowledged)" -ForegroundColor Cyan
    } else {
        Write-Host "   ✗ Queue not found" -ForegroundColor Red
        Write-Host "   → Start SkillService first to create the queue" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ✗ Error checking queues: $_" -ForegroundColor Red
}

# 3. Check exchange
Write-Host "`n3. Checking exchange..." -ForegroundColor Yellow
try {
    $exchanges = Invoke-RestMethod -Uri "http://localhost:15672/api/exchanges" -UseBasicParsing
    $employeeExchange = $exchanges | Where-Object { $_.name -eq "employee.events" }
    if ($employeeExchange) {
        Write-Host "   ✓ Exchange exists: employee.events" -ForegroundColor Green
        Write-Host "   Type: $($employeeExchange.type)" -ForegroundColor Cyan
    } else {
        Write-Host "   ✗ Exchange not found" -ForegroundColor Red
        Write-Host "   → EmployeeService needs to start first to create exchange" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ✗ Error checking exchanges: $_" -ForegroundColor Red
}

# 4. Check bindings
Write-Host "`n4. Checking bindings..." -ForegroundColor Yellow
try {
    $bindings = Invoke-RestMethod -Uri "http://localhost:15672/api/bindings" -UseBasicParsing
    $skillBinding = $bindings | Where-Object { 
        $_.source -eq "employee.events" -and 
        $_.destination -eq "skillservice_employee_created" 
    }
    if ($skillBinding) {
        Write-Host "   ✓ Binding exists: employee.events → skillservice_employee_created" -ForegroundColor Green
    } else {
        Write-Host "   ✗ Binding not found" -ForegroundColor Red
        Write-Host "   → SkillService needs to start to create binding" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ✗ Error checking bindings: $_" -ForegroundColor Red
}

# 5. Instructions for manual testing
Write-Host "`n5. Manual Testing Instructions:" -ForegroundColor Yellow
Write-Host "   To manually test event consumption:" -ForegroundColor Cyan
Write-Host "   1. Open RabbitMQ Management UI: http://localhost:15672 (guest/guest)" -ForegroundColor White
Write-Host "   2. Go to Exchanges → employee.events" -ForegroundColor White
Write-Host "   3. Scroll to 'Publish message' section" -ForegroundColor White
Write-Host "   4. Use this test payload:" -ForegroundColor White
Write-Host ""
$testGuid = New-Guid
$testPayload = @{
    employeeId = $testGuid.ToString()
    name = "Test Employee"
    email = "test@example.com"
    role = "Developer"
} | ConvertTo-Json
Write-Host $testPayload -ForegroundColor Gray
Write-Host ""
Write-Host "   5. Click 'Publish message'" -ForegroundColor White
Write-Host "   6. Check SkillService console for:" -ForegroundColor White
Write-Host "      'Received EmployeeCreatedEvent: EmployeeId=$testGuid'" -ForegroundColor Gray
Write-Host "   7. Check database: SELECT * FROM EmployeeReferences WHERE EmployeeId='$testGuid'" -ForegroundColor White

Write-Host "`n=== Test Complete ===" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  - If consumer is attached: Test by publishing a message via RabbitMQ UI" -ForegroundColor White
Write-Host "  - If consumer is NOT attached: Start SkillService (cd SkillService; dotnet run)" -ForegroundColor White
Write-Host "  - Check SkillService console logs for event consumption" -ForegroundColor White


