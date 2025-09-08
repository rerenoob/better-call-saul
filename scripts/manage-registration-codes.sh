#!/bin/bash

# Enhanced registration code management script
# Usage: ./manage-registration-codes.sh [command] [options]

set -e

DB_PATH="./BetterCallSaul.API/BetterCallSaul.db"

show_help() {
    cat << EOF
🔑 Registration Code Management Tool
====================================

Usage: $0 <command> [options]

Commands:
  seed <count> [expires_days] [created_by] [notes]
                        Seed database with registration codes
  list [limit]         List existing codes (default: 20)
  stats               Show registration code statistics
  cleanup             Remove expired and used codes
  validate <code>     Check if a registration code is valid
  export              Export all codes to CSV
  help                Show this help message

Examples:
  $0 seed 100 365 "Admin" "Monthly batch"
  $0 list 50
  $0 stats
  $0 cleanup
  $0 validate ABC123XYZ456
  $0 export

Database: $DB_PATH
EOF
}

check_db() {
    if [ ! -f "$DB_PATH" ]; then
        echo "❌ Database not found at $DB_PATH"
        echo "Please run the application first to create the database."
        exit 1
    fi
}

generate_code() {
    openssl rand -base64 12 | tr -d "=+/" | cut -c1-12 | tr '[:lower:]' '[:upper:]'
}

seed_codes() {
    local count=${1:-100}
    local expires_days=${2:-365}
    local created_by=${3:-"System"}
    local notes=${4:-"Batch seeding - $(date +'%Y-%m-%d %H:%M:%S')"}
    
    check_db
    
    echo "🌱 Seeding $count registration codes..."
    echo "   Expires in: $expires_days days"
    echo "   Created by: $created_by"
    echo "   Notes: $notes"
    echo ""
    
    local expires_at=$(date -u -d "+${expires_days} days" '+%Y-%m-%d %H:%M:%S')
    local created_at=$(date -u '+%Y-%m-%d %H:%M:%S')
    
    # Create temp SQL file
    local sql_file=$(mktemp)
    trap "rm -f $sql_file" EXIT
    
    echo "BEGIN TRANSACTION;" > "$sql_file"
    
    for i in $(seq 1 $count); do
        local code=$(generate_code)
        # Ensure uniqueness
        while sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM RegistrationCodes WHERE Code = '$code';" | grep -q "1"; do
            code=$(generate_code)
        done
        
        local id=$(uuidgen)
        echo "INSERT INTO RegistrationCodes (Id, Code, CreatedBy, IsUsed, UsedByUserId, UsedAt, ExpiresAt, CreatedAt, UpdatedAt, Notes) VALUES ('$id', '$code', '$created_by', 0, NULL, NULL, '$expires_at', '$created_at', NULL, '$notes');" >> "$sql_file"
        
        if [ $((i % 25)) -eq 0 ]; then
            echo "  Generated $i/$count codes..."
        fi
    done
    
    echo "COMMIT;" >> "$sql_file"
    
    echo "💾 Inserting codes into database..."
    sqlite3 "$DB_PATH" < "$sql_file"
    
    echo "✅ Successfully seeded $count registration codes!"
    show_stats
}

show_stats() {
    check_db
    
    local total=$(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM RegistrationCodes;")
    local used=$(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM RegistrationCodes WHERE IsUsed = 1;")
    local expired=$(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM RegistrationCodes WHERE IsUsed = 0 AND ExpiresAt < datetime('now');")
    local active=$((total - used - expired))
    
    echo "📊 Registration Code Statistics"
    echo "==============================="
    echo "Total codes:    $total"
    echo "Active codes:   $active"
    echo "Used codes:     $used"
    echo "Expired codes:  $expired"
    echo ""
    
    if [ $total -gt 0 ]; then
        local usage_percent=$((used * 100 / total))
        echo "Usage rate:     ${usage_percent}%"
        
        # Show expiration info
        local expiring_soon=$(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM RegistrationCodes WHERE IsUsed = 0 AND ExpiresAt BETWEEN datetime('now') AND datetime('now', '+7 days');")
        if [ $expiring_soon -gt 0 ]; then
            echo "⚠️  Expiring in 7 days: $expiring_soon codes"
        fi
    fi
}

list_codes() {
    local limit=${1:-20}
    check_db
    
    echo "📋 Registration Codes (showing latest $limit)"
    echo "=============================================="
    printf "%-15s %-15s %-20s %-20s %-5s %-20s\n" "Code" "Created By" "Created" "Expires" "Used" "Notes"
    echo "$(printf '%*s' 100 '' | tr ' ' '-')"
    
    sqlite3 "$DB_PATH" "
        SELECT 
            Code,
            CreatedBy,
            datetime(CreatedAt, 'localtime') as Created,
            datetime(ExpiresAt, 'localtime') as Expires,
            CASE WHEN IsUsed = 1 THEN 'Yes' ELSE 'No' END as Used,
            COALESCE(substr(Notes, 1, 20), '') as Notes
        FROM RegistrationCodes 
        ORDER BY CreatedAt DESC 
        LIMIT $limit;
    " | while IFS='|' read -r code created_by created expires used notes; do
        printf "%-15s %-15s %-20s %-20s %-5s %-20s\n" "$code" "$created_by" "$created" "$expires" "$used" "$notes"
    done
}

cleanup_codes() {
    check_db
    
    echo "🧹 Cleaning up expired and used registration codes..."
    
    local before_count=$(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM RegistrationCodes;")
    local expired_count=$(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM RegistrationCodes WHERE IsUsed = 1 OR ExpiresAt < datetime('now');")
    
    if [ $expired_count -eq 0 ]; then
        echo "✨ No expired or used codes to clean up."
        return
    fi
    
    echo "Found $expired_count codes to remove..."
    read -p "Continue? (y/N): " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        sqlite3 "$DB_PATH" "DELETE FROM RegistrationCodes WHERE IsUsed = 1 OR ExpiresAt < datetime('now');"
        local after_count=$(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM RegistrationCodes;")
        local removed=$((before_count - after_count))
        echo "✅ Removed $removed codes from database."
    else
        echo "❌ Cleanup cancelled."
    fi
}

validate_code() {
    local code=$1
    if [ -z "$code" ]; then
        echo "❌ Please provide a code to validate"
        echo "Usage: $0 validate <code>"
        exit 1
    fi
    
    check_db
    
    echo "🔍 Validating registration code: $code"
    
    local result=$(sqlite3 "$DB_PATH" "
        SELECT 
            CASE 
                WHEN COUNT(*) = 0 THEN 'NOT_FOUND'
                WHEN IsUsed = 1 THEN 'USED'
                WHEN ExpiresAt < datetime('now') THEN 'EXPIRED'
                ELSE 'VALID'
            END as Status,
            CreatedBy,
            datetime(CreatedAt, 'localtime') as Created,
            datetime(ExpiresAt, 'localtime') as Expires,
            Notes
        FROM RegistrationCodes 
        WHERE Code = '$code'
        GROUP BY Code, IsUsed, ExpiresAt, CreatedBy, CreatedAt, Notes;
    ")
    
    if [ -z "$result" ]; then
        echo "❌ Code not found"
        exit 1
    fi
    
    IFS='|' read -r status created_by created expires notes <<< "$result"
    
    case "$status" in
        "VALID")
            echo "✅ Code is VALID"
            ;;
        "USED")
            echo "❌ Code has been USED"
            ;;
        "EXPIRED")
            echo "⏰ Code has EXPIRED"
            ;;
        "NOT_FOUND")
            echo "❌ Code NOT FOUND"
            ;;
    esac
    
    if [ "$status" != "NOT_FOUND" ]; then
        echo "   Created by: $created_by"
        echo "   Created:    $created"
        echo "   Expires:    $expires"
        if [ -n "$notes" ]; then
            echo "   Notes:      $notes"
        fi
    fi
}

export_codes() {
    check_db
    
    local csv_file="registration-codes-$(date +'%Y%m%d-%H%M%S').csv"
    
    echo "📤 Exporting registration codes to $csv_file..."
    
    # Create CSV with headers
    echo "Code,CreatedBy,IsUsed,UsedAt,ExpiresAt,CreatedAt,Notes" > "$csv_file"
    
    sqlite3 "$DB_PATH" "
        SELECT 
            Code,
            CreatedBy,
            CASE WHEN IsUsed = 1 THEN 'true' ELSE 'false' END,
            COALESCE(UsedAt, ''),
            ExpiresAt,
            CreatedAt,
            COALESCE(Notes, '')
        FROM RegistrationCodes 
        ORDER BY CreatedAt DESC;
    " | tr '|' ',' >> "$csv_file"
    
    local count=$(tail -n +2 "$csv_file" | wc -l)
    echo "✅ Exported $count registration codes to $csv_file"
}

# Main command handling
case "${1:-help}" in
    "seed")
        seed_codes "$2" "$3" "$4" "$5"
        ;;
    "list")
        list_codes "$2"
        ;;
    "stats")
        show_stats
        ;;
    "cleanup")
        cleanup_codes
        ;;
    "validate")
        validate_code "$2"
        ;;
    "export")
        export_codes
        ;;
    "help"|*)
        show_help
        ;;
esac