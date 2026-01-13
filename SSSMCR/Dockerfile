# ============================
# BUILD STAGE
# ============================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore "SSSMCR.sln"

RUN dotnet publish "SSSMCR.ApiService/SSSMCR.ApiService.csproj" -c Release -o /app/out

# ============================
# RUNTIME STAGE
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/out .

EXPOSE 5000
EXPOSE 5001

ENTRYPOINT ["dotnet", "SSSMCR.ApiService.dll"]
