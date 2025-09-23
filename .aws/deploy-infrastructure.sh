#!/bin/bash

set -e

# Configuration
STACK_NAME="better-call-saul-infrastructure"
ENVIRONMENT="production"
REGION="us-east-1"
DOMAIN_NAME="bettercallsaul.example.com"

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
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if AWS CLI is installed
if ! command -v aws &> /dev/null; then
    print_error "AWS CLI is not installed. Please install it first."
    exit 1
fi

# Check if AWS credentials are configured
if ! aws sts get-caller-identity &> /dev/null; then
    print_error "AWS credentials are not configured. Please run 'aws configure' first."
    exit 1
fi

# Validate CloudFormation template
print_status "Validating CloudFormation template..."
if ! aws cloudformation validate-template --template-body file://cloudformation-stack.yml --region $REGION > /dev/null 2>&1; then
    print_error "CloudFormation template validation failed."
    exit 1
fi

# Check if stack exists
print_status "Checking if stack exists..."
if aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION > /dev/null 2>&1; then
    print_status "Stack exists, updating..."
    OPERATION="update-stack"
else
    print_status "Stack does not exist, creating..."
    OPERATION="create-stack"
fi

# Generate secure database password if not provided
if [ -z "$DATABASE_PASSWORD" ]; then
    DATABASE_PASSWORD=$(openssl rand -base64 32)
    print_status "Generated secure database password"
fi

# Deploy CloudFormation stack
print_status "Deploying CloudFormation stack: $STACK_NAME"

aws cloudformation $OPERATION \
    --stack-name $STACK_NAME \
    --template-body file://cloudformation-stack.yml \
    --parameters \
        ParameterKey=EnvironmentName,ParameterValue=$ENVIRONMENT \
        ParameterKey=DatabaseUsername,ParameterValue=bettercallsaul \
        ParameterKey=DatabasePassword,ParameterValue=$DATABASE_PASSWORD \
    --capabilities CAPABILITY_NAMED_IAM \
    --region $REGION

# Wait for stack to complete
print_status "Waiting for stack operation to complete..."
if [ "$OPERATION" = "create-stack" ]; then
    aws cloudformation wait stack-create-complete \
        --stack-name $STACK_NAME \
        --region $REGION
else
    aws cloudformation wait stack-update-complete \
        --stack-name $STACK_NAME \
        --region $REGION
fi

# Get stack outputs
print_status "Retrieving stack outputs..."
aws cloudformation describe-stacks \
    --stack-name $STACK_NAME \
    --region $REGION \
    --query 'Stacks[0].Outputs'

print_status "Infrastructure deployment completed successfully!"

# Display next steps
print_status ""
print_status "Next steps:"
print_status "1. Update your GitHub repository secrets with the following:"
print_status "   - AWS_ACCESS_KEY_ID"
print_status "   - AWS_SECRET_ACCESS_KEY"
print_status "   - CLOUDFRONT_DISTRIBUTION_ID (from stack outputs)"
print_status "   - API_BASE_URL (ECS service URL)"
print_status ""
print_status "2. Push to main branch to trigger AWS deployment workflows"