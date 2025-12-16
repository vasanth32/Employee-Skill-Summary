# Manual RabbitMQ Testing Guide

## Verify SkillService is Listening to RabbitMQ

### Method 1: Check SkillService Console Logs

1. **Start SkillService:**
   ```powershell
   cd SkillService
   dotnet run
   ```

2. **Look for this log message:**
   ```
   EmployeeCreatedEvent consumer started. Waiting for messages...
   ```
   ✅ If you see this, the consumer is running and listening!

3. **If you don't see this message:**
   - Check for errors in console
   - Verify RabbitMQ is running
   - Check connection string in `appsettings.json`

---

### Method 2: Check RabbitMQ Management UI

1. **Open RabbitMQ Management UI:**
   ```
   http://localhost:15672
   ```
   Login: `guest` / `guest`

2. **Check Queues:**
   - Go to **Queues** tab
   - Look for: `skillservice_employee_created`
   - Status should show: **Ready** (consumer attached)

3. **Check Exchange:**
   - Go to **Exchanges** tab
   - Look for: `employee.events`
   - Type: `fanout`
   - Should have bindings

4. **Check Bindings:**
   - Click on `employee.events` exchange
   - Go to **Bindings** tab
   - Should see: `skillservice_employee_created` queue bound to it

---

### Method 3: Manual Message Publishing via RabbitMQ UI

1. **Open RabbitMQ Management UI:**
   ```
   http://localhost:15672
   ```

2. **Navigate to Exchange:**
   - Go to **Exchanges** → `employee.events`

3. **Publish Message:**
   - Click on the exchange name
   - Scroll down to **Publish message** section
   - **Routing key:** Leave empty (fanout doesn't use routing keys)
   - **Payload:** Paste this JSON:
   ```json
   {
     "employeeId": "12345678-1234-1234-1234-123456789012",
     "name": "Test Employee",
     "email": "test.employee@example.com",
     "role": "Developer"
   }
   ```
   - Click **Publish message**

4. **Check SkillService Console:**
   - You should immediately see:
   ```
   Received EmployeeCreatedEvent: EmployeeId=12345678-1234-1234-1234-123456789012, Name=Test Employee, Role=Developer
   Stored employee reference: EmployeeId=12345678-1234-1234-1234-123456789012
   ```

5. **Verify in Database:**
   ```sql
   SELECT * FROM EmployeeReferences 
   WHERE EmployeeId = '12345678-1234-1234-1234-123456789012'
   ```

---

### Method 4: Use RabbitMQ CLI (Command Line)

1. **Check if queue exists:**
   ```powershell
   rabbitmqctl list_queues name consumers
   ```
   Should show: `skillservice_employee_created` with `1` consumer

2. **Check exchange:**
   ```powershell
   rabbitmqctl list_exchanges name type
   ```
   Should show: `employee.events` with type `fanout`

3. **Check bindings:**
   ```powershell
   rabbitmqctl list_bindings
   ```
   Should show binding between `employee.events` and `skillservice_employee_created`

4. **Publish message manually:**
   ```powershell
   # Create a test message file
   $message = @'
   {
     "employeeId": "87654321-4321-4321-4321-210987654321",
     "name": "CLI Test Employee",
     "email": "cli.test@example.com",
     "role": "Tester"
   }
   '@
   $message | Out-File -FilePath test-message.json -Encoding UTF8

   # Publish using rabbitmqadmin (if installed)
   rabbitmqadmin publish exchange=employee.events routing_key="" payload=@test-message.json
   ```

---

### Method 5: Use Postman to Create Employee (Real Event)

1. **Use Postman Collection:**
   - Run: `2. Create Employee (Triggers Event) > Create Employee`
   - This creates a real employee and publishes event

2. **Watch SkillService Console:**
   - Should see event consumption immediately
   - Check for log messages

3. **Verify:**
   - Check database for new employee reference
   - Check RabbitMQ UI for message in exchange

---

### Method 6: Create Test Script (PowerShell)

Create a file `test-rabbitmq-consumer.ps1`:

```powershell
# Test RabbitMQ Consumer
Write-Host "=== Testing SkillService RabbitMQ Consumer ===" -ForegroundColor Green

# 1. Check if RabbitMQ is running
Write-Host "`n1. Checking RabbitMQ..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:15672/api/overview" -UseBasicParsing -ErrorAction Stop
    Write-Host "✓ RabbitMQ is running" -ForegroundColor Green
} catch {
    Write-Host "✗ RabbitMQ is not running or not accessible" -ForegroundColor Red
    Write-Host "  Start RabbitMQ: docker start rabbitmq" -ForegroundColor Yellow
    exit
}

# 2. Check if queue exists
Write-Host "`n2. Checking queue..." -ForegroundColor Yellow
try {
    $queues = Invoke-RestMethod -Uri "http://localhost:15672/api/queues" -UseBasicParsing
    $skillQueue = $queues | Where-Object { $_.name -eq "skillservice_employee_created" }
    if ($skillQueue) {
        Write-Host "✓ Queue exists: skillservice_employee_created" -ForegroundColor Green
        Write-Host "  Consumers: $($skillQueue.consumers)" -ForegroundColor Cyan
        if ($skillQueue.consumers -gt 0) {
            Write-Host "  ✓ Consumer is attached!" -ForegroundColor Green
        } else {
            Write-Host "  ✗ No consumer attached - SkillService may not be running" -ForegroundColor Red
        }
    } else {
        Write-Host "✗ Queue not found - SkillService needs to start first" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Error checking queues: $_" -ForegroundColor Red
}

# 3. Check exchange
Write-Host "`n3. Checking exchange..." -ForegroundColor Yellow
try {
    $exchanges = Invoke-RestMethod -Uri "http://localhost:15672/api/exchanges" -UseBasicParsing
    $employeeExchange = $exchanges | Where-Object { $_.name -eq "employee.events" }
    if ($employeeExchange) {
        Write-Host "✓ Exchange exists: employee.events" -ForegroundColor Green
    } else {
        Write-Host "✗ Exchange not found" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Error checking exchanges: $_" -ForegroundColor Red
}

# 4. Test message publishing
Write-Host "`n4. Testing message publish..." -ForegroundColor Yellow
Write-Host "   Open RabbitMQ UI: http://localhost:15672" -ForegroundColor Cyan
Write-Host "   Go to Exchanges → employee.events → Publish message" -ForegroundColor Cyan
Write-Host "   Use this payload:" -ForegroundColor Cyan
Write-Host @"
{
  "employeeId": "$(New-Guid)",
  "name": "Test Employee",
  "email": "test@example.com",
  "role": "Developer"
}
"@ -ForegroundColor White

Write-Host "`n=== Test Complete ===" -ForegroundColor Green
Write-Host "Check SkillService console for event consumption" -ForegroundColor Yellow
```

Run it:
```powershell
.\test-rabbitmq-consumer.ps1
```

---

## Troubleshooting

### Consumer Not Starting

**Check SkillService logs for errors:**
- Connection errors
- Authentication errors
- Exchange/queue creation errors

**Common issues:**
1. **RabbitMQ not running:**
   ```powershell
   docker ps  # Check if rabbitmq container is running
   docker start rabbitmq  # Start if stopped
   ```

2. **Wrong connection string:**
   - Check `appsettings.json`:
   ```json
   "RabbitMQ": {
     "HostName": "localhost",
     "UserName": "guest",
     "Password": "guest"
   }
   ```

3. **Port blocked:**
   - Default port: 5672
   - Check firewall settings

### Messages Not Consumed

1. **Check queue has consumer:**
   - RabbitMQ UI → Queues → `skillservice_employee_created`
   - Should show: `Consumers: 1`

2. **Check message is in queue:**
   - RabbitMQ UI → Queues → `skillservice_employee_created`
   - Check "Ready" count

3. **Check SkillService logs:**
   - Look for error messages
   - Check if consumer is processing

### Verify Consumer is Processing

1. **Publish test message via UI**
2. **Watch SkillService console** - should see logs immediately
3. **Check database** - should see new EmployeeReference entry
4. **Check queue** - message count should decrease

---

## Quick Test Checklist

- [ ] RabbitMQ is running (http://localhost:15672)
- [ ] SkillService is running (check console logs)
- [ ] Queue `skillservice_employee_created` exists
- [ ] Queue has 1 consumer attached
- [ ] Exchange `employee.events` exists
- [ ] Binding exists between exchange and queue
- [ ] Publish test message → Check SkillService logs
- [ ] Verify database has new entry

---

## Expected Console Output

When SkillService starts:
```
info: SkillService.Messaging.EmployeeCreatedEventConsumer[0]
      EmployeeCreatedEvent consumer started. Waiting for messages...
```

When message is received:
```
info: SkillService.Messaging.EmployeeCreatedEventConsumer[0]
      Received EmployeeCreatedEvent: EmployeeId=..., Name=..., Role=...
info: SkillService.Messaging.EmployeeCreatedEventConsumer[0]
      Stored employee reference: EmployeeId=...
```


