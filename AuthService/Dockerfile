# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["AuthService/AuthService.csproj", "AuthService/"]
COPY ["SharedModels/SharedModels.csproj", "SharedModels/"]

# Restore dependencies
RUN dotnet restore "AuthService/AuthService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/AuthService"
RUN dotnet build "AuthService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AuthService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.dll"]

