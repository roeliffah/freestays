"""CMS Pydantic Models"""
from pydantic import BaseModel, EmailStr, Field
from typing import Optional, List, Dict, Any
from datetime import datetime
from enum import Enum


class AdminRole(str, Enum):
    SUPER_ADMIN = "super_admin"
    SUPPORT = "support"
    FINANCE = "finance"
    READ_ONLY = "read_only"


class AdminUserCreate(BaseModel):
    email: EmailStr
    password: str
    role: AdminRole = AdminRole.READ_ONLY
    first_name: Optional[str] = None
    last_name: Optional[str] = None


class AdminUserUpdate(BaseModel):
    role: Optional[AdminRole] = None
    is_active: Optional[bool] = None
    first_name: Optional[str] = None
    last_name: Optional[str] = None


class AdminLogin(BaseModel):
    email: EmailStr
    password: str


class AdminUserResponse(BaseModel):
    admin_id: str
    email: str
    role: AdminRole
    first_name: Optional[str] = None
    last_name: Optional[str] = None
    is_active: bool = True
    created_at: datetime
    last_login: Optional[datetime] = None


class UserFilter(BaseModel):
    search: Optional[str] = None
    country: Optional[str] = None
    has_pass: Optional[bool] = None
    pass_type: Optional[str] = None
    is_active: Optional[bool] = None
    date_from: Optional[str] = None
    date_to: Optional[str] = None


class UserUpdate(BaseModel):
    first_name: Optional[str] = None
    last_name: Optional[str] = None
    phone: Optional[str] = None
    country: Optional[str] = None
    city: Optional[str] = None
    address: Optional[str] = None
    postal_code: Optional[str] = None
    is_suspended: Optional[bool] = None
    suspension_reason: Optional[str] = None


class PassFilter(BaseModel):
    search: Optional[str] = None
    pass_type: Optional[str] = None
    status: Optional[str] = None
    date_from: Optional[str] = None
    date_to: Optional[str] = None


class PassUpdate(BaseModel):
    status: Optional[str] = None
    valid_until: Optional[str] = None
    notes: Optional[str] = None


class PassExtend(BaseModel):
    days: int = Field(gt=0, le=365)
    reason: str


class PaymentFilter(BaseModel):
    search: Optional[str] = None
    status: Optional[str] = None
    provider: Optional[str] = None
    date_from: Optional[str] = None
    date_to: Optional[str] = None
    min_amount: Optional[float] = None
    max_amount: Optional[float] = None


class RefundRequest(BaseModel):
    reason: str
    amount: Optional[float] = None  # Partial refund amount, None = full refund


class AuditLogEntry(BaseModel):
    log_id: str
    admin_id: str
    admin_email: str
    action: str
    entity_type: str
    entity_id: Optional[str] = None
    metadata: Optional[Dict[str, Any]] = None
    ip_address: Optional[str] = None
    created_at: datetime


class DashboardStats(BaseModel):
    total_users: int
    active_users: int
    total_passes: int
    active_passes: int
    total_revenue: float
    monthly_revenue: float
    total_bookings: int
    monthly_bookings: int
    pending_refunds: int
    
    
class ReportRequest(BaseModel):
    report_type: str  # users, passes, payments, bookings
    date_from: Optional[str] = None
    date_to: Optional[str] = None
    filters: Optional[Dict[str, Any]] = None


# ==================== PHASE 2 MODELS ====================

class TwoFactorSetup(BaseModel):
    """2FA setup request"""
    method: str = "totp"  # totp or email


class TwoFactorVerify(BaseModel):
    """2FA verification request"""
    code: str


class FraudRuleType(str, Enum):
    """Types of fraud detection rules"""
    MULTIPLE_BOOKINGS = "multiple_bookings"
    HIGH_VALUE_BOOKING = "high_value_booking"
    SUSPICIOUS_COUNTRY = "suspicious_country"
    RAPID_PASS_USAGE = "rapid_pass_usage"
    MULTIPLE_REFUNDS = "multiple_refunds"
    INVALID_PASS_ATTEMPTS = "invalid_pass_attempts"
    ACCOUNT_SHARING = "account_sharing"


class FraudRuleSeverity(str, Enum):
    """Severity levels for fraud alerts"""
    LOW = "low"
    MEDIUM = "medium"
    HIGH = "high"
    CRITICAL = "critical"


class FraudRuleCreate(BaseModel):
    """Create a new fraud detection rule"""
    name: str
    rule_type: FraudRuleType
    description: Optional[str] = None
    is_active: bool = True
    severity: FraudRuleSeverity = FraudRuleSeverity.MEDIUM
    threshold: int = 3  # Number of occurrences before triggering
    time_window_hours: int = 24  # Time window for counting occurrences
    auto_suspend: bool = False  # Auto-suspend user on trigger
    notify_admin: bool = True


class FraudRuleUpdate(BaseModel):
    """Update fraud detection rule"""
    name: Optional[str] = None
    description: Optional[str] = None
    is_active: Optional[bool] = None
    severity: Optional[FraudRuleSeverity] = None
    threshold: Optional[int] = None
    time_window_hours: Optional[int] = None
    auto_suspend: Optional[bool] = None
    notify_admin: Optional[bool] = None


class AlertType(str, Enum):
    """Types of automated alerts"""
    FRAUD_DETECTED = "fraud_detected"
    HIGH_VALUE_REFUND = "high_value_refund"
    PASS_ABUSE = "pass_abuse"
    SUSPICIOUS_ACTIVITY = "suspicious_activity"
    SYSTEM_WARNING = "system_warning"
    DAILY_SUMMARY = "daily_summary"


class AlertStatus(str, Enum):
    """Alert status"""
    NEW = "new"
    ACKNOWLEDGED = "acknowledged"
    RESOLVED = "resolved"
    DISMISSED = "dismissed"


class AlertCreate(BaseModel):
    """Create an alert"""
    alert_type: AlertType
    title: str
    message: str
    severity: FraudRuleSeverity = FraudRuleSeverity.MEDIUM
    entity_type: Optional[str] = None  # user, pass, booking, payment
    entity_id: Optional[str] = None
    metadata: Optional[Dict[str, Any]] = None


class AlertUpdate(BaseModel):
    """Update alert status"""
    status: AlertStatus
    resolution_note: Optional[str] = None


class PassValidationRequest(BaseModel):
    """Enhanced pass validation request"""
    pass_code: str
    user_ip: Optional[str] = None
    user_agent: Optional[str] = None
    booking_amount: Optional[float] = None


class PassValidationLog(BaseModel):
    """Log entry for pass validation attempts"""
    pass_code: str
    user_id: Optional[str] = None
    validation_result: bool
    ip_address: Optional[str] = None
    user_agent: Optional[str] = None
    validation_type: str  # booking, manual_check, api
    fail_reason: Optional[str] = None
    created_at: datetime


class AdvancedAnalytics(BaseModel):
    """Advanced analytics data structure"""
    metric_type: str  # revenue_trend, user_growth, pass_conversion, etc.
    period: str  # daily, weekly, monthly
    data_points: List[Dict[str, Any]]
    comparison_period: Optional[Dict[str, Any]] = None
    insights: Optional[List[str]] = None


# ==================== B2B PASS MODELS ====================

class B2BPassOrder(BaseModel):
    """B2B Pass Order for business customers"""
    business_name: str
    business_address: str
    business_email: str
    business_phone: str
    vat_number: str
    pass_type: str  # one_time, annual
    quantity: int
    unit_price: float
    total_price: float
    notes: Optional[str] = None


class B2BPassOrderCreate(BaseModel):
    """Create B2B pass order"""
    business_name: str
    business_address: str
    business_email: str
    business_phone: str
    vat_number: str
    pass_type: str  # one_time, annual
    quantity: int = 1
    unit_price: Optional[float] = None  # Admin can override
    notes: Optional[str] = None


class B2BPassOrderUpdate(BaseModel):
    """Update B2B pass order"""
    business_name: Optional[str] = None
    business_address: Optional[str] = None
    business_email: Optional[str] = None
    business_phone: Optional[str] = None
    vat_number: Optional[str] = None
    status: Optional[str] = None  # pending, invoiced, paid, cancelled
    invoice_number: Optional[str] = None
    payment_date: Optional[str] = None
    notes: Optional[str] = None


class B2BInvoice(BaseModel):
    """B2B Invoice"""
    order_id: str
    invoice_number: str
    invoice_date: str
    due_date: str
    status: str  # sent, paid, overdue, cancelled
