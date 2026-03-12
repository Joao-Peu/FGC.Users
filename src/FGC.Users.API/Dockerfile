FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/FGC.Users.API/FGC.Users.API.csproj", "src/FGC.Users.API/"]
COPY ["src/FGC.Users.Application/FGC.Users.Application.csproj", "src/FGC.Users.Application/"]
COPY ["src/FGC.Users.Domain/FGC.Users.Domain.csproj", "src/FGC.Users.Domain/"]
COPY ["src/FGC.Users.Infrastructure/FGC.Users.Infrastructure.csproj", "src/FGC.Users.Infrastructure/"]
RUN dotnet restore "src/FGC.Users.API/FGC.Users.API.csproj"
COPY . .
WORKDIR "/src/src/FGC.Users.API"
RUN dotnet publish "FGC.Users.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FGC.Users.API.dll"]
