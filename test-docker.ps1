# Docker Services Testing Script
# This script helps verify that all Docker services are running correctly

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Docker Services Testing Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to test HTTP endpoint
function Test-Service {
    param(
        [string]$ServiceName,
        [string]$Url,
        [int]$Port
    )
    
    Write-Host "Testing $ServiceName..." -ForegroundColor Yellow -NoNewline
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host " ✓ OK (Port $Port)" -ForegroundColor Green
            return $true
        } else {
            Write-Host " ✗ Failed (Status: $($response.StatusCode))" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host " ✗ Failed ($($_.Exception.Message))" -ForegroundColor Red
        return $false
    }
}

# Check Docker Compose
Write-Host "Step 1: Checking Docker Compose..." -ForegroundColor Cyan
if (Get-Command docker-compose -ErrorAction SilentlyContinue) {
    Write-Host "  ✓ docker-compose is available" -ForegroundColor Green
} else {
    Write-Host "  ✗ docker-compose not found. Please install Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check if containers are running
Write-Host "`nStep 2: Checking container status..." -ForegroundColor Cyan
$containers = docker-compose ps --services 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ Docker Compose file found" -ForegroundColor Green
    
    $running = docker-compose ps --format json | ConvertFrom-Json | Where-Object { $_.State -eq "running" }
    Write-Host "  Running containers: $($running.Count)" -ForegroundColor Yellow
    
    docker-compose ps
} else {
    Write-Host "  ✗ Could not read docker-compose.yml" -ForegroundColor Red
    Write-Host "  Please ensure you're in the project root directory" -ForegroundColor Yellow
    exit 1
}

Write-Host "`nStep 3: Testing service endpoints..." -ForegroundColor Cyan
Write-Host "  (Waiting 5 seconds for services to be ready...)" -ForegroundColor Yellow
Start-Sleep -Seconds 5

$results = @{}

# Test services
$results["API Gateway"] = Test-Service "API Gateway" "http://localhost:5112/swagger/index.html" 5112
$results["Auth Service"] = Test-Service "Auth Service" "http://localhost:5163/swagger/index.html" 5163
$results["Employee Service"] = Test-Service "Employee Service" "http://localhost:5110/swagger/index.html" 5110
$results["Skill Service"] = Test-Service "Skill Service" "http://localhost:5212/swagger/index.html" 5212
$results["Search Service"] = Test-Service "Search Service" "http://localhost:5234/swagger/index.html" 5234
$results["RabbitMQ Management"] = Test-Service "RabbitMQ Management" "http://localhost:15672" 15672

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$passed = ($results.Values | Where-Object { $_ -eq $true }).Count
$total = $results.Count

foreach ($key in $results.Keys) {
    $status = if ($results[$key]) { "✓ PASS" } else { "✗ FAIL" }
    $color = if ($results[$key]) { "Green" } else { "Red" }
    Write-Host "  $key : $status" -ForegroundColor $color
}

Write-Host ""
if ($passed -eq $total) {
    Write-Host "All services are running correctly! ($passed/$total)" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Run database migrations (see DOCKER_TESTING_GUIDE.md)" -ForegroundColor White
    Write-Host "  2. Test API endpoints via Swagger UI" -ForegroundColor White
    Write-Host "  3. Verify RabbitMQ message flow" -ForegroundColor White
} else {
    Write-Host "Some services are not responding ($passed/$total passed)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Check logs: docker-compose logs -f" -ForegroundColor White
    Write-Host "  2. Restart services: docker-compose restart" -ForegroundColor White
    Write-Host "  3. Rebuild: docker-compose up -d --build" -ForegroundColor White
}

Write-Host ""
Write-Host "Useful commands:" -ForegroundColor Cyan
Write-Host "  docker-compose ps              - View container status" -ForegroundColor White
Write-Host "  docker-compose logs -f         - View all logs" -ForegroundColor White
Write-Host "  docker-compose logs -f <service> - View specific service logs" -ForegroundColor White
Write-Host "  docker-compose restart <service> - Restart a service" -ForegroundColor White
Write-Host "  docker-compose down             - Stop all services" -ForegroundColor White
Write-Host ""

