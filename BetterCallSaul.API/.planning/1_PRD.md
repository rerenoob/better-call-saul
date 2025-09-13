# Product Requirements Document - File Processing Pipeline Fix

**Created:** 2025-09-13
**Version:** 1.0

## Overview

### Feature Summary
Fix the file processing pipeline in the Better Call Saul AI Lawyer application to ensure uploaded documents are properly processed through OCR (Optical Character Recognition) and AI case analysis workflows.

### Problem Statement
Production database analysis reveals that while the Cases table contains data, the Documents, DocumentText, and CaseAnalyses tables are completely empty. This indicates a critical failure in the file processing pipeline that prevents:
- Document metadata from being stored after upload
- OCR text extraction from processing uploaded files
- AI case analysis from running on extracted document content
- Case detail pages from displaying file-derived information to users

### Goals
1. **Restore Document Processing**: Ensure uploaded files create Document records in the database
2. **Fix OCR Pipeline**: Ensure Azure Form Recognizer extracts text and stores it in DocumentText table
3. **Enable AI Analysis**: Ensure case analysis runs on extracted text and stores results in CaseAnalyses table
4. **Improve Observability**: Add comprehensive logging and monitoring to prevent future failures
5. **User Experience**: Provide clear feedback on processing status and error handling

### Success Metrics
- Document upload creates corresponding Document records (100% success rate)
- OCR text extraction populates DocumentText table for supported file types (95% success rate)
- Case analysis completes and stores results in CaseAnalyses table (90% success rate)
- Case detail pages display extracted text and analysis results (100% for successful processes)
- Processing errors are logged and surfaced to users with actionable feedback

## Requirements

### Core Functional Requirements

#### FR1: Document Upload and Storage
- File upload creates Document entity with metadata (filename, size, content type)
- Files are securely stored in configured storage location
- Upload progress is tracked and reported to users
- Supported file types: PDF, DOC, DOCX, TXT, images (PNG, JPG)

#### FR2: OCR Text Extraction
- Azure Form Recognizer processes uploaded documents
- Extracted text is stored in DocumentText entity linked to Document
- OCR processing status is tracked (pending, processing, completed, failed)
- Extraction handles multi-page documents and various layouts

#### FR3: AI Case Analysis
- Background processing analyzes extracted text using Azure OpenAI
- Analysis results stored in CaseAnalysis entity with structured JSON data
- Analysis includes: viability scores, legal issues, recommendations, evidence evaluation
- Processing integrates with existing AI prompt templates

#### FR4: Error Handling and Recovery
- Failed OCR operations retry with exponential backoff
- Failed AI analysis operations can be manually reprocessed
- Processing errors are logged with detailed context
- Users receive meaningful error messages and guidance

#### FR5: Status Tracking and Feedback
- Real-time processing status updates via SignalR
- Processing progress indicators in UI
- Clear success/failure states with actionable messages
- Admin dashboard shows processing pipeline health

### Constraints
- Must work with existing Azure services (Form Recognizer, OpenAI, Storage)
- Must maintain backward compatibility with existing Case entities
- Processing must handle production file volumes efficiently
- Error recovery must not corrupt existing data

### Dependencies
- Azure Form Recognizer service availability and quotas
- Azure OpenAI service availability and token limits
- SignalR infrastructure for real-time updates
- Entity Framework database connectivity

## User Experience

### Basic User Flow
1. User uploads document via drag-and-drop or file picker
2. Upload progress bar shows file transfer status
3. Processing spinner indicates OCR and analysis are running
4. Real-time updates show processing milestones
5. Case detail page displays extracted text and analysis results
6. Error states provide clear guidance on resolution steps

### UI Considerations
- Processing status prominently displayed on case detail pages
- Upload area shows supported file types and size limits
- Error messages include specific actions users can take
- Processing history available for troubleshooting

## Acceptance Criteria

### AC1: Document Upload Success
- GIVEN a user uploads a supported file type
- WHEN the upload completes successfully
- THEN a Document record is created with correct metadata
- AND the file is stored in the configured storage location

### AC2: OCR Processing Success
- GIVEN a Document record exists for an uploaded file
- WHEN OCR processing is triggered
- THEN Azure Form Recognizer extracts text content
- AND a DocumentText record is created with extracted content
- AND processing status is updated appropriately

### AC3: AI Analysis Success
- GIVEN extracted text exists in DocumentText
- WHEN background analysis is triggered
- THEN Azure OpenAI analyzes the content
- AND a CaseAnalysis record is created with structured results
- AND real-time updates notify the user of completion

### AC4: Error Recovery
- GIVEN OCR or AI analysis fails
- WHEN the failure is detected
- THEN the error is logged with detailed context
- AND users receive meaningful error messages
- AND manual reprocessing options are available

### AC5: Case Detail Integration
- GIVEN successful document processing
- WHEN users view the case detail page
- THEN extracted text is displayed in the documents section
- AND AI analysis results are shown with proper formatting
- AND processing status is clearly indicated

## Open Questions

⚠️ **Critical Questions Needing Resolution:**

1. **Azure Service Configurations**: Are Azure Form Recognizer and OpenAI service credentials properly configured in production environment?

2. **Background Processing**: Is the current background processing infrastructure (e.g., Hangfire, Azure Functions, or in-process tasks) properly configured and running?

3. **Storage Configuration**: Is the file storage configuration (local filesystem vs Azure Blob Storage) correctly set up for the production environment?

4. **Database Permissions**: Does the application have proper database permissions to create records in Documents, DocumentText, and CaseAnalyses tables?

5. **Service Quotas**: Are there Azure service quota limits being hit that would prevent OCR or AI processing?

6. **Network Connectivity**: Can the production environment reach Azure services for OCR and AI processing?

7. **Mock vs Production Services**: Are mock services accidentally running in production instead of real Azure integrations?

## Next Steps
1. Review architecture decisions document for technical approach
2. Validate Azure service configurations in production
3. Implement comprehensive logging and monitoring
4. Execute implementation tasks in priority order
5. Establish testing strategy for validation
