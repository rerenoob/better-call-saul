# Implementation Breakdown: Cloud-Agnostic Migration
*Created: 2025-09-14*

## Phase 1: Foundation and Abstractions (Week 1-2)

### Task 1.1: Create Service Abstraction Interfaces
**ID**: IMPL-001
**Complexity**: Medium
**Dependencies**: None
**Estimated Effort**: 16 hours

**Description**: Design and implement core service interfaces that abstract cloud provider functionality.

**Detailed Steps**:
- Define `IAIService` interface with methods for case analysis, document summarization, and outcome prediction
- Define `IStorageService` interface with methods for upload, download, delete, and SAS token generation
- Define `IDocumentProcessingService` interface for text extraction and document analysis
- Create common response models (`AIResponse`, `StorageResult`, `TextExtractionResult`)
- Update existing Azure services to implement these interfaces
- Add comprehensive XML documentation for all interfaces

**Acceptance Criteria**:
- [ ] All interfaces compile and are well-documented
- [ ] Existing Azure services implement new interfaces without functionality loss
- [ ] Unit tests pass for interface contract validation
- [ ] Code review approved by team lead

**Files Modified**:
- `BetterCallSaul.Core/Interfaces/Services/IAIService.cs` (new)
- `BetterCallSaul.Core/Interfaces/Services/IStorageService.cs` (new)
- `BetterCallSaul.Core/Interfaces/Services/IDocumentProcessingService.cs` (new)
- `BetterCallSaul.Core/Models/ServiceResponses/*.cs` (new)
- `BetterCallSaul.Infrastructure/Services/AI/AzureOpenAIService.cs` (modified)

### Task 1.2: Configuration Management System
**ID**: IMPL-002
**Complexity**: Medium
**Dependencies**: IMPL-001
**Estimated Effort**: 12 hours

**Description**: Implement cloud provider configuration management with environment variable override support.

**Detailed Steps**:
- Create `CloudProviderOptions` configuration class
- Implement provider-specific option classes (Azure, AWS, Google Cloud)
- Add configuration validation and error handling
- Implement environment variable override mechanism
- Create configuration health checks for startup validation
- Add configuration service for runtime provider selection

**Acceptance Criteria**:
- [ ] Configuration loads correctly from appsettings.json and environment variables
- [ ] Invalid configurations fail fast with clear error messages
- [ ] Provider switching works through configuration changes
- [ ] All configuration values are validated on startup
- [ ] Health checks pass for active provider configuration

**Files Modified**:
- `BetterCallSaul.Core/Configuration/CloudProviderOptions.cs` (new)
- `BetterCallSaul.Core/Configuration/ProviderSettings/*.cs` (new)
- `BetterCallSaul.API/Program.cs` (modified)
- `appsettings.json` and `appsettings.Production.json` (modified)

### Task 1.3: Service Factory Implementation
**ID**: IMPL-003
**Complexity**: High
**Dependencies**: IMPL-001, IMPL-002
**Estimated Effort**: 20 hours

**Description**: Create service factories that instantiate appropriate provider implementations based on configuration.

**Detailed Steps**:
- Implement `ServiceProviderFactory` for dependency injection registration
- Create provider-specific service factories (Azure, AWS, Google Cloud)
- Add factory registration in Program.cs with configuration-driven selection
- Implement health check system for all registered services
- Add graceful degradation mechanisms for service unavailability
- Create service discovery and registration system

**Acceptance Criteria**:
- [ ] Correct service implementations are injected based on configuration
- [ ] Factory supports runtime provider switching scenarios
- [ ] Health checks validate service availability and configuration
- [ ] Graceful fallback mechanisms work when services are unavailable
- [ ] Dependency injection container resolves all services correctly

**Files Modified**:
- `BetterCallSaul.Infrastructure/Factories/ServiceProviderFactory.cs` (new)
- `BetterCallSaul.Infrastructure/Extensions/ServiceCollectionExtensions.cs` (new)
- `BetterCallSaul.API/Program.cs` (modified)

## Phase 2: AWS Provider Implementation (Week 3-4)

### Task 2.1: AWS Bedrock AI Service Implementation
**ID**: IMPL-004
**Complexity**: High
**Dependencies**: IMPL-001, IMPL-003
**Estimated Effort**: 24 hours

**Description**: Implement AWS Bedrock service adapter for AI functionality with Claude and other foundation models.

**Detailed Steps**:
- Add AWS SDK dependencies (AWS.BedrockRuntime, AWSSDK.BedrockRuntime)
- Implement `AWSBedrockService` class implementing `IAIService`
- Map prompts to Bedrock-compatible formats for different models (Claude, Jurassic)
- Implement streaming support for real-time analysis
- Add AWS-specific configuration options and validation
- Create response normalization from Bedrock to standard `AIResponse`
- Implement proper error handling and retry logic

**Acceptance Criteria**:
- [ ] All `IAIService` methods work with AWS Bedrock
- [ ] Streaming analysis provides real-time responses
- [ ] Response format matches existing Azure implementation
- [ ] Error handling provides meaningful messages
- [ ] Performance is within 10% of Azure implementation
- [ ] Integration tests pass with real AWS Bedrock service

**Files Modified**:
- `BetterCallSaul.Infrastructure/Services/AI/AWSBedrockService.cs` (new)
- `BetterCallSaul.Core/Configuration/AWSBedrockOptions.cs` (new)
- Package references in project files

### Task 2.2: AWS S3 Storage Service Implementation
**ID**: IMPL-005
**Complexity**: Medium
**Dependencies**: IMPL-001, IMPL-003
**Estimated Effort**: 18 hours

**Description**: Implement AWS S3 service adapter for file storage functionality with presigned URL support.

**Detailed Steps**:
- Add AWS S3 SDK dependencies (AWSSDK.S3)
- Implement `AWSS3StorageService` class implementing `IStorageService`
- Add file upload/download functionality with multipart upload support
- Implement presigned URL generation for secure file access
- Add S3-specific configuration options (bucket names, regions, access policies)
- Create response normalization to standard `StorageResult`
- Implement proper error handling and retry logic with S3-specific errors

**Acceptance Criteria**:
- [ ] All `IStorageService` methods work with AWS S3
- [ ] File uploads/downloads maintain data integrity
- [ ] Presigned URLs work for secure file access
- [ ] Error handling covers S3-specific scenarios
- [ ] Performance matches current Azure Blob Storage implementation
- [ ] Integration tests pass with real S3 service

**Files Modified**:
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AWSS3StorageService.cs` (new)
- `BetterCallSaul.Core/Configuration/AWSS3Options.cs` (new)

### Task 2.3: AWS Textract Document Processing Implementation
**ID**: IMPL-006
**Complexity**: High
**Dependencies**: IMPL-001, IMPL-003
**Estimated Effort**: 22 hours

**Description**: Implement AWS Textract service adapter for document text extraction functionality.

**Detailed Steps**:
- Add AWS Textract SDK dependencies (AWSSDK.Textract)
- Implement `AWSTextractService` class implementing `IDocumentProcessingService`
- Add support for both synchronous and asynchronous document analysis
- Implement confidence score normalization to standard 0-1 scale
- Add support for different document types (PDF, images, forms)
- Create response normalization to standard `TextExtractionResult`
- Implement proper error handling and polling for async operations

**Acceptance Criteria**:
- [ ] All `IDocumentProcessingService` methods work with AWS Textract
- [ ] Text extraction accuracy matches Azure Form Recognizer
- [ ] Confidence scores are properly normalized
- [ ] Both sync and async processing work correctly
- [ ] Performance is comparable to current Azure implementation
- [ ] Integration tests pass with real Textract service

**Files Modified**:
- `BetterCallSaul.Infrastructure/Services/FileProcessing/AWSTextractService.cs` (new)
- `BetterCallSaul.Core/Configuration/AWSTextractOptions.cs` (new)

## Phase 3: Google Cloud Provider Implementation (Week 5-6)

### Task 3.1: Google Vertex AI Service Implementation
**ID**: IMPL-007
**Complexity**: High
**Dependencies**: IMPL-001, IMPL-003
**Estimated Effort**: 24 hours

**Description**: Implement Google Vertex AI service adapter for AI functionality with PaLM and Gemini models.

**Detailed Steps**:
- Add Google Cloud AI Platform SDK dependencies
- Implement `GoogleVertexAIService` class implementing `IAIService`
- Support both PaLM and Gemini model families
- Implement streaming support for real-time analysis
- Add Google Cloud-specific authentication and configuration
- Create response normalization from Vertex AI to standard `AIResponse`
- Implement proper error handling and retry logic

**Acceptance Criteria**:
- [ ] All `IAIService` methods work with Google Vertex AI
- [ ] Streaming analysis provides real-time responses
- [ ] Response format matches existing implementations
- [ ] Authentication works with service accounts and ADC
- [ ] Performance is within acceptable range
- [ ] Integration tests pass with real Vertex AI service

**Files Modified**:
- `BetterCallSaul.Infrastructure/Services/AI/GoogleVertexAIService.cs` (new)
- `BetterCallSaul.Core/Configuration/GoogleVertexAIOptions.cs` (new)

### Task 3.2: Google Cloud Storage Service Implementation
**ID**: IMPL-008
**Complexity**: Medium
**Dependencies**: IMPL-001, IMPL-003
**Estimated Effort**: 16 hours

**Description**: Implement Google Cloud Storage service adapter for file storage functionality.

**Detailed Steps**:
- Add Google Cloud Storage SDK dependencies
- Implement `GoogleCloudStorageService` class implementing `IStorageService`
- Add file upload/download functionality with resumable uploads
- Implement signed URL generation for secure file access
- Add Google Cloud-specific configuration options
- Create response normalization to standard `StorageResult`
- Implement proper error handling and retry logic

**Acceptance Criteria**:
- [ ] All `IStorageService` methods work with Google Cloud Storage
- [ ] File uploads/downloads maintain data integrity
- [ ] Signed URLs work for secure file access
- [ ] Error handling covers GCS-specific scenarios
- [ ] Performance matches other storage implementations
- [ ] Integration tests pass with real GCS service

**Files Modified**:
- `BetterCallSaul.Infrastructure/Services/FileProcessing/GoogleCloudStorageService.cs` (new)
- `BetterCallSaul.Core/Configuration/GoogleCloudStorageOptions.cs` (new)

### Task 3.3: Google Document AI Service Implementation
**ID**: IMPL-009
**Complexity**: High
**Dependencies**: IMPL-001, IMPL-003
**Estimated Effort**: 20 hours

**Description**: Implement Google Document AI service adapter for document text extraction functionality.

**Detailed Steps**:
- Add Google Cloud Document AI SDK dependencies
- Implement `GoogleDocumentAIService` class implementing `IDocumentProcessingService`
- Support different processor types for various document formats
- Implement confidence score normalization to standard 0-1 scale
- Add support for batch and online processing
- Create response normalization to standard `TextExtractionResult`
- Implement proper error handling and long-running operation support

**Acceptance Criteria**:
- [ ] All `IDocumentProcessingService` methods work with Document AI
- [ ] Text extraction accuracy is acceptable
- [ ] Confidence scores are properly normalized
- [ ] Both online and batch processing work correctly
- [ ] Performance is comparable to other implementations
- [ ] Integration tests pass with real Document AI service

**Files Modified**:
- `BetterCallSaul.Infrastructure/Services/FileProcessing/GoogleDocumentAIService.cs` (new)
- `BetterCallSaul.Core/Configuration/GoogleDocumentAIOptions.cs` (new)

## Phase 4: Integration and Testing (Week 7)

### Task 4.1: End-to-End Integration Testing
**ID**: IMPL-010
**Complexity**: High
**Dependencies**: IMPL-004, IMPL-005, IMPL-006, IMPL-007, IMPL-008, IMPL-009
**Estimated Effort**: 20 hours

**Description**: Create comprehensive integration tests that validate functionality across all cloud providers.

**Detailed Steps**:
- Create integration test project with provider-switching capabilities
- Implement test scenarios for case analysis across all AI providers
- Create file storage tests for all storage providers
- Implement document processing tests for all text extraction services
- Add performance benchmarking tests comparing providers
- Create provider failover and fallback scenario tests
- Implement configuration validation tests

**Acceptance Criteria**:
- [ ] All integration tests pass for each provider independently
- [ ] Performance benchmarks show acceptable variance between providers
- [ ] Provider switching works correctly in test scenarios
- [ ] Configuration validation catches invalid setups
- [ ] Test coverage exceeds 80% for all new provider implementations

### Task 4.2: AWS Deployment Template Creation
**ID**: IMPL-011
**Complexity**: Medium
**Dependencies**: IMPL-004, IMPL-005, IMPL-006
**Estimated Effort**: 16 hours

**Description**: Create AWS-specific deployment templates and infrastructure as code.

**Detailed Steps**:
- Create AWS CloudFormation or CDK templates for infrastructure
- Configure AWS services (Bedrock access, S3 buckets, Textract permissions)
- Create AWS-specific environment variable configurations
- Implement AWS secrets management integration
- Create deployment scripts and documentation
- Add AWS health checks and monitoring configuration

**Acceptance Criteria**:
- [ ] AWS infrastructure deploys successfully via templates
- [ ] All AWS services are properly configured and accessible
- [ ] Environment variables are correctly set for AWS deployment
- [ ] Secrets are managed securely through AWS Secrets Manager
- [ ] Health checks validate all AWS services are operational

## Phase 5: Documentation and Deployment (Week 8)

### Task 5.1: Comprehensive Documentation Update
**ID**: IMPL-012
**Complexity**: Low
**Dependencies**: All previous tasks
**Estimated Effort**: 12 hours

**Description**: Update all documentation to reflect cloud-agnostic architecture and deployment options.

**Detailed Steps**:
- Update README.md with multi-cloud deployment instructions
- Create provider-specific configuration guides
- Update CLAUDE.md with new architecture information
- Create troubleshooting guides for each provider
- Document performance benchmarks and recommendations
- Create migration guide from Azure-only to cloud-agnostic

**Acceptance Criteria**:
- [ ] Documentation is complete and accurate for all providers
- [ ] Configuration examples are provided for each cloud platform
- [ ] Troubleshooting guides help resolve common issues
- [ ] Migration guide enables smooth transition from Azure-only setup

### Task 5.2: Production Deployment Validation
**ID**: IMPL-013
**Complexity**: Medium
**Dependencies**: IMPL-011, IMPL-012
**Estimated Effort**: 16 hours

**Description**: Validate production deployment on AWS and ensure feature parity with existing Azure deployment.

**Detailed Steps**:
- Deploy application to AWS production environment
- Execute full regression test suite on AWS deployment
- Validate performance metrics against Azure benchmarks
- Test provider switching scenarios in production
- Validate monitoring and alerting systems
- Create rollback procedures and disaster recovery plans

**Acceptance Criteria**:
- [ ] AWS production deployment is successful and stable
- [ ] All features work identically to Azure deployment
- [ ] Performance metrics are within acceptable range
- [ ] Monitoring and alerting systems are operational
- [ ] Rollback procedures are tested and documented

## Critical Path Analysis

**Critical Path**: IMPL-001 → IMPL-002 → IMPL-003 → IMPL-004 → IMPL-005 → IMPL-006 → IMPL-010 → IMPL-011 → IMPL-013

**Parallel Work Opportunities**:
- Tasks 2.1, 2.2, 2.3 can be developed in parallel after Phase 1 completion
- Tasks 3.1, 3.2, 3.3 can be developed in parallel with Phase 2
- Task 5.1 documentation can begin after Phase 3 completion
- Task 4.1 testing can begin as each provider is completed

**Total Estimated Effort**: 206 hours (approximately 5-6 weeks with parallel work)

## Dependencies and Blockers

### External Dependencies
- AWS account setup with Bedrock, S3, and Textract access
- Google Cloud project setup with appropriate API enablement
- Service account and IAM role configuration for all providers
- Secrets management system setup for each cloud platform

### Internal Dependencies
- Database schema may need updates for provider-specific metadata
- Frontend configuration updates for provider-specific capabilities
- DevOps pipeline updates for multi-provider deployment
- Testing infrastructure updates for provider validation

## Next Steps
1. Begin Phase 1 implementation with service abstraction interfaces
2. Set up AWS and Google Cloud development accounts
3. Configure CI/CD pipeline for multi-provider testing
4. Establish monitoring and logging strategy for cloud-agnostic deployment