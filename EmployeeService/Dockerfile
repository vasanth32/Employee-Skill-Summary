# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["EmployeeService/EmployeeService.csproj", "EmployeeService/"]
COPY ["SharedModels/SharedModels.csproj", "SharedModels/"]

# Restore dependencies
RUN dotnet restore "EmployeeService/EmployeeService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/EmployeeService"
RUN dotnet build "EmployeeService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "EmployeeService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EmployeeService.dll"]

