# Manual Test Plan - Better Call Saul AI Lawyer Application

## Overview
This document outlines the comprehensive manual testing strategy for the Better Call Saul AI Lawyer application. The testing covers all user workflows, error handling, and edge cases.

## Test Environment
- **Application**: Better Call Saul Blazor Server App
- **Database**: SQLite (BetterCallSaul.db)
- **File Storage**: Local file system (wwwroot/uploads/)
- **AI Integration**: Azure OpenAI (if configured)

## User Authentication

### Registration Flow
- [ ] **TC-AUTH-001**: User registration with valid email and password
  - Steps: Navigate to Register page, fill valid details, submit
  - Expected: User created successfully, redirected to home page

- [ ] **TC-AUTH-002**: User registration with invalid email format
  - Steps: Enter invalid email (e.g., "invalid-email"), submit
  - Expected: Validation error displayed, registration prevented

- [ ] **TC-AUTH-003**: User registration with weak password
  - Steps: Enter password that doesn't meet complexity requirements
  - Expected: Validation error displayed, registration prevented

- [ ] **TC-AUTH-004**: User registration with mismatched passwords
  - Steps: Enter different values in Password and Confirm Password fields
  - Expected: Validation error displayed, registration prevented

### Login Flow
- [ ] **TC-AUTH-005**: User login with correct credentials
  - Steps: Navigate to Login page, enter valid credentials, submit
  - Expected: Successful login, redirected to home/dashboard

- [ ] **TC-AUTH-006**: User login with incorrect password
  - Steps: Enter valid email but wrong password, submit
  - Expected: Error message displayed, login prevented

- [ ] **TC-AUTH-007**: User login with non-existent email
  - Steps: Enter email not in system, submit
  - Expected: Error message displayed, login prevented

- [ ] **TC-AUTH-008**: Login page accessibility without authentication
  - Steps: Access login page while not authenticated
  - Expected: Page loads successfully

### Logout Flow
- [ ] **TC-AUTH-009**: User logout functionality
  - Steps: Click logout button while authenticated
  - Expected: User logged out, redirected to login page

### Password Reset Flow
- [ ] **TC-AUTH-010**: Password reset request with valid email
  - Steps: Navigate to Forgot Password, enter registered email, submit
  - Expected: Success message displayed (email sent)

- [ ] **TC-AUTH-011**: Password reset request with invalid email
  - Steps: Enter non-existent email in Forgot Password
  - Expected: Success message displayed (for security reasons)

- [ ] **TC-AUTH-012**: Password reset with valid token
  - Steps: Follow reset link from email, enter new password, submit
  - Expected: Password updated successfully, can login with new password

- [ ] **TC-AUTH-013**: Password reset with expired/invalid token
  - Steps: Use expired reset link, attempt to set new password
  - Expected: Error message displayed, reset prevented

## Document Processing

### File Upload Validation
- [ ] **TC-DOC-001**: Upload valid PDF document (<10MB)
  - Steps: Select valid PDF file, upload
  - Expected: File accepted, processing begins

- [ ] **TC-DOC-002**: Upload valid DOCX document (<10MB)
  - Steps: Select valid DOCX file, upload
  - Expected: File accepted, processing begins

- [ ] **TC-DOC-003**: Upload invalid file type (e.g., .txt, .jpg)
  - Steps: Select invalid file type, attempt upload
  - Expected: Error message displayed, upload rejected

- [ ] **TC-DOC-004**: Upload oversized file (>10MB)
  - Steps: Select large file, attempt upload
  - Expected: Error message displayed, upload rejected

- [ ] **TC-DOC-005**: Upload without selecting file
  - Steps: Click upload without file selection
  - Expected: Error message displayed, upload prevented

### Text Extraction
- [ ] **TC-DOC-006**: Text extraction from PDF with readable text
  - Steps: Upload PDF with selectable text
  - Expected: Text extracted successfully, displayed in interface

- [ ] **TC-DOC-007**: Text extraction from scanned PDF (image-based)
  - Steps: Upload image-based PDF
  - Expected: Appropriate error or handling for non-text content

- [ ] **TC-DOC-008**: Text extraction from DOCX with formatting
  - Steps: Upload DOCX with complex formatting
  - Expected: Text extracted successfully, formatting handled appropriately

### Error Handling
- [ ] **TC-DOC-009**: File upload with network interruption
  - Steps: Start upload, simulate network failure
  - Expected: Graceful error handling, user notified

- [ ] **TC-DOC-010**: File processing timeout
  - Steps: Upload very complex document
  - Expected: Timeout handling, user notified appropriately

## Case Management

### Case Creation
- [ ] **TC-CASE-001**: Create new case with valid data
  - Steps: Navigate to Create Case, fill required fields, submit
  - Expected: Case created successfully, redirected to case list

- [ ] **TC-CASE-002**: Create case with missing required fields
  - Steps: Attempt to create case without title/description
  - Expected: Validation errors displayed, creation prevented

- [ ] **TC-CASE-003**: Create case with very long text
  - Steps: Enter extremely long title/description
  - Expected: Appropriate validation/truncation handling

### Case Viewing
- [ ] **TC-CASE-004**: View case list (authenticated user)
  - Steps: Navigate to Cases page while authenticated
  - Expected: List of user's cases displayed

- [ ] **TC-CASE-005**: View case details
  - Steps: Click on case from list
  - Expected: Case details page loads with all information

- [ ] **TC-CASE-006**: Access case belonging to another user
  - Steps: Attempt to access case ID not owned by current user
  - Expected: Access denied/not found error

### Case Editing
- [ ] **TC-CASE-007**: Edit existing case
  - Steps: Open case, click edit, modify fields, save
  - Expected: Changes saved successfully

- [ ] **TC-CASE-008**: Edit case with invalid data
  - Steps: Attempt to save invalid changes
  - Expected: Validation errors, changes not saved

### Case Deletion
- [ ] **TC-CASE-009**: Delete case
  - Steps: Open case, click delete, confirm
  - Expected: Case deleted, removed from list

- [ ] **TC-CASE-010**: Delete confirmation dialog
  - Steps: Click delete, verify confirmation appears
  - Expected: Confirmation dialog prevents accidental deletion

## AI Analysis Integration

### Analysis Workflow
- [ ] **TC-AI-001**: Document analysis initiation
  - Steps: Upload document, initiate analysis
  - Expected: Analysis process starts, status updates

- [ ] **TC-AI-002**: Analysis results display
  - Steps: Wait for analysis completion, view results
  - Expected: Analysis results displayed in readable format

- [ ] **TC-AI-003**: Analysis error handling
  - Steps: Simulate AI service failure during analysis
  - Expected: Graceful error handling, user notified

### Analysis Results
- [ ] **TC-AI-004**: Legal issue identification
  - Steps: Upload legal document, check analysis
  - Expected: Legal issues correctly identified

- [ ] **TC-AI-005**: Risk assessment display
  - Steps: View analysis results for risk assessment
  - Expected: Risk levels clearly indicated

- [ ] **TC-AI-006**: Recommendation generation
  - Steps: Check analysis for actionable recommendations
  - Expected: Clear, actionable recommendations provided

## Dashboard & Navigation

### Dashboard Functionality
- [ ] **TC-DASH-001**: Dashboard load for authenticated user
  - Steps: Login, navigate to dashboard
  - Expected: Dashboard loads with user statistics

- [ ] **TC-DASH-002**: Dashboard statistics accuracy
  - Steps: Create cases/documents, verify dashboard counts
  - Expected: Statistics accurately reflect user data

### Navigation
- [ ] **TC-NAV-001**: Navigation menu functionality
  - Steps: Click all navigation items
  - Expected: Correct pages load, navigation works smoothly

- [ ] **TC-NAV-002**: Breadcrumb navigation
  - Steps: Navigate through multiple levels
  - Expected: Breadcrumbs accurately reflect location

- [ ] **TC-NAV-003**: Mobile navigation responsiveness
  - Steps: Test on mobile device/simulator
  - Expected: Navigation adapts to mobile view

## Responsive Design

### Desktop View
- [ ] **TC-RESP-001**: Desktop layout validation
  - Steps: Test on desktop browser (1200px+)
  - Expected: Proper desktop layout, all elements visible

### Tablet View
- [ ] **TC-RESP-002**: Tablet layout validation (768px-1199px)
  - Steps: Test on tablet-sized viewport
  - Expected: Responsive adaptation, readable layout

### Mobile View
- [ ] **TC-RESP-003**: Mobile layout validation (<768px)
  - Steps: Test on mobile-sized viewport
  - Expected: Mobile-optimized layout, touch-friendly

### Cross-Browser Testing
- [ ] **TC-BROWSER-001**: Chrome compatibility
  - Steps: Test all functionality in Chrome
  - Expected: All features work correctly

- [ ] **TC-BROWSER-002**: Firefox compatibility
  - Steps: Test all functionality in Firefox
  - Expected: All features work correctly

- [ ] **TC-BROWSER-003**: Safari compatibility
  - Steps: Test all functionality in Safari
  - Expected: All features work correctly

- [ ] **TC-BROWSER-004**: Edge compatibility
  - Steps: Test all functionality in Edge
  - Expected: All features work correctly

## Performance Testing

### Load Times
- [ ] **TC-PERF-001**: Page load performance
  - Steps: Measure load times for key pages
  - Expected: Pages load within acceptable time (<3s)

- [ ] **TC-PERF-002**: Document processing performance
  - Steps: Upload documents, measure processing time
  - Expected: Processing completes within reasonable time

### Concurrent Usage
- [ ] **TC-PERF-003**: Multiple simultaneous users
  - Steps: Simulate multiple users accessing simultaneously
  - Expected: System handles concurrent access gracefully

## Error & Edge Cases

### Network Issues
- [ ] **TC-ERROR-001**: Offline mode handling
  - Steps: Disconnect network, attempt operations
  - Expected: Graceful error messages, no crashes

- [ ] **TC-ERROR-002**: Slow network performance
  - Steps: Simulate slow network, test responsiveness
  - Expected: Timeouts handled appropriately

### Data Validation
- [ ] **TC-ERROR-003**: SQL injection prevention
  - Steps: Attempt SQL injection in text fields
  - Expected: Input sanitized, no SQL execution

- [ ] **TC-ERROR-004**: XSS prevention
  - Steps: Attempt XSS in text fields
  - Expected: Input sanitized, no script execution

### System Limits
- [ ] **TC-ERROR-005**: Database connection limits
  - Steps: Simulate database connection exhaustion
  - Expected: Graceful degradation, user notified

- [ ] **TC-ERROR-006**: File system limits
  - Steps: Fill disk space, attempt file operations
  - Expected: Appropriate error handling

## Security Testing

### Authentication Security
- [ ] **TC-SEC-001**: Session timeout
  - Steps: Wait for session timeout, attempt action
  - Expected: Redirected to login, session expired

- [ ] **TC-SEC-002**: CSRF protection
  - Steps: Attempt CSRF attack
  - Expected: Request rejected, protection working

### Authorization
- [ ] **TC-SEC-003**: Role-based access control
  - Steps: Test access to privileged functionality
  - Expected: Access restricted appropriately

- [ ] **TC-SEC-004**: URL manipulation protection
  - Steps: Attempt to access unauthorized URLs directly
  - Expected: Access denied/redirected to login

## Test Data Preparation

### Sample Documents
Create test documents in `Testing/TestDocuments/` folder:
- `sample-contract.pdf` - Legal contract with standard clauses
- `sample-lease.docx` - Rental agreement document
- `large-document.pdf` - File接近10MB limit
- `invalid-file.txt` - Unsupported file type
- `oversized-file.pdf` - File >10MB

### Test User Accounts
Create test users for comprehensive testing:
- testuser1@example.com / Password123!
- testuser2@example.com / Password123!

## Test Execution Schedule

### Phase 1: Core Functionality (Day 1)
- Authentication flows
- Basic document upload
- Case management basics

### Phase 2: Advanced Features (Day 2)
- AI analysis integration
- Advanced error handling
- Performance testing

### Phase 3: Edge Cases & Security (Day 3)
- Security testing
- Edge case validation
- Cross-browser testing

### Phase 4: Final Validation (Day 4)
- End-to-end workflow testing
- User acceptance testing
- Final bug verification

## Bug Reporting Template

When issues are found, report using this format:

```
**Bug ID**: [Unique identifier]
**Severity**: [Critical/Major/Minor/Cosmetic]
**Module**: [Authentication/Document Processing/etc.]
**Steps to Reproduce**:
1. 
2. 
3.
**Expected Result**:
**Actual Result**:
**Environment**: [Browser/OS/Device]
**Screenshots**: [If applicable]
```

## Success Criteria

- All critical user workflows function correctly
- No data loss or corruption scenarios
- Performance meets acceptable standards
- Security vulnerabilities addressed
- Cross-browser compatibility achieved
- Mobile responsiveness validated
- Error handling graceful and informative
- User experience intuitive and smooth