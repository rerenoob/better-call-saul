# Simplified Implementation Plan: AWS Migration
*Created: 2025-09-14*

## Overview
This simplified plan focuses exclusively on AWS compatibility, reducing complexity and timeline from the original cloud-agnostic approach.

**Timeline: 4-5 weeks (vs 8 weeks for full multi-cloud)**
**Tasks: 12 (vs 23 for multi-cloud)**
**Focus: AWS production deployment readiness**

## Phase 1: Foundation (Week 1)

### Task 1.1: Core Service Interfaces
**ID**: AWS-001
**Effort**: 8 hours
**Dependencies**: None

Create minimal interfaces for Azure/AWS service switching:
- `IAIService` - AI analysis functionality
- `IStorageService` - File upload/download
- `IDocumentProcessingService` - Text extraction

**Files**:
- `BetterCallSaul.Core/Interfaces/Services/IAIService.cs`
- `BetterCallSaul.Core/Interfaces/Services/IStorageService.cs`
- `BetterCallSaul.Core/Interfaces/Services/IDocumentProcessingService.cs`

### Task 1.2: Configuration System
**ID**: AWS-002
**Effort**: 6 hours
**Dependencies**: AWS-001

Add AWS configuration support:
- Update `appsettings.json` with AWS sections
- Environment variable override support
- Provider selection logic

**Files**:
- `appsettings.json`, `appsettings.Production.json`
- `BetterCallSaul.API/Program.cs`

### Task 1.3: Service Registration
**ID**: AWS-003
**Effort**: 4 hours
**Dependencies**: AWS-001, AWS-002

Update dependency injection for provider switching:
- Service factory for Azure/AWS selection
- Health checks for both providers

**Files**:
- `BetterCallSaul.API/Program.cs`
- Service registration extensions

## Phase 2: AWS Implementation (Week 2-3)

### Task 2.1: AWS Bedrock Service
**ID**: AWS-004
**Effort**: 16 hours
**Dependencies**: AWS-001, AWS-003

Replace Azure OpenAI with AWS Bedrock:
- Install AWS SDK packages
- Implement `AWSBedrockService : IAIService`
- Support Claude models for case analysis
- Streaming analysis support

**Files**:
- `BetterCallSaul.Infrastructure/Services/AI/AWSBedrockService.cs`
- Package references

### Task 2.2: AWS S3 Service
**ID**: AWS-005
**Effort**: 12 hours
**Dependencies**: AWS-001, AWS-003

Replace Azure Blob Storage with AWS S3:
- Install AWS S3 SDK
- Implement `AWSS3StorageService : IStorageService`
- File upload/download with presigned URLs
- Maintain existing API contracts

**Files**:
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AWSS3StorageService.cs`

### Task 2.3: AWS Textract Service
**ID**: AWS-006
**Effort**: 14 hours
**Dependencies**: AWS-001, AWS-003

Replace Azure Form Recognizer with AWS Textract:
- Install AWS Textract SDK
- Implement `AWSTextractService : IDocumentProcessingService`
- Text extraction with confidence normalization
- Async operation support

**Files**:
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AWSTextractService.cs`

### Task 2.4: Update Existing Azure Services
**ID**: AWS-007
**Effort**: 8 hours
**Dependencies**: AWS-001

Update existing Azure services to implement new interfaces:
- Modify `AzureOpenAIService`
- Modify blob storage service
- Modify Form Recognizer service

**Files**:
- `BetterCallSaul.Infrastructure/Services/AI/AzureOpenAIService.cs`
- Existing Azure service files

## Phase 3: Integration & Deployment (Week 4-5)

### Task 3.1: Integration Testing
**ID**: AWS-008
**Effort**: 12 hours
**Dependencies**: AWS-004, AWS-005, AWS-006

Comprehensive testing across both providers:
- API endpoint compatibility tests
- Performance comparison tests
- Provider switching tests

### Task 3.2: AWS Infrastructure Setup
**ID**: AWS-009
**Effort**: 16 hours
**Dependencies**: AWS-008

AWS production environment preparation:
- CloudFormation/CDK templates
- IAM roles and permissions
- Bedrock model access setup
- S3 bucket configuration
- Secrets Manager setup

### Task 3.3: Deployment Pipeline
**ID**: AWS-010
**Effort**: 8 hours
**Dependencies**: AWS-009

Update deployment processes:
- Environment-specific configurations
- AWS deployment scripts
- Health check endpoints

### Task 3.4: Production Validation
**ID**: AWS-011
**Effort**: 12 hours
**Dependencies**: AWS-010

End-to-end production testing:
- Deploy to AWS staging
- Full regression testing
- Performance validation
- Monitoring setup

### Task 3.5: Documentation Update
**ID**: AWS-012
**Effort**: 6 hours
**Dependencies**: AWS-011

Update project documentation:
- AWS deployment guide
- Configuration examples
- Troubleshooting guide

## Critical Path
**Total Critical Path: 52 hours (6.5 days)**

AWS-001 → AWS-002 → AWS-003 → AWS-004 → AWS-008 → AWS-009 → AWS-010 → AWS-011

## Parallel Work Opportunities

**Week 1**: AWS-001 → (AWS-002 + AWS-007 parallel) → AWS-003
**Week 2-3**: After AWS-003, all service implementations (AWS-004, AWS-005, AWS-006) can run in parallel
**Week 4**: AWS-008 → AWS-009 in parallel with AWS-012
**Week 5**: AWS-010 → AWS-011

## Resource Requirements

**Minimum Team**: 2 developers
**Optimal Team**: 2 developers + 1 DevOps engineer
**Timeline**: 4-5 weeks vs 8 weeks for multi-cloud

## Risk Mitigation

1. **AWS Service Learning Curve**: Allocate extra time for Bedrock/Textract integration
2. **Configuration Complexity**: Keep Azure as fallback during transition
3. **Performance Differences**: Establish benchmarks early
4. **Deployment Issues**: Set up staging environment first

## Success Criteria

**Week 1**: Service interfaces complete, configuration system working
**Week 2**: AWS Bedrock producing comparable results to Azure OpenAI
**Week 3**: All AWS services functional with existing frontend
**Week 4**: Integration tests passing, infrastructure templates ready
**Week 5**: AWS production deployment validated and operational

This simplified approach eliminates Google Cloud complexity while maintaining the core goal of AWS deployment readiness within a reasonable timeline.