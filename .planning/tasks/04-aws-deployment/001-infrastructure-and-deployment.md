# Task: AWS Infrastructure Setup and Production Deployment

## Overview
- **Parent Feature**: AWS Migration - Production Deployment
- **Complexity**: High
- **Estimated Time**: 18 hours
- **Status**: Not Started

**Note**: This combines infrastructure templates, deployment automation, and production validation.

## Dependencies
### Required Tasks
- [ ] 03-integration-testing/001-azure-aws-validation: Integration testing complete
- All AWS service implementations ready and tested

### External Dependencies
- AWS production account setup
- Domain/SSL certificates configured
- CI/CD pipeline access (GitHub Actions or Azure DevOps)

## Implementation Details
### Files to Create/Modify
- `infrastructure/aws/cloudformation-template.yaml`: AWS infrastructure as code
- `infrastructure/aws/deploy.sh`: Deployment automation script
- `.github/workflows/deploy-aws.yml`: CI/CD pipeline for AWS deployment
- Update deployment documentation

### AWS Infrastructure Components
1. **Compute**: ECS Fargate or App Runner for containerized .NET app
2. **Storage**: S3 bucket for documents with proper IAM policies
3. **AI Services**: Bedrock access permissions
4. **Database**: RDS for SQL Server or PostgreSQL
5. **Security**: Secrets Manager, IAM roles, VPC configuration
6. **Monitoring**: CloudWatch logs and metrics

## Acceptance Criteria
- [ ] AWS infrastructure deploys successfully via automation
- [ ] All AWS services (Bedrock, S3, Textract) are accessible from app
- [ ] Database migration runs successfully
- [ ] SSL certificates and domain routing configured
- [ ] Environment variables and secrets properly managed
- [ ] Application starts and passes health checks
- [ ] End-to-end functionality validation in production
- [ ] Monitoring and alerting configured

## Key Implementation Areas

### Infrastructure as Code (CloudFormation)
```yaml
# Simplified structure
Resources:
  BetterCallSaulAppService:
    Type: AWS::AppRunner::Service
    Properties:
      ServiceName: better-call-saul-api
      SourceConfiguration:
        ImageRepository:
          ImageIdentifier: !Sub "${AWS::AccountId}.dkr.ecr.${AWS::Region}.amazonaws.com/better-call-saul:latest"

  DocumentStorageBucket:
    Type: AWS::S3::Bucket
    Properties:
      BucketName: !Sub "better-call-saul-docs-${Environment}"
      PublicAccessBlockConfiguration:
        BlockPublicAcls: true

  BedrockExecutionRole:
    Type: AWS::IAM::Role
    Properties:
      AssumeRolePolicyDocument:
        Statement:
          - Effect: Allow
            Principal:
              Service: apprunner.amazonaws.com
```

### Environment Configuration
- **Production**: AWS Bedrock + S3 + Textract
- **Staging**: AWS services with test buckets
- **Development**: Local Azure services (no change)

### Deployment Pipeline
1. **Build**: Docker container with .NET app
2. **Test**: Integration tests against AWS staging
3. **Deploy**: CloudFormation stack update
4. **Validate**: Health checks and smoke tests

### Security Configuration
- **IAM Roles**: Minimal permissions for each service
- **Secrets Manager**: Database connections, API keys
- **VPC**: Network security groups and subnets
- **SSL/TLS**: HTTPS enforcement with AWS Certificate Manager

## Production Validation Checklist
- [ ] Case analysis produces expected results
- [ ] File upload/download works from frontend
- [ ] Document text extraction processes correctly
- [ ] User authentication and authorization functional
- [ ] Database operations complete successfully
- [ ] Performance within acceptable limits
- [ ] Error logging and monitoring operational

## Rollback Strategy
- **CloudFormation Rollback**: Automatic stack rollback on failure
- **Database Rollback**: Backup restoration procedures
- **DNS Failover**: Route traffic back to Azure environment if needed
- **Configuration Rollback**: Switch back to Azure via environment variables

## Monitoring and Alerting
- **Application Metrics**: Response times, error rates, throughput
- **AWS Service Metrics**: Bedrock usage, S3 operations, Textract jobs
- **Cost Monitoring**: Daily AWS spend alerts
- **Health Checks**: Synthetic transaction monitoring

## Documentation Updates
- **Deployment Guide**: Step-by-step AWS deployment instructions
- **Configuration Reference**: All AWS environment variables
- **Troubleshooting Guide**: Common issues and resolution steps
- **Cost Optimization**: AWS service cost comparison and recommendations

This comprehensive task ensures AWS production deployment is reliable, secure, and properly monitored.