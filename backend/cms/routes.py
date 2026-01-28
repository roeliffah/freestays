"""CMS Routes - Admin Backend API"""
from fastapi import APIRouter, HTTPException, Request, Depends, Query, BackgroundTasks
from fastapi.responses import StreamingResponse
from typing import Optional, List
from datetime import datetime, timezone, timedelta
import bcrypt
import jwt
import uuid
import csv
import io
import stripe
import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
import asyncio
import pyotp
import secrets
from concurrent.futures import ThreadPoolExecutor
from .models import (
    AdminRole, AdminUserCreate, AdminUserUpdate, AdminLogin, AdminUserResponse,
    UserFilter, UserUpdate, PassFilter, PassUpdate, PassExtend,
    PaymentFilter, RefundRequest, AuditLogEntry, DashboardStats, ReportRequest,
    # Phase 2 Models
    TwoFactorSetup, TwoFactorVerify, FraudRuleType, FraudRuleSeverity,
    FraudRuleCreate, FraudRuleUpdate, AlertType, AlertStatus, AlertCreate, AlertUpdate,
    PassValidationRequest, PassValidationLog, AdvancedAnalytics
)

cms_router = APIRouter(prefix="/api/cms", tags=["CMS"])

# Will be set from main server
db = None
JWT_SECRET = None
JWT_ALGORITHM = "HS256"
STRIPE_API_KEY = None

# CMS Admin notification email
CMS_NOTIFICATION_EMAIL = "administration@freestays.eu"

def init_cms(database, jwt_secret, stripe_key):
    """Initialize CMS with database and config"""
    global db, JWT_SECRET, STRIPE_API_KEY
    db = database
    JWT_SECRET = jwt_secret
    STRIPE_API_KEY = stripe_key
    if stripe_key:
        stripe.api_key = stripe_key


# ==================== EMAIL NOTIFICATIONS ====================

async def get_smtp_settings():
    """Get SMTP settings from database"""
    settings = await db.settings.find_one({})
    if not settings:
        return None
    return {
        "host": settings.get("smtp_host", "smtp.strato.de"),
        "port": int(settings.get("smtp_port", 587)),
        "username": settings.get("smtp_username", ""),
        "password": settings.get("smtp_password", ""),
        "from_email": settings.get("smtp_from_email", "booking@freestays.eu"),
        "from_name": settings.get("smtp_from_name", "FreeStays"),
        "enabled": settings.get("smtp_enabled", False)
    }


async def send_cms_notification(
    subject: str, 
    action: str, 
    admin_email: str, 
    details: dict,
    background_tasks: BackgroundTasks = None
):
    """Send email notification for CMS admin actions to administration@freestays.eu"""
    smtp_settings = await get_smtp_settings()
    
    if not smtp_settings or not smtp_settings.get("enabled"):
        print(f"[CMS] SMTP not enabled, skipping notification for: {action}")
        return {"success": False, "message": "SMTP not enabled"}
    
    # Build HTML email
    details_html = ""
    for key, value in details.items():
        label = key.replace("_", " ").title()
        details_html += f"""
        <tr>
            <td style="padding: 8px 15px; color: #666; font-size: 14px; border-bottom: 1px solid #eee;">{label}</td>
            <td style="padding: 8px 15px; color: #1e3a5f; font-size: 14px; font-weight: 500; border-bottom: 1px solid #eee;">{value}</td>
        </tr>
        """
    
    html_content = f"""
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
    </head>
    <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f4f4;">
        <table width="100%" cellpadding="0" cellspacing="0" style="max-width: 600px; margin: 20px auto;">
            <!-- Header -->
            <tr>
                <td style="background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); padding: 25px 30px; border-radius: 8px 8px 0 0;">
                    <table width="100%" cellpadding="0" cellspacing="0">
                        <tr>
                            <td style="vertical-align: middle;">
                                <span style="font-size: 24px; font-weight: bold; color: #ffffff;">FreeStays CMS</span>
                            </td>
                            <td style="text-align: right; vertical-align: middle;">
                                <span style="color: #a3c9f1; font-size: 12px;">Admin Notification</span>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
            
            <!-- Content -->
            <tr>
                <td style="background-color: #ffffff; padding: 30px;">
                    <h2 style="color: #1e3a5f; margin: 0 0 20px 0; font-size: 20px;">{subject}</h2>
                    
                    <div style="background-color: #f8fafc; border-radius: 8px; padding: 15px; margin-bottom: 20px;">
                        <p style="margin: 0; color: #666; font-size: 14px;">
                            <strong style="color: #1e3a5f;">Action:</strong> {action}
                        </p>
                        <p style="margin: 10px 0 0 0; color: #666; font-size: 14px;">
                            <strong style="color: #1e3a5f;">Performed by:</strong> {admin_email}
                        </p>
                        <p style="margin: 10px 0 0 0; color: #666; font-size: 14px;">
                            <strong style="color: #1e3a5f;">Time:</strong> {datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC")}
                        </p>
                    </div>
                    
                    <h3 style="color: #1e3a5f; margin: 20px 0 15px 0; font-size: 16px;">Details</h3>
                    <table width="100%" cellpadding="0" cellspacing="0" style="border: 1px solid #eee; border-radius: 8px; overflow: hidden;">
                        {details_html}
                    </table>
                </td>
            </tr>
            
            <!-- Footer -->
            <tr>
                <td style="background-color: #f8fafc; padding: 20px 30px; border-radius: 0 0 8px 8px; border-top: 1px solid #eee;">
                    <p style="margin: 0; color: #666; font-size: 12px; text-align: center;">
                        This is an automated notification from FreeStays CMS Admin Panel.
                    </p>
                    <p style="margin: 10px 0 0 0; color: #999; font-size: 11px; text-align: center;">
                        FreeStays by TravelAR Group BV | Van Haersoltelaan 19, NL - 3771 JW Barneveld
                    </p>
                </td>
            </tr>
        </table>
    </body>
    </html>
    """
    
    def send_email_sync():
        try:
            msg = MIMEMultipart("alternative")
            msg["Subject"] = f"[FreeStays CMS] {subject}"
            msg["From"] = f"{smtp_settings['from_name']} <{smtp_settings['from_email']}>"
            msg["To"] = CMS_NOTIFICATION_EMAIL
            
            msg.attach(MIMEText(html_content, "html"))
            
            with smtplib.SMTP(smtp_settings["host"], smtp_settings["port"]) as server:
                server.starttls()
                server.login(smtp_settings["username"], smtp_settings["password"])
                server.sendmail(smtp_settings["from_email"], CMS_NOTIFICATION_EMAIL, msg.as_string())
            
            print(f"[CMS] Notification sent to {CMS_NOTIFICATION_EMAIL}: {subject}")
            return {"success": True}
        except Exception as e:
            print(f"[CMS] Failed to send notification: {e}")
            return {"success": False, "error": str(e)}
    
    # Send in background to not block the response
    if background_tasks:
        background_tasks.add_task(asyncio.get_event_loop().run_in_executor, None, send_email_sync)
    else:
        loop = asyncio.get_event_loop()
        await loop.run_in_executor(None, send_email_sync)
    
    return {"success": True, "message": f"Notification queued for {CMS_NOTIFICATION_EMAIL}"}


# ==================== AUTHENTICATION ====================

async def get_current_admin(request: Request) -> dict:
    """Verify CMS admin token and return admin user"""
    auth_header = request.headers.get("Authorization")
    if not auth_header or not auth_header.startswith("Bearer "):
        raise HTTPException(status_code=401, detail="Not authenticated")
    
    token = auth_header.split(" ")[1]
    try:
        payload = jwt.decode(token, JWT_SECRET, algorithms=[JWT_ALGORITHM])
        if payload.get("type") != "cms_admin":
            raise HTTPException(status_code=401, detail="Invalid token type")
        
        admin = await db.admin_users.find_one({"admin_id": payload["admin_id"]}, {"_id": 0})
        if not admin or not admin.get("is_active"):
            raise HTTPException(status_code=401, detail="Admin account disabled")
        
        return admin
    except jwt.ExpiredSignatureError:
        raise HTTPException(status_code=401, detail="Token expired")
    except jwt.InvalidTokenError:
        raise HTTPException(status_code=401, detail="Invalid token")


def require_role(*roles: AdminRole):
    """Dependency to require specific admin roles"""
    async def check_role(admin: dict = Depends(get_current_admin)):
        if admin["role"] not in [r.value for r in roles]:
            raise HTTPException(status_code=403, detail="Insufficient permissions")
        return admin
    return check_role


async def log_audit(admin_id: str, admin_email: str, action: str, entity_type: str, 
                   entity_id: str = None, metadata: dict = None, ip_address: str = None):
    """Log admin action to audit trail"""
    log_entry = {
        "log_id": str(uuid.uuid4()),
        "admin_id": admin_id,
        "admin_email": admin_email,
        "action": action,
        "entity_type": entity_type,
        "entity_id": entity_id,
        "metadata": metadata,
        "ip_address": ip_address,
        "created_at": datetime.now(timezone.utc)
    }
    await db.audit_logs.insert_one(log_entry)


# ==================== AUTH ENDPOINTS ====================

@cms_router.post("/auth/login")
async def cms_login(credentials: AdminLogin, request: Request):
    """CMS Admin Login"""
    admin = await db.admin_users.find_one({"email": credentials.email.lower()}, {"_id": 0})
    
    if not admin:
        raise HTTPException(status_code=401, detail="Invalid credentials")
    
    if not admin.get("is_active"):
        raise HTTPException(status_code=401, detail="Account disabled")
    
    if not bcrypt.checkpw(credentials.password.encode(), admin["password_hash"].encode()):
        raise HTTPException(status_code=401, detail="Invalid credentials")
    
    # Generate JWT token
    token_payload = {
        "admin_id": admin["admin_id"],
        "email": admin["email"],
        "role": admin["role"],
        "type": "cms_admin",
        "exp": datetime.now(timezone.utc) + timedelta(hours=24)
    }
    token = jwt.encode(token_payload, JWT_SECRET, algorithm=JWT_ALGORITHM)
    
    # Update last login
    await db.admin_users.update_one(
        {"admin_id": admin["admin_id"]},
        {"$set": {"last_login": datetime.now(timezone.utc)}}
    )
    
    # Audit log
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "login", "admin", 
                   admin["admin_id"], None, client_ip)
    
    return {
        "token": token,
        "admin": {
            "admin_id": admin["admin_id"],
            "email": admin["email"],
            "role": admin["role"],
            "first_name": admin.get("first_name"),
            "last_name": admin.get("last_name")
        }
    }


@cms_router.post("/auth/logout")
async def cms_logout(request: Request, admin: dict = Depends(get_current_admin)):
    """CMS Admin Logout"""
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "logout", "admin", 
                   admin["admin_id"], None, client_ip)
    return {"message": "Logged out successfully"}


@cms_router.get("/auth/me")
async def cms_get_current_admin(admin: dict = Depends(get_current_admin)):
    """Get current admin user info"""
    return {
        "admin_id": admin["admin_id"],
        "email": admin["email"],
        "role": admin["role"],
        "first_name": admin.get("first_name"),
        "last_name": admin.get("last_name"),
        "is_active": admin.get("is_active", True)
    }


# ==================== ADMIN USER MANAGEMENT ====================

@cms_router.get("/admins")
async def list_admin_users(admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN))):
    """List all admin users (super_admin only)"""
    admins = await db.admin_users.find({}, {"_id": 0, "password_hash": 0}).to_list(100)
    return {"admins": admins}


@cms_router.post("/admins")
async def create_admin_user(
    user_data: AdminUserCreate,
    request: Request,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN))
):
    """Create new admin user (super_admin only)"""
    existing = await db.admin_users.find_one({"email": user_data.email.lower()})
    if existing:
        raise HTTPException(status_code=400, detail="Email already exists")
    
    password_hash = bcrypt.hashpw(user_data.password.encode(), bcrypt.gensalt()).decode()
    
    new_admin = {
        "admin_id": str(uuid.uuid4()),
        "email": user_data.email.lower(),
        "password_hash": password_hash,
        "role": user_data.role.value,
        "first_name": user_data.first_name,
        "last_name": user_data.last_name,
        "is_active": True,
        "created_at": datetime.now(timezone.utc),
        "last_login": None
    }
    
    await db.admin_users.insert_one(new_admin)
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "create_admin", "admin",
                   new_admin["admin_id"], {"email": new_admin["email"], "role": new_admin["role"]}, client_ip)
    
    # Send email notification
    await send_cms_notification(
        subject="New Admin User Created",
        action="create_admin",
        admin_email=admin["email"],
        details={
            "new_admin_email": new_admin["email"],
            "new_admin_name": f"{user_data.first_name} {user_data.last_name}",
            "assigned_role": user_data.role.value,
            "admin_ip": client_ip or "N/A"
        },
        background_tasks=background_tasks
    )
    
    new_admin.pop("password_hash", None)
    new_admin.pop("_id", None)
    return new_admin


@cms_router.patch("/admins/{admin_id}")
async def update_admin_user(
    admin_id: str,
    update_data: AdminUserUpdate,
    request: Request,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN))
):
    """Update admin user (super_admin only)"""
    target = await db.admin_users.find_one({"admin_id": admin_id})
    if not target:
        raise HTTPException(status_code=404, detail="Admin not found")
    
    updates = {k: v for k, v in update_data.dict().items() if v is not None}
    if "role" in updates:
        updates["role"] = updates["role"].value
    
    if updates:
        await db.admin_users.update_one({"admin_id": admin_id}, {"$set": updates})
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "update_admin", "admin",
                   admin_id, updates, client_ip)
    
    # Send notification if role changed
    if "role" in updates:
        await send_cms_notification(
            subject="Admin Role Updated",
            action="update_admin",
            admin_email=admin["email"],
            details={
                "target_admin_email": target.get("email"),
                "target_admin_name": f"{target.get('first_name', '')} {target.get('last_name', '')}".strip() or "N/A",
                "new_role": updates["role"],
                "admin_ip": client_ip or "N/A"
            },
            background_tasks=background_tasks
        )
    
    return {"message": "Admin updated successfully"}


@cms_router.delete("/admins/{admin_id}")
async def delete_admin_user(
    admin_id: str,
    request: Request,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN))
):
    """Delete admin user (super_admin only)"""
    if admin_id == admin["admin_id"]:
        raise HTTPException(status_code=400, detail="Cannot delete yourself")
    
    # Get admin info before deletion for notification
    target = await db.admin_users.find_one({"admin_id": admin_id})
    
    result = await db.admin_users.delete_one({"admin_id": admin_id})
    if result.deleted_count == 0:
        raise HTTPException(status_code=404, detail="Admin not found")
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "delete_admin", "admin",
                   admin_id, None, client_ip)
    
    # Send email notification
    if target:
        await send_cms_notification(
            subject="Admin User Deleted",
            action="delete_admin",
            admin_email=admin["email"],
            details={
                "deleted_admin_email": target.get("email", "N/A"),
                "deleted_admin_name": f"{target.get('first_name', '')} {target.get('last_name', '')}".strip() or "N/A",
                "deleted_admin_role": target.get("role", "N/A"),
                "admin_ip": client_ip or "N/A"
            },
            background_tasks=background_tasks
        )
    
    return {"message": "Admin deleted successfully"}


# ==================== DASHBOARD ====================

@cms_router.get("/dashboard/stats")
async def get_dashboard_stats(admin: dict = Depends(get_current_admin)):
    """Get dashboard statistics"""
    now = datetime.now(timezone.utc)
    month_start = now.replace(day=1, hour=0, minute=0, second=0, microsecond=0)
    
    # User stats
    total_users = await db.users.count_documents({})
    active_users = await db.users.count_documents({"is_suspended": {"$ne": True}})
    
    # Pass stats
    total_passes = await db.users.count_documents({"pass_type": {"$exists": True, "$ne": None}})
    active_passes = await db.users.count_documents({
        "pass_type": {"$exists": True, "$ne": None},
        "pass_expiry": {"$gt": now}
    })
    
    # Payment stats
    pipeline = [
        {"$match": {"status": "paid"}},
        {"$group": {"_id": None, "total": {"$sum": "$amount"}}}
    ]
    total_revenue_result = await db.payments.aggregate(pipeline).to_list(1)
    total_revenue = total_revenue_result[0]["total"] if total_revenue_result else 0
    
    pipeline_monthly = [
        {"$match": {"status": "paid", "created_at": {"$gte": month_start}}},
        {"$group": {"_id": None, "total": {"$sum": "$amount"}}}
    ]
    monthly_revenue_result = await db.payments.aggregate(pipeline_monthly).to_list(1)
    monthly_revenue = monthly_revenue_result[0]["total"] if monthly_revenue_result else 0
    
    # Booking stats
    total_bookings = await db.bookings.count_documents({})
    monthly_bookings = await db.bookings.count_documents({"created_at": {"$gte": month_start}})
    
    # Pending refunds
    pending_refunds = await db.payments.count_documents({"status": "refund_pending"})
    
    return {
        "total_users": total_users,
        "active_users": active_users,
        "total_passes": total_passes,
        "active_passes": active_passes,
        "total_revenue": round(total_revenue, 2),
        "monthly_revenue": round(monthly_revenue, 2),
        "total_bookings": total_bookings,
        "monthly_bookings": monthly_bookings,
        "pending_refunds": pending_refunds
    }


@cms_router.get("/dashboard/charts")
async def get_dashboard_charts(
    days: int = Query(30, ge=7, le=90),
    admin: dict = Depends(get_current_admin)
):
    """Get chart data for dashboard"""
    now = datetime.now(timezone.utc)
    start_date = now - timedelta(days=days)
    
    # Daily registrations
    pipeline = [
        {"$match": {"created_at": {"$gte": start_date}}},
        {"$group": {
            "_id": {"$dateToString": {"format": "%Y-%m-%d", "date": "$created_at"}},
            "count": {"$sum": 1}
        }},
        {"$sort": {"_id": 1}}
    ]
    registrations = await db.users.aggregate(pipeline).to_list(100)
    
    # Daily revenue
    pipeline_revenue = [
        {"$match": {"status": "paid", "created_at": {"$gte": start_date}}},
        {"$group": {
            "_id": {"$dateToString": {"format": "%Y-%m-%d", "date": "$created_at"}},
            "amount": {"$sum": "$amount"}
        }},
        {"$sort": {"_id": 1}}
    ]
    revenue = await db.payments.aggregate(pipeline_revenue).to_list(100)
    
    # Daily bookings
    pipeline_bookings = [
        {"$match": {"created_at": {"$gte": start_date}}},
        {"$group": {
            "_id": {"$dateToString": {"format": "%Y-%m-%d", "date": "$created_at"}},
            "count": {"$sum": 1}
        }},
        {"$sort": {"_id": 1}}
    ]
    bookings = await db.bookings.aggregate(pipeline_bookings).to_list(100)
    
    return {
        "registrations": [{"date": r["_id"], "count": r["count"]} for r in registrations],
        "revenue": [{"date": r["_id"], "amount": round(r["amount"], 2)} for r in revenue],
        "bookings": [{"date": r["_id"], "count": r["count"]} for r in bookings]
    }


# ==================== USER MANAGEMENT ====================

@cms_router.get("/users")
async def list_users(
    page: int = Query(1, ge=1),
    limit: int = Query(20, ge=1, le=100),
    search: Optional[str] = None,
    country: Optional[str] = None,
    has_pass: Optional[bool] = None,
    pass_type: Optional[str] = None,
    is_suspended: Optional[bool] = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.SUPPORT, AdminRole.READ_ONLY))
):
    """List users with pagination and filters"""
    query = {}
    
    if search:
        query["$or"] = [
            {"email": {"$regex": search, "$options": "i"}},
            {"first_name": {"$regex": search, "$options": "i"}},
            {"last_name": {"$regex": search, "$options": "i"}},
            {"user_id": {"$regex": search, "$options": "i"}}
        ]
    
    if country:
        query["country"] = country
    
    if has_pass is not None:
        if has_pass:
            query["pass_type"] = {"$exists": True, "$ne": None}
        else:
            query["$or"] = [{"pass_type": {"$exists": False}}, {"pass_type": None}]
    
    if pass_type:
        query["pass_type"] = pass_type
    
    if is_suspended is not None:
        query["is_suspended"] = is_suspended
    
    skip = (page - 1) * limit
    total = await db.users.count_documents(query)
    users = await db.users.find(query, {"_id": 0, "password_hash": 0}).skip(skip).limit(limit).to_list(limit)
    
    return {
        "users": users,
        "total": total,
        "page": page,
        "limit": limit,
        "pages": (total + limit - 1) // limit
    }


@cms_router.get("/users/{user_id}")
async def get_user_detail(
    user_id: str,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.SUPPORT, AdminRole.READ_ONLY))
):
    """Get detailed user information"""
    user = await db.users.find_one({"user_id": user_id}, {"_id": 0, "password_hash": 0})
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    
    # Get user's bookings
    bookings = await db.bookings.find({"user_id": user_id}, {"_id": 0}).sort("created_at", -1).to_list(50)
    
    # Get user's payments
    payments = await db.payments.find({"user_id": user_id}, {"_id": 0}).sort("created_at", -1).to_list(50)
    
    return {
        "user": user,
        "bookings": bookings,
        "payments": payments
    }


@cms_router.patch("/users/{user_id}")
async def update_user(
    user_id: str,
    update_data: UserUpdate,
    request: Request,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.SUPPORT))
):
    """Update user information"""
    user = await db.users.find_one({"user_id": user_id})
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    
    updates = {k: v for k, v in update_data.dict().items() if v is not None}
    updates["updated_at"] = datetime.now(timezone.utc)
    
    await db.users.update_one({"user_id": user_id}, {"$set": updates})
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "update_user", "user",
                   user_id, updates, client_ip)
    
    return {"message": "User updated successfully"}


@cms_router.post("/users/{user_id}/suspend")
async def suspend_user(
    user_id: str,
    reason: str = Query(..., min_length=1),
    request: Request = None,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.SUPPORT))
):
    """Suspend a user account"""
    user = await db.users.find_one({"user_id": user_id})
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    
    await db.users.update_one(
        {"user_id": user_id},
        {"$set": {
            "is_suspended": True,
            "suspension_reason": reason,
            "suspended_at": datetime.now(timezone.utc),
            "suspended_by": admin["admin_id"]
        }}
    )
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "suspend_user", "user",
                   user_id, {"reason": reason}, client_ip)
    
    # Send email notification
    await send_cms_notification(
        subject="User Account Suspended",
        action="suspend_user",
        admin_email=admin["email"],
        details={
            "user_email": user.get("email", "N/A"),
            "user_name": f"{user.get('first_name', '')} {user.get('last_name', '')}".strip() or "N/A",
            "user_id": user_id,
            "suspension_reason": reason,
            "admin_ip": client_ip or "N/A"
        },
        background_tasks=background_tasks
    )
    
    return {"message": "User suspended successfully"}


@cms_router.post("/users/{user_id}/reactivate")
async def reactivate_user(
    user_id: str,
    request: Request,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.SUPPORT))
):
    """Reactivate a suspended user account"""
    user = await db.users.find_one({"user_id": user_id})
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    
    await db.users.update_one(
        {"user_id": user_id},
        {"$set": {"is_suspended": False},
         "$unset": {"suspension_reason": "", "suspended_at": "", "suspended_by": ""}}
    )
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "reactivate_user", "user",
                   user_id, None, client_ip)
    
    # Send email notification
    await send_cms_notification(
        subject="User Account Reactivated",
        action="reactivate_user",
        admin_email=admin["email"],
        details={
            "user_email": user.get("email", "N/A"),
            "user_name": f"{user.get('first_name', '')} {user.get('last_name', '')}".strip() or "N/A",
            "user_id": user_id,
            "admin_ip": client_ip or "N/A"
        },
        background_tasks=background_tasks
    )
    
    return {"message": "User reactivated successfully"}


@cms_router.get("/users/{user_id}/export")
async def export_user_data(
    user_id: str,
    request: Request,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN))
):
    """Export user data (GDPR compliance)"""
    user = await db.users.find_one({"user_id": user_id}, {"_id": 0, "password_hash": 0})
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    
    bookings = await db.bookings.find({"user_id": user_id}, {"_id": 0}).to_list(1000)
    payments = await db.payments.find({"user_id": user_id}, {"_id": 0}).to_list(1000)
    
    # Create CSV
    output = io.StringIO()
    
    # User data section
    output.write("=== USER DATA ===\n")
    writer = csv.writer(output)
    writer.writerow(user.keys())
    writer.writerow([str(v) for v in user.values()])
    
    # Bookings section
    output.write("\n=== BOOKINGS ===\n")
    if bookings:
        writer.writerow(bookings[0].keys())
        for b in bookings:
            writer.writerow([str(v) for v in b.values()])
    
    # Payments section
    output.write("\n=== PAYMENTS ===\n")
    if payments:
        writer.writerow(payments[0].keys())
        for p in payments:
            writer.writerow([str(v) for v in p.values()])
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "export_user_data", "user",
                   user_id, None, client_ip)
    
    output.seek(0)
    return StreamingResponse(
        iter([output.getvalue()]),
        media_type="text/csv",
        headers={"Content-Disposition": f"attachment; filename=user_{user_id}_export.csv"}
    )


# ==================== PASS MANAGEMENT ====================

@cms_router.get("/passes")
async def list_passes(
    page: int = Query(1, ge=1),
    limit: int = Query(20, ge=1, le=100),
    search: Optional[str] = None,
    pass_type: Optional[str] = None,
    status: Optional[str] = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.SUPPORT, AdminRole.READ_ONLY))
):
    """List all passes with pagination"""
    query = {"pass_type": {"$exists": True, "$ne": None}}
    
    if search:
        query["$or"] = [
            {"email": {"$regex": search, "$options": "i"}},
            {"pass_code": {"$regex": search, "$options": "i"}},
            {"user_id": {"$regex": search, "$options": "i"}}
        ]
    
    if pass_type:
        query["pass_type"] = pass_type
    
    now = datetime.now(timezone.utc)
    if status == "active":
        query["pass_expiry"] = {"$gt": now}
    elif status == "expired":
        query["pass_expiry"] = {"$lte": now}
    
    skip = (page - 1) * limit
    total = await db.users.count_documents(query)
    
    passes = await db.users.find(
        query,
        {"_id": 0, "password_hash": 0, "password": 0}
    ).skip(skip).limit(limit).to_list(limit)
    
    # Enrich with status
    for p in passes:
        expiry = p.get("pass_expiry") or p.get("pass_expires_at")
        if expiry:
            # Handle both string and datetime formats
            if isinstance(expiry, str):
                try:
                    expiry_dt = datetime.fromisoformat(expiry.replace('Z', '+00:00'))
                except:
                    expiry_dt = None
            else:
                expiry_dt = expiry
            
            if expiry_dt:
                # Make sure both are timezone aware for comparison
                if expiry_dt.tzinfo is None:
                    expiry_dt = expiry_dt.replace(tzinfo=timezone.utc)
                p["pass_status"] = "active" if expiry_dt > now else "expired"
            else:
                p["pass_status"] = "unknown"
        else:
            p["pass_status"] = "no_expiry" if p.get("pass_type") == "one_time" else "unknown"
    
    return {
        "passes": passes,
        "total": total,
        "page": page,
        "limit": limit,
        "pages": (total + limit - 1) // limit
    }


@cms_router.post("/passes/{user_id}/activate")
async def activate_pass(
    user_id: str,
    pass_type: str = Query(..., regex="^(one_time|annual)$"),
    days: int = Query(365, ge=1, le=730),
    request: Request = None,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.SUPPORT))
):
    """Manually activate a pass for a user"""
    user = await db.users.find_one({"user_id": user_id})
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    
    now = datetime.now(timezone.utc)
    expiry = now + timedelta(days=days)
    
    await db.users.update_one(
        {"user_id": user_id},
        {"$set": {
            "pass_type": pass_type,
            "pass_expiry": expiry,
            "pass_activated_at": now,
            "pass_activated_by": admin["admin_id"]
        }}
    )
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "activate_pass", "pass",
                   user_id, {"pass_type": pass_type, "days": days}, client_ip)
    
    # Send email notification
    await send_cms_notification(
        subject="Pass Manually Activated",
        action="activate_pass",
        admin_email=admin["email"],
        details={
            "user_email": user.get("email", "N/A"),
            "user_name": f"{user.get('first_name', '')} {user.get('last_name', '')}".strip() or "N/A",
            "user_id": user_id,
            "pass_type": pass_type,
            "validity_days": days,
            "expiry_date": expiry.strftime("%Y-%m-%d"),
            "admin_ip": client_ip or "N/A"
        },
        background_tasks=background_tasks
    )
    
    return {"message": "Pass activated successfully", "expiry": expiry.isoformat()}


@cms_router.post("/passes/{user_id}/deactivate")
async def deactivate_pass(
    user_id: str,
    reason: str = Query(..., min_length=1),
    request: Request = None,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.SUPPORT))
):
    """Deactivate a user's pass"""
    user = await db.users.find_one({"user_id": user_id})
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    
    old_pass_type = user.get("pass_type", "N/A")
    old_expiry = user.get("pass_expiry", "N/A")
    
    await db.users.update_one(
        {"user_id": user_id},
        {"$set": {
            "pass_deactivated_at": datetime.now(timezone.utc),
            "pass_deactivated_by": admin["admin_id"],
            "pass_deactivation_reason": reason
        },
         "$unset": {"pass_type": "", "pass_expiry": ""}}
    )
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "deactivate_pass", "pass",
                   user_id, {"reason": reason}, client_ip)
    
    # Send email notification
    await send_cms_notification(
        subject="Pass Deactivated",
        action="deactivate_pass",
        admin_email=admin["email"],
        details={
            "user_email": user.get("email", "N/A"),
            "user_name": f"{user.get('first_name', '')} {user.get('last_name', '')}".strip() or "N/A",
            "user_id": user_id,
            "previous_pass_type": old_pass_type,
            "previous_expiry": str(old_expiry),
            "deactivation_reason": reason,
            "admin_ip": client_ip or "N/A"
        },
        background_tasks=background_tasks
    )
    
    return {"message": "Pass deactivated successfully"}


@cms_router.post("/passes/{user_id}/extend")
async def extend_pass(
    user_id: str,
    extend_data: PassExtend,
    request: Request,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.SUPPORT))
):
    """Extend a user's pass validity"""
    user = await db.users.find_one({"user_id": user_id})
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    
    if not user.get("pass_expiry"):
        raise HTTPException(status_code=400, detail="User has no active pass")
    
    current_expiry = user["pass_expiry"]
    if isinstance(current_expiry, str):
        current_expiry = datetime.fromisoformat(current_expiry.replace("Z", "+00:00"))
    
    new_expiry = current_expiry + timedelta(days=extend_data.days)
    
    await db.users.update_one(
        {"user_id": user_id},
        {"$set": {"pass_expiry": new_expiry},
         "$push": {"pass_extensions": {
             "extended_by": admin["admin_id"],
             "extended_at": datetime.now(timezone.utc),
             "days": extend_data.days,
             "reason": extend_data.reason,
             "old_expiry": current_expiry,
             "new_expiry": new_expiry
         }}}
    )
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "extend_pass", "pass",
                   user_id, {"days": extend_data.days, "reason": extend_data.reason}, client_ip)
    
    # Send email notification
    await send_cms_notification(
        subject="Pass Extended",
        action="extend_pass",
        admin_email=admin["email"],
        details={
            "user_email": user.get("email", "N/A"),
            "user_name": f"{user.get('first_name', '')} {user.get('last_name', '')}".strip() or "N/A",
            "user_id": user_id,
            "pass_type": user.get("pass_type", "N/A"),
            "days_extended": extend_data.days,
            "extension_reason": extend_data.reason,
            "old_expiry": current_expiry.strftime("%Y-%m-%d"),
            "new_expiry": new_expiry.strftime("%Y-%m-%d"),
            "admin_ip": client_ip or "N/A"
        },
        background_tasks=background_tasks
    )
    
    return {"message": "Pass extended successfully", "new_expiry": new_expiry.isoformat()}


# ==================== PAYMENT MANAGEMENT ====================

@cms_router.get("/payments")
async def list_payments(
    page: int = Query(1, ge=1),
    limit: int = Query(20, ge=1, le=100),
    search: Optional[str] = None,
    status: Optional[str] = None,
    date_from: Optional[str] = None,
    date_to: Optional[str] = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE, AdminRole.READ_ONLY))
):
    """List all payments with pagination"""
    query = {}
    
    if search:
        query["$or"] = [
            {"payment_id": {"$regex": search, "$options": "i"}},
            {"user_id": {"$regex": search, "$options": "i"}},
            {"stripe_payment_intent": {"$regex": search, "$options": "i"}}
        ]
    
    if status:
        query["status"] = status
    
    if date_from:
        query["created_at"] = {"$gte": datetime.fromisoformat(date_from)}
    if date_to:
        if "created_at" in query:
            query["created_at"]["$lte"] = datetime.fromisoformat(date_to)
        else:
            query["created_at"] = {"$lte": datetime.fromisoformat(date_to)}
    
    skip = (page - 1) * limit
    total = await db.payments.count_documents(query)
    payments = await db.payments.find(query, {"_id": 0}).sort("created_at", -1).skip(skip).limit(limit).to_list(limit)
    
    return {
        "payments": payments,
        "total": total,
        "page": page,
        "limit": limit,
        "pages": (total + limit - 1) // limit
    }


@cms_router.get("/payments/{payment_id}")
async def get_payment_detail(
    payment_id: str,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE, AdminRole.READ_ONLY))
):
    """Get detailed payment information"""
    payment = await db.payments.find_one({"payment_id": payment_id}, {"_id": 0})
    if not payment:
        raise HTTPException(status_code=404, detail="Payment not found")
    
    # Get associated user
    user = None
    if payment.get("user_id"):
        user = await db.users.find_one({"user_id": payment["user_id"]}, {"_id": 0, "password_hash": 0})
    
    return {
        "payment": payment,
        "user": user
    }


@cms_router.post("/payments/{payment_id}/refund")
async def refund_payment(
    payment_id: str,
    refund_data: RefundRequest,
    request: Request,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE))
):
    """Process a refund via Stripe"""
    payment = await db.payments.find_one({"payment_id": payment_id})
    if not payment:
        raise HTTPException(status_code=404, detail="Payment not found")
    
    if payment.get("status") == "refunded":
        raise HTTPException(status_code=400, detail="Payment already refunded")
    
    if not payment.get("stripe_payment_intent"):
        raise HTTPException(status_code=400, detail="No Stripe payment intent found")
    
    try:
        # Process refund via Stripe
        refund_amount = refund_data.amount if refund_data.amount else None
        
        refund = stripe.Refund.create(
            payment_intent=payment["stripe_payment_intent"],
            amount=int(refund_amount * 100) if refund_amount else None,
            reason="requested_by_customer"
        )
        
        # Update payment record
        await db.payments.update_one(
            {"payment_id": payment_id},
            {"$set": {
                "status": "refunded" if not refund_amount else "partially_refunded",
                "refund_id": refund.id,
                "refund_amount": refund_amount or payment.get("amount"),
                "refund_reason": refund_data.reason,
                "refunded_at": datetime.now(timezone.utc),
                "refunded_by": admin["admin_id"]
            }}
        )
        
        # If full refund, deactivate pass
        if not refund_amount and payment.get("user_id"):
            await db.users.update_one(
                {"user_id": payment["user_id"]},
                {"$unset": {"pass_type": "", "pass_expiry": ""}}
            )
        
        client_ip = request.client.host if request.client else None
        await log_audit(admin["admin_id"], admin["email"], "refund_payment", "payment",
                       payment_id, {"amount": refund_amount, "reason": refund_data.reason}, client_ip)
        
        # Send email notification for refund
        await send_cms_notification(
            subject="Payment Refunded",
            action="refund_payment",
            admin_email=admin["email"],
            details={
                "payment_id": payment_id,
                "user_email": payment.get("user_email", "N/A"),
                "original_amount": f"€{payment.get('amount', 0):.2f}",
                "refund_amount": f"€{(refund_amount or payment.get('amount', 0)):.2f}",
                "refund_type": "Full Refund" if not refund_amount else "Partial Refund",
                "refund_reason": refund_data.reason,
                "stripe_refund_id": refund.id,
                "admin_ip": client_ip or "N/A"
            },
            background_tasks=background_tasks
        )
        
        return {"message": "Refund processed successfully", "refund_id": refund.id}
        
    except stripe.error.StripeError as e:
        raise HTTPException(status_code=400, detail=f"Stripe error: {str(e)}")


@cms_router.get("/payments/export")
async def export_payments(
    date_from: Optional[str] = None,
    date_to: Optional[str] = None,
    status: Optional[str] = None,
    request: Request = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE))
):
    """Export payments to CSV"""
    query = {}
    if status:
        query["status"] = status
    if date_from:
        query["created_at"] = {"$gte": datetime.fromisoformat(date_from)}
    if date_to:
        if "created_at" in query:
            query["created_at"]["$lte"] = datetime.fromisoformat(date_to)
        else:
            query["created_at"] = {"$lte": datetime.fromisoformat(date_to)}
    
    payments = await db.payments.find(query, {"_id": 0}).to_list(10000)
    
    output = io.StringIO()
    if payments:
        writer = csv.DictWriter(output, fieldnames=payments[0].keys())
        writer.writeheader()
        for p in payments:
            writer.writerow({k: str(v) for k, v in p.items()})
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "export_payments", "payment",
                   None, {"count": len(payments)}, client_ip)
    
    output.seek(0)
    return StreamingResponse(
        iter([output.getvalue()]),
        media_type="text/csv",
        headers={"Content-Disposition": f"attachment; filename=payments_export_{datetime.now().strftime('%Y%m%d')}.csv"}
    )


# ==================== AUDIT LOGS ====================

@cms_router.get("/audit-logs")
async def list_audit_logs(
    page: int = Query(1, ge=1),
    limit: int = Query(50, ge=1, le=200),
    admin_id: Optional[str] = None,
    action: Optional[str] = None,
    entity_type: Optional[str] = None,
    date_from: Optional[str] = None,
    date_to: Optional[str] = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN))
):
    """List audit logs with pagination"""
    query = {}
    
    if admin_id:
        query["admin_id"] = admin_id
    if action:
        query["action"] = action
    if entity_type:
        query["entity_type"] = entity_type
    
    if date_from:
        query["created_at"] = {"$gte": datetime.fromisoformat(date_from)}
    if date_to:
        if "created_at" in query:
            query["created_at"]["$lte"] = datetime.fromisoformat(date_to)
        else:
            query["created_at"] = {"$lte": datetime.fromisoformat(date_to)}
    
    skip = (page - 1) * limit
    total = await db.audit_logs.count_documents(query)
    logs = await db.audit_logs.find(query, {"_id": 0}).sort("created_at", -1).skip(skip).limit(limit).to_list(limit)
    
    return {
        "logs": logs,
        "total": total,
        "page": page,
        "limit": limit,
        "pages": (total + limit - 1) // limit
    }


@cms_router.get("/audit-logs/export")
async def export_audit_logs(
    date_from: Optional[str] = None,
    date_to: Optional[str] = None,
    request: Request = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN))
):
    """Export audit logs to CSV"""
    query = {}
    if date_from:
        query["created_at"] = {"$gte": datetime.fromisoformat(date_from)}
    if date_to:
        if "created_at" in query:
            query["created_at"]["$lte"] = datetime.fromisoformat(date_to)
        else:
            query["created_at"] = {"$lte": datetime.fromisoformat(date_to)}
    
    logs = await db.audit_logs.find(query, {"_id": 0}).to_list(50000)
    
    output = io.StringIO()
    if logs:
        writer = csv.DictWriter(output, fieldnames=logs[0].keys())
        writer.writeheader()
        for log in logs:
            writer.writerow({k: str(v) for k, v in log.items()})
    
    output.seek(0)
    return StreamingResponse(
        iter([output.getvalue()]),
        media_type="text/csv",
        headers={"Content-Disposition": f"attachment; filename=audit_logs_{datetime.now().strftime('%Y%m%d')}.csv"}
    )


# ==================== REPORTS ====================

@cms_router.get("/reports/users")
async def report_users(
    period: str = Query("month", regex="^(week|month|quarter|year)$"),
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.READ_ONLY))
):
    """Generate user report"""
    now = datetime.now(timezone.utc)
    
    if period == "week":
        start_date = now - timedelta(days=7)
    elif period == "month":
        start_date = now - timedelta(days=30)
    elif period == "quarter":
        start_date = now - timedelta(days=90)
    else:
        start_date = now - timedelta(days=365)
    
    # New users
    new_users = await db.users.count_documents({"created_at": {"$gte": start_date}})
    
    # Users by country
    pipeline = [
        {"$match": {"created_at": {"$gte": start_date}}},
        {"$group": {"_id": "$country", "count": {"$sum": 1}}},
        {"$sort": {"count": -1}},
        {"$limit": 10}
    ]
    by_country = await db.users.aggregate(pipeline).to_list(10)
    
    # Pass distribution
    pipeline_passes = [
        {"$match": {"pass_type": {"$exists": True, "$ne": None}}},
        {"$group": {"_id": "$pass_type", "count": {"$sum": 1}}}
    ]
    pass_distribution = await db.users.aggregate(pipeline_passes).to_list(10)
    
    return {
        "period": period,
        "new_users": new_users,
        "by_country": [{"country": r["_id"] or "Unknown", "count": r["count"]} for r in by_country],
        "pass_distribution": [{"type": r["_id"], "count": r["count"]} for r in pass_distribution]
    }


@cms_router.get("/reports/revenue")
async def report_revenue(
    period: str = Query("month", regex="^(week|month|quarter|year)$"),
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE, AdminRole.READ_ONLY))
):
    """Generate revenue report"""
    now = datetime.now(timezone.utc)
    
    if period == "week":
        start_date = now - timedelta(days=7)
    elif period == "month":
        start_date = now - timedelta(days=30)
    elif period == "quarter":
        start_date = now - timedelta(days=90)
    else:
        start_date = now - timedelta(days=365)
    
    # Total revenue
    pipeline = [
        {"$match": {"status": "paid", "created_at": {"$gte": start_date}}},
        {"$group": {"_id": None, "total": {"$sum": "$amount"}, "count": {"$sum": 1}}}
    ]
    revenue_result = await db.payments.aggregate(pipeline).to_list(1)
    total_revenue = revenue_result[0]["total"] if revenue_result else 0
    payment_count = revenue_result[0]["count"] if revenue_result else 0
    
    # Revenue by pass type
    pipeline_by_type = [
        {"$match": {"status": "paid", "created_at": {"$gte": start_date}}},
        {"$group": {"_id": "$pass_type", "total": {"$sum": "$amount"}, "count": {"$sum": 1}}}
    ]
    by_type = await db.payments.aggregate(pipeline_by_type).to_list(10)
    
    # Refunds
    pipeline_refunds = [
        {"$match": {"status": {"$in": ["refunded", "partially_refunded"]}, "refunded_at": {"$gte": start_date}}},
        {"$group": {"_id": None, "total": {"$sum": "$refund_amount"}, "count": {"$sum": 1}}}
    ]
    refunds_result = await db.payments.aggregate(pipeline_refunds).to_list(1)
    total_refunds = refunds_result[0]["total"] if refunds_result else 0
    refund_count = refunds_result[0]["count"] if refunds_result else 0
    
    return {
        "period": period,
        "total_revenue": round(total_revenue, 2),
        "payment_count": payment_count,
        "by_type": [{"type": r["_id"] or "Unknown", "total": round(r["total"], 2), "count": r["count"]} for r in by_type],
        "total_refunds": round(total_refunds, 2),
        "refund_count": refund_count,
        "net_revenue": round(total_revenue - total_refunds, 2)
    }


@cms_router.get("/reports/bookings")
async def report_bookings(
    period: str = Query("month", regex="^(week|month|quarter|year)$"),
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.READ_ONLY))
):
    """Generate bookings report"""
    now = datetime.now(timezone.utc)
    
    if period == "week":
        start_date = now - timedelta(days=7)
    elif period == "month":
        start_date = now - timedelta(days=30)
    elif period == "quarter":
        start_date = now - timedelta(days=90)
    else:
        start_date = now - timedelta(days=365)
    
    # Total bookings
    total_bookings = await db.bookings.count_documents({"created_at": {"$gte": start_date}})
    
    # Bookings by status
    pipeline_status = [
        {"$match": {"created_at": {"$gte": start_date}}},
        {"$group": {"_id": "$status", "count": {"$sum": 1}}}
    ]
    by_status = await db.bookings.aggregate(pipeline_status).to_list(10)
    
    # Average booking value
    pipeline_value = [
        {"$match": {"created_at": {"$gte": start_date}}},
        {"$group": {"_id": None, "avg": {"$avg": "$total_price"}, "total": {"$sum": "$total_price"}}}
    ]
    value_result = await db.bookings.aggregate(pipeline_value).to_list(1)
    avg_value = value_result[0]["avg"] if value_result else 0
    total_value = value_result[0]["total"] if value_result else 0
    
    return {
        "period": period,
        "total_bookings": total_bookings,
        "by_status": [{"status": r["_id"] or "Unknown", "count": r["count"]} for r in by_status],
        "average_value": round(avg_value, 2) if avg_value else 0,
        "total_value": round(total_value, 2) if total_value else 0
    }


# ==================== PHASE 2: TWO-FACTOR AUTHENTICATION ====================

@cms_router.post("/auth/2fa/setup")
async def setup_2fa(
    setup_data: TwoFactorSetup,
    request: Request,
    admin: dict = Depends(get_current_admin)
):
    """Setup 2FA for admin account"""
    if admin.get("two_factor_enabled"):
        raise HTTPException(status_code=400, detail="2FA is already enabled")
    
    # Generate TOTP secret
    secret = pyotp.random_base32()
    totp = pyotp.TOTP(secret)
    
    # Store temporary secret (not enabled until verified)
    await db.admin_users.update_one(
        {"admin_id": admin["admin_id"]},
        {"$set": {
            "two_factor_temp_secret": secret,
            "two_factor_setup_at": datetime.now(timezone.utc)
        }}
    )
    
    # Generate provisioning URI for authenticator apps
    provisioning_uri = totp.provisioning_uri(
        name=admin["email"],
        issuer_name="FreeStays CMS"
    )
    
    return {
        "secret": secret,
        "provisioning_uri": provisioning_uri,
        "message": "Scan the QR code with your authenticator app, then verify with a code"
    }


@cms_router.post("/auth/2fa/verify")
async def verify_2fa_setup(
    verify_data: TwoFactorVerify,
    request: Request,
    admin: dict = Depends(get_current_admin)
):
    """Verify 2FA setup with TOTP code"""
    admin_user = await db.admin_users.find_one({"admin_id": admin["admin_id"]})
    
    temp_secret = admin_user.get("two_factor_temp_secret")
    if not temp_secret:
        raise HTTPException(status_code=400, detail="No 2FA setup in progress")
    
    totp = pyotp.TOTP(temp_secret)
    if not totp.verify(verify_data.code):
        raise HTTPException(status_code=400, detail="Invalid verification code")
    
    # Generate backup codes
    backup_codes = [secrets.token_hex(4).upper() for _ in range(8)]
    hashed_backups = [bcrypt.hashpw(c.encode(), bcrypt.gensalt()).decode() for c in backup_codes]
    
    # Enable 2FA
    await db.admin_users.update_one(
        {"admin_id": admin["admin_id"]},
        {"$set": {
            "two_factor_enabled": True,
            "two_factor_secret": temp_secret,
            "two_factor_backup_codes": hashed_backups,
            "two_factor_enabled_at": datetime.now(timezone.utc)
        },
        "$unset": {"two_factor_temp_secret": "", "two_factor_setup_at": ""}}
    )
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "enable_2fa", "admin",
                   admin["admin_id"], None, client_ip)
    
    return {
        "message": "2FA enabled successfully",
        "backup_codes": backup_codes,
        "warning": "Save these backup codes securely. They cannot be shown again."
    }


@cms_router.post("/auth/2fa/validate")
async def validate_2fa_code(
    verify_data: TwoFactorVerify,
    admin_id: str = Query(...),
):
    """Validate 2FA code during login"""
    admin_user = await db.admin_users.find_one({"admin_id": admin_id})
    if not admin_user or not admin_user.get("two_factor_enabled"):
        raise HTTPException(status_code=400, detail="2FA not enabled for this account")
    
    secret = admin_user.get("two_factor_secret")
    totp = pyotp.TOTP(secret)
    
    # Try TOTP code first
    if totp.verify(verify_data.code, valid_window=1):
        return {"valid": True, "method": "totp"}
    
    # Try backup codes
    backup_codes = admin_user.get("two_factor_backup_codes", [])
    for i, hashed_code in enumerate(backup_codes):
        if bcrypt.checkpw(verify_data.code.upper().encode(), hashed_code.encode()):
            # Remove used backup code
            backup_codes.pop(i)
            await db.admin_users.update_one(
                {"admin_id": admin_id},
                {"$set": {"two_factor_backup_codes": backup_codes}}
            )
            return {"valid": True, "method": "backup_code", "remaining_backups": len(backup_codes)}
    
    return {"valid": False, "message": "Invalid 2FA code"}


@cms_router.delete("/auth/2fa")
async def disable_2fa(
    request: Request,
    password: str = Query(..., min_length=1),
    admin: dict = Depends(get_current_admin)
):
    """Disable 2FA for admin account"""
    admin_user = await db.admin_users.find_one({"admin_id": admin["admin_id"]})
    
    # Verify password
    if not bcrypt.checkpw(password.encode(), admin_user["password_hash"].encode()):
        raise HTTPException(status_code=401, detail="Invalid password")
    
    await db.admin_users.update_one(
        {"admin_id": admin["admin_id"]},
        {"$unset": {
            "two_factor_enabled": "",
            "two_factor_secret": "",
            "two_factor_backup_codes": "",
            "two_factor_enabled_at": ""
        }}
    )
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "disable_2fa", "admin",
                   admin["admin_id"], None, client_ip)
    
    return {"message": "2FA disabled successfully"}


# ==================== PHASE 2: FRAUD DETECTION RULES ====================

@cms_router.get("/fraud-rules")
async def list_fraud_rules(
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN))
):
    """List all fraud detection rules"""
    rules = await db.fraud_rules.find({}, {"_id": 0}).to_list(100)
    return {"rules": rules, "total": len(rules)}


@cms_router.post("/fraud-rules")
async def create_fraud_rule(
    rule_data: FraudRuleCreate,
    request: Request,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN))
):
    """Create a new fraud detection rule"""
    rule = {
        "rule_id": str(uuid.uuid4()),
        "name": rule_data.name,
        "rule_type": rule_data.rule_type.value,
        "description": rule_data.description,
        "is_active": rule_data.is_active,
        "severity": rule_data.severity.value,
        "threshold": rule_data.threshold,
        "time_window_hours": rule_data.time_window_hours,
        "auto_suspend": rule_data.auto_suspend,
        "notify_admin": rule_data.notify_admin,
        "created_at": datetime.now(timezone.utc),
        "created_by": admin["admin_id"],
        "triggers_count": 0
    }
    
    await db.fraud_rules.insert_one(rule)
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "create_fraud_rule", "fraud_rule",
                   rule["rule_id"], {"name": rule["name"], "type": rule["rule_type"]}, client_ip)
    
    rule.pop("_id", None)
    return rule


@cms_router.patch("/fraud-rules/{rule_id}")
async def update_fraud_rule(
    rule_id: str,
    update_data: FraudRuleUpdate,
    request: Request,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN))
):
    """Update a fraud detection rule"""
    rule = await db.fraud_rules.find_one({"rule_id": rule_id})
    if not rule:
        raise HTTPException(status_code=404, detail="Rule not found")
    
    updates = {k: v.value if hasattr(v, 'value') else v for k, v in update_data.dict().items() if v is not None}
    if updates:
        updates["updated_at"] = datetime.now(timezone.utc)
        updates["updated_by"] = admin["admin_id"]
        await db.fraud_rules.update_one({"rule_id": rule_id}, {"$set": updates})
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "update_fraud_rule", "fraud_rule",
                   rule_id, updates, client_ip)
    
    return {"message": "Rule updated successfully"}


@cms_router.delete("/fraud-rules/{rule_id}")
async def delete_fraud_rule(
    rule_id: str,
    request: Request,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN))
):
    """Delete a fraud detection rule"""
    result = await db.fraud_rules.delete_one({"rule_id": rule_id})
    if result.deleted_count == 0:
        raise HTTPException(status_code=404, detail="Rule not found")
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "delete_fraud_rule", "fraud_rule",
                   rule_id, None, client_ip)
    
    return {"message": "Rule deleted successfully"}


# ==================== PHASE 2: ALERTS SYSTEM ====================

@cms_router.get("/alerts")
async def list_alerts(
    page: int = Query(1, ge=1),
    limit: int = Query(20, ge=1, le=100),
    status: Optional[str] = None,
    severity: Optional[str] = None,
    alert_type: Optional[str] = None,
    admin: dict = Depends(get_current_admin)
):
    """List all alerts with pagination and filters"""
    query = {}
    if status:
        query["status"] = status
    if severity:
        query["severity"] = severity
    if alert_type:
        query["alert_type"] = alert_type
    
    skip = (page - 1) * limit
    total = await db.alerts.count_documents(query)
    alerts = await db.alerts.find(query, {"_id": 0}).sort("created_at", -1).skip(skip).limit(limit).to_list(limit)
    
    # Count by status for dashboard
    new_count = await db.alerts.count_documents({"status": "new"})
    
    return {
        "alerts": alerts,
        "total": total,
        "new_count": new_count,
        "page": page,
        "limit": limit,
        "pages": (total + limit - 1) // limit
    }


@cms_router.post("/alerts")
async def create_alert(
    alert_data: AlertCreate,
    request: Request,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.SUPPORT))
):
    """Manually create an alert"""
    alert = {
        "alert_id": str(uuid.uuid4()),
        "alert_type": alert_data.alert_type.value,
        "title": alert_data.title,
        "message": alert_data.message,
        "severity": alert_data.severity.value,
        "status": AlertStatus.NEW.value,
        "entity_type": alert_data.entity_type,
        "entity_id": alert_data.entity_id,
        "metadata": alert_data.metadata,
        "created_at": datetime.now(timezone.utc),
        "created_by": admin["admin_id"]
    }
    
    await db.alerts.insert_one(alert)
    
    # Send notification if high severity
    if alert_data.severity in [FraudRuleSeverity.HIGH, FraudRuleSeverity.CRITICAL]:
        await send_cms_notification(
            subject=f"[{alert_data.severity.value.upper()}] {alert_data.title}",
            action="alert_created",
            admin_email=admin["email"],
            details={
                "alert_type": alert_data.alert_type.value,
                "severity": alert_data.severity.value,
                "message": alert_data.message[:200],
                "entity": f"{alert_data.entity_type}: {alert_data.entity_id}" if alert_data.entity_type else "N/A"
            },
            background_tasks=background_tasks
        )
    
    alert.pop("_id", None)
    return alert


@cms_router.patch("/alerts/{alert_id}")
async def update_alert(
    alert_id: str,
    update_data: AlertUpdate,
    request: Request,
    admin: dict = Depends(get_current_admin)
):
    """Update alert status"""
    alert = await db.alerts.find_one({"alert_id": alert_id})
    if not alert:
        raise HTTPException(status_code=404, detail="Alert not found")
    
    updates = {
        "status": update_data.status.value,
        "updated_at": datetime.now(timezone.utc),
        "updated_by": admin["admin_id"]
    }
    
    if update_data.resolution_note:
        updates["resolution_note"] = update_data.resolution_note
    
    if update_data.status == AlertStatus.RESOLVED:
        updates["resolved_at"] = datetime.now(timezone.utc)
        updates["resolved_by"] = admin["admin_id"]
    
    await db.alerts.update_one({"alert_id": alert_id}, {"$set": updates})
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "update_alert", "alert",
                   alert_id, {"status": update_data.status.value}, client_ip)
    
    return {"message": "Alert updated successfully"}


@cms_router.post("/alerts/{alert_id}/acknowledge")
async def acknowledge_alert(
    alert_id: str,
    request: Request,
    admin: dict = Depends(get_current_admin)
):
    """Quick acknowledge an alert"""
    result = await db.alerts.update_one(
        {"alert_id": alert_id, "status": "new"},
        {"$set": {
            "status": "acknowledged",
            "acknowledged_at": datetime.now(timezone.utc),
            "acknowledged_by": admin["admin_id"]
        }}
    )
    
    if result.modified_count == 0:
        raise HTTPException(status_code=400, detail="Alert not found or already acknowledged")
    
    return {"message": "Alert acknowledged"}


# ==================== PHASE 2: ENHANCED PASS VALIDATION ====================

async def check_fraud_rules(user_id: str, action_type: str, metadata: dict = None):
    """Check active fraud rules and create alerts if triggered"""
    now = datetime.now(timezone.utc)
    
    # Get active rules for this action type
    rules = await db.fraud_rules.find({
        "is_active": True,
        "rule_type": action_type
    }).to_list(100)
    
    for rule in rules:
        time_window = now - timedelta(hours=rule.get("time_window_hours", 24))
        threshold = rule.get("threshold", 3)
        
        # Count occurrences in time window
        if action_type == "rapid_pass_usage":
            count = await db.pass_validation_logs.count_documents({
                "user_id": user_id,
                "created_at": {"$gte": time_window},
                "validation_result": True
            })
        elif action_type == "invalid_pass_attempts":
            count = await db.pass_validation_logs.count_documents({
                "pass_code": metadata.get("pass_code") if metadata else None,
                "created_at": {"$gte": time_window},
                "validation_result": False
            })
        elif action_type == "multiple_refunds":
            count = await db.payments.count_documents({
                "user_id": user_id,
                "status": {"$in": ["refunded", "partially_refunded"]},
                "refunded_at": {"$gte": time_window}
            })
        else:
            count = 0
        
        # Check if threshold exceeded
        if count >= threshold:
            # Create alert
            alert = {
                "alert_id": str(uuid.uuid4()),
                "alert_type": AlertType.FRAUD_DETECTED.value,
                "title": f"Fraud Rule Triggered: {rule['name']}",
                "message": f"User {user_id} triggered fraud rule '{rule['name']}' with {count} occurrences in {rule['time_window_hours']}h (threshold: {threshold})",
                "severity": rule.get("severity", "medium"),
                "status": AlertStatus.NEW.value,
                "entity_type": "user",
                "entity_id": user_id,
                "metadata": {
                    "rule_id": rule["rule_id"],
                    "rule_name": rule["name"],
                    "occurrences": count,
                    "threshold": threshold,
                    "action_data": metadata
                },
                "created_at": now
            }
            await db.alerts.insert_one(alert)
            
            # Increment rule trigger count
            await db.fraud_rules.update_one(
                {"rule_id": rule["rule_id"]},
                {"$inc": {"triggers_count": 1}, "$set": {"last_triggered": now}}
            )
            
            # Auto-suspend if configured
            if rule.get("auto_suspend"):
                await db.users.update_one(
                    {"user_id": user_id},
                    {"$set": {
                        "is_suspended": True,
                        "suspension_reason": f"Auto-suspended by fraud rule: {rule['name']}",
                        "suspended_at": now
                    }}
                )


@cms_router.post("/pass/validate-enhanced")
async def validate_pass_enhanced(
    validation: PassValidationRequest,
    request: Request,
    background_tasks: BackgroundTasks = None
):
    """Enhanced pass validation with fraud detection and logging"""
    code_upper = validation.pass_code.upper()
    now = datetime.now(timezone.utc)
    client_ip = validation.user_ip or (request.client.host if request.client else None)
    
    # Log validation attempt
    log_entry = {
        "log_id": str(uuid.uuid4()),
        "pass_code": code_upper,
        "ip_address": client_ip,
        "user_agent": validation.user_agent,
        "booking_amount": validation.booking_amount,
        "validation_type": "enhanced_api",
        "created_at": now
    }
    
    # Check admin-generated codes
    admin_code = await db.pass_codes.find_one({"code": code_upper})
    if admin_code:
        if admin_code.get("status") == "used":
            log_entry.update({"validation_result": False, "fail_reason": "already_used"})
            await db.pass_validation_logs.insert_one(log_entry)
            return {"valid": False, "has_discount": False, "message": "Pass code already used", "fraud_check": "passed"}
        
        expires_at = admin_code.get("expires_at")
        if expires_at:
            if isinstance(expires_at, str):
                expires_at = datetime.fromisoformat(expires_at.replace('Z', '+00:00'))
            if expires_at < now:
                log_entry.update({"validation_result": False, "fail_reason": "expired"})
                await db.pass_validation_logs.insert_one(log_entry)
                return {"valid": False, "has_discount": False, "message": "Pass code expired", "fraud_check": "passed"}
        
        log_entry.update({"validation_result": True, "user_id": admin_code.get("assigned_to")})
        await db.pass_validation_logs.insert_one(log_entry)
        
        # Check fraud rules
        if admin_code.get("assigned_to"):
            await check_fraud_rules(admin_code["assigned_to"], "rapid_pass_usage")
        
        return {
            "valid": True,
            "has_discount": True,
            "pass_type": admin_code.get("pass_type", "one_time"),
            "discount_rate": 0.15,
            "code_source": "admin_generated",
            "expires_at": admin_code.get("expires_at"),
            "fraud_check": "passed",
            "message": f"Valid {admin_code.get('pass_type', 'one_time').replace('_', ' ')} pass!"
        }
    
    # Check user pass codes
    user_with_code = await db.users.find_one({"pass_code": code_upper})
    if user_with_code:
        user_id = user_with_code.get("user_id")
        pass_type = user_with_code.get("pass_type", "free")
        expires_at = user_with_code.get("pass_expires_at") or user_with_code.get("pass_expiry")
        
        # Check suspension
        if user_with_code.get("is_suspended"):
            log_entry.update({"validation_result": False, "fail_reason": "user_suspended", "user_id": user_id})
            await db.pass_validation_logs.insert_one(log_entry)
            return {"valid": False, "has_discount": False, "message": "Account suspended", "fraud_check": "flagged"}
        
        # Check expiration
        if pass_type != "free" and expires_at:
            if isinstance(expires_at, str):
                expires_at_dt = datetime.fromisoformat(expires_at.replace('Z', '+00:00'))
            else:
                expires_at_dt = expires_at
            if expires_at_dt < now:
                log_entry.update({"validation_result": False, "fail_reason": "expired", "user_id": user_id})
                await db.pass_validation_logs.insert_one(log_entry)
                return {"valid": False, "has_discount": False, "message": "Pass expired", "fraud_check": "passed"}
        
        log_entry.update({"validation_result": True, "user_id": user_id})
        await db.pass_validation_logs.insert_one(log_entry)
        
        # Check fraud rules
        await check_fraud_rules(user_id, "rapid_pass_usage")
        
        if pass_type == "free":
            return {
                "valid": True,
                "has_discount": False,
                "pass_type": "free",
                "fraud_check": "passed",
                "message": "Valid free account. Upgrade for discounts!"
            }
        
        return {
            "valid": True,
            "has_discount": True,
            "pass_type": pass_type,
            "discount_rate": 0.15,
            "code_source": "user_account",
            "expires_at": expires_at,
            "fraud_check": "passed",
            "message": f"Valid {pass_type} pass! Discounts applied."
        }
    
    # Invalid code - log and check fraud
    log_entry.update({"validation_result": False, "fail_reason": "not_found"})
    await db.pass_validation_logs.insert_one(log_entry)
    
    # Check for invalid attempt fraud
    await check_fraud_rules(None, "invalid_pass_attempts", {"pass_code": code_upper, "ip": client_ip})
    
    return {"valid": False, "has_discount": False, "message": "Invalid pass code", "fraud_check": "logged"}


@cms_router.get("/pass/validation-logs")
async def get_validation_logs(
    page: int = Query(1, ge=1),
    limit: int = Query(50, ge=1, le=200),
    pass_code: Optional[str] = None,
    user_id: Optional[str] = None,
    result: Optional[bool] = None,
    date_from: Optional[str] = None,
    date_to: Optional[str] = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.SUPPORT))
):
    """Get pass validation logs"""
    query = {}
    if pass_code:
        query["pass_code"] = pass_code.upper()
    if user_id:
        query["user_id"] = user_id
    if result is not None:
        query["validation_result"] = result
    if date_from:
        query["created_at"] = {"$gte": datetime.fromisoformat(date_from)}
    if date_to:
        if "created_at" in query:
            query["created_at"]["$lte"] = datetime.fromisoformat(date_to)
        else:
            query["created_at"] = {"$lte": datetime.fromisoformat(date_to)}
    
    skip = (page - 1) * limit
    total = await db.pass_validation_logs.count_documents(query)
    logs = await db.pass_validation_logs.find(query, {"_id": 0}).sort("created_at", -1).skip(skip).limit(limit).to_list(limit)
    
    return {
        "logs": logs,
        "total": total,
        "page": page,
        "limit": limit,
        "pages": (total + limit - 1) // limit
    }


# ==================== PHASE 2: ADVANCED ANALYTICS ====================

@cms_router.get("/analytics/revenue-trend")
async def get_revenue_trend(
    period: str = Query("daily", regex="^(daily|weekly|monthly)$"),
    days: int = Query(30, ge=7, le=365),
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE, AdminRole.READ_ONLY))
):
    """Get revenue trend with comparison to previous period"""
    now = datetime.now(timezone.utc)
    start_date = now - timedelta(days=days)
    prev_start = start_date - timedelta(days=days)
    
    # Current period revenue by day
    if period == "daily":
        group_format = "%Y-%m-%d"
    elif period == "weekly":
        group_format = "%Y-W%V"
    else:
        group_format = "%Y-%m"
    
    pipeline = [
        {"$match": {"created_at": {"$gte": start_date}, "status": "paid"}},
        {"$group": {
            "_id": {"$dateToString": {"format": group_format, "date": "$created_at"}},
            "revenue": {"$sum": "$amount"},
            "count": {"$sum": 1}
        }},
        {"$sort": {"_id": 1}}
    ]
    current_data = await db.payments.aggregate(pipeline).to_list(400)
    
    # Previous period for comparison
    pipeline[0]["$match"]["created_at"] = {"$gte": prev_start, "$lt": start_date}
    prev_data = await db.payments.aggregate(pipeline).to_list(400)
    
    current_total = sum(d["revenue"] for d in current_data)
    prev_total = sum(d["revenue"] for d in prev_data)
    growth = ((current_total - prev_total) / prev_total * 100) if prev_total > 0 else 0
    
    return {
        "period": period,
        "days": days,
        "data_points": [{"date": d["_id"], "revenue": round(d["revenue"], 2), "transactions": d["count"]} for d in current_data],
        "current_total": round(current_total, 2),
        "previous_total": round(prev_total, 2),
        "growth_percentage": round(growth, 1),
        "insights": [
            f"Revenue {'increased' if growth >= 0 else 'decreased'} by {abs(round(growth, 1))}% compared to previous period",
            f"Average daily revenue: €{round(current_total / days, 2)}" if days > 0 else ""
        ]
    }


@cms_router.get("/analytics/user-growth")
async def get_user_growth(
    period: str = Query("daily", regex="^(daily|weekly|monthly)$"),
    days: int = Query(30, ge=7, le=365),
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.READ_ONLY))
):
    """Get user growth analytics"""
    now = datetime.now(timezone.utc)
    start_date = now - timedelta(days=days)
    
    if period == "daily":
        group_format = "%Y-%m-%d"
    elif period == "weekly":
        group_format = "%Y-W%V"
    else:
        group_format = "%Y-%m"
    
    # User registrations
    pipeline = [
        {"$match": {"created_at": {"$gte": start_date}}},
        {"$group": {
            "_id": {"$dateToString": {"format": group_format, "date": "$created_at"}},
            "new_users": {"$sum": 1},
            "with_pass": {"$sum": {"$cond": [{"$ne": ["$pass_type", "free"]}, 1, 0]}}
        }},
        {"$sort": {"_id": 1}}
    ]
    data = await db.users.aggregate(pipeline).to_list(400)
    
    total_new = sum(d["new_users"] for d in data)
    total_with_pass = sum(d["with_pass"] for d in data)
    conversion_rate = (total_with_pass / total_new * 100) if total_new > 0 else 0
    
    # Active users (logged in recently)
    active_users = await db.users.count_documents({
        "last_login": {"$gte": now - timedelta(days=30)}
    })
    
    return {
        "period": period,
        "days": days,
        "data_points": [{"date": d["_id"], "new_users": d["new_users"], "with_pass": d["with_pass"]} for d in data],
        "total_new_users": total_new,
        "total_with_pass": total_with_pass,
        "conversion_rate": round(conversion_rate, 1),
        "active_users_30d": active_users,
        "insights": [
            f"{total_new} new users registered in the past {days} days",
            f"Pass conversion rate: {round(conversion_rate, 1)}%",
            f"{active_users} users active in the last 30 days"
        ]
    }


@cms_router.get("/analytics/pass-performance")
async def get_pass_performance(
    days: int = Query(30, ge=7, le=365),
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE, AdminRole.READ_ONLY))
):
    """Get pass sales and usage performance"""
    now = datetime.now(timezone.utc)
    start_date = now - timedelta(days=days)
    
    # Pass sales by type
    pipeline = [
        {"$match": {"created_at": {"$gte": start_date}, "pass_type": {"$in": ["one_time", "annual"]}}},
        {"$group": {
            "_id": "$pass_type",
            "count": {"$sum": 1},
            "revenue": {"$sum": {"$cond": [{"$eq": ["$pass_type", "annual"]}, 129, 35]}}
        }}
    ]
    sales_by_type = await db.payments.aggregate(pipeline).to_list(10)
    
    # Active passes count
    active_passes = await db.users.count_documents({
        "pass_type": {"$in": ["one_time", "annual"]},
        "pass_expiry": {"$gt": now}
    })
    
    # Pass usage (bookings with pass discount)
    pass_usage = await db.bookings.count_documents({
        "created_at": {"$gte": start_date},
        "pass_discount_applied": True
    })
    
    # Expiring soon
    expiring_soon = await db.users.count_documents({
        "pass_type": {"$in": ["one_time", "annual"]},
        "pass_expiry": {"$gt": now, "$lt": now + timedelta(days=30)}
    })
    
    return {
        "days": days,
        "sales_by_type": [{"type": s["_id"], "count": s["count"], "revenue": s["revenue"]} for s in sales_by_type],
        "active_passes": active_passes,
        "pass_bookings": pass_usage,
        "expiring_in_30_days": expiring_soon,
        "insights": [
            f"{active_passes} active passes currently",
            f"{pass_usage} bookings used pass discounts in the past {days} days",
            f"{expiring_soon} passes expiring in the next 30 days - renewal opportunity"
        ]
    }


@cms_router.get("/analytics/booking-funnel")
async def get_booking_funnel(
    days: int = Query(30, ge=7, le=365),
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.READ_ONLY))
):
    """Get booking funnel analytics"""
    now = datetime.now(timezone.utc)
    start_date = now - timedelta(days=days)
    
    # Search count (from logs if available)
    searches = await db.search_logs.count_documents({"created_at": {"$gte": start_date}}) if await db.list_collection_names() and "search_logs" in await db.list_collection_names() else 0
    
    # Prebook count
    prebooks = await db.prebooks.count_documents({"created_at": {"$gte": start_date}}) if "prebooks" in await db.list_collection_names() else 0
    
    # Completed bookings
    completed = await db.bookings.count_documents({
        "created_at": {"$gte": start_date},
        "status": {"$in": ["confirmed", "completed"]}
    })
    
    # Cancelled/Failed
    cancelled = await db.bookings.count_documents({
        "created_at": {"$gte": start_date},
        "status": {"$in": ["cancelled", "failed"]}
    })
    
    # Calculate conversion rates
    search_to_prebook = (prebooks / searches * 100) if searches > 0 else 0
    prebook_to_booking = (completed / prebooks * 100) if prebooks > 0 else 0
    overall_conversion = (completed / searches * 100) if searches > 0 else 0
    
    return {
        "days": days,
        "funnel": {
            "searches": searches,
            "prebooks": prebooks,
            "completed_bookings": completed,
            "cancelled": cancelled
        },
        "conversion_rates": {
            "search_to_prebook": round(search_to_prebook, 2),
            "prebook_to_booking": round(prebook_to_booking, 2),
            "overall": round(overall_conversion, 2)
        },
        "insights": [
            f"{completed} bookings completed in the past {days} days",
            f"Overall conversion rate: {round(overall_conversion, 2)}%",
            f"{cancelled} bookings were cancelled or failed"
        ]
    }


# ==================== DAILY SUMMARY REPORT ====================

async def generate_daily_summary_report():
    """Generate daily summary data for the report email"""
    now = datetime.now(timezone.utc)
    yesterday = now - timedelta(days=1)
    yesterday_start = yesterday.replace(hour=0, minute=0, second=0, microsecond=0)
    today_start = now.replace(hour=0, minute=0, second=0, microsecond=0)
    
    # New registrations yesterday
    new_users = await db.users.count_documents({
        "created_at": {"$gte": yesterday_start, "$lt": today_start}
    })
    
    # New users with pass
    new_users_with_pass = await db.users.count_documents({
        "created_at": {"$gte": yesterday_start, "$lt": today_start},
        "pass_type": {"$in": ["one_time", "annual"]}
    })
    
    # Revenue yesterday
    revenue_pipeline = [
        {"$match": {"created_at": {"$gte": yesterday_start, "$lt": today_start}, "status": "paid"}},
        {"$group": {"_id": None, "total": {"$sum": "$amount"}, "count": {"$sum": 1}}}
    ]
    revenue_result = await db.payments.aggregate(revenue_pipeline).to_list(1)
    yesterday_revenue = revenue_result[0]["total"] if revenue_result else 0
    yesterday_transactions = revenue_result[0]["count"] if revenue_result else 0
    
    # Bookings yesterday
    new_bookings = await db.bookings.count_documents({
        "created_at": {"$gte": yesterday_start, "$lt": today_start}
    })
    
    # Confirmed bookings
    confirmed_bookings = await db.bookings.count_documents({
        "created_at": {"$gte": yesterday_start, "$lt": today_start},
        "status": "confirmed"
    })
    
    # Fraud alerts yesterday
    fraud_alerts = await db.alerts.count_documents({
        "created_at": {"$gte": yesterday_start, "$lt": today_start},
        "alert_type": "fraud_detected"
    })
    
    # Unresolved alerts
    unresolved_alerts = await db.alerts.count_documents({
        "status": {"$in": ["new", "acknowledged"]}
    })
    
    # Passes expiring in next 7 days
    week_from_now = now + timedelta(days=7)
    expiring_passes = await db.users.count_documents({
        "pass_type": {"$in": ["one_time", "annual"]},
        "pass_expiry": {"$gt": now, "$lt": week_from_now}
    })
    
    # Passes expiring in next 30 days
    month_from_now = now + timedelta(days=30)
    expiring_passes_30d = await db.users.count_documents({
        "pass_type": {"$in": ["one_time", "annual"]},
        "pass_expiry": {"$gt": now, "$lt": month_from_now}
    })
    
    # Active passes total
    active_passes = await db.users.count_documents({
        "pass_type": {"$in": ["one_time", "annual"]},
        "pass_expiry": {"$gt": now}
    })
    
    # Total users
    total_users = await db.users.count_documents({})
    
    # Week-over-week comparison
    last_week_start = yesterday_start - timedelta(days=7)
    last_week_end = today_start - timedelta(days=7)
    last_week_users = await db.users.count_documents({
        "created_at": {"$gte": last_week_start, "$lt": last_week_end}
    })
    user_growth = ((new_users - last_week_users) / last_week_users * 100) if last_week_users > 0 else 0
    
    # Refunds yesterday
    refunds = await db.payments.count_documents({
        "refunded_at": {"$gte": yesterday_start, "$lt": today_start}
    })
    
    return {
        "date": yesterday.strftime("%Y-%m-%d"),
        "new_users": new_users,
        "new_users_with_pass": new_users_with_pass,
        "user_growth_wow": round(user_growth, 1),
        "total_users": total_users,
        "revenue": round(yesterday_revenue, 2),
        "transactions": yesterday_transactions,
        "new_bookings": new_bookings,
        "confirmed_bookings": confirmed_bookings,
        "fraud_alerts": fraud_alerts,
        "unresolved_alerts": unresolved_alerts,
        "expiring_passes_7d": expiring_passes,
        "expiring_passes_30d": expiring_passes_30d,
        "active_passes": active_passes,
        "refunds": refunds
    }


async def send_daily_summary_email():
    """Send daily summary report email to administration@freestays.eu"""
    smtp_settings = await get_smtp_settings()
    
    if not smtp_settings or not smtp_settings.get("enabled"):
        print("[CMS] SMTP not enabled, skipping daily summary report")
        return {"success": False, "message": "SMTP not enabled"}
    
    try:
        data = await generate_daily_summary_report()
    except Exception as e:
        print(f"[CMS] Failed to generate daily summary: {e}")
        return {"success": False, "error": str(e)}
    
    # Generate HTML email
    html_content = f"""
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
    </head>
    <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f4f4;">
        <table width="100%" cellpadding="0" cellspacing="0" style="max-width: 600px; margin: 20px auto;">
            <!-- Header -->
            <tr>
                <td style="background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); padding: 25px 30px; border-radius: 8px 8px 0 0;">
                    <table width="100%" cellpadding="0" cellspacing="0">
                        <tr>
                            <td style="vertical-align: middle;">
                                <span style="font-size: 24px; font-weight: bold; color: #ffffff;">FreeStays</span>
                            </td>
                            <td style="text-align: right; vertical-align: middle;">
                                <span style="color: #a3c9f1; font-size: 14px;">Daily Summary Report</span>
                            </td>
                        </tr>
                    </table>
                    <h1 style="color: #ffffff; margin: 20px 0 0 0; font-size: 20px; font-weight: 600; text-align: center;">
                        📊 Daily Report - {data['date']}
                    </h1>
                </td>
            </tr>
            
            <!-- Content -->
            <tr>
                <td style="background-color: #ffffff; padding: 30px;">
                    
                    <!-- User Stats -->
                    <div style="margin-bottom: 25px;">
                        <h2 style="color: #1e3a5f; margin: 0 0 15px 0; font-size: 16px; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px;">
                            👥 User Activity
                        </h2>
                        <table width="100%" cellpadding="8" cellspacing="0" style="background-color: #f8fafc; border-radius: 8px;">
                            <tr>
                                <td style="color: #666; width: 60%;">New Registrations</td>
                                <td style="color: #1e3a5f; font-weight: bold; text-align: right;">{data['new_users']}</td>
                            </tr>
                            <tr>
                                <td style="color: #666;">New Users with Pass</td>
                                <td style="color: #10b981; font-weight: bold; text-align: right;">{data['new_users_with_pass']}</td>
                            </tr>
                            <tr>
                                <td style="color: #666;">Week-over-Week Growth</td>
                                <td style="color: {'#10b981' if data['user_growth_wow'] >= 0 else '#ef4444'}; font-weight: bold; text-align: right;">
                                    {'+' if data['user_growth_wow'] >= 0 else ''}{data['user_growth_wow']}%
                                </td>
                            </tr>
                            <tr>
                                <td style="color: #666;">Total Users</td>
                                <td style="color: #1e3a5f; font-weight: bold; text-align: right;">{data['total_users']:,}</td>
                            </tr>
                        </table>
                    </div>
                    
                    <!-- Revenue Stats -->
                    <div style="margin-bottom: 25px;">
                        <h2 style="color: #1e3a5f; margin: 0 0 15px 0; font-size: 16px; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px;">
                            💰 Revenue
                        </h2>
                        <table width="100%" cellpadding="8" cellspacing="0" style="background-color: #f0fdf4; border-radius: 8px;">
                            <tr>
                                <td style="color: #666; width: 60%;">Yesterday's Revenue</td>
                                <td style="color: #10b981; font-weight: bold; font-size: 18px; text-align: right;">€{data['revenue']:,.2f}</td>
                            </tr>
                            <tr>
                                <td style="color: #666;">Transactions</td>
                                <td style="color: #1e3a5f; font-weight: bold; text-align: right;">{data['transactions']}</td>
                            </tr>
                            <tr>
                                <td style="color: #666;">Refunds Processed</td>
                                <td style="color: #f59e0b; font-weight: bold; text-align: right;">{data['refunds']}</td>
                            </tr>
                        </table>
                    </div>
                    
                    <!-- Bookings -->
                    <div style="margin-bottom: 25px;">
                        <h2 style="color: #1e3a5f; margin: 0 0 15px 0; font-size: 16px; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px;">
                            🏨 Bookings
                        </h2>
                        <table width="100%" cellpadding="8" cellspacing="0" style="background-color: #f8fafc; border-radius: 8px;">
                            <tr>
                                <td style="color: #666; width: 60%;">New Bookings</td>
                                <td style="color: #1e3a5f; font-weight: bold; text-align: right;">{data['new_bookings']}</td>
                            </tr>
                            <tr>
                                <td style="color: #666;">Confirmed</td>
                                <td style="color: #10b981; font-weight: bold; text-align: right;">{data['confirmed_bookings']}</td>
                            </tr>
                        </table>
                    </div>
                    
                    <!-- Alerts -->
                    {'<div style="margin-bottom: 25px; background-color: #fef2f2; border-radius: 8px; padding: 15px; border-left: 4px solid #ef4444;">' if data['fraud_alerts'] > 0 or data['unresolved_alerts'] > 0 else '<div style="margin-bottom: 25px;">'}
                        <h2 style="color: #1e3a5f; margin: 0 0 15px 0; font-size: 16px;">
                            🚨 Alerts {'- Action Required!' if data['unresolved_alerts'] > 0 else ''}
                        </h2>
                        <table width="100%" cellpadding="8" cellspacing="0">
                            <tr>
                                <td style="color: #666; width: 60%;">Fraud Alerts (Yesterday)</td>
                                <td style="color: {'#ef4444' if data['fraud_alerts'] > 0 else '#10b981'}; font-weight: bold; text-align: right;">{data['fraud_alerts']}</td>
                            </tr>
                            <tr>
                                <td style="color: #666;">Unresolved Alerts</td>
                                <td style="color: {'#ef4444' if data['unresolved_alerts'] > 0 else '#10b981'}; font-weight: bold; text-align: right;">{data['unresolved_alerts']}</td>
                            </tr>
                        </table>
                    </div>
                    
                    <!-- Pass Stats -->
                    <div style="margin-bottom: 25px;">
                        <h2 style="color: #1e3a5f; margin: 0 0 15px 0; font-size: 16px; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px;">
                            🎫 FreeStays Passes
                        </h2>
                        <table width="100%" cellpadding="8" cellspacing="0" style="background-color: #f8fafc; border-radius: 8px;">
                            <tr>
                                <td style="color: #666; width: 60%;">Active Passes</td>
                                <td style="color: #1e3a5f; font-weight: bold; text-align: right;">{data['active_passes']}</td>
                            </tr>
                            <tr>
                                <td style="color: {'#f59e0b' if data['expiring_passes_7d'] > 0 else '#666'};">Expiring in 7 Days</td>
                                <td style="color: {'#f59e0b' if data['expiring_passes_7d'] > 0 else '#1e3a5f'}; font-weight: bold; text-align: right;">{data['expiring_passes_7d']}</td>
                            </tr>
                            <tr>
                                <td style="color: #666;">Expiring in 30 Days</td>
                                <td style="color: #1e3a5f; font-weight: bold; text-align: right;">{data['expiring_passes_30d']}</td>
                            </tr>
                        </table>
                    </div>
                    
                    <!-- CTA -->
                    <div style="text-align: center; margin-top: 25px;">
                        <a href="https://freestays.eu/cms/dashboard" 
                           style="display: inline-block; background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); 
                                  color: white; text-decoration: none; padding: 12px 30px; border-radius: 8px; font-weight: 600;">
                            View CMS Dashboard →
                        </a>
                    </div>
                </td>
            </tr>
            
            <!-- Footer -->
            <tr>
                <td style="background-color: #f8fafc; padding: 20px 30px; border-radius: 0 0 8px 8px; border-top: 1px solid #eee;">
                    <p style="margin: 0; color: #666; font-size: 12px; text-align: center;">
                        This is an automated daily report from FreeStays CMS.
                    </p>
                    <p style="margin: 10px 0 0 0; color: #999; font-size: 11px; text-align: center;">
                        FreeStays by TravelAR Group BV | Van Haersoltelaan 19, NL - 3771 JW Barneveld
                    </p>
                </td>
            </tr>
        </table>
    </body>
    </html>
    """
    
    def send_email_sync():
        try:
            msg = MIMEMultipart("alternative")
            msg["Subject"] = f"📊 FreeStays Daily Summary - {data['date']}"
            msg["From"] = f"{smtp_settings['from_name']} <{smtp_settings['from_email']}>"
            msg["To"] = CMS_NOTIFICATION_EMAIL
            
            msg.attach(MIMEText(html_content, "html"))
            
            with smtplib.SMTP(smtp_settings["host"], smtp_settings["port"]) as server:
                server.starttls()
                server.login(smtp_settings["username"], smtp_settings["password"])
                server.sendmail(smtp_settings["from_email"], CMS_NOTIFICATION_EMAIL, msg.as_string())
            
            print(f"[CMS] Daily summary report sent to {CMS_NOTIFICATION_EMAIL}")
            return {"success": True}
        except Exception as e:
            print(f"[CMS] Failed to send daily summary: {e}")
            return {"success": False, "error": str(e)}
    
    loop = asyncio.get_event_loop()
    result = await loop.run_in_executor(None, send_email_sync)
    return result


@cms_router.get("/reports/daily-summary")
async def get_daily_summary(
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.READ_ONLY))
):
    """Get daily summary data (same data as email report)"""
    data = await generate_daily_summary_report()
    return data


@cms_router.post("/reports/send-daily-summary")
async def trigger_daily_summary(
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN))
):
    """Manually trigger daily summary email"""
    result = await send_daily_summary_email()
    if result.get("success"):
        return {"message": f"Daily summary sent to {CMS_NOTIFICATION_EMAIL}"}
    else:
        raise HTTPException(status_code=500, detail=result.get("error", "Failed to send email"))


# ==================== STRIPE SYNC ====================

@cms_router.post("/sync/stripe")
async def sync_stripe_data(
    request: Request,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE))
):
    """Sync payment data from Stripe - retrieves recent payments and updates local records"""
    if not STRIPE_API_KEY:
        raise HTTPException(status_code=500, detail="Stripe not configured")
    
    client_ip = request.client.host if request.client else None
    synced_count = 0
    updated_count = 0
    enriched_count = 0
    errors = []
    
    # Helper function to lookup user by email
    async def find_user_by_email(email: str):
        if not email:
            return None
        user = await db.users.find_one({"email": email.lower()}, {"_id": 0, "user_id": 1, "email": 1, "name": 1})
        return user
    
    try:
        # Retrieve recent Stripe payments (last 30 days)
        thirty_days_ago = int((datetime.now(timezone.utc) - timedelta(days=30)).timestamp())
        
        # Get payment intents
        payment_intents = stripe.PaymentIntent.list(
            created={"gte": thirty_days_ago},
            limit=100,
            expand=["data.customer"]  # Expand customer to get email
        )
        
        for pi in payment_intents.data:
            try:
                # Try to find user email from multiple sources
                user_email = ""
                user_id = ""
                
                # 1. Check metadata first (if we stored it during checkout)
                metadata = pi.metadata or {}
                user_email = metadata.get("user_email", "")
                user_id = metadata.get("user_id", "")
                
                # 2. If no metadata, try receipt_email
                if not user_email and pi.receipt_email:
                    user_email = pi.receipt_email
                
                # 3. If still no email, try customer object
                if not user_email and pi.customer:
                    if isinstance(pi.customer, str):
                        # Customer is just an ID, need to fetch
                        try:
                            customer = stripe.Customer.retrieve(pi.customer)
                            user_email = customer.email or ""
                        except:
                            pass
                    elif hasattr(pi.customer, 'email'):
                        user_email = pi.customer.email or ""
                
                # 4. Lookup user in our database by email
                if user_email and not user_id:
                    user = await find_user_by_email(user_email)
                    if user:
                        user_id = user.get("user_id", "")
                
                # Check if we have this payment in our database
                existing = await db.payments.find_one({"stripe_payment_intent": pi.id})
                
                if existing:
                    # Update status if changed, and enrich with user data if missing
                    new_status = "paid" if pi.status == "succeeded" else pi.status
                    update_data = {"synced_at": datetime.now(timezone.utc)}
                    
                    if existing.get("status") != new_status:
                        update_data["status"] = new_status
                    
                    # Enrich with user data if we found it and it's missing
                    if user_email and not existing.get("user_email"):
                        update_data["user_email"] = user_email
                        enriched_count += 1
                    if user_id and not existing.get("user_id"):
                        update_data["user_id"] = user_id
                    
                    if len(update_data) > 1:  # More than just synced_at
                        await db.payments.update_one(
                            {"stripe_payment_intent": pi.id},
                            {"$set": update_data}
                        )
                        updated_count += 1
                else:
                    # Create new payment record
                    payment = {
                        "payment_id": str(uuid.uuid4()),
                        "stripe_payment_intent": pi.id,
                        "amount": pi.amount / 100,  # Convert from cents
                        "currency": pi.currency.upper(),
                        "status": "paid" if pi.status == "succeeded" else pi.status,
                        "user_email": user_email,
                        "user_id": user_id,
                        "pass_type": metadata.get("pass_type", ""),
                        "created_at": datetime.fromtimestamp(pi.created, tz=timezone.utc),
                        "synced_at": datetime.now(timezone.utc),
                        "source": "stripe_sync"
                    }
                    await db.payments.insert_one(payment)
                    synced_count += 1
                    if user_email:
                        enriched_count += 1
                    
            except Exception as e:
                errors.append(f"Error processing payment {pi.id}: {str(e)}")
        
        # Also check recent checkout sessions for additional email data
        sessions = stripe.checkout.Session.list(
            created={"gte": thirty_days_ago},
            limit=100
        )
        
        for session in sessions.data:
            if session.payment_status == "paid" and session.payment_intent:
                existing = await db.payments.find_one({"stripe_payment_intent": session.payment_intent})
                if existing:
                    update_data = {}
                    
                    # Add session ID if missing
                    if not existing.get("stripe_session_id"):
                        update_data["stripe_session_id"] = session.id
                    
                    # Enrich with customer_email if missing
                    if not existing.get("user_email") and session.customer_email:
                        update_data["user_email"] = session.customer_email
                        # Try to find user
                        user = await find_user_by_email(session.customer_email)
                        if user:
                            update_data["user_id"] = user.get("user_id", "")
                        enriched_count += 1
                    
                    if update_data:
                        await db.payments.update_one(
                            {"stripe_payment_intent": session.payment_intent},
                            {"$set": update_data}
                        )
        
        # Log the sync
        await log_audit(admin["admin_id"], admin["email"], "stripe_sync", "system",
                       None, {"synced": synced_count, "updated": updated_count, "enriched": enriched_count, "errors": len(errors)}, client_ip)
        
        return {
            "success": True,
            "synced": synced_count,
            "updated": updated_count,
            "enriched": enriched_count,
            "message": f"Synced {synced_count} new payments, updated {updated_count}, enriched {enriched_count} with user data",
            "errors": errors[:5] if errors else [],
            "total_errors": len(errors)
        }
        
    except stripe.error.StripeError as e:
        raise HTTPException(status_code=500, detail=f"Stripe error: {str(e)}")


@cms_router.get("/sync/stripe/status")
async def get_stripe_sync_status(
    admin: dict = Depends(get_current_admin)
):
    """Get last Stripe sync status"""
    # Get last sync from audit log
    last_sync = await db.cms_audit_logs.find_one(
        {"action": "stripe_sync"},
        sort=[("created_at", -1)]
    )
    
    if last_sync:
        return {
            "last_sync": last_sync.get("created_at"),
            "synced_by": last_sync.get("admin_email"),
            "details": last_sync.get("details", {})
        }
    
    return {"last_sync": None, "message": "No sync performed yet"}


# ==================== B2B PASS MANAGEMENT ====================

# Default B2B pricing (can be overridden)
B2B_PRICING = {
    "one_time": 28.00,  # Discounted from €35
    "annual": 99.00     # Discounted from €129
}

@cms_router.get("/b2b/orders")
async def list_b2b_orders(
    page: int = Query(1, ge=1),
    limit: int = Query(20, ge=1, le=100),
    status: Optional[str] = None,
    search: Optional[str] = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE))
):
    """List all B2B pass orders"""
    query = {}
    if status:
        query["status"] = status
    if search:
        query["$or"] = [
            {"business_name": {"$regex": search, "$options": "i"}},
            {"business_email": {"$regex": search, "$options": "i"}},
            {"invoice_number": {"$regex": search, "$options": "i"}}
        ]
    
    skip = (page - 1) * limit
    total = await db.b2b_orders.count_documents(query)
    orders = await db.b2b_orders.find(query, {"_id": 0}).sort("created_at", -1).skip(skip).limit(limit).to_list(limit)
    
    return {
        "orders": orders,
        "total": total,
        "page": page,
        "limit": limit,
        "pages": (total + limit - 1) // limit
    }


@cms_router.post("/b2b/orders")
async def create_b2b_order(
    order_data: dict,
    request: Request,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE))
):
    """Create a new B2B pass order"""
    pass_type = order_data.get("pass_type", "one_time")
    quantity = order_data.get("quantity", 1)
    unit_price = order_data.get("unit_price") or B2B_PRICING.get(pass_type, 28.00)
    total_price = unit_price * quantity
    
    order = {
        "order_id": str(uuid.uuid4()),
        "business_name": order_data.get("business_name"),
        "business_address": order_data.get("business_address"),
        "business_email": order_data.get("business_email"),
        "business_phone": order_data.get("business_phone"),
        "vat_number": order_data.get("vat_number"),
        "pass_type": pass_type,
        "quantity": quantity,
        "unit_price": unit_price,
        "total_price": total_price,
        "status": "pending",
        "invoice_number": None,
        "notes": order_data.get("notes"),
        "created_at": datetime.now(timezone.utc),
        "created_by": admin["admin_id"],
        "passes_generated": False
    }
    
    await db.b2b_orders.insert_one(order)
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "create_b2b_order", "b2b",
                   order["order_id"], {"business": order["business_name"], "quantity": quantity}, client_ip)
    
    order.pop("_id", None)
    return order


@cms_router.patch("/b2b/orders/{order_id}")
async def update_b2b_order(
    order_id: str,
    update_data: dict,
    request: Request,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE))
):
    """Update a B2B order"""
    order = await db.b2b_orders.find_one({"order_id": order_id})
    if not order:
        raise HTTPException(status_code=404, detail="Order not found")
    
    updates = {k: v for k, v in update_data.items() if v is not None}
    updates["updated_at"] = datetime.now(timezone.utc)
    updates["updated_by"] = admin["admin_id"]
    
    await db.b2b_orders.update_one({"order_id": order_id}, {"$set": updates})
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "update_b2b_order", "b2b",
                   order_id, updates, client_ip)
    
    return {"message": "Order updated successfully"}


@cms_router.delete("/b2b/orders/{order_id}")
async def delete_b2b_order(
    order_id: str,
    request: Request,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE))
):
    """Delete a B2B order (only pending orders can be deleted)"""
    order = await db.b2b_orders.find_one({"order_id": order_id})
    if not order:
        raise HTTPException(status_code=404, detail="Order not found")
    
    if order.get("status") != "pending":
        raise HTTPException(status_code=400, detail="Only pending orders can be deleted")
    
    await db.b2b_orders.delete_one({"order_id": order_id})
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "delete_b2b_order", "b2b",
                   order_id, {"business_name": order.get("business_name")}, client_ip)
    
    return {"message": "Order deleted successfully"}


@cms_router.post("/b2b/orders/{order_id}/generate-invoice")
async def generate_b2b_invoice(
    order_id: str,
    request: Request,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE))
):
    """Generate invoice for B2B order"""
    order = await db.b2b_orders.find_one({"order_id": order_id})
    if not order:
        raise HTTPException(status_code=404, detail="Order not found")
    
    if order.get("invoice_number"):
        raise HTTPException(status_code=400, detail="Invoice already generated")
    
    # Generate invoice number
    year = datetime.now().year
    count = await db.b2b_orders.count_documents({"invoice_number": {"$regex": f"^INV-{year}"}})
    invoice_number = f"INV-{year}-{str(count + 1).zfill(4)}"
    
    await db.b2b_orders.update_one(
        {"order_id": order_id},
        {"$set": {
            "invoice_number": invoice_number,
            "invoice_date": datetime.now(timezone.utc),
            "due_date": datetime.now(timezone.utc) + timedelta(days=30),
            "status": "invoiced"
        }}
    )
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "generate_b2b_invoice", "b2b",
                   order_id, {"invoice_number": invoice_number}, client_ip)
    
    return {"invoice_number": invoice_number, "message": "Invoice generated"}


@cms_router.post("/b2b/orders/{order_id}/mark-paid")
async def mark_b2b_order_paid(
    order_id: str,
    request: Request,
    background_tasks: BackgroundTasks = None,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE))
):
    """Mark B2B order as paid and generate passes"""
    order = await db.b2b_orders.find_one({"order_id": order_id})
    if not order:
        raise HTTPException(status_code=404, detail="Order not found")
    
    if order.get("status") == "paid":
        raise HTTPException(status_code=400, detail="Order already marked as paid")
    
    # Generate passes
    pass_codes = []
    for i in range(order.get("quantity", 1)):
        code = f"B2B-{order['pass_type'][:3].upper()}-{str(uuid.uuid4())[:8].upper()}"
        pass_codes.append({
            "code": code,
            "pass_type": order["pass_type"],
            "status": "active",
            "expires_at": datetime.now(timezone.utc) + timedelta(days=365 if order["pass_type"] == "annual" else 365),
            "b2b_order_id": order_id,
            "business_name": order["business_name"],
            "created_at": datetime.now(timezone.utc)
        })
    
    # Store pass codes
    if pass_codes:
        await db.pass_codes.insert_many(pass_codes)
    
    # Update order
    await db.b2b_orders.update_one(
        {"order_id": order_id},
        {"$set": {
            "status": "paid",
            "payment_date": datetime.now(timezone.utc),
            "passes_generated": True,
            "pass_codes": [p["code"] for p in pass_codes]
        }}
    )
    
    # Also record in payments
    payment = {
        "payment_id": str(uuid.uuid4()),
        "amount": order["total_price"],
        "currency": "EUR",
        "status": "paid",
        "payment_method": "b2b_invoice",
        "user_email": order["business_email"],
        "b2b_order_id": order_id,
        "business_name": order["business_name"],
        "invoice_number": order.get("invoice_number"),
        "created_at": datetime.now(timezone.utc)
    }
    await db.payments.insert_one(payment)
    
    client_ip = request.client.host if request.client else None
    await log_audit(admin["admin_id"], admin["email"], "mark_b2b_paid", "b2b",
                   order_id, {"passes_generated": len(pass_codes)}, client_ip)
    
    # Send notification
    await send_cms_notification(
        subject="B2B Order Paid - Passes Generated",
        action="b2b_order_paid",
        admin_email=admin["email"],
        details={
            "order_id": order_id,
            "business_name": order["business_name"],
            "quantity": order["quantity"],
            "total": f"€{order['total_price']:.2f}",
            "passes_generated": len(pass_codes)
        },
        background_tasks=background_tasks
    )
    
    return {
        "message": "Order marked as paid",
        "passes_generated": len(pass_codes),
        "pass_codes": [p["code"] for p in pass_codes]
    }


@cms_router.get("/b2b/orders/{order_id}")
async def get_b2b_order(
    order_id: str,
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE))
):
    """Get B2B order details"""
    order = await db.b2b_orders.find_one({"order_id": order_id}, {"_id": 0})
    if not order:
        raise HTTPException(status_code=404, detail="Order not found")
    
    # Get pass codes if generated
    if order.get("passes_generated"):
        pass_codes = await db.pass_codes.find(
            {"b2b_order_id": order_id}, 
            {"_id": 0}
        ).to_list(1000)
        order["pass_codes_detail"] = pass_codes
    
    return order


@cms_router.get("/b2b/stats")
async def get_b2b_stats(
    admin: dict = Depends(require_role(AdminRole.SUPER_ADMIN, AdminRole.FINANCE))
):
    """Get B2B statistics"""
    total_orders = await db.b2b_orders.count_documents({})
    pending_orders = await db.b2b_orders.count_documents({"status": "pending"})
    invoiced_orders = await db.b2b_orders.count_documents({"status": "invoiced"})
    paid_orders = await db.b2b_orders.count_documents({"status": "paid"})
    
    # Revenue
    pipeline = [
        {"$match": {"status": "paid"}},
        {"$group": {"_id": None, "total": {"$sum": "$total_price"}, "passes": {"$sum": "$quantity"}}}
    ]
    revenue_result = await db.b2b_orders.aggregate(pipeline).to_list(1)
    total_revenue = revenue_result[0]["total"] if revenue_result else 0
    total_passes = revenue_result[0]["passes"] if revenue_result else 0
    
    return {
        "total_orders": total_orders,
        "pending_orders": pending_orders,
        "invoiced_orders": invoiced_orders,
        "paid_orders": paid_orders,
        "total_revenue": round(total_revenue, 2),
        "total_passes_sold": total_passes
    }


# ==================== B2B API FOR MANAGERS ====================

@cms_router.post("/b2b/request")
async def request_b2b_order(
    order_data: dict,
    request: Request
):
    """Allow managers to request B2B pass order (no auth required for initial request)"""
    # Validate required fields
    required = ["business_name", "business_address", "business_email", "business_phone", "vat_number", "pass_type", "quantity"]
    for field in required:
        if not order_data.get(field):
            raise HTTPException(status_code=400, detail=f"Missing required field: {field}")
    
    pass_type = order_data["pass_type"]
    quantity = int(order_data["quantity"])
    unit_price = B2B_PRICING.get(pass_type, 28.00)
    
    order = {
        "order_id": str(uuid.uuid4()),
        "business_name": order_data["business_name"],
        "business_address": order_data["business_address"],
        "business_email": order_data["business_email"],
        "business_phone": order_data["business_phone"],
        "vat_number": order_data["vat_number"],
        "pass_type": pass_type,
        "quantity": quantity,
        "unit_price": unit_price,
        "total_price": unit_price * quantity,
        "status": "pending",
        "notes": order_data.get("notes"),
        "created_at": datetime.now(timezone.utc),
        "source": "manager_request"
    }
    
    await db.b2b_orders.insert_one(order)
    
    order.pop("_id", None)
    return {
        "message": "B2B order request submitted",
        "order_id": order["order_id"],
        "total_price": order["total_price"],
        "status": "pending"
    }
