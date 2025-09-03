# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["better-call-saul.csproj", "./"]
RUN dotnet restore "better-call-saul.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "better-call-saul.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "better-call-saul.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install required system dependencies for document processing
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create uploads directory and set permissions
RUN mkdir -p /app/uploads && \
    chmod 755 /app/uploads && \
    chown -R www-data:www-data /app/uploads

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Expose ports
EXPOSE 80
EXPOSE 443

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Entry point
ENTRYPOINT ["dotnet", "better-call-saul.dll"]