#!/bin/bash

# Deploy Docker image to AWS ECR and update ECS service
# Usage: ./deploy-docker.sh

set -e

# Configuration
REGION="us-east-1"
ACCOUNT_ID="946591677346"
REPOSITORY_NAME="bettercallsaul-api"
CLUSTER_NAME="bettercallsaul-cluster-production"
SERVICE_NAME="bettercallsaul-api"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if we're in the right directory
if [ ! -f "Dockerfile" ]; then
    print_error "Dockerfile not found. Please run this script from the project root directory."
    exit 1
fi

# Get current git commit hash for tagging
GIT_COMMIT=$(git rev-parse --short HEAD)
IMAGE_TAG="${GIT_COMMIT}"

print_status "Building Docker image..."
sudo docker build -t ${REPOSITORY_NAME}:${IMAGE_TAG} .
sudo docker tag ${REPOSITORY_NAME}:${IMAGE_TAG} ${REPOSITORY_NAME}:latest

print_status "Logging into AWS ECR..."
aws ecr get-login-password --region ${REGION} | sudo docker login --username AWS --password-stdin ${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com

print_status "Tagging image for ECR..."
sudo docker tag ${REPOSITORY_NAME}:${IMAGE_TAG} ${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/${REPOSITORY_NAME}:${IMAGE_TAG}
sudo docker tag ${REPOSITORY_NAME}:latest ${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/${REPOSITORY_NAME}:latest

print_status "Pushing image to ECR..."
sudo docker push ${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/${REPOSITORY_NAME}:${IMAGE_TAG}
sudo docker push ${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/${REPOSITORY_NAME}:latest

print_status "Updating ECS service to force new deployment..."
aws ecs update-service \
    --cluster ${CLUSTER_NAME} \
    --service ${SERVICE_NAME} \
    --force-new-deployment \
    --region ${REGION}

print_success "Deployment initiated!"
print_status "Monitor deployment progress with:"
echo "aws ecs describe-services --cluster ${CLUSTER_NAME} --services ${SERVICE_NAME} --query 'services[0].events[:3]'"
print_status "Check logs with:"
echo "aws logs tail /ecs/bettercallsaul-api-production --follow"

print_status "Waiting for service to stabilize..."
aws ecs wait services-stable --cluster ${CLUSTER_NAME} --services ${SERVICE_NAME} --region ${REGION}

print_success "✅ Deployment completed successfully!"
print_status "Testing API endpoint..."
sleep 10
curl -f http://bettercallsaul-alb-production-1289827668.us-east-1.elb.amazonaws.com/health && print_success "✅ API is responding!" || print_error "❌ API health check failed"