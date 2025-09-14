# Azure Removal - Integration Checkpoints & Validation

**Created:** 2025-09-14
**Version:** 1.0

## Overview

This document defines the integration checkpoints and validation procedures for the Azure removal project. Each checkpoint represents a critical milestone where system stability and functionality must be verified before proceeding to the next phase.

## Checkpoint Schedule

| Checkpoint | Phase | Tasks Completed | Validation Time | Rollback Risk |
|------------|-------|-----------------|-----------------|---------------|
| **CP1** | Foundation | Service Architecture (2 tasks) | 30 min | Low |
| **CP2** | Azure Removal | Code & Config Removal (3 tasks) | 45 min | High |
| **CP3** | Service Implementation | Mock Services & Config (4 tasks) | 60 min | Medium |
| **CP4** | Quality Assurance | Testing & Documentation (5 tasks) | 90 min | Low |

## Checkpoint 1: Service Architecture Foundation

### Trigger
- Completed: `01-service-architecture/001-di-container-refactor.md`
- Completed: `01-service-architecture/002-environment-service-validation.md`

### Validation Procedures

#### 1.1 Build Verification
```bash
# Verify application builds successfully
cd BetterCallSaul.API
dotnet build --configuration Debug
dotnet build --configuration Release
```
**Success Criteria:** Zero build errors or warnings

#### 1.2 Service Resolution Validation
```bash
# Test development environment service resolution
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --urls="https://localhost:7191"
# Check logs for mock service registration messages

# Test production environment service resolution
export ASPNETCORE_ENVIRONMENT=Production
export AWS_ACCESS_KEY_ID=mock-key
export AWS_SECRET_ACCESS_KEY=mock-secret
dotnet run --urls="https://localhost:7191"
# Check logs for AWS service registration messages
```
**Success Criteria:**
- Development registers mock services
- Production registers AWS services
- Application starts without errors

#### 1.3 Integration Test Execution
```bash
# Run service registration tests
cd BetterCallSaul.Tests
dotnet test --filter "Category=ServiceRegistration"
```
**Success Criteria:** All service registration tests pass

### Quality Gates
- [ ] Application builds successfully in both Debug and Release
- [ ] Service resolution works correctly in both environments
- [ ] Existing functionality remains operational
- [ ] No performance regression observed
- [ ] Integration tests validate new service registration pattern

### Rollback Trigger
If any quality gate fails, rollback DI container changes and investigate issues before proceeding.

---

## Checkpoint 2: Azure Code Elimination

### Trigger
- Completed: `02-azure-code-removal/001-azure-service-implementations.md`
- Completed: `02-azure-code-removal/002-azure-configuration-classes.md`
- Completed: `02-azure-code-removal/003-nuget-package-cleanup.md`

### Validation Procedures

#### 2.1 Code Reference Validation
```bash
# Search for any remaining Azure references
cd /home/duong/Projects/better-call-saul
grep -r "Azure" --include="*.cs" BetterCallSaul.Core/ BetterCallSaul.Infrastructure/ BetterCallSaul.API/
grep -r "azure" --include="*.cs" BetterCallSaul.Core/ BetterCallSaul.Infrastructure/ BetterCallSaul.API/
```
**Success Criteria:** Zero Azure references found in code

#### 2.2 Build and Package Validation
```bash
# Verify build succeeds without Azure packages
cd BetterCallSaul.Infrastructure
dotnet restore
dotnet build
dotnet list package | grep -i azure
```
**Success Criteria:**
- Build succeeds
- No Azure packages in dependency list

#### 2.3 Application Startup Validation
```bash
# Test application startup without Azure dependencies
cd BetterCallSaul.API
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --urls="https://localhost:7191"
# Verify startup logs show no Azure configuration attempts
```
**Success Criteria:** Application starts successfully without Azure references

### Quality Gates
- [ ] No Azure code references remain in source files
- [ ] Application builds successfully without Azure packages
- [ ] Application starts without Azure configuration errors
- [ ] Git history preserves deleted files for potential recovery
- [ ] All existing tests still pass (excluding deleted Azure tests)

### Rollback Trigger
**HIGH RISK CHECKPOINT** - If critical functionality is broken, immediately restore Azure files and packages from git history.

---

## Checkpoint 3: Mock Services & Configuration

### Trigger
- Completed: `03-mock-services/001-mock-ai-service.md`
- Completed: `03-mock-services/002-local-file-storage-service.md`
- Completed: `04-configuration/001-appsettings-cleanup.md`
- Completed: `04-configuration/002-aws-configuration-validation.md`

### Validation Procedures

#### 3.1 Mock Service Functionality
```bash
# Test mock services in development environment
cd BetterCallSaul.API
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --urls="https://localhost:7191"

# Test API endpoints with mock services
curl -X POST http://localhost:7191/api/cases -H "Content-Type: application/json" -d "{...}"
# Verify mock AI service responses
```
**Success Criteria:** Mock services provide realistic responses

#### 3.2 Configuration Validation
```bash
# Verify development environment needs no cloud config
cd BetterCallSaul.API
unset AWS_ACCESS_KEY_ID
unset AWS_SECRET_ACCESS_KEY
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --urls="https://localhost:7191"
```
**Success Criteria:** Application starts without cloud configuration

#### 3.3 Production Configuration Validation
```bash
# Test production environment with AWS config
export ASPNETCORE_ENVIRONMENT=Production
export AWS_ACCESS_KEY_ID=test-key
export AWS_SECRET_ACCESS_KEY=test-secret
dotnet run --urls="https://localhost:7191"
# Should start successfully with AWS service registration
```
**Success Criteria:** Production environment validates AWS configuration

### Quality Gates
- [ ] Mock services provide realistic, consistent responses
- [ ] Local file storage operations work correctly
- [ ] Development environment requires no cloud configuration
- [ ] Production environment validates AWS configuration properly
- [ ] All API endpoints continue to function correctly

### Rollback Trigger
If mock services don't provide adequate functionality or configuration issues prevent startup, investigate and fix before proceeding.

---

## Checkpoint 4: Quality Assurance Complete

### Trigger
- Completed: `05-testing-validation/001-azure-test-removal.md`
- Completed: `05-testing-validation/002-mock-service-tests.md`
- Completed: `05-testing-validation/003-environment-integration-tests.md`
- Completed: `05-testing-validation/004-documentation-updates.md`
- Completed: `05-testing-validation/005-final-system-validation.md`

### Validation Procedures

#### 4.1 Complete Test Suite Execution
```bash
# Run full test suite
cd BetterCallSaul.Tests
dotnet test --configuration Release --logger "console;verbosity=detailed"
```
**Success Criteria:** All tests pass, test coverage maintained

#### 4.2 End-to-End Functionality Validation
```bash
# Test complete user workflows
cd better-call-saul-frontend
npm run dev &
cd ../BetterCallSaul.API
dotnet run --urls="https://localhost:7191" &

# Manual testing of key workflows:
# 1. User registration and login
# 2. Case creation and file upload
# 3. AI analysis execution
# 4. Document text extraction
```
**Success Criteria:** All major user workflows function correctly

#### 4.3 Performance Baseline Validation
```bash
# Performance testing (basic)
cd BetterCallSaul.Tests
dotnet test --filter "Category=Performance"
```
**Success Criteria:** Performance within 10% of baseline

#### 4.4 Documentation Validation
```bash
# Follow setup instructions exactly as documented
# Verify all commands work as described
```
**Success Criteria:** Documentation is accurate and complete

### Quality Gates
- [ ] All automated tests pass consistently
- [ ] Manual testing validates all major functionality
- [ ] Performance meets baseline requirements
- [ ] Documentation is complete and accurate
- [ ] System ready for production deployment

### Rollback Trigger
If critical functionality is broken or performance is severely degraded, complete rollback may be necessary.

## Validation Commands Quick Reference

### Development Environment Test
```bash
cd BetterCallSaul.API
unset AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY CLOUD_PROVIDER
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --urls="https://localhost:7191"
# Should start with mock services, no cloud config needed
```

### Production Environment Test
```bash
cd BetterCallSaul.API
export ASPNETCORE_ENVIRONMENT=Production
export AWS_ACCESS_KEY_ID=mock-key
export AWS_SECRET_ACCESS_KEY=mock-secret
export AWS_REGION=us-east-1
dotnet run --urls="https://localhost:7191"
# Should start with AWS services, validate configuration
```

### Build Validation
```bash
dotnet clean && dotnet restore && dotnet build --configuration Release
dotnet test --configuration Release
```

### Code Quality Check
```bash
# Check for Azure references
grep -r -i "azure" --include="*.cs" BetterCallSaul.Core/ BetterCallSaul.Infrastructure/ BetterCallSaul.API/

# Check package dependencies
dotnet list package | grep -i azure
```

## Emergency Rollback Procedures

### Immediate Rollback (Any Checkpoint)
```bash
# Rollback to last known good state
git stash
git reset --hard HEAD~n  # where n is number of commits to rollback
dotnet restore
dotnet build
```

### Partial Rollback (Restore specific components)
```bash
# Restore specific deleted files
git checkout HEAD~n -- path/to/deleted/file.cs
dotnet restore
dotnet build
```

## Success Metrics Tracking

### Checkpoint Metrics
- **CP1**: Service registration simplification validated
- **CP2**: Azure code elimination confirmed (0 references)
- **CP3**: Mock services operational, config simplified
- **CP4**: Full system validation passed

### Project Metrics
- **Code Complexity**: Reduced service registration complexity
- **Dependencies**: 3 Azure packages removed
- **Configuration**: Simplified appsettings structure
- **Test Coverage**: Maintained or improved
- **Performance**: Within 10% of baseline

## Notes
- Each checkpoint includes time estimates for validation procedures
- Rollback procedures are clearly defined for each risk level
- Quality gates must be met before proceeding to next phase
- All validation commands are tested and ready for execution