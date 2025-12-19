# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SkillService/SkillService.csproj", "SkillService/"]
COPY ["SharedModels/SharedModels.csproj", "SharedModels/"]

# Restore dependencies
RUN dotnet restore "SkillService/SkillService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/SkillService"
RUN dotnet build "SkillService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SkillService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SkillService.dll"]

