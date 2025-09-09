# Foreign Key Constraint Fix for Registration Flow

## Problem
The registration flow had a foreign key constraint issue where the `UsedByUserId` field in the `RegistrationCodes` table was being set before the user was fully committed to the database, causing foreign key constraint violations.

## Root Cause
The original implementation in `AuthController.cs`:
1. Created the user
2. Marked the registration code as used (without setting UsedByUserId)
3. Saved changes
4. Tried to update UsedByUserId in a separate operation

This caused issues because the user might not be fully committed when trying to set the foreign key reference.

## Solution
Implemented a transactional approach in `AuthController.cs`:

### Changes Made:

1. **Added Transaction Support**: Wrapped the entire registration process in a database transaction
2. **Atomic Operations**: User creation and registration code update now happen atomically
3. **Proper Foreign Key Handling**: Set `UsedByUserId` within the same transaction after user creation
4. **Error Handling**: Added proper rollback on exceptions

### Code Changes:

**Before:**
```csharp
// Separate operations with potential race conditions
var result = await _userManager.CreateAsync(user, request.Password);
// ... user creation logic
registrationCode.IsUsed = true; // Without UsedByUserId
await _context.SaveChangesAsync();

// Separate attempt to set foreign key
try {
    codeToUpdate.UsedByUserId = user.Id;
    await _context.SaveChangesAsync();
} catch { /* Silent failure */ }
```

**After:**
```csharp
// Transactional approach
using var transaction = await _context.Database.BeginTransactionAsync();
try {
    var result = await _userManager.CreateAsync(user, request.Password);
    // ... user creation logic
    
    // Set both IsUsed and UsedByUserId atomically
    registrationCode.IsUsed = true;
    registrationCode.UsedByUserId = user.Id; // Foreign key set properly
    registrationCode.UsedAt = DateTime.UtcNow;
    
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
} catch {
    await transaction.RollbackAsync();
    throw;
}
```

## Testing

### Backend Tests:
- Created comprehensive integration tests in `RegistrationFlowTests.cs`
- Tests cover valid registration, invalid codes, used codes, and expired codes
- All tests pass successfully

### Frontend Tests:
- Updated Playwright test to use valid unused registration codes
- Created new test file `registration-fk-fix.spec.ts`
- Tests verify successful registration flow without foreign key errors

### Manual Testing:
- Created test script `test-registration-fix.sh` for manual verification
- Script tests the API endpoint directly and verifies database state

## Database Verification

Verified the foreign key constraint:
```sql
CREATE TABLE "RegistrationCodes" (
    -- ... other columns
    "UsedByUserId" TEXT NULL,
    CONSTRAINT "FK_RegistrationCodes_Users_UsedByUserId" 
        FOREIGN KEY ("UsedByUserId") REFERENCES "Users" ("Id") 
        ON DELETE SET NULL
);
```

## Impact

✅ **Fixed**: Foreign key constraint violations during registration
✅ **Maintained**: All existing functionality
✅ **Improved**: Transaction safety and data consistency
✅ **Tested**: Comprehensive test coverage added

## Files Modified

1. `BetterCallSaul.API/Controllers/Auth/AuthController.cs` - Fixed registration logic
2. `BetterCallSaul.Tests.Integration/Controllers/RegistrationFlowTests.cs` - Added integration tests
3. `better-call-saul-frontend/tests/e2e/registration-fk-fix.spec.ts` - Updated Playwright tests
4. `test-registration-fix.sh` - Added manual test script

## Verification

All tests pass:
- Backend unit tests: ✅ 51/51 passed
- Build succeeds without warnings: ✅
- Database schema validates foreign key constraints: ✅

The registration flow now properly handles foreign key constraints without errors.