#!/bin/bash

# Better Call Saul Deployment Script
# This script provides common deployment commands

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if .env file exists
if [ ! -f .env ]; then
    print_warning ".env file not found. Creating from example..."
    cp .env.example .env
    print_status "Please update .env file with your actual configuration values"
    exit 1
fi

# Load environment variables
export $(grep -v '^#' .env | xargs)

# Deployment functions

deploy_docker() {
    print_status "Building and deploying with Docker Compose..."
    docker-compose up --build -d
    print_status "Application deployed successfully!"
    echo "Access the application at: http://localhost:8080"
}

deploy_production() {
    print_status "Deploying production configuration..."
    docker-compose -f docker-compose.yml -f docker-compose.production.yml up --build -d
    print_status "Production deployment completed!"
}

deploy_azure() {
    print_status "Deploying to Azure App Service..."
    
    # Build the application
    dotnet publish -c Release -o ./publish
    
    # Deploy to Azure (requires Azure CLI and configured profile)
    az webapp up \
        --name better-call-saul-app \
        --resource-group better-call-saul-rg \
        --plan better-call-saul-plan \
        --runtime "DOTNETCORE:8.0" \
        --html
    
    print_status "Azure deployment completed!"
}

migrate_database() {
    print_status "Applying database migrations..."
    
    # Install EF tools if not available
    if ! command -v dotnet-ef &> /dev/null; then
        print_status "Installing Entity Framework tools..."
        dotnet tool install --global dotnet-ef
    fi
    
    # Apply migrations
    dotnet ef database update --connection "$ConnectionStrings__DefaultConnection"
    
    print_status "Database migrations applied successfully!"
}

check_health() {
    print_status "Checking application health..."
    
    # Wait a bit for application to start
    sleep 10
    
    # Check health endpoint
    if curl -f http://localhost:8080/health; then
        print_status "Application is healthy!"
    else
        print_error "Application health check failed"
        exit 1
    fi
}

# Main deployment logic
case "$1" in
    "docker")
        deploy_docker
        ;;
    "production")
        deploy_production
        ;;
    "azure")
        deploy_azure
        ;;
    "migrate")
        migrate_database
        ;;
    "health")
        check_health
        ;;
    "full")
        deploy_docker
        migrate_database
        check_health
        ;;
    *)
        echo "Usage: $0 {docker|production|azure|migrate|health|full}"
        echo ""
        echo "Commands:"
        echo "  docker     - Deploy using Docker Compose (development)"
        echo "  production - Deploy production configuration"
        echo "  azure      - Deploy to Azure App Service"
        echo "  migrate    - Apply database migrations"
        echo "  health     - Check application health"
        echo "  full       - Full deployment (docker + migrate + health)"
        exit 1
        ;;
esac

print_status "Deployment process completed!"