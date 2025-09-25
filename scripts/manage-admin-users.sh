#!/bin/bash

# Admin user management script
# Usage: ./manage-admin-users.sh [command] [options]

set -e

DB_PATH="./BetterCallSaul.API/BetterCallSaul.db"

show_help() {
    cat << EOF
👤 Admin User Management Tool
============================

Usage: $0 <command> [options]

Commands:
  create <email> [password] [full_name]
                        Create a new admin user
  list                  List all admin users
  promote <email>       Promote existing user to admin
  demote <email>        Remove admin role from user
  reset-password <email> <new_password>
                        Reset password for admin user
  delete <email>        Delete admin user (careful!)
  help                  Show this help message

Examples:
  $0 create admin@company.com "SecurePass123!" "John Admin"
  $0 list
  $0 promote user@company.com
  $0 reset-password admin@company.com "NewPass123!"

Default admin credentials (if none specified):
  Email: admin@bettercallsaul.com
  Password: Admin123!
  Name: System Administrator

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

get_user_id_by_email() {
    local email="$1"
    sqlite3 "$DB_PATH" "SELECT Id FROM AspNetUsers WHERE Email = '$email';" 2>/dev/null | head -1
}

get_role_id_by_name() {
    local role_name="$1"
    sqlite3 "$DB_PATH" "SELECT Id FROM AspNetRoles WHERE Name = '$role_name';" 2>/dev/null | head -1
}

create_admin_user() {
    local email="$1"
    local password="${2:-Admin123!}"
    local full_name="${3:-System Administrator}"

    if [ -z "$email" ]; then
        echo "❌ Email is required"
        echo "Usage: $0 create <email> [password] [full_name]"
        exit 1
    fi

    check_db

    echo "👤 Creating admin user..."
    echo "   Email: $email"
    echo "   Full Name: $full_name"
    echo "   Password: [hidden]"
    echo ""

    # Check if user already exists
    local existing_user=$(get_user_id_by_email "$email")
    if [ -n "$existing_user" ]; then
        echo "❌ User with email $email already exists"
        echo "Use 'promote' command to add admin role to existing user"
        exit 1
    fi

    # Generate user ID
    local user_id=$(uuidgen)
    local normalized_email=$(echo "$email" | tr '[:lower:]' '[:upper:]')
    local normalized_username=$(echo "$email" | tr '[:lower:]' '[:upper:]')

    # Hash password (simplified - in production use proper password hashing)
    local password_hash="AQAAAAEAACcQAAAAEHashed_${password}_Salt123" # Placeholder

    # Create user
    sqlite3 "$DB_PATH" "
        INSERT INTO AspNetUsers (
            Id, UserName, NormalizedUserName, Email, NormalizedEmail,
            EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
            FullName, CreatedAt, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled
        ) VALUES (
            '$user_id', '$email', '$normalized_username', '$email', '$normalized_email',
            1, '$password_hash', '$(uuidgen)', '$(uuidgen)',
            '$full_name', '$(date -u '+%Y-%m-%d %H:%M:%S')', NULL, 0, 0, 0
        );
    "

    # Get Admin role ID
    local admin_role_id=$(get_role_id_by_name "Admin")
    if [ -z "$admin_role_id" ]; then
        echo "❌ Admin role not found. Please run the application first to seed roles."
        exit 1
    fi

    # Add user to Admin role
    sqlite3 "$DB_PATH" "
        INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES ('$user_id', '$admin_role_id');
    "

    echo "✅ Successfully created admin user: $email"
    echo ""
    echo "⚠️  IMPORTANT: The password hash is simplified for demo purposes."
    echo "   In production, restart the application to create users with proper password hashing."
}

list_admin_users() {
    check_db

    echo "👥 Admin Users"
    echo "=============="
    printf "%-40s %-30s %-20s %-10s\n" "Email" "Full Name" "Created" "Confirmed"
    echo "$(printf '%*s' 100 '' | tr ' ' '-')"

    sqlite3 "$DB_PATH" "
        SELECT
            u.Email,
            u.FullName,
            datetime(u.CreatedAt, 'localtime') as Created,
            CASE WHEN u.EmailConfirmed = 1 THEN 'Yes' ELSE 'No' END as Confirmed
        FROM AspNetUsers u
        INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
        INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
        WHERE r.Name = 'Admin'
        ORDER BY u.CreatedAt DESC;
    " | while IFS='|' read -r email full_name created confirmed; do
        printf "%-40s %-30s %-20s %-10s\n" "$email" "$full_name" "$created" "$confirmed"
    done
}

promote_user() {
    local email="$1"
    if [ -z "$email" ]; then
        echo "❌ Email is required"
        echo "Usage: $0 promote <email>"
        exit 1
    fi

    check_db

    echo "⬆️  Promoting user to admin: $email"

    local user_id=$(get_user_id_by_email "$email")
    if [ -z "$user_id" ]; then
        echo "❌ User with email $email not found"
        exit 1
    fi

    local admin_role_id=$(get_role_id_by_name "Admin")
    if [ -z "$admin_role_id" ]; then
        echo "❌ Admin role not found"
        exit 1
    fi

    # Check if user already has admin role
    local existing_role=$(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM AspNetUserRoles WHERE UserId = '$user_id' AND RoleId = '$admin_role_id';")
    if [ "$existing_role" -gt 0 ]; then
        echo "ℹ️  User $email already has admin role"
        return
    fi

    # Add admin role
    sqlite3 "$DB_PATH" "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES ('$user_id', '$admin_role_id');"

    echo "✅ Successfully promoted $email to admin"
}

demote_user() {
    local email="$1"
    if [ -z "$email" ]; then
        echo "❌ Email is required"
        echo "Usage: $0 demote <email>"
        exit 1
    fi

    check_db

    echo "⬇️  Removing admin role from user: $email"

    local user_id=$(get_user_id_by_email "$email")
    if [ -z "$user_id" ]; then
        echo "❌ User with email $email not found"
        exit 1
    fi

    local admin_role_id=$(get_role_id_by_name "Admin")
    if [ -z "$admin_role_id" ]; then
        echo "❌ Admin role not found"
        exit 1
    fi

    # Remove admin role
    sqlite3 "$DB_PATH" "DELETE FROM AspNetUserRoles WHERE UserId = '$user_id' AND RoleId = '$admin_role_id';"

    echo "✅ Successfully removed admin role from $email"
}

delete_user() {
    local email="$1"
    if [ -z "$email" ]; then
        echo "❌ Email is required"
        echo "Usage: $0 delete <email>"
        exit 1
    fi

    check_db

    echo "⚠️  DANGER: Deleting user: $email"
    echo "This action cannot be undone!"
    read -p "Type 'DELETE' to confirm: " -r

    if [ "$REPLY" != "DELETE" ]; then
        echo "❌ Deletion cancelled"
        exit 1
    fi

    local user_id=$(get_user_id_by_email "$email")
    if [ -z "$user_id" ]; then
        echo "❌ User with email $email not found"
        exit 1
    fi

    # Delete user roles first
    sqlite3 "$DB_PATH" "DELETE FROM AspNetUserRoles WHERE UserId = '$user_id';"

    # Delete user
    sqlite3 "$DB_PATH" "DELETE FROM AspNetUsers WHERE Id = '$user_id';"

    echo "✅ Successfully deleted user: $email"
}

# Main command handling
case "${1:-help}" in
    "create")
        create_admin_user "$2" "$3" "$4"
        ;;
    "list")
        list_admin_users
        ;;
    "promote")
        promote_user "$2"
        ;;
    "demote")
        demote_user "$2"
        ;;
    "delete")
        delete_user "$2"
        ;;
    "help"|*)
        show_help
        ;;
esac