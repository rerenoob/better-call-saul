# Manual Deployment Commands

Since Docker requires special permissions, here are the exact commands to run manually:

## 1. Build and Push Docker Image

```bash
# Navigate to project directory
cd /home/dpham/Projects/better-call-saul

# Build the Docker image
sudo docker build -t bettercallsaul-api:latest .

# Login to AWS ECR
aws ecr get-login-password --region us-east-1 | sudo docker login --username AWS --password-stdin 946591677346.dkr.ecr.us-east-1.amazonaws.com

# Tag the image for ECR
sudo docker tag bettercallsaul-api:latest 946591677346.dkr.ecr.us-east-1.amazonaws.com/bettercallsaul-api:latest

# Push to ECR
sudo docker push 946591677346.dkr.ecr.us-east-1.amazonaws.com/bettercallsaul-api:latest
```

## 2. Update ECS Service

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

## 3. Verify Deployment

```bash
# Check service status
aws ecs describe-services --cluster bettercallsaul-cluster-production --services bettercallsaul-api --query "services[0].[serviceName,status,runningCount,desiredCount]" --output table

# Test API health endpoint
curl -v http://bettercallsaul-alb-production-1289827668.us-east-1.elb.amazonaws.com/health

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