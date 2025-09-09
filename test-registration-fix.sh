#!/bin/bash

echo "Testing Registration Flow Foreign Key Constraint Fix"
echo "=================================================="

# Get an unused registration code from the database
UNUSED_CODE=$(sqlite3 BetterCallSaul.API/BetterCallSaul.db "SELECT Code FROM RegistrationCodes WHERE IsUsed = 0 LIMIT 1;")

if [ -z "$UNUSED_CODE" ]; then
    echo "Error: No unused registration codes found in database"
    exit 1
fi

echo "Using registration code: $UNUSED_CODE"

# Create a unique test email
TEST_EMAIL="test-$(date +%s)@example.com"

# Test registration API endpoint
echo "Testing registration with email: $TEST_EMAIL"

# Build the JSON payload
JSON_PAYLOAD=$(cat <<EOF
{
  "email": "$TEST_EMAIL",
  "password": "Password123!",
  "firstName": "Test",
  "lastName": "User",
  "registrationCode": "$UNUSED_CODE"
}
EOF
)

echo "Payload: $JSON_PAYLOAD"

# Make the API request (assuming backend is running on localhost:7191)
echo "Making API request to http://localhost:7191/api/auth/register..."

# Use curl to test the registration endpoint
RESPONSE=$(curl -s -X POST "http://localhost:7191/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "$JSON_PAYLOAD" \
  -w "\n%{http_code}")

# Extract status code and response body
STATUS_CODE=$(echo "$RESPONSE" | tail -n1)
RESPONSE_BODY=$(echo "$RESPONSE" | sed '$d')

echo "Status Code: $STATUS_CODE"
echo "Response: $RESPONSE_BODY"

if [ "$STATUS_CODE" -eq 200 ]; then
    echo "✅ SUCCESS: Registration completed without foreign key constraint errors"
    
    # Verify the registration code was properly updated
    CODE_STATUS=$(sqlite3 BetterCallSaul.API/BetterCallSaul.db "SELECT IsUsed, UsedByUserId FROM RegistrationCodes WHERE Code = '$UNUSED_CODE';")
    echo "Registration code status after registration: $CODE_STATUS"
    
    # Verify user was created
    USER_EXISTS=$(sqlite3 BetterCallSaul.API/BetterCallSaul.db "SELECT COUNT(*) FROM Users WHERE Email = '$TEST_EMAIL';")
    if [ "$USER_EXISTS" -eq 1 ]; then
        echo "✅ SUCCESS: User was successfully created in the database"
    else
        echo "❌ ERROR: User was not created in the database"
    fi
else
    echo "❌ ERROR: Registration failed with status code $STATUS_CODE"
    echo "Response body: $RESPONSE_BODY"
fi

echo ""
echo "Test completed."