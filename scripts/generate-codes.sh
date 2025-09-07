#!/bin/bash

# Generate Registration Codes for Better Call Saul
# Usage: ./generate-codes.sh [count] [expire_days] [created_by] [notes]

set -e

# Default values
COUNT=${1:-1}
EXPIRE_DAYS=${2:-30}
CREATED_BY="${3:-Admin}"
NOTES="${4:-}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Function to generate secure code
generate_code() {
    # Generate 12-character code using alphanumeric characters
    openssl rand -base64 32 | tr -d "=+/" | cut -c1-12 | tr '[:lower:]' '[:upper:]'
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [count] [expire_days] [created_by] [notes]"
    echo ""
    echo "Arguments:"
    echo "  count       Number of codes to generate (default: 1)"
    echo "  expire_days Days until expiration (default: 30)"
    echo "  created_by  Who is creating the codes (default: 'Admin')"
    echo "  notes       Optional notes for the codes"
    echo ""
    echo "Examples:"
    echo "  $0 5 60 'John Doe' 'Public defender registration'"
    echo "  $0 10 30"
    echo ""
    echo "Special commands:"
    echo "  $0 list     - List existing codes"
    echo "  $0 cleanup  - Remove expired codes"
}

# Check for special commands
if [ "$1" = "help" ] || [ "$1" = "--help" ] || [ "$1" = "-h" ]; then
    show_usage
    exit 0
fi

if [ "$1" = "list" ]; then
    echo -e "${YELLOW}This would list codes from the database.${NC}"
    echo -e "${GRAY}Run this from the API project with Entity Framework tools:${NC}"
    echo "  cd BetterCallSaul.API"
    echo "  dotnet ef database update"
    echo "  dotnet run -- list-codes"
    exit 0
fi

if [ "$1" = "cleanup" ]; then
    echo -e "${YELLOW}This would clean up expired codes from the database.${NC}"
    echo -e "${GRAY}Run this from the API project:${NC}"
    echo "  cd BetterCallSaul.API"
    echo "  dotnet run -- cleanup-codes"
    exit 0
fi

# Validate count is a number
if ! [[ "$COUNT" =~ ^[0-9]+$ ]]; then
    echo -e "${RED}Error: count must be a number${NC}"
    show_usage
    exit 1
fi

if ! [[ "$EXPIRE_DAYS" =~ ^[0-9]+$ ]]; then
    echo -e "${RED}Error: expire_days must be a number${NC}"
    show_usage
    exit 1
fi

# Calculate expiration date
if command -v date >/dev/null 2>&1; then
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        EXPIRE_DATE=$(date -v +${EXPIRE_DAYS}d -u +"%Y-%m-%d %H:%M:%S")
    else
        # Linux
        EXPIRE_DATE=$(date -u -d "+${EXPIRE_DAYS} days" +"%Y-%m-%d %H:%M:%S")
    fi
else
    EXPIRE_DATE="$(($EXPIRE_DAYS)) days from now"
fi

echo -e "${YELLOW}Generating $COUNT registration codes...${NC}"
echo -e "${GRAY}Created by: $CREATED_BY${NC}"
echo -e "${GRAY}Expires: $EXPIRE_DATE UTC${NC}"
if [ -n "$NOTES" ]; then
    echo -e "${GRAY}Notes: $NOTES${NC}"
fi
echo ""

# Generate codes
CODES=()
echo -e "${GREEN}Generated codes:${NC}"
echo -e "${GRAY}$(printf '%*s' 50 | tr ' ' '-')${NC}"

for ((i=1; i<=COUNT; i++)); do
    CODE=$(generate_code)
    CODES+=("$CODE")
    echo -e "${CYAN}$CODE${NC}"
done

echo -e "${GRAY}$(printf '%*s' 50 | tr ' ' '-')${NC}"
echo -e "${GREEN}Successfully generated ${#CODES[@]} registration codes!${NC}"
echo ""
echo -e "${YELLOW}Note: These codes are generated but not saved to the database.${NC}"
echo -e "${GRAY}To save them, use the PowerShell script or add them through the API.${NC}"
echo ""
echo -e "${GRAY}Save these codes securely before closing this terminal.${NC}"

# Optionally save to file
read -p "Save codes to file? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    TIMESTAMP=$(date +%Y%m%d_%H%M%S)
    FILENAME="registration_codes_$TIMESTAMP.txt"
    
    {
        echo "Registration Codes Generated: $(date)"
        echo "Created by: $CREATED_BY"
        echo "Expires: $EXPIRE_DATE UTC"
        echo "Notes: $NOTES"
        echo ""
        echo "Codes:"
        printf '%s\n' "${CODES[@]}"
    } > "$FILENAME"
    
    echo -e "${GREEN}Codes saved to: $FILENAME${NC}"
fi