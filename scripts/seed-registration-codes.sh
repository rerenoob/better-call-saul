#!/bin/bash

# Bash script to seed registration codes
# Usage: ./seed-registration-codes.sh [count] [expires_days] [created_by] [notes]

set -e

# Default values
COUNT=${1:-100}
EXPIRES_DAYS=${2:-365}
CREATED_BY=${3:-"System"}
NOTES=${4:-"Initial seeding - batch registration codes"}

echo "🔑 Seeding Registration Codes"
echo "=================================="
echo "Count: $COUNT"
echo "Expires in: $EXPIRES_DAYS days"
echo "Created by: $CREATED_BY"
echo "Notes: $NOTES"
echo ""

# Generate secure random codes
generate_code() {
    openssl rand -base64 12 | tr -d "=+/" | cut -c1-12 | tr '[:lower:]' '[:upper:]'
}

# Check if SQLite database exists
DB_PATH="./BetterCallSaul.API/BetterCallSaul.db"

if [ ! -f "$DB_PATH" ]; then
    echo "❌ Database not found at $DB_PATH"
    echo "Please run the application first to create the database."
    exit 1
fi

# Calculate expiration date
EXPIRES_AT=$(date -u -d "+${EXPIRES_DAYS} days" '+%Y-%m-%d %H:%M:%S')
CREATED_AT=$(date -u '+%Y-%m-%d %H:%M:%S')

echo "📊 Generating $COUNT registration codes..."

# Create temporary SQL file
SQL_FILE=$(mktemp)
trap "rm -f $SQL_FILE" EXIT

# Write SQL statements
cat > "$SQL_FILE" << EOF
-- Seeding $COUNT registration codes
BEGIN TRANSACTION;

EOF

for i in $(seq 1 $COUNT); do
    CODE=$(generate_code)
    # Ensure code is unique by checking against database
    while sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM RegistrationCodes WHERE Code = '$CODE';" | grep -q "1"; do
        CODE=$(generate_code)
    done
    
    ID=$(uuidgen)
    
    cat >> "$SQL_FILE" << EOF
INSERT INTO RegistrationCodes (Id, Code, CreatedBy, IsUsed, UsedByUserId, UsedAt, ExpiresAt, CreatedAt, UpdatedAt, Notes)
VALUES ('$ID', '$CODE', '$CREATED_BY', 0, NULL, NULL, '$EXPIRES_AT', '$CREATED_AT', NULL, '$NOTES');

EOF

    if [ $((i % 10)) -eq 0 ]; then
        echo "  Generated $i/$COUNT codes..."
    fi
done

echo "COMMIT;" >> "$SQL_FILE"

echo "💾 Inserting codes into database..."
sqlite3 "$DB_PATH" < "$SQL_FILE"

# Verify insertion
TOTAL_CODES=$(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM RegistrationCodes;")
NEW_CODES=$(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM RegistrationCodes WHERE Notes = '$NOTES';")

echo ""
echo "✅ Success!"
echo "   Total registration codes in database: $TOTAL_CODES"
echo "   New codes added: $NEW_CODES"
echo ""

# Show sample of generated codes
echo "📋 Sample of generated codes:"
echo "==============================="
sqlite3 "$DB_PATH" "SELECT Code, CreatedAt, ExpiresAt FROM RegistrationCodes WHERE Notes = '$NOTES' LIMIT 5;" | while IFS='|' read -r code created expires; do
    echo "  $code (expires: $expires)"
done

echo ""
echo "🎉 Registration code seeding completed!"