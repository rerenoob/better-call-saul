# Complete AWS Deployment Commands

## Issues Fixed ✅
1. **Backend AWS credentials** - Removed environment variable requirement, now uses ECS IAM roles
2. **Frontend API endpoint** - Fixed to use `VITE_API_BASE_URL` and connect to AWS ALB
3. **CORS configuration** - Added production CloudFront URL to backend CORS policy

## Required Deployment Steps

### 1. Build and Push Docker Image (Backend)

```bash
# Navigate to project directory
cd /home/dpham/Projects/better-call-saul

# Build the Docker image with the AWS credentials fix
sudo docker build -t bettercallsaul-api:latest .

# Login to AWS ECR
aws ecr get-login-password --region us-east-1 | sudo docker login --username AWS --password-stdin 946591677346.dkr.ecr.us-east-1.amazonaws.com

# Tag the image for ECR
sudo docker tag bettercallsaul-api:latest 946591677346.dkr.ecr.us-east-1.amazonaws.com/bettercallsaul-api:latest

# Push to ECR
sudo docker push 946591677346.dkr.ecr.us-east-1.amazonaws.com/bettercallsaul-api:latest
```

### 2. Update ECS Service

```bash
# Force new deployment with the updated image
aws ecs update-service \
    --cluster bettercallsaul-cluster-production \
    --service bettercallsaul-api \
    --force-new-deployment \
    --region us-east-1

# Wait for deployment to complete
aws ecs wait services-stable --cluster bettercallsaul-cluster-production --services bettercallsaul-api --region us-east-1
```

### 3. Build and Deploy Frontend

```bash
# Navigate to frontend directory
cd /home/dpham/Projects/better-call-saul/better-call-saul-frontend

# Install dependencies
npm install

# Build for production (uses .env.production with AWS ALB endpoint)
npm run build

# Deploy to S3 (check CloudFormation outputs for exact bucket name)
aws s3 sync dist/ s3://better-call-saul-frontend-production/ --delete

# Invalidate CloudFront cache
aws cloudfront create-invalidation --distribution-id d1c0215ar7cs56 --paths "/*"
```

### 4. Verify Deployment

```bash
# Check ECS service status
aws ecs describe-services --cluster bettercallsaul-cluster-production --services bettercallsaul-api --query "services[0].[serviceName,status,runningCount,desiredCount]" --output table

# Test API health endpoint
curl -v http://bettercallsaul-alb-production-1289827668.us-east-1.elb.amazonaws.com/health

# Test frontend
echo "Frontend URL: https://d1c0215ar7cs56.cloudfront.net"

# Check logs if needed
aws logs tail /ecs/bettercallsaul-api-production --follow
```

## What the Fix Addresses

The fix I made addresses the main issue:
- **Removed AWS credentials environment variable requirement** in production
- **Uses ECS IAM roles** instead (which are already configured)
- **Updated frontend** to use the correct AWS backend URL

## Expected Result

After running these commands:
1. ✅ ECS tasks should start successfully (no more exit code 139)
2. ✅ API health endpoint should return HTTP 200
3. ✅ Frontend at https://d1c0215ar7cs56.cloudfront.net should connect to backend
4. ✅ Full application should be functional

## Next Steps After Deployment

1. **Test frontend-backend integration**
2. **Verify database connectivity**
3. **Test user registration and authentication**
4. **Set up monitoring and alerts**

---

**Note**: These commands need to be run with sudo permissions for Docker, which is why the automated script couldn't complete.