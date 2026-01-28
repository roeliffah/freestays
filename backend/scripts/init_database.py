#!/usr/bin/env python3
"""
FreeStays Database Initialization Script
=========================================
Run this script on new deployments to initialize the database with:
- Admin user (is_admin=true in users collection)
- CMS super admin user
- Required indexes

Usage:
    python3 init_database.py
    
Or with custom values:
    python3 init_database.py --admin-email admin@example.com --admin-password MyPassword123
"""

import os
import sys
import argparse
import uuid
import hashlib
from datetime import datetime, timezone

try:
    import bcrypt
    HAS_BCRYPT = True
except ImportError:
    HAS_BCRYPT = False
    print("Warning: bcrypt not installed. Install with: pip install bcrypt")

from pymongo import MongoClient
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

# Default values
DEFAULT_ADMIN_EMAIL = "rob.ozinga@freestays.eu"
DEFAULT_ADMIN_PASSWORD = "Barneveld2026!@"
DEFAULT_ADMIN_NAME = "Rob Ozinga"


def hash_password_bcrypt(password: str) -> str:
    """Hash password using bcrypt (same as server.py for users collection)"""
    if HAS_BCRYPT:
        return bcrypt.hashpw(password.encode('utf-8'), bcrypt.gensalt()).decode('utf-8')
    else:
        # Fallback to SHA256 if bcrypt not available
        return hashlib.sha256(password.encode()).hexdigest()


def hash_password_sha256(password: str) -> str:
    """Hash password using SHA256 (for CMS admins)"""
    return hashlib.sha256(password.encode()).hexdigest()


def init_database(
    mongo_url: str = None,
    db_name: str = None,
    admin_email: str = DEFAULT_ADMIN_EMAIL,
    admin_password: str = DEFAULT_ADMIN_PASSWORD,
    admin_name: str = DEFAULT_ADMIN_NAME,
    skip_if_exists: bool = True
):
    """Initialize the database with required collections and data"""
    
    # Get connection details from env or parameters
    mongo_url = mongo_url or os.environ.get("MONGO_URL", "mongodb://localhost:27017")
    db_name = db_name or os.environ.get("DB_NAME", "freestays")
    
    print(f"=" * 60)
    print("FreeStays Database Initialization")
    print(f"=" * 60)
    print(f"MongoDB URL: {mongo_url[:30]}...")
    print(f"Database: {db_name}")
    print(f"Admin Email: {admin_email}")
    print(f"Bcrypt available: {HAS_BCRYPT}")
    print(f"=" * 60)
    
    try:
        # Connect to MongoDB
        client = MongoClient(mongo_url)
        db = client[db_name]
        
        # Test connection
        client.admin.command('ping')
        print("✓ Connected to MongoDB successfully")
        
        results = {
            "admin_user": False,
            "cms_admin": False,
            "collections": []
        }
        
        # =========================================
        # 1. Create/Update Admin User in users collection
        # =========================================
        print("\n[1/4] Setting up Admin User (for /admin login)...")
        
        existing_user = db.users.find_one({"email": admin_email.lower()})
        
        # Hash password with bcrypt (same as server.py)
        bcrypt_hash = hash_password_bcrypt(admin_password)
        
        if existing_user:
            # Update existing user with is_admin=true and new password
            db.users.update_one(
                {"email": admin_email.lower()},
                {"$set": {
                    "is_admin": True,
                    "role": "admin",
                    "password": bcrypt_hash
                }}
            )
            print(f"  → User exists: {admin_email}")
            print(f"  ✓ Updated user with is_admin=true and new password")
            results["admin_user"] = True
        else:
            # Create new admin user
            user_id = f"user_{uuid.uuid4().hex[:16]}"
            referral_code = f"REF{uuid.uuid4().hex[:8].upper()}"
            
            user_doc = {
                "user_id": user_id,
                "email": admin_email.lower(),
                "name": admin_name,
                "password": bcrypt_hash,
                "is_admin": True,
                "role": "admin",
                "pass_type": "free",
                "pass_code": None,
                "pass_expires_at": None,
                "email_verified": True,
                "referral_code": referral_code,
                "referral_count": 0,
                "referral_discount": 0,
                "newsletter_subscribed": False,
                "created_at": datetime.now(timezone.utc).isoformat()
            }
            
            db.users.insert_one(user_doc)
            print(f"  ✓ Created admin user: {admin_email}")
            print(f"  ✓ User ID: {user_id}")
            results["admin_user"] = True
        
        # =========================================
        # 2. Create CMS Admin User (for /cms login)
        # =========================================
        print("\n[2/4] Setting up CMS Admin User (for /cms login)...")
        
        # CMS uses bcrypt hash in admin_users collection
        cms_bcrypt_hash = hash_password_bcrypt(admin_password)
        
        existing_cms_admin = db.admin_users.find_one({"email": admin_email.lower()})
        
        if existing_cms_admin:
            # Update password and ensure super_admin role
            db.admin_users.update_one(
                {"email": admin_email.lower()},
                {"$set": {
                    "password_hash": cms_bcrypt_hash,
                    "role": "super_admin",
                    "is_active": True
                }}
            )
            print(f"  → CMS admin exists: {admin_email}")
            print(f"  ✓ Updated CMS admin credentials")
        else:
            cms_admin_doc = {
                "admin_id": str(uuid.uuid4()),
                "email": admin_email.lower(),
                "password_hash": cms_bcrypt_hash,
                "first_name": admin_name.split()[0] if admin_name else "Admin",
                "last_name": admin_name.split()[-1] if admin_name and len(admin_name.split()) > 1 else "User",
                "role": "super_admin",
                "is_active": True,
                "two_factor_enabled": False,
                "created_at": datetime.now(timezone.utc).isoformat(),
                "last_login": None
            }
            
            db.admin_users.insert_one(cms_admin_doc)
            print(f"  ✓ Created CMS admin: {admin_email}")
            results["cms_admin"] = True
        
        # =========================================
        # 3. Create Required Indexes
        # =========================================
        print("\n[3/4] Creating database indexes...")
        
        indexes_created = []
        
        # Users collection indexes
        try:
            db.users.create_index("email", unique=True)
            db.users.create_index("user_id", unique=True)
            db.users.create_index("referral_code")
            db.users.create_index("is_admin")
            indexes_created.append("users")
        except Exception as e:
            print(f"  → Users indexes: {e}")
        
        # Bookings collection indexes
        try:
            db.bookings.create_index("booking_id", unique=True)
            db.bookings.create_index("user_id")
            db.bookings.create_index("created_at")
            indexes_created.append("bookings")
        except Exception as e:
            print(f"  → Bookings indexes: {e}")
        
        # Payments collection indexes
        try:
            db.payments.create_index("payment_id", unique=True)
            db.payments.create_index("stripe_payment_intent")
            db.payments.create_index("user_id")
            indexes_created.append("payments")
        except Exception as e:
            print(f"  → Payments indexes: {e}")
        
        # B2B Orders indexes
        try:
            db.b2b_orders.create_index("order_id", unique=True)
            db.b2b_orders.create_index("business_email")
            indexes_created.append("b2b_orders")
        except Exception as e:
            print(f"  → B2B orders indexes: {e}")
        
        # Admin Users indexes (CMS)
        try:
            db.admin_users.create_index("email", unique=True)
            db.admin_users.create_index("admin_id", unique=True)
            indexes_created.append("admin_users")
        except Exception as e:
            print(f"  → Admin users indexes: {e}")
        
        print(f"  ✓ Indexes created for: {', '.join(indexes_created)}")
        results["collections"] = indexes_created
        
        # =========================================
        # 4. Verify Setup
        # =========================================
        print("\n[4/4] Verifying setup...")
        
        # Verify admin user
        admin_user = db.users.find_one({"email": admin_email.lower()})
        if admin_user and admin_user.get("is_admin"):
            print(f"  ✓ Admin user ready: {admin_email} (is_admin=true)")
        else:
            print(f"  ✗ Admin user NOT configured!")
        
        # Verify CMS admin
        cms_admin = db.admin_users.find_one({"email": admin_email.lower()})
        if cms_admin and cms_admin.get("role") == "super_admin":
            print(f"  ✓ CMS admin ready: {admin_email} (role=super_admin)")
        else:
            print(f"  ✗ CMS admin NOT configured!")
        
        # Summary
        print(f"\n{'=' * 60}")
        print("INITIALIZATION COMPLETE")
        print(f"{'=' * 60}")
        print(f"""
Login Credentials (SAME FOR BOTH):
-----------------------------------
Admin Panel (/admin):
  Email: {admin_email}
  Password: {admin_password}
  → Uses 'users' collection with is_admin=true
  → Password hashed with bcrypt

CMS Panel (/cms):
  Email: {admin_email}
  Password: {admin_password}
  → Uses 'admin_users' collection
  → Password hashed with bcrypt

NOTE: Admin login now uses the same user database as regular users.
Just set is_admin=true on any user to grant admin access.
""")
        
        client.close()
        return results
        
    except Exception as e:
        print(f"\n✗ ERROR: {str(e)}")
        import traceback
        traceback.print_exc()
        sys.exit(1)


def main():
    parser = argparse.ArgumentParser(
        description="Initialize FreeStays database with required data"
    )
    parser.add_argument(
        "--mongo-url",
        help="MongoDB connection URL (default: from MONGO_URL env var)"
    )
    parser.add_argument(
        "--db-name",
        help="Database name (default: from DB_NAME env var)"
    )
    parser.add_argument(
        "--admin-email",
        default=DEFAULT_ADMIN_EMAIL,
        help=f"Admin email (default: {DEFAULT_ADMIN_EMAIL})"
    )
    parser.add_argument(
        "--admin-password",
        default=DEFAULT_ADMIN_PASSWORD,
        help="Admin password"
    )
    parser.add_argument(
        "--admin-name",
        default=DEFAULT_ADMIN_NAME,
        help=f"Admin display name (default: {DEFAULT_ADMIN_NAME})"
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Overwrite existing data"
    )
    
    args = parser.parse_args()
    
    init_database(
        mongo_url=args.mongo_url,
        db_name=args.db_name,
        admin_email=args.admin_email,
        admin_password=args.admin_password,
        admin_name=args.admin_name,
        skip_if_exists=not args.force
    )


if __name__ == "__main__":
    main()
