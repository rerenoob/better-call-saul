# GitHub Actions AWS Secrets Setup Guide

## Required Secrets

To enable AWS deployment via GitHub Actions, you need to configure the following secrets in your GitHub repository:

### 1. AWS Credentials
- **AWS_ACCESS_KEY_ID** - AWS IAM user access key ID
- **AWS_SECRET_ACCESS_KEY** - AWS IAM user secret access key

### 2. AWS Service Configuration
- **CLOUDFRONT_DISTRIBUTION_ID** - CloudFront distribution ID (from infrastructure deployment)
- **API_BASE_URL** - ECS service URL (e.g., `http://ecs-load-balancer-url`)

## Setting Up Secrets

### Option 1: GitHub Web Interface
1. Go to your repository on GitHub
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add each secret with the exact names listed above

### Option 2: GitHub CLI
```bash
# Install GitHub CLI first: https://cli.github.com/
gh secret set AWS_ACCESS_KEY_ID --body "your-access-key-id"
gh secret set AWS_SECRET_ACCESS_KEY --body "your-secret-access-key"
gh secret set CLOUDFRONT_DISTRIBUTION_ID --body "your-distribution-id"
gh secret set API_BASE_URL --body "http://your-ecs-url"
```

## IAM User Permissions

The AWS IAM user should have the following permissions:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "ecr:*",
                "ecs:*",
                "s3:*",
                "cloudfront:*",
                "logs:*",
                "iam:PassRole"
            ],
            "Resource": "*"
        }
    ]
}
```

## Testing the Setup

1. Push to the main branch to trigger the deployment workflows
2. Monitor the GitHub Actions runs in the **Actions** tab
3. Check AWS CloudWatch logs for any deployment issues

## Troubleshooting

### Common Issues

1. **Permission denied errors**: Verify IAM user has correct permissions
2. **Resource not found**: Ensure infrastructure is deployed first using `./deploy-infrastructure.sh`
3. **Build failures**: Check that all required files exist (Dockerfile, task-definition.json)

### Debugging

- Check GitHub Actions logs for detailed error messages
- Verify AWS credentials are correct
- Ensure CloudFormation stack deployment completed successfully