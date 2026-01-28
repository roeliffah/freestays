#!/bin/bash
#
# FreeStays Quick Database Init Script
# =====================================
# Run this on a new EC2 deployment to set up admin credentials
#
# Usage:
#   chmod +x init_db.sh
#   ./init_db.sh
#
# Or with custom credentials:
#   ./init_db.sh "admin@example.com" "MyPassword123"
#

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default credentials (change these!)
ADMIN_EMAIL="${1:-rob.ozinga@freestays.eu}"
ADMIN_PASSWORD="${2:-Barneveld2026!@}"
ADMIN_NAME="${3:-Rob Ozinga}"

# Get DB name from .env or use default
if [ -f "/app/backend/.env" ]; then
    DB_NAME=$(grep "^DB_NAME=" /app/backend/.env | cut -d '=' -f2 | tr -d '"')
fi
DB_NAME="${DB_NAME:-freestays}"

echo -e "${YELLOW}======================================${NC}"
echo -e "${YELLOW}FreeStays Database Initialization${NC}"
echo -e "${YELLOW}======================================${NC}"
echo ""
echo -e "Database: ${GREEN}$DB_NAME${NC}"
echo -e "Admin Email: ${GREEN}$ADMIN_EMAIL${NC}"
echo ""

# Check if mongosh is available
if command -v mongosh &> /dev/null; then
    MONGO_CMD="mongosh"
elif command -v mongo &> /dev/null; then
    MONGO_CMD="mongo"
else
    echo -e "${RED}Error: MongoDB shell not found (mongosh or mongo)${NC}"
    echo "Please install MongoDB tools or use the Python script instead:"
    echo "  python3 /app/backend/scripts/init_database.py"
    exit 1
fi

echo "Using MongoDB shell: $MONGO_CMD"
echo ""

# Generate password hash (SHA256)
PASSWORD_HASH=$(echo -n "$ADMIN_PASSWORD" | sha256sum | cut -d' ' -f1)

# Generate IDs
USER_ID="user_$(cat /dev/urandom | tr -dc 'a-f0-9' | fold -w 16 | head -n 1)"
REFERRAL_CODE="REF$(cat /dev/urandom | tr -dc 'A-Z0-9' | fold -w 8 | head -n 1)"

# =========================================
# 1. Create/Update Admin User in users collection (for /admin login)
# =========================================
echo -e "${YELLOW}[1/2] Setting up Admin User (for /admin login)...${NC}"

$MONGO_CMD $DB_NAME --quiet --eval "
var email = '$ADMIN_EMAIL'.toLowerCase();
var existing = db.users.findOne({ email: email });

if (existing) {
    // Update existing user with is_admin=true
    db.users.updateOne(
        { email: email },
        {
            \$set: {
                is_admin: true,
                role: 'admin',
                password: '$PASSWORD_HASH'
            }
        }
    );
    print('Updated existing user with is_admin=true');
} else {
    // Create new admin user
    db.users.insertOne({
        user_id: '$USER_ID',
        email: email,
        name: '$ADMIN_NAME',
        password: '$PASSWORD_HASH',
        is_admin: true,
        role: 'admin',
        pass_type: 'free',
        pass_code: null,
        pass_expires_at: null,
        email_verified: true,
        referral_code: '$REFERRAL_CODE',
        referral_count: 0,
        referral_discount: 0,
        newsletter_subscribed: false,
        created_at: new Date().toISOString()
    });
    print('Created new admin user');
}
"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Admin User ready (is_admin=true in users collection)${NC}"
else
    echo -e "${RED}✗ Failed to set up Admin User${NC}"
fi

# =========================================
# 2. Create CMS Admin (for /cms login)
# =========================================
echo ""
echo -e "${YELLOW}[2/2] Setting up CMS Admin User (for /cms login)...${NC}"

ADMIN_ID="admin_$(cat /dev/urandom | tr -dc 'a-f0-9' | fold -w 16 | head -n 1)"

$MONGO_CMD $DB_NAME --quiet --eval "
var email = '$ADMIN_EMAIL'.toLowerCase();
var existing = db.cms_admins.findOne({ email: email });

if (existing) {
    db.cms_admins.updateOne(
        { email: email },
        {
            \$set: {
                password_hash: '$PASSWORD_HASH',
                role: 'super_admin',
                is_active: true
            }
        }
    );
    print('Updated CMS admin');
} else {
    db.cms_admins.insertOne({
        admin_id: '$ADMIN_ID',
        email: email,
        password_hash: '$PASSWORD_HASH',
        first_name: '${ADMIN_NAME%% *}',
        last_name: '${ADMIN_NAME##* }',
        role: 'super_admin',
        is_active: true,
        two_factor_enabled: false,
        created_at: new Date().toISOString()
    });
    print('Created CMS admin');
}
"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ CMS Admin ready (in cms_admins collection)${NC}"
else
    echo -e "${RED}✗ Failed to set up CMS Admin${NC}"
fi

echo ""
echo -e "${YELLOW}======================================${NC}"
echo -e "${GREEN}INITIALIZATION COMPLETE${NC}"
echo -e "${YELLOW}======================================${NC}"
echo ""
echo "Login Credentials (SAME FOR BOTH):"
echo "-----------------------------------"
echo -e "Admin Panel (${GREEN}/admin${NC}):"
echo "  Email: $ADMIN_EMAIL"
echo "  Password: $ADMIN_PASSWORD"
echo "  → Uses 'users' collection with is_admin=true"
echo ""
echo -e "CMS Panel (${GREEN}/cms${NC}):"
echo "  Email: $ADMIN_EMAIL"
echo "  Password: $ADMIN_PASSWORD"
echo "  → Uses 'cms_admins' collection"
echo ""
echo -e "${YELLOW}Remember to restart your backend service!${NC}"
echo "  sudo systemctl restart freestays-backend"
echo "  # or"
echo "  sudo supervisorctl restart backend"
