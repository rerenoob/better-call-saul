FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BetterCallSaul.API/BetterCallSaul.API.csproj", "BetterCallSaul.API/"]
COPY ["BetterCallSaul.Core/BetterCallSaul.Core.csproj", "BetterCallSaul.Core/"]
COPY ["BetterCallSaul.Infrastructure/BetterCallSaul.Infrastructure.csproj", "BetterCallSaul.Infrastructure/"]
RUN dotnet restore "BetterCallSaul.API/BetterCallSaul.API.csproj"
COPY . .
WORKDIR "/src/BetterCallSaul.API"
RUN dotnet build "BetterCallSaul.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BetterCallSaul.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BetterCallSaul.API.dll"]