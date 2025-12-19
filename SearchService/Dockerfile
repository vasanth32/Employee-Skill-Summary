# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SearchService/SearchService.csproj", "SearchService/"]
COPY ["SharedModels/SharedModels.csproj", "SharedModels/"]

# Restore dependencies
RUN dotnet restore "SearchService/SearchService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/SearchService"
RUN dotnet build "SearchService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SearchService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SearchService.dll"]

