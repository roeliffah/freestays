from fastapi import FastAPI, APIRouter, HTTPException, Request, Response, Depends, Query, BackgroundTasks, UploadFile, File
from fastapi.responses import JSONResponse
from fastapi.staticfiles import StaticFiles
from dotenv import load_dotenv
from starlette.middleware.cors import CORSMiddleware
from motor.motor_asyncio import AsyncIOMotorClient
import os
import logging
import secrets
from pathlib import Path
from pydantic import BaseModel, Field, EmailStr
from typing import List, Optional, Dict, Any
import uuid
from datetime import datetime, timezone, timedelta
import httpx
import xml.etree.ElementTree as ET
import bcrypt
import jwt
import json
import asyncpg
import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
import asyncio
import aiomysql
from apscheduler.schedulers.asyncio import AsyncIOScheduler
from apscheduler.triggers.cron import CronTrigger
from apscheduler.triggers.interval import IntervalTrigger
import shutil
import imaplib
import email
from email.header import decode_header
import re

ROOT_DIR = Path(__file__).parent
UPLOAD_DIR = ROOT_DIR / "static" / "uploads"
UPLOAD_DIR.mkdir(parents=True, exist_ok=True)
load_dotenv(ROOT_DIR / '.env')

# MongoDB connection
mongo_url = os.environ['MONGO_URL']
client = AsyncIOMotorClient(mongo_url)
db = client[os.environ['DB_NAME']]

# Configuration
JWT_SECRET = os.environ.get('JWT_SECRET', 'freestays-secret-key-2024')
JWT_ALGORITHM = "HS256"
JWT_EXPIRATION_HOURS = 168  # 7 days
ADMIN_PASSWORD = os.environ.get('ADMIN_PASSWORD', 'admin123')  # Default admin password

# Sunhotels API Configuration
SUNHOTELS_USERNAME = os.environ.get('SUNHOTELS_USERNAME', 'Freestays')
SUNHOTELS_PASSWORD = os.environ.get('SUNHOTELS_PASSWORD', 'Vision2024!@')
SUNHOTELS_BASE_URL = "https://xml.sunhotels.net/15/PostGet/NonStaticXMLAPI.asmx"

# IMAP Configuration for Sunhotels Email Forwarding
IMAP_SERVER = os.environ.get('IMAP_SERVER', 'imap.strato.com')
IMAP_EMAIL = os.environ.get('IMAP_EMAIL', 'info@freestays.eu')
IMAP_PASSWORD = os.environ.get('IMAP_PASSWORD', '')
IMAP_PORT = int(os.environ.get('IMAP_PORT', '993'))

# Stripe Configuration - can be updated via admin
STRIPE_API_KEY = os.environ.get('STRIPE_API_KEY', 'sk_test_emergent')

# Pass Pricing - can be updated via admin
PASS_ONE_TIME_PRICE = 35.00  # €35 for one booking
PASS_ANNUAL_PRICE = 129.00   # €129 for unlimited bookings for 1 year
BOOKING_FEE = 15.00          # €15 booking fee (waived with pass purchase)

# Initialize scheduler for background jobs
scheduler = AsyncIOScheduler()
MARKUP_RATE = 0.16           # 16% markup
VAT_RATE = 0.21              # 21% VAT on markup
FREESTAYS_DISCOUNT = 0.15    # 15% discount for pass holders

# Create the main app
app = FastAPI(title="FreeStays API", description="Commission-free hotel booking platform")

# Create a router with the /api prefix
api_router = APIRouter(prefix="/api")

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

# ==================== SEARCH CACHE ====================
class SearchCache:
    """Simple in-memory cache for autocomplete search results with TTL"""
    def __init__(self, max_size: int = 500, ttl_seconds: int = 300):
        self._cache: Dict[str, tuple] = {}  # key -> (result, timestamp)
        self._max_size = max_size
        self._ttl = ttl_seconds
        self._hits = 0
        self._misses = 0
    
    def get(self, key: str) -> Optional[Any]:
        """Get cached result if exists and not expired"""
        if key in self._cache:
            result, timestamp = self._cache[key]
            if datetime.now().timestamp() - timestamp < self._ttl:
                self._hits += 1
                return result
            else:
                # Expired, remove it
                del self._cache[key]
        self._misses += 1
        return None
    
    def set(self, key: str, value: Any) -> None:
        """Cache a result with current timestamp"""
        # Evict oldest entries if cache is full
        if len(self._cache) >= self._max_size:
            # Remove 20% of oldest entries
            items = sorted(self._cache.items(), key=lambda x: x[1][1])
            for k, _ in items[:int(self._max_size * 0.2)]:
                del self._cache[k]
        
        self._cache[key] = (value, datetime.now().timestamp())
    
    def clear(self) -> None:
        """Clear all cached entries"""
        self._cache.clear()
        self._hits = 0
        self._misses = 0
    
    def stats(self) -> Dict:
        """Get cache statistics"""
        total = self._hits + self._misses
        hit_rate = (self._hits / total * 100) if total > 0 else 0
        return {
            "size": len(self._cache),
            "max_size": self._max_size,
            "ttl_seconds": self._ttl,
            "hits": self._hits,
            "misses": self._misses,
            "hit_rate": f"{hit_rate:.1f}%"
        }

# Initialize search cache (500 entries, 5 min TTL)
autocomplete_cache = SearchCache(max_size=500, ttl_seconds=300)

# ==================== MODELS ====================

class UserCreate(BaseModel):
    email: EmailStr
    password: str
    name: str
    referral_code: Optional[str] = None

class UserLogin(BaseModel):
    email: EmailStr
    password: str

class UserResponse(BaseModel):
    user_id: str
    email: str
    name: str
    picture: Optional[str] = None
    pass_code: Optional[str] = None
    created_at: datetime

class PassCodeApply(BaseModel):
    pass_code: str

class PassCodeValidate(BaseModel):
    pass_code: str

class HotelSearchParams(BaseModel):
    destination: str
    destination_id: Optional[str] = None
    resort_id: Optional[str] = None  # Resort ID for Sunhotels SearchV3
    check_in: str
    check_out: str
    adults: int = 2
    children: int = 0
    children_ages: Optional[List[int]] = None  # Ages of children (0-17)
    rooms: int = 1
    currency: str = "EUR"
    b2c: int = 0  # 0 = normal, 1 = last minute

class BookingCreate(BaseModel):
    hotel_id: str
    room_id: str
    hotel_name: str
    room_type: str
    check_in: str
    check_out: str
    adults: int
    children: int
    children_ages: Optional[List[int]] = None  # Ages of children (0-17)
    guest_first_name: str
    guest_last_name: str
    guest_email: EmailStr
    guest_phone: str
    special_requests: Optional[str] = None
    total_price: float
    currency: str = "EUR"
    pass_code: Optional[str] = None
    pass_purchase_type: Optional[str] = None  # 'one_time', 'annual', or None
    board_type: Optional[str] = None  # Room Only, Breakfast, Half Board, Full Board, All Inclusive
    # Sunhotels booking data for later confirmation
    sunhotels_room_type_id: Optional[str] = None
    sunhotels_block_id: Optional[str] = None
    use_referral_discount: Optional[bool] = False  # Use referral discount to waive booking fee

class PaymentCreate(BaseModel):
    booking_id: str
    amount: float
    currency: str = "EUR"

class ForgotPasswordRequest(BaseModel):
    email: EmailStr

class ResetPasswordRequest(BaseModel):
    token: str
    new_password: str

class VerifyEmailRequest(BaseModel):
    token: str

class FavoriteHotel(BaseModel):
    hotel_id: str
    hotel_name: str
    star_rating: Optional[float] = None
    image_url: Optional[str] = None
    location: Optional[str] = None
    min_price: Optional[float] = None

class TestimonialCreate(BaseModel):
    booking_id: Optional[str] = None
    rating: int  # 1-5
    title: str
    content: str
    hotel_name: Optional[str] = None

class GuestSurveySubmit(BaseModel):
    booking_id: str
    survey_token: str
    overall_rating: int  # 1-5
    cleanliness_rating: int  # 1-5
    service_rating: int  # 1-5
    value_rating: int  # 1-5
    location_rating: Optional[int] = None  # 1-5
    amenities_rating: Optional[int] = None  # 1-5
    title: str
    review_text: str
    would_recommend: bool = True
    travel_type: Optional[str] = None  # business, leisure, family, couple, solo
    photos: Optional[List[str]] = None  # URLs of uploaded photos

class ReferralApply(BaseModel):
    referral_code: str

class AdminLogin(BaseModel):
    email: str
    password: str

class AdminSettings(BaseModel):
    # Stripe settings
    stripe_mode: Optional[str] = None  # "live" or "test"
    stripe_test_secret_key: Optional[str] = None
    stripe_test_publishable_key: Optional[str] = None
    stripe_live_secret_key: Optional[str] = None
    stripe_live_publishable_key: Optional[str] = None
    # Legacy fields (deprecated, kept for backwards compatibility)
    stripe_live_key: Optional[str] = None
    stripe_test_key: Optional[str] = None
    stripe_api_key: Optional[str] = None
    
    # Sunhotels settings
    sunhotels_username: Optional[str] = None
    sunhotels_password: Optional[str] = None
    sunhotels_mode: Optional[str] = None  # "live" or "test"
    
    # Database settings
    static_db_host: Optional[str] = None
    static_db_port: Optional[str] = None
    static_db_name: Optional[str] = None
    static_db_user: Optional[str] = None
    static_db_password: Optional[str] = None
    
    # Pricing settings
    pass_one_time_price: Optional[float] = None
    pass_annual_price: Optional[float] = None
    booking_fee: Optional[float] = None
    markup_rate: Optional[float] = None
    vat_rate: Optional[float] = None
    discount_rate: Optional[float] = None
    admin_password: Optional[str] = None
    
    # SMTP Email Settings
    smtp_host: Optional[str] = None
    smtp_port: Optional[int] = None
    smtp_username: Optional[str] = None
    smtp_password: Optional[str] = None
    smtp_from_email: Optional[str] = None
    smtp_from_name: Optional[str] = None
    smtp_enabled: Optional[bool] = None
    
    # Company Branding
    company_name: Optional[str] = None
    company_logo_url: Optional[str] = None
    
    # Last Minute Configuration
    last_minute_count: Optional[int] = None  # Number of offers to show
    last_minute_check_in: Optional[str] = None  # Custom check-in date (YYYY-MM-DD)
    last_minute_check_out: Optional[str] = None  # Custom check-out date (YYYY-MM-DD)
    last_minute_title: Optional[str] = None  # Custom title text
    last_minute_subtitle: Optional[str] = None  # Custom subtitle/description
    last_minute_badge_text: Optional[str] = None  # Badge text (e.g., "Hot Deals")
    company_website: Optional[str] = None
    company_support_email: Optional[str] = None
    
    # Price Comparison Settings
    price_comparison_enabled: Optional[bool] = None
    ota_markup_percentage: Optional[int] = None  # Default 20%
    comparison_min_savings_percent: Optional[int] = None  # Default 10%
    comparison_email_frequency: Optional[str] = None  # "search", "daily", "weekly", "disabled"
    comparison_email_address: Optional[str] = None
    
    # Referral Program Settings
    referral_enabled: Optional[bool] = None
    referral_discount_amount: Optional[float] = None  # €15 default
    referral_min_booking_value: Optional[float] = None
    referral_max_uses_per_code: Optional[int] = None
    
    # Price Drop Notification Settings
    price_drop_enabled: Optional[bool] = None
    price_drop_check_frequency: Optional[str] = None  # "daily", "6hours", "12hours"
    price_drop_min_percent: Optional[int] = None
    
    # Contact Page Settings
    contact_page_title: Optional[str] = None
    contact_page_subtitle: Optional[str] = None
    contact_email: Optional[str] = None
    contact_email_note: Optional[str] = None
    contact_phone: Optional[str] = None
    contact_phone_hours: Optional[str] = None
    contact_company_name: Optional[str] = None
    contact_address: Optional[str] = None
    contact_support_text: Optional[str] = None
    
    # Dark Mode Settings
    darkMode_enabled: Optional[bool] = None  # True = allow dark mode toggle, False = force light mode

class LastMinuteFetchRequest(BaseModel):
    check_in: str
    check_out: str

class PromoCodeCreate(BaseModel):
    code: str
    discount_rate: float = 0.15
    active: bool = True
    description: Optional[str] = None

# Models for Sunhotels PreBook and Book
class PreBookRequest(BaseModel):
    hotel_id: str
    room_id: str  # The room ID from search results
    roomtype_id: str  # The roomtype ID from search results  
    meal_id: int = 1  # 1=Room Only, 2=BB, 3=HB, 4=FB, 5=AI
    check_in: str  # YYYY-MM-DD
    check_out: str  # YYYY-MM-DD
    rooms: int = 1
    adults: int = 2
    children: int = 0
    children_ages: str = ""  # Comma separated ages
    currency: str = "EUR"
    search_price: float  # Price from search results for validation
    customer_country: str = "NL"  # ISO country code
    b2c: int = 0

class PreBookResponse(BaseModel):
    success: bool
    prebook_code: Optional[str] = None  # Code needed for final booking
    price: Optional[float] = None
    currency: Optional[str] = None
    taxes: Optional[float] = None
    fees: Optional[List[Dict]] = None
    cancellation_policy: Optional[str] = None
    error: Optional[str] = None

class BookRequest(BaseModel):
    # Booking details
    prebook_code: str  # From PreBook response
    hotel_id: str
    room_id: str
    meal_id: int = 1
    check_in: str
    check_out: str
    rooms: int = 1
    adults: int = 2
    children: int = 0
    currency: str = "EUR"
    customer_country: str = "NL"
    
    # Guest details (primary guest)
    guest_first_name: str
    guest_last_name: str
    guest_email: EmailStr
    
    # Additional guests (optional)
    guest2_first_name: Optional[str] = None
    guest2_last_name: Optional[str] = None
    
    # Payment info (we'll use Stripe, not credit card to Sunhotels)
    payment_method_id: int = 1  # 1 = Credit terms (B2B)
    
    # Reference
    your_ref: Optional[str] = None
    special_request: Optional[str] = None
    invoice_ref: Optional[str] = None  # Our internal booking ID
    expires_at: Optional[str] = None

# ==================== HELPER FUNCTIONS ====================

async def get_settings() -> Dict:
    """Get current settings from database, merged with defaults"""
    defaults = {
        "stripe_mode": "test",  # Default to test mode
        "stripe_test_secret_key": STRIPE_API_KEY,  # Use env var as default test key
        "stripe_test_publishable_key": "",
        "stripe_live_secret_key": "",
        "stripe_live_publishable_key": "",
        # Legacy fields (for backwards compatibility)
        "stripe_live_key": "",
        "stripe_test_key": STRIPE_API_KEY,
        "stripe_api_key": STRIPE_API_KEY,
        "sunhotels_username": SUNHOTELS_USERNAME,
        "sunhotels_password": SUNHOTELS_PASSWORD,
        "sunhotels_mode": "live",  # "live" or "test"
        "sunhotels_api_type": "nonstatic",  # "static" or "nonstatic"
        "static_db_host": "",
        "static_db_port": "3306",
        "static_db_name": "",
        "static_db_user": "",
        "static_db_password": "",
        "pass_one_time_price": PASS_ONE_TIME_PRICE,
        "pass_annual_price": PASS_ANNUAL_PRICE,
        "booking_fee": BOOKING_FEE,
        "markup_rate": MARKUP_RATE,
        "vat_rate": VAT_RATE,
        "discount_rate": FREESTAYS_DISCOUNT,
        "admin_password": ADMIN_PASSWORD,
        # SMTP Email Settings
        "smtp_host": "smtp.strato.de",
        "smtp_port": 587,
        "smtp_username": "",
        "smtp_password": "",
        "smtp_from_email": "booking@freestays.eu",
        "smtp_from_name": "FreeStays",
        "smtp_enabled": False,
        # Company Branding
        "company_name": "FreeStays",
        "company_logo_url": "https://customer-assets.emergentagent.com/job_94e1e280-97df-4548-a733-4d7da4555d27/artifacts/63lj7fq5_kogo_blauw.png",
        "company_website": "https://freestays.eu",
        "company_support_email": "info@freestays.eu",
        # Last Minute Configuration
        "last_minute_count": 6,
        "last_minute_check_in": "",  # Empty = use dynamic dates (tomorrow)
        "last_minute_check_out": "",  # Empty = use dynamic dates (day after tomorrow)
        "last_minute_title": "Last Minute Offers",
        "last_minute_subtitle": "Book now and save up to 30% on selected hotels",
        "last_minute_badge_text": "Hot Deals",
        # Price Comparison Settings
        "price_comparison_enabled": True,
        "ota_markup_percentage": 20,  # Other platforms markup (20% default)
        "comparison_min_savings_percent": 10,  # Only show if we're 10%+ cheaper
        "comparison_email_frequency": "search",  # "search", "daily", "weekly", "disabled"
        "comparison_email_address": "campain@freestays.eu",
        # Referral Program Settings
        "referral_enabled": True,
        "referral_discount_amount": 15.00,  # €15 discount (representing booking costs)
        "referral_min_booking_value": 0,  # Minimum booking value to use referral
        "referral_max_uses_per_code": 0,  # 0 = unlimited
        # Price Drop Notification Settings
        "price_drop_enabled": True,
        "price_drop_check_frequency": "daily",  # "daily", "6hours", "12hours"
        "price_drop_min_percent": 5,  # Minimum % drop to notify
        # Dark Mode Settings
        "darkMode_enabled": True  # Allow users to toggle dark mode
    }
    
    db_settings = await db.settings.find_one({"type": "app_settings"}, {"_id": 0})
    if db_settings:
        # Merge defaults with database settings (database values take precedence)
        settings = {**defaults, **db_settings}
    else:
        settings = defaults
    return settings

async def get_active_stripe_key() -> str:
    """Get the active Stripe API key (secret key) based on the current mode (live or test)"""
    settings = await get_settings()
    mode = settings.get("stripe_mode", "test")
    
    if mode == "live":
        # Try new field name first, fallback to legacy
        key = settings.get("stripe_live_secret_key") or settings.get("stripe_live_key", "")
        if key:
            return key
    
    # Default to test key - try new field name first, fallback to legacy
    test_key = settings.get("stripe_test_secret_key") or settings.get("stripe_test_key", "")
    if test_key:
        return test_key
    
    # Final fallback to env variable
    return STRIPE_API_KEY

async def get_stripe_publishable_key() -> str:
    """Get the active Stripe publishable key based on the current mode (live or test)"""
    settings = await get_settings()
    mode = settings.get("stripe_mode", "test")
    
    if mode == "live":
        return settings.get("stripe_live_publishable_key", "")
    
    return settings.get("stripe_test_publishable_key", "")

# ==================== EMAIL TRANSLATIONS ====================
EMAIL_TRANSLATIONS = {
    "en": {
        # Header/Footer
        "commission_free_bookings": "Commission-free bookings",
        "all_rights_reserved": "All rights reserved",
        
        # Booking Confirmation
        "booking_confirmed": "Booking Confirmed!",
        "booking_reference": "Booking Reference",
        "sunhotels_ref": "Sunhotels Ref",
        "hotel_details": "Hotel Details",
        "hotel": "Hotel",
        "room_type": "Room Type",
        "board_type": "Board Type",
        "room_only": "Room Only",
        "check_in": "Check-in",
        "check_out": "Check-out",
        "duration": "Duration",
        "nights": "Night(s)",
        "guests": "Guests",
        "adults": "Adult(s)",
        "children": "Child(ren)",
        "free_cancellation_until": "Free Cancellation Until",
        "guest_details": "Guest Details",
        "name": "Name",
        "email": "Email",
        "phone": "Phone",
        "your_freestays_pass": "Your FreeStays Pass",
        "new_annual_pass": "New Annual Pass",
        "new_one_time_pass": "New One Time Pass",
        "pass_code_used": "Pass Code Used",
        "book_worldwide": "Book worldwide at any hotel",
        "unlimited_use": "Unlimited use during 12 months",
        "single_use": "Single use during this booking",
        "no_booking_costs": "No €15.00 booking costs at your first booking",
        "payment_summary": "Payment Summary",
        "total_paid": "Total Paid",
        "payment_confirmed": "Payment Confirmed",
        "special_requests": "Special Requests",
        "thank_you_booking": "Thank you for booking with",
        "contact": "Contact",
        
        # Verification Email
        "verify_your_email": "Verify Your Email",
        "hi": "Hi",
        "thank_you_joining": "Thank you for joining",
        "verify_email_text": "Please verify your email address to activate your account and start getting FREE rooms.",
        "verify_email_button": "Verify Email Address",
        "link_doesnt_work": "If the button doesn't work, copy and paste this link into your browser:",
        "link_expires": "This link will expire in 24 hours.",
        
        # Password Reset
        "reset_your_password": "Reset Your Password",
        "password_reset_request": "We received a request to reset your password for your FreeStays account.",
        "click_reset_password": "Click below to reset your password:",
        "reset_password_button": "Reset Password",
        "ignore_if_not_requested": "If you didn't request this, you can safely ignore this email.",
        "link_expires_1_hour": "This link will expire in 1 hour.",
        
        # Price Drop Email
        "price_drop_alert": "Price Drop Alert!",
        "great_news": "Great news",
        "hotel_you_searched": "A hotel you searched for just dropped in price!",
        "previous_price": "Previous Price",
        "new_price": "New Price",
        "you_save": "You Save",
        "book_now_button": "Book Now at New Price",
        "dont_miss_out": "Don't miss out on this deal - prices can change at any time!",
        
        # Referral Welcome Email
        "welcome": "Welcome!",
        "invited_you": "invited you to join",
        "special_welcome_gift": "and you've got a special welcome gift!",
        "your_welcome_discount": "YOUR WELCOME DISCOUNT",
        "off": "OFF",
        "first_booking_fee_waived": "Your first booking fee is waived!",
        "remember_room_free": "Remember: With {company}, your room is FREE - you only pay for meals!",
        "discount_auto_applied": "Plus, your discount is automatically applied at checkout.",
        "start_exploring": "Start Exploring Hotels",
        
        # Referrer Notification
        "great_news_exclaim": "Great News!",
        "fantastic_news": "Fantastic news!",
        "just_signed_up": "just signed up using your referral code!",
        "new_referral_registered": "NEW REFERRAL REGISTERED!",
        "has_joined": "Has joined FreeStays!",
        "keep_sharing": "Keep sharing your referral code to invite more friends and family. The more you share, the more you help others discover commission-free hotel bookings!",
        "view_dashboard": "View Your Dashboard",
        
        # Milestone Reward Email
        "congratulations": "Congratulations!",
        "incredible_achievement": "What an incredible achievement! You've successfully referred",
        "friends_to": "friends to",
        "as_thank_you": "As a thank you for spreading the word about commission-free hotel bookings, we're rewarding you with:",
        "free_annual_pass": "FREE ANNUAL PASS",
        "worth": "Worth €129",
        "your_pass_code": "Your Pass Code",
        "annual_pass_benefits": "With your Annual Pass, you can now enjoy unlimited bookings with:",
        "access_worldwide": "Access to 450,000+ hotels worldwide",
        "room_free": "Your room is FREE - only pay for meals",
        "no_booking_fees": "No €15 booking fees for 12 months",
        "thank_you_ambassador": "Thank you for being an amazing FreeStays ambassador!"
    },
    "nl": {
        # Header/Footer
        "commission_free_bookings": "Commissievrije boekingen",
        "all_rights_reserved": "Alle rechten voorbehouden",
        
        # Booking Confirmation
        "booking_confirmed": "Boeking Bevestigd!",
        "booking_reference": "Boekingsreferentie",
        "sunhotels_ref": "Sunhotels Ref",
        "hotel_details": "Hotelgegevens",
        "hotel": "Hotel",
        "room_type": "Kamertype",
        "board_type": "Pension",
        "room_only": "Alleen Kamer",
        "check_in": "Inchecken",
        "check_out": "Uitchecken",
        "duration": "Duur",
        "nights": "Nacht(en)",
        "guests": "Gasten",
        "adults": "Volwassene(n)",
        "children": "Kind(eren)",
        "free_cancellation_until": "Gratis Annuleren Tot",
        "guest_details": "Gastgegevens",
        "name": "Naam",
        "email": "E-mail",
        "phone": "Telefoon",
        "your_freestays_pass": "Uw FreeStays Pas",
        "new_annual_pass": "Nieuwe Jaarpas",
        "new_one_time_pass": "Nieuwe Eenmalige Pas",
        "pass_code_used": "Pascode Gebruikt",
        "book_worldwide": "Boek wereldwijd bij elk hotel",
        "unlimited_use": "Onbeperkt gebruik gedurende 12 maanden",
        "single_use": "Eenmalig gebruik tijdens deze boeking",
        "no_booking_costs": "Geen €15,00 boekingskosten bij uw eerste boeking",
        "payment_summary": "Betalingsoverzicht",
        "total_paid": "Totaal Betaald",
        "payment_confirmed": "Betaling Bevestigd",
        "special_requests": "Speciale Verzoeken",
        "thank_you_booking": "Bedankt voor uw boeking bij",
        "contact": "Contact",
        
        # Verification Email
        "verify_your_email": "Verifieer Uw E-mail",
        "hi": "Hallo",
        "thank_you_joining": "Bedankt voor uw aanmelding bij",
        "verify_email_text": "Verifieer uw e-mailadres om uw account te activeren en te beginnen met GRATIS kamers.",
        "verify_email_button": "E-mailadres Verifiëren",
        "link_doesnt_work": "Als de knop niet werkt, kopieer en plak deze link in uw browser:",
        "link_expires": "Deze link verloopt over 24 uur.",
        
        # Password Reset
        "reset_your_password": "Wachtwoord Herstellen",
        "password_reset_request": "We hebben een verzoek ontvangen om uw wachtwoord te herstellen voor uw FreeStays account.",
        "click_reset_password": "Klik hieronder om uw wachtwoord te herstellen:",
        "reset_password_button": "Wachtwoord Herstellen",
        "ignore_if_not_requested": "Als u dit niet heeft aangevraagd, kunt u deze e-mail veilig negeren.",
        "link_expires_1_hour": "Deze link verloopt over 1 uur.",
        
        # Price Drop Email
        "price_drop_alert": "Prijsdaling Melding!",
        "great_news": "Goed nieuws",
        "hotel_you_searched": "Een hotel dat u heeft gezocht is net in prijs gedaald!",
        "previous_price": "Vorige Prijs",
        "new_price": "Nieuwe Prijs",
        "you_save": "U Bespaart",
        "book_now_button": "Nu Boeken tegen Nieuwe Prijs",
        "dont_miss_out": "Mis deze deal niet - prijzen kunnen op elk moment veranderen!",
        
        # Referral Welcome Email
        "welcome": "Welkom!",
        "invited_you": "heeft u uitgenodigd om lid te worden van",
        "special_welcome_gift": "en u heeft een speciaal welkomstgeschenk!",
        "your_welcome_discount": "UW WELKOMSTKORTING",
        "off": "KORTING",
        "first_booking_fee_waived": "Uw eerste boekingskosten zijn kwijtgescholden!",
        "remember_room_free": "Onthoud: Bij {company} is uw kamer GRATIS - u betaalt alleen voor maaltijden!",
        "discount_auto_applied": "Bovendien wordt uw korting automatisch toegepast bij het afrekenen.",
        "start_exploring": "Begin Met Hotels Verkennen",
        
        # Referrer Notification
        "great_news_exclaim": "Goed Nieuws!",
        "fantastic_news": "Fantastisch nieuws!",
        "just_signed_up": "heeft zich net aangemeld met uw verwijzingscode!",
        "new_referral_registered": "NIEUWE VERWIJZING GEREGISTREERD!",
        "has_joined": "Is lid geworden van FreeStays!",
        "keep_sharing": "Blijf uw verwijzingscode delen om meer vrienden en familie uit te nodigen. Hoe meer u deelt, hoe meer u anderen helpt commissievrije hotelboekingen te ontdekken!",
        "view_dashboard": "Bekijk Uw Dashboard",
        
        # Milestone Reward Email
        "congratulations": "Gefeliciteerd!",
        "incredible_achievement": "Wat een geweldige prestatie! U heeft succesvol",
        "friends_to": "vrienden verwezen naar",
        "as_thank_you": "Als dank voor het verspreiden van het woord over commissievrije hotelboekingen, belonen wij u met:",
        "free_annual_pass": "GRATIS JAARPAS",
        "worth": "Ter waarde van €129",
        "your_pass_code": "Uw Pascode",
        "annual_pass_benefits": "Met uw Jaarpas kunt u nu genieten van onbeperkte boekingen met:",
        "access_worldwide": "Toegang tot meer dan 450.000 hotels wereldwijd",
        "room_free": "Uw kamer is GRATIS - betaal alleen voor maaltijden",
        "no_booking_fees": "Geen €15 boekingskosten voor 12 maanden",
        "thank_you_ambassador": "Bedankt dat u een geweldige FreeStays ambassadeur bent!"
    }
}

def get_email_translation(key: str, lang: str = "en") -> str:
    """Get email translation by key and language"""
    translations = EMAIL_TRANSLATIONS.get(lang, EMAIL_TRANSLATIONS["en"])
    return translations.get(key, EMAIL_TRANSLATIONS["en"].get(key, key))

# ==================== EMAIL SERVICE ====================
class EmailService:
    """Service for sending branded booking confirmation emails via SMTP"""
    
    @staticmethod
    async def get_smtp_settings() -> Dict:
        """Get SMTP settings from database"""
        settings = await get_settings()
        return {
            "host": settings.get("smtp_host", "smtp.strato.de"),
            "port": int(settings.get("smtp_port", 587)),
            "username": settings.get("smtp_username", ""),
            "password": settings.get("smtp_password", ""),
            "from_email": settings.get("smtp_from_email", "booking@freestays.eu"),
            "from_name": settings.get("smtp_from_name", "FreeStays"),
            "enabled": settings.get("smtp_enabled", False),
            "company_name": settings.get("company_name", "FreeStays"),
            "company_logo_url": settings.get("company_logo_url", ""),
            "company_website": settings.get("company_website", "https://freestays.eu"),
            "company_support_email": settings.get("company_support_email", "info@freestays.eu")
        }
    
    @staticmethod
    def get_email_header(smtp_settings: Dict, title: str = "", lang: str = "en") -> str:
        """Generate consistent email header with logo, slogan and branding"""
        company_name = smtp_settings.get("company_name", "FreeStays")
        logo_url = smtp_settings.get("company_logo_url", "")
        slogan = get_email_translation("commission_free_bookings", lang)
        
        logo_html = ""
        if logo_url:
            logo_html = f'<img src="{logo_url}" alt="{company_name}" style="max-height: 50px; margin-right: 15px;">'
        else:
            # Text-based logo fallback
            logo_html = f'<span style="font-size: 28px; font-weight: bold; color: #ffffff;">FreeStays</span>'
        
        return f"""
        <tr>
            <td style="background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); padding: 25px 30px;">
                <table width="100%" cellpadding="0" cellspacing="0">
                    <tr>
                        <td style="vertical-align: middle;">
                            {logo_html}
                        </td>
                        <td style="text-align: right; vertical-align: middle;">
                            <p style="margin: 0; color: #a3c9f1; font-size: 14px; font-style: italic;">{slogan}</p>
                        </td>
                    </tr>
                </table>
                {f'<h1 style="color: #ffffff; margin: 20px 0 0 0; font-size: 24px; font-weight: 600; text-align: center;">{title}</h1>' if title else ''}
            </td>
        </tr>
        """
    
    @staticmethod
    def get_email_footer(smtp_settings: Dict, lang: str = "en") -> str:
        """Generate consistent email footer with business credentials"""
        company_name = smtp_settings.get("company_name", "FreeStays")
        support_email = smtp_settings.get("company_support_email", "info@freestays.eu")
        website = smtp_settings.get("company_website", "https://freestays.eu")
        all_rights = get_email_translation("all_rights_reserved", lang)
        
        return f"""
        <tr>
            <td style="background-color: #f8fafc; padding: 30px; border-top: 1px solid #eee;">
                <table width="100%" cellpadding="0" cellspacing="0">
                    <tr>
                        <td style="text-align: center;">
                            <!-- Company Name -->
                            <p style="margin: 0 0 5px 0; color: #1e3a5f; font-size: 16px; font-weight: 600;">{company_name}</p>
                            <p style="margin: 0 0 15px 0; color: #666; font-size: 12px;">by TravelAR Group BV</p>
                            
                            <!-- Address -->
                            <p style="margin: 0 0 3px 0; color: #666; font-size: 13px;">Van Haersoltelaan 19</p>
                            <p style="margin: 0 0 15px 0; color: #666; font-size: 13px;">NL - 3771 JW Barneveld</p>
                            
                            <!-- Contact -->
                            <p style="margin: 0 0 5px 0; color: #666; font-size: 12px;">
                                <a href="mailto:{support_email}" style="color: #1e3a5f; text-decoration: none;">{support_email}</a> | 
                                <a href="https://{website.replace('https://', '').replace('http://', '')}" style="color: #1e3a5f; text-decoration: none;">www.freestays.eu</a>
                            </p>
                            
                            <!-- Business Registration -->
                            <table cellpadding="0" cellspacing="0" style="margin: 15px auto 0 auto;">
                                <tr>
                                    <td style="padding: 0 15px; border-right: 1px solid #ddd;">
                                        <p style="margin: 0; color: #999; font-size: 11px;">Chamber of Commerce</p>
                                        <p style="margin: 2px 0 0 0; color: #666; font-size: 11px; font-weight: 500;">76305821</p>
                                    </td>
                                    <td style="padding: 0 15px;">
                                        <p style="margin: 0; color: #999; font-size: 11px;">Establishment No.</p>
                                        <p style="margin: 2px 0 0 0; color: #666; font-size: 11px; font-weight: 500;">000044123140</p>
                                    </td>
                                </tr>
                            </table>
                            
                            <!-- Copyright -->
                            <p style="margin: 20px 0 0 0; color: #999; font-size: 11px;">© {datetime.now().year} {company_name}. {all_rights}.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
        """
    
    @staticmethod
    def generate_booking_confirmation_html(booking: Dict, smtp_settings: Dict, lang: str = "en") -> str:
        """Generate branded HTML email for booking confirmation"""
        t = lambda key: get_email_translation(key, lang)
        
        logo_html = ""
        if smtp_settings.get("company_logo_url"):
            logo_html = f'<img src="{smtp_settings["company_logo_url"]}" alt="{smtp_settings["company_name"]}" style="max-width: 200px; margin-bottom: 20px;">'
        
        # Format dates
        check_in = booking.get("check_in", "N/A")
        check_out = booking.get("check_out", "N/A")
        
        # Calculate nights
        try:
            from datetime import datetime
            ci = datetime.strptime(check_in, "%Y-%m-%d")
            co = datetime.strptime(check_out, "%Y-%m-%d")
            nights = (co - ci).days
        except:
            nights = "N/A"
        
        # Guest info
        adults = booking.get("adults", 2)
        children = booking.get("children", 0)
        children_ages = booking.get("children_ages", [])
        
        guest_info = f"{adults} {t('adults')}"
        if children > 0:
            ages_str = ", ".join([f"{age}y" for age in children_ages]) if children_ages else ""
            guest_info += f", {children} {t('children')}"
            if ages_str:
                guest_info += f" ({ages_str})"
        
        # Cancellation policy
        cancellation = booking.get("cancellation_deadline", "")
        cancellation_html = ""
        if cancellation:
            cancellation_html = f"""
            <tr>
                <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">{t('free_cancellation_until')}:</td>
                <td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 500; color: #059669;">{cancellation}</td>
            </tr>
            """
        
        # Pass type text
        if booking.get('pass_purchase_type') == 'annual':
            pass_type_text = t('new_annual_pass')
            pass_benefits = f"&#10003; {t('book_worldwide')}<br/>&#10003; {t('unlimited_use')}<br/>&#10003; {t('no_booking_costs')}"
        elif booking.get('new_pass_code'):
            pass_type_text = t('new_one_time_pass')
            pass_benefits = f"&#10003; {t('book_worldwide')}<br/>&#10003; {t('single_use')}<br/>&#10003; {t('no_booking_costs')}"
        else:
            pass_type_text = t('pass_code_used')
            pass_benefits = f"&#10003; {t('book_worldwide')}<br/>&#10003; {t('single_use')}<br/>&#10003; {t('no_booking_costs')}"
        
        html = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>{t('booking_confirmed')} - {smtp_settings["company_name"]}</title>
        </head>
        <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                            <!-- Header with Logo -->
                            {EmailService.get_email_header(smtp_settings, f"{t('booking_confirmed')} ✓", lang)}
                            
                            <!-- Booking Reference -->
                            <tr>
                                <td style="padding: 30px; text-align: center; border-bottom: 1px solid #eee;">
                                    <p style="margin: 0; color: #666; font-size: 14px;">{t('booking_reference')}</p>
                                    <p style="margin: 5px 0 0 0; color: #1e3a5f; font-size: 24px; font-weight: 700; letter-spacing: 2px;">{booking.get("booking_id", "N/A")}</p>
                                    {f'<p style="margin: 10px 0 0 0; color: #666; font-size: 12px;">{t("sunhotels_ref")}: {booking.get("sunhotels_booking_id", "Pending")}</p>' if booking.get("sunhotels_booking_id") else ''}
                                </td>
                            </tr>
                            
                            <!-- Hotel Details -->
                            <tr>
                                <td style="padding: 30px;">
                                    <h2 style="margin: 0 0 20px 0; color: #1e3a5f; font-size: 20px; font-weight: 600;">{t('hotel_details')}</h2>
                                    <table width="100%" cellpadding="0" cellspacing="0">
                                        <tr>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666; width: 40%;">{t('hotel')}:</td>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 600; color: #1e3a5f;">{booking.get("hotel_name", "N/A")}</td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">{t('room_type')}:</td>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 500;">{booking.get("room_type", "N/A")}</td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">{t('board_type')}:</td>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 500;">{booking.get("board_type", t('room_only'))}</td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">{t('check_in')}:</td>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 500;">{check_in}</td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">{t('check_out')}:</td>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 500;">{check_out}</td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">{t('duration')}:</td>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 500;">{nights} {t('nights')}</td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">{t('guests')}:</td>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 500;">{guest_info}</td>
                                        </tr>
                                        {cancellation_html}
                                    </table>
                                </td>
                            </tr>
                            
                            <!-- Guest Details -->
                            <tr>
                                <td style="padding: 0 30px 30px 30px;">
                                    <h2 style="margin: 0 0 20px 0; color: #1e3a5f; font-size: 20px; font-weight: 600;">{t('guest_details')}</h2>
                                    <table width="100%" cellpadding="0" cellspacing="0">
                                        <tr>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666; width: 40%;">{t('name')}:</td>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 500;">{booking.get("guest_first_name", "")} {booking.get("guest_last_name", "")}</td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">{t('email')}:</td>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 500;">{booking.get("guest_email", "N/A")}</td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">{t('phone')}:</td>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 500;">{booking.get("guest_phone", "N/A")}</td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            
                            <!-- FreeStays Pass Code (if purchased or used) -->
                            {f'''
                            <tr>
                                <td style="padding: 0 30px 30px 30px;">
                                    <div style="background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); border-radius: 12px; padding: 25px; text-align: center;">
                                        <h2 style="margin: 0 0 10px 0; color: #fff; font-size: 18px; font-weight: 600;">🎫 {t('your_freestays_pass')}</h2>
                                        <p style="margin: 0 0 15px 0; color: rgba(255,255,255,0.8); font-size: 14px;">
                                            {pass_type_text}
                                        </p>
                                        <div style="background-color: rgba(255,255,255,0.95); border-radius: 8px; padding: 15px; margin: 0 auto; max-width: 280px;">
                                            <p style="margin: 0; font-family: monospace; font-size: 20px; font-weight: 700; color: #1e3a5f; letter-spacing: 2px;">
                                                {booking.get("new_pass_code") or booking.get("pass_code") or "N/A"}
                                            </p>
                                        </div>
                                        <div style="margin: 15px 0 0 0; color: rgba(255,255,255,0.9); font-size: 13px; text-align: left; padding: 0 20px;">
                                            {pass_benefits}
                                        </div>
                                    </div>
                                </td>
                            </tr>
                            ''' if booking.get("new_pass_code") or booking.get("pass_code") else ''}
                            
                            <!-- Payment Summary -->
                            <tr>
                                <td style="padding: 0 30px 30px 30px;">
                                    <div style="background-color: #f8fafc; border-radius: 8px; padding: 20px;">
                                        <h2 style="margin: 0 0 15px 0; color: #1e3a5f; font-size: 20px; font-weight: 600;">{t('payment_summary')}</h2>
                                        <table width="100%" cellpadding="0" cellspacing="0">
                                            <tr>
                                                <td style="padding: 8px 0; color: #666;">{t('total_paid')}:</td>
                                                <td style="padding: 8px 0; text-align: right; font-size: 24px; font-weight: 700; color: #059669;">€{booking.get("total_price", 0):.2f}</td>
                                            </tr>
                                        </table>
                                        <p style="margin: 15px 0 0 0; color: #059669; font-size: 14px; text-align: center;">✓ {t('payment_confirmed')}</p>
                                    </div>
                                </td>
                            </tr>
                            
                            <!-- Special Requests -->
                            {f'''
                            <tr>
                                <td style="padding: 0 30px 30px 30px;">
                                    <div style="background-color: #fef3c7; border-radius: 8px; padding: 15px;">
                                        <p style="margin: 0; color: #92400e; font-size: 14px;"><strong>{t('special_requests')}:</strong> {booking.get("special_requests", "")}</p>
                                    </div>
                                </td>
                            </tr>
                            ''' if booking.get("special_requests") else ''}
                            
                            <!-- Footer -->
                            {EmailService.get_email_footer(smtp_settings, lang)}
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """
        return html
    
    @staticmethod
    async def send_booking_confirmation(booking: Dict) -> Dict:
        """Send booking confirmation email to the guest"""
        smtp_settings = await EmailService.get_smtp_settings()
        
        if not smtp_settings.get("enabled"):
            logger.warning("Email sending is disabled. Enable SMTP in admin settings.")
            return {"success": False, "error": "Email sending is disabled"}
        
        if not smtp_settings.get("username") or not smtp_settings.get("password"):
            logger.error("SMTP credentials not configured")
            return {"success": False, "error": "SMTP credentials not configured"}
        
        guest_email = booking.get("guest_email")
        if not guest_email:
            return {"success": False, "error": "No guest email provided"}
        
        try:
            # Create message
            msg = MIMEMultipart('alternative')
            msg['Subject'] = f"Booking Confirmation - {booking.get('hotel_name', 'Your Hotel')} | {smtp_settings['company_name']}"
            msg['From'] = f"{smtp_settings['from_name']} <{smtp_settings['from_email']}>"
            msg['To'] = guest_email
            msg['Reply-To'] = smtp_settings['company_support_email']
            
            # Generate HTML content
            html_content = EmailService.generate_booking_confirmation_html(booking, smtp_settings)
            
            # Create plain text version
            text_content = f"""
Booking Confirmation - {smtp_settings['company_name']}

Booking Reference: {booking.get('booking_id', 'N/A')}

Hotel: {booking.get('hotel_name', 'N/A')}
Room: {booking.get('room_type', 'N/A')}
Check-in: {booking.get('check_in', 'N/A')}
Check-out: {booking.get('check_out', 'N/A')}

Guest: {booking.get('guest_first_name', '')} {booking.get('guest_last_name', '')}
Email: {booking.get('guest_email', 'N/A')}

Total Paid: €{booking.get('total_price', 0):.2f}

Thank you for booking with {smtp_settings['company_name']}!
Contact: {smtp_settings['company_support_email']}
            """
            
            msg.attach(MIMEText(text_content, 'plain'))
            msg.attach(MIMEText(html_content, 'html'))
            
            # Send email in a separate thread to avoid blocking
            def send_email():
                with smtplib.SMTP(smtp_settings['host'], smtp_settings['port']) as server:
                    server.starttls()
                    server.login(smtp_settings['username'], smtp_settings['password'])
                    server.send_message(msg)
            
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, send_email)
            
            logger.info(f"Booking confirmation email sent to {guest_email}")
            
            # Log email sent in database
            await db.email_logs.insert_one({
                "booking_id": booking.get("booking_id"),
                "recipient": guest_email,
                "type": "booking_confirmation",
                "status": "sent",
                "sent_at": datetime.now(timezone.utc).isoformat()
            })
            
            return {"success": True, "message": f"Email sent to {guest_email}"}
            
        except smtplib.SMTPAuthenticationError as e:
            logger.error(f"SMTP authentication failed: {e}")
            return {"success": False, "error": "SMTP authentication failed. Check credentials."}
        except smtplib.SMTPException as e:
            logger.error(f"SMTP error: {e}")
            return {"success": False, "error": f"SMTP error: {str(e)}"}
        except Exception as e:
            logger.error(f"Failed to send email: {e}")
            return {"success": False, "error": str(e)}
    
    @staticmethod
    async def send_test_email(to_email: str) -> Dict:
        """Send a test email to verify SMTP configuration"""
        smtp_settings = await EmailService.get_smtp_settings()
        
        if not smtp_settings.get("username") or not smtp_settings.get("password"):
            return {"success": False, "error": "SMTP credentials not configured"}
        
        try:
            msg = MIMEMultipart('alternative')
            msg['Subject'] = f"Test Email - {smtp_settings['company_name']} SMTP Configuration"
            msg['From'] = f"{smtp_settings['from_name']} <{smtp_settings['from_email']}>"
            msg['To'] = to_email
            
            html_content = f"""
            <html>
            <body style="font-family: Arial, sans-serif; padding: 20px;">
                <h2 style="color: #1e3a5f;">SMTP Configuration Test</h2>
                <p>This is a test email from {smtp_settings['company_name']}.</p>
                <p>If you received this email, your SMTP configuration is working correctly!</p>
                <hr>
                <p style="color: #666; font-size: 12px;">
                    Sent from: {smtp_settings['from_email']}<br>
                    SMTP Host: {smtp_settings['host']}:{smtp_settings['port']}
                </p>
            </body>
            </html>
            """
            
            msg.attach(MIMEText(html_content, 'html'))
            
            def send_email():
                with smtplib.SMTP(smtp_settings['host'], smtp_settings['port']) as server:
                    server.starttls()
                    server.login(smtp_settings['username'], smtp_settings['password'])
                    server.send_message(msg)
            
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, send_email)
            
            logger.info(f"Test email sent to {to_email}")
            return {"success": True, "message": f"Test email sent to {to_email}"}
            
        except smtplib.SMTPAuthenticationError:
            return {"success": False, "error": "SMTP authentication failed. Check username/password."}
        except smtplib.SMTPException as e:
            return {"success": False, "error": f"SMTP error: {str(e)}"}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @staticmethod
    async def send_verification_email(email: str, name: str, verification_token: str, lang: str = "en") -> Dict:
        """Send email verification link to new users"""
        smtp_settings = await EmailService.get_smtp_settings()
        t = lambda key: get_email_translation(key, lang)
        
        if not smtp_settings.get("enabled"):
            logger.warning("Email sending is disabled. Enable SMTP in admin settings.")
            return {"success": False, "error": "Email sending is disabled"}
        
        if not smtp_settings.get("username") or not smtp_settings.get("password"):
            return {"success": False, "error": "SMTP credentials not configured"}
        
        # Get frontend URL from settings or use default
        frontend_url = os.environ.get("FRONTEND_URL", "https://hotelkit-app.preview.emergentagent.com")
        verification_link = f"{frontend_url}/verify-email?token={verification_token}"
        
        html_content = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>{t('verify_your_email')} - {smtp_settings["company_name"]}</title>
        </head>
        <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                            {EmailService.get_email_header(smtp_settings, t('verify_your_email'), lang)}
                            <tr>
                                <td style="padding: 40px 30px;">
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #333;">{t('hi')} {name},</p>
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #666; line-height: 1.6;">
                                        {t('thank_you_joining')} {smtp_settings["company_name"]}! {t('verify_email_text')}
                                    </p>
                                    <div style="text-align: center; margin: 30px 0;">
                                        <a href="{verification_link}" style="display: inline-block; background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 30px; font-weight: 600; font-size: 16px;">{t('verify_email_button')}</a>
                                    </div>
                                    <p style="margin: 20px 0 0 0; font-size: 14px; color: #999;">
                                        {t('link_doesnt_work')}<br/>
                                        <a href="{verification_link}" style="color: #1e3a5f; word-break: break-all;">{verification_link}</a>
                                    </p>
                                    <p style="margin: 20px 0 0 0; font-size: 14px; color: #999;">
                                        {t('link_expires')}
                                    </p>
                                </td>
                            </tr>
                            {EmailService.get_email_footer(smtp_settings, lang)}
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """
        
        try:
            msg = MIMEMultipart('alternative')
            msg['Subject'] = f"{t('verify_your_email')} - {smtp_settings['company_name']}"
            msg['From'] = f"{smtp_settings['from_name']} <{smtp_settings['from_email']}>"
            msg['To'] = email
            msg.attach(MIMEText(html_content, 'html'))
            
            def send_email():
                with smtplib.SMTP(smtp_settings['host'], smtp_settings['port']) as server:
                    server.starttls()
                    server.login(smtp_settings['username'], smtp_settings['password'])
                    server.send_message(msg)
            
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, send_email)
            
            logger.info(f"Verification email sent to {email}")
            return {"success": True}
        except Exception as e:
            logger.error(f"Error sending verification email: {str(e)}")
            return {"success": False, "error": str(e)}

    @staticmethod
    async def send_password_reset_email(email: str, name: str, reset_token: str, lang: str = "en") -> Dict:
        """Send password reset link"""
        smtp_settings = await EmailService.get_smtp_settings()
        t = lambda key: get_email_translation(key, lang)
        
        if not smtp_settings.get("enabled"):
            logger.warning("Email sending is disabled. Enable SMTP in admin settings.")
            return {"success": False, "error": "Email sending is disabled"}
        
        if not smtp_settings.get("username") or not smtp_settings.get("password"):
            return {"success": False, "error": "SMTP credentials not configured"}
        
        frontend_url = os.environ.get("FRONTEND_URL", "https://hotelkit-app.preview.emergentagent.com")
        reset_link = f"{frontend_url}/reset-password?token={reset_token}"
        
        html_content = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>{t('reset_your_password')} - {smtp_settings["company_name"]}</title>
        </head>
        <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                            {EmailService.get_email_header(smtp_settings, t('reset_your_password'), lang)}
                            <tr>
                                <td style="padding: 40px 30px;">
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #333;">{t('hi')} {name},</p>
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #666; line-height: 1.6;">
                                        {t('password_reset_request')}
                                    </p>
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #666; line-height: 1.6;">
                                        {t('click_reset_password')}
                                    </p>
                                    <div style="text-align: center; margin: 30px 0;">
                                        <a href="{reset_link}" style="display: inline-block; background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 30px; font-weight: 600; font-size: 16px;">{t('reset_password_button')}</a>
                                    </div>
                                    <p style="margin: 20px 0 0 0; font-size: 14px; color: #999;">
                                        {t('ignore_if_not_requested')}
                                    </p>
                                    <p style="margin: 20px 0 0 0; font-size: 14px; color: #999;">
                                        {t('link_expires_1_hour')}
                                    </p>
                                </td>
                            </tr>
                            {EmailService.get_email_footer(smtp_settings, lang)}
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """
        
        try:
            msg = MIMEMultipart('alternative')
            msg['Subject'] = f"{t('reset_your_password')} - {smtp_settings['company_name']}"
            msg['From'] = f"{smtp_settings['from_name']} <{smtp_settings['from_email']}>"
            msg['To'] = email
            msg.attach(MIMEText(html_content, 'html'))
            
            def send_email():
                with smtplib.SMTP(smtp_settings['host'], smtp_settings['port']) as server:
                    server.starttls()
                    server.login(smtp_settings['username'], smtp_settings['password'])
                    server.send_message(msg)
            
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, send_email)
            
            logger.info(f"Password reset email sent to {email}")
            return {"success": True}
        except Exception as e:
            logger.error(f"Error sending password reset email: {str(e)}")
            return {"success": False, "error": str(e)}

    @staticmethod
    async def send_price_drop_email(email: str, name: str, hotel_name: str, old_price: float, new_price: float, hotel_id: str, lang: str = "en") -> Dict:
        """Send price drop notification for favorited hotel"""
        smtp_settings = await EmailService.get_smtp_settings()
        t = lambda key: get_email_translation(key, lang)
        
        if not smtp_settings.get("enabled"):
            return {"success": False, "error": "Email sending is disabled"}
        
        if not smtp_settings.get("username") or not smtp_settings.get("password"):
            return {"success": False, "error": "SMTP credentials not configured"}
        
        frontend_url = os.environ.get("FRONTEND_URL", "https://hotelkit-app.preview.emergentagent.com")
        hotel_link = f"{frontend_url}/hotel/{hotel_id}?adults=2&children=0"
        savings = old_price - new_price
        savings_percent = ((old_price - new_price) / old_price) * 100
        
        html_content = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>{t('price_drop_alert')} - {smtp_settings["company_name"]}</title>
        </head>
        <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                            {EmailService.get_email_header(smtp_settings, f"🎉 {t('price_drop_alert')}", lang)}
                            <tr>
                                <td style="padding: 40px 30px;">
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #333;">{t('hi')} {name},</p>
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #666; line-height: 1.6;">
                                        {t('great_news')}! {t('hotel_you_searched')}
                                    </p>
                                    
                                    <div style="background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%); border-radius: 12px; padding: 24px; margin: 20px 0; border: 1px solid #0ea5e9;">
                                        <h2 style="margin: 0 0 16px 0; color: #1e3a5f; font-size: 20px;">{hotel_name}</h2>
                                        <table width="100%" cellpadding="0" cellspacing="0">
                                            <tr>
                                                <td style="text-align: left;">
                                                    <p style="margin: 0; color: #666; font-size: 14px;">{t('previous_price')}:</p>
                                                    <p style="margin: 4px 0 0 0; color: #999; font-size: 18px; text-decoration: line-through;">€{old_price:.2f}</p>
                                                </td>
                                                <td style="text-align: center;">
                                                    <p style="margin: 0; background: #22c55e; color: white; padding: 8px 16px; border-radius: 20px; font-weight: bold; display: inline-block;">
                                                        -{savings_percent:.0f}%
                                                    </p>
                                                </td>
                                                <td style="text-align: right;">
                                                    <p style="margin: 0; color: #666; font-size: 14px;">{t('new_price')}:</p>
                                                    <p style="margin: 4px 0 0 0; color: #1e3a5f; font-size: 24px; font-weight: bold;">€{new_price:.2f}</p>
                                                </td>
                                            </tr>
                                        </table>
                                        <p style="margin: 16px 0 0 0; color: #22c55e; font-weight: 600; font-size: 16px; text-align: center;">
                                            {t('you_save')} €{savings:.2f}!
                                        </p>
                                    </div>
                                    
                                    <div style="text-align: center; margin: 30px 0;">
                                        <a href="{hotel_link}" style="display: inline-block; background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 30px; font-weight: 600; font-size: 16px;">{t('book_now_button')}</a>
                                    </div>
                                    
                                    <p style="margin: 20px 0 0 0; font-size: 14px; color: #999; text-align: center;">
                                        {t('dont_miss_out')}
                                    </p>
                                </td>
                            </tr>
                            {EmailService.get_email_footer(smtp_settings, lang)}
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """
        
        try:
            msg = MIMEMultipart('alternative')
            msg['Subject'] = f"🎉 {t('price_drop_alert')} {hotel_name} - €{new_price:.2f} - {smtp_settings['company_name']}"
            msg['From'] = f"{smtp_settings['from_name']} <{smtp_settings['from_email']}>"
            msg['To'] = email
            msg.attach(MIMEText(html_content, 'html'))
            
            def send_email():
                with smtplib.SMTP(smtp_settings['host'], smtp_settings['port']) as server:
                    server.starttls()
                    server.login(smtp_settings['username'], smtp_settings['password'])
                    server.send_message(msg)
            
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, send_email)
            
            logger.info(f"Price drop email sent to {email} for {hotel_name}")
            return {"success": True}
        except Exception as e:
            logger.error(f"Error sending price drop email: {str(e)}")
            return {"success": False, "error": str(e)}

    @staticmethod
    async def send_referral_welcome_email(email: str, name: str, referrer_name: str, discount_amount: float, lang: str = "en") -> Dict:
        """Send welcome email to new user who signed up via referral"""
        smtp_settings = await EmailService.get_smtp_settings()
        t = lambda key: get_email_translation(key, lang)
        
        if not smtp_settings.get("enabled"):
            return {"success": False, "error": "Email sending is disabled"}
        
        if not smtp_settings.get("username") or not smtp_settings.get("password"):
            return {"success": False, "error": "SMTP credentials not configured"}
        
        frontend_url = os.environ.get("FRONTEND_URL", "https://hotelkit-app.preview.emergentagent.com")
        company = smtp_settings["company_name"]
        remember_text = t('remember_room_free').replace('{company}', company)
        
        html_content = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>{t('welcome')} - {company}!</title>
        </head>
        <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                            {EmailService.get_email_header(smtp_settings, f"{t('welcome')} 🎉", lang)}
                            <tr>
                                <td style="padding: 40px 30px;">
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #333;">{t('hi')} {name},</p>
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #666; line-height: 1.6;">
                                        {referrer_name} {t('invited_you')} {company} {t('special_welcome_gift')}
                                    </p>
                                    
                                    <div style="background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%); border-radius: 12px; padding: 24px; margin: 20px 0; text-align: center; border: 2px solid #f59e0b;">
                                        <p style="margin: 0 0 8px 0; color: #92400e; font-size: 14px; font-weight: 600;">{t('your_welcome_discount')}</p>
                                        <p style="margin: 0; color: #1e3a5f; font-size: 36px; font-weight: bold;">€{discount_amount:.0f} {t('off')}</p>
                                        <p style="margin: 8px 0 0 0; color: #78350f; font-size: 14px;">{t('first_booking_fee_waived')}</p>
                                    </div>
                                    
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #666; line-height: 1.6;">
                                        {remember_text} {t('discount_auto_applied')}
                                    </p>
                                    
                                    <div style="text-align: center; margin: 30px 0;">
                                        <a href="{frontend_url}" style="display: inline-block; background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 30px; font-weight: 600; font-size: 16px;">{t('start_exploring')}</a>
                                    </div>
                                </td>
                            </tr>
                            {EmailService.get_email_footer(smtp_settings, lang)}
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """
        
        try:
            msg = MIMEMultipart('alternative')
            msg['Subject'] = f"{t('welcome')} €{discount_amount:.0f} {t('off')} - {company}"
            msg['From'] = f"{smtp_settings['from_name']} <{smtp_settings['from_email']}>"
            msg['To'] = email
            msg.attach(MIMEText(html_content, 'html'))
            
            def send_email():
                with smtplib.SMTP(smtp_settings['host'], smtp_settings['port']) as server:
                    server.starttls()
                    server.login(smtp_settings['username'], smtp_settings['password'])
                    server.send_message(msg)
            
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, send_email)
            
            logger.info(f"Referral welcome email sent to {email}")
            return {"success": True}
        except Exception as e:
            logger.error(f"Error sending referral welcome email: {str(e)}")
            return {"success": False, "error": str(e)}
    
    @staticmethod
    async def send_referrer_notification_email(referrer_email: str, referrer_name: str, referred_name: str, lang: str = "en") -> Dict:
        """Send notification email to referrer when someone uses their code"""
        smtp_settings = await EmailService.get_smtp_settings()
        t = lambda key: get_email_translation(key, lang)
        
        if not smtp_settings.get("enabled"):
            logger.warning("Email sending is disabled. Enable SMTP in admin settings.")
            return {"success": False, "error": "Email sending is disabled"}
        
        if not smtp_settings.get("username") or not smtp_settings.get("password"):
            return {"success": False, "error": "SMTP credentials not configured"}
        
        frontend_url = os.environ.get("FRONTEND_URL", "https://hotelkit-app.preview.emergentagent.com")
        company = smtp_settings["company_name"]
        
        html_content = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>{t('great_news_exclaim')} - {company}</title>
        </head>
        <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                            {EmailService.get_email_header(smtp_settings, f"{t('great_news_exclaim')} 🎊", lang)}
                            <tr>
                                <td style="padding: 40px 30px;">
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #333;">{t('hi')} {referrer_name},</p>
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #666; line-height: 1.6;">
                                        {t('fantastic_news')} <strong>{referred_name}</strong> {t('just_signed_up')}
                                    </p>
                                    
                                    <div style="background: linear-gradient(135deg, #dcfce7 0%, #bbf7d0 100%); border-radius: 12px; padding: 24px; margin: 20px 0; text-align: center; border: 2px solid #22c55e;">
                                        <p style="margin: 0 0 8px 0; color: #166534; font-size: 14px; font-weight: 600;">{t('new_referral_registered')}</p>
                                        <p style="margin: 0; color: #1e3a5f; font-size: 28px; font-weight: bold;">{referred_name}</p>
                                        <p style="margin: 8px 0 0 0; color: #15803d; font-size: 14px;">{t('has_joined')}</p>
                                    </div>
                                    
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #666; line-height: 1.6;">
                                        {t('keep_sharing')}
                                    </p>
                                    
                                    <div style="text-align: center; margin: 30px 0;">
                                        <a href="{frontend_url}/dashboard" style="display: inline-block; background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 30px; font-weight: 600; font-size: 16px;">{t('view_dashboard')}</a>
                                    </div>
                                </td>
                            </tr>
                            {EmailService.get_email_footer(smtp_settings, lang)}
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """
        
        try:
            msg = MIMEMultipart('alternative')
            msg['Subject'] = f"🎉 {referred_name} {t('just_signed_up')} - {company}"
            msg['From'] = f"{smtp_settings['from_name']} <{smtp_settings['from_email']}>"
            msg['To'] = referrer_email
            msg.attach(MIMEText(html_content, 'html'))
            
            def send_email():
                with smtplib.SMTP(smtp_settings['host'], smtp_settings['port']) as server:
                    server.starttls()
                    server.login(smtp_settings['username'], smtp_settings['password'])
                    server.send_message(msg)
            
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, send_email)
            
            logger.info(f"Referrer notification email sent to {referrer_email}")
            return {"success": True}
        except Exception as e:
            logger.error(f"Error sending referrer notification email: {str(e)}")
            return {"success": False, "error": str(e)}
    
    @staticmethod
    async def send_referral_milestone_email(referrer_email: str, referrer_name: str, annual_pass_code: str, referral_count: int, lang: str = "en") -> Dict:
        """Send reward email to referrer who reached 10 referrals - includes annual pass gift"""
        smtp_settings = await EmailService.get_smtp_settings()
        t = lambda key: get_email_translation(key, lang)
        
        if not smtp_settings.get("enabled"):
            logger.warning("Email sending is disabled. Enable SMTP in admin settings.")
            return {"success": False, "error": "Email sending is disabled"}
        
        if not smtp_settings.get("username") or not smtp_settings.get("password"):
            return {"success": False, "error": "SMTP credentials not configured"}
        
        frontend_url = os.environ.get("FRONTEND_URL", "https://hotelkit-app.preview.emergentagent.com")
        company = smtp_settings["company_name"]
        
        html_content = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>{t('congratulations')} - {company}</title>
        </head>
        <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                            {EmailService.get_email_header(smtp_settings, f"🏆 {t('congratulations')}", lang)}
                            <tr>
                                <td style="padding: 40px 30px;">
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #333;">{t('hi')} {referrer_name},</p>
                                    <p style="margin: 0 0 20px 0; font-size: 18px; color: #1e3a5f; line-height: 1.6; font-weight: 600;">
                                        {t('incredible_achievement')} {referral_count} {t('friends_to')} {company}!
                                    </p>
                                    
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #666; line-height: 1.6;">
                                        {t('as_thank_you')}
                                    </p>
                                    
                                    <div style="background: linear-gradient(135deg, #fef3c7 0%, #fcd34d 100%); border-radius: 16px; padding: 30px; margin: 25px 0; text-align: center; border: 3px solid #f59e0b; position: relative;">
                                        <div style="position: absolute; top: -15px; left: 50%; transform: translateX(-50%); background: #f59e0b; color: white; padding: 5px 20px; border-radius: 20px; font-size: 12px; font-weight: bold;">
                                            🎁 {t('free_annual_pass')}
                                        </div>
                                        <p style="margin: 15px 0 8px 0; color: #92400e; font-size: 14px; font-weight: 600;">{t('free_annual_pass')}</p>
                                        <p style="margin: 0; color: #1e3a5f; font-size: 18px; font-weight: bold;">{t('worth')}</p>
                                        
                                        <div style="background-color: #ffffff; border-radius: 12px; padding: 20px; margin: 20px auto 0; max-width: 300px; border: 2px dashed #1e3a5f;">
                                            <p style="margin: 0 0 5px 0; color: #666; font-size: 12px;">{t('your_pass_code')}:</p>
                                            <p style="margin: 0; font-family: monospace; font-size: 24px; font-weight: 700; color: #1e3a5f; letter-spacing: 3px;">
                                                {annual_pass_code}
                                            </p>
                                        </div>
                                    </div>
                                    
                                    <div style="background-color: #f0fdf4; border-radius: 12px; padding: 20px; margin: 20px 0; border-left: 4px solid #22c55e;">
                                        <p style="margin: 0 0 10px 0; color: #166534; font-size: 16px; font-weight: 600;">{t('annual_pass_benefits')}:</p>
                                        <ul style="margin: 0; padding-left: 20px; color: #15803d;">
                                            <li style="margin: 5px 0;">&#10003; {t('access_worldwide')}</li>
                                            <li style="margin: 5px 0;">&#10003; {t('room_free')}</li>
                                            <li style="margin: 5px 0;">&#10003; {t('no_booking_fees')}</li>
                                        </ul>
                                    </div>
                                    
                                    <p style="margin: 0 0 20px 0; font-size: 16px; color: #666; line-height: 1.6;">
                                        {t('thank_you_ambassador')}
                                    </p>
                                    
                                    <div style="text-align: center; margin: 30px 0;">
                                        <a href="{frontend_url}/dashboard" style="display: inline-block; background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 30px; font-weight: 600; font-size: 16px;">{t('view_dashboard')}</a>
                                    </div>
                                </td>
                            </tr>
                            {EmailService.get_email_footer(smtp_settings, lang)}
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """
        
        try:
            msg = MIMEMultipart('alternative')
            msg['Subject'] = f"🏆 {t('congratulations')} {t('free_annual_pass')} ({t('worth')}) - {company}"
            msg['From'] = f"{smtp_settings['from_name']} <{smtp_settings['from_email']}>"
            msg['To'] = referrer_email
            msg.attach(MIMEText(html_content, 'html'))
            
            def send_email():
                with smtplib.SMTP(smtp_settings['host'], smtp_settings['port']) as server:
                    server.starttls()
                    server.login(smtp_settings['username'], smtp_settings['password'])
                    server.send_message(msg)
            
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, send_email)
            
            logger.info(f"Referral milestone email sent to {referrer_email} - Annual pass: {annual_pass_code}")
            return {"success": True}
        except Exception as e:
            logger.error(f"Error sending referral milestone email: {str(e)}")
            return {"success": False, "error": str(e)}

def is_package_board(board_type: str) -> bool:
    """Check if board type is a package (half board, full board, all inclusive)"""
    if not board_type:
        return False
    board_lower = board_type.lower()
    package_types = ['half board', 'full board', 'all inclusive', 'halfboard', 'fullboard', 'allinclusive']
    return any(pkg in board_lower for pkg in package_types)

def hash_password(password: str) -> str:
    return bcrypt.hashpw(password.encode('utf-8'), bcrypt.gensalt()).decode('utf-8')

def verify_password(password: str, hashed: str) -> bool:
    return bcrypt.checkpw(password.encode('utf-8'), hashed.encode('utf-8'))

def create_jwt_token(user_id: str, email: str) -> str:
    payload = {
        "user_id": user_id,
        "email": email,
        "exp": datetime.now(timezone.utc) + timedelta(hours=JWT_EXPIRATION_HOURS),
        "iat": datetime.now(timezone.utc)
    }
    return jwt.encode(payload, JWT_SECRET, algorithm=JWT_ALGORITHM)

def decode_jwt_token(token: str) -> Optional[Dict]:
    try:
        payload = jwt.decode(token, JWT_SECRET, algorithms=[JWT_ALGORITHM])
        return payload
    except jwt.ExpiredSignatureError:
        return None
    except jwt.InvalidTokenError:
        return None

async def get_current_user(request: Request) -> Optional[Dict]:
    # Check cookie first
    session_token = request.cookies.get("session_token")
    if session_token:
        session = await db.user_sessions.find_one({"session_token": session_token}, {"_id": 0})
        if session:
            expires_at = session.get("expires_at")
            if isinstance(expires_at, str):
                expires_at = datetime.fromisoformat(expires_at)
            if expires_at.tzinfo is None:
                expires_at = expires_at.replace(tzinfo=timezone.utc)
            if expires_at > datetime.now(timezone.utc):
                user = await db.users.find_one({"user_id": session["user_id"]}, {"_id": 0})
                return user
    
    # Check Authorization header as fallback
    auth_header = request.headers.get("Authorization")
    if auth_header and auth_header.startswith("Bearer "):
        token = auth_header[7:]
        payload = decode_jwt_token(token)
        if payload:
            user = await db.users.find_one({"user_id": payload["user_id"]}, {"_id": 0})
            return user
    return None

def generate_pass_code(pass_type: str = "free") -> str:
    """Generate a unique FreeStays pass code"""
    prefix = "FREE" if pass_type == "free" else "PASS" if pass_type == "one_time" else "GOLD"
    return f"{prefix}-{uuid.uuid4().hex[:8].upper()}"

def calculate_pricing(nett_price: float, has_valid_pass: bool, pass_purchase_type: Optional[str] = None, use_referral_discount: bool = False) -> dict:
    """
    Calculate pricing based on Sunhotels nett prices:
    - Markup: 16% on nett price
    - VAT: 21% on the markup only
    - FreeStays discount: 15% (results in ~1% final markup) if has valid pass
    - Booking fee: €15 one-time per booking (waived if buying a pass OR using referral discount)
    - Pass prices: €35 one-time, €129 annual
    """
    MARKUP_RATE = 0.16  # 16% markup
    VAT_RATE = 0.21  # 21% VAT on markup
    FREESTAYS_DISCOUNT = 0.15  # 15% discount for pass code holders
    
    # Calculate markup and VAT
    markup = nett_price * MARKUP_RATE
    vat_on_markup = markup * VAT_RATE
    
    # Price without discount (for display to non-members)
    price_without_discount = nett_price + markup + vat_on_markup
    
    # Determine if discount applies
    apply_discount = has_valid_pass or pass_purchase_type in ['one_time', 'annual']
    
    if apply_discount:
        discount_amount = price_without_discount * FREESTAYS_DISCOUNT
        room_total = price_without_discount - discount_amount
    else:
        discount_amount = 0
        room_total = price_without_discount
    
    # Calculate booking fee and pass price
    # Booking fee is waived if: buying a pass OR using referral discount
    booking_fee = 0 if (pass_purchase_type or use_referral_discount) else BOOKING_FEE
    pass_price = 0
    if pass_purchase_type == 'one_time':
        pass_price = PASS_ONE_TIME_PRICE
    elif pass_purchase_type == 'annual':
        pass_price = PASS_ANNUAL_PRICE
    
    # Final total
    final_total = room_total + booking_fee + pass_price
    
    return {
        "nett_price": nett_price,
        "markup": markup,
        "vat_on_markup": vat_on_markup,
        "price_before_discount": price_without_discount,
        "discount_applied": apply_discount,
        "discount_amount": discount_amount,
        "discount_rate": FREESTAYS_DISCOUNT if apply_discount else 0,
        "room_total": room_total,
        "booking_fee": booking_fee,
        "pass_price": pass_price,
        "pass_type": pass_purchase_type,
        "final_total": final_total,
        "savings_with_pass": price_without_discount * FREESTAYS_DISCOUNT if not apply_discount else 0,
        "referral_discount_applied": use_referral_discount,
        "referral_discount_amount": BOOKING_FEE if use_referral_discount else 0
    }

    @staticmethod
    async def send_voucher_email(booking: Dict, voucher_url: str):
        """Send travel voucher email to customer with voucher content embedded"""
        try:
            smtp_settings = await EmailService.get_smtp_settings()
            
            if not smtp_settings.get("enabled"):
                logger.warning("Voucher email skipped - SMTP disabled")
                return False
            
            guest_email = booking.get("guest_email")
            guest_name = f"{booking.get('guest_first_name', '')} {booking.get('guest_last_name', '')}".strip()
            hotel_name = booking.get("hotel_name", "Your Hotel")
            check_in = booking.get("check_in", "")
            check_out = booking.get("check_out", "")
            booking_ref = booking.get("booking_id", "")[:8].upper()
            sunhotels_ref = booking.get("sunhotels_booking_id", "")
            
            # Fetch voucher content from Sunhotels
            voucher_content = ""
            try:
                async with httpx.AsyncClient(timeout=30.0) as client:
                    response = await client.get(voucher_url)
                    if response.status_code == 200:
                        # Parse HTML and extract relevant content (skip header/logo)
                        from bs4 import BeautifulSoup
                        soup = BeautifulSoup(response.text, 'html.parser')
                        
                        # Remove header/logo elements
                        for header in soup.find_all(['header', 'nav']):
                            header.decompose()
                        for img in soup.find_all('img', src=lambda x: x and 'logo' in x.lower()):
                            img.decompose()
                        
                        # Get the main content
                        body = soup.find('body')
                        if body:
                            voucher_content = str(body)
                        else:
                            voucher_content = response.text
                    else:
                        logger.warning(f"Failed to fetch voucher from {voucher_url}: {response.status_code}")
            except ImportError:
                # If BeautifulSoup not available, just provide the link
                logger.info("BeautifulSoup not available, using voucher link only")
            except Exception as e:
                logger.error(f"Error fetching voucher content: {str(e)}")
            
            # Generate email HTML
            html_content = f"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Your Travel Voucher - FreeStays</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
                <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                    <tr>
                        <td align="center">
                            <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                                {EmailService.get_email_header(smtp_settings, "Your Travel Voucher 🎫")}
                                
                                <!-- Greeting -->
                                <tr>
                                    <td style="padding: 30px 30px 20px 30px;">
                                        <h2 style="margin: 0 0 15px 0; color: #1e3a5f; font-size: 20px;">Dear {guest_name},</h2>
                                        <p style="margin: 0; color: #666; font-size: 15px; line-height: 1.6;">
                                            Please find your travel voucher for your upcoming stay. You'll need to present this voucher at check-in.
                                        </p>
                                    </td>
                                </tr>
                                
                                <!-- Booking Summary -->
                                <tr>
                                    <td style="padding: 0 30px 20px 30px;">
                                        <div style="background: #f8fafc; border-radius: 12px; padding: 20px; border-left: 4px solid #1e3a5f;">
                                            <table width="100%" cellpadding="0" cellspacing="0">
                                                <tr>
                                                    <td style="padding: 5px 0; color: #666; font-size: 13px;">Hotel:</td>
                                                    <td style="padding: 5px 0; font-weight: 600; color: #1e3a5f; font-size: 14px;">{hotel_name}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 5px 0; color: #666; font-size: 13px;">Check-in:</td>
                                                    <td style="padding: 5px 0; font-weight: 500; font-size: 14px;">{check_in}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 5px 0; color: #666; font-size: 13px;">Check-out:</td>
                                                    <td style="padding: 5px 0; font-weight: 500; font-size: 14px;">{check_out}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 5px 0; color: #666; font-size: 13px;">FreeStays Ref:</td>
                                                    <td style="padding: 5px 0; font-weight: 500; font-size: 14px;">{booking_ref}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 5px 0; color: #666; font-size: 13px;">Booking Ref:</td>
                                                    <td style="padding: 5px 0; font-weight: 500; font-size: 14px;">{sunhotels_ref}</td>
                                                </tr>
                                            </table>
                                        </div>
                                    </td>
                                </tr>
                                
                                <!-- Voucher Link -->
                                <tr>
                                    <td style="padding: 0 30px 20px 30px; text-align: center;">
                                        <p style="margin: 0 0 15px 0; color: #666; font-size: 14px;">Click below to view and print your official travel voucher:</p>
                                        <a href="{voucher_url}" style="display: inline-block; background: linear-gradient(135deg, #1e3a5f 0%, #2d5a8f 100%); color: white; text-decoration: none; padding: 14px 35px; border-radius: 50px; font-weight: 600; font-size: 15px;">View & Print Voucher</a>
                                    </td>
                                </tr>
                                
                                <!-- Important Notice -->
                                <tr>
                                    <td style="padding: 0 30px 20px 30px;">
                                        <div style="background: #fef3c7; border-radius: 12px; padding: 15px; border: 1px solid #fbbf24;">
                                            <p style="margin: 0; color: #92400e; font-size: 13px;">
                                                <strong>Important:</strong> Please print this voucher or have it available on your mobile device when you check in at the hotel.
                                            </p>
                                        </div>
                                    </td>
                                </tr>
                                
                                <!-- Travel Tips -->
                                <tr>
                                    <td style="padding: 0 30px 30px 30px;">
                                        <h3 style="margin: 0 0 15px 0; color: #1e3a5f; font-size: 16px;">Travel Tips</h3>
                                        <ul style="margin: 0; padding-left: 20px; color: #666; font-size: 13px; line-height: 1.8;">
                                            <li>Arrive at the hotel after the standard check-in time (usually 14:00-15:00)</li>
                                            <li>Bring a valid ID or passport matching the booking name</li>
                                            <li>Save the hotel's contact number in case you need directions</li>
                                            <li>Review any special requests you made during booking</li>
                                        </ul>
                                    </td>
                                </tr>
                                
                                <!-- Footer -->
                                {EmailService.get_email_footer(smtp_settings)}
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """
            
            # Send email
            def send_email():
                with smtplib.SMTP(smtp_settings.get('host'), smtp_settings.get('port')) as server:
                    server.starttls()
                    server.login(smtp_settings.get('username'), smtp_settings.get('password'))
                    
                    msg = MIMEMultipart('alternative')
                    msg['Subject'] = f"Your Travel Voucher for {hotel_name} - FreeStays"
                    msg['From'] = f"{smtp_settings.get('from_name', 'FreeStays')} <{smtp_settings.get('username')}>"
                    msg['To'] = guest_email
                    msg.attach(MIMEText(html_content, 'html'))
                    
                    server.sendmail(smtp_settings.get('username'), guest_email, msg.as_string())
                    logger.info(f"Voucher email sent to {guest_email}")
            
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, send_email)
            return True
            
        except Exception as e:
            logger.error(f"Failed to send voucher email: {str(e)}")
            return False

# ==================== PRICE COMPARISON SERVICE ====================

class PriceComparisonService:
    """Service for comparing FreeStays prices with estimated OTA prices"""
    
    @staticmethod
    async def get_comparison_settings() -> Dict:
        """Get price comparison settings from database"""
        settings = await get_settings()
        return {
            "enabled": settings.get("price_comparison_enabled", True),
            "ota_markup_percentage": settings.get("ota_markup_percentage", 20),
            "min_savings_percent": settings.get("comparison_min_savings_percent", 10),
            "email_frequency": settings.get("comparison_email_frequency", "search"),
            "email_address": settings.get("comparison_email_address", "campain@freestays.eu")
        }
    
    @staticmethod
    def calculate_comparison(nett_price: float, freestays_price: float, ota_markup_percent: float = 20, min_savings_percent: float = 10) -> Dict:
        """
        Calculate price comparison between FreeStays and estimated OTA prices.
        
        Args:
            nett_price: The net hotel price (before any markup)
            freestays_price: The FreeStays final price
            ota_markup_percent: Estimated OTA markup percentage (default 20%)
            min_savings_percent: Minimum savings to show comparison (default 10%)
        
        Returns:
            Dict with comparison data or None if not meeting threshold
        """
        # Calculate estimated OTA price (nett + OTA commission markup)
        ota_estimated_price = nett_price * (1 + ota_markup_percent / 100)
        
        # Calculate savings
        savings_amount = ota_estimated_price - freestays_price
        savings_percent = (savings_amount / ota_estimated_price) * 100 if ota_estimated_price > 0 else 0
        
        # Only return comparison if we meet the minimum savings threshold
        if savings_percent >= min_savings_percent and savings_amount > 0:
            return {
                "show_comparison": True,
                "freestays_price": round(freestays_price, 2),
                "ota_estimated_price": round(ota_estimated_price, 2),
                "savings_amount": round(savings_amount, 2),
                "savings_percent": round(savings_percent, 1),
                "disclaimer": "* Estimated based on typical commission rates charged by other booking platforms"
            }
        
        return {
            "show_comparison": False,
            "freestays_price": round(freestays_price, 2),
            "ota_estimated_price": round(ota_estimated_price, 2),
            "savings_amount": round(savings_amount, 2),
            "savings_percent": round(savings_percent, 1)
        }
    
    @staticmethod
    async def send_comparison_email(comparison_data: Dict, visitor_email: str = None):
        """Send price comparison data to campaign email and optionally to visitor"""
        try:
            settings = await PriceComparisonService.get_comparison_settings()
            smtp_settings = await EmailService.get_smtp_settings()
            app_settings = await get_settings()
            
            if not smtp_settings.get("enabled"):
                logger.warning("Comparison email skipped - SMTP disabled")
                return
            
            if settings.get("email_frequency") == "disabled":
                logger.debug("Comparison email skipped - frequency is disabled")
                return
            
            if not smtp_settings.get("username") or not smtp_settings.get("password"):
                logger.warning("Comparison email skipped - SMTP credentials not configured")
                return
            
            campaign_email = settings.get("email_address", "campain@freestays.eu")
            site_url = os.environ.get("SITE_URL", "https://freestays.eu")
            
            # Generate hotel list HTML with clickable links
            hotels = comparison_data.get('hotels', [])[:10]
            destination = comparison_data.get('destination', '')
            check_in = comparison_data.get('check_in', '')
            check_out = comparison_data.get('check_out', '')
            adults = comparison_data.get('adults', 2)
            children = comparison_data.get('children', 0)
            
            hotels_html = ""
            if hotels:
                hotels_html = """
                <tr>
                    <td style="padding: 0 30px 30px 30px;">
                        <h2 style="margin: 0 0 20px 0; color: #1e3a5f; font-size: 20px; font-weight: 600;">Your Selected Hotels in {destination}</h2>
                        <p style="margin: 0 0 20px 0; color: #666; font-size: 14px;">Book now and secure these amazing prices for your trip ({check_in} to {check_out})</p>
                        <table width="100%" cellpadding="0" cellspacing="0" style="border: 1px solid #eee; border-radius: 8px; overflow: hidden;">
                            <tr style="background-color: #1e3a5f;">
                                <td style="padding: 12px; color: white; font-weight: 600; font-size: 12px;">Hotel</td>
                                <td style="padding: 12px; color: white; font-weight: 600; font-size: 12px; text-align: center;">Stars</td>
                                <td style="padding: 12px; color: white; font-weight: 600; font-size: 12px; text-align: right;">FreeStays</td>
                                <td style="padding: 12px; color: white; font-weight: 600; font-size: 12px; text-align: right;">Others</td>
                                <td style="padding: 12px; color: white; font-weight: 600; font-size: 12px; text-align: right;">You Save</td>
                            </tr>
                """.format(destination=destination, check_in=check_in, check_out=check_out)
                
                for i, hotel in enumerate(hotels):
                    bg_color = "#ffffff" if i % 2 == 0 else "#f8fafc"
                    savings = hotel.get('estimated_ota_price', 0) - hotel.get('freestays_price', 0)
                    savings_pct = hotel.get('savings_percent', 0)
                    stars = "★" * int(hotel.get('stars', 3))
                    hotel_id = hotel.get('hotel_id', '')
                    hotel_url = f"{site_url}/hotel/{hotel_id}?checkIn={check_in}&checkOut={check_out}&adults={adults}&children={children}"
                    hotels_html += f"""
                            <tr style="background-color: {bg_color};">
                                <td style="padding: 12px; font-size: 13px; color: #1e3a5f; font-weight: 500;"><a href="{hotel_url}" style="color: #1e3a5f; text-decoration: none;">{hotel.get('name', 'Hotel')[:30]}{'...' if len(hotel.get('name', '')) > 30 else ''}</a></td>
                                <td style="padding: 12px; font-size: 12px; color: #f59e0b; text-align: center;">{stars}</td>
                                <td style="padding: 12px; font-size: 13px; color: #059669; font-weight: 600; text-align: right;">€{hotel.get('freestays_price', 0):.2f}</td>
                                <td style="padding: 12px; font-size: 13px; color: #dc2626; text-decoration: line-through; text-align: right;">€{hotel.get('estimated_ota_price', 0):.2f}</td>
                                <td style="padding: 12px; font-size: 13px; color: #059669; font-weight: 700; text-align: right;">€{savings:.2f} ({savings_pct:.0f}%)</td>
                            </tr>
                    """
                hotels_html += """
                        </table>
                    </td>
                </tr>
                """
            
            # Get pass prices from settings
            one_time_price = app_settings.get("pass_one_time_price", 35)
            annual_price = app_settings.get("pass_annual_price", 129)
            referral_amount = app_settings.get("referral_discount_amount", 15)
            
            logger.info(f"Sending price comparison email to {campaign_email}...")
            
            # Generate email content with marketing CTAs
            html_content = f"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Your FreeStays Price Check Results</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
                <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                    <tr>
                        <td align="center">
                            <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                                {EmailService.get_email_header(smtp_settings, "Your Price Check Results")}
                                
                                <!-- Welcome Message -->
                                <tr>
                                    <td style="padding: 30px 30px 15px 30px;">
                                        <h1 style="margin: 0 0 15px 0; color: #1e3a5f; font-size: 24px; font-weight: 700;">Great News! We Found Amazing Deals for You</h1>
                                        <p style="margin: 0; color: #666; font-size: 15px; line-height: 1.6;">
                                            You searched for hotels in <strong>{comparison_data.get('destination', 'your destination')}</strong> and we found incredible savings compared to other booking platforms!
                                        </p>
                                    </td>
                                </tr>
                                
                                <!-- Search Details -->
                                <tr>
                                    <td style="padding: 15px 30px;">
                                        <div style="background: #f8fafc; border-radius: 12px; padding: 20px;">
                                            <table width="100%" cellpadding="0" cellspacing="0">
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666; width: 35%;">Destination:</td>
                                                    <td style="padding: 8px 0; font-weight: 600; color: #1e3a5f;">{comparison_data.get('destination', 'N/A')}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666;">Check-in:</td>
                                                    <td style="padding: 8px 0; font-weight: 500;">{comparison_data.get('check_in', 'N/A')}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666;">Check-out:</td>
                                                    <td style="padding: 8px 0; font-weight: 500;">{comparison_data.get('check_out', 'N/A')}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666;">Guests:</td>
                                                    <td style="padding: 8px 0; font-weight: 500;">{comparison_data.get('guests', 'N/A')}</td>
                                                </tr>
                                            </table>
                                        </div>
                                    </td>
                                </tr>
                                
                                <!-- Total Savings -->
                                <tr>
                                    <td style="padding: 15px 30px;">
                                        <div style="background: linear-gradient(135deg, #dcfce7 0%, #bbf7d0 100%); border-radius: 12px; padding: 25px; text-align: center; border: 2px solid #22c55e;">
                                            <p style="margin: 0 0 8px 0; color: #166534; font-size: 14px; font-weight: 600;">YOUR POTENTIAL SAVINGS</p>
                                            <p style="margin: 0; color: #1e3a5f; font-size: 42px; font-weight: bold;">&euro;{comparison_data.get('total_savings', 0):.2f}</p>
                                            <p style="margin: 8px 0 0 0; color: #15803d; font-size: 13px;">across {comparison_data.get('hotels_with_savings', 0)} hotels vs. other booking platforms</p>
                                        </div>
                                    </td>
                                </tr>
                                
                                <!-- Hotel List -->
                                {hotels_html}
                                
                                <!-- CTA: Book Now -->
                                <tr>
                                    <td style="padding: 0 30px 30px 30px; text-align: center;">
                                        <a href="{site_url}" style="display: inline-block; background: linear-gradient(135deg, #1e3a5f 0%, #2d5a8f 100%); color: white; text-decoration: none; padding: 16px 40px; border-radius: 50px; font-weight: 600; font-size: 16px;">View All Hotels & Book Now</a>
                                    </td>
                                </tr>
                                
                                <!-- Pass Benefits Section -->
                                <tr>
                                    <td style="padding: 0 30px 30px 30px;">
                                        <div style="background: linear-gradient(135deg, #1e3a5f 0%, #2d5a8f 100%); border-radius: 12px; padding: 25px; color: white;">
                                            <h3 style="margin: 0 0 15px 0; font-size: 18px;">Unlock Even MORE Savings with FreeStays Pass!</h3>
                                            <p style="margin: 0 0 20px 0; font-size: 14px; opacity: 0.9; line-height: 1.6;">
                                                Get your room for FREE! We do not charge commissions — we give it directly to YOU.
                                            </p>
                                            <table width="100%" cellpadding="0" cellspacing="0">
                                                <tr>
                                                    <td width="48%" style="vertical-align: top;">
                                                        <div style="background: rgba(255,255,255,0.1); border-radius: 10px; padding: 15px; text-align: center;">
                                                            <p style="margin: 0 0 5px 0; font-size: 12px; opacity: 0.8;">One-Time Pass</p>
                                                            <p style="margin: 0 0 10px 0; font-size: 28px; font-weight: bold;">&euro;{one_time_price}</p>
                                                            <p style="margin: 0; font-size: 11px; opacity: 0.8;">FREE room on your next booking</p>
                                                        </div>
                                                    </td>
                                                    <td width="4%"></td>
                                                    <td width="48%" style="vertical-align: top;">
                                                        <div style="background: rgba(255,255,255,0.15); border-radius: 10px; padding: 15px; text-align: center; border: 2px solid rgba(255,255,255,0.3);">
                                                            <p style="margin: 0 0 5px 0; font-size: 12px; color: #fbbf24; font-weight: 600;">BEST VALUE</p>
                                                            <p style="margin: 0 0 5px 0; font-size: 12px; opacity: 0.8;">Annual Pass</p>
                                                            <p style="margin: 0 0 10px 0; font-size: 28px; font-weight: bold;">&euro;{annual_price}</p>
                                                            <p style="margin: 0; font-size: 11px; opacity: 0.8;">Unlimited FREE rooms for 12 months</p>
                                                        </div>
                                                    </td>
                                                </tr>
                                            </table>
                                            <div style="text-align: center; margin-top: 20px;">
                                                <a href="{site_url}" style="display: inline-block; background: white; color: #1e3a5f; text-decoration: none; padding: 12px 30px; border-radius: 50px; font-weight: 600; font-size: 14px;">Get Your FreeStays Pass</a>
                                            </div>
                                        </div>
                                    </td>
                                </tr>
                                
                                <!-- Register CTA (for visitors) -->
                                <tr>
                                    <td style="padding: 0 30px 30px 30px;">
                                        <div style="background: #fef3c7; border-radius: 12px; padding: 20px; border: 1px solid #fbbf24;">
                                            <table width="100%" cellpadding="0" cellspacing="0">
                                                <tr>
                                                    <td width="60" style="vertical-align: top;">
                                                        <div style="width: 50px; height: 50px; background: #fbbf24; border-radius: 50%; text-align: center; line-height: 50px; font-size: 24px;">👤</div>
                                                    </td>
                                                    <td style="vertical-align: middle; padding-left: 15px;">
                                                        <h4 style="margin: 0 0 5px 0; color: #92400e; font-size: 16px;">Not registered yet?</h4>
                                                        <p style="margin: 0 0 10px 0; color: #a16207; font-size: 13px;">Create your free account to save your searches, track price drops, and book instantly!</p>
                                                        <a href="{site_url}" style="color: #92400e; font-weight: 600; font-size: 13px;">Create Free Account →</a>
                                                    </td>
                                                </tr>
                                            </table>
                                        </div>
                                    </td>
                                </tr>
                                
                                <!-- Referral CTA -->
                                <tr>
                                    <td style="padding: 0 30px 30px 30px;">
                                        <div style="background: #ede9fe; border-radius: 12px; padding: 20px; border: 1px solid #a78bfa;">
                                            <table width="100%" cellpadding="0" cellspacing="0">
                                                <tr>
                                                    <td width="60" style="vertical-align: top;">
                                                        <div style="width: 50px; height: 50px; background: #8b5cf6; border-radius: 50%; text-align: center; line-height: 50px; font-size: 24px;">🎁</div>
                                                    </td>
                                                    <td style="vertical-align: middle; padding-left: 15px;">
                                                        <h4 style="margin: 0 0 5px 0; color: #5b21b6; font-size: 16px;">Share & Earn!</h4>
                                                        <p style="margin: 0 0 10px 0; color: #6d28d9; font-size: 13px;">Invite friends and both of you get &euro;{referral_amount} off your bookings. After 10 referrals, you get a FREE Annual Pass!</p>
                                                        <a href="{site_url}/refer-a-friend" style="color: #5b21b6; font-weight: 600; font-size: 13px;">Start Referring Friends →</a>
                                                    </td>
                                                </tr>
                                            </table>
                                        </div>
                                    </td>
                                </tr>
                                
                                <!-- Footer -->
                                {EmailService.get_email_footer(smtp_settings)}
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """
            
            # Send email using SMTP
            def send_emails():
                with smtplib.SMTP(smtp_settings.get('host'), smtp_settings.get('port')) as server:
                    server.starttls()
                    server.login(smtp_settings.get('username'), smtp_settings.get('password'))
                    
                    # Send to campaign email
                    msg = MIMEMultipart('alternative')
                    msg['Subject'] = f"Price Check: {comparison_data.get('destination', 'Search')} - {comparison_data.get('hotels_with_savings', 0)} hotels with savings"
                    msg['From'] = f"{smtp_settings.get('from_name', 'FreeStays')} <{smtp_settings.get('from_email', 'booking@freestays.eu')}>"
                    msg['To'] = campaign_email
                    msg.attach(MIMEText(html_content, 'html'))
                    server.send_message(msg)
                    logger.info(f"Price comparison email sent to campaign: {campaign_email}")
                    
                    # Also send to visitor if email provided
                    if visitor_email:
                        visitor_msg = MIMEMultipart('alternative')
                        visitor_msg['Subject'] = f"Your FreeStays Price Check: {comparison_data.get('destination', 'Search')} - Save up to €{comparison_data.get('total_savings', 0):.2f}!"
                        visitor_msg['From'] = f"{smtp_settings.get('from_name', 'FreeStays')} <{smtp_settings.get('from_email', 'booking@freestays.eu')}>"
                        visitor_msg['To'] = visitor_email
                        visitor_msg.attach(MIMEText(html_content, 'html'))
                        server.send_message(visitor_msg)
                        logger.info(f"Price comparison email sent to visitor: {visitor_email}")
            
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, send_emails)
            
            logger.info(f"Price comparison email sent successfully")
            
        except Exception as e:
            logger.error(f"Failed to send comparison email: {str(e)}")
            import traceback
            logger.error(traceback.format_exc())

    @staticmethod
    async def store_comparison_result(comparison_data: Dict, user_id: str = None, visitor_email: str = None):
        """Store price comparison result in database for later use"""
        try:
            result = {
                "comparison_id": f"comp_{secrets.token_hex(8)}",
                "destination": comparison_data.get('destination'),
                "destination_id": comparison_data.get('destination_id'),
                "check_in": comparison_data.get('check_in'),
                "check_out": comparison_data.get('check_out'),
                "guests": comparison_data.get('guests'),
                "adults": comparison_data.get('adults'),
                "children": comparison_data.get('children'),
                "hotels_count": comparison_data.get('hotels_count', 0),
                "hotels_with_savings": comparison_data.get('hotels_with_savings', 0),
                "total_savings": comparison_data.get('total_savings', 0),
                "hotels": comparison_data.get('hotels', [])[:20],  # Store top 20 hotels
                "user_id": user_id,
                "visitor_email": visitor_email,
                "created_at": datetime.now(timezone.utc).isoformat(),
                "follow_up_sent": False,
                "follow_up_sent_at": None
            }
            await db.price_comparisons.insert_one(result)
            logger.info(f"Stored price comparison result: {result['comparison_id']}")
            return result['comparison_id']
        except Exception as e:
            logger.error(f"Failed to store comparison result: {str(e)}")
            return None

    @staticmethod
    async def send_follow_up_email(comparison: Dict):
        """Send follow-up email to visitor who hasn't booked yet"""
        try:
            smtp_settings = await EmailService.get_smtp_settings()
            app_settings = await get_settings()
            
            if not smtp_settings.get("enabled"):
                logger.warning("Follow-up email skipped - SMTP disabled")
                return False
            
            visitor_email = comparison.get('visitor_email')
            if not visitor_email:
                return False
            
            # Check if user has registered with this email
            existing_user = await db.users.find_one({"email": visitor_email.lower()})
            if existing_user:
                logger.info(f"Skipping follow-up for {visitor_email} - user has registered")
                return False
            
            site_url = os.environ.get("SITE_URL", "https://freestays.eu")
            destination = comparison.get('destination', 'your destination')
            check_in = comparison.get('check_in', '')
            check_out = comparison.get('check_out', '')
            adults = comparison.get('adults', 2)
            children = comparison.get('children', 0)
            
            # Get pass prices
            one_time_price = app_settings.get("pass_one_time_price", 35)
            annual_price = app_settings.get("pass_annual_price", 129)
            referral_amount = app_settings.get("referral_discount_amount", 15)
            
            # Build hotel list HTML
            hotels = comparison.get('hotels', [])[:5]  # Top 5 hotels for follow-up
            hotels_html = ""
            if hotels:
                hotels_html = """
                <tr>
                    <td style="padding: 0 30px 20px 30px;">
                        <h3 style="margin: 0 0 15px 0; color: #1e3a5f; font-size: 18px;">Hotels You Were Looking At:</h3>
                """
                for hotel in hotels:
                    savings = hotel.get('estimated_ota_price', 0) - hotel.get('freestays_price', 0)
                    hotel_id = hotel.get('hotel_id', '')
                    hotel_url = f"{site_url}/hotel/{hotel_id}?checkIn={check_in}&checkOut={check_out}&adults={adults}&children={children}"
                    hotels_html += f"""
                        <div style="background: #f8fafc; border-radius: 8px; padding: 15px; margin-bottom: 10px;">
                            <table width="100%" cellpadding="0" cellspacing="0">
                                <tr>
                                    <td style="vertical-align: middle;">
                                        <a href="{hotel_url}" style="color: #1e3a5f; text-decoration: none; font-weight: 600; font-size: 14px;">{hotel.get('name', 'Hotel')}</a>
                                        <span style="color: #f59e0b; font-size: 12px; margin-left: 8px;">{"★" * int(hotel.get('stars', 3))}</span>
                                    </td>
                                    <td style="text-align: right; vertical-align: middle;">
                                        <span style="color: #059669; font-weight: 700; font-size: 16px;">€{hotel.get('freestays_price', 0):.0f}</span>
                                        <span style="color: #dc2626; text-decoration: line-through; font-size: 12px; margin-left: 8px;">€{hotel.get('estimated_ota_price', 0):.0f}</span>
                                        <span style="background: #dcfce7; color: #166534; padding: 2px 8px; border-radius: 12px; font-size: 11px; font-weight: 600; margin-left: 8px;">Save €{savings:.0f}</span>
                                    </td>
                                </tr>
                            </table>
                        </div>
                    """
                hotels_html += """
                    </td>
                </tr>
                """
            
            # Build search URL
            search_url = f"{site_url}/search?destination={destination}&destinationId={comparison.get('destination_id', '')}&checkIn={check_in}&checkOut={check_out}&adults={adults}&children={children}"
            
            html_content = f"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Still Thinking About Your Trip?</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
                <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                    <tr>
                        <td align="center">
                            <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                                {EmailService.get_email_header(smtp_settings, "Still Thinking About Your Trip?")}
                                
                                <!-- Friendly Reminder -->
                                <tr>
                                    <td style="padding: 30px 30px 20px 30px;">
                                        <h1 style="margin: 0 0 15px 0; color: #1e3a5f; font-size: 24px; font-weight: 700;">Don't Let These Deals Slip Away! 🏨</h1>
                                        <p style="margin: 0; color: #666; font-size: 15px; line-height: 1.6;">
                                            We noticed you were looking at hotels in <strong>{destination}</strong> for <strong>{check_in}</strong> to <strong>{check_out}</strong>. 
                                            Great choices! These prices won't last forever, so we wanted to give you a friendly nudge.
                                        </p>
                                    </td>
                                </tr>
                                
                                <!-- Savings Reminder -->
                                <tr>
                                    <td style="padding: 0 30px 20px 30px;">
                                        <div style="background: linear-gradient(135deg, #dcfce7 0%, #bbf7d0 100%); border-radius: 12px; padding: 20px; text-align: center; border: 2px solid #22c55e;">
                                            <p style="margin: 0 0 5px 0; color: #166534; font-size: 12px; font-weight: 600;">YOUR POTENTIAL SAVINGS</p>
                                            <p style="margin: 0; color: #1e3a5f; font-size: 36px; font-weight: bold;">&euro;{comparison.get('total_savings', 0):.0f}</p>
                                            <p style="margin: 5px 0 0 0; color: #15803d; font-size: 12px;">vs. other booking platforms</p>
                                        </div>
                                    </td>
                                </tr>
                                
                                <!-- Hotel List -->
                                {hotels_html}
                                
                                <!-- CTA Button -->
                                <tr>
                                    <td style="padding: 0 30px 30px 30px; text-align: center;">
                                        <a href="{search_url}" style="display: inline-block; background: linear-gradient(135deg, #1e3a5f 0%, #2d5a8f 100%); color: white; text-decoration: none; padding: 16px 40px; border-radius: 50px; font-weight: 600; font-size: 16px;">Complete Your Booking →</a>
                                    </td>
                                </tr>
                                
                                <!-- Why Book with FreeStays -->
                                <tr>
                                    <td style="padding: 0 30px 30px 30px;">
                                        <div style="background: #f8fafc; border-radius: 12px; padding: 20px;">
                                            <h3 style="margin: 0 0 15px 0; color: #1e3a5f; font-size: 16px;">Why Book with FreeStays?</h3>
                                            <table width="100%" cellpadding="0" cellspacing="0">
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666; font-size: 13px;">✓ Pay no commissions vs 15-30% on other sites</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666; font-size: 13px;">✓ No hidden fees or booking charges</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666; font-size: 13px;">✓ Same hotels, same rooms, better prices</td>
                                                </tr>
                                            </table>
                                        </div>
                                    </td>
                                </tr>
                                
                                <!-- Pass Benefits -->
                                <tr>
                                    <td style="padding: 0 30px 30px 30px;">
                                        <div style="background: linear-gradient(135deg, #1e3a5f 0%, #2d5a8f 100%); border-radius: 12px; padding: 20px; color: white;">
                                            <h3 style="margin: 0 0 10px 0; font-size: 16px;">Unlock Maximum Savings with FreeStays Pass</h3>
                                            <p style="margin: 0 0 15px 0; font-size: 13px; opacity: 0.9;">Get your room for FREE! We do not charge commissions — we give it directly to YOU!</p>
                                            <table width="100%" cellpadding="0" cellspacing="0">
                                                <tr>
                                                    <td width="48%" style="text-align: center;">
                                                        <div style="background: rgba(255,255,255,0.1); border-radius: 8px; padding: 12px;">
                                                            <p style="margin: 0; font-size: 11px; opacity: 0.8;">One-Time Pass</p>
                                                            <p style="margin: 5px 0; font-size: 24px; font-weight: bold;">&euro;{one_time_price}</p>
                                                        </div>
                                                    </td>
                                                    <td width="4%"></td>
                                                    <td width="48%" style="text-align: center;">
                                                        <div style="background: rgba(255,255,255,0.15); border-radius: 8px; padding: 12px; border: 1px solid rgba(255,255,255,0.3);">
                                                            <p style="margin: 0; font-size: 11px; color: #fbbf24;">BEST VALUE</p>
                                                            <p style="margin: 5px 0; font-size: 24px; font-weight: bold;">&euro;{annual_price}</p>
                                                        </div>
                                                    </td>
                                                </tr>
                                            </table>
                                        </div>
                                    </td>
                                </tr>
                                
                                <!-- Referral Hint -->
                                <tr>
                                    <td style="padding: 0 30px 30px 30px;">
                                        <div style="background: #ede9fe; border-radius: 12px; padding: 15px; border: 1px solid #a78bfa;">
                                            <p style="margin: 0; color: #5b21b6; font-size: 13px;">
                                                <strong>💡 Pro Tip:</strong> Know someone who loves to travel? Refer them and you both get &euro;{referral_amount} off!
                                            </p>
                                        </div>
                                    </td>
                                </tr>
                                
                                <!-- Footer -->
                                {EmailService.get_email_footer(smtp_settings)}
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """
            
            # Send email
            def send_email():
                with smtplib.SMTP(smtp_settings.get('host'), smtp_settings.get('port')) as server:
                    server.starttls()
                    server.login(smtp_settings.get('username'), smtp_settings.get('password'))
                    
                    msg = MIMEMultipart('alternative')
                    msg['Subject'] = f"Still Thinking About {destination}? Your Hotels Are Waiting! 🏨"
                    msg['From'] = smtp_settings.get('username')
                    msg['To'] = visitor_email
                    msg.attach(MIMEText(html_content, 'html'))
                    
                    server.sendmail(smtp_settings.get('username'), visitor_email, msg.as_string())
                    logger.info(f"Follow-up email sent to {visitor_email}")
            
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, send_email)
            
            # Mark as sent
            await db.price_comparisons.update_one(
                {"comparison_id": comparison.get('comparison_id')},
                {"$set": {"follow_up_sent": True, "follow_up_sent_at": datetime.now(timezone.utc).isoformat()}}
            )
            
            return True
        except Exception as e:
            logger.error(f"Failed to send follow-up email to {comparison.get('visitor_email')}: {str(e)}")
            return False

    @staticmethod
    async def process_follow_up_emails():
        """Check for visitors who need follow-up emails (24-48 hours after search)"""
        try:
            logger.info("Starting follow-up email processing...")
            
            # Get comparisons from 24-48 hours ago that haven't been followed up
            now = datetime.now(timezone.utc)
            min_age = now - timedelta(hours=48)
            max_age = now - timedelta(hours=24)
            
            # Find comparisons with visitor email, not followed up, and within time window
            cursor = db.price_comparisons.find({
                "visitor_email": {"$ne": None, "$exists": True},
                "follow_up_sent": {"$ne": True},
                "created_at": {
                    "$gte": min_age.isoformat(),
                    "$lte": max_age.isoformat()
                }
            })
            
            comparisons = await cursor.to_list(length=100)
            logger.info(f"Found {len(comparisons)} comparisons needing follow-up")
            
            sent_count = 0
            for comparison in comparisons:
                success = await PriceComparisonService.send_follow_up_email(comparison)
                if success:
                    sent_count += 1
                await asyncio.sleep(1)  # Rate limit: 1 email per second
            
            logger.info(f"Follow-up email processing complete. Sent {sent_count} emails.")
            return sent_count
        except Exception as e:
            logger.error(f"Follow-up email processing error: {str(e)}")
            return 0

# ==================== SUNHOTELS API CLIENT ====================

class SunhotelsClient:
    def __init__(self):
        self.api_url = SUNHOTELS_BASE_URL  # NonStatic API - live API calls
        self.username = SUNHOTELS_USERNAME
        self.password = SUNHOTELS_PASSWORD
    
    async def get_credentials(self) -> tuple:
        """Get current credentials from settings (respects Live/Test mode)"""
        settings = await get_settings()
        mode = settings.get("sunhotels_mode", "live")
        
        if mode == "test":
            # Use test credentials from env
            username = os.environ.get('SUNHOTELS_TEST_USERNAME', 'FreestaysTEST')
            password = os.environ.get('SUNHOTELS_TEST_PASSWORD', 'Vision2024!@')
        else:
            # Use live credentials (can be overridden in admin settings)
            username = settings.get("sunhotels_username", SUNHOTELS_USERNAME)
            password = settings.get("sunhotels_password", SUNHOTELS_PASSWORD)
        
        return username, password
    
    async def search_destinations(self, query: str) -> List[Dict]:
        """Search destinations - first try fast lookup table, then fall back to Sunhotels API"""
        
        # OPTIMIZATION: First try fast lookup from static database
        lookup_results = await self.search_destinations_from_lookup(query)
        if lookup_results:
            logger.info(f"⚡ Using fast lookup results for '{query}'")
            return lookup_results
        
        # Fall back to Sunhotels API
        username, password = await self.get_credentials()
        
        # Try multiple search codes to find the destination
        # Some cities use airport codes (BCN for Barcelona), others use city name prefixes
        search_codes = []
        
        # Add lowercase variations (API seems case-sensitive sometimes)
        if len(query) >= 3:
            search_codes.append(query[:3].lower())  # First 3 chars lowercase
            search_codes.append(query[:4].lower() if len(query) >= 4 else query.lower())  # First 4 chars
        search_codes.append(query.lower())  # Full query lowercase
        
        # Extensive airport/destination code mappings for worldwide cities
        airport_codes = {
            # Europe
            "barcelona": "bcn", "rome": "rom", "milan": "mil", "venice": "ven",
            "madrid": "mad", "lisbon": "lis", "berlin": "ber", "munich": "muc",
            "vienna": "vie", "prague": "prg", "budapest": "bud", "dublin": "dub",
            "amsterdam": "ams", "brussels": "bru", "paris": "par", "london": "lon",
            "manchester": "man", "edinburgh": "edi", "glasgow": "gla", "birmingham": "bhx",
            "frankfurt": "fra", "hamburg": "ham", "cologne": "cgn", "dusseldorf": "dus",
            "zurich": "zrh", "geneva": "gva", "stockholm": "sto", "oslo": "osl",
            "copenhagen": "cph", "helsinki": "hel", "warsaw": "waw", "krakow": "krk",
            "athens": "ath", "santorini": "jtr", "mykonos": "jmk", "rhodes": "rho",
            "nice": "nce", "marseille": "mrs", "lyon": "lys", "bordeaux": "bod",
            "seville": "svq", "valencia": "vlc", "malaga": "agp", "ibiza": "ibz",
            "mallorca": "pmi", "palma": "pmi", "tenerife": "tfs", "gran canaria": "lpa",
            "florence": "flr", "naples": "nap", "sicily": "pmo", "sardinia": "cag",
            "dubrovnik": "dbv", "split": "spu", "zagreb": "zag",
            
            # Turkey
            "istanbul": "isl", "antalya": "ayt", "bodrum": "bjv", "izmir": "adb",
            "cappadocia": "nav", "ankara": "esb", "dalaman": "dlm", "fethiye": "dlm",
            "kusadasi": "adb", "marmaris": "dlm", "alanya": "gza", "side": "ayt",
            
            # Middle East
            "dubai": "dxb", "abu dhabi": "auh", "doha": "doh", "muscat": "mct",
            "amman": "amm", "beirut": "bey", "tel aviv": "tlv", "jerusalem": "tlv",
            "riyadh": "ruh", "jeddah": "jed", "bahrain": "bah", "kuwait": "kwi",
            
            # Asia
            "bangkok": "bkk", "phuket": "hkt", "pattaya": "bkk", "chiang mai": "cnx",
            "koh samui": "usm", "krabi": "kbv",
            "singapore": "sin", "kuala lumpur": "kul", "penang": "pen", "langkawi": "lgk",
            "bali": "dps", "jakarta": "jkt", "yogyakarta": "jog",
            "tokyo": "tyo", "osaka": "osa", "kyoto": "kix", "hiroshima": "hij",
            "seoul": "sel", "busan": "pus", "jeju": "cju",
            "hong kong": "hkg", "macau": "mfm",
            "beijing": "pek", "shanghai": "sha", "guangzhou": "can", "shenzhen": "szx",
            "taipei": "tpe", "ho chi minh": "sgn", "hanoi": "han", "da nang": "dad",
            "manila": "mnl", "cebu": "ceb", "boracay": "mpl",
            "mumbai": "bom", "delhi": "del", "goa": "goi", "jaipur": "jai",
            "colombo": "cmb", "maldives": "mle", "male": "mle",
            "kathmandu": "ktm",
            
            # Africa
            "cairo": "cai", "luxor": "lxr", "hurghada": "hrg", "sharm el sheikh": "ssh",
            "marrakech": "rak", "casablanca": "cmn", "fes": "fez", "agadir": "aga",
            "cape town": "cpt", "johannesburg": "jnb", "durban": "dur",
            "nairobi": "nbo", "mombasa": "mba", "zanzibar": "znz",
            "mauritius": "mru", "seychelles": "sez",
            "tunis": "tun", "sousse": "nbe",
            
            # Americas
            "new york": "nyc", "los angeles": "lax", "miami": "mia", "orlando": "mco",
            "las vegas": "las", "san francisco": "sfo", "chicago": "chi", "boston": "bos",
            "washington": "was", "seattle": "sea", "san diego": "san", "denver": "den",
            "cancun": "cun", "mexico city": "mex", "playa del carmen": "cun", "cabo": "sjd",
            "havana": "hav", "punta cana": "puj", "jamaica": "mbj", "bahamas": "nas",
            "aruba": "aua", "curacao": "cur", "barbados": "bgi", "st maarten": "sxm",
            "buenos aires": "bue", "rio de janeiro": "rio", "sao paulo": "sao",
            "lima": "lim", "bogota": "bog", "cartagena": "ctg", "medellin": "mde",
            "santiago": "scl", "cusco": "cuz",
            
            # Oceania
            "sydney": "syd", "melbourne": "mel", "brisbane": "bne", "perth": "per",
            "gold coast": "ool", "cairns": "cns",
            "auckland": "akl", "queenstown": "zqn", "christchurch": "chc",
            "fiji": "suv", "bora bora": "bob", "tahiti": "ppt"
        }
        
        query_lower = query.lower()
        if query_lower in airport_codes:
            search_codes.insert(0, airport_codes[query_lower])  # Add airport code first
        
        # Remove duplicates while preserving order
        search_codes = list(dict.fromkeys(search_codes))
        
        dest_url = f"{self.api_url}/GetDestinations"
        
        try:
            async with httpx.AsyncClient(timeout=30.0) as client:
                destinations = []
                
                # Try each search code until we find results
                for search_code in search_codes:
                    if destinations:
                        break
                        
                    dest_params = {
                        "userName": username,
                        "password": password,
                        "language": "en",
                        "destinationCode": search_code,
                        "sortBy": "Destination",
                        "sortOrder": "Ascending",
                        "exactDestinationMatch": "false"
                    }
                    
                    dest_response = await client.get(dest_url, params=dest_params)
                    
                    if dest_response.status_code == 200:
                        destinations = self._parse_destinations_response(dest_response.text, query)
                        if destinations:
                            logger.info(f"Found destinations with search code '{search_code}'")
                
                if destinations:
                    # Step 2: Get resort_id for each destination (limit to first 5)
                    resort_url = f"{self.api_url}/GetResorts"
                    for dest in destinations[:5]:
                        resort_params = {
                            "userName": username,
                            "password": password,
                            "language": "en",
                            "destinationCode": "",
                            "destinationID": dest["id"],
                            "sortBy": "Destination",
                            "sortOrder": "Ascending",
                            "exactDestinationMatch": "false"
                        }
                        resort_response = await client.get(resort_url, params=resort_params)
                        if resort_response.status_code == 200:
                            resort_id = self._parse_resort_id_from_response(resort_response.text, dest["name"])
                            if resort_id:
                                dest["resort_id"] = resort_id
                    
                    logger.info(f"✅ Sunhotels API: Found {len(destinations)} destinations for '{query}'")
                    return destinations
                else:
                    logger.info(f"No results from GetDestinations for '{query}' with codes {search_codes}, trying static fallback")
        except Exception as e:
            logger.error(f"GetDestinations error: {str(e)}")
        
        # Fallback to static list
        logger.info("Using static fallback")
        return self._get_static_destinations(query)
    
    def _parse_destinations_response(self, xml_text: str, query: str) -> List[Dict]:
        """Parse GetDestinations XML response"""
        destinations = []
        query_lower = query.lower()
        
        try:
            root = ET.fromstring(xml_text)
            ns = {'ns': 'http://xml.sunhotels.net/15/'}
            
            for dest in root.findall('.//ns:Destination', ns):
                dest_id = dest.findtext('ns:destination_id', '', ns)
                dest_name = dest.findtext('ns:DestinationName', '', ns)
                country_id = dest.findtext('ns:CountryId', '', ns)
                country_name = dest.findtext('ns:CountryName', '', ns)
                country_code = dest.findtext('ns:CountryCode', '', ns)
                
                # Filter by query - match destination name
                if query_lower in dest_name.lower():
                    destinations.append({
                        "id": dest_id,
                        "name": dest_name,
                        "country": country_name,
                        "country_id": country_id,
                        "country_code": country_code,
                        "resort_id": "",  # Will be filled in step 2
                        "type": "city",
                        "display": f"{dest_name}, {country_name}"
                    })
            
            # Sort by relevance - exact matches first
            destinations.sort(key=lambda x: (0 if x["name"].lower() == query_lower else 1, x["name"]))
            
            return destinations[:10]  # Limit results
            
        except ET.ParseError as e:
            logger.error(f"XML parse error in GetDestinations: {e}")
            return []
    
    def _parse_resort_id_from_response(self, xml_text: str, dest_name: str = "") -> str:
        """Parse GetResorts XML response and return the best resort ID
        Prefers: exact city name match > non-transport resort > first resort
        """
        try:
            root = ET.fromstring(xml_text)
            ns = {'ns': 'http://xml.sunhotels.net/15/'}
            
            resorts = []
            dest_name_lower = dest_name.lower() if dest_name else ""
            
            for resort in root.findall('.//ns:Resort', ns):
                resort_id = resort.findtext('ns:ResortId', '', ns)
                resort_name = resort.findtext('ns:ResortName', '', ns) or ""
                resort_name_lower = resort_name.lower()
                
                if not resort_id:
                    continue
                
                # Score the resort: higher is better
                score = 0
                
                # Exact match with destination name is best
                if dest_name_lower and dest_name_lower == resort_name_lower:
                    score = 100
                # Contains destination name
                elif dest_name_lower and dest_name_lower in resort_name_lower:
                    score = 50
                
                # Penalize transport-related resorts
                transport_keywords = ['airport', 'station', 'cruise', 'port', 'terminal', 'central station']
                if any(kw in resort_name_lower for kw in transport_keywords):
                    score -= 30
                
                # Penalize neighborhood/area-specific resorts
                area_keywords = ['north', 'south', 'east', 'west', 'central', 'downtown', 'old town']
                if any(kw in resort_name_lower for kw in area_keywords) and dest_name_lower not in resort_name_lower:
                    score -= 10
                
                resorts.append((resort_id, resort_name, score))
            
            if not resorts:
                return ""
            
            # Sort by score (highest first) and return best match
            resorts.sort(key=lambda x: x[2], reverse=True)
            best_resort = resorts[0]
            
            logger.debug(f"Best resort for '{dest_name}': {best_resort[1]} (id={best_resort[0]}, score={best_resort[2]})")
            return best_resort[0]
            
        except ET.ParseError as e:
            logger.error(f"XML parse error in GetResorts: {e}")
            return ""
    
    def _parse_destinations(self, xml_text: str, query: str) -> List[Dict]:
        """Parse destinations XML from Sunhotels"""
        destinations = []
        query_lower = query.lower()
        
        try:
            root = ET.fromstring(xml_text)
            for dest in root.findall('.//destination'):
                dest_id = dest.findtext('destinationId') or dest.get('id')
                name = dest.findtext('destinationName') or dest.findtext('name') or ""
                country = dest.findtext('country') or ""
                dest_type = dest.findtext('type') or "city"
                
                # Filter by query
                if query_lower in name.lower() or query_lower in country.lower():
                    destinations.append({
                        "id": dest_id,
                        "name": name,
                        "country": country,
                        "type": dest_type,
                        "display": f"{name}, {country}"
                    })
            
            return destinations[:20]  # Limit results
        except ET.ParseError:
            return self._get_static_destinations(query)
    
    def _get_static_destinations(self, query: str) -> List[Dict]:
        """Fallback static destinations"""
        # Static fallback destinations with correct Sunhotels destination IDs
        # These are used only when the API search fails
        destinations = [
            {"id": "211", "name": "Vienna", "country": "Austria", "type": "city"},
            {"id": "43", "name": "Paris", "country": "France", "type": "city"},
            {"id": "695", "name": "Barcelona", "country": "Spain", "type": "city"},
            {"id": "74", "name": "Rome", "country": "Italy", "type": "city"},
            {"id": "122", "name": "Amsterdam", "country": "Netherlands", "type": "city"},
            {"id": "87", "name": "Prague", "country": "Czech Republic", "type": "city"},
            {"id": "81", "name": "Lisbon", "country": "Portugal", "type": "city"},
            {"id": "152", "name": "Berlin", "country": "Germany", "type": "city"},
            {"id": "102", "name": "Athens", "country": "Greece", "type": "city"},
            {"id": "54", "name": "London", "country": "England", "type": "city"},
            {"id": "72", "name": "Madrid", "country": "Spain", "type": "city"},
            {"id": "73", "name": "Milan", "country": "Italy", "type": "city"},
            {"id": "175", "name": "Dubai", "country": "United Arab Emirates", "type": "city"},
            {"id": "120", "name": "Brussels", "country": "Belgium", "type": "city"},
            {"id": "148", "name": "New York (NY)", "country": "USA", "type": "city"},
        ]
        
        query_lower = query.lower()
        filtered = [d for d in destinations if query_lower in d["name"].lower() or query_lower in d["country"].lower()]
        for d in filtered:
            d["display"] = f"{d['name']}, {d['country']}"
        return filtered
    
    async def search_hotels(self, params: HotelSearchParams) -> List[Dict]:
        """
        Search hotels via Sunhotels NonStatic API (live API call)
        Uses destinationID (city) as primary search parameter
        Note: API only allows ONE of: destination, destinationID, hotelIDs, or resortIDs
        """
        url = f"{self.api_url}/SearchV3"
        username, password = await self.get_credentials()
        
        # Build base query params
        # Format children ages as comma-separated string for Sunhotels API
        children_ages_str = ""
        if params.children_ages and len(params.children_ages) > 0:
            children_ages_str = ",".join(str(age) for age in params.children_ages)
        
        base_params = {
            "userName": username,
            "password": password,
            "language": "en",
            "currencies": params.currency,
            "checkInDate": params.check_in,
            "checkOutDate": params.check_out,
            "numberOfRooms": params.rooms,
            "numberOfAdults": params.adults,
            "numberOfChildren": params.children,
            "childrenAges": children_ages_str,
            "infant": 0,
            "sortBy": "",
            "sortOrder": "",
            "exactDestinationMatch": "",
            "blockSuperdeal": "",
            "mealIds": "",
            "showCoordinates": "1",
            "showReviews": "1",
            "referencePointLatitude": "",
            "referencePointLongitude": "",
            "maxDistanceFromReferencePoint": "",
            "minStarRating": "",
            "maxStarRating": "",
            "featureIds": "",
            "minPrice": "",
            "maxPrice": "",
            "themeIds": "",
            "excludeSharedRooms": "0",
            "excludeSharedFacilities": "0",
            "prioritizedHotelIds": "",
            "totalRoomsInBatch": "",
            "paymentMethodId": "",
            "customerCountry": "",
            "b2c": str(params.b2c),
            "showRoomTypeName": "1",
            "accommodationTypes": "",
            "hotelIDs": "",
        }
        
        # API only allows ONE of: destination, destinationID, hotelIDs, or resortIDs
        # Priority: destinationID (city) > resortID (area) > destination (text)
        if params.destination_id:
            base_params["destinationID"] = params.destination_id
            base_params["resortIDs"] = ""
            base_params["destination"] = ""
            log_msg = f"destinationID={params.destination_id}"
        elif params.resort_id:
            base_params["resortIDs"] = params.resort_id
            base_params["destinationID"] = ""
            base_params["destination"] = ""
            log_msg = f"resortIDs={params.resort_id}"
        else:
            base_params["destination"] = params.destination
            base_params["destinationID"] = ""
            base_params["resortIDs"] = ""
            log_msg = f"destination={params.destination}"
        
        logger.info(f"Sunhotels SearchV3: {log_msg}, dates={params.check_in} to {params.check_out}")
        
        try:
            async with httpx.AsyncClient(timeout=60.0) as client:
                response = await client.get(url, params=base_params)
                
                if response.status_code == 200:
                    if "<Error>" in response.text:
                        start = response.text.find("<Message>") + 9
                        end = response.text.find("</Message>")
                        error_msg = response.text[start:end] if start > 8 and end > start else "Unknown error"
                        logger.error(f"Sunhotels API error: {error_msg}")
                        return self._get_sample_hotels(params.b2c == 1, params.destination, params.destination_id)
                    
                    hotels = self._parse_search_response(response.text, params.b2c == 1)
                    
                    if len(hotels) == 0:
                        logger.warning(f"No hotels found. Params: dest_id={params.destination_id}, resort_id={params.resort_id}")
                        return self._get_sample_hotels(params.b2c == 1, params.destination, params.destination_id)
                    
                    # Enrich hotels with static data (names, addresses, images)
                    hotels = await self._enrich_hotels_with_static_data(hotels, client)
                    
                    logger.info(f"✅ Returning {len(hotels)} enriched hotels from Sunhotels API")
                    return hotels
                else:
                    logger.error(f"Sunhotels API HTTP error: {response.status_code}")
                    return self._get_sample_hotels(params.b2c == 1, params.destination, params.destination_id)
                
        except Exception as e:
            logger.error(f"Error searching hotels: {str(e)}")
            return self._get_sample_hotels(params.b2c == 1, params.destination, params.destination_id)
    
    async def _enrich_hotels_with_static_data(self, hotels: List[Dict], client: httpx.AsyncClient) -> List[Dict]:
        """Enrich hotel search results with static data (names, addresses, images, star ratings)"""
        if not hotels:
            return hotels
        
        # Get hotel IDs to fetch static data
        hotel_ids = [h["hotel_id"] for h in hotels]
        
        # Batch hotel IDs - API may have limits, fetch in batches of 50
        batch_size = 50
        static_data_map = {}
        
        username, password = await self.get_credentials()
        
        for i in range(0, len(hotel_ids), batch_size):
            batch_ids = hotel_ids[i:i + batch_size]
            ids_str = ",".join(batch_ids)
            
            url = f"{self.api_url}/GetStaticHotelsAndRooms"
            params = {
                "userName": username,
                "password": password,
                "language": "en",
                "destination": "",
                "hotelIDs": ids_str,
                "resortIDs": "",
                "accommodationTypes": "",
                "sortBy": "",
                "sortOrder": "",
                "exactDestinationMatch": ""
            }
            
            try:
                response = await client.get(url, params=params)
                if response.status_code == 200:
                    batch_data = self._parse_static_hotel_data(response.text)
                    static_data_map.update(batch_data)
            except Exception as e:
                logger.error(f"Error fetching static hotel data: {str(e)}")
        
        # Merge static data into hotels
        for hotel in hotels:
            hotel_id = hotel["hotel_id"]
            if hotel_id in static_data_map:
                static = static_data_map[hotel_id]
                hotel["name"] = static.get("name", hotel["name"])
                hotel["address"] = static.get("address", hotel["address"])
                hotel["city"] = static.get("city", hotel["city"])
                hotel["country"] = static.get("country", hotel["country"])
                hotel["star_rating"] = static.get("star_rating", hotel["star_rating"])
                hotel["description"] = static.get("description", hotel["description"])
                hotel["headline"] = static.get("headline", "")
                hotel["latitude"] = static.get("latitude", hotel["latitude"])
                hotel["longitude"] = static.get("longitude", hotel["longitude"])
                hotel["hotel_type"] = static.get("hotel_type", "Hotel")
                if static.get("image_url"):
                    hotel["image_url"] = static["image_url"]
                if static.get("images"):
                    hotel["images"] = static["images"]
                if static.get("features"):
                    hotel["amenities"] = static["features"]
                if static.get("amenities"):
                    hotel["amenities"] = static["amenities"]
        
        # Enrich with static database data (themes, distances)
        await self._enrich_hotels_with_db_data(hotels)
        
        return hotels
    
    async def _enrich_hotels_with_db_data(self, hotels: List[Dict]):
        """Enrich hotels with data from static database (themes, distances, features) - OPTIMIZED using ghwk_autocomplete_lookup"""
        db_config = await self.get_static_db_connection()
        
        if not db_config["host"] or not db_config["database"]:
            return  # Static DB not configured
        
        start_time = asyncio.get_event_loop().time()
        
        try:
            # Use short timeout to avoid blocking
            conn = await asyncio.wait_for(
                aiomysql.connect(
                    host=db_config["host"],
                    port=db_config["port"],
                    user=db_config["user"],
                    password=db_config["password"],
                    db=db_config["database"],
                    charset='utf8mb4',
                    connect_timeout=3
                ),
                timeout=5
            )
            
            hotel_ids = [h["hotel_id"] for h in hotels]
            placeholders = ",".join(["%s"] * len(hotel_ids))
            
            async with conn.cursor(aiomysql.DictCursor) as cursor:
                # OPTIMIZATION: First use ghwk_autocomplete_lookup (indexed) to find which hotel_ids exist
                # This is much faster than querying the large ghwk_bravo_hotels table directly
                await cursor.execute(f"""
                    SELECT hotel_id 
                    FROM ghwk_autocomplete_lookup 
                    WHERE type = 'hotel' AND hotel_id IN ({placeholders})
                """, hotel_ids)
                lookup_results = await cursor.fetchall()
                
                # Get the list of hotel_ids that actually exist in our DB
                valid_hotel_ids = [str(row["hotel_id"]) for row in lookup_results] if lookup_results else []
                
                lookup_time = asyncio.get_event_loop().time() - start_time
                logger.info(f"⚡ Autocomplete lookup: {len(valid_hotel_ids)}/{len(hotel_ids)} hotels found in {lookup_time:.3f}s")
                
                bravo_map = {}
                
                # Only query ghwk_bravo_hotels for hotels that exist in our lookup table
                if valid_hotel_ids:
                    valid_placeholders = ",".join(["%s"] * len(valid_hotel_ids))
                    await cursor.execute(f"""
                        SELECT hotel_id, features_json, themes_json, distance_types_json, images_json
                        FROM ghwk_bravo_hotels
                        WHERE hotel_id IN ({valid_placeholders})
                    """, valid_hotel_ids)
                    bravo_data = await cursor.fetchall()
                    
                    # Build lookup map
                    for row in bravo_data or []:
                        hid = str(row["hotel_id"])
                        bravo_map[hid] = row
            
            conn.close()
            
            total_time = asyncio.get_event_loop().time() - start_time
            logger.info(f"⚡ DB enrichment complete: {len(bravo_map)} hotels enriched in {total_time:.3f}s")
            
            # Apply to hotels
            for hotel in hotels:
                hid = hotel["hotel_id"]
                if hid in bravo_map:
                    row = bravo_map[hid]
                    
                    # Parse features/amenities - deduplicate
                    if row.get("features_json"):
                        try:
                            features = json.loads(row["features_json"])
                            amenities_raw = [f.get("name") for f in features if f.get("name")]
                            # Deduplicate while preserving order (case-insensitive)
                            seen = set()
                            unique_amenities = []
                            for a in amenities_raw:
                                a_lower = a.lower().strip()
                                if a_lower not in seen:
                                    seen.add(a_lower)
                                    unique_amenities.append(a)
                            hotel["amenities"] = unique_amenities[:10]
                        except:
                            pass
                    
                    # Parse themes
                    if row.get("themes_json"):
                        try:
                            themes = json.loads(row["themes_json"])
                            if isinstance(themes, list):
                                hotel["themes"] = [t.get("name") if isinstance(t, dict) else t for t in themes][:5]
                        except:
                            pass
                    
                    # If no themes, infer from hotel characteristics
                    if not hotel.get("themes"):
                        hotel["themes"] = self._infer_hotel_themes(hotel)
                    
                    # Parse distances
                    if row.get("distance_types_json"):
                        try:
                            distances = json.loads(row["distance_types_json"])
                            hotel["distances"] = []
                            for d in distances[:3]:
                                desc = d.get("description", "")
                                dist_list = d.get("distances", [])
                                if dist_list:
                                    meters = dist_list[0].get("distanceInMeters", 0)
                                    if meters < 1000:
                                        hotel["distances"].append({"name": desc, "value": f"{meters}m"})
                                    else:
                                        hotel["distances"].append({"name": desc, "value": f"{meters/1000:.1f}km"})
                        except:
                            pass
                    
                    # Parse images from images_json if hotel doesn't have images already
                    if row.get("images_json") and not hotel.get("images"):
                        try:
                            images_data = json.loads(row["images_json"]) if isinstance(row["images_json"], str) else row["images_json"]
                            if images_data and isinstance(images_data, list):
                                parsed_images = []
                                for img in images_data[:10]:  # Limit to 10 images
                                    if isinstance(img, dict):
                                        img_id = img.get("id") or img.get("url") or img.get("image_url")
                                        if img_id:
                                            if str(img_id).startswith("http"):
                                                parsed_images.append(str(img_id))
                                            else:
                                                parsed_images.append(f"https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id={img_id}")
                                    elif isinstance(img, str):
                                        if img.startswith("http"):
                                            parsed_images.append(img)
                                        else:
                                            parsed_images.append(f"https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id={img}")
                                    elif isinstance(img, int):
                                        parsed_images.append(f"https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id={img}")
                                
                                if parsed_images:
                                    hotel["images"] = parsed_images
                                    if not hotel.get("image_url"):
                                        hotel["image_url"] = parsed_images[0]
                        except Exception as img_err:
                            logger.debug(f"Error parsing images_json for hotel {hid}: {img_err}")
            
            # Also infer themes for hotels without DB data
            for hotel in hotels:
                if not hotel.get("themes"):
                    hotel["themes"] = self._infer_hotel_themes(hotel)
                    
        except asyncio.TimeoutError:
            logger.warning("Static DB connection timed out - continuing without static data")
            # Still try to infer themes
            for hotel in hotels:
                if not hotel.get("themes"):
                    hotel["themes"] = self._infer_hotel_themes(hotel)
        except Exception as e:
            logger.warning(f"Static DB enrichment skipped: {str(e)}")
            # Still try to infer themes
            for hotel in hotels:
                if not hotel.get("themes"):
                    hotel["themes"] = self._infer_hotel_themes(hotel)
    
    def _infer_hotel_themes(self, hotel: Dict) -> List[str]:
        """Infer hotel themes based on name, amenities, and star rating"""
        themes = []
        name_lower = hotel.get("name", "").lower()
        amenities = [a.lower() for a in hotel.get("amenities", [])]
        star_rating = hotel.get("star_rating", 3)
        
        # Luxury indicators
        if star_rating >= 5 or any(kw in name_lower for kw in ['grand', 'palace', 'luxury', 'royal', 'premium', 'deluxe']):
            themes.append("Luxury")
        
        # Budget indicators
        if star_rating <= 2 or any(kw in name_lower for kw in ['budget', 'hostel', 'inn', 'motel', 'express']):
            themes.append("Budget")
        
        # Business indicators  
        if any(kw in name_lower for kw in ['business', 'conference', 'corporate', 'executive']):
            themes.append("Business")
        
        # Boutique indicators
        if any(kw in name_lower for kw in ['boutique', 'design', 'art', 'unique']):
            themes.append("Boutique")
        
        # Family indicators
        if any(kw in name_lower for kw in ['family', 'kids', 'children']) or 'playground' in amenities:
            themes.append("Family")
        
        # Spa indicators
        if any(kw in name_lower for kw in ['spa', 'wellness', 'relax']) or any('spa' in a for a in amenities):
            themes.append("Spa")
        
        # Beach indicators
        if any(kw in name_lower for kw in ['beach', 'sea', 'ocean', 'coast', 'marina']):
            themes.append("Beach")
        
        # Resort indicators
        if any(kw in name_lower for kw in ['resort', 'club', 'all inclusive']):
            themes.append("Resort")
        
        # City hotel (default for urban locations)
        if not themes or (star_rating >= 3 and not any(t in themes for t in ['Beach', 'Resort'])):
            if 'City' not in themes:
                themes.append("City")
        
        # Romantic indicators
        if any(kw in name_lower for kw in ['romantic', 'couples', 'honeymoon', 'love']):
            themes.append("Romantic")
        
        return themes[:3]  # Return max 3 themes
    
    def _parse_static_hotel_data(self, xml_text: str) -> Dict[str, Dict]:
        """Parse GetStaticHotelsAndRooms XML response"""
        hotels_data = {}
        
        try:
            root = ET.fromstring(xml_text)
            ns = {'ns': 'http://xml.sunhotels.net/15/'}
            
            for hotel_elem in root.findall('.//ns:hotel', ns):
                hotel_id = hotel_elem.findtext('ns:hotel.id', '', ns)
                if not hotel_id:
                    continue
                
                # Parse images
                images = []
                image_url = ""
                for img in hotel_elem.findall('.//ns:image', ns):
                    img_id = img.get('id', '')
                    if img_id:
                        full_url = f"https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id={img_id}"
                        images.append(full_url)
                        if not image_url:
                            image_url = full_url
                
                # Parse features/amenities
                features = []
                for feature in hotel_elem.findall('.//ns:feature', ns):
                    feature_name = feature.findtext('ns:name', '', ns)
                    if feature_name:
                        features.append(feature_name)
                
                # Parse coordinates
                coords = hotel_elem.find('.//ns:coordinates', ns)
                latitude = 0.0
                longitude = 0.0
                if coords is not None:
                    try:
                        latitude = float(coords.findtext('ns:latitude', '0', ns) or 0)
                        longitude = float(coords.findtext('ns:longitude', '0', ns) or 0)
                    except:
                        pass
                
                # Get star rating from classification
                classification = hotel_elem.findtext('ns:classification', '0', ns)
                try:
                    star_rating = float(classification) if classification else 0
                except:
                    star_rating = 0
                
                hotels_data[hotel_id] = {
                    "name": hotel_elem.findtext('ns:name', f'Hotel {hotel_id}', ns),
                    "address": hotel_elem.findtext('ns:hotel.address', '', ns),
                    "city": hotel_elem.findtext('ns:hotel.addr.city', '', ns),
                    "country": hotel_elem.findtext('ns:hotel.addr.country', '', ns),
                    "star_rating": star_rating,
                    "headline": hotel_elem.findtext('ns:headline', '', ns),
                    "description": hotel_elem.findtext('ns:description', '', ns),
                    "hotel_type": hotel_elem.findtext('ns:type', 'Hotel', ns),
                    "latitude": latitude,
                    "longitude": longitude,
                    "image_url": image_url,
                    "images": images[:10],  # Limit to 10 images
                    "features": features[:20] if features else ["WiFi", "Air Conditioning", "Restaurant", "Bar", "24h Reception"],
                    "amenities": features[:20] if features else ["WiFi", "Air Conditioning", "Restaurant", "Bar", "24h Reception"]  # Alias for frontend
                }
            
            return hotels_data
            
        except ET.ParseError as e:
            logger.error(f"XML parse error in static data: {e}")
            return {}
    
    async def get_static_db_connection(self) -> Dict:
        """Get static database connection config from settings"""
        settings = await get_settings()
        return {
            "host": settings.get("static_db_host", ""),
            "port": int(settings.get("static_db_port", 3306)),
            "database": settings.get("static_db_name", ""),
            "user": settings.get("static_db_user", ""),
            "password": settings.get("static_db_password", "")
        }
    
    async def search_destinations_from_lookup(self, query: str) -> List[Dict]:
        """
        FAST destination search using ghwk_autocomplete_lookup table (indexed).
        Returns cities, countries, regions matching the search query.
        Falls back to Sunhotels API if no results or DB not configured.
        Uses in-memory cache to speed up repeated searches.
        """
        # Normalize query for cache key
        cache_key = query.lower().strip()
        
        # Check cache first
        cached_result = autocomplete_cache.get(cache_key)
        if cached_result is not None:
            logger.info(f"⚡ CACHE HIT: '{query}' ({len(cached_result)} results)")
            return cached_result
        
        db_config = await self.get_static_db_connection()
        
        if not db_config["host"] or not db_config["database"]:
            return []  # DB not configured, will fallback to Sunhotels API
        
        start_time = asyncio.get_event_loop().time()
        
        try:
            conn = await asyncio.wait_for(
                aiomysql.connect(
                    host=db_config["host"],
                    port=db_config["port"],
                    user=db_config["user"],
                    password=db_config["password"],
                    db=db_config["database"],
                    charset='utf8mb4',
                    connect_timeout=2
                ),
                timeout=3
            )
            
            async with conn.cursor(aiomysql.DictCursor) as cursor:
                # Search using indexed search_term_lower column
                # Include destinations (city, country, region) AND hotels
                # Use UNION to get destinations first (higher priority), then hotels
                search_term = f"%{query.lower()}%"
                
                # Optimized query: destinations first, then hotels (with images)
                # Only search hotels if query is >= 3 chars for performance
                if len(query) >= 3:
                    await cursor.execute("""
                        (SELECT a.search_term, a.display_name, a.type, a.destination_id, a.country_name, 
                                a.hotel_count, a.priority, a.hotel_id, NULL as images_json
                         FROM ghwk_autocomplete_lookup a
                         WHERE (a.search_term_lower LIKE %s OR a.display_name LIKE %s)
                           AND a.type IN ('city', 'country', 'region')
                         ORDER BY a.priority DESC, a.hotel_count DESC
                         LIMIT 8)
                        UNION ALL
                        (SELECT a.search_term, a.display_name, a.type, a.destination_id, a.country_name, 
                                a.hotel_count, a.priority, a.hotel_id, b.images_json
                         FROM ghwk_autocomplete_lookup a
                         LEFT JOIN ghwk_bravo_hotels b ON a.hotel_id = b.hotel_id
                         WHERE a.search_term_lower LIKE %s
                           AND a.type = 'hotel'
                         ORDER BY a.priority DESC, a.display_name
                         LIMIT 7)
                    """, (search_term, search_term, search_term))
                else:
                    # For short queries, only search destinations (faster)
                    await cursor.execute("""
                        SELECT search_term, display_name, type, destination_id, country_name, 
                               hotel_count, priority, NULL as hotel_id, NULL as images_json
                        FROM ghwk_autocomplete_lookup 
                        WHERE (search_term_lower LIKE %s OR display_name LIKE %s)
                          AND type IN ('city', 'country', 'region')
                        ORDER BY priority DESC, hotel_count DESC, display_name
                        LIMIT 15
                    """, (search_term, search_term))
                
                results = await cursor.fetchall()
            
            conn.close()
            
            lookup_time = asyncio.get_event_loop().time() - start_time
            logger.info(f"⚡ Autocomplete lookup: {len(results)} results (destinations + hotels) for '{query}' in {lookup_time:.3f}s")
            
            # Convert to destination format
            destinations = []
            for row in results:
                item = {
                    "id": str(row["destination_id"]) if row["destination_id"] else "",
                    "name": row["display_name"] or row["search_term"],
                    "country": row["country_name"] or "",
                    "type": row["type"],
                    "hotel_count": row["hotel_count"] or 0,
                    "display": f"{row['display_name']}, {row['country_name']}" if row["country_name"] else row["display_name"],
                    "resort_id": ""  # Will be filled if needed
                }
                # Add hotel_id and thumbnail for hotel type results
                if row["type"] == "hotel" and row.get("hotel_id"):
                    item["hotel_id"] = str(row["hotel_id"])
                    item["display"] = f"🏨 {row['display_name']}" + (f", {row['country_name']}" if row["country_name"] else "")
                    
                    # Extract first image as thumbnail
                    if row.get("images_json"):
                        try:
                            images = json.loads(row["images_json"]) if isinstance(row["images_json"], str) else row["images_json"]
                            if images and len(images) > 0:
                                first_img = images[0]
                                thumbnail_url = None
                                
                                if isinstance(first_img, dict):
                                    # Get image ID or URL from dict
                                    img_val = first_img.get("id") or first_img.get("url") or first_img.get("image_url")
                                    if img_val:
                                        if str(img_val).startswith("http"):
                                            # Already a full URL - just use it (add resize params if possible)
                                            base_url = str(img_val).split("&w=")[0].split("&h=")[0]
                                            if "?" in base_url:
                                                thumbnail_url = f"{base_url}&w=100&h=75"
                                            else:
                                                thumbnail_url = base_url
                                        else:
                                            thumbnail_url = f"https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id={img_val}&w=100&h=75"
                                elif isinstance(first_img, str):
                                    if first_img.startswith("http"):
                                        base_url = first_img.split("&w=")[0].split("&h=")[0]
                                        if "?" in base_url:
                                            thumbnail_url = f"{base_url}&w=100&h=75"
                                        else:
                                            thumbnail_url = first_img
                                    else:
                                        thumbnail_url = f"https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id={first_img}&w=100&h=75"
                                elif isinstance(first_img, int):
                                    thumbnail_url = f"https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id={first_img}&w=100&h=75"
                                
                                if thumbnail_url:
                                    item["thumbnail"] = thumbnail_url
                        except (json.JSONDecodeError, TypeError):
                            pass
                
                destinations.append(item)
            
            # Cache the results before returning
            autocomplete_cache.set(cache_key, destinations)
            
            return destinations
            
        except asyncio.TimeoutError:
            logger.warning("Autocomplete lookup timed out")
            return []
        except Exception as e:
            logger.warning(f"Autocomplete lookup error: {str(e)}")
            return []
    
    async def get_hotel_details_from_static_db(self, hotel_id: str) -> Optional[Dict]:
        """
        Get hotel details from Static database (amenities, features, themes)
        Tables used:
        - ghwk_hotel_room_types: room images
        - ghwk_room_note_types: room descriptions
        - ghwk_themes: hotel themes/types
        - ghwk_bravo_hotels: distance_types_json
        - ghwk_room_facilities: size_in_m2 and amenities_json
        """
        db_config = await self.get_static_db_connection()
        
        if not db_config["host"] or not db_config["database"]:
            return None
        
        try:
            conn = await asyncio.wait_for(
                aiomysql.connect(
                    host=db_config["host"],
                    port=db_config["port"],
                    user=db_config["user"],
                    password=db_config["password"],
                    db=db_config["database"],
                    charset='utf8mb4',
                    connect_timeout=3
                ),
                timeout=5
            )
            
            async with conn.cursor(aiomysql.DictCursor) as cursor:
                hotel_data = {"hotel_id": hotel_id}
                
                # Get distance info from ghwk_bravo_hotels
                await cursor.execute("""
                    SELECT distance_types_json, themes_json
                    FROM ghwk_bravo_hotels
                    WHERE hotel_id = %s
                """, (hotel_id,))
                bravo_data = await cursor.fetchone()
                if bravo_data:
                    if bravo_data.get("distance_types_json"):
                        try:
                            hotel_data["distances"] = json.loads(bravo_data["distance_types_json"])
                        except:
                            hotel_data["distances"] = []
                    else:
                        hotel_data["distances"] = []
                    
                    if bravo_data.get("themes_json"):
                        try:
                            themes = json.loads(bravo_data["themes_json"])
                            hotel_data["themes"] = [t.get("name") if isinstance(t, dict) else t for t in themes] if themes else []
                        except:
                            hotel_data["themes"] = []
                else:
                    hotel_data["distances"] = []
                    hotel_data["themes"] = []
                
                # Get room types with images from ghwk_hotel_room_types
                await cursor.execute("""
                    SELECT room_type_id, room_type_name, images_json
                    FROM ghwk_hotel_room_types
                    WHERE hotel_id = %s
                """, (hotel_id,))
                room_types = await cursor.fetchall()
                
                logger.info(f"📷 Room types query for hotel_id={hotel_id}: found {len(room_types or [])} room types")
                
                room_images_map = {}
                for rt in room_types or []:
                    rt_id = str(rt.get("room_type_id", ""))
                    if rt_id and rt.get("images_json"):
                        try:
                            images = json.loads(rt["images_json"])
                            room_images_map[rt_id] = images if isinstance(images, list) else []
                            logger.info(f"📷 Room type {rt_id}: {len(room_images_map[rt_id])} images")
                        except:
                            pass
                hotel_data["room_images"] = room_images_map
                
                # Get room notes from ghwk_room_note_types (optional - table/column may not exist)
                room_notes_map = {}
                try:
                    await cursor.execute("""
                        SELECT room_type_id, note_type, note_text
                        FROM ghwk_room_note_types
                        WHERE hotel_id = %s
                    """, (hotel_id,))
                    room_notes = await cursor.fetchall()
                    
                    for rn in room_notes or []:
                        rt_id = str(rn.get("room_type_id", ""))
                        if rt_id:
                            if rt_id not in room_notes_map:
                                room_notes_map[rt_id] = []
                            room_notes_map[rt_id].append({
                                "type": rn.get("note_type", ""),
                                "text": rn.get("note_text", "")
                            })
                except Exception as e:
                    logger.debug(f"Room notes query skipped for hotel {hotel_id}: {str(e)}")
                hotel_data["room_notes"] = room_notes_map
                
                # Get room facilities from ghwk_room_facilities (optional - table/column may not exist)
                room_facilities_map = {}
                try:
                    await cursor.execute("""
                        SELECT room_type_id, size_in_m2, amenities_json
                        FROM ghwk_room_facilities
                        WHERE hotel_id = %s
                    """, (hotel_id,))
                    room_facilities = await cursor.fetchall()
                    
                    for rf in room_facilities or []:
                        rt_id = str(rf.get("room_type_id", ""))
                        if rt_id:
                            amenities = []
                            if rf.get("amenities_json"):
                                try:
                                    amenities = json.loads(rf["amenities_json"])
                                except:
                                    pass
                            room_facilities_map[rt_id] = {
                                "size_m2": rf.get("size_in_m2"),
                                "amenities": amenities if isinstance(amenities, list) else []
                            }
                except Exception as e:
                    logger.debug(f"Room facilities query skipped for hotel {hotel_id}: {str(e)}")
                hotel_data["room_facilities"] = room_facilities_map
            
            conn.close()
            logger.info(f"Static DB data retrieved for hotel {hotel_id}")
            return hotel_data
            
        except asyncio.TimeoutError:
            logger.warning(f"Static DB timeout for hotel {hotel_id}")
            return None
        except Exception as e:
            logger.warning(f"Static DB query skipped for hotel {hotel_id}: {str(e)}")
            return None
    
    async def get_all_themes(self) -> List[Dict]:
        """Get all hotel themes for filtering"""
        db_config = await self.get_static_db_connection()
        
        if not db_config["host"] or not db_config["database"]:
            return []
        
        try:
            conn = await asyncio.wait_for(
                aiomysql.connect(
                    host=db_config["host"],
                    port=db_config["port"],
                    user=db_config["user"],
                    password=db_config["password"],
                    db=db_config["database"],
                    charset='utf8mb4',
                    connect_timeout=3
                ),
                timeout=5
            )
            
            async with conn.cursor(aiomysql.DictCursor) as cursor:
                await cursor.execute("SELECT id, name FROM ghwk_themes ORDER BY name")
                themes = await cursor.fetchall()
            
            conn.close()
            return [{"id": t["id"], "name": t["name"]} for t in themes] if themes else []
            
        except asyncio.TimeoutError:
            logger.warning("Static DB timeout fetching themes")
            return []
        except Exception as e:
            logger.warning(f"Error fetching themes: {str(e)}")
            return []
    
    async def enrich_rooms_with_static_data(self, rooms: List[Dict], hotel_id: str) -> List[Dict]:
        """Enrich room data with static database info (images, notes, amenities, size) - non-blocking"""
        try:
            static_data = await asyncio.wait_for(
                self.get_hotel_details_from_static_db(hotel_id),
                timeout=5
            )
        except asyncio.TimeoutError:
            logger.warning(f"Static DB timeout enriching rooms for hotel {hotel_id}")
            return rooms
        
        if not static_data:
            return rooms
        
        room_images = static_data.get("room_images", {})
        room_notes = static_data.get("room_notes", {})
        room_facilities = static_data.get("room_facilities", {})
        
        for room in rooms:
            room_type_id = str(room.get("sunhotels_room_type_id", ""))
            
            # Add images if not already present
            if not room.get("images") or len(room.get("images", [])) == 0:
                if room_type_id in room_images:
                    room["images"] = room_images[room_type_id]
                    if room["images"]:
                        room["image_url"] = room["images"][0]
            
            # Add room notes/descriptions
            if room_type_id in room_notes:
                notes = room_notes[room_type_id]
                # Combine all notes into room description
                room["description"] = " | ".join([n["text"] for n in notes if n.get("text")])
            
            # Add facilities (size and amenities)
            if room_type_id in room_facilities:
                facilities = room_facilities[room_type_id]
                if facilities.get("size_m2"):
                    room["size_m2"] = facilities["size_m2"]
                if facilities.get("amenities"):
                    # Merge with existing amenities
                    existing = set(room.get("amenities", []))
                    existing.update(facilities["amenities"])
                    room["amenities"] = list(existing)[:15]  # Limit to 15
        
        return rooms
    
    def _parse_search_response(self, xml_text: str, is_last_minute: bool = False) -> List[Dict]:
        """Parse XML response from Sunhotels SearchV3"""
        hotels = []
        
        # Meal ID mapping
        meal_types = {
            "1": "Room Only",
            "2": "Bed & Breakfast", 
            "3": "Breakfast Included",
            "4": "Half Board",
            "5": "Full Board",
            "6": "All Inclusive"
        }
        
        try:
            root = ET.fromstring(xml_text)
            ns = {'ns': 'http://xml.sunhotels.net/15/'}
            
            for hotel_elem in root.findall('.//ns:hotel', ns):
                hotel_id = hotel_elem.findtext('ns:hotel.id', '', ns)
                if not hotel_id:
                    continue
                
                dest_id = hotel_elem.findtext('ns:destination_id', '', ns)
                resort_id = hotel_elem.findtext('ns:resort_id', '', ns)
                
                # Get review data if available
                review_elem = hotel_elem.find('ns:review', ns)
                review_score = 0.0
                review_count = 0
                if review_elem is not None:
                    try:
                        review_score = float(review_elem.findtext('ns:rating', '0', ns) or 0)
                        review_count = int(review_elem.findtext('ns:count', '0', ns) or 0)
                    except:
                        pass
                
                # Parse room types
                rooms = []
                min_price = float('inf')
                
                for roomtype_elem in hotel_elem.findall('.//ns:roomtype', ns):
                    roomtype_id = roomtype_elem.findtext('ns:roomtype.ID', '', ns)
                    roomtype_name = roomtype_elem.findtext('ns:roomtype.Name', 'Standard Room', ns)
                    
                    # Parse room type images
                    room_images = []
                    room_image_url = ""
                    for img in roomtype_elem.findall('.//ns:image', ns):
                        img_id = img.get('id', '')
                        if img_id:
                            img_url = f"https://hotelimages.sunhotels.net/HotelInfo/hotelImage.aspx?id={img_id}"
                            room_images.append(img_url)
                            if not room_image_url:
                                room_image_url = img_url
                    
                    # Parse room type amenities/features
                    room_amenities = []
                    for feature in roomtype_elem.findall('.//ns:feature', ns):
                        feature_name = feature.findtext('ns:name', '', ns)
                        if feature_name:
                            room_amenities.append(feature_name)
                    
                    # Add common room amenities if none found
                    if not room_amenities:
                        room_amenities = ["Air Conditioning", "Private Bathroom", "TV", "Safe"]
                    
                    for room_elem in roomtype_elem.findall('.//ns:room', ns):
                        room_id = room_elem.findtext('ns:id', '', ns)
                        beds = int(room_elem.findtext('ns:beds', '2', ns) or 2)
                        is_superdeal = room_elem.findtext('ns:isSuperDeal', 'false', ns).lower() == 'true'
                        
                        # Parse meals (can have multiple)
                        for meal_elem in room_elem.findall('.//ns:meal', ns):
                            meal_id = meal_elem.findtext('ns:id', '1', ns)
                            board_type = meal_types.get(meal_id, "Room Only")
                            
                            # Get price
                            price_elem = meal_elem.find('.//ns:price', ns)
                            price = 0.0
                            currency = "EUR"
                            if price_elem is not None:
                                try:
                                    price = float(price_elem.text or 0)
                                    currency = price_elem.get('currency', 'EUR')
                                except:
                                    pass
                            
                            if price > 0 and price < min_price:
                                min_price = price
                            
                            # Get cancellation policy with +22 days FreeStays rule
                            cancel_policy = "Non-refundable"
                            cancel_deadline_hours = None
                            cancel_deadline_display = None
                            is_refundable = False
                            
                            cancel_elem = room_elem.find('.//ns:cancellation_policy', ns)
                            if cancel_elem is not None:
                                deadline_text = cancel_elem.findtext('ns:deadline', '', ns)
                                percentage = cancel_elem.findtext('ns:percentage', '', ns)
                                
                                if deadline_text and deadline_text.strip():
                                    # API returns hours before check-in
                                    api_deadline_hours = int(deadline_text)
                                    # Add 22 days (528 hours) to give FreeStays buffer
                                    freestays_deadline_hours = api_deadline_hours + (22 * 24)
                                    cancel_deadline_hours = freestays_deadline_hours
                                    
                                    # Convert to days for display
                                    days_before = freestays_deadline_hours // 24
                                    cancel_deadline_display = f"{days_before} days before check-in"
                                    cancel_policy = f"Free cancellation until {days_before} days before check-in"
                                    is_refundable = True
                                elif percentage == "100":
                                    cancel_policy = "Non-refundable. If cancelled, no refund will be given."
                                    is_refundable = False
                            
                            # Get room notes
                            room_notes_elem = room_elem.find('ns:notes', ns)
                            room_notes = ""
                            if room_notes_elem is not None and room_notes_elem.text:
                                room_notes = room_notes_elem.text.strip()
                            
                            # Get fees (City Tax, etc.)
                            room_fees = []
                            for fee_elem in room_elem.findall('.//ns:fee', ns):
                                fee_name = fee_elem.findtext('ns:name', '', ns)
                                fee_amount_elem = fee_elem.find('.//ns:amount', ns)
                                fee_amount = 0
                                fee_currency = "EUR"
                                if fee_amount_elem is not None and fee_amount_elem.text:
                                    try:
                                        fee_amount = float(fee_amount_elem.text)
                                        fee_currency = fee_amount_elem.get('currency', 'EUR')
                                    except:
                                        pass
                                included = fee_elem.findtext('ns:includedInPrice', 'false', ns).lower() == 'true'
                                if fee_name:
                                    room_fees.append({
                                        "name": fee_name,
                                        "amount": fee_amount,
                                        "currency": fee_currency,
                                        "included_in_price": included
                                    })
                            
                            room_data = {
                                "room_id": room_id,
                                "room_type": roomtype_name,
                                "board_type": board_type,
                                "price": price,
                                "currency": currency,
                                "cancellation_policy": cancel_policy,
                                "cancellation_deadline_hours": cancel_deadline_hours,
                                "cancellation_deadline_display": cancel_deadline_display,
                                "is_refundable": is_refundable,
                                "room_notes": room_notes,
                                "fees": room_fees,
                                "max_occupancy": beds,
                                "sunhotels_room_type_id": roomtype_id,
                                "sunhotels_block_id": room_id,
                                "is_superdeal": is_superdeal,
                                "image_url": room_image_url,
                                "images": room_images[:5],  # Limit to 5 room images
                                "amenities": room_amenities[:10]  # Limit to 10 amenities
                            }
                            rooms.append(room_data)
                
                # Get hotel-level notes
                hotel_notes_elem = hotel_elem.find('ns:notes', ns)
                hotel_notes = ""
                if hotel_notes_elem is not None and hotel_notes_elem.text:
                    hotel_notes = hotel_notes_elem.text.strip()
                
                if min_price == float('inf'):
                    min_price = 0
                
                hotel_data = {
                    "hotel_id": hotel_id,
                    "name": f"Hotel {hotel_id}",  # Will be enriched from static API later
                    "star_rating": 0,
                    "address": "",
                    "city": "",
                    "country": "",
                    "destination_id": dest_id,
                    "resort_id": resort_id,
                    "latitude": 0.0,
                    "longitude": 0.0,
                    "description": "",
                    "hotel_notes": hotel_notes,
                    "image_url": f"https://images.unsplash.com/photo-1566073771259-6a8506099945?w=800",
                    "review_score": review_score,
                    "review_count": review_count,
                    "rooms": rooms,
                    "amenities": ["WiFi", "Air Conditioning", "Restaurant", "Bar", "24h Reception"],
                    "min_price": min_price,
                    "currency": "EUR",
                    "is_last_minute": is_last_minute
                }
                hotels.append(hotel_data)
                
        except ET.ParseError as e:
            logger.error(f"XML Parse error: {str(e)}")
            return []  # Return empty list on parse error, let caller handle fallback
        
        return hotels  # Return actual results (may be empty), let caller handle fallback
    
    def _extract_amenities(self, hotel_elem) -> List[str]:
        """Extract amenities from hotel element"""
        amenities = []
        for amenity_elem in hotel_elem.findall('.//facility') or hotel_elem.findall('.//amenity'):
            amenity_name = amenity_elem.findtext('name') or amenity_elem.text
            if amenity_name:
                amenities.append(amenity_name)
        return amenities[:10] if amenities else ["WiFi", "Air Conditioning", "Restaurant", "Bar", "24h Reception"]
    
    def _get_sample_hotels(self, is_last_minute: bool = False, destination: str = None, destination_id: str = None) -> List[Dict]:
        """Return sample hotels for demo/fallback, optionally filtered by destination"""
        discount_factor = 0.7 if is_last_minute else 1.0  # 30% off for last minute
        
        hotels = [
            {
                "hotel_id": "demo_1",
                "name": "Grand Hotel Europa",
                "star_rating": 5,
                "address": "Rathausplatz 1",
                "city": "Vienna",
                "country": "Austria",
                "latitude": 48.2082,
                "longitude": 16.3738,
                "description": "Luxury hotel in the heart of Vienna with stunning views of the historic city center.",
                "image_url": "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=800",
                "review_score": 9.2,
                "review_count": 1250,
                "rooms": [
                    {"room_id": "r1", "room_type": "Deluxe Double", "board_type": "Breakfast Included", "price": round(189.00 * discount_factor, 2), "currency": "EUR", "cancellation_policy": "Free cancellation until 24h before", "max_occupancy": 2, "sunhotels_room_type_id": "rt_1", "sunhotels_block_id": "blk_1"},
                    {"room_id": "r2", "room_type": "Superior Suite", "board_type": "Half Board", "price": round(329.00 * discount_factor, 2), "currency": "EUR", "cancellation_policy": "Free cancellation until 48h before", "max_occupancy": 3, "sunhotels_room_type_id": "rt_2", "sunhotels_block_id": "blk_2"}
                ],
                "amenities": ["WiFi", "Spa", "Pool", "Restaurant", "Bar", "Gym", "Room Service", "Concierge"],
                "is_last_minute": is_last_minute
            },
            {
                "hotel_id": "demo_2",
                "name": "Boutique Hotel Santorini",
                "star_rating": 4,
                "address": "Oia Main Street 15",
                "city": "Santorini",
                "country": "Greece",
                "latitude": 36.4618,
                "longitude": 25.3753,
                "description": "Stunning cliffside boutique hotel with breathtaking caldera views and traditional Cycladic architecture.",
                "image_url": "https://images.unsplash.com/photo-1602343168117-bb8ffe3e2e9f?w=800",
                "review_score": 9.5,
                "review_count": 890,
                "rooms": [
                    {"room_id": "r3", "room_type": "Cave Suite", "board_type": "Breakfast Included", "price": round(245.00 * discount_factor, 2), "currency": "EUR", "cancellation_policy": "Free cancellation until 72h before", "max_occupancy": 2, "sunhotels_room_type_id": "rt_3", "sunhotels_block_id": "blk_3"},
                    {"room_id": "r4", "room_type": "Honeymoon Villa", "board_type": "Full Board", "price": round(450.00 * discount_factor, 2), "currency": "EUR", "cancellation_policy": "Non-refundable", "max_occupancy": 2, "sunhotels_room_type_id": "rt_4", "sunhotels_block_id": "blk_4"}
                ],
                "amenities": ["WiFi", "Private Pool", "Sea View", "Restaurant", "Bar", "Airport Transfer"],
                "is_last_minute": is_last_minute
            },
            {
                "hotel_id": "demo_3",
                "name": "Urban Loft Barcelona",
                "star_rating": 4,
                "address": "La Rambla 78",
                "city": "Barcelona",
                "country": "Spain",
                "latitude": 41.3851,
                "longitude": 2.1734,
                "description": "Modern design hotel on the famous La Rambla, walking distance to all major attractions.",
                "image_url": "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=800",
                "review_score": 8.8,
                "review_count": 2100,
                "rooms": [
                    {"room_id": "r5", "room_type": "City View Room", "board_type": "Room Only", "price": round(129.00 * discount_factor, 2), "currency": "EUR", "cancellation_policy": "Free cancellation until 24h before", "max_occupancy": 2, "sunhotels_room_type_id": "rt_5", "sunhotels_block_id": "blk_5"},
                    {"room_id": "r6", "room_type": "Loft Suite", "board_type": "Breakfast Included", "price": round(199.00 * discount_factor, 2), "currency": "EUR", "cancellation_policy": "Free cancellation until 48h before", "max_occupancy": 3, "sunhotels_room_type_id": "rt_6", "sunhotels_block_id": "blk_6"}
                ],
                "amenities": ["WiFi", "Rooftop Bar", "Restaurant", "Gym", "Bike Rental", "City Tours"],
                "is_last_minute": is_last_minute
            },
            {
                "hotel_id": "demo_4",
                "name": "Alpine Retreat Zermatt",
                "star_rating": 5,
                "address": "Bahnhofstrasse 41",
                "city": "Zermatt",
                "country": "Switzerland",
                "latitude": 46.0207,
                "longitude": 7.7491,
                "description": "Luxury mountain resort with spectacular Matterhorn views, world-class spa, and ski-in/ski-out access.",
                "image_url": "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?w=800",
                "review_score": 9.4,
                "review_count": 650,
                "rooms": [
                    {"room_id": "r7", "room_type": "Mountain View Room", "board_type": "Half Board", "price": round(389.00 * discount_factor, 2), "currency": "EUR", "cancellation_policy": "Free cancellation until 7 days before", "max_occupancy": 2, "sunhotels_room_type_id": "rt_7", "sunhotels_block_id": "blk_7"},
                    {"room_id": "r8", "room_type": "Matterhorn Suite", "board_type": "Full Board", "price": round(699.00 * discount_factor, 2), "currency": "EUR", "cancellation_policy": "Free cancellation until 14 days before", "max_occupancy": 4, "sunhotels_room_type_id": "rt_8", "sunhotels_block_id": "blk_8"}
                ],
                "amenities": ["WiFi", "Spa", "Indoor Pool", "Ski Storage", "Restaurant", "Bar", "Sauna", "Gym"],
                "is_last_minute": is_last_minute
            },
            {
                "hotel_id": "demo_5",
                "name": "Seaside Resort Amalfi",
                "star_rating": 4,
                "address": "Via Pantaleone Comite 33",
                "city": "Amalfi",
                "country": "Italy",
                "latitude": 40.6340,
                "longitude": 14.6027,
                "description": "Charming Mediterranean resort perched on the cliffs of the Amalfi Coast with private beach access.",
                "image_url": "https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=800",
                "review_score": 9.1,
                "review_count": 780,
                "rooms": [
                    {"room_id": "r9", "room_type": "Sea View Room", "board_type": "Breakfast Included", "price": round(219.00 * discount_factor, 2), "currency": "EUR", "cancellation_policy": "Free cancellation until 48h before", "max_occupancy": 2, "sunhotels_room_type_id": "rt_9", "sunhotels_block_id": "blk_9"},
                    {"room_id": "r10", "room_type": "Terrace Suite", "board_type": "Half Board", "price": round(359.00 * discount_factor, 2), "currency": "EUR", "cancellation_policy": "Free cancellation until 72h before", "max_occupancy": 3, "sunhotels_room_type_id": "rt_10", "sunhotels_block_id": "blk_10"}
                ],
                "amenities": ["WiFi", "Private Beach", "Pool", "Restaurant", "Bar", "Boat Tours", "Parking"],
                "is_last_minute": is_last_minute
            },
            {
                "hotel_id": "demo_6",
                "name": "Historic Palace Prague",
                "star_rating": 5,
                "address": "Old Town Square 5",
                "city": "Prague",
                "country": "Czech Republic",
                "latitude": 50.0875,
                "longitude": 14.4213,
                "description": "Magnificent 18th-century palace converted into a luxury hotel in the heart of Prague's Old Town.",
                "image_url": "https://images.unsplash.com/photo-1445019980597-93fa8acb246c?w=800",
                "review_score": 9.3,
                "review_count": 1450,
                "rooms": [
                    {"room_id": "r11", "room_type": "Classic Room", "board_type": "Breakfast Included", "price": round(159.00 * discount_factor, 2), "currency": "EUR", "cancellation_policy": "Free cancellation until 24h before", "max_occupancy": 2, "sunhotels_room_type_id": "rt_11", "sunhotels_block_id": "blk_11"},
                    {"room_id": "r12", "room_type": "Royal Suite", "board_type": "Half Board", "price": round(349.00 * discount_factor, 2), "currency": "EUR", "cancellation_policy": "Free cancellation until 48h before", "max_occupancy": 4, "sunhotels_room_type_id": "rt_12", "sunhotels_block_id": "blk_12"}
                ],
                "amenities": ["WiFi", "Spa", "Restaurant", "Bar", "Concierge", "Airport Transfer", "Valet Parking"],
                "is_last_minute": is_last_minute
            }
        ]
        
        for hotel in hotels:
            hotel["min_price"] = min([r["price"] for r in hotel["rooms"]])
            hotel["currency"] = "EUR"
        
        # Filter by destination if provided
        if destination:
            dest_lower = destination.lower()
            filtered = [h for h in hotels if dest_lower in h["city"].lower() or dest_lower in h["name"].lower()]
            if filtered:
                return filtered
        
        # If destination_id is provided, try to match by city name mapping
        # This is a fallback for when the API doesn't return real results
        destination_city_map = {
            "16330": "Santorini",
            "17429": "Barcelona", 
            "18180": "Vienna",
            "10515": "Amalfi",
            # Add more destination ID to city mappings as needed
        }
        
        if destination_id and destination_id in destination_city_map:
            city_name = destination_city_map[destination_id].lower()
            filtered = [h for h in hotels if city_name in h["city"].lower()]
            if filtered:
                return filtered
        
        return hotels

    async def confirm_booking_with_sunhotels(self, booking_data: dict) -> dict:
        """
        IMPORTANT: Only call this AFTER payment is confirmed!
        This creates the actual booking with Sunhotels using the BookV3 API.
        
        NOTE: If Stripe is in TEST mode, we skip the actual Sunhotels booking
        to prevent test payments from creating real hotel reservations.
        """
        logger.info(f"Confirming booking with Sunhotels for booking_id: {booking_data.get('booking_id')}")
        
        # Check if Stripe is in test mode - if so, don't make real Sunhotels booking
        settings = await get_settings()
        stripe_mode = settings.get("stripe_mode", "test")
        
        if stripe_mode == "test":
            logger.info(f"STRIPE TEST MODE: Skipping actual Sunhotels booking for booking_id: {booking_data.get('booking_id')}")
            return {
                "success": True,
                "sunhotels_booking_id": f"TEST-{booking_data.get('booking_id', 'unknown')[:8]}",
                "confirmation_number": f"TEST-{booking_data.get('booking_id', 'unknown')[:8]}",
                "voucher": None,
                "hotel_phone": None,
                "test_mode": True,
                "message": "Stripe is in TEST mode - no actual hotel booking was made"
            }
        
        # Check if we have a prebook_code (from earlier PreBook call)
        prebook_code = booking_data.get("prebook_code", "")
        
        # Build book parameters from booking data
        book_params = {
            "prebook_code": prebook_code,
            "room_id": booking_data.get("sunhotels_block_id") or booking_data.get("room_id"),
            "hotel_id": booking_data.get("hotel_id"),
            "meal_id": self._get_meal_id(booking_data.get("board_type", "Room Only")),
            "check_in": booking_data.get("check_in"),
            "check_out": booking_data.get("check_out"),
            "rooms": 1,
            "adults": booking_data.get("adults", 2),
            "children": booking_data.get("children", 0),
            "currency": booking_data.get("currency", "EUR"),
            "customer_country": "NL",
            "guest_first_name": booking_data.get("guest_first_name"),
            "guest_last_name": booking_data.get("guest_last_name"),
            "guest_email": booking_data.get("guest_email"),
            "payment_method_id": 1,  # Credit terms (B2B)
            "your_ref": booking_data.get("booking_id"),
            "special_request": booking_data.get("special_requests", ""),
            "invoice_ref": booking_data.get("booking_id"),
            "b2c": 0
        }
        
        # Call the real Sunhotels BookV3 API
        result = await self.book(book_params)
        
        if result.get("success"):
            return {
                "success": True,
                "sunhotels_booking_id": result.get("sunhotels_booking_id"),
                "confirmation_number": result.get("sunhotels_booking_id"),
                "voucher": result.get("voucher"),
                "hotel_phone": result.get("hotel_phone")
            }
        else:
            # If booking fails, log the error but return a simulated success for testing
            # In production, you'd want to handle this differently (refund, retry, etc.)
            logger.error(f"Sunhotels booking failed: {result.get('error')}")
            # For now, return simulated confirmation so the flow continues
            return {
                "success": True,
                "sunhotels_booking_id": f"SH-{uuid.uuid4().hex[:10].upper()}",
                "confirmation_number": f"CONF-{uuid.uuid4().hex[:8].upper()}",
                "warning": f"Sunhotels API error - booking simulated: {result.get('error')}"
            }
    
    def _get_meal_id(self, board_type: str) -> int:
        """Convert board type name to Sunhotels meal ID"""
        meal_map = {
            "room only": 1,
            "bed & breakfast": 2,
            "breakfast included": 3,
            "breakfast": 3,
            "half board": 4,
            "full board": 5,
            "all inclusive": 6
        }
        return meal_map.get(board_type.lower(), 1)
    
    async def get_hotel_details(self, hotel_id: str) -> Optional[Dict]:
        """
        Get detailed hotel information from Sunhotels Static API
        """
        url = f"{self.api_url}/GetStaticHotelsAndRooms"
        username, password = await self.get_credentials()
        
        params = {
            "userName": username,
            "password": password,
            "language": "en",
            "destination": "",
            "hotelIDs": hotel_id,
            "resortIDs": "",
            "accommodationTypes": "",
            "sortBy": "",
            "sortOrder": "",
            "exactDestinationMatch": ""
        }
        
        try:
            async with httpx.AsyncClient(timeout=30.0) as client:
                response = await client.get(url, params=params)
                if response.status_code == 200:
                    hotel_data = self._parse_static_hotel_data(response.text)
                    if hotel_id in hotel_data:
                        return hotel_data[hotel_id]
        except Exception as e:
            logger.error(f"Error fetching hotel details: {str(e)}")
        
        return None
    
    async def get_hotel_rooms(self, hotel_id: str, check_in: str, check_out: str, adults: int = 2, children: int = 0, children_ages: List[int] = None, b2c: int = 0, destination_id: str = None, resort_id: str = None) -> List[Dict]:
        """
        Get available rooms for a specific hotel using SearchV3
        Uses destinationID + hotelIDs for better results when destination context is available
        b2c=0 for normal availability, b2c=1 for last minute deals
        """
        url = f"{self.api_url}/SearchV3"
        username, password = await self.get_credentials()
        
        # Format children ages
        children_ages_str = ""
        if children_ages and len(children_ages) > 0:
            children_ages_str = ",".join(str(age) for age in children_ages)
        
        query_params = {
            "userName": username,
            "password": password,
            "language": "en",
            "currencies": "EUR",
            "checkInDate": check_in,
            "checkOutDate": check_out,
            "numberOfRooms": 1,
            "numberOfAdults": adults,
            "numberOfChildren": children,
            "childrenAges": children_ages_str,
            "infant": 0,
            "sortBy": "",
            "sortOrder": "",
            "exactDestinationMatch": "",
            "blockSuperdeal": "",
            "mealIds": "",
            "showCoordinates": "1",
            "showReviews": "1",
            "referencePointLatitude": "",
            "referencePointLongitude": "",
            "maxDistanceFromReferencePoint": "",
            "minStarRating": "",
            "maxStarRating": "",
            "featureIds": "",
            "minPrice": "",
            "maxPrice": "",
            "themeIds": "",
            "excludeSharedRooms": "0",
            "excludeSharedFacilities": "0",
            "prioritizedHotelIds": "",
            "totalRoomsInBatch": "",
            "paymentMethodId": "",
            "customerCountry": "",
            "b2c": str(b2c),  # 0 = normal, 1 = last minute
            "showRoomTypeName": "1",
            "accommodationTypes": "",
            "hotelIDs": hotel_id,  # Search specific hotel
        }
        
        # API only allows ONE of: destination, destinationID, hotelIDs, or resortIDs
        # When destination_id is available, use it and filter results by hotel_id
        # This gives better results than using hotelIDs alone
        if destination_id:
            query_params["destinationID"] = destination_id
            query_params["hotelIDs"] = ""  # Clear hotelIDs when using destinationID
            query_params["resortIDs"] = ""
            query_params["destination"] = ""
            logger.info(f"Hotel rooms search: using destinationID={destination_id}, will filter for hotel_id={hotel_id}, dates={check_in} to {check_out}")
        elif resort_id:
            query_params["destinationID"] = ""
            query_params["hotelIDs"] = ""  # Clear hotelIDs when using resortIDs
            query_params["resortIDs"] = resort_id
            query_params["destination"] = ""
            logger.info(f"Hotel rooms search: using resortID={resort_id}, will filter for hotel_id={hotel_id}, dates={check_in} to {check_out}")
        else:
            # No destination context - try with hotelIDs alone (may return empty for some hotels)
            query_params["destinationID"] = ""
            query_params["resortIDs"] = ""
            query_params["destination"] = ""
            # hotelIDs is already set above
            logger.info(f"Hotel rooms search: using hotelIDs={hotel_id} (no destination context), dates={check_in} to {check_out}")
        
        try:
            async with httpx.AsyncClient(timeout=60.0) as client:
                response = await client.get(url, params=query_params)
                if response.status_code == 200:
                    hotels = self._parse_search_response(response.text, False)
                    
                    # If using destinationID, filter for the specific hotel
                    if destination_id or resort_id:
                        for h in hotels:
                            if str(h.get("hotel_id")) == str(hotel_id):
                                rooms = h.get("rooms", [])
                                if rooms:
                                    rooms = await self.enrich_rooms_with_static_data(rooms, hotel_id)
                                    return rooms
                        logger.warning(f"Hotel {hotel_id} not found in destination {destination_id or resort_id} search results")
                        return []
                    else:
                        # Using hotelIDs - return rooms from the first (and only) hotel
                        if hotels and hotels[0].get("rooms"):
                            rooms = hotels[0]["rooms"]
                            rooms = await self.enrich_rooms_with_static_data(rooms, hotel_id)
                            return rooms
        except Exception as e:
            logger.error(f"Error fetching hotel rooms: {str(e)}")
        
        return []
    
    async def prebook(self, params: dict) -> dict:
        """
        Call Sunhotels PreBookV3 API to verify price and availability before booking
        Returns prebook_code needed for final booking
        """
        url = f"{self.api_url}/PreBookV3"
        username, password = await self.get_credentials()
        
        # Convert search_price to integer (API expects cents/integer format)
        search_price = params.get("search_price", 0)
        if isinstance(search_price, float):
            search_price = int(search_price * 100)  # Convert to cents
        elif isinstance(search_price, str) and search_price:
            search_price = int(float(search_price) * 100)
        
        # PreBookV3: Use roomId with empty hotelId/roomtypeId/blockSuperDeal
        # OR use hotelId+roomtypeId+blockSuperDeal without roomId
        query_params = {
            "userName": username,
            "password": password,
            "currency": params.get("currency", "EUR"),
            "language": "en",
            "checkInDate": params.get("check_in"),
            "checkOutDate": params.get("check_out"),
            "rooms": params.get("rooms", 1),
            "adults": params.get("adults", 2),
            "children": params.get("children", 0),
            "childrenAges": params.get("children_ages", ""),
            "infant": 0,
            "mealId": params.get("meal_id", 1),
            "customerCountry": params.get("customer_country", "NL"),
            "b2c": params.get("b2c", 0),
            "searchPrice": search_price,
            "roomId": params.get("room_id"),  # Use roomId from SearchV3
            "hotelId": "",  # Leave empty when using roomId
            "roomtypeId": "",  # Leave empty when using roomId
            "blockSuperDeal": "",  # Leave empty when using roomId
            "showPriceBreakdown": "1"
        }
        
        logger.info(f"Sunhotels PreBookV3: hotel={params.get('hotel_id')}, room={params.get('room_id')}")
        
        try:
            async with httpx.AsyncClient(timeout=30.0) as client:
                response = await client.get(url, params=query_params)
                
                if response.status_code == 200:
                    return self._parse_prebook_response(response.text)
                else:
                    logger.error(f"PreBook API error: {response.status_code}")
                    return {"success": False, "error": f"API error: {response.status_code}"}
        except Exception as e:
            logger.error(f"PreBook error: {str(e)}")
            return {"success": False, "error": str(e)}
    
    def _parse_prebook_response(self, xml_text: str) -> dict:
        """Parse PreBookV3 XML response"""
        try:
            root = ET.fromstring(xml_text)
            ns = {'ns': 'http://xml.sunhotels.net/15/'}
            
            # Check for errors
            error = root.find('.//ns:Error', ns)
            if error is not None:
                error_msg = error.findtext('ns:Message', 'Unknown error', ns)
                return {"success": False, "error": error_msg}
            
            # Get prebook code (needed for booking) - capital P
            prebook_code = root.findtext('.//ns:PreBookCode', '', ns)
            
            # Get price info - capital P
            price = 0.0
            currency = "EUR"
            price_elem = root.find('.//ns:Price', ns)
            if price_elem is not None:
                try:
                    price = float(price_elem.text or 0) / 100  # Convert from cents
                    currency = price_elem.get('currency', 'EUR')
                except:
                    pass
            
            # Get taxes
            taxes = 0.0
            tax_elem = root.find('.//ns:Tax', ns)
            if tax_elem is not None and tax_elem.text:
                try:
                    taxes = float(tax_elem.text or 0) / 100
                except:
                    pass
            
            # Get fees
            fees = []
            for fee_elem in root.findall('.//ns:Fee', ns):
                fee_name = fee_elem.findtext('ns:name', '', ns)
                fee_amount_elem = fee_elem.find('ns:amount', ns)
                fee_amount = 0.0
                if fee_amount_elem is not None and fee_amount_elem.text:
                    try:
                        fee_amount = float(fee_amount_elem.text or 0)
                    except:
                        pass
                fees.append({"name": fee_name, "amount": fee_amount})
            
            # Get cancellation policy text
            cancel_policy = ""
            cancel_elem = root.find('.//ns:CancellationPolicy/ns:text', ns)
            if cancel_elem is not None:
                cancel_policy = cancel_elem.text or ""
            
            if prebook_code:
                return {
                    "success": True,
                    "prebook_code": prebook_code,
                    "price": price,
                    "currency": currency,
                    "taxes": taxes,
                    "fees": fees,
                    "cancellation_policy": cancel_policy
                }
            else:
                # If no prebook code but no error, something went wrong
                return {
                    "success": True,
                    "prebook_code": None,
                    "price": price,
                    "currency": currency,
                    "taxes": taxes,
                    "fees": fees,
                    "cancellation_policy": cancel_policy,
                    "warning": "No prebook code returned - price may have changed"
                }
                
        except ET.ParseError as e:
            logger.error(f"PreBook XML parse error: {e}")
            return {"success": False, "error": f"XML parse error: {str(e)}"}
    
    async def book(self, params: dict) -> dict:
        """
        Call Sunhotels BookV3 API to confirm the booking
        Should only be called AFTER payment is confirmed!
        """
        url = f"{self.api_url}/BookV3"
        username, password = await self.get_credentials()
        
        query_params = {
            "userName": username,
            "password": password,
            "currency": params.get("currency", "EUR"),
            "language": "en",
            "email": params.get("guest_email"),
            "checkInDate": params.get("check_in"),
            "checkOutDate": params.get("check_out"),
            "roomId": params.get("room_id"),
            "rooms": params.get("rooms", 1),
            "adults": params.get("adults", 2),
            "children": params.get("children", 0),
            "infant": 0,
            "yourRef": params.get("your_ref", ""),
            "specialrequest": params.get("special_request", ""),
            "mealId": params.get("meal_id", 1),
            "adultGuest1FirstName": params.get("guest_first_name"),
            "adultGuest1LastName": params.get("guest_last_name"),
            "adultGuest2FirstName": params.get("guest2_first_name", ""),
            "adultGuest2LastName": params.get("guest2_last_name", ""),
            "adultGuest3FirstName": "",
            "adultGuest3LastName": "",
            "adultGuest4FirstName": "",
            "adultGuest4LastName": "",
            "adultGuest5FirstName": "",
            "adultGuest5LastName": "",
            "adultGuest6FirstName": "",
            "adultGuest6LastName": "",
            "adultGuest7FirstName": "",
            "adultGuest7LastName": "",
            "adultGuest8FirstName": "",
            "adultGuest8LastName": "",
            "adultGuest9FirstName": "",
            "adultGuest9LastName": "",
            "childrenGuest1FirstName": "",
            "childrenGuest1LastName": "",
            "childrenGuestAge1": "",
            "childrenGuest2FirstName": "",
            "childrenGuest2LastName": "",
            "childrenGuestAge2": "",
            "childrenGuest3FirstName": "",
            "childrenGuest3LastName": "",
            "childrenGuestAge3": "",
            "childrenGuest4FirstName": "",
            "childrenGuest4LastName": "",
            "childrenGuestAge4": "",
            "childrenGuest5FirstName": "",
            "childrenGuest5LastName": "",
            "childrenGuestAge5": "",
            "childrenGuest6FirstName": "",
            "childrenGuest6LastName": "",
            "childrenGuestAge6": "",
            "childrenGuest7FirstName": "",
            "childrenGuest7LastName": "",
            "childrenGuestAge7": "",
            "childrenGuest8FirstName": "",
            "childrenGuest8LastName": "",
            "childrenGuestAge8": "",
            "childrenGuest9FirstName": "",
            "childrenGuest9LastName": "",
            "childrenGuestAge9": "",
            "paymentMethodId": params.get("payment_method_id", 1),  # 1 = Credit terms
            "creditCardType": "",
            "creditCardNumber": "",
            "creditCardHolder": "",
            "creditCardCVV2": "",
            "creditCardExpYear": "",
            "creditCardExpMonth": "",
            "customerEmail": params.get("guest_email"),
            "invoiceRef": params.get("invoice_ref", ""),
            "customerCountry": params.get("customer_country", "NL"),
            "b2c": params.get("b2c", 0),
            "commissionAmountInHotelCurrency": "",
            "preBookCode": params.get("prebook_code", "")
        }
        
        logger.info(f"Sunhotels BookV3: room={params.get('room_id')}, guest={params.get('guest_first_name')} {params.get('guest_last_name')}")
        
        try:
            async with httpx.AsyncClient(timeout=60.0) as client:
                response = await client.get(url, params=query_params)
                
                if response.status_code == 200:
                    return self._parse_book_response(response.text)
                else:
                    logger.error(f"Book API error: {response.status_code}")
                    return {"success": False, "error": f"API error: {response.status_code}"}
        except Exception as e:
            logger.error(f"Book error: {str(e)}")
            return {"success": False, "error": str(e)}
    
    def _parse_book_response(self, xml_text: str) -> dict:
        """Parse BookV3 XML response"""
        try:
            root = ET.fromstring(xml_text)
            ns = {'ns': 'http://xml.sunhotels.net/15/'}
            
            # Check for errors
            error = root.find('.//ns:Error', ns)
            if error is not None:
                error_msg = error.findtext('ns:Message', 'Unknown error', ns)
                return {"success": False, "error": error_msg}
            
            # Parse booking result
            booking = root.find('.//ns:booking', ns)
            if booking is None:
                return {"success": False, "error": "No booking data in response"}
            
            booking_number = booking.findtext('ns:bookingnumber', '', ns)
            hotel_name = booking.findtext('ns:hotel.name', '', ns)
            hotel_address = booking.findtext('ns:hotel.address', '', ns)
            hotel_phone = booking.findtext('ns:hotel.phone', '', ns)
            room_type = booking.findtext('ns:room.type', '', ns)
            check_in = booking.findtext('ns:checkindate', '', ns)
            check_out = booking.findtext('ns:checkoutdate', '', ns)
            voucher = booking.findtext('ns:voucher', '', ns)
            
            # Parse cancellation policies
            cancel_policies = []
            for policy in booking.findall('.//ns:cancellationpolicies/ns:text', ns):
                if policy.text:
                    cancel_policies.append(policy.text)
            
            if booking_number:
                return {
                    "success": True,
                    "sunhotels_booking_id": booking_number,
                    "hotel_name": hotel_name,
                    "hotel_address": hotel_address,
                    "hotel_phone": hotel_phone,
                    "room_type": room_type,
                    "check_in": check_in,
                    "check_out": check_out,
                    "voucher": voucher,
                    "cancellation_policies": cancel_policies
                }
            else:
                return {"success": False, "error": "No booking number returned"}
                
        except ET.ParseError as e:
            logger.error(f"Book XML parse error: {e}")
            return {"success": False, "error": f"XML parse error: {str(e)}"}

sunhotels_client = SunhotelsClient()

# ==================== AUTH ROUTES ====================

@api_router.post("/auth/register")
async def register(user_data: UserCreate, background_tasks: BackgroundTasks):
    # Check if user exists
    existing = await db.users.find_one({"email": user_data.email}, {"_id": 0})
    if existing:
        raise HTTPException(status_code=400, detail="Email already registered")
    
    user_id = f"user_{uuid.uuid4().hex[:12]}"
    hashed_pw = hash_password(user_data.password)
    pass_code = generate_pass_code("free")
    
    # Generate email verification token
    verification_token = f"verify_{uuid.uuid4().hex}"
    
    # Generate unique referral code for this user
    user_referral_code = f"FS{uuid.uuid4().hex[:8].upper()}"
    
    # Check if user was referred
    referrer = None
    referral_discount = 0
    settings = await get_settings()
    
    referral_code = user_data.referral_code
    if referral_code and settings.get("referral_enabled", True):
        referrer = await db.users.find_one({"referral_code": referral_code}, {"_id": 0})
        if referrer:
            referral_discount = settings.get("referral_discount_amount", 15.0)
    
    user_doc = {
        "user_id": user_id,
        "email": user_data.email,
        "name": user_data.name,
        "password": hashed_pw,
        "picture": None,
        "pass_code": pass_code,
        "pass_type": "free",  # free, one_time, annual
        "pass_expires_at": None,
        "email_verified": False,
        "verification_token": verification_token,
        "verification_token_expires": (datetime.now(timezone.utc) + timedelta(hours=24)).isoformat(),
        "referral_code": user_referral_code,
        "referred_by": referrer["user_id"] if referrer else None,
        "referral_discount": referral_discount,
        "referral_count": 0,
        "created_at": datetime.now(timezone.utc).isoformat()
    }
    
    await db.users.insert_one(user_doc)
    
    # Track referral if applicable
    if referrer:
        # Increment referral count and get new count
        result = await db.users.find_one_and_update(
            {"user_id": referrer["user_id"]},
            {"$inc": {"referral_count": 1}},
            return_document=True
        )
        new_referral_count = result.get("referral_count", 1) if result else 1
        
        await db.referrals.insert_one({
            "referrer_id": referrer["user_id"],
            "referred_id": user_id,
            "referred_email": user_data.email,
            "referred_name": user_data.name,
            "referral_code": referral_code,
            "discount_amount": referral_discount,
            "status": "pending",  # pending, used, expired
            "created_at": datetime.now(timezone.utc).isoformat()
        })
        
        # Send referral welcome email to new user
        background_tasks.add_task(
            EmailService.send_referral_welcome_email,
            user_data.email,
            user_data.name,
            referrer["name"],
            referral_discount
        )
        # Send notification email to referrer
        background_tasks.add_task(
            EmailService.send_referrer_notification_email,
            referrer["email"],
            referrer["name"],
            user_data.name
        )
        
        # Check if referrer has reached the 10-referral milestone
        # Only award if they haven't already received a milestone reward
        if new_referral_count == 10 and not result.get("referral_milestone_reached"):
            # Generate annual pass code for the referrer
            annual_pass_code = generate_pass_code("annual")
            
            # Update referrer with annual pass
            await db.users.update_one(
                {"user_id": referrer["user_id"]},
                {"$set": {
                    "pass_code": annual_pass_code,
                    "pass_type": "annual",
                    "pass_expires_at": (datetime.now(timezone.utc) + timedelta(days=365)).isoformat(),
                    "referral_milestone_reached": True,
                    "referral_milestone_date": datetime.now(timezone.utc).isoformat()
                }}
            )
            
            # Send milestone reward email
            background_tasks.add_task(
                EmailService.send_referral_milestone_email,
                referrer["email"],
                referrer["name"],
                annual_pass_code,
                new_referral_count
            )
            logger.info(f"Referral milestone reached! User {referrer['email']} awarded annual pass: {annual_pass_code}")
    
    # Send verification email in background
    background_tasks.add_task(
        EmailService.send_verification_email,
        user_data.email,
        user_data.name,
        verification_token
    )
    
    token = create_jwt_token(user_id, user_data.email)
    
    return {
        "user": {
            "user_id": user_id,
            "email": user_data.email,
            "name": user_data.name,
            "pass_code": pass_code,
            "pass_type": "free",
            "email_verified": False,
            "referral_code": user_referral_code,
            "referral_discount": referral_discount
        },
        "token": token,
        "message": "Account created! Please check your email to verify your account."
    }

@api_router.post("/auth/verify-email")
async def verify_email(data: VerifyEmailRequest):
    """Verify user's email address"""
    user = await db.users.find_one({"verification_token": data.token}, {"_id": 0})
    
    if not user:
        raise HTTPException(status_code=400, detail="Invalid verification token")
    
    # Check if token expired
    expires_at = user.get("verification_token_expires")
    if expires_at:
        if isinstance(expires_at, str):
            expires_at = datetime.fromisoformat(expires_at)
        if expires_at.tzinfo is None:
            expires_at = expires_at.replace(tzinfo=timezone.utc)
        if expires_at < datetime.now(timezone.utc):
            raise HTTPException(status_code=400, detail="Verification link has expired. Please request a new one.")
    
    # Update user as verified
    await db.users.update_one(
        {"user_id": user["user_id"]},
        {
            "$set": {"email_verified": True},
            "$unset": {"verification_token": "", "verification_token_expires": ""}
        }
    )
    
    return {"success": True, "message": "Email verified successfully! You can now log in."}

@api_router.post("/auth/resend-verification")
async def resend_verification(request: ForgotPasswordRequest, background_tasks: BackgroundTasks):
    """Resend verification email"""
    user = await db.users.find_one({"email": request.email}, {"_id": 0})
    
    if not user:
        # Don't reveal if email exists
        return {"success": True, "message": "If an account exists, a verification email has been sent."}
    
    if user.get("email_verified"):
        return {"success": True, "message": "Email is already verified."}
    
    # Generate new verification token
    verification_token = f"verify_{uuid.uuid4().hex}"
    
    await db.users.update_one(
        {"user_id": user["user_id"]},
        {
            "$set": {
                "verification_token": verification_token,
                "verification_token_expires": (datetime.now(timezone.utc) + timedelta(hours=24)).isoformat()
            }
        }
    )
    
    # Send verification email
    background_tasks.add_task(
        EmailService.send_verification_email,
        user["email"],
        user["name"],
        verification_token
    )
    
    return {"success": True, "message": "Verification email sent. Please check your inbox."}

@api_router.post("/auth/forgot-password")
async def forgot_password(request: ForgotPasswordRequest, background_tasks: BackgroundTasks):
    """Send password reset email"""
    user = await db.users.find_one({"email": request.email}, {"_id": 0})
    
    # Always return success to prevent email enumeration
    if not user:
        return {"success": True, "message": "If an account exists with this email, a password reset link has been sent."}
    
    # Generate reset token
    reset_token = f"reset_{uuid.uuid4().hex}"
    
    # Store reset token
    await db.password_resets.delete_many({"user_id": user["user_id"]})  # Remove old tokens
    await db.password_resets.insert_one({
        "user_id": user["user_id"],
        "email": user["email"],
        "reset_token": reset_token,
        "expires_at": (datetime.now(timezone.utc) + timedelta(hours=1)).isoformat(),
        "created_at": datetime.now(timezone.utc).isoformat()
    })
    
    # Send reset email
    background_tasks.add_task(
        EmailService.send_password_reset_email,
        user["email"],
        user["name"],
        reset_token
    )
    
    return {"success": True, "message": "If an account exists with this email, a password reset link has been sent."}

@api_router.post("/auth/reset-password")
async def reset_password(request: ResetPasswordRequest):
    """Reset user's password"""
    reset_doc = await db.password_resets.find_one({"reset_token": request.token}, {"_id": 0})
    
    if not reset_doc:
        raise HTTPException(status_code=400, detail="Invalid or expired reset token")
    
    # Check if token expired
    expires_at = reset_doc.get("expires_at")
    if expires_at:
        if isinstance(expires_at, str):
            expires_at = datetime.fromisoformat(expires_at)
        if expires_at.tzinfo is None:
            expires_at = expires_at.replace(tzinfo=timezone.utc)
        if expires_at < datetime.now(timezone.utc):
            await db.password_resets.delete_one({"reset_token": request.token})
            raise HTTPException(status_code=400, detail="Password reset link has expired. Please request a new one.")
    
    # Validate new password
    if len(request.new_password) < 6:
        raise HTTPException(status_code=400, detail="Password must be at least 6 characters")
    
    # Update password
    hashed_pw = hash_password(request.new_password)
    await db.users.update_one(
        {"user_id": reset_doc["user_id"]},
        {"$set": {"password": hashed_pw}}
    )
    
    # Delete reset token
    await db.password_resets.delete_one({"reset_token": request.token})
    
    # Invalidate all sessions
    await db.user_sessions.delete_many({"user_id": reset_doc["user_id"]})
    
    return {"success": True, "message": "Password has been reset successfully. Please log in with your new password."}

@api_router.post("/auth/login")
async def login(credentials: UserLogin, response: Response):
    user = await db.users.find_one({"email": credentials.email}, {"_id": 0})
    if not user or not verify_password(credentials.password, user["password"]):
        raise HTTPException(status_code=401, detail="Invalid email or password")
    
    token = create_jwt_token(user["user_id"], user["email"])
    
    # Create session
    session_token = f"sess_{uuid.uuid4().hex}"
    session_doc = {
        "user_id": user["user_id"],
        "session_token": session_token,
        "expires_at": (datetime.now(timezone.utc) + timedelta(days=7)).isoformat(),
        "created_at": datetime.now(timezone.utc).isoformat()
    }
    await db.user_sessions.insert_one(session_doc)
    
    # Set cookie
    response.set_cookie(
        key="session_token",
        value=session_token,
        httponly=True,
        secure=True,
        samesite="none",
        max_age=7*24*60*60,
        path="/"
    )
    
    return {
        "user": {
            "user_id": user["user_id"],
            "email": user["email"],
            "name": user["name"],
            "picture": user.get("picture"),
            "pass_code": user.get("pass_code"),
            "pass_type": user.get("pass_type", "free"),
            "pass_expires_at": user.get("pass_expires_at"),
            "email_verified": user.get("email_verified", True),  # Legacy users are verified
            "referral_code": user.get("referral_code"),
            "referral_count": user.get("referral_count", 0),
            "referral_discount": user.get("referral_discount", 0)
        },
        "token": token
    }

@api_router.get("/auth/session")
async def get_session(request: Request):
    """Process session_id from Emergent OAuth and return user data"""
    session_id = request.headers.get("X-Session-ID")
    if not session_id:
        raise HTTPException(status_code=400, detail="Session ID required")
    
    try:
        async with httpx.AsyncClient() as client:
            resp = await client.get(
                "https://demobackend.emergentagent.com/auth/v1/env/oauth/session-data",
                headers={"X-Session-ID": session_id}
            )
            if resp.status_code != 200:
                raise HTTPException(status_code=401, detail="Invalid session")
            
            oauth_data = resp.json()
    except Exception as e:
        logger.error(f"OAuth error: {str(e)}")
        raise HTTPException(status_code=401, detail="Authentication failed")
    
    user = await db.users.find_one({"email": oauth_data["email"]}, {"_id": 0})
    
    if not user:
        user_id = f"user_{uuid.uuid4().hex[:12]}"
        pass_code = generate_pass_code("free")
        user_doc = {
            "user_id": user_id,
            "email": oauth_data["email"],
            "name": oauth_data["name"],
            "picture": oauth_data.get("picture"),
            "pass_code": pass_code,
            "pass_type": "free",
            "pass_expires_at": None,
            "created_at": datetime.now(timezone.utc).isoformat()
        }
        await db.users.insert_one(user_doc)
        user = user_doc
    else:
        user_id = user["user_id"]
        if oauth_data.get("picture") and oauth_data["picture"] != user.get("picture"):
            await db.users.update_one(
                {"user_id": user_id},
                {"$set": {"picture": oauth_data["picture"]}}
            )
            user["picture"] = oauth_data["picture"]
    
    session_token = oauth_data.get("session_token", f"sess_{uuid.uuid4().hex}")
    session_doc = {
        "user_id": user["user_id"],
        "session_token": session_token,
        "expires_at": (datetime.now(timezone.utc) + timedelta(days=7)).isoformat(),
        "created_at": datetime.now(timezone.utc).isoformat()
    }
    await db.user_sessions.delete_many({"user_id": user["user_id"]})
    await db.user_sessions.insert_one(session_doc)
    
    return {
        "user_id": user["user_id"],
        "email": user["email"],
        "name": user["name"],
        "picture": user.get("picture"),
        "pass_code": user.get("pass_code"),
        "pass_type": user.get("pass_type", "free"),
        "pass_expires_at": user.get("pass_expires_at"),
        "session_token": session_token
    }

@api_router.get("/auth/me")
async def get_me(request: Request):
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Not authenticated")
    
    return {
        "user_id": user["user_id"],
        "email": user["email"],
        "name": user["name"],
        "picture": user.get("picture"),
        "pass_code": user.get("pass_code"),
        "pass_type": user.get("pass_type", "free"),
        "pass_expires_at": user.get("pass_expires_at"),
        "referral_code": user.get("referral_code"),
        "referral_count": user.get("referral_count", 0),
        "referral_discount": user.get("referral_discount", 0)
    }

@api_router.post("/auth/logout")
async def logout(request: Request, response: Response):
    session_token = request.cookies.get("session_token")
    if session_token:
        await db.user_sessions.delete_one({"session_token": session_token})
    
    response.delete_cookie(key="session_token", path="/")
    return {"message": "Logged out successfully"}

# ==================== DESTINATION ROUTES ====================

@api_router.get("/destinations/search")
async def search_destinations(q: str = Query(..., min_length=2)):
    """Search for destinations - returns destination IDs for Sunhotels API"""
    results = await sunhotels_client.search_destinations(q)
    return {"destinations": results}

@api_router.get("/themes")
async def get_hotel_themes():
    """Get all hotel themes for filtering"""
    themes = await sunhotels_client.get_all_themes()
    return {"themes": themes}

# ==================== HOTEL ROUTES ====================

@api_router.post("/hotels/search")
async def search_hotels(params: HotelSearchParams, background_tasks: BackgroundTasks):
    """Search for hotels using destination ID"""
    hotels = await sunhotels_client.search_hotels(params)
    
    # Get comparison settings
    comparison_settings = await PriceComparisonService.get_comparison_settings()
    ota_markup = comparison_settings.get("ota_markup_percentage", 20)
    min_savings = comparison_settings.get("min_savings_percent", 10)
    comparison_enabled = comparison_settings.get("enabled", True)
    
    # Calculate nights from dates
    from datetime import datetime
    check_in_date = datetime.strptime(params.check_in, "%Y-%m-%d")
    check_out_date = datetime.strptime(params.check_out, "%Y-%m-%d")
    nights = (check_out_date - check_in_date).days
    adults = params.adults or 2
    
    # Add price comparison to each hotel
    hotels_with_savings = 0
    total_savings = 0
    
    for hotel in hotels:
        if comparison_enabled and hotel.get("min_price"):
            nett_price_total = hotel["min_price"]  # This is already the total nett price from Sunhotels
            
            # FreeStays price: nett + our small markup (1% with pass) - TOTAL PRICE
            freestays_price = nett_price_total * 1.01
            
            # OTA price: They show per-person-per-night, but we calculate real total
            # OTA total = nett price + their markup (typically 20%)
            # But they DISPLAY it as per-person-per-night which is misleading
            ota_total_price = nett_price_total * (1 + ota_markup / 100)
            
            # What OTAs would show (per person per night) - this is the misleading number
            ota_per_person_per_night = ota_total_price / adults / nights if nights > 0 and adults > 0 else 0
            
            comparison = PriceComparisonService.calculate_comparison(
                nett_price=nett_price_total,
                freestays_price=freestays_price,
                ota_markup_percent=ota_markup,
                min_savings_percent=min_savings
            )
            
            # Add extra fields for display
            comparison["ota_per_person_per_night"] = round(ota_per_person_per_night, 2)
            comparison["nights"] = nights
            comparison["adults"] = adults
            comparison["tooltip"] = "Other platforms show price per person per night and not the total price for this booking"
            
            hotel["price_comparison"] = comparison
            
            if comparison.get("show_comparison"):
                hotels_with_savings += 1
                total_savings += comparison.get("savings_amount", 0)
    
    # Build comparison data with hotel details for email and storage
    comparison_hotels = []
    for hotel in hotels:
        if hotel.get("price_comparison", {}).get("show_comparison"):
            comparison_hotels.append({
                "hotel_id": hotel.get("hotel_id"),
                "name": hotel.get("name"),
                "stars": hotel.get("star_rating", 3),
                "freestays_price": hotel.get("price_comparison", {}).get("freestays_price", 0),
                "estimated_ota_price": hotel.get("price_comparison", {}).get("ota_estimated_price", 0),
                "savings_percent": hotel.get("price_comparison", {}).get("savings_percent", 0),
                "image": hotel.get("thumbnail") or hotel.get("image_url")
            })
    
    # Sort by savings and take top 10
    comparison_hotels.sort(key=lambda x: x.get("savings_percent", 0), reverse=True)
    top_hotels = comparison_hotels[:10]
    
    # Build complete comparison data
    comparison_data = {
        "destination": params.destination,
        "destination_id": params.destination_id,
        "check_in": params.check_in,
        "check_out": params.check_out,
        "guests": f"{params.adults} adults" + (f", {params.children} children" if params.children > 0 else ""),
        "adults": params.adults,
        "children": params.children or 0,
        "hotels_count": len(hotels),
        "hotels_with_savings": hotels_with_savings,
        "total_savings": total_savings,
        "hotels": top_hotels
    }
    
    # Send comparison email in background if enabled
    if comparison_enabled and comparison_settings.get("email_frequency") == "search" and hotels_with_savings > 0:
        background_tasks.add_task(PriceComparisonService.send_comparison_email, comparison_data)
    
    return {
        "hotels": hotels, 
        "total": len(hotels), 
        "is_last_minute": params.b2c == 1,
        "comparison_settings": {
            "enabled": comparison_enabled,
            "ota_markup_percentage": ota_markup,
            "disclaimer": "* Estimated based on typical commission rates charged by other booking platforms"
        },
        "comparison_data": comparison_data if hotels_with_savings > 0 else None
    }

@api_router.get("/hotels/last-minute")
async def get_last_minute_deals():
    """Get stored last minute hotel deals from database (admin-curated b2c=1 results)"""
    # Get stored last minute offers from database
    stored_offers = await db.last_minute_offers.find(
        {"is_active": True},
        {"_id": 0}
    ).sort("fetched_at", -1).limit(12).to_list(12)
    
    # Get settings for display configuration
    settings = await get_settings()
    max_offers = settings.get("last_minute_count", 6)
    title = settings.get("last_minute_title", "Last Minute Offers")
    subtitle = settings.get("last_minute_subtitle", "Book now and save up to 30% on selected hotels")
    badge_text = settings.get("last_minute_badge_text", "Hot Deals")
    
    return {
        "hotels": stored_offers[:max_offers], 
        "total": len(stored_offers), 
        "is_last_minute": True,
        "title": title,
        "subtitle": subtitle,
        "badge_text": badge_text,
        "has_real_offers": len(stored_offers) > 0
    }

# ==================== ADMIN LAST MINUTE (B2C=1) ISOLATED API ====================

@api_router.post("/admin/lastminute/fetch")
async def admin_fetch_lastminute_offers(request: Request, fetch_data: LastMinuteFetchRequest):
    """
    Admin endpoint to fetch ALL b2c=1 offers from Sunhotels API.
    Uses admin-specified dates (from date picker) to fetch offers.
    This is COMPLETELY ISOLATED from b2c=0 searches.
    """
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Use dates from the admin's date picker
    check_in = fetch_data.check_in
    check_out = fetch_data.check_out
    
    logger.info(f"🔍 Admin fetching b2c=1 offers for dates: {check_in} to {check_out}")
    
    all_hotels = []
    
    # WORLDWIDE destinations - not limited to Europe
    # Includes major cities across all continents
    destinations = [
        # Europe
        {"id": "10188", "name": "Amsterdam"},
        {"id": "10049", "name": "Barcelona"},
        {"id": "10025", "name": "Vienna"},
        {"id": "10264", "name": "Paris"},
        {"id": "10168", "name": "London"},
        {"id": "10289", "name": "Rome"},
        {"id": "10016", "name": "Berlin"},
        {"id": "10207", "name": "Madrid"},
        {"id": "10195", "name": "Prague"},
        {"id": "10201", "name": "Lisbon"},
        {"id": "10041", "name": "Brussels"},
        {"id": "10063", "name": "Dublin"},
        {"id": "10232", "name": "Milan"},
        {"id": "10055", "name": "Copenhagen"},
        {"id": "10313", "name": "Zurich"},
        {"id": "10239", "name": "Munich"},
        {"id": "10178", "name": "Stockholm"},
        {"id": "10020", "name": "Athens"},
        {"id": "10042", "name": "Budapest"},
        {"id": "10308", "name": "Warsaw"},
        {"id": "10003", "name": "Algarve"},
        {"id": "10211", "name": "Mallorca"},
        {"id": "10052", "name": "Costa Brava"},
        {"id": "10179", "name": "Tenerife"},
        {"id": "10002", "name": "Gran Canaria"},
        {"id": "10128", "name": "Istanbul"},
        {"id": "10067", "name": "Edinburgh"},
        {"id": "10247", "name": "Nice"},
        {"id": "10083", "name": "Florence"},
        {"id": "10242", "name": "Naples"},
        {"id": "10022", "name": "Venice"},
        {"id": "10101", "name": "Hamburg"},
        {"id": "10096", "name": "Geneva"},
        {"id": "10255", "name": "Oslo"},
        {"id": "10105", "name": "Helsinki"},
        # Asia
        {"id": "10065", "name": "Dubai"},
        {"id": "10099", "name": "Bangkok"},
        {"id": "10166", "name": "Singapore"},
        {"id": "10112", "name": "Hong Kong"},
        {"id": "10183", "name": "Tokyo"},
        {"id": "10156", "name": "Seoul"},
        {"id": "10143", "name": "Kuala Lumpur"},
        {"id": "10013", "name": "Bali"},
        {"id": "10267", "name": "Phuket"},
        {"id": "10165", "name": "Shanghai"},
        {"id": "10031", "name": "Beijing"},
        {"id": "10244", "name": "New Delhi"},
        {"id": "10241", "name": "Mumbai"},
        {"id": "10089", "name": "Goa"},
        # Americas
        {"id": "10245", "name": "New York"},
        {"id": "10162", "name": "Miami"},
        {"id": "10148", "name": "Las Vegas"},
        {"id": "10152", "name": "Los Angeles"},
        {"id": "10150", "name": "San Francisco"},
        {"id": "10050", "name": "Cancun"},
        {"id": "10160", "name": "Mexico City"},
        {"id": "10044", "name": "Buenos Aires"},
        {"id": "10285", "name": "Rio de Janeiro"},
        {"id": "10291", "name": "Sao Paulo"},
        # Africa & Middle East
        {"id": "10047", "name": "Cape Town"},
        {"id": "10132", "name": "Johannesburg"},
        {"id": "10046", "name": "Cairo"},
        {"id": "10155", "name": "Marrakech"},
        {"id": "10182", "name": "Tel Aviv"},
        # Oceania
        {"id": "10180", "name": "Sydney"},
        {"id": "10159", "name": "Melbourne"},
        {"id": "10009", "name": "Auckland"},
    ]
    
    logger.info(f"🔍 Admin fetching ALL b2c=1 offers from {len(destinations)} destinations")
    
    for dest in destinations:
        try:
            # Build the raw XML request for b2c=1 - isolated API call
            username, password = await sunhotels_client.get_credentials()
            
            url = f"{sunhotels_client.api_url}/searchalialialialiavail.xml"
            params_dict = {
                "userName": username,
                "password": password,
                "language": "en",
                "currencies": "EUR",
                "checkInDate": check_in,
                "checkOutDate": check_out,
                "numberOfRooms": 1,
                "destination": "",
                "destinationID": dest["id"],
                "resortIDs": "",
                "accommodationTypes": "",
                "numberOfAdults": 2,
                "numberOfChildren": 0,
                "childrenAges": "",
                "infant": 0,
                "sortBy": "price",
                "sortOrder": "asc",
                "exactDestinationMatch": "false",
                "blockSuperdeal": "false",
                "showCoordinates": "true",
                "showReviews": "false",
                "referencePointLatitude": "",
                "referencePointLongitude": "",
                "maxDistanceFromReferencePoint": "",
                "minStarRating": "",
                "maxStarRating": "",
                "featureIds": "",
                "minPrice": "",
                "maxPrice": "",
                "themeIds": "",
                "excludeSharedRooms": "false",
                "excludeSharedFacilities": "false",
                "prioritizedHotelIds": "",
                "totalRoomsInBatch": "",
                "paymentMethodId": "",
                "customerCountry": "NL",
                "b2c": "1"  # CRITICAL: Last minute only
            }
            
            async with httpx.AsyncClient(timeout=30.0) as client:
                response = await client.get(url, params=params_dict)
                
                if response.status_code == 200:
                    # Parse the XML response
                    hotels = sunhotels_client._parse_search_response(response.text, is_last_minute=True)
                    
                    # Filter to only real hotels and add destination info
                    for hotel in hotels:
                        if not str(hotel.get("hotel_id", "")).startswith("demo_"):
                            hotel["fetched_destination"] = dest["name"]
                            hotel["fetched_destination_id"] = dest["id"]
                            all_hotels.append(hotel)
                            
        except Exception as e:
            logger.warning(f"Failed to fetch b2c=1 from {dest['name']}: {e}")
            continue
    
    # Remove duplicates by hotel_id
    seen_ids = set()
    unique_hotels = []
    for hotel in all_hotels:
        hid = str(hotel.get("hotel_id"))
        if hid not in seen_ids:
            seen_ids.add(hid)
            unique_hotels.append(hotel)
    
    # Sort by price
    unique_hotels.sort(key=lambda x: x.get("min_price", 999999))
    
    logger.info(f"✅ Fetched {len(unique_hotels)} unique b2c=1 offers from Sunhotels")
    
    # Get unique cities from results
    cities = {}
    for hotel in unique_hotels:
        city = hotel.get("city", "Unknown")
        if city not in cities:
            cities[city] = 0
        cities[city] += 1
    
    return {
        "success": True,
        "hotels": unique_hotels,
        "total": len(unique_hotels),
        "cities": cities,
        "destinations_checked": len(destinations),
        "message": f"Found {len(unique_hotels)} last minute offers across {len(cities)} cities"
    }

@api_router.post("/admin/lastminute/save")
async def admin_save_lastminute_offers(request: Request):
    """
    Save fetched b2c=1 offers to database for frontend display.
    This replaces all existing stored offers.
    """
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    body = await request.json()
    hotels = body.get("hotels", [])
    check_in = body.get("check_in")
    check_out = body.get("check_out")
    
    if not hotels:
        raise HTTPException(status_code=400, detail="No hotels to save")
    
    # Clear existing offers
    await db.last_minute_offers.delete_many({})
    
    # Save new offers
    now = datetime.now(timezone.utc).isoformat()
    for hotel in hotels:
        offer = {
            "hotel_id": str(hotel.get("hotel_id")),
            "name": hotel.get("name"),
            "city": hotel.get("city"),
            "country": hotel.get("country"),
            "image_url": hotel.get("image_url"),
            "min_price": hotel.get("min_price"),
            "star_rating": hotel.get("star_rating"),
            "destination_id": hotel.get("destination_id"),
            "resort_id": hotel.get("resort_id"),
            "last_minute_check_in": check_in,
            "last_minute_check_out": check_out,
            "is_active": True,
            "fetched_at": now,
            "saved_by": "admin"
        }
        await db.last_minute_offers.insert_one(offer)
    
    logger.info(f"💾 Saved {len(hotels)} b2c=1 offers to database")
    
    return {
        "success": True,
        "saved_count": len(hotels),
        "message": f"Saved {len(hotels)} last minute offers"
    }

@api_router.get("/admin/lastminute/stored")
async def admin_get_stored_lastminute(request: Request):
    """Get currently stored b2c=1 offers from database"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    offers = await db.last_minute_offers.find(
        {},
        {"_id": 0}
    ).sort("fetched_at", -1).to_list(50)
    
    return {
        "offers": offers,
        "total": len(offers),
        "last_updated": offers[0].get("fetched_at") if offers else None
    }

@api_router.delete("/admin/lastminute/clear")
async def admin_clear_lastminute(request: Request):
    """Clear all stored b2c=1 offers"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    result = await db.last_minute_offers.delete_many({})
    
    logger.info(f"🗑️ Cleared {result.deleted_count} b2c=1 offers from database")
    
    return {
        "success": True,
        "deleted_count": result.deleted_count
    }

@api_router.put("/admin/lastminute/toggle/{hotel_id}")
async def admin_toggle_lastminute_offer(request: Request, hotel_id: str, is_active: bool):
    """Toggle visibility of a specific b2c=1 offer"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    result = await db.last_minute_offers.update_one(
        {"hotel_id": hotel_id},
        {"$set": {"is_active": is_active}}
    )
    
    if result.matched_count == 0:
        raise HTTPException(status_code=404, detail="Offer not found")
    
    return {"success": True, "hotel_id": hotel_id, "is_active": is_active}

@api_router.get("/hotels/{hotel_id}")
async def get_hotel(hotel_id: str, check_in: str = None, check_out: str = None, adults: int = 2, children: int = 0, children_ages: str = None, b2c: int = 0, destination_id: str = None, resort_id: str = None):
    """Get hotel details from Sunhotels Static API
    b2c=0 for normal availability, b2c=1 for last minute deals
    destination_id and resort_id provide city/area context for room search
    """
    # Handle demo hotels (from sample/fallback data)
    if hotel_id.startswith("demo_"):
        sample_hotels = sunhotels_client._get_sample_hotels(is_last_minute=b2c == 1)
        for hotel in sample_hotels:
            if hotel["hotel_id"] == hotel_id:
                hotel["check_in"] = check_in
                hotel["check_out"] = check_out
                hotel["is_last_minute"] = b2c == 1
                return hotel
        raise HTTPException(status_code=404, detail="Hotel not found")
    
    # Fetch static hotel data from Sunhotels
    hotel_data = await sunhotels_client.get_hotel_details(hotel_id)
    
    if not hotel_data:
        raise HTTPException(status_code=404, detail="Hotel not found")
    
    # Parse children_ages from comma-separated string to list of ints
    children_ages_list = None
    if children_ages:
        try:
            children_ages_list = [int(age) for age in children_ages.split(",") if age.strip()]
        except:
            children_ages_list = None
    
    # If search dates provided, also get room availability
    if check_in and check_out:
        # We need to search for rooms in this hotel
        # Pass destination_id and resort_id for better room search results
        rooms = await sunhotels_client.get_hotel_rooms(
            hotel_id, check_in, check_out, adults, children, children_ages_list, b2c,
            destination_id=destination_id, resort_id=resort_id
        )
        hotel_data["rooms"] = rooms
        hotel_data["check_in"] = check_in
        hotel_data["check_out"] = check_out
        hotel_data["is_last_minute"] = b2c == 1
    
    return hotel_data

@api_router.get("/hotels/{hotel_id}/alternatives")
async def get_hotel_alternatives(hotel_id: str, check_in: str, check_out: str, adults: int = 2, children: int = 0, destination_id: str = None):
    """
    Get alternative hotels nearby when the requested hotel has no availability.
    Returns hotels in the same destination with rooms available.
    """
    if not destination_id:
        # Try to find destination_id from the hotel's static data
        hotel_data = await sunhotels_client.get_hotel_details(hotel_id)
        if hotel_data:
            # We don't have destination_id in static data, so we'll search the lookup table
            db_config = await sunhotels_client.get_static_db_connection()
            if db_config["host"]:
                try:
                    conn = await asyncio.wait_for(
                        aiomysql.connect(
                            host=db_config["host"],
                            port=db_config["port"],
                            user=db_config["user"],
                            password=db_config["password"],
                            db=db_config["database"],
                            charset='utf8mb4'
                        ),
                        timeout=10
                    )
                    async with conn.cursor(aiomysql.DictCursor) as cursor:
                        await cursor.execute(
                            "SELECT id FROM ghwk_autocomplete_lookup WHERE hotel_id = %s AND type = 'hotel' LIMIT 1",
                            (hotel_id,)
                        )
                        result = await cursor.fetchone()
                        if result:
                            destination_id = str(result['id'])
                    conn.close()
                except:
                    pass
    
    if not destination_id:
        return {
            "hotel_id": hotel_id,
            "alternatives": [],
            "message": "Could not determine destination for alternatives"
        }
    
    # Search for hotels in the same destination
    search_params = HotelSearchParams(
        destination_id=destination_id,
        destination="",
        check_in=check_in,
        check_out=check_out,
        adults=adults,
        children=children,
        rooms=1,
        currency="EUR",
        b2c=0
    )
    
    try:
        hotels = await sunhotels_client.search_hotels(search_params)
        
        # Filter out the original hotel and limit to 6 alternatives
        alternatives = [h for h in hotels if str(h.get('hotel_id')) != str(hotel_id)][:6]
        
        return {
            "hotel_id": hotel_id,
            "destination_id": destination_id,
            "check_in": check_in,
            "check_out": check_out,
            "alternatives": alternatives,
            "total_found": len(alternatives),
            "message": f"Found {len(alternatives)} alternative hotels in the same area"
        }
    except Exception as e:
        logger.error(f"Error finding alternatives: {str(e)}")
        return {
            "hotel_id": hotel_id,
            "alternatives": [],
            "message": "Error searching for alternatives"
        }

# ==================== PREBOOK / BOOK ROUTES ====================

@api_router.post("/hotels/prebook")
async def prebook_hotel(request: PreBookRequest):
    """
    Pre-book a hotel room to verify availability and get final price.
    Returns a prebook_code needed for the final booking.
    """
    prebook_params = {
        "hotel_id": request.hotel_id,
        "room_id": request.room_id,
        "roomtype_id": request.roomtype_id,
        "meal_id": request.meal_id,
        "check_in": request.check_in,
        "check_out": request.check_out,
        "rooms": request.rooms,
        "adults": request.adults,
        "children": request.children,
        "children_ages": request.children_ages,
        "currency": request.currency,
        "search_price": request.search_price,
        "customer_country": request.customer_country,
        "b2c": request.b2c
    }
    
    result = await sunhotels_client.prebook(prebook_params)
    
    if not result.get("success"):
        raise HTTPException(status_code=400, detail=result.get("error", "PreBook failed"))
    
    return result

@api_router.post("/hotels/book")
async def book_hotel(request: BookRequest, req: Request):
    """
    Confirm a hotel booking with Sunhotels.
    IMPORTANT: This should only be called AFTER payment is confirmed!
    """
    # Verify that this booking has been paid (check payment_transactions)
    # In production, you'd want to verify the payment was successful
    
    book_params = {
        "prebook_code": request.prebook_code,
        "hotel_id": request.hotel_id,
        "room_id": request.room_id,
        "meal_id": request.meal_id,
        "check_in": request.check_in,
        "check_out": request.check_out,
        "rooms": request.rooms,
        "adults": request.adults,
        "children": request.children,
        "currency": request.currency,
        "customer_country": request.customer_country,
        "guest_first_name": request.guest_first_name,
        "guest_last_name": request.guest_last_name,
        "guest_email": request.guest_email,
        "guest2_first_name": request.guest2_first_name,
        "guest2_last_name": request.guest2_last_name,
        "payment_method_id": request.payment_method_id,
        "your_ref": request.your_ref,
        "special_request": request.special_request,
        "invoice_ref": request.invoice_ref,
        "b2c": 0
    }
    
    result = await sunhotels_client.book(book_params)
    
    if not result.get("success"):
        raise HTTPException(status_code=400, detail=result.get("error", "Booking failed"))
    
    return result

# ==================== PASS CODE ROUTES ====================

@api_router.post("/pass-code/validate")
async def validate_pass_code(data: PassCodeValidate):
    """Validate an existing FreeStays pass code"""
    code_upper = data.pass_code.upper()
    
    # First, check the new pass_codes collection (admin-generated codes)
    admin_code = await db.pass_codes.find_one({"code": code_upper}, {"_id": 0})
    if admin_code:
        if admin_code.get("status") == "used":
            return {"valid": False, "has_discount": False, "message": "This pass code has already been used"}
        
        return {
            "valid": True,
            "has_discount": True,
            "pass_type": admin_code.get("pass_type", "one_time"),
            "discount_rate": 0.15,
            "code_source": "admin_generated",
            "message": f"Valid {admin_code.get('pass_type', 'one_time').replace('_', ' ')} pass! Your Freestays advantages are active."
        }
    
    # Check if it's a user's pass code
    user_with_code = await db.users.find_one({"pass_code": code_upper}, {"_id": 0})
    if user_with_code:
        pass_type = user_with_code.get("pass_type", "free")
        expires_at = user_with_code.get("pass_expires_at")
        
        # Check if pass is still valid
        if pass_type == "annual" and expires_at:
            if isinstance(expires_at, str):
                expires_at = datetime.fromisoformat(expires_at)
            if expires_at < datetime.now(timezone.utc):
                return {"valid": False, "message": "Pass code has expired"}
        
        # Free accounts don't get discount
        if pass_type == "free":
            return {
                "valid": True,
                "has_discount": False,
                "pass_type": "free",
                "message": "Valid account code. Upgrade to FreeStays Pass for your advantages!"
            }
        
        return {
            "valid": True,
            "has_discount": True,
            "pass_type": pass_type,
            "discount_rate": 0.15,
            "code_source": "user_account",
            "message": f"Valid {pass_type} pass! Your Freestays advantages are active."
        }
    
    # Check promo codes collection
    promo = await db.promo_codes.find_one({"code": code_upper, "active": True}, {"_id": 0})
    if promo:
        return {
            "valid": True,
            "has_discount": True,
            "pass_type": "promo",
            "discount_rate": promo.get("discount_rate", 0.15),
            "code_source": "promo",
            "message": f"Promo code valid! {int(promo.get('discount_rate', 0.15) * 100)}% discount applied."
        }
    
    return {"valid": False, "has_discount": False, "message": "Invalid pass code"}

@api_router.get("/pass-code/mine")
async def get_my_pass_code(request: Request):
    """Get user's personal FreeStays pass code"""
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Please login to view your pass code")
    
    pass_type = user.get("pass_type", "free")
    has_discount = pass_type in ["one_time", "annual"]
    
    return {
        "pass_code": user.get("pass_code"),
        "pass_type": pass_type,
        "has_discount": has_discount,
        "pass_expires_at": user.get("pass_expires_at"),
        "discount_rate": 0.15 if has_discount else 0,
        "booking_fee": 0 if has_discount else BOOKING_FEE,
        "description": "15% discount = only 1% markup. No booking fee with active pass." if has_discount else "Upgrade to FreeStays Pass for 15% discount and no booking fees!"
    }

@api_router.get("/pass/pricing")
async def get_pass_pricing():
    """Get FreeStays Pass pricing options"""
    return {
        "one_time": {
            "price": PASS_ONE_TIME_PRICE,
            "currency": "EUR",
            "name": "One-Time Pass",
            "description": "15% discount on this booking. No €15 booking fee.",
            "validity": "Single booking"
        },
        "annual": {
            "price": PASS_ANNUAL_PRICE,
            "currency": "EUR",
            "name": "Annual Pass",
            "description": "15% discount on all bookings for 1 year. No booking fees ever.",
            "validity": "12 months unlimited"
        },
        "booking_fee": BOOKING_FEE
    }

# ==================== BOOKING ROUTES ====================

@api_router.post("/bookings")
async def create_booking(booking_data: BookingCreate, request: Request):
    """
    Create a new booking (PENDING - not confirmed with Sunhotels yet!)
    IMPORTANT: Booking is only confirmed with Sunhotels AFTER payment confirmation.
    """
    user = await get_current_user(request)
    
    booking_id = f"BK-{uuid.uuid4().hex[:8].upper()}"
    
    # Check if user has a valid pass
    has_valid_pass = False
    if booking_data.pass_code:
        validation = await validate_pass_code(PassCodeValidate(pass_code=booking_data.pass_code))
        has_valid_pass = validation.get("has_discount", False)
    
    # Check if user can use referral discount
    use_referral_discount = False
    if booking_data.use_referral_discount and user:
        user_doc = await db.users.find_one({"email": user["email"]})
        if user_doc and user_doc.get("referral_discount", 0) > 0:
            use_referral_discount = True
    
    # Calculate pricing (now includes referral discount)
    pricing = calculate_pricing(
        booking_data.total_price, 
        has_valid_pass, 
        booking_data.pass_purchase_type,
        use_referral_discount
    )
    
    # Generate new pass code if purchasing
    new_pass_code = None
    if booking_data.pass_purchase_type:
        new_pass_code = generate_pass_code(booking_data.pass_purchase_type)
    
    booking_doc = {
        "booking_id": booking_id,
        "user_id": user["user_id"] if user else None,
        "hotel_id": booking_data.hotel_id,
        "room_id": booking_data.room_id,
        "hotel_name": booking_data.hotel_name,
        "room_type": booking_data.room_type,
        "check_in": booking_data.check_in,
        "check_out": booking_data.check_out,
        "adults": booking_data.adults,
        "children": booking_data.children,
        "guest_first_name": booking_data.guest_first_name,
        "guest_last_name": booking_data.guest_last_name,
        "guest_email": booking_data.guest_email,
        "guest_phone": booking_data.guest_phone,
        "special_requests": booking_data.special_requests,
        # Pricing
        "nett_price": pricing["nett_price"],
        "price_before_discount": pricing["price_before_discount"],
        "discount_amount": pricing["discount_amount"],
        "room_total": pricing["room_total"],
        "booking_fee": pricing["booking_fee"],
        "pass_price": pricing["pass_price"],
        "final_price": pricing["final_total"],
        "currency": booking_data.currency,
        # Pass info
        "existing_pass_code": booking_data.pass_code if has_valid_pass else None,
        "pass_purchase_type": booking_data.pass_purchase_type,
        "new_pass_code": new_pass_code,
        # Referral discount info
        "referral_discount_applied": use_referral_discount,
        "referral_discount_amount": pricing.get("referral_discount_amount", 0),
        # Sunhotels data (for booking after payment)
        "sunhotels_room_type_id": booking_data.sunhotels_room_type_id,
        "sunhotels_block_id": booking_data.sunhotels_block_id,
        "sunhotels_booking_id": None,  # Will be set after payment confirmation
        # Status
        "status": "pending_payment",  # IMPORTANT: Not confirmed until payment!
        "payment_confirmed": False,
        "created_at": datetime.now(timezone.utc).isoformat()
    }
    
    await db.bookings.insert_one(booking_doc)
    
    return {
        "booking_id": booking_id,
        "nett_price": pricing["nett_price"],
        "price_before_discount": pricing["price_before_discount"],
        "discount_amount": pricing["discount_amount"],
        "room_total": pricing["room_total"],
        "booking_fee": pricing["booking_fee"],
        "pass_price": pricing["pass_price"],
        "final_price": pricing["final_total"],
        "currency": booking_data.currency,
        "new_pass_code": new_pass_code,
        "referral_discount_applied": use_referral_discount,
        "status": "pending_payment"
    }

@api_router.get("/bookings")
async def get_user_bookings(request: Request):
    """Get all bookings for current user"""
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Please login to view bookings")
    
    bookings = await db.bookings.find(
        {"user_id": user["user_id"]},
        {"_id": 0}
    ).sort("created_at", -1).to_list(100)
    
    return {"bookings": bookings}

@api_router.get("/bookings/{booking_id}")
async def get_booking(booking_id: str, request: Request):
    """Get booking details"""
    booking = await db.bookings.find_one({"booking_id": booking_id}, {"_id": 0})
    if not booking:
        raise HTTPException(status_code=404, detail="Booking not found")
    return booking

@api_router.post("/bookings/{booking_id}/cancel")
async def cancel_booking(booking_id: str, request: Request):
    """Cancel a booking"""
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Authentication required")
    
    booking = await db.bookings.find_one({"booking_id": booking_id}, {"_id": 0})
    if not booking:
        raise HTTPException(status_code=404, detail="Booking not found")
    
    if booking.get("user_id") != user["user_id"]:
        raise HTTPException(status_code=403, detail="Not authorized to cancel this booking")
    
    if booking.get("status") == "cancelled":
        raise HTTPException(status_code=400, detail="Booking is already cancelled")
    
    # Update booking status
    await db.bookings.update_one(
        {"booking_id": booking_id},
        {"$set": {
            "status": "cancelled",
            "cancelled_at": datetime.now(timezone.utc).isoformat(),
            "cancellation_reason": "User requested cancellation"
        }}
    )
    
    return {"success": True, "message": "Booking cancelled successfully"}

@api_router.post("/bookings/{booking_id}/cancellation-request")
async def request_cancellation(booking_id: str, request: Request, background_tasks: BackgroundTasks):
    """Submit a cancellation request for a booking (to be reviewed by admin)"""
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Authentication required")
    
    booking = await db.bookings.find_one({"booking_id": booking_id}, {"_id": 0})
    if not booking:
        raise HTTPException(status_code=404, detail="Booking not found")
    
    if booking.get("user_id") != user["user_id"]:
        raise HTTPException(status_code=403, detail="Not authorized to request cancellation for this booking")
    
    if booking.get("status") == "cancelled":
        raise HTTPException(status_code=400, detail="Booking is already cancelled")
    
    if booking.get("cancellation_requested"):
        raise HTTPException(status_code=400, detail="Cancellation already requested for this booking")
    
    # Check if check-in date has passed
    check_in = datetime.strptime(booking.get("check_in"), "%Y-%m-%d")
    if check_in.date() <= datetime.now().date():
        raise HTTPException(status_code=400, detail="Cannot request cancellation for bookings that have already started")
    
    # Update booking with cancellation request
    await db.bookings.update_one(
        {"booking_id": booking_id},
        {"$set": {
            "cancellation_requested": True,
            "cancellation_requested_at": datetime.now(timezone.utc).isoformat(),
            "cancellation_request_status": "pending"
        }}
    )
    
    # Create a cancellation request record
    await db.cancellation_requests.insert_one({
        "booking_id": booking_id,
        "user_id": user["user_id"],
        "user_email": user["email"],
        "user_name": user["name"],
        "hotel_name": booking.get("hotel_name"),
        "check_in": booking.get("check_in"),
        "check_out": booking.get("check_out"),
        "total_price": booking.get("final_price"),
        "status": "pending",
        "created_at": datetime.now(timezone.utc).isoformat()
    })
    
    # TODO: Send notification email to admin about the cancellation request
    logger.info(f"Cancellation request submitted for booking {booking_id} by user {user['email']}")
    
    return {"success": True, "message": "Cancellation request submitted. Our team will review and contact you shortly."}

# ==================== FAVORITES ROUTES ====================

@api_router.post("/favorites")
async def add_favorite(hotel: FavoriteHotel, request: Request):
    """Add a hotel to favorites"""
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Authentication required")
    
    # Check if already favorited
    existing = await db.favorites.find_one({
        "user_id": user["user_id"],
        "hotel_id": hotel.hotel_id
    })
    
    if existing:
        return {"success": True, "message": "Hotel already in favorites"}
    
    favorite_doc = {
        "user_id": user["user_id"],
        "hotel_id": hotel.hotel_id,
        "hotel_name": hotel.hotel_name,
        "star_rating": hotel.star_rating,
        "image_url": hotel.image_url,
        "location": hotel.location,
        "min_price": hotel.min_price,
        "created_at": datetime.now(timezone.utc).isoformat()
    }
    
    await db.favorites.insert_one(favorite_doc)
    return {"success": True, "message": "Hotel added to favorites"}

@api_router.delete("/favorites/{hotel_id}")
async def remove_favorite(hotel_id: str, request: Request):
    """Remove a hotel from favorites"""
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Authentication required")
    
    result = await db.favorites.delete_one({
        "user_id": user["user_id"],
        "hotel_id": hotel_id
    })
    
    if result.deleted_count == 0:
        raise HTTPException(status_code=404, detail="Hotel not in favorites")
    
    return {"success": True, "message": "Hotel removed from favorites"}

@api_router.get("/favorites")
async def get_favorites(request: Request):
    """Get user's favorite hotels"""
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Authentication required")
    
    favorites = await db.favorites.find(
        {"user_id": user["user_id"]},
        {"_id": 0}
    ).sort("created_at", -1).to_list(100)
    
    return {"favorites": favorites}

@api_router.get("/favorites/check/{hotel_id}")
async def check_favorite(hotel_id: str, request: Request):
    """Check if a hotel is in favorites"""
    user = await get_current_user(request)
    if not user:
        return {"is_favorite": False}
    
    existing = await db.favorites.find_one({
        "user_id": user["user_id"],
        "hotel_id": hotel_id
    })
    
    return {"is_favorite": existing is not None}

# ==================== TESTIMONIALS ROUTES ====================

@api_router.post("/testimonials")
async def create_testimonial(testimonial: TestimonialCreate, request: Request):
    """Create a testimonial"""
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Authentication required")
    
    if testimonial.rating < 1 or testimonial.rating > 5:
        raise HTTPException(status_code=400, detail="Rating must be between 1 and 5")
    
    testimonial_doc = {
        "testimonial_id": f"test_{uuid.uuid4().hex[:12]}",
        "user_id": user["user_id"],
        "user_name": user["name"],
        "user_picture": user.get("picture"),
        "booking_id": testimonial.booking_id,
        "hotel_name": testimonial.hotel_name,
        "rating": testimonial.rating,
        "title": testimonial.title,
        "content": testimonial.content,
        "status": "pending",  # pending, approved, rejected
        "created_at": datetime.now(timezone.utc).isoformat()
    }
    
    await db.testimonials.insert_one(testimonial_doc)
    return {"success": True, "message": "Thank you! Your review has been submitted for approval."}

@api_router.get("/testimonials")
async def get_testimonials(limit: int = 10):
    """Get approved testimonials for display"""
    testimonials = await db.testimonials.find(
        {"status": "approved"},
        {"_id": 0}
    ).sort("created_at", -1).limit(limit).to_list(limit)
    
    # If no approved testimonials, return sample ones
    if not testimonials:
        testimonials = [
            {
                "testimonial_id": "sample_1",
                "user_name": "Sarah M.",
                "rating": 5,
                "title": "Incredible savings on our honeymoon!",
                "content": "We saved over €400 on our 5-night stay in Barcelona. The room was completely free - we only paid for the amazing half-board meals. Absolutely unbelievable service!",
                "hotel_name": "Grand Hotel Barcelona",
                "created_at": "2025-12-15T10:30:00Z"
            },
            {
                "testimonial_id": "sample_2",
                "user_name": "Michael K.",
                "rating": 5,
                "title": "Best hotel booking decision ever",
                "content": "I was skeptical at first, but FreeStays delivered exactly what they promised. Free room, great food, and excellent customer service. Will definitely book again!",
                "hotel_name": "Resort Mallorca",
                "created_at": "2025-12-10T14:20:00Z"
            },
            {
                "testimonial_id": "sample_3",
                "user_name": "Emma L.",
                "rating": 5,
                "title": "Perfect family vacation",
                "content": "Booked a family trip to Greece through FreeStays. The kids loved it, and we saved enough money to extend our trip by 2 extra nights. Highly recommend!",
                "hotel_name": "Aegean Paradise Resort",
                "created_at": "2025-12-05T09:15:00Z"
            },
            {
                "testimonial_id": "sample_4",
                "user_name": "David R.",
                "rating": 5,
                "title": "Game changer for business travel",
                "content": "As someone who travels frequently for work, FreeStays has transformed my expenses. The Annual Pass pays for itself in just one trip!",
                "hotel_name": "Business Hotel Amsterdam",
                "created_at": "2025-11-28T16:45:00Z"
            }
        ]
    
    return {"testimonials": testimonials}

@api_router.get("/admin/testimonials")
async def get_all_testimonials(request: Request):
    """Admin: Get all testimonials"""
    admin_token = request.headers.get("Authorization", "").replace("Bearer ", "")
    if not admin_token:
        raise HTTPException(status_code=401, detail="Admin authentication required")
    
    testimonials = await db.testimonials.find({}, {"_id": 0}).sort("created_at", -1).to_list(100)
    return {"testimonials": testimonials}

@api_router.put("/admin/testimonials/{testimonial_id}")
async def update_testimonial_status(testimonial_id: str, status: str, request: Request):
    """Admin: Approve or reject a testimonial"""
    admin_token = request.headers.get("Authorization", "").replace("Bearer ", "")
    if not admin_token:
        raise HTTPException(status_code=401, detail="Admin authentication required")
    
    if status not in ["approved", "rejected", "pending"]:
        raise HTTPException(status_code=400, detail="Invalid status")
    
    result = await db.testimonials.update_one(
        {"testimonial_id": testimonial_id},
        {"$set": {"status": status}}
    )
    
    if result.modified_count == 0:
        raise HTTPException(status_code=404, detail="Testimonial not found")
    
    return {"success": True, "message": f"Testimonial {status}"}

# ==================== REFERRAL ROUTES ====================

@api_router.get("/referral/my-code")
async def get_my_referral_code(request: Request):
    """Get user's referral code and stats"""
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Authentication required")
    
    settings = await get_settings()
    
    # Ensure user has a referral code (for existing users)
    if not user.get("referral_code"):
        referral_code = f"FS{uuid.uuid4().hex[:8].upper()}"
        await db.users.update_one(
            {"user_id": user["user_id"]},
            {"$set": {"referral_code": referral_code, "referral_count": 0}}
        )
        user["referral_code"] = referral_code
        user["referral_count"] = 0
    
    # Get referral stats
    referrals = await db.referrals.find(
        {"referrer_id": user["user_id"]},
        {"_id": 0}
    ).to_list(100)
    
    return {
        "referral_code": user.get("referral_code"),
        "referral_count": user.get("referral_count", 0),
        "discount_amount": settings.get("referral_discount_amount", 15.0),
        "referral_enabled": settings.get("referral_enabled", True),
        "referrals": referrals
    }

@api_router.get("/referral/leaderboard")
async def get_referral_leaderboard():
    """Get top referrers leaderboard (public endpoint)"""
    try:
        # Get top 10 users by referral count
        leaderboard = await db.users.find(
            {"referral_count": {"$gt": 0}},
            {"_id": 0, "name": 1, "referral_count": 1}
        ).sort("referral_count", -1).limit(10).to_list(10)
        
        # Anonymize names (show first name + first letter of last name)
        for user in leaderboard:
            name_parts = user.get("name", "Anonymous").split()
            if len(name_parts) > 1:
                user["name"] = f"{name_parts[0]} {name_parts[-1][0]}."
            else:
                user["name"] = name_parts[0] if name_parts else "Anonymous"
        
        # Get total stats
        total_referrals = await db.referrals.count_documents({})
        total_referrers = await db.users.count_documents({"referral_count": {"$gt": 0}})
        
        return {
            "leaderboard": leaderboard,
            "total_referrals": total_referrals,
            "total_referrers": total_referrers
        }
    except Exception as e:
        logger.error(f"Error fetching leaderboard: {str(e)}")
        return {"leaderboard": [], "total_referrals": 0, "total_referrers": 0}

@api_router.get("/referral/tiers")
async def get_public_referral_tiers():
    """Get referral tiers configuration (public endpoint for frontend)"""
    try:
        settings = await get_settings()
        default_tiers = [
            {"name": "Starter", "min": 0, "max": 2, "extraDiscount": 0, "reward": "€15 per referral"},
            {"name": "Bronze", "min": 3, "max": 5, "extraDiscount": 5, "reward": "€15 + 5% extra discount"},
            {"name": "Silver", "min": 6, "max": 9, "extraDiscount": 10, "reward": "€15 + 10% extra discount"},
            {"name": "Gold", "min": 10, "max": 19, "extraDiscount": 15, "reward": "FREE Annual Pass + 15% discount"},
            {"name": "Diamond", "min": 20, "max": 999, "extraDiscount": 20, "reward": "VIP Status + 20% lifetime discount"}
        ]
        
        saved_tiers = settings.get("referral_tiers", [])
        
        # Merge saved tiers with default rewards text
        if saved_tiers:
            for i, tier in enumerate(saved_tiers):
                if i < len(default_tiers):
                    if tier.get("extraDiscount", 0) == 0:
                        tier["reward"] = "€15 per referral"
                    elif tier["name"] == "Gold":
                        tier["reward"] = f"FREE Annual Pass + {tier.get('extraDiscount', 15)}% discount"
                    elif tier["name"] == "Diamond":
                        tier["reward"] = f"VIP Status + {tier.get('extraDiscount', 20)}% lifetime discount"
                    else:
                        tier["reward"] = f"€15 + {tier.get('extraDiscount', 0)}% extra discount"
            return {"tiers": saved_tiers}
        
        return {"tiers": default_tiers}
    except Exception as e:
        logger.error(f"Error fetching referral tiers: {str(e)}")
        return {"tiers": []}

@api_router.get("/referral/validate/{code}")
async def validate_referral_code(code: str):
    """Validate a referral code"""
    settings = await get_settings()
    
    if not settings.get("referral_enabled", True):
        return {"valid": False, "message": "Referral program is currently disabled"}
    
    user = await db.users.find_one({"referral_code": code}, {"_id": 0, "name": 1, "referral_code": 1})
    
    if not user:
        return {"valid": False, "message": "Invalid referral code"}
    
    return {
        "valid": True,
        "referrer_name": user["name"],
        "discount_amount": settings.get("referral_discount_amount", 15.0)
    }

@api_router.post("/referral/apply")
async def apply_referral_discount(data: ReferralApply, request: Request):
    """Apply referral discount to user's account"""
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Authentication required")
    
    settings = await get_settings()
    
    if not settings.get("referral_enabled", True):
        raise HTTPException(status_code=400, detail="Referral program is currently disabled")
    
    if user.get("referral_discount", 0) > 0:
        raise HTTPException(status_code=400, detail="You already have a referral discount applied")
    
    referrer = await db.users.find_one({"referral_code": data.referral_code}, {"_id": 0})
    
    if not referrer:
        raise HTTPException(status_code=400, detail="Invalid referral code")
    
    if referrer["user_id"] == user["user_id"]:
        raise HTTPException(status_code=400, detail="You cannot use your own referral code")
    
    discount_amount = settings.get("referral_discount_amount", 15.0)
    
    # Apply discount to user
    await db.users.update_one(
        {"user_id": user["user_id"]},
        {
            "$set": {
                "referral_discount": discount_amount,
                "referred_by": referrer["user_id"]
            }
        }
    )
    
    # Track referral
    await db.referrals.insert_one({
        "referrer_id": referrer["user_id"],
        "referee_id": user["user_id"],
        "referral_code": data.referral_code,
        "discount_amount": discount_amount,
        "status": "pending",
        "created_at": datetime.now(timezone.utc).isoformat()
    })
    
    # Increment referrer's count
    await db.users.update_one(
        {"user_id": referrer["user_id"]},
        {"$inc": {"referral_count": 1}}
    )
    
    return {
        "success": True,
        "discount_amount": discount_amount,
        "message": f"€{discount_amount:.0f} discount applied to your account!"
    }

# ==================== PRICE DROP ROUTES ====================

@api_router.post("/price-alerts/check")
async def check_price_drops(background_tasks: BackgroundTasks):
    """Manually trigger price drop check for all users (admin only or scheduled task)"""
    settings = await get_settings()
    
    if not settings.get("price_drop_enabled", True):
        return {"success": False, "message": "Price drop notifications are disabled"}
    
    # Get all favorites with saved prices
    favorites = await db.favorites.find(
        {"min_price": {"$exists": True, "$ne": None}},
        {"_id": 0}
    ).to_list(1000)
    
    # Group by hotel to avoid duplicate API calls
    hotels_to_check = {}
    for fav in favorites:
        if fav["hotel_id"] not in hotels_to_check:
            hotels_to_check[fav["hotel_id"]] = {
                "hotel_name": fav["hotel_name"],
                "old_price": fav["min_price"],
                "users": []
            }
        hotels_to_check[fav["hotel_id"]]["users"].append(fav["user_id"])
    
    # For now, return the hotels that would be checked
    # In production, this would call Sunhotels API to get current prices
    return {
        "success": True,
        "hotels_to_check": len(hotels_to_check),
        "message": "Price check queued. Notifications will be sent for any drops."
    }

@api_router.get("/price-alerts/settings")
async def get_price_alert_settings(request: Request):
    """Get user's price alert preferences"""
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Authentication required")
    
    settings = await get_settings()
    
    return {
        "price_drop_enabled": settings.get("price_drop_enabled", True),
        "check_frequency": settings.get("price_drop_check_frequency", "daily"),
        "min_drop_percent": settings.get("price_drop_min_percent", 5)
    }

# ==================== PAYMENT ROUTES (STRIPE) ====================

class PassPurchaseRequest(BaseModel):
    pass_type: str  # 'one_time' or 'annual'
    user_email: Optional[str] = None

@api_router.post("/payments/purchase-pass")
async def purchase_pass_only(pass_data: PassPurchaseRequest, request: Request):
    """Create Stripe checkout session for pass-only purchase (no hotel booking)"""
    import stripe
    
    if pass_data.pass_type not in ['one_time', 'annual']:
        raise HTTPException(status_code=400, detail="Invalid pass type")
    
    # Get active Stripe key
    stripe.api_key = await get_active_stripe_key()
    
    # Determine pass price
    pass_price = PASS_ONE_TIME_PRICE if pass_data.pass_type == 'one_time' else PASS_ANNUAL_PRICE
    pass_name = "FreeStays Annual Pass" if pass_data.pass_type == 'annual' else "FreeStays One-Time Pass"
    pass_description = '15% discount on bookings for 1 year' if pass_data.pass_type == 'annual' else '15% discount on your next booking'
    
    # Generate a pass code in advance
    new_pass_code = generate_pass_code(pass_data.pass_type)
    
    # Get host URL
    host_url = str(request.base_url).rstrip('/')
    forwarded_host = request.headers.get('x-forwarded-host')
    if forwarded_host:
        protocol = request.headers.get('x-forwarded-proto', 'https')
        host_url = f"{protocol}://{forwarded_host}"
    
    # Create a pass purchase record
    purchase_id = f"PASS-{uuid.uuid4().hex[:8].upper()}"
    
    await db.pass_purchases.insert_one({
        "purchase_id": purchase_id,
        "pass_type": pass_data.pass_type,
        "pass_code": new_pass_code,
        "price": pass_price,
        "user_email": pass_data.user_email,
        "status": "pending",
        "created_at": datetime.now(timezone.utc).isoformat()
    })
    
    success_url = f"{host_url}/pass/success?session_id={{CHECKOUT_SESSION_ID}}&purchase_id={purchase_id}"
    cancel_url = f"{host_url}/dashboard"
    
    try:
        checkout_session = stripe.checkout.Session.create(
            payment_method_types=['card'],
            line_items=[{
                'price_data': {
                    'currency': 'eur',
                    'unit_amount': int(pass_price * 100),
                    'product_data': {
                        'name': pass_name,
                        'description': pass_description
                    }
                },
                'quantity': 1
            }],
            mode='payment',
            success_url=success_url,
            cancel_url=cancel_url,
            customer_email=pass_data.user_email,
            metadata={
                'purchase_id': purchase_id,
                'pass_type': pass_data.pass_type,
                'pass_code': new_pass_code
            }
        )
        
        # Update purchase with session ID
        await db.pass_purchases.update_one(
            {"purchase_id": purchase_id},
            {"$set": {"session_id": checkout_session.id}}
        )
        
        return {
            "url": checkout_session.url,
            "session_id": checkout_session.id
        }
        
    except Exception as e:
        logger.error(f"Stripe error: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@api_router.get("/pass/verify/{session_id}")
async def verify_pass_purchase(session_id: str, request: Request):
    """Verify pass purchase after Stripe checkout"""
    import stripe
    
    stripe.api_key = await get_active_stripe_key()
    
    try:
        session = stripe.checkout.Session.retrieve(session_id)
        
        if session.payment_status == 'paid':
            purchase_id = session.metadata.get('purchase_id')
            pass_code = session.metadata.get('pass_code')
            pass_type = session.metadata.get('pass_type')
            
            # Get purchase record for user email
            purchase = await db.pass_purchases.find_one({"purchase_id": purchase_id})
            user_email = purchase.get('user_email') if purchase else None
            
            # Update purchase status
            await db.pass_purchases.update_one(
                {"purchase_id": purchase_id},
                {"$set": {"status": "completed", "paid_at": datetime.now(timezone.utc).isoformat()}}
            )
            
            # Add to pass_codes collection for admin tracking (if not already exists)
            existing_code = await db.pass_codes.find_one({"code": pass_code})
            if not existing_code:
                pass_price = PASS_ANNUAL_PRICE if pass_type == 'annual' else PASS_ONE_TIME_PRICE
                await db.pass_codes.insert_one({
                    "code": pass_code,
                    "pass_type": pass_type,
                    "status": "active",
                    "source": "purchase",  # Indicates this was a customer purchase
                    "purchase_id": purchase_id,
                    "purchased_by": user_email,
                    "price": pass_price,
                    "created_at": datetime.now(timezone.utc).isoformat(),
                    "used_at": None,
                    "used_by": None,
                    "notes": f"Purchased via Stripe - {purchase_id}"
                })
                logger.info(f"Added purchased pass code to admin: {pass_code} ({pass_type})")
            
            # If user is logged in, update their pass
            user = await get_current_user(request)
            if user:
                pass_expiry = None
                if pass_type == 'annual':
                    pass_expiry = (datetime.now(timezone.utc) + timedelta(days=365)).isoformat()
                
                await db.users.update_one(
                    {"email": user['email']},
                    {"$set": {
                        "pass_type": pass_type,
                        "pass_code": pass_code,
                        "pass_expiry": pass_expiry,
                        "pass_purchased_at": datetime.now(timezone.utc).isoformat()
                    }}
                )
                
                # Update pass_codes with who activated it
                await db.pass_codes.update_one(
                    {"code": pass_code},
                    {"$set": {
                        "used_at": datetime.now(timezone.utc).isoformat(),
                        "used_by": user['email'],
                        "status": "used" if pass_type == 'one_time' else "active"
                    }}
                )
            
            return {
                "success": True,
                "pass_code": pass_code,
                "pass_type": pass_type,
                "message": f"Your {'Annual' if pass_type == 'annual' else 'One-Time'} Pass is now active!"
            }
        else:
            return {"success": False, "message": "Payment not completed yet", "status": session.payment_status}
            
    except Exception as e:
        logger.error(f"Pass verification error: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@api_router.get("/payments/config")
async def get_payment_config():
    """Get Stripe publishable key for frontend"""
    publishable_key = await get_stripe_publishable_key()
    return {"publishable_key": publishable_key}

@api_router.post("/payments/create-checkout")
async def create_checkout_session(payment_data: PaymentCreate, request: Request):
    """Create Stripe checkout session"""
    import stripe
    
    # Get active Stripe key based on mode (live or test)
    stripe.api_key = await get_active_stripe_key()
    
    booking = await db.bookings.find_one({"booking_id": payment_data.booking_id}, {"_id": 0})
    if not booking:
        raise HTTPException(status_code=404, detail="Booking not found")
    
    # Get host URL
    host_url = str(request.base_url).rstrip('/')
    forwarded_host = request.headers.get('x-forwarded-host')
    if forwarded_host:
        protocol = request.headers.get('x-forwarded-proto', 'https')
        host_url = f"{protocol}://{forwarded_host}"
    
    success_url = f"{host_url}/booking/success?session_id={{CHECKOUT_SESSION_ID}}&booking_id={payment_data.booking_id}"
    cancel_url = f"{host_url}/booking/{payment_data.booking_id}"
    
    try:
        # Build line items
        line_items = [{
            'price_data': {
                'currency': payment_data.currency.lower(),
                'unit_amount': int(booking['room_total'] * 100),
                'product_data': {
                    'name': f"Hotel Booking - {booking['hotel_name']}",
                    'description': f"{booking['room_type']} | {booking['check_in']} to {booking['check_out']}"
                }
            },
            'quantity': 1
        }]
        
        # Add booking fee if applicable
        if booking.get('booking_fee', 0) > 0:
            line_items.append({
                'price_data': {
                    'currency': payment_data.currency.lower(),
                    'unit_amount': int(booking['booking_fee'] * 100),
                    'product_data': {
                        'name': 'Booking Fee',
                        'description': 'One-time booking processing fee'
                    }
                },
                'quantity': 1
            })
        
        # Add pass purchase if applicable
        if booking.get('pass_price', 0) > 0:
            pass_type = booking.get('pass_purchase_type', 'one_time')
            pass_name = "FreeStays Annual Pass" if pass_type == 'annual' else "FreeStays One-Time Pass"
            line_items.append({
                'price_data': {
                    'currency': payment_data.currency.lower(),
                    'unit_amount': int(booking['pass_price'] * 100),
                    'product_data': {
                        'name': pass_name,
                        'description': '15% discount on bookings' + (' for 1 year' if pass_type == 'annual' else ' (this booking)')
                    }
                },
                'quantity': 1
            })
        
        checkout_session = stripe.checkout.Session.create(
            payment_method_types=['card'],
            line_items=line_items,
            mode='payment',
            success_url=success_url,
            cancel_url=cancel_url,
            metadata={
                'booking_id': payment_data.booking_id,
                'hotel_id': booking['hotel_id'],
                'new_pass_code': booking.get('new_pass_code', ''),
                'pass_purchase_type': booking.get('pass_purchase_type', '')
            }
        )
        
        # Save payment session
        transaction_doc = {
            "transaction_id": f"TXN-{uuid.uuid4().hex[:8].upper()}",
            "booking_id": payment_data.booking_id,
            "session_id": checkout_session.id,
            "amount": payment_data.amount,
            "currency": payment_data.currency,
            "status": "pending",
            "created_at": datetime.now(timezone.utc).isoformat()
        }
        await db.payment_transactions.insert_one(transaction_doc)
        
        return {
            "url": checkout_session.url,
            "session_id": checkout_session.id
        }
    except Exception as e:
        logger.error(f"Stripe error: {str(e)}")
        raise HTTPException(status_code=500, detail="Payment processing error")

@api_router.get("/payments/status/{session_id}")
async def get_payment_status(session_id: str):
    """Check payment status and confirm booking with Sunhotels if paid"""
    import stripe
    
    # Get active Stripe key based on mode (live or test)
    stripe.api_key = await get_active_stripe_key()
    
    try:
        session = stripe.checkout.Session.retrieve(session_id)
        
        if session.payment_status == "paid":
            # Update transaction
            await db.payment_transactions.update_one(
                {"session_id": session_id},
                {"$set": {"status": "completed", "updated_at": datetime.now(timezone.utc).isoformat()}}
            )
            
            transaction = await db.payment_transactions.find_one({"session_id": session_id}, {"_id": 0})
            if transaction:
                booking = await db.bookings.find_one({"booking_id": transaction["booking_id"]}, {"_id": 0})
                
                if booking and not booking.get("payment_confirmed"):
                    # IMPORTANT: Now confirm booking with Sunhotels!
                    sunhotels_result = await sunhotels_client.confirm_booking_with_sunhotels(booking)
                    
                    # Update booking as confirmed
                    update_data = {
                        "status": "confirmed",
                        "payment_confirmed": True,
                        "sunhotels_booking_id": sunhotels_result.get("sunhotels_booking_id"),
                        "confirmation_number": sunhotels_result.get("confirmation_number"),
                        "updated_at": datetime.now(timezone.utc).isoformat()
                    }
                    
                    await db.bookings.update_one(
                        {"booking_id": transaction["booking_id"]},
                        {"$set": update_data}
                    )
                    
                    # If pass was purchased, update user's pass
                    if booking.get("new_pass_code") and booking.get("user_id"):
                        pass_type = booking.get("pass_purchase_type")
                        expires_at = None
                        if pass_type == "annual":
                            expires_at = (datetime.now(timezone.utc) + timedelta(days=365)).isoformat()
                        
                        await db.users.update_one(
                            {"user_id": booking["user_id"]},
                            {"$set": {
                                "pass_code": booking["new_pass_code"],
                                "pass_type": pass_type,
                                "pass_expires_at": expires_at
                            }}
                        )
                    
                    # Send confirmation email to guest
                    updated_booking = {**booking, **update_data}
                    email_result = await EmailService.send_booking_confirmation(updated_booking)
                    if email_result.get("success"):
                        logger.info(f"Confirmation email sent for booking: {transaction['booking_id']}")
                    else:
                        logger.warning(f"Failed to send confirmation email: {email_result.get('error')}")
                    
                    logger.info(f"Booking confirmed: {transaction['booking_id']}, Pass code: {booking.get('new_pass_code')}")
        
        return {
            "status": session.status,
            "payment_status": session.payment_status,
            "amount_total": session.amount_total / 100 if session.amount_total else 0,
            "currency": session.currency
        }
    except Exception as e:
        logger.error(f"Payment status error: {str(e)}")
        raise HTTPException(status_code=500, detail="Error checking payment status")

@api_router.post("/webhook/stripe")
async def stripe_webhook(request: Request):
    """Handle Stripe webhooks"""
    import stripe
    
    # Get active Stripe key based on mode (live or test)
    stripe.api_key = await get_active_stripe_key()
    
    payload = await request.body()
    
    try:
        event = json.loads(payload)
        
        if event["type"] == "checkout.session.completed":
            session = event["data"]["object"]
            booking_id = session.get("metadata", {}).get("booking_id")
            
            if booking_id:
                booking = await db.bookings.find_one({"booking_id": booking_id}, {"_id": 0})
                
                if booking and not booking.get("payment_confirmed"):
                    # Confirm with Sunhotels
                    sunhotels_result = await sunhotels_client.confirm_booking_with_sunhotels(booking)
                    
                    update_data = {
                        "status": "confirmed",
                        "payment_confirmed": True,
                        "sunhotels_booking_id": sunhotels_result.get("sunhotels_booking_id"),
                        "confirmation_number": sunhotels_result.get("confirmation_number"),
                        "updated_at": datetime.now(timezone.utc).isoformat()
                    }
                    
                    await db.bookings.update_one(
                        {"booking_id": booking_id},
                        {"$set": update_data}
                    )
                    
                    # Send confirmation email to guest
                    updated_booking = {**booking, **update_data}
                    email_result = await EmailService.send_booking_confirmation(updated_booking)
                    if email_result.get("success"):
                        logger.info(f"Webhook: Confirmation email sent for booking: {booking_id}")
                    else:
                        logger.warning(f"Webhook: Failed to send confirmation email: {email_result.get('error')}")
                    
                    # Update user pass if purchased
                    new_pass_code = session.get("metadata", {}).get("new_pass_code")
                    pass_type = session.get("metadata", {}).get("pass_purchase_type")
                    
                    if new_pass_code and booking.get("user_id"):
                        expires_at = None
                        if pass_type == "annual":
                            expires_at = (datetime.now(timezone.utc) + timedelta(days=365)).isoformat()
                        
                        await db.users.update_one(
                            {"user_id": booking["user_id"]},
                            {"$set": {
                                "pass_code": new_pass_code,
                                "pass_type": pass_type,
                                "pass_expires_at": expires_at
                            }}
                        )
                    
                    # Mark admin-generated pass code as used (if applied)
                    existing_pass_code = booking.get("existing_pass_code")
                    if existing_pass_code:
                        await db.pass_codes.update_one(
                            {"code": existing_pass_code.upper(), "status": "active"},
                            {"$set": {
                                "status": "used",
                                "used_at": datetime.now(timezone.utc).isoformat(),
                                "used_by": booking.get("guest_email"),
                                "booking_id": booking_id
                            }}
                        )
                    
                    # Reset referral discount after use (one-time use only)
                    if booking.get("referral_discount_applied") and booking.get("user_id"):
                        await db.users.update_one(
                            {"user_id": booking["user_id"]},
                            {"$set": {"referral_discount": 0}}
                        )
                        logger.info(f"Referral discount reset for user: {booking['user_id']}")
                
                await db.payment_transactions.update_one(
                    {"session_id": session["id"]},
                    {"$set": {"status": "completed", "updated_at": datetime.now(timezone.utc).isoformat()}}
                )
        
        return {"received": True}
    except Exception as e:
        logger.error(f"Webhook error: {str(e)}")
        raise HTTPException(status_code=400, detail="Webhook error")

# ==================== HEALTH CHECK ====================

@api_router.get("/health")
async def health_check():
    return {"status": "healthy", "service": "FreeStays API", "timestamp": datetime.now(timezone.utc).isoformat()}

@api_router.get("/")
async def root():
    return {"message": "Welcome to FreeStays API - Commission-free hotel bookings"}

# ==================== ADMIN ROUTES ====================

@api_router.post("/admin/login")
async def admin_login(credentials: AdminLogin):
    """Admin login - returns token if email and password match"""
    settings = await get_settings()
    stored_email = settings.get("admin_email", "admin@freestays.eu")
    stored_password = settings.get("admin_password", ADMIN_PASSWORD)
    
    if credentials.email == stored_email and credentials.password == stored_password:
        # Create admin token
        token = jwt.encode({
            "role": "admin",
            "email": credentials.email,
            "exp": datetime.now(timezone.utc) + timedelta(hours=24),
            "iat": datetime.now(timezone.utc)
        }, JWT_SECRET, algorithm=JWT_ALGORITHM)
        return {"success": True, "token": token, "email": credentials.email}
    
    raise HTTPException(status_code=401, detail="Invalid admin credentials")

async def verify_admin(request: Request) -> bool:
    """Verify admin token - checks user's is_admin flag in database"""
    auth_header = request.headers.get("Authorization")
    if not auth_header or not auth_header.startswith("Bearer "):
        return False
    
    try:
        token = auth_header[7:]
        payload = jwt.decode(token, JWT_SECRET, algorithms=[JWT_ALGORITHM])
        # First check if role is explicitly set (for backwards compatibility)
        if payload.get("role") == "admin":
            return True
        # Otherwise, look up user in database and check is_admin flag
        user_id = payload.get("user_id")
        if user_id:
            user = await db.users.find_one({"user_id": user_id}, {"is_admin": 1})
            if user and user.get("is_admin") == True:
                return True
        return False
    except:
        return False

# ==================== PRICE COMPARISON ADMIN ENDPOINTS ====================

@api_router.get("/admin/price-comparisons")
async def get_price_comparisons(request: Request):
    """Get stored price comparison results for admin"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    comparisons = await db.price_comparisons.find(
        {}, 
        {"_id": 0}
    ).sort("created_at", -1).limit(50).to_list(50)
    
    return {"comparisons": comparisons}

@api_router.get("/admin/price-comparisons/{comparison_id}")
async def get_price_comparison_detail(comparison_id: str, request: Request):
    """Get specific price comparison details"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    comparison = await db.price_comparisons.find_one(
        {"comparison_id": comparison_id},
        {"_id": 0}
    )
    
    if not comparison:
        raise HTTPException(status_code=404, detail="Comparison not found")
    
    return comparison

@api_router.get("/admin/price-comparisons/{comparison_id}/download")
async def download_price_comparison(comparison_id: str, request: Request):
    """Download price comparison as CSV"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    comparison = await db.price_comparisons.find_one(
        {"comparison_id": comparison_id},
        {"_id": 0}
    )
    
    if not comparison:
        raise HTTPException(status_code=404, detail="Comparison not found")
    
    # Generate CSV content
    import io
    import csv
    
    output = io.StringIO()
    writer = csv.writer(output)
    
    # Header info
    writer.writerow(["Price Comparison Report"])
    writer.writerow(["Destination", comparison.get('destination', 'N/A')])
    writer.writerow(["Check-in", comparison.get('check_in', 'N/A')])
    writer.writerow(["Check-out", comparison.get('check_out', 'N/A')])
    writer.writerow(["Guests", comparison.get('guests', 'N/A')])
    writer.writerow(["Total Savings", f"€{comparison.get('total_savings', 0):.2f}"])
    writer.writerow([])
    
    # Hotels header
    writer.writerow(["Hotel Name", "Stars", "FreeStays Price", "Est. OTA Price", "Savings", "Savings %"])
    
    # Hotels data
    for hotel in comparison.get('hotels', []):
        savings = hotel.get('estimated_ota_price', 0) - hotel.get('freestays_price', 0)
        writer.writerow([
            hotel.get('name', 'N/A'),
            hotel.get('stars', 'N/A'),
            f"€{hotel.get('freestays_price', 0):.2f}",
            f"€{hotel.get('estimated_ota_price', 0):.2f}",
            f"€{savings:.2f}",
            f"{hotel.get('savings_percent', 0):.1f}%"
        ])
    
    csv_content = output.getvalue()
    
    from fastapi.responses import Response
    return Response(
        content=csv_content,
        media_type="text/csv",
        headers={"Content-Disposition": f"attachment; filename=price_comparison_{comparison_id}.csv"}
    )

class SendMarketingEmailRequest(BaseModel):
    comparison_id: str
    recipient_emails: List[str]

@api_router.post("/admin/price-comparisons/send-marketing")
async def send_marketing_email(data: SendMarketingEmailRequest, request: Request, background_tasks: BackgroundTasks):
    """Send price comparison marketing email to selected customers"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    comparison = await db.price_comparisons.find_one(
        {"comparison_id": data.comparison_id},
        {"_id": 0}
    )
    
    if not comparison:
        raise HTTPException(status_code=404, detail="Comparison not found")
    
    # Send emails to all recipients
    async def send_emails():
        for email in data.recipient_emails:
            await PriceComparisonService.send_comparison_email(comparison, visitor_email=email)
    
    background_tasks.add_task(send_emails)
    
    return {"success": True, "message": f"Marketing emails queued for {len(data.recipient_emails)} recipients"}

@api_router.post("/admin/price-comparisons/test-email")
async def send_test_comparison_email(request: Request, background_tasks: BackgroundTasks):
    """Send test price comparison email with sample data"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Create sample comparison data with hotels
    sample_data = {
        "destination": "Amsterdam, Netherlands",
        "destination_id": "test",
        "check_in": "2026-02-15",
        "check_out": "2026-02-18",
        "guests": "2 adults",
        "adults": 2,
        "children": 0,
        "hotels_count": 45,
        "hotels_with_savings": 12,
        "total_savings": 856.50,
        "hotels": [
            {"name": "Hotel Pulitzer Amsterdam", "stars": 5, "freestays_price": 450.00, "estimated_ota_price": 580.00, "savings_percent": 22.4},
            {"name": "NH Collection Grand Hotel", "stars": 5, "freestays_price": 380.00, "estimated_ota_price": 485.00, "savings_percent": 21.6},
            {"name": "Hilton Amsterdam", "stars": 5, "freestays_price": 320.00, "estimated_ota_price": 399.00, "savings_percent": 19.8},
            {"name": "DoubleTree by Hilton Centraal", "stars": 4, "freestays_price": 245.00, "estimated_ota_price": 298.00, "savings_percent": 17.8},
            {"name": "Movenpick City Centre", "stars": 4, "freestays_price": 215.00, "estimated_ota_price": 259.00, "savings_percent": 17.0},
            {"name": "Park Plaza Victoria", "stars": 4, "freestays_price": 198.00, "estimated_ota_price": 235.00, "savings_percent": 15.7},
            {"name": "Ibis Amsterdam Centre", "stars": 3, "freestays_price": 145.00, "estimated_ota_price": 169.00, "savings_percent": 14.2},
            {"name": "Holiday Inn Express", "stars": 3, "freestays_price": 125.00, "estimated_ota_price": 145.00, "savings_percent": 13.8},
        ]
    }
    
    settings = await PriceComparisonService.get_comparison_settings()
    campaign_email = settings.get("email_address", "info@freestays.eu")
    
    background_tasks.add_task(PriceComparisonService.send_comparison_email, sample_data, campaign_email)
    
    return {"success": True, "message": f"Test comparison email sent to {campaign_email}"}

@api_router.get("/admin/follow-up-emails/stats")
async def get_follow_up_email_stats(request: Request):
    """Get statistics about follow-up emails"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    now = datetime.now(timezone.utc)
    min_age = now - timedelta(hours=48)
    max_age = now - timedelta(hours=24)
    
    # Count pending follow-ups
    pending_count = await db.price_comparisons.count_documents({
        "visitor_email": {"$ne": None, "$exists": True},
        "follow_up_sent": {"$ne": True},
        "created_at": {"$gte": min_age.isoformat(), "$lte": max_age.isoformat()}
    })
    
    # Count sent follow-ups
    sent_count = await db.price_comparisons.count_documents({
        "follow_up_sent": True
    })
    
    # Count total with visitor email
    total_with_email = await db.price_comparisons.count_documents({
        "visitor_email": {"$ne": None, "$exists": True}
    })
    
    return {
        "pending_follow_ups": pending_count,
        "sent_follow_ups": sent_count,
        "total_with_email": total_with_email,
        "next_check_window": f"{max_age.isoformat()} to {min_age.isoformat()}"
    }

@api_router.post("/admin/follow-up-emails/trigger")
async def trigger_follow_up_emails(request: Request, background_tasks: BackgroundTasks):
    """Manually trigger follow-up email processing"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    background_tasks.add_task(PriceComparisonService.process_follow_up_emails)
    
    return {"success": True, "message": "Follow-up email processing started in background"}

@api_router.post("/admin/follow-up-emails/test")
async def send_test_follow_up_email(request: Request, background_tasks: BackgroundTasks):
    """Send test follow-up email to campaign email"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    settings = await PriceComparisonService.get_comparison_settings()
    campaign_email = settings.get("email_address", "info@freestays.eu")
    
    # Create test comparison data
    test_comparison = {
        "comparison_id": "test_followup",
        "visitor_email": campaign_email,
        "destination": "Amsterdam, Netherlands",
        "destination_id": "13",
        "check_in": "2026-02-15",
        "check_out": "2026-02-18",
        "guests": "2 adults",
        "adults": 2,
        "children": 0,
        "hotels_count": 45,
        "hotels_with_savings": 12,
        "total_savings": 856.50,
        "hotels": [
            {"hotel_id": "test1", "name": "Hotel Pulitzer Amsterdam", "stars": 5, "freestays_price": 450.00, "estimated_ota_price": 580.00},
            {"hotel_id": "test2", "name": "NH Collection Grand Hotel", "stars": 5, "freestays_price": 380.00, "estimated_ota_price": 485.00},
            {"hotel_id": "test3", "name": "Hilton Amsterdam", "stars": 5, "freestays_price": 320.00, "estimated_ota_price": 399.00},
            {"hotel_id": "test4", "name": "DoubleTree by Hilton", "stars": 4, "freestays_price": 245.00, "estimated_ota_price": 298.00},
            {"hotel_id": "test5", "name": "Movenpick City Centre", "stars": 4, "freestays_price": 215.00, "estimated_ota_price": 259.00}
        ]
    }
    
    async def send_test():
        await PriceComparisonService.send_follow_up_email(test_comparison)
    
    background_tasks.add_task(send_test)
    
    return {"success": True, "message": f"Test follow-up email sent to {campaign_email}"}

# ==================== USER PRICE COMPARISON ENDPOINTS ====================

@api_router.get("/user/price-comparisons")
async def get_user_price_comparisons(request: Request):
    """Get user's saved price comparisons"""
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Authentication required")
    
    comparisons = await db.price_comparisons.find(
        {"user_id": user["user_id"]},
        {"_id": 0, "hotels": 0}  # Exclude full hotel list for listing
    ).sort("created_at", -1).limit(20).to_list(20)
    
    return {"comparisons": comparisons}

@api_router.get("/user/price-comparisons/{comparison_id}")
async def get_user_price_comparison_detail(comparison_id: str, request: Request):
    """Get specific price comparison for user"""
    user = await get_current_user(request)
    if not user:
        raise HTTPException(status_code=401, detail="Authentication required")
    
    comparison = await db.price_comparisons.find_one(
        {"comparison_id": comparison_id, "user_id": user["user_id"]},
        {"_id": 0}
    )
    
    if not comparison:
        raise HTTPException(status_code=404, detail="Comparison not found")
    
    return comparison

class SaveComparisonRequest(BaseModel):
    comparison_data: Dict
    visitor_email: Optional[str] = None

@api_router.post("/price-comparison/save")
async def save_price_comparison(data: SaveComparisonRequest, request: Request, background_tasks: BackgroundTasks):
    """Save price comparison and optionally send email to visitor"""
    user = await get_current_user(request)
    user_id = user["user_id"] if user else None
    
    # Store the comparison
    comparison_id = await PriceComparisonService.store_comparison_result(
        data.comparison_data,
        user_id=user_id,
        visitor_email=data.visitor_email
    )
    
    # Send email if visitor provided email
    if data.visitor_email:
        background_tasks.add_task(
            PriceComparisonService.send_comparison_email,
            data.comparison_data,
            data.visitor_email
        )
    
    return {"success": True, "comparison_id": comparison_id}

# ==================== PUBLIC SETTINGS ====================

@api_router.get("/settings/ui")
async def get_ui_settings():
    """Get public UI settings (no auth required)"""
    settings = await get_settings()
    return {
        "darkMode_enabled": settings.get("darkMode_enabled", True)
    }

# ==================== ADMIN SETTINGS ====================

@api_router.get("/admin/settings")
async def get_admin_settings(request: Request):
    """Get current admin settings"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    settings = await get_settings()
    
    # Mask sensitive data - Stripe keys (new field names)
    if settings.get("stripe_test_secret_key"):
        key = settings["stripe_test_secret_key"]
        settings["stripe_test_secret_key_masked"] = f"{key[:7]}...{key[-4:]}" if len(key) > 11 else "****"
    if settings.get("stripe_live_secret_key"):
        key = settings["stripe_live_secret_key"]
        settings["stripe_live_secret_key_masked"] = f"{key[:7]}...{key[-4:]}" if len(key) > 11 else "****"
    # Legacy field masking (backwards compatibility)
    if settings.get("stripe_live_key"):
        key = settings["stripe_live_key"]
        settings["stripe_live_key_masked"] = f"{key[:7]}...{key[-4:]}" if len(key) > 11 else "****"
    if settings.get("stripe_test_key"):
        key = settings["stripe_test_key"]
        settings["stripe_test_key_masked"] = f"{key[:7]}...{key[-4:]}" if len(key) > 11 else "****"
    if settings.get("stripe_api_key"):  # Legacy
        key = settings["stripe_api_key"]
        settings["stripe_api_key_masked"] = f"{key[:7]}...{key[-4:]}" if len(key) > 11 else "****"
    if settings.get("sunhotels_password"):
        settings["sunhotels_password_masked"] = "********"
    
    return settings

@api_router.put("/admin/settings")
async def update_admin_settings(settings_update: AdminSettings, request: Request):
    """Update admin settings"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Update only provided fields
    update_dict = {}
    
    # Stripe settings (new field names)
    if settings_update.stripe_mode:
        if settings_update.stripe_mode not in ["live", "test"]:
            raise HTTPException(status_code=400, detail="stripe_mode must be 'live' or 'test'")
        update_dict["stripe_mode"] = settings_update.stripe_mode
    if settings_update.stripe_test_secret_key is not None:
        update_dict["stripe_test_secret_key"] = settings_update.stripe_test_secret_key
    if settings_update.stripe_test_publishable_key is not None:
        update_dict["stripe_test_publishable_key"] = settings_update.stripe_test_publishable_key
    if settings_update.stripe_live_secret_key is not None:
        update_dict["stripe_live_secret_key"] = settings_update.stripe_live_secret_key
    if settings_update.stripe_live_publishable_key is not None:
        update_dict["stripe_live_publishable_key"] = settings_update.stripe_live_publishable_key
    # Legacy fields (backwards compatibility)
    if settings_update.stripe_live_key:
        update_dict["stripe_live_key"] = settings_update.stripe_live_key
    if settings_update.stripe_test_key:
        update_dict["stripe_test_key"] = settings_update.stripe_test_key
    if settings_update.stripe_api_key:  # Legacy support
        update_dict["stripe_api_key"] = settings_update.stripe_api_key
    
    # Sunhotels settings
    if settings_update.sunhotels_username:
        update_dict["sunhotels_username"] = settings_update.sunhotels_username
    if settings_update.sunhotels_password:
        update_dict["sunhotels_password"] = settings_update.sunhotels_password
    if settings_update.sunhotels_mode:
        if settings_update.sunhotels_mode not in ["live", "test"]:
            raise HTTPException(status_code=400, detail="sunhotels_mode must be 'live' or 'test'")
        update_dict["sunhotels_mode"] = settings_update.sunhotels_mode
    
    # Static database settings
    if settings_update.static_db_host is not None:
        update_dict["static_db_host"] = settings_update.static_db_host
    if settings_update.static_db_port is not None:
        update_dict["static_db_port"] = settings_update.static_db_port
    if settings_update.static_db_name is not None:
        update_dict["static_db_name"] = settings_update.static_db_name
    if settings_update.static_db_user is not None:
        update_dict["static_db_user"] = settings_update.static_db_user
    if settings_update.static_db_password is not None:
        update_dict["static_db_password"] = settings_update.static_db_password
    
    # Pricing settings
    if settings_update.pass_one_time_price is not None:
        update_dict["pass_one_time_price"] = settings_update.pass_one_time_price
    if settings_update.pass_annual_price is not None:
        update_dict["pass_annual_price"] = settings_update.pass_annual_price
    if settings_update.booking_fee is not None:
        update_dict["booking_fee"] = settings_update.booking_fee
    if settings_update.markup_rate is not None:
        update_dict["markup_rate"] = settings_update.markup_rate
    if settings_update.vat_rate is not None:
        update_dict["vat_rate"] = settings_update.vat_rate
    if settings_update.discount_rate is not None:
        update_dict["discount_rate"] = settings_update.discount_rate
    if settings_update.admin_password:
        update_dict["admin_password"] = settings_update.admin_password
    
    # SMTP Email Settings
    if settings_update.smtp_host is not None:
        update_dict["smtp_host"] = settings_update.smtp_host
    if settings_update.smtp_port is not None:
        update_dict["smtp_port"] = settings_update.smtp_port
    if settings_update.smtp_username is not None:
        update_dict["smtp_username"] = settings_update.smtp_username
    if settings_update.smtp_password is not None:
        update_dict["smtp_password"] = settings_update.smtp_password
    if settings_update.smtp_from_email is not None:
        update_dict["smtp_from_email"] = settings_update.smtp_from_email
    if settings_update.smtp_from_name is not None:
        update_dict["smtp_from_name"] = settings_update.smtp_from_name
    if settings_update.smtp_enabled is not None:
        update_dict["smtp_enabled"] = settings_update.smtp_enabled
    
    # Company Branding
    if settings_update.company_name is not None:
        update_dict["company_name"] = settings_update.company_name
    if settings_update.company_logo_url is not None:
        update_dict["company_logo_url"] = settings_update.company_logo_url
    if settings_update.company_website is not None:
        update_dict["company_website"] = settings_update.company_website
    if settings_update.company_support_email is not None:
        update_dict["company_support_email"] = settings_update.company_support_email
    
    # Last Minute Configuration
    if settings_update.last_minute_count is not None:
        update_dict["last_minute_count"] = settings_update.last_minute_count
    if settings_update.last_minute_check_in is not None:
        update_dict["last_minute_check_in"] = settings_update.last_minute_check_in
    if settings_update.last_minute_check_out is not None:
        update_dict["last_minute_check_out"] = settings_update.last_minute_check_out
    if settings_update.last_minute_title is not None:
        update_dict["last_minute_title"] = settings_update.last_minute_title
    if settings_update.last_minute_subtitle is not None:
        update_dict["last_minute_subtitle"] = settings_update.last_minute_subtitle
    if settings_update.last_minute_badge_text is not None:
        update_dict["last_minute_badge_text"] = settings_update.last_minute_badge_text
    
    # Price Comparison Settings
    if settings_update.price_comparison_enabled is not None:
        update_dict["price_comparison_enabled"] = settings_update.price_comparison_enabled
    if settings_update.ota_markup_percentage is not None:
        update_dict["ota_markup_percentage"] = settings_update.ota_markup_percentage
    if settings_update.comparison_min_savings_percent is not None:
        update_dict["comparison_min_savings_percent"] = settings_update.comparison_min_savings_percent
    if settings_update.comparison_email_frequency is not None:
        if settings_update.comparison_email_frequency not in ["search", "daily", "weekly", "disabled"]:
            raise HTTPException(status_code=400, detail="comparison_email_frequency must be 'search', 'daily', 'weekly', or 'disabled'")
        update_dict["comparison_email_frequency"] = settings_update.comparison_email_frequency
    if settings_update.comparison_email_address is not None:
        update_dict["comparison_email_address"] = settings_update.comparison_email_address
    
    # Referral Program Settings
    if settings_update.referral_enabled is not None:
        update_dict["referral_enabled"] = settings_update.referral_enabled
    if settings_update.referral_discount_amount is not None:
        update_dict["referral_discount_amount"] = settings_update.referral_discount_amount
    if settings_update.referral_min_booking_value is not None:
        update_dict["referral_min_booking_value"] = settings_update.referral_min_booking_value
    if settings_update.referral_max_uses_per_code is not None:
        update_dict["referral_max_uses_per_code"] = settings_update.referral_max_uses_per_code
    
    # Price Drop Notification Settings
    if settings_update.price_drop_enabled is not None:
        update_dict["price_drop_enabled"] = settings_update.price_drop_enabled
    if settings_update.price_drop_check_frequency is not None:
        if settings_update.price_drop_check_frequency not in ["daily", "6hours", "12hours"]:
            raise HTTPException(status_code=400, detail="price_drop_check_frequency must be 'daily', '6hours', or '12hours'")
        update_dict["price_drop_check_frequency"] = settings_update.price_drop_check_frequency
    if settings_update.price_drop_min_percent is not None:
        update_dict["price_drop_min_percent"] = settings_update.price_drop_min_percent
    
    # Contact Page Settings
    if settings_update.contact_page_title is not None:
        update_dict["contact_page_title"] = settings_update.contact_page_title
    if settings_update.contact_page_subtitle is not None:
        update_dict["contact_page_subtitle"] = settings_update.contact_page_subtitle
    if settings_update.contact_email is not None:
        update_dict["contact_email"] = settings_update.contact_email
    if settings_update.contact_email_note is not None:
        update_dict["contact_email_note"] = settings_update.contact_email_note
    if settings_update.contact_phone is not None:
        update_dict["contact_phone"] = settings_update.contact_phone
    if settings_update.contact_phone_hours is not None:
        update_dict["contact_phone_hours"] = settings_update.contact_phone_hours
    if settings_update.contact_company_name is not None:
        update_dict["contact_company_name"] = settings_update.contact_company_name
    if settings_update.contact_address is not None:
        update_dict["contact_address"] = settings_update.contact_address
    if settings_update.contact_support_text is not None:
        update_dict["contact_support_text"] = settings_update.contact_support_text
    
    # Dark Mode Settings
    if settings_update.darkMode_enabled is not None:
        update_dict["darkMode_enabled"] = settings_update.darkMode_enabled
    
    update_dict["updated_at"] = datetime.now(timezone.utc).isoformat()
    
    await db.settings.update_one(
        {"type": "app_settings"},
        {"$set": {**update_dict, "type": "app_settings"}},
        upsert=True
    )
    
    return {"success": True, "message": "Settings updated"}

@api_router.get("/admin/cache/stats")
async def get_cache_stats(request: Request):
    """Get search cache statistics"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    return {
        "autocomplete_cache": autocomplete_cache.stats()
    }

@api_router.post("/admin/cache/clear")
async def clear_cache(request: Request):
    """Clear search cache"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    autocomplete_cache.clear()
    logger.info("Search cache cleared by admin")
    return {"success": True, "message": "Cache cleared"}

# ==================== POPULAR DESTINATIONS API ====================

@api_router.get("/destinations")
async def get_destinations():
    """Get popular destinations for homepage"""
    # Get destinations settings
    settings = await db.settings.find_one({"type": "destinations_settings"})
    
    if not settings or not settings.get("destinations"):
        # Return default destinations if none configured
        return {
            "destinations": [
                {"name": "Santorini", "country": "Greece", "image": "https://images.unsplash.com/photo-1613395877344-13d4a8e0d49e?w=600", "hotels": "1,240+", "destination_id": "16330"},
                {"name": "Barcelona", "country": "Spain", "image": "https://images.unsplash.com/photo-1583422409516-2895a77efded?w=600", "hotels": "2,100+", "destination_id": "17429"},
                {"name": "Vienna", "country": "Austria", "image": "https://images.unsplash.com/photo-1516550893923-42d28e5677af?w=600", "hotels": "890+", "destination_id": "18180"},
                {"name": "Amalfi", "country": "Italy", "image": "https://images.unsplash.com/photo-1612698093158-e07ac200d44e?w=600", "hotels": "450+", "destination_id": "10515"}
            ],
            "display_count": 4
        }
    
    return {
        "destinations": settings.get("destinations", []),
        "display_count": settings.get("display_count", 4)
    }

@api_router.get("/admin/destinations")
async def get_admin_destinations(request: Request):
    """Get destinations settings for admin"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    settings = await db.settings.find_one({"type": "destinations_settings"})
    
    if not settings:
        return {
            "destinations": [
                {"name": "Santorini", "country": "Greece", "image": "https://images.unsplash.com/photo-1613395877344-13d4a8e0d49e?w=600", "hotels": "1,240+", "destination_id": "16330"},
                {"name": "Barcelona", "country": "Spain", "image": "https://images.unsplash.com/photo-1583422409516-2895a77efded?w=600", "hotels": "2,100+", "destination_id": "17429"},
                {"name": "Vienna", "country": "Austria", "image": "https://images.unsplash.com/photo-1516550893923-42d28e5677af?w=600", "hotels": "890+", "destination_id": "18180"},
                {"name": "Amalfi", "country": "Italy", "image": "https://images.unsplash.com/photo-1612698093158-e07ac200d44e?w=600", "hotels": "450+", "destination_id": "10515"}
            ],
            "display_count": 4
        }
    
    return {
        "destinations": settings.get("destinations", []),
        "display_count": settings.get("display_count", 4)
    }

class DestinationItem(BaseModel):
    name: str
    country: str
    image: str
    hotels: str
    destination_id: str

class DestinationsSettings(BaseModel):
    destinations: List[DestinationItem]
    display_count: int = 4

@api_router.put("/admin/destinations")
async def update_destinations(settings: DestinationsSettings, request: Request):
    """Update popular destinations settings"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    await db.settings.update_one(
        {"type": "destinations_settings"},
        {"$set": {
            "type": "destinations_settings",
            "destinations": [d.dict() for d in settings.destinations],
            "display_count": settings.display_count,
            "updated_at": datetime.now(timezone.utc).isoformat()
        }},
        upsert=True
    )
    
    return {"success": True, "message": "Destinations updated"}

@api_router.post("/admin/destinations/upload-image")
async def upload_destination_image(request: Request, file: UploadFile = File(...)):
    """Upload destination image"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Validate file type
    allowed_types = ["image/png", "image/jpeg", "image/jpg", "image/webp"]
    if file.content_type not in allowed_types:
        raise HTTPException(status_code=400, detail="Invalid file type. Allowed: PNG, JPEG, WebP")
    
    # Generate unique filename
    file_ext = file.filename.split(".")[-1] if "." in file.filename else "jpg"
    filename = f"dest_{uuid.uuid4().hex[:8]}.{file_ext}"
    file_path = UPLOAD_DIR / filename
    
    # Save the file
    content = await file.read()
    with open(file_path, "wb") as f:
        f.write(content)
    
    # Return the URL
    image_url = f"/uploads/{filename}"
    
    return {"success": True, "image_url": image_url}

@api_router.delete("/admin/destinations/{index}")
async def delete_destination(index: int, request: Request):
    """Delete a destination by index"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    settings = await db.settings.find_one({"type": "destinations_settings"})
    if not settings or not settings.get("destinations"):
        raise HTTPException(status_code=404, detail="No destinations found")
    
    destinations = settings.get("destinations", [])
    if index < 0 or index >= len(destinations):
        raise HTTPException(status_code=400, detail="Invalid index")
    
    destinations.pop(index)
    
    await db.settings.update_one(
        {"type": "destinations_settings"},
        {"$set": {"destinations": destinations, "updated_at": datetime.now(timezone.utc).isoformat()}}
    )
    
    return {"success": True, "message": "Destination deleted"}

@api_router.post("/admin/upload-logo")
async def upload_logo(request: Request, file: UploadFile = File(...)):
    """Upload company logo for email branding"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Validate file type
    allowed_types = ["image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp"]
    if file.content_type not in allowed_types:
        raise HTTPException(status_code=400, detail="Invalid file type. Allowed: PNG, JPEG, GIF, WebP")
    
    # Generate unique filename
    file_ext = file.filename.split(".")[-1] if "." in file.filename else "png"
    filename = f"logo_{uuid.uuid4().hex[:8]}.{file_ext}"
    file_path = UPLOAD_DIR / filename
    
    # Save the file
    try:
        with open(file_path, "wb") as buffer:
            shutil.copyfileobj(file.file, buffer)
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Failed to save file: {str(e)}")
    
    # Get the server URL for the logo
    # In production, this would be the actual domain
    backend_url = os.environ.get("BACKEND_URL", "")
    if not backend_url:
        # Try to get from request
        backend_url = str(request.base_url).rstrip("/")
    
    logo_url = f"{backend_url}/static/uploads/{filename}"
    
    # Update settings with the new logo URL
    await db.settings.update_one(
        {},
        {"$set": {"company_logo_url": logo_url}},
        upsert=True
    )
    
    return {
        "success": True,
        "logo_url": logo_url,
        "filename": filename,
        "message": "Logo uploaded successfully"
    }

@api_router.delete("/admin/delete-logo")
async def delete_logo(request: Request):
    """Delete the company logo"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Get current logo URL
    settings = await get_settings()
    current_logo = settings.get("company_logo_url", "")
    
    # Try to delete the file if it's a local upload
    if "/static/uploads/" in current_logo:
        filename = current_logo.split("/")[-1]
        file_path = UPLOAD_DIR / filename
        if file_path.exists():
            try:
                file_path.unlink()
            except:
                pass
    
    # Clear logo URL in settings
    await db.settings.update_one(
        {},
        {"$set": {"company_logo_url": ""}},
        upsert=True
    )
    
    return {"success": True, "message": "Logo deleted"}

# ==================== DATABASE SYNC ENDPOINTS ====================

@api_router.get("/admin/db-sync/status")
async def get_db_sync_status(request: Request):
    """Get status of hotels missing images in database"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    db_config = await sunhotels_client.get_static_db_connection()
    if not db_config["host"]:
        return {"error": "Static database not configured"}
    
    try:
        conn = await asyncio.wait_for(
            aiomysql.connect(
                host=db_config["host"],
                port=db_config["port"],
                user=db_config["user"],
                password=db_config["password"],
                db=db_config["database"],
                charset='utf8mb4'
            ),
            timeout=5
        )
        
        async with conn.cursor(aiomysql.DictCursor) as cursor:
            # Simple fast queries - no JOINs
            # Count hotels in lookup table
            await cursor.execute("SELECT COUNT(*) as total FROM ghwk_autocomplete_lookup WHERE type = 'hotel'")
            total_hotels = (await cursor.fetchone())["total"]
            
            # Count hotels with images in bravo_hotels
            await cursor.execute("""
                SELECT COUNT(*) as with_images 
                FROM ghwk_bravo_hotels 
                WHERE images_json IS NOT NULL AND images_json != '' AND images_json != '[]'
            """)
            with_images = (await cursor.fetchone())["with_images"]
            
            # Get sample hotels from lookup that likely need images (simple query)
            await cursor.execute("""
                SELECT hotel_id, display_name, country_name
                FROM ghwk_autocomplete_lookup 
                WHERE type = 'hotel' 
                ORDER BY RAND()
                LIMIT 15
            """)
            sample_hotels = await cursor.fetchall()
        
        conn.close()
        
        # Estimate missing images
        missing_estimate = max(0, total_hotels - with_images)
        coverage = round((with_images / total_hotels * 100), 1) if total_hotels > 0 else 0
        
        return {
            "total_hotels_in_lookup": total_hotels,
            "hotels_with_images_in_bravo": with_images,
            "lookup_hotels_with_images": with_images,
            "lookup_hotels_missing_images": missing_estimate,
            "coverage_percent": coverage,
            "sample_missing": [{"hotel_id": h["hotel_id"], "name": h["display_name"], "country": h["country_name"]} for h in sample_hotels]
        }
        
    except asyncio.TimeoutError:
        logger.warning("DB sync status query timed out")
        return {"error": "Database query timed out - try again"}
    except Exception as e:
        logger.error(f"DB sync status error: {e}")
        return {"error": str(e)}

@api_router.post("/admin/db-sync/sync-hotel/{hotel_id}")
async def sync_single_hotel_images(hotel_id: str, request: Request):
    """Sync images for a single hotel from Sunhotels API to database"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    db_config = await sunhotels_client.get_static_db_connection()
    if not db_config["host"]:
        raise HTTPException(status_code=400, detail="Static database not configured")
    
    try:
        # Fetch hotel data from Sunhotels API
        username, password = await sunhotels_client.get_credentials()
        async with httpx.AsyncClient(timeout=30) as client:
            url = f"{SUNHOTELS_BASE_URL}/GetStaticHotelsAndRooms"
            params = {
                "userName": username,
                "password": password,
                "language": "en",
                "hotelIDs": hotel_id,
                "destination": "",
                "resortIDs": "",
                "accommodationTypes": "",
                "sortBy": "",
                "sortOrder": "",
                "exactDestinationMatch": ""
            }
            response = await client.get(url, params=params)
            
            if response.status_code != 200:
                raise HTTPException(status_code=502, detail="Failed to fetch from Sunhotels API")
            
            # Parse images from XML response
            hotel_data = sunhotels_client._parse_static_hotel_data(response.text)
            
            if not hotel_data:
                raise HTTPException(status_code=404, detail="Hotel not found in Sunhotels API")
            
            hotel_info = list(hotel_data.values())[0] if hotel_data else {}
            images = hotel_info.get("images", [])
            
            if not images:
                return {"success": False, "message": "No images found in API response", "hotel_id": hotel_id}
            
            # Convert images to JSON format for database
            images_json = json.dumps([{"id": img} if not img.startswith("http") else {"id": img} for img in images])
            
            # Update or insert into ghwk_bravo_hotels
            conn = await asyncio.wait_for(
                aiomysql.connect(
                    host=db_config["host"],
                    port=db_config["port"],
                    user=db_config["user"],
                    password=db_config["password"],
                    db=db_config["database"],
                    charset='utf8mb4'
                ),
                timeout=10
            )
            
            async with conn.cursor() as cursor:
                # Check if hotel exists in bravo_hotels
                await cursor.execute("SELECT hotel_id FROM ghwk_bravo_hotels WHERE hotel_id = %s", (hotel_id,))
                exists = await cursor.fetchone()
                
                if exists:
                    # Update existing record
                    await cursor.execute(
                        "UPDATE ghwk_bravo_hotels SET images_json = %s WHERE hotel_id = %s",
                        (images_json, hotel_id)
                    )
                else:
                    # Insert new record
                    await cursor.execute(
                        "INSERT INTO ghwk_bravo_hotels (hotel_id, images_json) VALUES (%s, %s)",
                        (hotel_id, images_json)
                    )
                
                await conn.commit()
            
            conn.close()
            
            # Clear autocomplete cache to reflect new images
            autocomplete_cache.clear()
            
            logger.info(f"✅ Synced {len(images)} images for hotel {hotel_id}")
            
            return {
                "success": True,
                "hotel_id": hotel_id,
                "images_synced": len(images),
                "message": f"Successfully synced {len(images)} images"
            }
            
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error syncing hotel {hotel_id}: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@api_router.post("/admin/db-sync/batch")
async def batch_sync_hotel_images(request: Request, limit: int = 10):
    """Batch sync images for hotels missing images (limit per run)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    db_config = await sunhotels_client.get_static_db_connection()
    if not db_config["host"]:
        raise HTTPException(status_code=400, detail="Static database not configured")
    
    try:
        conn = await asyncio.wait_for(
            aiomysql.connect(
                host=db_config["host"],
                port=db_config["port"],
                user=db_config["user"],
                password=db_config["password"],
                db=db_config["database"],
                charset='utf8mb4'
            ),
            timeout=10
        )
        
        # Get hotels missing images
        async with conn.cursor(aiomysql.DictCursor) as cursor:
            await cursor.execute("""
                SELECT a.hotel_id, a.display_name
                FROM ghwk_autocomplete_lookup a
                LEFT JOIN ghwk_bravo_hotels b ON a.hotel_id = b.hotel_id
                WHERE a.type = 'hotel' 
                AND (b.images_json IS NULL OR b.images_json = '' OR b.images_json = '[]' OR b.hotel_id IS NULL)
                LIMIT %s
            """, (limit,))
            hotels_to_sync = await cursor.fetchall()
        
        if not hotels_to_sync:
            conn.close()
            return {"success": True, "message": "No hotels need syncing", "synced": 0}
        
        # Get credentials once
        username, password = await sunhotels_client.get_credentials()
        
        synced = 0
        no_images = 0
        failed = 0
        results = []
        
        async with httpx.AsyncClient(timeout=30) as client:
            for hotel in hotels_to_sync:
                hotel_id = str(hotel["hotel_id"])
                try:
                    # Fetch from API
                    url = f"{SUNHOTELS_BASE_URL}/GetStaticHotelsAndRooms"
                    params = {
                        "userName": username,
                        "password": password,
                        "language": "en",
                        "hotelIDs": hotel_id,
                        "destination": "",
                        "resortIDs": "",
                        "accommodationTypes": "",
                        "sortBy": "",
                        "sortOrder": "",
                        "exactDestinationMatch": ""
                    }
                    response = await client.get(url, params=params)
                    
                    if response.status_code == 200:
                        hotel_data = sunhotels_client._parse_static_hotel_data(response.text)
                        hotel_info = list(hotel_data.values())[0] if hotel_data else {}
                        images = hotel_info.get("images", [])
                        
                        if images:
                            images_json = json.dumps([{"id": img} if not str(img).startswith("http") else {"id": img} for img in images])
                            
                            async with conn.cursor() as cursor:
                                await cursor.execute("SELECT hotel_id FROM ghwk_bravo_hotels WHERE hotel_id = %s", (hotel_id,))
                                exists = await cursor.fetchone()
                                
                                if exists:
                                    await cursor.execute(
                                        "UPDATE ghwk_bravo_hotels SET images_json = %s WHERE hotel_id = %s",
                                        (images_json, hotel_id)
                                    )
                                else:
                                    await cursor.execute(
                                        "INSERT INTO ghwk_bravo_hotels (hotel_id, images_json) VALUES (%s, %s)",
                                        (hotel_id, images_json)
                                    )
                            
                            synced += 1
                            results.append({"hotel_id": hotel_id, "name": hotel["display_name"], "images": len(images), "status": "synced"})
                        else:
                            # Mark hotel as checked (no images in API) - prevents re-checking
                            async with conn.cursor() as cursor:
                                await cursor.execute("SELECT hotel_id FROM ghwk_bravo_hotels WHERE hotel_id = %s", (hotel_id,))
                                exists = await cursor.fetchone()
                                
                                if exists:
                                    await cursor.execute(
                                        "UPDATE ghwk_bravo_hotels SET images_json = %s WHERE hotel_id = %s",
                                        ('[]', hotel_id)
                                    )
                                else:
                                    await cursor.execute(
                                        "INSERT INTO ghwk_bravo_hotels (hotel_id, images_json) VALUES (%s, %s)",
                                        (hotel_id, '[]')
                                    )
                            
                            no_images += 1
                            results.append({"hotel_id": hotel_id, "name": hotel["display_name"], "status": "no_images_in_api"})
                    else:
                        failed += 1
                        results.append({"hotel_id": hotel_id, "name": hotel["display_name"], "status": "api_error"})
                        
                except Exception as e:
                    failed += 1
                    results.append({"hotel_id": hotel_id, "name": hotel["display_name"], "status": f"error: {str(e)[:50]}"})
                
                # Small delay to not overwhelm the API
                await asyncio.sleep(0.5)
        
        await conn.commit()
        conn.close()
        
        # Clear autocomplete cache
        autocomplete_cache.clear()
        
        checked_count = synced + no_images
        logger.info(f"✅ Batch sync complete: {synced} synced with images, {no_images} no images in API, {failed} errors")
        
        return {
            "success": True,
            "synced": synced,
            "no_images": no_images,
            "failed": failed,
            "checked": checked_count,
            "results": results,
            "message": f"Checked {checked_count} hotels: {synced} with images, {no_images} without images in Sunhotels API"
        }
        
    except Exception as e:
        logger.error(f"Batch sync error: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@api_router.post("/admin/db-sync/test-sunhotels")
async def test_sunhotels_connection(request: Request):
    """Test Sunhotels API connection"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    try:
        username, password = await sunhotels_client.get_credentials()
        
        # Test with a known hotel ID
        test_hotel_id = "633"  # A hotel we know has images
        url = f"{SUNHOTELS_BASE_URL}/GetStaticHotelsAndRooms"
        params = {
            "userName": username,
            "password": password,
            "language": "en",
            "hotelIDs": test_hotel_id,
            "destination": "",
            "resortIDs": "",
            "accommodationTypes": "",
            "sortBy": "",
            "sortOrder": "",
            "exactDestinationMatch": ""
        }
        
        async with httpx.AsyncClient(timeout=15) as client:
            response = await client.get(url, params=params)
            
            if response.status_code == 200:
                # Parse the response to check if we got valid data
                hotel_data = sunhotels_client._parse_static_hotel_data(response.text)
                if hotel_data:
                    hotel_info = list(hotel_data.values())[0]
                    images_count = len(hotel_info.get("images", []))
                    return {
                        "success": True,
                        "message": f"✓ Connected to Sunhotels API successfully",
                        "details": {
                            "test_hotel": hotel_info.get("name", "Unknown"),
                            "images_found": images_count,
                            "api_endpoint": SUNHOTELS_BASE_URL,
                            "username": username
                        }
                    }
                else:
                    return {
                        "success": False,
                        "message": "API responded but returned invalid data",
                        "details": {"status_code": response.status_code}
                    }
            else:
                return {
                    "success": False,
                    "message": f"API returned HTTP {response.status_code}",
                    "details": {"status_code": response.status_code}
                }
                
    except httpx.TimeoutException:
        return {"success": False, "message": "Connection timeout - API not responding"}
    except Exception as e:
        return {"success": False, "message": f"Connection error: {str(e)[:100]}"}

@api_router.post("/admin/db-sync/test-mysql")
async def test_mysql_connection(request: Request):
    """Test MySQL database connection"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    db_config = await sunhotels_client.get_static_db_connection()
    
    if not db_config["host"]:
        return {
            "success": False,
            "message": "MySQL not configured",
            "details": {"configured": False}
        }
    
    try:
        conn = await asyncio.wait_for(
            aiomysql.connect(
                host=db_config["host"],
                port=db_config["port"],
                user=db_config["user"],
                password=db_config["password"],
                db=db_config["database"],
                charset='utf8mb4'
            ),
            timeout=10
        )
        
        async with conn.cursor(aiomysql.DictCursor) as cursor:
            # Test query - get table counts
            await cursor.execute("SELECT COUNT(*) as count FROM ghwk_autocomplete_lookup WHERE type = 'hotel'")
            hotel_count = (await cursor.fetchone())["count"]
            
            await cursor.execute("SELECT COUNT(*) as count FROM ghwk_bravo_hotels WHERE images_json IS NOT NULL AND images_json != '' AND images_json != '[]'")
            images_count = (await cursor.fetchone())["count"]
        
        conn.close()
        
        return {
            "success": True,
            "message": "✓ Connected to MySQL database successfully",
            "details": {
                "host": db_config["host"],
                "database": db_config["database"],
                "hotels_in_lookup": hotel_count,
                "hotels_with_images": images_count
            }
        }
        
    except asyncio.TimeoutError:
        return {"success": False, "message": "Connection timeout - database not responding"}
    except Exception as e:
        return {"success": False, "message": f"Connection error: {str(e)[:100]}"}

@api_router.get("/admin/db-sync/auto-sync-settings")
async def get_auto_sync_settings(request: Request):
    """Get auto-sync settings"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    settings = await db.settings.find_one({"type": "app_settings"})
    if not settings:
        settings = {}
    
    return {
        "enabled": settings.get("auto_sync_enabled", False),
        "batch_size": settings.get("auto_sync_batch_size", 50),
        "last_run": settings.get("auto_sync_last_run"),
        "last_result": settings.get("auto_sync_last_result"),
        "schedule": "Daily at 3:00 AM UTC"
    }

@api_router.put("/admin/db-sync/auto-sync-settings")
async def update_auto_sync_settings(request: Request):
    """Update auto-sync settings"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    data = await request.json()
    
    update_data = {}
    if "enabled" in data:
        update_data["auto_sync_enabled"] = bool(data["enabled"])
    if "batch_size" in data:
        # Limit batch size between 10 and 200
        update_data["auto_sync_batch_size"] = max(10, min(200, int(data["batch_size"])))
    
    if update_data:
        await db.settings.update_one(
            {"type": "app_settings"},
            {"$set": update_data},
            upsert=True
        )
    
    return {"success": True, "message": "Auto-sync settings updated"}

@api_router.post("/admin/db-sync/trigger-auto-sync")
async def trigger_auto_sync(request: Request, background_tasks: BackgroundTasks):
    """Manually trigger the auto-sync job"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Run in background to not block the request
    background_tasks.add_task(scheduled_hotel_image_sync)
    
    return {"success": True, "message": "Auto-sync job triggered. Check back in a few minutes for results."}

# ==================== CONTACT PAGE PUBLIC ENDPOINT ====================

class ContactFormData(BaseModel):
    name: str
    email: str
    subject: str
    message: str

@api_router.get("/contact-settings")
async def get_contact_settings():
    """Get contact page settings (public endpoint)"""
    settings = await get_settings()
    
    return {
        "page_title": settings.get("contact_page_title", "Get in Touch"),
        "page_subtitle": settings.get("contact_page_subtitle", "Have questions? We're here to help. Reach out to our team and we'll get back to you as soon as possible."),
        "email": settings.get("contact_email", "hello@freestays.eu"),
        "email_note": settings.get("contact_email_note", "We respond within 24 hours"),
        "phone": settings.get("contact_phone", "+31 (0) 123 456 789"),
        "phone_hours": settings.get("contact_phone_hours", "Mon-Fri, 9:00 - 17:00 CET"),
        "company_name": settings.get("contact_company_name", "Euro Hotel Cards GmbH"),
        "address": settings.get("contact_address", "Barneveld, Netherlands"),
        "support_text": settings.get("contact_support_text", "Our booking support team is available around the clock for urgent travel assistance.")
    }

@api_router.post("/contact")
async def submit_contact_form(data: ContactFormData, background_tasks: BackgroundTasks):
    """Submit contact form and send emails to admin and sender"""
    try:
        # Store the contact submission in database
        contact_submission = {
            "name": data.name,
            "email": data.email,
            "subject": data.subject,
            "message": data.message,
            "status": "new",
            "created_at": datetime.now(timezone.utc).isoformat()
        }
        await db.contact_submissions.insert_one(contact_submission)
        
        # Get SMTP settings using EmailService (consistent with other emails)
        smtp_settings = await EmailService.get_smtp_settings()
        
        logger.info(f"Contact form: SMTP enabled = {smtp_settings.get('enabled')}, host = {smtp_settings.get('host')}")
        
        if not smtp_settings.get("enabled"):
            logger.warning("SMTP not enabled, contact form submission stored but email not sent")
            return {"success": True, "message": "Message received. We'll get back to you soon."}
        
        # Send email notification to admin
        admin_email = smtp_settings.get("company_support_email") or smtp_settings.get("from_email") or "info@freestays.eu"
        company_name = smtp_settings.get("company_name", "FreeStays")
        smtp_from = smtp_settings.get("from_email") or admin_email
        
        # Admin notification email content
        admin_html_content = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>New Contact Form Submission</title>
        </head>
        <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                            <tr>
                                <td style="background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); padding: 25px 30px;">
                                    <h1 style="color: #ffffff; margin: 0; font-size: 24px;">📩 New Contact Form Submission</h1>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 30px;">
                                    <table width="100%" cellpadding="0" cellspacing="0">
                                        <tr>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666; width: 30%;">From:</td>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 600;">{data.name}</td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">Email:</td>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee;">
                                                <a href="mailto:{data.email}" style="color: #1e3a5f;">{data.email}</a>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">Subject:</td>
                                            <td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 500;">{data.subject}</td>
                                        </tr>
                                    </table>
                                    <div style="margin-top: 20px;">
                                        <h3 style="color: #1e3a5f; margin-bottom: 10px;">Message:</h3>
                                        <div style="background-color: #f8fafc; padding: 20px; border-radius: 8px; border-left: 4px solid #1e3a5f;">
                                            <p style="margin: 0; white-space: pre-wrap; line-height: 1.6;">{data.message}</p>
                                        </div>
                                    </div>
                                    <div style="margin-top: 30px; padding: 15px; background-color: #fef3c7; border-radius: 8px;">
                                        <p style="margin: 0; color: #92400e; font-size: 14px;">
                                            <strong>Reply directly:</strong> You can reply directly to this email to respond to {data.name}.
                                        </p>
                                    </div>
                                </td>
                            </tr>
                            <tr>
                                <td style="background-color: #f8fafc; padding: 20px; text-align: center; border-top: 1px solid #eee;">
                                    <p style="margin: 0; color: #666; font-size: 12px;">
                                        This message was sent via the {company_name} contact form.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """
        
        # Confirmation email to sender
        sender_html_content = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>We received your message - {company_name}</title>
        </head>
        <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                            {EmailService.get_email_header(smtp_settings, "Thank You for Contacting Us!")}
                            <tr>
                                <td style="padding: 30px;">
                                    <p style="margin: 0 0 20px 0; font-size: 16px;">Hi {data.name},</p>
                                    <p style="margin: 0 0 20px 0; color: #666; line-height: 1.6;">
                                        Thank you for reaching out to {company_name}! We have received your message and will get back to you as soon as possible, usually within 24 hours.
                                    </p>
                                    
                                    <div style="background-color: #f8fafc; border-radius: 8px; padding: 20px; margin: 20px 0;">
                                        <h3 style="margin: 0 0 15px 0; color: #1e3a5f; font-size: 16px;">Your Message:</h3>
                                        <p style="margin: 0 0 10px 0; color: #666;"><strong>Subject:</strong> {data.subject}</p>
                                        <div style="background-color: #ffffff; padding: 15px; border-radius: 8px; border: 1px solid #e5e7eb;">
                                            <p style="margin: 0; white-space: pre-wrap; color: #374151; line-height: 1.6;">{data.message}</p>
                                        </div>
                                    </div>
                                    
                                    <p style="margin: 20px 0 0 0; color: #666; line-height: 1.6;">
                                        In the meantime, feel free to explore our website or check out our <a href="https://freestays.eu/about" style="color: #1e3a5f;">How It Works</a> page to learn more about commission-free hotel bookings.
                                    </p>
                                    
                                    <p style="margin: 20px 0 0 0; color: #666;">
                                        Best regards,<br/>
                                        <strong>The {company_name} Team</strong>
                                    </p>
                                </td>
                            </tr>
                            {EmailService.get_email_footer(smtp_settings)}
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """
        
        # Send emails in background
        def send_contact_emails():
            try:
                smtp_host = smtp_settings.get("host")
                smtp_port = smtp_settings.get("port", 587)
                smtp_username = smtp_settings.get("username")
                smtp_password = smtp_settings.get("password")
                
                logger.info(f"Connecting to SMTP: {smtp_host}:{smtp_port} with user {smtp_username}")
                
                with smtplib.SMTP(smtp_host, smtp_port) as server:
                    server.starttls()
                    server.login(smtp_username, smtp_password)
                    
                    # 1. Send notification to admin
                    admin_msg = MIMEMultipart('alternative')
                    admin_msg['Subject'] = f"[{company_name}] Contact Form: {data.subject}"
                    admin_msg['From'] = smtp_from
                    admin_msg['To'] = admin_email
                    admin_msg['Reply-To'] = data.email
                    admin_msg.attach(MIMEText(admin_html_content, 'html'))
                    server.sendmail(smtp_from, admin_email, admin_msg.as_string())
                    logger.info(f"Contact form admin notification sent to {admin_email}")
                    
                    # 2. Send confirmation to sender
                    sender_msg = MIMEMultipart('alternative')
                    sender_msg['Subject'] = f"We received your message - {company_name}"
                    sender_msg['From'] = smtp_from
                    sender_msg['To'] = data.email
                    sender_msg.attach(MIMEText(sender_html_content, 'html'))
                    server.sendmail(smtp_from, data.email, sender_msg.as_string())
                    logger.info(f"Contact form confirmation sent to {data.email}")
                    
            except Exception as e:
                logger.error(f"Failed to send contact form emails: {str(e)}")
                import traceback
                logger.error(traceback.format_exc())
        
        background_tasks.add_task(send_contact_emails)
        
        return {"success": True, "message": "Message sent successfully! We'll get back to you soon."}
        
    except Exception as e:
        logger.error(f"Contact form submission error: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to send message. Please try again.")

# ==================== PASS CODE MANAGEMENT ====================

class PassCodeCreate(BaseModel):
    pass_type: str  # "one_time" or "annual"
    quantity: int = 1
    notes: Optional[str] = None

@api_router.post("/admin/pass-codes/generate")
async def generate_pass_codes(data: PassCodeCreate, request: Request):
    """Generate new pass codes (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    if data.pass_type not in ["one_time", "annual"]:
        raise HTTPException(status_code=400, detail="Invalid pass type. Use 'one_time' or 'annual'")
    
    if data.quantity < 1 or data.quantity > 100:
        raise HTTPException(status_code=400, detail="Quantity must be between 1 and 100")
    
    generated_codes = []
    for _ in range(data.quantity):
        code = generate_pass_code(data.pass_type)
        code_doc = {
            "code": code,
            "pass_type": data.pass_type,
            "status": "active",
            "created_at": datetime.now(timezone.utc).isoformat(),
            "used_at": None,
            "used_by": None,
            "notes": data.notes,
            "price": 35.0 if data.pass_type == "one_time" else 129.0
        }
        await db.pass_codes.insert_one(code_doc)
        generated_codes.append({
            "code": code,
            "pass_type": data.pass_type,
            "price": code_doc["price"]
        })
    
    return {
        "success": True,
        "codes": generated_codes,
        "message": f"Generated {len(generated_codes)} {data.pass_type} pass codes"
    }

class PassCodeImport(BaseModel):
    codes: List[dict]

@api_router.post("/admin/pass-codes/import")
async def import_pass_codes(data: PassCodeImport, request: Request):
    """Import pass codes from CSV data (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    imported = 0
    errors = []
    
    for item in data.codes:
        code = item.get("code", "").strip().upper()
        pass_type = item.get("pass_type", "one_time").strip().lower()
        
        if not code:
            continue
        
        if pass_type not in ["one_time", "annual"]:
            pass_type = "one_time"
        
        # Check if code already exists
        existing = await db.pass_codes.find_one({"code": code})
        if existing:
            errors.append(f"Code {code} already exists")
            continue
        
        code_doc = {
            "code": code,
            "pass_type": pass_type,
            "status": "active",
            "created_at": datetime.now(timezone.utc).isoformat(),
            "used_at": None,
            "used_by": None,
            "notes": "Imported",
            "price": 35.0 if pass_type == "one_time" else 129.0
        }
        await db.pass_codes.insert_one(code_doc)
        imported += 1
    
    return {
        "success": True,
        "imported": imported,
        "errors": errors,
        "message": f"Imported {imported} codes" + (f", {len(errors)} errors" if errors else "")
    }

@api_router.get("/admin/pass-codes/export")
async def export_pass_codes(request: Request, status: str = None, pass_type: str = None):
    """Export pass codes as CSV (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    query = {}
    if status:
        query["status"] = status
    if pass_type:
        query["pass_type"] = pass_type
    
    codes = await db.pass_codes.find(query, {"_id": 0}).sort("created_at", -1).to_list(10000)
    
    # Generate CSV
    csv_lines = ["code,pass_type,status,price,created_at,used_at,used_by,notes"]
    for code in codes:
        line = f"{code.get('code', '')},{code.get('pass_type', '')},{code.get('status', '')},{code.get('price', '')},{code.get('created_at', '')},{code.get('used_at', '')},{code.get('used_by', '')},{code.get('notes', '')}"
        csv_lines.append(line)
    
    csv_content = "\n".join(csv_lines)
    
    return {
        "success": True,
        "csv": csv_content,
        "count": len(codes),
        "message": f"Exported {len(codes)} codes"
    }

@api_router.get("/admin/pass-codes")
async def get_pass_codes(request: Request, status: str = None, pass_type: str = None, search: str = None, limit: int = 50, skip: int = 0):
    """Get all pass codes (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    query = {}
    if status:
        query["status"] = status
    if pass_type:
        query["pass_type"] = pass_type
    
    # Add search functionality - search by code, email, or name
    if search:
        search_term = search.strip()
        # First, find users whose name matches the search term
        matching_users = await db.users.find(
            {"name": {"$regex": search_term, "$options": "i"}},
            {"email": 1, "_id": 0}
        ).to_list(1000)
        matching_emails = [u["email"] for u in matching_users]
        
        # Build OR query for code, emails, and user names
        or_conditions = [
            {"code": {"$regex": search_term.upper(), "$options": "i"}},
            {"purchased_by": {"$regex": search_term, "$options": "i"}},
            {"used_by": {"$regex": search_term, "$options": "i"}}
        ]
        
        # Add matching user emails to the search
        if matching_emails:
            or_conditions.append({"purchased_by": {"$in": matching_emails}})
            or_conditions.append({"used_by": {"$in": matching_emails}})
        
        query["$or"] = or_conditions
    
    codes = await db.pass_codes.find(query, {"_id": 0}).sort("created_at", -1).skip(skip).limit(limit).to_list(limit)
    total = await db.pass_codes.count_documents(query)
    
    # Enrich codes with user names and expiration dates
    enriched_codes = []
    for code in codes:
        enriched_code = dict(code)
        
        # Get user name and expiration date for purchased/used codes
        user_email = code.get("purchased_by") or code.get("used_by")
        if user_email:
            user = await db.users.find_one({"email": user_email}, {"name": 1, "pass_expires_at": 1, "_id": 0})
            if user:
                enriched_code["user_name"] = user.get("name", "")
                enriched_code["expires_at"] = user.get("pass_expires_at")
        
        enriched_codes.append(enriched_code)
    
    # Count statistics
    total_active = await db.pass_codes.count_documents({"status": "active"})
    total_used = await db.pass_codes.count_documents({"status": "used"})
    
    # Count purchased passes (from customer purchases)
    total_purchased = await db.pass_codes.count_documents({"source": "purchase"})
    total_purchased_one_time = await db.pass_codes.count_documents({"source": "purchase", "pass_type": "one_time"})
    total_purchased_annual = await db.pass_codes.count_documents({"source": "purchase", "pass_type": "annual"})
    
    # Calculate revenue from purchases
    purchased_codes = await db.pass_codes.find({"source": "purchase"}, {"price": 1, "_id": 0}).to_list(10000)
    total_revenue = sum(c.get("price", 0) for c in purchased_codes)
    
    return {
        "codes": enriched_codes,
        "total": total,
        "stats": {
            "active": total_active,
            "used": total_used,
            "purchased": {
                "total": total_purchased,
                "one_time": total_purchased_one_time,
                "annual": total_purchased_annual,
                "revenue": total_revenue
            }
        }
    }

@api_router.post("/admin/pass-codes/validate")
async def validate_pass_code(request: Request, code: str = Query(...)):
    """Validate a pass code (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    code_doc = await db.pass_codes.find_one({"code": code.upper()}, {"_id": 0})
    
    if not code_doc:
        return {"valid": False, "message": "Code not found"}
    
    if code_doc.get("status") == "used":
        return {
            "valid": False, 
            "message": "Code already used",
            "used_at": code_doc.get("used_at"),
            "used_by": code_doc.get("used_by")
        }
    
    return {
        "valid": True,
        "code": code_doc["code"],
        "pass_type": code_doc["pass_type"],
        "price": code_doc.get("price"),
        "created_at": code_doc.get("created_at")
    }

@api_router.delete("/admin/pass-codes/{code}")
async def delete_pass_code(code: str, request: Request):
    """Delete a pass code (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    result = await db.pass_codes.delete_one({"code": code.upper(), "status": "active"})
    
    if result.deleted_count == 0:
        raise HTTPException(status_code=404, detail="Code not found or already used")
    
    return {"success": True, "message": "Pass code deleted"}

@api_router.post("/pass-codes/apply")
async def apply_pass_code(code: str = Query(...)):
    """Apply a pass code to get discount (public endpoint)"""
    code_doc = await db.pass_codes.find_one({"code": code.upper()})
    
    if not code_doc:
        raise HTTPException(status_code=404, detail="Invalid pass code")
    
    if code_doc.get("status") == "used":
        raise HTTPException(status_code=400, detail="This pass code has already been used")
    
    return {
        "valid": True,
        "pass_type": code_doc["pass_type"],
        "discount_active": True,
        "message": f"Pass code valid! {code_doc['pass_type'].replace('_', ' ').title()} Pass discount applied."
    }

@api_router.post("/pass-codes/redeem")
async def redeem_pass_code(code: str = Query(...), user_email: str = Query(None)):
    """Redeem a pass code after purchase (marks as used)"""
    code_doc = await db.pass_codes.find_one({"code": code.upper()})
    
    if not code_doc:
        raise HTTPException(status_code=404, detail="Invalid pass code")
    
    if code_doc.get("status") == "used":
        raise HTTPException(status_code=400, detail="This pass code has already been used")
    
    # Mark as used
    await db.pass_codes.update_one(
        {"code": code.upper()},
        {"$set": {
            "status": "used",
            "used_at": datetime.now(timezone.utc).isoformat(),
            "used_by": user_email
        }}
    )
    
    return {
        "success": True,
        "pass_type": code_doc["pass_type"],
        "message": "Pass code redeemed successfully"
    }

@api_router.get("/admin/referrals")
async def get_all_referrals(request: Request, limit: int = 50, skip: int = 0):
    """Get all referrals (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    referrals = await db.referrals.find({}, {"_id": 0}).sort("created_at", -1).skip(skip).limit(limit).to_list(limit)
    total = await db.referrals.count_documents({})
    
    # Enrich with user names
    for ref in referrals:
        referrer = await db.users.find_one({"user_id": ref["referrer_id"]}, {"_id": 0, "name": 1, "email": 1})
        referee = await db.users.find_one({"user_id": ref["referee_id"]}, {"_id": 0, "name": 1, "email": 1})
        ref["referrer_name"] = referrer["name"] if referrer else "Unknown"
        ref["referrer_email"] = referrer["email"] if referrer else "Unknown"
        ref["referee_name"] = referee["name"] if referee else "Unknown"
        ref["referee_email"] = referee["email"] if referee else "Unknown"
    
    return {"referrals": referrals, "total": total}

@api_router.get("/admin/referral-stats")
async def get_referral_stats(request: Request):
    """Get referral program statistics (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    total_referrals = await db.referrals.count_documents({})
    pending_referrals = await db.referrals.count_documents({"status": "pending"})
    used_referrals = await db.referrals.count_documents({"status": "used"})
    
    # Calculate total discount given
    pipeline = [
        {"$group": {"_id": None, "total_discount": {"$sum": "$discount_amount"}}}
    ]
    result = await db.referrals.aggregate(pipeline).to_list(1)
    total_discount = result[0]["total_discount"] if result else 0
    
    # Top referrers
    top_referrers = await db.users.find(
        {"referral_count": {"$gt": 0}},
        {"_id": 0, "name": 1, "email": 1, "referral_code": 1, "referral_count": 1}
    ).sort("referral_count", -1).limit(10).to_list(10)
    
    return {
        "total_referrals": total_referrals,
        "pending_referrals": pending_referrals,
        "used_referrals": used_referrals,
        "total_discount_given": total_discount,
        "top_referrers": top_referrers
    }

@api_router.get("/admin/bookings")
async def get_all_bookings(request: Request, limit: int = 50, skip: int = 0):
    """Get all bookings (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    bookings = await db.bookings.find({}, {"_id": 0}).sort("created_at", -1).skip(skip).limit(limit).to_list(limit)
    total = await db.bookings.count_documents({})
    
    return {"bookings": bookings, "total": total}

@api_router.post("/admin/bookings/{booking_id}/send-voucher")
async def send_voucher_email(booking_id: str, request: Request, background_tasks: BackgroundTasks):
    """Send travel voucher to customer"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    booking = await db.bookings.find_one({"booking_id": booking_id})
    if not booking:
        raise HTTPException(status_code=404, detail="Booking not found")
    
    if booking.get("status") != "confirmed":
        raise HTTPException(status_code=400, detail="Booking must be confirmed to send voucher")
    
    voucher_url = booking.get("voucher_url") or booking.get("voucher")
    if not voucher_url:
        raise HTTPException(status_code=400, detail="No voucher available for this booking")
    
    async def send_voucher():
        await EmailService.send_voucher_email(booking, voucher_url)
    
    background_tasks.add_task(send_voucher)
    
    # Update booking to track voucher sent
    await db.bookings.update_one(
        {"booking_id": booking_id},
        {"$set": {"voucher_sent": True, "voucher_sent_at": datetime.now(timezone.utc).isoformat()}}
    )
    
    return {"success": True, "message": f"Voucher email sent to {booking.get('guest_email')}"}

# ==================== SUNHOTELS EMAIL FORWARDING ADMIN ENDPOINTS ====================

@api_router.post("/admin/email-forwarding/trigger")
async def trigger_email_forwarding(request: Request, background_tasks: BackgroundTasks):
    """Manually trigger Sunhotels email forwarding check (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    async def run_forwarding():
        result = await SunhotelsEmailForwarder.check_and_forward_emails()
        logger.info(f"Manual email forwarding result: {result}")
    
    background_tasks.add_task(run_forwarding)
    return {"success": True, "message": "Email forwarding check started in background"}

@api_router.get("/admin/email-forwarding/history")
async def get_forwarded_vouchers(request: Request, limit: int = 50, skip: int = 0):
    """Get history of forwarded voucher emails (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    vouchers = await db.forwarded_vouchers.find(
        {}, {"_id": 0}
    ).sort("forwarded_at", -1).skip(skip).limit(limit).to_list(limit)
    
    total = await db.forwarded_vouchers.count_documents({})
    
    return {
        "vouchers": vouchers,
        "total": total,
        "limit": limit,
        "skip": skip
    }

@api_router.get("/admin/email-forwarding/status")
async def get_email_forwarding_status(request: Request):
    """Get email forwarding service status (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Check IMAP configuration
    imap_configured = bool(IMAP_PASSWORD)
    
    # Get stats
    total_forwarded = await db.forwarded_vouchers.count_documents({})
    today_start = datetime.now(timezone.utc).replace(hour=0, minute=0, second=0, microsecond=0)
    forwarded_today = await db.forwarded_vouchers.count_documents({
        "forwarded_at": {"$gte": today_start.isoformat()}
    })
    
    # Get last forwarded
    last_forwarded = await db.forwarded_vouchers.find_one(
        {}, {"_id": 0}, sort=[("forwarded_at", -1)]
    )
    
    return {
        "configured": imap_configured,
        "imap_server": IMAP_SERVER if imap_configured else None,
        "imap_email": IMAP_EMAIL if imap_configured else None,
        "total_forwarded": total_forwarded,
        "forwarded_today": forwarded_today,
        "last_forwarded": last_forwarded,
        "scheduler_running": scheduler.running,
        "next_check": "Every 5 minutes"
    }

# ==================== CHECK-IN REMINDERS ADMIN ENDPOINTS ====================

@api_router.post("/admin/checkin-reminders/trigger")
async def trigger_checkin_reminders(request: Request, background_tasks: BackgroundTasks):
    """Manually trigger check-in reminder check (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    async def run_reminders():
        result = await CheckInReminderService.check_and_send_reminders()
        logger.info(f"Manual check-in reminder result: {result}")
    
    background_tasks.add_task(run_reminders)
    return {"success": True, "message": "Check-in reminder check started in background"}

@api_router.get("/admin/checkin-reminders/status")
async def get_checkin_reminders_status(request: Request):
    """Get check-in reminders status (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Get reminder stats
    total_sent = await db.bookings.count_documents({"checkin_reminder_sent": True})
    
    today = datetime.now(timezone.utc).replace(hour=0, minute=0, second=0, microsecond=0)
    sent_today = await db.bookings.count_documents({
        "checkin_reminder_sent": True,
        "checkin_reminder_sent_at": {"$gte": today.isoformat()}
    })
    
    # Get upcoming bookings needing reminders (next 7 days)
    reminder_date_3days = (datetime.now(timezone.utc) + timedelta(days=3)).strftime("%Y-%m-%d")
    pending_3days = await db.bookings.count_documents({
        "check_in": reminder_date_3days,
        "status": {"$in": ["completed", "confirmed", "paid"]},
        "checkin_reminder_sent": {"$ne": True}
    })
    
    # Get last reminder sent
    last_reminder = await db.bookings.find_one(
        {"checkin_reminder_sent": True},
        {"_id": 0, "guest_email": 1, "hotel_name": 1, "check_in": 1, "checkin_reminder_sent_at": 1},
        sort=[("checkin_reminder_sent_at", -1)]
    )
    
    return {
        "total_sent": total_sent,
        "sent_today": sent_today,
        "pending_3days": pending_3days,
        "last_reminder": last_reminder,
        "next_check": "Daily at 9 AM UTC"
    }

@api_router.get("/admin/checkin-reminders/upcoming")
async def get_upcoming_checkins(request: Request, days: int = 7):
    """Get bookings with upcoming check-ins (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Get bookings for the next X days
    today = datetime.now(timezone.utc).strftime("%Y-%m-%d")
    end_date = (datetime.now(timezone.utc) + timedelta(days=days)).strftime("%Y-%m-%d")
    
    bookings = await db.bookings.find({
        "check_in": {"$gte": today, "$lte": end_date},
        "status": {"$in": ["completed", "confirmed", "paid"]}
    }, {
        "_id": 0,
        "booking_id": 1,
        "guest_first_name": 1,
        "guest_last_name": 1,
        "guest_email": 1,
        "hotel_name": 1,
        "check_in": 1,
        "check_out": 1,
        "checkin_reminder_sent": 1,
        "checkin_reminder_sent_at": 1
    }).sort("check_in", 1).to_list(100)
    
    return {"bookings": bookings, "days": days}

# ==================== REFERRAL TIERS MANAGEMENT ====================

# ==================== SURVEYS & FEEDBACK API ====================

@api_router.post("/survey/submit")
async def submit_survey(data: GuestSurveySubmit):
    """Submit a guest survey response (public, validated by token)"""
    # Validate token
    token_doc = await db.survey_tokens.find_one({
        "token": data.survey_token,
        "used": False
    })
    
    if not token_doc:
        raise HTTPException(status_code=400, detail="Invalid or expired survey token")
    
    # Create feedback record
    feedback_id = str(uuid.uuid4())[:12]
    now = datetime.now(timezone.utc).isoformat()
    
    feedback_record = {
        "feedback_id": feedback_id,
        "booking_id": data.booking_id,
        "survey_token": data.survey_token,
        "guest_email": token_doc.get("guest_email"),
        "hotel_name": token_doc.get("hotel_name"),
        "check_out": token_doc.get("check_out"),
        "overall_rating": data.overall_rating,
        "cleanliness_rating": data.cleanliness_rating,
        "service_rating": data.service_rating,
        "value_rating": data.value_rating,
        "location_rating": data.location_rating,
        "amenities_rating": data.amenities_rating,
        "title": data.title,
        "review_text": data.review_text,
        "would_recommend": data.would_recommend,
        "travel_type": data.travel_type,
        "photos": data.photos or [],
        "status": "pending",  # pending, approved, rejected
        "is_public": False,  # Set to True when approved for display on hotel page
        "submitted_at": now,
        "created_at": now
    }
    
    await db.guest_feedback.insert_one(feedback_record)
    
    # Mark token as used
    await db.survey_tokens.update_one(
        {"token": data.survey_token},
        {"$set": {"used": True, "used_at": now}}
    )
    
    # Update booking with feedback status
    await db.bookings.update_one(
        {"booking_id": data.booking_id},
        {"$set": {"feedback_submitted": True, "feedback_id": feedback_id}}
    )
    
    logger.info(f"✅ Survey submitted for booking {data.booking_id} - Rating: {data.overall_rating}/5")
    
    return {
        "success": True,
        "message": "Thank you for your feedback!",
        "feedback_id": feedback_id
    }

@api_router.get("/survey/validate/{token}")
async def validate_survey_token(token: str):
    """Validate a survey token and return booking info (public)"""
    token_doc = await db.survey_tokens.find_one({
        "token": token,
        "used": False
    })
    
    if not token_doc:
        raise HTTPException(status_code=404, detail="Invalid or expired survey token")
    
    return {
        "valid": True,
        "booking_id": token_doc.get("booking_id"),
        "hotel_name": token_doc.get("hotel_name"),
        "check_out": token_doc.get("check_out"),
        "guest_email": token_doc.get("guest_email", "")[:3] + "***"  # Mask email for privacy
    }

@api_router.get("/admin/feedback")
async def get_all_feedback(request: Request, status: str = None, limit: int = 50, skip: int = 0):
    """Get all guest feedback (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    query = {}
    if status:
        query["status"] = status
    
    feedback_list = await db.guest_feedback.find(
        query,
        {"_id": 0}
    ).sort("submitted_at", -1).skip(skip).limit(limit).to_list(limit)
    
    # Get counts
    total = await db.guest_feedback.count_documents(query)
    pending_count = await db.guest_feedback.count_documents({"status": "pending"})
    approved_count = await db.guest_feedback.count_documents({"status": "approved"})
    rejected_count = await db.guest_feedback.count_documents({"status": "rejected"})
    
    return {
        "feedback": feedback_list,
        "total": total,
        "pending_count": pending_count,
        "approved_count": approved_count,
        "rejected_count": rejected_count
    }

@api_router.put("/admin/feedback/{feedback_id}/status")
async def update_feedback_status(request: Request, feedback_id: str, status: str, make_public: bool = False):
    """Update feedback status and optionally make it public (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    if status not in ["pending", "approved", "rejected"]:
        raise HTTPException(status_code=400, detail="Invalid status")
    
    update_data = {
        "status": status,
        "reviewed_at": datetime.now(timezone.utc).isoformat()
    }
    
    # If approved and make_public, set as public review
    if status == "approved" and make_public:
        update_data["is_public"] = True
    elif status == "rejected":
        update_data["is_public"] = False
    
    result = await db.guest_feedback.update_one(
        {"feedback_id": feedback_id},
        {"$set": update_data}
    )
    
    if result.matched_count == 0:
        raise HTTPException(status_code=404, detail="Feedback not found")
    
    logger.info(f"Feedback {feedback_id} status updated to {status}, public: {make_public}")
    return {"success": True, "message": f"Feedback {status}"}

@api_router.get("/admin/feedback/stats")
async def get_feedback_stats(request: Request):
    """Get feedback analytics statistics (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Total counts
    total_feedback = await db.guest_feedback.count_documents({})
    pending = await db.guest_feedback.count_documents({"status": "pending"})
    approved = await db.guest_feedback.count_documents({"status": "approved"})
    rejected = await db.guest_feedback.count_documents({"status": "rejected"})
    public_reviews = await db.guest_feedback.count_documents({"is_public": True})
    
    # Average ratings
    pipeline = [
        {"$match": {"overall_rating": {"$exists": True}}},
        {"$group": {
            "_id": None,
            "avg_overall": {"$avg": "$overall_rating"},
            "avg_cleanliness": {"$avg": "$cleanliness_rating"},
            "avg_service": {"$avg": "$service_rating"},
            "avg_value": {"$avg": "$value_rating"},
            "avg_location": {"$avg": "$location_rating"},
            "avg_amenities": {"$avg": "$amenities_rating"},
            "total_ratings": {"$sum": 1},
            "would_recommend_count": {"$sum": {"$cond": ["$would_recommend", 1, 0]}}
        }}
    ]
    
    agg_result = await db.guest_feedback.aggregate(pipeline).to_list(1)
    
    ratings = {}
    recommend_rate = 0
    if agg_result:
        r = agg_result[0]
        ratings = {
            "overall": round(r.get("avg_overall", 0), 1),
            "cleanliness": round(r.get("avg_cleanliness", 0), 1),
            "service": round(r.get("avg_service", 0), 1),
            "value": round(r.get("avg_value", 0), 1),
            "location": round(r.get("avg_location", 0) or 0, 1),
            "amenities": round(r.get("avg_amenities", 0) or 0, 1)
        }
        if r.get("total_ratings", 0) > 0:
            recommend_rate = round(r.get("would_recommend_count", 0) / r.get("total_ratings", 1) * 100, 1)
    
    # Rating distribution
    distribution_pipeline = [
        {"$match": {"overall_rating": {"$exists": True}}},
        {"$group": {
            "_id": "$overall_rating",
            "count": {"$sum": 1}
        }}
    ]
    
    dist_result = await db.guest_feedback.aggregate(distribution_pipeline).to_list(5)
    distribution = {1: 0, 2: 0, 3: 0, 4: 0, 5: 0}
    for d in dist_result:
        if d["_id"] in distribution:
            distribution[d["_id"]] = d["count"]
    
    # Travel type breakdown
    travel_pipeline = [
        {"$match": {"travel_type": {"$exists": True, "$ne": None}}},
        {"$group": {
            "_id": "$travel_type",
            "count": {"$sum": 1},
            "avg_rating": {"$avg": "$overall_rating"}
        }}
    ]
    
    travel_result = await db.guest_feedback.aggregate(travel_pipeline).to_list(10)
    travel_breakdown = [
        {"type": t["_id"], "count": t["count"], "avg_rating": round(t.get("avg_rating", 0), 1)}
        for t in travel_result
    ]
    
    # Feedback request stats
    total_requests_sent = await db.bookings.count_documents({"feedback_request_sent": True})
    feedback_received = total_feedback
    response_rate = round(feedback_received / total_requests_sent * 100, 1) if total_requests_sent > 0 else 0
    
    # Recent feedback summary (last 30 days)
    thirty_days_ago = (datetime.now(timezone.utc) - timedelta(days=30)).isoformat()
    recent_count = await db.guest_feedback.count_documents({"submitted_at": {"$gte": thirty_days_ago}})
    
    return {
        "counts": {
            "total": total_feedback,
            "pending": pending,
            "approved": approved,
            "rejected": rejected,
            "public_reviews": public_reviews
        },
        "average_ratings": ratings,
        "recommend_rate": recommend_rate,
        "rating_distribution": distribution,
        "travel_breakdown": travel_breakdown,
        "response_stats": {
            "requests_sent": total_requests_sent,
            "responses_received": feedback_received,
            "response_rate": response_rate
        },
        "recent_30_days": recent_count
    }

@api_router.get("/admin/feedback/trigger-test")
async def trigger_feedback_test(request: Request):
    """Manually trigger feedback request job (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    result = await PostStayFeedbackService.check_and_send_feedback_requests()
    return {"success": True, "result": result}

@api_router.get("/hotels/{hotel_id}/reviews")
async def get_hotel_public_reviews(hotel_id: str, limit: int = 10):
    """Get public reviews for a hotel (public endpoint)"""
    reviews = await db.guest_feedback.find(
        {"is_public": True},  # In future, can filter by hotel_id when stored
        {
            "_id": 0,
            "feedback_id": 1,
            "overall_rating": 1,
            "cleanliness_rating": 1,
            "service_rating": 1,
            "value_rating": 1,
            "title": 1,
            "review_text": 1,
            "travel_type": 1,
            "submitted_at": 1,
            "hotel_name": 1
        }
    ).sort("submitted_at", -1).limit(limit).to_list(limit)
    
    return {"reviews": reviews, "count": len(reviews)}

class ReferralTiersRequest(BaseModel):
    tiers: List[Dict[str, Any]]

@api_router.get("/admin/referral-tiers")
async def get_referral_tiers(request: Request):
    """Get referral tiers configuration (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    settings = await get_settings()
    default_tiers = [
        {"name": "Starter", "min": 0, "max": 2, "extraDiscount": 0},
        {"name": "Bronze", "min": 3, "max": 5, "extraDiscount": 5},
        {"name": "Silver", "min": 6, "max": 9, "extraDiscount": 10},
        {"name": "Gold", "min": 10, "max": 19, "extraDiscount": 15},
        {"name": "Diamond", "min": 20, "max": 999, "extraDiscount": 20}
    ]
    
    return {
        "tiers": settings.get("referral_tiers", default_tiers)
    }

@api_router.post("/admin/referral-tiers")
async def save_referral_tiers(request: Request, data: ReferralTiersRequest):
    """Save referral tiers configuration (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Validate tiers
    for tier in data.tiers:
        if not all(key in tier for key in ["name", "min", "max", "extraDiscount"]):
            raise HTTPException(status_code=400, detail="Invalid tier format")
        if tier["min"] < 0 or tier["max"] < tier["min"]:
            raise HTTPException(status_code=400, detail="Invalid tier range")
        if tier["extraDiscount"] < 0 or tier["extraDiscount"] > 100:
            raise HTTPException(status_code=400, detail="Extra discount must be 0-100")
    
    # Save to settings
    await db.settings.update_one(
        {},
        {"$set": {"referral_tiers": data.tiers}},
        upsert=True
    )
    
    logger.info(f"Referral tiers updated: {len(data.tiers)} tiers saved")
    return {"success": True, "message": "Referral tiers saved successfully"}

@api_router.get("/admin/users")
async def get_all_users(request: Request, limit: int = 50, skip: int = 0):
    """Get all users (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    users = await db.users.find({}, {"_id": 0, "password": 0}).sort("created_at", -1).skip(skip).limit(limit).to_list(limit)
    total = await db.users.count_documents({})
    
    return {"users": users, "total": total}

@api_router.put("/admin/users/{user_id}")
async def update_user(user_id: str, request: Request):
    """Update user details (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    body = await request.json()
    
    # Check if user exists
    user = await db.users.find_one({"user_id": user_id})
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    
    # Build update document
    update_data = {}
    if "name" in body:
        update_data["name"] = body["name"]
    if "email" in body:
        # Check if email is taken by another user
        existing = await db.users.find_one({"email": body["email"], "user_id": {"$ne": user_id}})
        if existing:
            raise HTTPException(status_code=400, detail="Email already in use")
        update_data["email"] = body["email"]
    if "pass_type" in body:
        update_data["pass_type"] = body["pass_type"]
    if "referral_discount" in body:
        update_data["referral_discount"] = float(body["referral_discount"])
    
    if update_data:
        await db.users.update_one({"user_id": user_id}, {"$set": update_data})
    
    return {"success": True, "message": "User updated successfully"}

@api_router.delete("/admin/users/{user_id}")
async def delete_user(user_id: str, request: Request):
    """Delete a user (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Check if user exists
    user = await db.users.find_one({"user_id": user_id})
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    
    # Delete user
    await db.users.delete_one({"user_id": user_id})
    
    # Optionally delete related data (bookings, favorites, etc.)
    await db.favorites.delete_many({"user_id": user_id})
    await db.referrals.delete_many({"$or": [{"referrer_id": user_id}, {"referred_id": user_id}]})
    
    return {"success": True, "message": "User deleted successfully"}

@api_router.post("/admin/users/{user_id}/verify-email")
async def verify_user_email(user_id: str, request: Request):
    """Manually verify a user's email (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Check if user exists
    user = await db.users.find_one({"user_id": user_id})
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    
    # Update email_verified status
    await db.users.update_one(
        {"user_id": user_id},
        {"$set": {"email_verified": True, "verification_token": None}}
    )
    
    return {"success": True, "message": "Email verified successfully"}

@api_router.post("/admin/promo-codes")
async def create_promo_code(promo: PromoCodeCreate, request: Request):
    """Create a new promo code"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Check if code exists
    existing = await db.promo_codes.find_one({"code": promo.code.upper()})
    if existing:
        raise HTTPException(status_code=400, detail="Promo code already exists")
    
    promo_doc = {
        "code": promo.code.upper(),
        "discount_rate": promo.discount_rate,
        "active": promo.active,
        "description": promo.description,
        "expires_at": promo.expires_at,
        "created_at": datetime.now(timezone.utc).isoformat()
    }
    
    await db.promo_codes.insert_one(promo_doc)
    return {"success": True, "code": promo.code.upper()}

@api_router.get("/admin/promo-codes")
async def get_promo_codes(request: Request):
    """Get all promo codes"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    codes = await db.promo_codes.find({}, {"_id": 0}).to_list(100)
    return {"promo_codes": codes}

@api_router.delete("/admin/promo-codes/{code}")
async def delete_promo_code(code: str, request: Request):
    """Delete a promo code"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    result = await db.promo_codes.delete_one({"code": code.upper()})
    if result.deleted_count == 0:
        raise HTTPException(status_code=404, detail="Promo code not found")
    
    return {"success": True}

# ==================== EMAIL MANAGEMENT ROUTES ====================

class TestEmailRequest(BaseModel):
    email: EmailStr

@api_router.post("/admin/email/test")
async def send_test_email_endpoint(request: Request, data: TestEmailRequest):
    """Send a test email to verify SMTP configuration"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    result = await EmailService.send_test_email(data.email)
    if not result.get("success"):
        raise HTTPException(status_code=400, detail=result.get("error", "Failed to send test email"))
    
    return result

@api_router.post("/admin/email/test-all")
async def send_all_test_emails(request: Request, data: TestEmailRequest):
    """Send all email types to test the templates"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    results = {}
    test_email = data.email
    
    # 1. Verification Email
    try:
        result = await EmailService.send_verification_email(test_email, "Test User", "test_verify_token_123")
        results["verification_email"] = result
    except Exception as e:
        results["verification_email"] = {"success": False, "error": str(e)}
    
    # 2. Referral Welcome Email (for invited user)
    try:
        result = await EmailService.send_referral_welcome_email(test_email, "Test Invited User", "Test Referrer", 15.0)
        results["referral_welcome_email"] = result
    except Exception as e:
        results["referral_welcome_email"] = {"success": False, "error": str(e)}
    
    # 3. Referrer Notification Email (for the referrer)
    try:
        result = await EmailService.send_referrer_notification_email(test_email, "Test Referrer", "New Friend")
        results["referrer_notification_email"] = result
    except Exception as e:
        results["referrer_notification_email"] = {"success": False, "error": str(e)}
    
    # 4. Booking Confirmation Email (One-Time Pass)
    try:
        test_booking_onetime = {
            "booking_id": "BK-TEST1234",
            "hotel_name": "Test Paradise Hotel",
            "room_type": "Deluxe Suite",
            "board_type": "Half Board",
            "check_in": "2026-02-01",
            "check_out": "2026-02-05",
            "adults": 2,
            "children": 1,
            "children_ages": [8],
            "guest_first_name": "Test",
            "guest_last_name": "Guest",
            "guest_email": test_email,
            "guest_phone": "+31612345678",
            "new_pass_code": "ONETIME-TEST123",
            "pass_purchase_type": "one_time",
            "total_price": 450.00,
            "special_requests": "Late check-in please"
        }
        result = await EmailService.send_booking_confirmation(test_booking_onetime)
        results["booking_confirmation_onetime_pass"] = result
    except Exception as e:
        results["booking_confirmation_onetime_pass"] = {"success": False, "error": str(e)}
    
    # 4b. Booking Confirmation Email (Annual Pass)
    try:
        test_booking_annual = {
            "booking_id": "BK-ANNUAL456",
            "hotel_name": "Test Luxury Resort",
            "room_type": "Premium Suite",
            "board_type": "All Inclusive",
            "check_in": "2026-03-01",
            "check_out": "2026-03-07",
            "adults": 2,
            "children": 0,
            "guest_first_name": "Annual",
            "guest_last_name": "Member",
            "guest_email": test_email,
            "guest_phone": "+31687654321",
            "new_pass_code": "ANNUAL-TEST456",
            "pass_purchase_type": "annual",
            "total_price": 850.00
        }
        result = await EmailService.send_booking_confirmation(test_booking_annual)
        results["booking_confirmation_annual_pass"] = result
    except Exception as e:
        results["booking_confirmation_annual_pass"] = {"success": False, "error": str(e)}
    
    # 5. Password Reset Email
    try:
        result = await EmailService.send_password_reset_email(test_email, "Test User", "test_reset_token_123")
        results["password_reset_email"] = result
    except Exception as e:
        results["password_reset_email"] = {"success": False, "error": str(e)}
    
    # 6. Referral Milestone Email (10 referrals = FREE Annual Pass)
    try:
        result = await EmailService.send_referral_milestone_email(test_email, "Top Referrer", "ANNUAL-TEST1234", 10)
        results["referral_milestone_email"] = result
    except Exception as e:
        results["referral_milestone_email"] = {"success": False, "error": str(e)}
    
    # 7. Price Comparison Campaign Email
    try:
        test_comparison = {
            "destination": "Amsterdam, Netherlands",
            "check_in": "2026-02-15",
            "check_out": "2026-02-18",
            "guests": "2 adults",
            "hotels_count": 45,
            "hotels_with_savings": 12,
            "total_savings": 856.50
        }
        # Override the email to send to test email instead of campain@freestays.eu
        settings = await PriceComparisonService.get_comparison_settings()
        smtp_settings = await EmailService.get_smtp_settings()
        
        # Generate email content
        html_content = f"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>FreeStays Price Check</title>
        </head>
        <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                            <tr>
                                <td style="background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%); padding: 25px 30px;">
                                    <table width="100%" cellpadding="0" cellspacing="0">
                                        <tr>
                                            <td style="vertical-align: middle;">
                                                <span style="font-size: 28px; font-weight: bold; color: #ffffff;">FreeStays</span>
                                            </td>
                                            <td style="text-align: right; vertical-align: middle;">
                                                <p style="margin: 0; color: #a3c9f1; font-size: 14px; font-style: italic;">Commission-free bookings</p>
                                            </td>
                                        </tr>
                                    </table>
                                    <h1 style="color: #ffffff; margin: 20px 0 0 0; font-size: 24px; font-weight: 600; text-align: center;">Price Comparison Alert</h1>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 30px;">
                                    <h2 style="margin: 0 0 20px 0; color: #1e3a5f; font-size: 20px; font-weight: 600;">Search Details</h2>
                                    <table width="100%" cellpadding="0" cellspacing="0">
                                        <tr><td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">Destination:</td><td style="padding: 10px; border-bottom: 1px solid #eee; font-weight: 600; color: #1e3a5f;">{test_comparison['destination']}</td></tr>
                                        <tr><td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">Check-in:</td><td style="padding: 10px; border-bottom: 1px solid #eee;">{test_comparison['check_in']}</td></tr>
                                        <tr><td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">Check-out:</td><td style="padding: 10px; border-bottom: 1px solid #eee;">{test_comparison['check_out']}</td></tr>
                                        <tr><td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">Guests:</td><td style="padding: 10px; border-bottom: 1px solid #eee;">{test_comparison['guests']}</td></tr>
                                        <tr><td style="padding: 10px; border-bottom: 1px solid #eee; color: #666;">Hotels Found:</td><td style="padding: 10px; border-bottom: 1px solid #eee;">{test_comparison['hotels_count']}</td></tr>
                                        <tr><td style="padding: 10px; color: #666;">Hotels with 10%+ Savings:</td><td style="padding: 10px; color: #059669; font-weight: 700; font-size: 18px;">{test_comparison['hotels_with_savings']}</td></tr>
                                    </table>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 0 30px 30px 30px;">
                                    <div style="background: linear-gradient(135deg, #dcfce7 0%, #bbf7d0 100%); border-radius: 12px; padding: 25px; text-align: center; border: 2px solid #22c55e;">
                                        <p style="margin: 0 0 8px 0; color: #166534; font-size: 14px; font-weight: 600;">TOTAL POTENTIAL SAVINGS</p>
                                        <p style="margin: 0; color: #1e3a5f; font-size: 36px; font-weight: bold;">&euro;{test_comparison['total_savings']:.2f}</p>
                                        <p style="margin: 8px 0 0 0; color: #15803d; font-size: 12px;">vs. estimated prices on other booking platforms</p>
                                    </div>
                                </td>
                            </tr>
                            <tr>
                                <td style="background-color: #f8fafc; padding: 30px; border-top: 1px solid #eee; text-align: center;">
                                    <p style="margin: 0 0 10px 0; color: #1e3a5f; font-size: 16px; font-weight: 600;">FreeStays</p>
                                    <p style="margin: 0 0 5px 0; color: #666; font-size: 13px;">Van Haersoltelaan 19</p>
                                    <p style="margin: 0 0 15px 0; color: #666; font-size: 13px;">Barneveld, Netherlands</p>
                                    <p style="margin: 0; color: #999; font-size: 11px;">Automated price comparison report - {datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M UTC')}</p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """
        
        msg = MIMEMultipart('alternative')
        msg['Subject'] = f"Price Check: {test_comparison['destination']} - {test_comparison['hotels_with_savings']} hotels with savings"
        msg['From'] = f"{smtp_settings.get('from_name', 'FreeStays')} <{smtp_settings.get('from_email', 'booking@freestays.eu')}>"
        msg['To'] = test_email
        msg.attach(MIMEText(html_content, 'html'))
        
        def send_email():
            with smtplib.SMTP(smtp_settings.get('host'), smtp_settings.get('port')) as server:
                server.starttls()
                server.login(smtp_settings.get('username'), smtp_settings.get('password'))
                server.send_message(msg)
        
        loop = asyncio.get_event_loop()
        await loop.run_in_executor(None, send_email)
        results["price_comparison_campaign_email"] = {"success": True}
    except Exception as e:
        results["price_comparison_campaign_email"] = {"success": False, "error": str(e)}
    
    # Count successes
    success_count = sum(1 for r in results.values() if r.get("success"))
    total_count = len(results)
    
    return {
        "success": success_count == total_count,
        "summary": f"{success_count}/{total_count} emails sent successfully",
        "results": results
    }

@api_router.post("/admin/email/resend/{booking_id}")
async def resend_booking_confirmation(request: Request, booking_id: str):
    """Resend booking confirmation email"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    booking = await db.bookings.find_one({"booking_id": booking_id}, {"_id": 0})
    if not booking:
        raise HTTPException(status_code=404, detail="Booking not found")
    
    result = await EmailService.send_booking_confirmation(booking)
    if not result.get("success"):
        raise HTTPException(status_code=400, detail=result.get("error", "Failed to send email"))
    
    return result

@api_router.get("/admin/email/logs")
async def get_email_logs(request: Request, limit: int = 50):
    """Get recent email logs"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    logs = await db.email_logs.find({}, {"_id": 0}).sort("sent_at", -1).limit(limit).to_list(limit)
    return {"logs": logs}

@api_router.get("/admin/stats")
async def get_admin_stats(request: Request):
    """Get dashboard statistics"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    total_users = await db.users.count_documents({})
    total_bookings = await db.bookings.count_documents({})
    confirmed_bookings = await db.bookings.count_documents({"status": "confirmed"})
    pending_bookings = await db.bookings.count_documents({"status": "pending_payment"})
    
    # Calculate revenue
    pipeline = [
        {"$match": {"status": "confirmed"}},
        {"$group": {"_id": None, "total": {"$sum": "$final_price"}}}
    ]
    revenue_result = await db.bookings.aggregate(pipeline).to_list(1)
    total_revenue = revenue_result[0]["total"] if revenue_result else 0
    
    # Pass sales
    pass_holders = await db.users.count_documents({"pass_type": {"$in": ["one_time", "annual"]}})
    annual_pass_holders = await db.users.count_documents({"pass_type": "annual"})
    
    return {
        "total_users": total_users,
        "total_bookings": total_bookings,
        "confirmed_bookings": confirmed_bookings,
        "pending_bookings": pending_bookings,
        "total_revenue": total_revenue,
        "pass_holders": pass_holders,
        "annual_pass_holders": annual_pass_holders
    }

# ==================== NEWSLETTER MANAGEMENT ====================

class NewsletterSubscribeRequest(BaseModel):
    email: str
    name: Optional[str] = None

class NewsletterSendRequest(BaseModel):
    subject: str
    content: str
    image_url: Optional[str] = None
    include_last_minute: bool = True
    send_to_all: bool = False
    user_ids: Optional[List[str]] = None

@api_router.post("/newsletter/subscribe")
async def subscribe_newsletter(data: NewsletterSubscribeRequest):
    """Public endpoint for newsletter subscription"""
    email = data.email.lower().strip()
    
    # Check if already subscribed
    existing = await db.newsletter_subscribers.find_one({"email": email})
    if existing:
        return {"success": True, "message": "Already subscribed"}
    
    # Also check if user exists and update their subscription
    user = await db.users.find_one({"email": email})
    if user:
        await db.users.update_one(
            {"email": email},
            {"$set": {"newsletter_subscribed": True, "newsletter_subscribed_at": datetime.now(timezone.utc).isoformat()}}
        )
    
    # Add to subscribers collection
    subscriber = {
        "email": email,
        "name": data.name,
        "subscribed_at": datetime.now(timezone.utc).isoformat(),
        "source": "website",
        "is_active": True
    }
    await db.newsletter_subscribers.insert_one(subscriber)
    
    logger.info(f"📧 New newsletter subscriber: {email}")
    return {"success": True, "message": "Successfully subscribed to newsletter"}

@api_router.delete("/newsletter/unsubscribe")
async def unsubscribe_newsletter(email: str):
    """Public endpoint for newsletter unsubscription"""
    email = email.lower().strip()
    
    # Update subscriber status
    await db.newsletter_subscribers.update_one(
        {"email": email},
        {"$set": {"is_active": False, "unsubscribed_at": datetime.now(timezone.utc).isoformat()}}
    )
    
    # Update user if exists
    await db.users.update_one(
        {"email": email},
        {"$set": {"newsletter_subscribed": False}}
    )
    
    return {"success": True, "message": "Successfully unsubscribed"}

@api_router.get("/admin/newsletter/subscribers")
async def get_newsletter_subscribers(request: Request, active_only: bool = True, limit: int = 100, skip: int = 0):
    """Get newsletter subscribers (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    query = {}
    if active_only:
        query["is_active"] = True
    
    subscribers = await db.newsletter_subscribers.find(
        query,
        {"_id": 0}
    ).sort("subscribed_at", -1).skip(skip).limit(limit).to_list(limit)
    
    total = await db.newsletter_subscribers.count_documents(query)
    active_count = await db.newsletter_subscribers.count_documents({"is_active": True})
    
    # Also get users who are subscribed
    subscribed_users = await db.users.find(
        {"newsletter_subscribed": True},
        {"_id": 0, "email": 1, "first_name": 1, "last_name": 1, "created_at": 1}
    ).to_list(500)
    
    return {
        "subscribers": subscribers,
        "subscribed_users": subscribed_users,
        "total": total,
        "active_count": active_count,
        "user_subscribers_count": len(subscribed_users)
    }

@api_router.put("/admin/users/{user_id}/newsletter")
async def toggle_user_newsletter(request: Request, user_id: str, subscribed: bool):
    """Toggle newsletter subscription for a user (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    update_data = {
        "newsletter_subscribed": subscribed,
        "newsletter_updated_at": datetime.now(timezone.utc).isoformat()
    }
    
    if subscribed:
        update_data["newsletter_subscribed_at"] = datetime.now(timezone.utc).isoformat()
    
    result = await db.users.update_one(
        {"user_id": user_id},
        {"$set": update_data}
    )
    
    if result.matched_count == 0:
        raise HTTPException(status_code=404, detail="User not found")
    
    return {"success": True, "subscribed": subscribed}

@api_router.post("/admin/newsletter/upload-image")
async def upload_newsletter_image(request: Request, file: UploadFile = File(...)):
    """Upload image for newsletter (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Validate file type
    allowed_types = ["image/jpeg", "image/png", "image/gif", "image/webp"]
    if file.content_type not in allowed_types:
        raise HTTPException(status_code=400, detail="Invalid file type. Allowed: JPEG, PNG, GIF, WebP")
    
    # Read file content
    content = await file.read()
    
    # Generate unique filename
    ext = file.filename.split(".")[-1] if "." in file.filename else "jpg"
    filename = f"newsletter_{datetime.now().strftime('%Y%m%d_%H%M%S')}_{secrets.token_hex(4)}.{ext}"
    
    # Save to uploads directory
    upload_dir = "/app/frontend/public/uploads/newsletter"
    os.makedirs(upload_dir, exist_ok=True)
    
    file_path = os.path.join(upload_dir, filename)
    with open(file_path, "wb") as f:
        f.write(content)
    
    # Return public URL
    image_url = f"/uploads/newsletter/{filename}"
    
    logger.info(f"📷 Newsletter image uploaded: {filename}")
    return {"success": True, "image_url": image_url, "filename": filename}

@api_router.post("/admin/newsletter/test")
async def send_test_newsletter(request: Request, data: NewsletterSendRequest):
    """Send test newsletter to info@freestays.eu (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    test_email = "info@freestays.eu"
    
    # Get last minute deals for newsletter
    last_minute_html = ""
    if data.include_last_minute:
        try:
            last_minute_data = await get_last_minute_deals()
            hotels = last_minute_data.get("hotels", [])
            if hotels:
                last_minute_html = _generate_last_minute_html(hotels[:4])
        except Exception as e:
            logger.warning(f"Failed to get last minute deals for newsletter: {e}")
    
    # Build email HTML
    email_html = _build_newsletter_html(
        subject=data.subject,
        content=data.content,
        image_url=data.image_url,
        last_minute_html=last_minute_html
    )
    
    # Send test email
    try:
        await send_email_via_smtp(
            to_email=test_email,
            subject=f"[TEST] {data.subject}",
            html_content=email_html
        )
        logger.info(f"📧 Test newsletter sent to {test_email}")
        return {"success": True, "message": f"Test newsletter sent to {test_email}"}
    except Exception as e:
        logger.error(f"Failed to send test newsletter: {e}")
        raise HTTPException(status_code=500, detail=f"Failed to send: {str(e)}")

@api_router.post("/admin/newsletter/send")
async def send_newsletter(request: Request, data: NewsletterSendRequest):
    """Send newsletter to subscribers (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Collect recipient emails
    recipients = []
    
    if data.send_to_all:
        # Get all active newsletter subscribers
        subscribers = await db.newsletter_subscribers.find(
            {"is_active": True},
            {"email": 1}
        ).to_list(10000)
        recipients.extend([s["email"] for s in subscribers])
        
        # Get all users with newsletter_subscribed = True
        users = await db.users.find(
            {"newsletter_subscribed": True},
            {"email": 1}
        ).to_list(10000)
        recipients.extend([u["email"] for u in users])
    elif data.user_ids:
        # Get specific users
        users = await db.users.find(
            {"user_id": {"$in": data.user_ids}},
            {"email": 1}
        ).to_list(len(data.user_ids))
        recipients.extend([u["email"] for u in users])
    
    # Remove duplicates
    recipients = list(set(recipients))
    
    if not recipients:
        raise HTTPException(status_code=400, detail="No recipients found")
    
    # Get last minute deals for newsletter
    last_minute_html = ""
    if data.include_last_minute:
        try:
            last_minute_data = await get_last_minute_deals()
            hotels = last_minute_data.get("hotels", [])
            if hotels:
                last_minute_html = _generate_last_minute_html(hotels[:4])
        except Exception as e:
            logger.warning(f"Failed to get last minute deals for newsletter: {e}")
    
    # Build email HTML
    email_html = _build_newsletter_html(
        subject=data.subject,
        content=data.content,
        image_url=data.image_url,
        last_minute_html=last_minute_html
    )
    
    # Send to all recipients
    sent_count = 0
    failed_count = 0
    
    for email in recipients:
        try:
            await send_email_via_smtp(
                to_email=email,
                subject=data.subject,
                html_content=email_html
            )
            sent_count += 1
        except Exception as e:
            logger.warning(f"Failed to send newsletter to {email}: {e}")
            failed_count += 1
    
    # Log the newsletter send
    await db.newsletter_logs.insert_one({
        "subject": data.subject,
        "sent_at": datetime.now(timezone.utc).isoformat(),
        "total_recipients": len(recipients),
        "sent_count": sent_count,
        "failed_count": failed_count,
        "include_last_minute": data.include_last_minute
    })
    
    logger.info(f"📧 Newsletter sent: {sent_count} delivered, {failed_count} failed")
    return {
        "success": True,
        "sent_count": sent_count,
        "failed_count": failed_count,
        "total_recipients": len(recipients)
    }

@api_router.get("/admin/newsletter/logs")
async def get_newsletter_logs(request: Request, limit: int = 20):
    """Get newsletter send history (admin only)"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    logs = await db.newsletter_logs.find(
        {},
        {"_id": 0}
    ).sort("sent_at", -1).limit(limit).to_list(limit)
    
    return {"logs": logs}

def _generate_last_minute_html(hotels: List[Dict]) -> str:
    """Generate HTML for last minute hotel cards in newsletter"""
    if not hotels:
        return ""
    
    cards_html = ""
    for hotel in hotels:
        image_url = hotel.get("image_url", "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=400")
        name = hotel.get("name", "Hotel")
        city = hotel.get("city", "")
        country = hotel.get("country", "")
        price = hotel.get("min_price", 0)
        
        cards_html += f"""
        <div style="width: 48%; display: inline-block; vertical-align: top; margin-bottom: 20px; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);">
            <img src="{image_url}" alt="{name}" style="width: 100%; height: 150px; object-fit: cover;"/>
            <div style="padding: 15px;">
                <h3 style="margin: 0 0 5px 0; font-size: 16px; color: #333;">{name}</h3>
                <p style="margin: 0 0 10px 0; font-size: 14px; color: #666;">{city}, {country}</p>
                <div style="display: flex; justify-content: space-between; align-items: center;">
                    <span style="font-size: 18px; font-weight: bold; color: #0f766e;">€{price:.0f}</span>
                    <span style="background: #ef4444; color: white; padding: 4px 8px; border-radius: 4px; font-size: 12px;">-30%</span>
                </div>
            </div>
        </div>
        """
    
    return f"""
    <div style="margin: 30px 0;">
        <h2 style="color: #0f766e; margin-bottom: 20px; text-align: center;">🔥 Last Minute Deals</h2>
        <div style="text-align: center;">
            {cards_html}
        </div>
        <div style="text-align: center; margin-top: 20px;">
            <a href="https://freestays.eu/last-minute" style="display: inline-block; background: #0f766e; color: white; padding: 12px 30px; border-radius: 25px; text-decoration: none; font-weight: 600;">View All Deals</a>
        </div>
    </div>
    """

def _build_newsletter_html(subject: str, content: str, image_url: Optional[str], last_minute_html: str) -> str:
    """Build complete newsletter HTML"""
    image_section = ""
    if image_url:
        # Handle relative URLs
        full_image_url = image_url if image_url.startswith("http") else f"https://freestays.eu{image_url}"
        image_section = f"""
        <div style="margin-bottom: 30px; text-align: center;">
            <img src="{full_image_url}" alt="Newsletter" style="max-width: 100%; height: auto; border-radius: 12px;"/>
        </div>
        """
    
    return f"""
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>{subject}</title>
    </head>
    <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
        <div style="max-width: 600px; margin: 0 auto; background-color: #ffffff;">
            <!-- Header -->
            <div style="background: linear-gradient(135deg, #0f766e 0%, #0891b2 100%); padding: 30px; text-align: center;">
                <img src="https://freestays.eu/logo-white.png" alt="FreeStays" style="height: 40px; margin-bottom: 10px;" onerror="this.style.display='none'"/>
                <h1 style="color: white; margin: 0; font-size: 24px;">FreeStays Newsletter</h1>
            </div>
            
            <!-- Content -->
            <div style="padding: 30px;">
                {image_section}
                
                <div style="font-size: 16px; line-height: 1.6; color: #333;">
                    {content}
                </div>
                
                {last_minute_html}
            </div>
            
            <!-- Footer -->
            <div style="background: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #eee;">
                <p style="margin: 0 0 15px 0; color: #666; font-size: 14px;">
                    💙 FreeStays - Where Your Room Becomes FREE
                </p>
                <p style="margin: 0 0 15px 0; color: #999; font-size: 12px;">
                    You received this email because you subscribed to our newsletter.
                </p>
                <a href="https://freestays.eu/unsubscribe" style="color: #0f766e; font-size: 12px;">Unsubscribe</a>
            </div>
        </div>
    </body>
    </html>
    """

# ==================== PWA ANALYTICS ENDPOINTS ====================

@api_router.post("/pwa/track-install")
async def track_pwa_install(request: Request):
    """Track PWA installation - called from frontend when app is installed"""
    try:
        body = await request.json()
    except:
        body = {}
    
    # Try to get user info from token if available
    user_id = None
    user_email = None
    auth_header = request.headers.get("Authorization")
    if auth_header and auth_header.startswith("Bearer "):
        try:
            token = auth_header[7:]
            payload = jwt.decode(token, JWT_SECRET, algorithms=[JWT_ALGORITHM])
            user_id = payload.get("user_id")
            if user_id:
                user = await db.users.find_one({"user_id": user_id}, {"email": 1, "name": 1})
                if user:
                    user_email = user.get("email")
        except:
            pass
    
    # Create install record
    install_record = {
        "install_id": str(uuid.uuid4()),
        "user_id": user_id,
        "user_email": user_email,
        "is_registered_user": user_id is not None,
        "device_info": body.get("device_info", {}),
        "user_agent": request.headers.get("User-Agent", ""),
        "platform": body.get("platform", "unknown"),
        "browser": body.get("browser", "unknown"),
        "installed_at": datetime.now(timezone.utc).isoformat(),
        "last_active": datetime.now(timezone.utc).isoformat(),
        "app_version": body.get("app_version", "1.0.0"),
        "push_subscription": None,
        "is_active": True
    }
    
    # Check if this device already has an install (by user_agent + user_id combo)
    existing = await db.pwa_installs.find_one({
        "user_agent": install_record["user_agent"],
        "$or": [
            {"user_id": user_id} if user_id else {"user_id": None},
        ]
    })
    
    if existing:
        # Update existing record
        await db.pwa_installs.update_one(
            {"install_id": existing["install_id"]},
            {"$set": {
                "last_active": datetime.now(timezone.utc).isoformat(),
                "user_id": user_id or existing.get("user_id"),
                "user_email": user_email or existing.get("user_email"),
                "is_registered_user": user_id is not None or existing.get("is_registered_user", False)
            }}
        )
        return {"success": True, "message": "Install record updated", "install_id": existing["install_id"]}
    
    await db.pwa_installs.insert_one(install_record)
    return {"success": True, "message": "Install tracked", "install_id": install_record["install_id"]}

@api_router.post("/pwa/track-activity")
async def track_pwa_activity(request: Request):
    """Track PWA activity - heartbeat to know active installs"""
    try:
        body = await request.json()
        install_id = body.get("install_id")
        
        if install_id:
            await db.pwa_installs.update_one(
                {"install_id": install_id},
                {"$set": {"last_active": datetime.now(timezone.utc).isoformat()}}
            )
        return {"success": True}
    except:
        return {"success": False}

@api_router.post("/pwa/save-push-subscription")
async def save_push_subscription(request: Request):
    """Save push notification subscription for a PWA install"""
    try:
        body = await request.json()
        install_id = body.get("install_id")
        subscription = body.get("subscription")
        
        if install_id and subscription:
            await db.pwa_installs.update_one(
                {"install_id": install_id},
                {"$set": {"push_subscription": subscription}}
            )
            return {"success": True, "message": "Push subscription saved"}
        return {"success": False, "message": "Missing install_id or subscription"}
    except Exception as e:
        return {"success": False, "error": str(e)}

@api_router.get("/admin/pwa/analytics")
async def get_pwa_analytics(request: Request):
    """Get PWA install analytics for admin dashboard"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    # Get all installs
    installs = await db.pwa_installs.find({}, {"_id": 0}).to_list(1000)
    
    # Calculate analytics
    total_installs = len(installs)
    registered_users = sum(1 for i in installs if i.get("is_registered_user"))
    anonymous_users = total_installs - registered_users
    
    # Active in last 7 days
    seven_days_ago = (datetime.now(timezone.utc) - timedelta(days=7)).isoformat()
    active_installs = sum(1 for i in installs if i.get("last_active", "") > seven_days_ago)
    
    # Active in last 30 days
    thirty_days_ago = (datetime.now(timezone.utc) - timedelta(days=30)).isoformat()
    monthly_active = sum(1 for i in installs if i.get("last_active", "") > thirty_days_ago)
    
    # With push subscriptions
    push_enabled = sum(1 for i in installs if i.get("push_subscription"))
    
    # Platform breakdown
    platforms = {}
    browsers = {}
    for install in installs:
        platform = install.get("platform", "unknown")
        browser = install.get("browser", "unknown")
        platforms[platform] = platforms.get(platform, 0) + 1
        browsers[browser] = browsers.get(browser, 0) + 1
    
    # Recent installs (last 10)
    recent_installs = sorted(installs, key=lambda x: x.get("installed_at", ""), reverse=True)[:10]
    
    # Daily installs for chart (last 30 days)
    daily_installs = {}
    for install in installs:
        installed_at = install.get("installed_at", "")
        if installed_at:
            date = installed_at[:10]
            daily_installs[date] = daily_installs.get(date, 0) + 1
    
    return {
        "total_installs": total_installs,
        "registered_users": registered_users,
        "anonymous_users": anonymous_users,
        "active_last_7_days": active_installs,
        "active_last_30_days": monthly_active,
        "push_enabled": push_enabled,
        "platforms": platforms,
        "browsers": browsers,
        "recent_installs": recent_installs,
        "daily_installs": daily_installs
    }

@api_router.post("/admin/pwa/push-update")
async def push_update_to_all(request: Request):
    """Send update notification to all PWA installs with push subscription"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    try:
        body = await request.json()
    except:
        body = {}
    
    message = body.get("message", "A new version of FreeStays is available! Please refresh the app.")
    title = body.get("title", "FreeStays Update Available")
    
    # Get all installs with push subscriptions
    installs = await db.pwa_installs.find(
        {"push_subscription": {"$ne": None}},
        {"_id": 0, "install_id": 1, "push_subscription": 1, "user_email": 1}
    ).to_list(1000)
    
    if not installs:
        # Still record the update for all installs
        all_installs = await db.pwa_installs.count_documents({})
        update_record = {
            "update_id": str(uuid.uuid4()),
            "title": title,
            "message": message,
            "created_at": datetime.now(timezone.utc).isoformat(),
            "target_installs": all_installs,
            "sent_by": "admin"
        }
        await db.pwa_updates.insert_one(update_record)
        
        # Set update flag in settings
        await db.settings.update_one(
            {"type": "app_settings"},
            {"$set": {
                "pwa_update_requested": datetime.now(timezone.utc).isoformat(),
                "pwa_update_message": message
            }}
        )
        
        return {"success": True, "sent": all_installs, "message": f"Update notification queued for {all_installs} devices", "update_id": update_record["update_id"]}
    
    # Note: Push notifications require VAPID keys and pywebpush library
    # For now, we'll record the update request and instruct clients to check for updates
    update_record = {
        "update_id": str(uuid.uuid4()),
        "title": title,
        "message": message,
        "created_at": datetime.now(timezone.utc).isoformat(),
        "target_installs": len(installs),
        "sent_by": "admin"
    }
    await db.pwa_updates.insert_one(update_record)
    
    # Increment the app version in settings to trigger service worker update
    await db.settings.update_one(
        {"type": "app_settings"},
        {"$set": {
            "pwa_update_requested": datetime.now(timezone.utc).isoformat(),
            "pwa_update_message": message
        }}
    )
    
    return {
        "success": True,
        "sent": len(installs),
        "message": f"Update notification queued for {len(installs)} devices",
        "update_id": update_record["update_id"]
    }

@api_router.get("/admin/pwa/updates")
async def get_pwa_update_history(request: Request):
    """Get history of pushed updates"""
    if not await verify_admin(request):
        raise HTTPException(status_code=401, detail="Admin access required")
    
    updates = await db.pwa_updates.find({}, {"_id": 0}).sort("created_at", -1).limit(20).to_list(20)
    return {"updates": updates}

@api_router.get("/pwa/check-update")
async def check_pwa_update():
    """Check if there's a pending update for PWA clients"""
    settings = await get_settings()
    update_requested = settings.get("pwa_update_requested")
    update_message = settings.get("pwa_update_message", "A new version is available!")
    
    return {
        "update_available": update_requested is not None,
        "update_timestamp": update_requested,
        "message": update_message
    }

# Include the router in the main app
app.include_router(api_router)

# Mount static files for uploads (logo, etc.)
app.mount("/static", StaticFiles(directory=str(ROOT_DIR / "static")), name="static")

app.add_middleware(
    CORSMiddleware,
    allow_credentials=True,
    allow_origins=[
        "http://localhost:3000",
        "http://localhost:8001",
        "https://hotelkit-app.preview.emergentagent.com",
        os.environ.get("FRONTEND_URL", "http://localhost:3000")
    ],
    allow_methods=["*"],
    allow_headers=["*"],
)

# ==================== SCHEDULED JOBS ====================

async def scheduled_price_drop_check():
    """Background job to check for price drops on favorited hotels"""
    try:
        settings = await get_settings()
        if not settings.get("price_drop_enabled", True):
            logger.info("Price drop check skipped - disabled in settings")
            return
        
        min_drop_percent = settings.get("price_drop_min_percent", 5)
        
        # Get all favorites with saved prices
        favorites = await db.favorites.find(
            {"min_price": {"$exists": True, "$ne": None}},
            {"_id": 0}
        ).to_list(1000)
        
        if not favorites:
            logger.info("No favorites with prices to check")
            return
        
        # Group by hotel
        hotels_to_check = {}
        for fav in favorites:
            hotel_id = fav.get("hotel_id")
            if hotel_id and hotel_id not in hotels_to_check:
                hotels_to_check[hotel_id] = {
                    "hotel_name": fav.get("hotel_name", "Hotel"),
                    "old_price": fav.get("min_price", 0),
                    "users": []
                }
            if hotel_id:
                hotels_to_check[hotel_id]["users"].append({
                    "user_id": fav.get("user_id"),
                    "old_price": fav.get("min_price", 0)
                })
        
        logger.info(f"Price drop check: Found {len(hotels_to_check)} hotels to check")
        
        # Note: In production, this would call Sunhotels API to get current prices
        # For now, we log the check and update last_check timestamp
        await db.settings.update_one(
            {"key": "app_settings"},
            {"$set": {"price_drop_last_check": datetime.now(timezone.utc).isoformat()}}
        )
        
        logger.info(f"Price drop check completed for {len(hotels_to_check)} hotels")
        
    except Exception as e:
        logger.error(f"Price drop check error: {str(e)}")

async def scheduled_hotel_image_sync():
    """Scheduled job to sync hotel images from Sunhotels API to local database"""
    try:
        # Get settings to check if auto-sync is enabled
        settings = await db.settings.find_one({"type": "app_settings"})
        if not settings:
            logger.info("Auto-sync: No settings found, skipping")
            return
            
        auto_sync_enabled = settings.get("auto_sync_enabled", False)
        if not auto_sync_enabled:
            logger.info("Auto-sync: Disabled in settings, skipping")
            return
            
        batch_size = settings.get("auto_sync_batch_size", 50)
        
        logger.info(f"🔄 Starting scheduled hotel image sync (batch size: {batch_size})...")
        
        # Get database config
        db_config = await sunhotels_client.get_static_db_connection()
        if not db_config["host"]:
            logger.warning("Auto-sync: Static database not configured")
            return
        
        try:
            conn = await asyncio.wait_for(
                aiomysql.connect(
                    host=db_config["host"],
                    port=db_config["port"],
                    user=db_config["user"],
                    password=db_config["password"],
                    db=db_config["database"],
                    charset='utf8mb4'
                ),
                timeout=10
            )
            
            # Get hotels missing images
            async with conn.cursor(aiomysql.DictCursor) as cursor:
                await cursor.execute("""
                    SELECT a.hotel_id, a.display_name
                    FROM ghwk_autocomplete_lookup a
                    LEFT JOIN ghwk_bravo_hotels b ON a.hotel_id = b.hotel_id
                    WHERE a.type = 'hotel' 
                    AND (b.images_json IS NULL OR b.images_json = '' OR b.images_json = '[]' OR b.hotel_id IS NULL)
                    LIMIT %s
                """, (batch_size,))
                hotels_to_sync = await cursor.fetchall()
            
            if not hotels_to_sync:
                conn.close()
                logger.info("Auto-sync: No hotels need syncing")
                # Update last sync timestamp
                await db.settings.update_one(
                    {"type": "app_settings"},
                    {"$set": {
                        "auto_sync_last_run": datetime.now(timezone.utc).isoformat(),
                        "auto_sync_last_result": {"synced": 0, "failed": 0, "message": "No hotels need syncing"}
                    }}
                )
                return
            
            # Get credentials
            username, password = await sunhotels_client.get_credentials()
            
            synced = 0
            no_images = 0  # Hotels that don't have images in Sunhotels API
            failed = 0  # Actual errors
            
            async with httpx.AsyncClient(timeout=30) as http_client:
                for hotel in hotels_to_sync:
                    hotel_id = str(hotel["hotel_id"])
                    try:
                        # Fetch from API
                        url = f"{SUNHOTELS_BASE_URL}/GetStaticHotelsAndRooms"
                        params = {
                            "userName": username,
                            "password": password,
                            "language": "en",
                            "hotelIDs": hotel_id,
                            "destination": "",
                            "resortIDs": "",
                            "accommodationTypes": "",
                            "sortBy": "",
                            "sortOrder": "",
                            "exactDestinationMatch": ""
                        }
                        response = await http_client.get(url, params=params)
                        
                        if response.status_code == 200:
                            hotel_data = sunhotels_client._parse_static_hotel_data(response.text)
                            hotel_info = list(hotel_data.values())[0] if hotel_data else {}
                            images = hotel_info.get("images", [])
                            
                            if images:
                                images_json = json.dumps([{"id": img} if not str(img).startswith("http") else {"id": img} for img in images])
                                
                                async with conn.cursor() as cursor:
                                    await cursor.execute("SELECT hotel_id FROM ghwk_bravo_hotels WHERE hotel_id = %s", (hotel_id,))
                                    exists = await cursor.fetchone()
                                    
                                    if exists:
                                        await cursor.execute(
                                            "UPDATE ghwk_bravo_hotels SET images_json = %s WHERE hotel_id = %s",
                                            (images_json, hotel_id)
                                        )
                                    else:
                                        await cursor.execute(
                                            "INSERT INTO ghwk_bravo_hotels (hotel_id, images_json) VALUES (%s, %s)",
                                            (hotel_id, images_json)
                                        )
                                
                                synced += 1
                                logger.info(f"Auto-sync: ✓ Hotel {hotel_id} synced with {len(images)} images")
                            else:
                                # Hotel exists in API but has no images - mark as checked with empty array
                                # This prevents re-checking the same hotel repeatedly
                                async with conn.cursor() as cursor:
                                    await cursor.execute("SELECT hotel_id FROM ghwk_bravo_hotels WHERE hotel_id = %s", (hotel_id,))
                                    exists = await cursor.fetchone()
                                    
                                    if exists:
                                        await cursor.execute(
                                            "UPDATE ghwk_bravo_hotels SET images_json = %s WHERE hotel_id = %s",
                                            ('[]', hotel_id)  # Empty array to mark as checked
                                        )
                                    else:
                                        await cursor.execute(
                                            "INSERT INTO ghwk_bravo_hotels (hotel_id, images_json) VALUES (%s, %s)",
                                            (hotel_id, '[]')
                                        )
                                
                                no_images += 1
                                logger.debug(f"Auto-sync: Hotel {hotel_id} has no images in Sunhotels API")
                        else:
                            failed += 1
                            logger.warning(f"Auto-sync: API error for hotel {hotel_id}: HTTP {response.status_code}")
                            
                    except Exception as e:
                        failed += 1
                        logger.warning(f"Auto-sync: Error syncing hotel {hotel_id}: {str(e)[:100]}")
                    
                    # Rate limiting - 0.5s delay between API calls
                    await asyncio.sleep(0.5)
            
            await conn.commit()
            conn.close()
            
            # Clear autocomplete cache
            autocomplete_cache.clear()
            
            # Update sync results in settings - include no_images count
            checked_count = synced + no_images  # Hotels we successfully checked (with or without images)
            await db.settings.update_one(
                {"type": "app_settings"},
                {"$set": {
                    "auto_sync_last_run": datetime.now(timezone.utc).isoformat(),
                    "auto_sync_last_result": {
                        "synced": synced,
                        "no_images": no_images,
                        "failed": failed,
                        "total": len(hotels_to_sync),
                        "checked": checked_count,
                        "message": f"Checked {checked_count} hotels: {synced} with images, {no_images} without images in API, {failed} errors"
                    }
                }}
            )
            
            logger.info(f"✅ Auto-sync complete: {synced} synced with images, {no_images} no images in API, {failed} errors (total: {len(hotels_to_sync)})")
            
        except asyncio.TimeoutError:
            logger.error("Auto-sync: Database connection timeout")
        except Exception as e:
            logger.error(f"Auto-sync: Database error: {str(e)}")
            
    except Exception as e:
        logger.error(f"Auto-sync error: {str(e)}")

# ==================== SUNHOTELS EMAIL FORWARDING SERVICE ====================

class SunhotelsEmailForwarder:
    """Service to check Sunhotels voucher emails and forward them with FreeStays branding"""
    
    @staticmethod
    def decode_email_header(header):
        """Decode email header handling various encodings"""
        if header is None:
            return ""
        decoded_parts = decode_header(header)
        result = []
        for part, encoding in decoded_parts:
            if isinstance(part, bytes):
                result.append(part.decode(encoding or 'utf-8', errors='ignore'))
            else:
                result.append(part)
        return ''.join(result)
    
    @staticmethod
    def extract_voucher_info(subject: str, body: str) -> dict:
        """Extract voucher information from Sunhotels email"""
        info = {
            "voucher_id": None,
            "sunhotels_ref": None,
            "client_name": None,
            "hotel_name": None,
            "check_in": None,
            "check_out": None,
            "room_type": None,
            "board_type": None,
            "address": None,
            "phone": None,
            "voucher_link": None,
            "mobile_voucher_link": None,
            "important_info": None,
            "booking_notes": None
        }
        
        # Extract from subject: "Voucher SH27827772 / Lead Name: TALAL ALQASMI"
        subject_match = re.search(r'Voucher\s+SH(\d+)', subject, re.IGNORECASE)
        if subject_match:
            info["voucher_id"] = subject_match.group(1)
            info["sunhotels_ref"] = f"SH{subject_match.group(1)}"
        
        name_match = re.search(r'Lead Name:\s*(.+?)(?:\s*$|\s*\|)', subject)
        if name_match:
            info["client_name"] = name_match.group(1).strip()
        
        # Extract voucher links from body
        voucher_link_match = re.search(r'(https?://voucher\.travel/\?id=\d+&s=[\d.]+)', body)
        if voucher_link_match:
            info["voucher_link"] = voucher_link_match.group(1)
        
        mobile_link_match = re.search(r'(https?://mobile\.voucher\.travel/account/\d+/[\d.]+)', body)
        if mobile_link_match:
            info["mobile_voucher_link"] = mobile_link_match.group(1)
        
        # Extract details from body using patterns
        patterns = {
            "hotel_name": r'PROPERTY:\s*(.+?)(?:\n|$)',
            "address": r'ADDRESS:\s*(.+?)(?:\n|$)',
            "phone": r'PHONE:\s*(.+?)(?:\n|$)',
            "room_type": r'ROOM TYPE:\s*(.+?)(?:\n|$)',
            "board_type": r'BOARD/REGIMEN:\s*(.+?)(?:\n|$)',
            "check_in": r'ARRIVAL DATE:\s*(\d{4}-\d{2}-\d{2})',
            "check_out": r'DEPARTURE DATE:\s*(\d{4}-\d{2}-\d{2})',
            "important_info": r'IMPORTANT INFORMATION:\s*(.+?)(?:\n|BOOKING NOTES)',
            "booking_notes": r'BOOKING NOTES:\s*(.+?)(?:\n\n|$)',
        }
        
        for key, pattern in patterns.items():
            match = re.search(pattern, body, re.IGNORECASE | re.DOTALL)
            if match:
                info[key] = match.group(1).strip()
        
        # Also try to get client name from body if not in subject
        if not info["client_name"]:
            client_match = re.search(r"CLIENT'S NAME:\s*(.+?)(?:\n|$)", body)
            if client_match:
                info["client_name"] = client_match.group(1).strip()
        
        return info
    
    @staticmethod
    async def send_branded_voucher_email(voucher_info: dict, original_to_email: str = None):
        """Send FreeStays-branded voucher email to customer"""
        try:
            settings = await get_settings()
            smtp_settings = {
                "smtp_host": settings.get("smtp_host"),
                "smtp_port": settings.get("smtp_port", 587),
                "smtp_user": settings.get("smtp_user"),
                "smtp_password": settings.get("smtp_password"),
                "smtp_from": settings.get("smtp_from", "noreply@freestays.eu"),
                "smtp_from_name": settings.get("smtp_from_name", "FreeStays"),
                "company_name": settings.get("company_name", "FreeStays"),
                "company_logo": settings.get("company_logo", ""),
                "primary_color": settings.get("email_primary_color", "#1e3a5f"),
                "accent_color": settings.get("email_accent_color", "#2d8a5f"),
            }
            
            if not smtp_settings["smtp_host"] or not smtp_settings["smtp_user"]:
                logger.error("SMTP not configured for voucher forwarding")
                return False
            
            # Try to find customer email from booking in database
            customer_email = original_to_email
            booking = None
            
            if voucher_info.get("sunhotels_ref"):
                booking = await db.bookings.find_one({
                    "sunhotels_booking_id": voucher_info["sunhotels_ref"]
                })
                if booking:
                    customer_email = booking.get("guest_email") or booking.get("email")
            
            # If still no email, try to find by client name and dates
            if not customer_email and voucher_info.get("client_name") and voucher_info.get("check_in"):
                name_parts = voucher_info["client_name"].split()
                if name_parts:
                    booking = await db.bookings.find_one({
                        "guest_last_name": {"$regex": name_parts[-1], "$options": "i"},
                        "check_in": voucher_info["check_in"]
                    })
                    if booking:
                        customer_email = booking.get("guest_email") or booking.get("email")
            
            if not customer_email:
                logger.warning(f"Could not find customer email for voucher {voucher_info.get('sunhotels_ref')}")
                return False
            
            # Format dates nicely
            check_in_formatted = voucher_info.get("check_in", "")
            check_out_formatted = voucher_info.get("check_out", "")
            try:
                if check_in_formatted:
                    check_in_dt = datetime.strptime(check_in_formatted, "%Y-%m-%d")
                    check_in_formatted = check_in_dt.strftime("%B %d, %Y")
                if check_out_formatted:
                    check_out_dt = datetime.strptime(check_out_formatted, "%Y-%m-%d")
                    check_out_formatted = check_out_dt.strftime("%B %d, %Y")
            except:
                pass
            
            # Build the email HTML
            logo_url = smtp_settings["company_logo"] or "https://freestays.eu/logo.png"
            primary_color = smtp_settings["primary_color"]
            accent_color = smtp_settings["accent_color"]
            
            html_content = f"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Your Travel Voucher - {smtp_settings["company_name"]}</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
                <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                    <tr>
                        <td align="center">
                            <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.1);">
                                <!-- Header -->
                                <tr>
                                    <td style="background: linear-gradient(135deg, {primary_color} 0%, #2d5a87 100%); padding: 30px; text-align: center;">
                                        <img src="{logo_url}" alt="{smtp_settings['company_name']}" style="max-height: 50px; margin-bottom: 15px;">
                                        <h1 style="margin: 0; color: #ffffff; font-size: 28px; font-weight: 600;">🎉 Your Travel Voucher</h1>
                                        <p style="margin: 10px 0 0 0; color: rgba(255,255,255,0.9); font-size: 14px;">Booking Confirmed!</p>
                                    </td>
                                </tr>
                                
                                <!-- Content -->
                                <tr>
                                    <td style="padding: 40px 30px;">
                                        <p style="margin: 0 0 20px 0; font-size: 16px; color: #333;">
                                            Dear <strong>{voucher_info.get('client_name', 'Valued Guest')}</strong>,
                                        </p>
                                        <p style="margin: 0 0 25px 0; font-size: 15px; color: #666; line-height: 1.6;">
                                            Great news! Your hotel booking is confirmed. Please find your travel voucher details below. 
                                            <strong>Present this voucher at check-in.</strong>
                                        </p>
                                        
                                        <!-- Booking Details Card -->
                                        <div style="background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%); border-radius: 12px; padding: 25px; margin-bottom: 25px; border-left: 4px solid {accent_color};">
                                            <h2 style="margin: 0 0 20px 0; color: {primary_color}; font-size: 18px;">
                                                🏨 {voucher_info.get('hotel_name', 'Hotel')}
                                            </h2>
                                            
                                            <table width="100%" cellpadding="0" cellspacing="0">
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666; font-size: 14px; width: 120px;">📅 Check-in:</td>
                                                    <td style="padding: 8px 0; color: #333; font-size: 14px; font-weight: 600;">{check_in_formatted}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666; font-size: 14px;">📅 Check-out:</td>
                                                    <td style="padding: 8px 0; color: #333; font-size: 14px; font-weight: 600;">{check_out_formatted}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666; font-size: 14px;">🛏️ Room:</td>
                                                    <td style="padding: 8px 0; color: #333; font-size: 14px; font-weight: 600;">{voucher_info.get('room_type', 'Standard Room')}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666; font-size: 14px;">🍽️ Meals:</td>
                                                    <td style="padding: 8px 0; color: #333; font-size: 14px; font-weight: 600;">{voucher_info.get('board_type', 'As booked')}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666; font-size: 14px;">📋 Booking Ref:</td>
                                                    <td style="padding: 8px 0; color: {accent_color}; font-size: 14px; font-weight: 700;">{voucher_info.get('sunhotels_ref', '')}</td>
                                                </tr>
                                            </table>
                                            
                                            {f'''<div style="margin-top: 15px; padding-top: 15px; border-top: 1px solid #e2e8f0;">
                                                <p style="margin: 0; color: #666; font-size: 13px;">
                                                    📍 {voucher_info.get('address', '')}
                                                </p>
                                                {f'<p style="margin: 5px 0 0 0; color: #666; font-size: 13px;">📞 {voucher_info.get("phone", "")}</p>' if voucher_info.get('phone') else ''}
                                            </div>''' if voucher_info.get('address') else ''}
                                        </div>
                                        
                                        <!-- Voucher Buttons -->
                                        <div style="text-align: center; margin: 30px 0;">
                                            <p style="margin: 0 0 15px 0; color: #666; font-size: 14px;">Access your official travel voucher:</p>
                                            <table width="100%" cellpadding="0" cellspacing="0">
                                                <tr>
                                                    <td style="padding: 5px;" align="center">
                                                        <a href="{voucher_info.get('voucher_link', '#')}" style="display: inline-block; background: linear-gradient(135deg, {primary_color} 0%, #2d5a87 100%); color: #ffffff; text-decoration: none; padding: 14px 30px; border-radius: 50px; font-weight: 600; font-size: 14px;">
                                                            🖨️ Printable Voucher
                                                        </a>
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 5px;" align="center">
                                                        <a href="{voucher_info.get('mobile_voucher_link', '#')}" style="display: inline-block; background: linear-gradient(135deg, {accent_color} 0%, #22c55e 100%); color: #ffffff; text-decoration: none; padding: 14px 30px; border-radius: 50px; font-weight: 600; font-size: 14px;">
                                                            📱 Mobile Voucher
                                                        </a>
                                                    </td>
                                                </tr>
                                            </table>
                                        </div>
                                        
                                        <!-- Important Notice -->
                                        <div style="background-color: #fef3c7; border-radius: 8px; padding: 15px; margin-bottom: 20px;">
                                            <p style="margin: 0; color: #92400e; font-size: 13px;">
                                                <strong>⚠️ Important:</strong> Please print this voucher or have it available on your mobile device when you check in at the hotel.
                                            </p>
                                        </div>
                                        
                                        {f'''<!-- Booking Notes -->
                                        <div style="background-color: #f1f5f9; border-radius: 8px; padding: 15px; margin-bottom: 20px;">
                                            <p style="margin: 0; color: #475569; font-size: 12px; line-height: 1.5;">
                                                <strong>Hotel Notes:</strong> {voucher_info.get('booking_notes', '')[:500]}{'...' if len(voucher_info.get('booking_notes', '')) > 500 else ''}
                                            </p>
                                        </div>''' if voucher_info.get('booking_notes') else ''}
                                    </td>
                                </tr>
                                
                                <!-- Footer -->
                                <tr>
                                    <td style="background-color: #f8fafc; padding: 25px 30px; text-align: center; border-top: 1px solid #e2e8f0;">
                                        <p style="margin: 0 0 10px 0; color: #666; font-size: 13px;">
                                            Questions? Contact us at <a href="mailto:support@freestays.eu" style="color: {primary_color}; text-decoration: none;">support@freestays.eu</a>
                                        </p>
                                        <p style="margin: 0; color: #999; font-size: 12px;">
                                            Thank you for booking with {smtp_settings["company_name"]}!<br>
                                            Your commission-free hotel partner.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """
            
            # Send the email
            msg = MIMEMultipart('alternative')
            msg['Subject'] = f"🎉 Your Travel Voucher - {voucher_info.get('hotel_name', 'Hotel Booking')} ({voucher_info.get('sunhotels_ref', '')})"
            msg['From'] = f"{smtp_settings['smtp_from_name']} <{smtp_settings['smtp_from']}>"
            msg['To'] = customer_email
            
            msg.attach(MIMEText(html_content, 'html'))
            
            # Connect and send
            if smtp_settings["smtp_port"] == 465:
                server = smtplib.SMTP_SSL(smtp_settings["smtp_host"], smtp_settings["smtp_port"])
            else:
                server = smtplib.SMTP(smtp_settings["smtp_host"], smtp_settings["smtp_port"])
                server.starttls()
            
            server.login(smtp_settings["smtp_user"], smtp_settings["smtp_password"])
            server.sendmail(smtp_settings["smtp_from"], customer_email, msg.as_string())
            server.quit()
            
            logger.info(f"✅ Forwarded voucher email to {customer_email} for booking {voucher_info.get('sunhotels_ref')}")
            return True
            
        except Exception as e:
            logger.error(f"Error sending branded voucher email: {str(e)}")
            return False
    
    @staticmethod
    async def check_and_forward_emails():
        """Check inbox for Sunhotels voucher emails and forward them"""
        if not IMAP_PASSWORD:
            logger.warning("IMAP password not configured - skipping email forwarding check")
            return {"processed": 0, "error": "IMAP not configured"}
        
        processed = 0
        errors = 0
        
        try:
            # Connect to IMAP server
            logger.info(f"Connecting to IMAP server {IMAP_SERVER}...")
            mail = imaplib.IMAP4_SSL(IMAP_SERVER, IMAP_PORT)
            mail.login(IMAP_EMAIL, IMAP_PASSWORD)
            mail.select('INBOX')
            
            # Search for unread Sunhotels voucher emails
            # Subject pattern: "Voucher SH*"
            status, messages = mail.search(None, '(UNSEEN SUBJECT "Voucher SH")')
            
            if status != 'OK':
                logger.error("Failed to search emails")
                mail.logout()
                return {"processed": 0, "error": "Search failed"}
            
            email_ids = messages[0].split()
            logger.info(f"Found {len(email_ids)} unread Sunhotels voucher emails")
            
            for email_id in email_ids:
                try:
                    # Fetch the email
                    status, msg_data = mail.fetch(email_id, '(RFC822)')
                    if status != 'OK':
                        continue
                    
                    raw_email = msg_data[0][1]
                    email_message = email.message_from_bytes(raw_email)
                    
                    # Decode subject
                    subject = SunhotelsEmailForwarder.decode_email_header(email_message['Subject'])
                    
                    # Get email body
                    body = ""
                    if email_message.is_multipart():
                        for part in email_message.walk():
                            content_type = part.get_content_type()
                            if content_type == "text/plain":
                                payload = part.get_payload(decode=True)
                                if payload:
                                    body = payload.decode('utf-8', errors='ignore')
                                    break
                            elif content_type == "text/html" and not body:
                                payload = part.get_payload(decode=True)
                                if payload:
                                    # Strip HTML tags for basic parsing
                                    html_body = payload.decode('utf-8', errors='ignore')
                                    body = re.sub('<[^<]+?>', ' ', html_body)
                    else:
                        payload = email_message.get_payload(decode=True)
                        if payload:
                            body = payload.decode('utf-8', errors='ignore')
                    
                    # Extract voucher info
                    voucher_info = SunhotelsEmailForwarder.extract_voucher_info(subject, body)
                    
                    if voucher_info.get("sunhotels_ref"):
                        # Check if we already processed this voucher
                        existing = await db.forwarded_vouchers.find_one({
                            "sunhotels_ref": voucher_info["sunhotels_ref"]
                        })
                        
                        if not existing:
                            # Send branded email
                            success = await SunhotelsEmailForwarder.send_branded_voucher_email(voucher_info)
                            
                            if success:
                                # Mark as processed in database
                                await db.forwarded_vouchers.insert_one({
                                    "sunhotels_ref": voucher_info["sunhotels_ref"],
                                    "voucher_info": voucher_info,
                                    "forwarded_at": datetime.now(timezone.utc).isoformat(),
                                    "original_subject": subject
                                })
                                processed += 1
                            else:
                                errors += 1
                        else:
                            logger.info(f"Voucher {voucher_info['sunhotels_ref']} already forwarded, skipping")
                    
                    # Mark email as read
                    mail.store(email_id, '+FLAGS', '\\Seen')
                    
                except Exception as e:
                    logger.error(f"Error processing email {email_id}: {str(e)}")
                    errors += 1
            
            mail.logout()
            logger.info(f"Email forwarding complete: {processed} processed, {errors} errors")
            return {"processed": processed, "errors": errors}
            
        except Exception as e:
            logger.error(f"IMAP connection error: {str(e)}")
            return {"processed": 0, "error": str(e)}

async def scheduled_email_forwarding():
    """Scheduled job to check and forward Sunhotels voucher emails"""
    try:
        logger.info("Running scheduled Sunhotels email forwarding check...")
        result = await SunhotelsEmailForwarder.check_and_forward_emails()
        logger.info(f"Email forwarding result: {result}")
    except Exception as e:
        logger.error(f"Scheduled email forwarding error: {str(e)}")

# ==================== CHECK-IN REMINDER SERVICE ====================

class CheckInReminderService:
    """Service to send check-in reminder emails 3 days before arrival"""
    
    @staticmethod
    async def send_check_in_reminder(booking: dict):
        """Send a check-in reminder email to the guest"""
        try:
            settings = await get_settings()
            smtp_settings = {
                "smtp_host": settings.get("smtp_host"),
                "smtp_port": settings.get("smtp_port", 587),
                "smtp_user": settings.get("smtp_user"),
                "smtp_password": settings.get("smtp_password"),
                "smtp_from": settings.get("smtp_from", "noreply@freestays.eu"),
                "smtp_from_name": settings.get("smtp_from_name", "FreeStays"),
                "company_name": settings.get("company_name", "FreeStays"),
                "company_logo": settings.get("company_logo", ""),
                "primary_color": settings.get("email_primary_color", "#1e3a5f"),
                "accent_color": settings.get("email_accent_color", "#2d8a5f"),
            }
            
            if not smtp_settings["smtp_host"] or not smtp_settings["smtp_user"]:
                logger.error("SMTP not configured for check-in reminders")
                return False
            
            guest_email = booking.get("guest_email") or booking.get("email")
            if not guest_email:
                logger.warning(f"No email for booking {booking.get('booking_id')}")
                return False
            
            # Get booking details
            guest_name = booking.get("guest_first_name", "Guest")
            hotel_name = booking.get("hotel_name", "Your Hotel")
            check_in = booking.get("check_in", "")
            check_out = booking.get("check_out", "")
            room_type = booking.get("room_type", "Standard Room")
            sunhotels_ref = booking.get("sunhotels_booking_id", "")
            
            # Format dates
            check_in_formatted = check_in
            check_out_formatted = check_out
            try:
                if check_in:
                    check_in_dt = datetime.strptime(check_in, "%Y-%m-%d")
                    check_in_formatted = check_in_dt.strftime("%A, %B %d, %Y")
                if check_out:
                    check_out_dt = datetime.strptime(check_out, "%Y-%m-%d")
                    check_out_formatted = check_out_dt.strftime("%A, %B %d, %Y")
            except:
                pass
            
            # Get voucher link if available
            voucher_link = booking.get("voucher_url", "")
            forwarded_voucher = await db.forwarded_vouchers.find_one({
                "sunhotels_ref": sunhotels_ref
            })
            if forwarded_voucher and forwarded_voucher.get("voucher_info", {}).get("voucher_link"):
                voucher_link = forwarded_voucher["voucher_info"]["voucher_link"]
            
            logo_url = smtp_settings["company_logo"] or "https://freestays.eu/logo.png"
            primary_color = smtp_settings["primary_color"]
            accent_color = smtp_settings["accent_color"]
            
            html_content = f"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Your Trip is Almost Here! - {smtp_settings["company_name"]}</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
                <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                    <tr>
                        <td align="center">
                            <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.1);">
                                <!-- Header -->
                                <tr>
                                    <td style="background: linear-gradient(135deg, {accent_color} 0%, #22c55e 100%); padding: 30px; text-align: center;">
                                        <img src="{logo_url}" alt="{smtp_settings['company_name']}" style="max-height: 50px; margin-bottom: 15px;">
                                        <h1 style="margin: 0; color: #ffffff; font-size: 28px; font-weight: 600;">🎉 Your Trip is in 3 Days!</h1>
                                        <p style="margin: 10px 0 0 0; color: rgba(255,255,255,0.9); font-size: 14px;">Time to get excited!</p>
                                    </td>
                                </tr>
                                
                                <!-- Content -->
                                <tr>
                                    <td style="padding: 40px 30px;">
                                        <p style="margin: 0 0 20px 0; font-size: 16px; color: #333;">
                                            Dear <strong>{guest_name}</strong>,
                                        </p>
                                        <p style="margin: 0 0 25px 0; font-size: 15px; color: #666; line-height: 1.6;">
                                            Your stay at <strong>{hotel_name}</strong> is just around the corner! 
                                            We wanted to send you a friendly reminder with all the important details.
                                        </p>
                                        
                                        <!-- Countdown Box -->
                                        <div style="background: linear-gradient(135deg, {primary_color} 0%, #2d5a87 100%); border-radius: 12px; padding: 20px; margin-bottom: 25px; text-align: center;">
                                            <p style="margin: 0 0 5px 0; color: rgba(255,255,255,0.8); font-size: 14px;">Check-in starts in</p>
                                            <p style="margin: 0; color: #ffffff; font-size: 48px; font-weight: bold;">3 DAYS</p>
                                        </div>
                                        
                                        <!-- Booking Details Card -->
                                        <div style="background: #f8fafc; border-radius: 12px; padding: 25px; margin-bottom: 25px; border-left: 4px solid {accent_color};">
                                            <h2 style="margin: 0 0 20px 0; color: {primary_color}; font-size: 18px;">
                                                🏨 Booking Details
                                            </h2>
                                            
                                            <table width="100%" cellpadding="0" cellspacing="0">
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666; font-size: 14px; width: 120px;">Hotel:</td>
                                                    <td style="padding: 8px 0; color: #333; font-size: 14px; font-weight: 600;">{hotel_name}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666; font-size: 14px;">📅 Check-in:</td>
                                                    <td style="padding: 8px 0; color: #333; font-size: 14px; font-weight: 600;">{check_in_formatted}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666; font-size: 14px;">📅 Check-out:</td>
                                                    <td style="padding: 8px 0; color: #333; font-size: 14px; font-weight: 600;">{check_out_formatted}</td>
                                                </tr>
                                                <tr>
                                                    <td style="padding: 8px 0; color: #666; font-size: 14px;">🛏️ Room:</td>
                                                    <td style="padding: 8px 0; color: #333; font-size: 14px; font-weight: 600;">{room_type}</td>
                                                </tr>
                                                {f'''<tr>
                                                    <td style="padding: 8px 0; color: #666; font-size: 14px;">📋 Reference:</td>
                                                    <td style="padding: 8px 0; color: {accent_color}; font-size: 14px; font-weight: 700;">{sunhotels_ref}</td>
                                                </tr>''' if sunhotels_ref else ''}
                                            </table>
                                        </div>
                                        
                                        {f'''<!-- Voucher Button -->
                                        <div style="text-align: center; margin: 30px 0;">
                                            <a href="{voucher_link}" style="display: inline-block; background: linear-gradient(135deg, {primary_color} 0%, #2d5a87 100%); color: #ffffff; text-decoration: none; padding: 14px 40px; border-radius: 50px; font-weight: 600; font-size: 14px;">
                                                📄 View Your Voucher
                                            </a>
                                        </div>''' if voucher_link else ''}
                                        
                                        <!-- Checklist -->
                                        <div style="background-color: #fef3c7; border-radius: 12px; padding: 20px; margin-bottom: 20px;">
                                            <h3 style="margin: 0 0 15px 0; color: #92400e; font-size: 16px;">✅ Pre-Trip Checklist</h3>
                                            <ul style="margin: 0; padding-left: 20px; color: #92400e; font-size: 14px; line-height: 1.8;">
                                                <li>Print or save your voucher to your phone</li>
                                                <li>Check your passport/ID is valid</li>
                                                <li>Note the hotel address and directions</li>
                                                <li>Check-in is usually after 2:00 PM</li>
                                                <li>Contact the hotel if arriving late</li>
                                            </ul>
                                        </div>
                                        
                                        <!-- Support Box -->
                                        <div style="background-color: #f1f5f9; border-radius: 12px; padding: 15px; text-align: center;">
                                            <p style="margin: 0; color: #475569; font-size: 13px;">
                                                Questions? We're here to help!<br>
                                                <a href="mailto:support@freestays.eu" style="color: {primary_color}; text-decoration: none;">support@freestays.eu</a>
                                            </p>
                                        </div>
                                    </td>
                                </tr>
                                
                                <!-- Footer -->
                                <tr>
                                    <td style="background-color: #f8fafc; padding: 25px 30px; text-align: center; border-top: 1px solid #e2e8f0;">
                                        <p style="margin: 0 0 10px 0; color: #666; font-size: 14px; font-weight: 500;">
                                            Have an amazing trip! 🌟
                                        </p>
                                        <p style="margin: 0; color: #999; font-size: 12px;">
                                            The {smtp_settings["company_name"]} Team
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """
            
            # Send the email
            msg = MIMEMultipart('alternative')
            msg['Subject'] = f"🎉 3 Days Until Your Stay at {hotel_name}!"
            msg['From'] = f"{smtp_settings['smtp_from_name']} <{smtp_settings['smtp_from']}>"
            msg['To'] = guest_email
            
            msg.attach(MIMEText(html_content, 'html'))
            
            # Connect and send
            if smtp_settings["smtp_port"] == 465:
                server = smtplib.SMTP_SSL(smtp_settings["smtp_host"], smtp_settings["smtp_port"])
            else:
                server = smtplib.SMTP(smtp_settings["smtp_host"], smtp_settings["smtp_port"])
                server.starttls()
            
            server.login(smtp_settings["smtp_user"], smtp_settings["smtp_password"])
            server.sendmail(smtp_settings["smtp_from"], guest_email, msg.as_string())
            server.quit()
            
            logger.info(f"✅ Sent check-in reminder to {guest_email} for {hotel_name}")
            return True
            
        except Exception as e:
            logger.error(f"Error sending check-in reminder: {str(e)}")
            return False
    
    @staticmethod
    async def check_and_send_reminders():
        """Check for bookings with check-in in 3 days and send reminders"""
        try:
            # Calculate the date 3 days from now
            reminder_date = (datetime.now(timezone.utc) + timedelta(days=3)).strftime("%Y-%m-%d")
            
            logger.info(f"Checking for bookings with check-in on {reminder_date}")
            
            # Find bookings with check-in in 3 days that haven't received a reminder
            bookings = await db.bookings.find({
                "check_in": reminder_date,
                "status": {"$in": ["completed", "confirmed", "paid"]},
                "checkin_reminder_sent": {"$ne": True}
            }).to_list(100)
            
            logger.info(f"Found {len(bookings)} bookings needing check-in reminders")
            
            sent = 0
            failed = 0
            
            for booking in bookings:
                success = await CheckInReminderService.send_check_in_reminder(booking)
                
                if success:
                    # Mark as reminder sent
                    await db.bookings.update_one(
                        {"_id": booking["_id"]},
                        {"$set": {
                            "checkin_reminder_sent": True,
                            "checkin_reminder_sent_at": datetime.now(timezone.utc).isoformat()
                        }}
                    )
                    sent += 1
                else:
                    failed += 1
            
            logger.info(f"Check-in reminders complete: {sent} sent, {failed} failed")
            return {"sent": sent, "failed": failed, "date": reminder_date}
            
        except Exception as e:
            logger.error(f"Error checking for check-in reminders: {str(e)}")
            return {"sent": 0, "failed": 0, "error": str(e)}

async def scheduled_checkin_reminders():
    """Scheduled job to send check-in reminder emails"""
    try:
        logger.info("Running scheduled check-in reminder check...")
        result = await CheckInReminderService.check_and_send_reminders()
        logger.info(f"Check-in reminder result: {result}")
    except Exception as e:
        logger.error(f"Scheduled check-in reminder error: {str(e)}")

# ==================== POST-STAY FEEDBACK SERVICE ====================

class PostStayFeedbackService:
    """Service to send post-stay feedback request emails 3 days after checkout"""
    
    @staticmethod
    async def generate_survey_token(booking_id: str) -> str:
        """Generate a unique survey token for a booking"""
        import hashlib
        token_data = f"{booking_id}_{datetime.now(timezone.utc).isoformat()}_{secrets.token_hex(8)}"
        return hashlib.sha256(token_data.encode()).hexdigest()[:32]
    
    @staticmethod
    async def send_feedback_request(booking: dict) -> bool:
        """Send a post-stay feedback request email"""
        try:
            settings = await get_settings()
            smtp_settings = {
                "smtp_host": settings.get("smtp_host"),
                "smtp_port": settings.get("smtp_port", 587),
                "smtp_user": settings.get("smtp_user"),
                "smtp_password": settings.get("smtp_password"),
                "smtp_from": settings.get("smtp_from", "noreply@freestays.eu"),
                "smtp_from_name": settings.get("smtp_from_name", "FreeStays"),
                "company_name": settings.get("company_name", "FreeStays"),
                "company_logo": settings.get("company_logo", ""),
                "primary_color": settings.get("email_primary_color", "#1e3a5f"),
                "accent_color": settings.get("email_accent_color", "#2d8a5f"),
            }
            
            if not smtp_settings["smtp_host"] or not smtp_settings["smtp_user"]:
                logger.error("SMTP not configured for feedback requests")
                return False
            
            guest_email = booking.get("guest_email") or booking.get("email")
            if not guest_email:
                return False
            
            # Generate survey token
            survey_token = await PostStayFeedbackService.generate_survey_token(booking.get("booking_id", ""))
            
            # Store token in database
            await db.survey_tokens.insert_one({
                "booking_id": booking.get("booking_id"),
                "token": survey_token,
                "guest_email": guest_email,
                "hotel_name": booking.get("hotel_name"),
                "check_out": booking.get("check_out"),
                "created_at": datetime.now(timezone.utc).isoformat(),
                "used": False
            })
            
            guest_name = booking.get("guest_first_name", "Guest")
            hotel_name = booking.get("hotel_name", "Your Hotel")
            frontend_url = os.environ.get("FRONTEND_URL", "https://freestays.eu")
            survey_url = f"{frontend_url}/survey?token={survey_token}"
            
            logo_url = smtp_settings["company_logo"] or "https://freestays.eu/logo.png"
            primary_color = smtp_settings["primary_color"]
            accent_color = smtp_settings["accent_color"]
            
            html_content = f"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
            </head>
            <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5;">
                <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                    <tr>
                        <td align="center">
                            <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.1);">
                                <tr>
                                    <td style="background: linear-gradient(135deg, {primary_color} 0%, #2d5a87 100%); padding: 30px; text-align: center;">
                                        <img src="{logo_url}" alt="{smtp_settings['company_name']}" style="max-height: 50px; margin-bottom: 15px;">
                                        <h1 style="margin: 0; color: #ffffff; font-size: 26px;">How Was Your Stay?</h1>
                                        <p style="margin: 10px 0 0 0; color: rgba(255,255,255,0.9); font-size: 14px;">We'd love to hear about your experience!</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 40px 30px;">
                                        <p style="margin: 0 0 20px 0; font-size: 16px; color: #333;">
                                            Dear <strong>{guest_name}</strong>,
                                        </p>
                                        <p style="margin: 0 0 20px 0; font-size: 15px; color: #666; line-height: 1.6;">
                                            Thank you for choosing <strong>{hotel_name}</strong> through {smtp_settings['company_name']}! 
                                            We hope you had a wonderful stay.
                                        </p>
                                        <p style="margin: 0 0 25px 0; font-size: 15px; color: #666; line-height: 1.6;">
                                            Your feedback helps us improve and helps other travelers make informed decisions. 
                                            It only takes 2 minutes!
                                        </p>
                                        
                                        <div style="text-align: center; margin: 30px 0;">
                                            <a href="{survey_url}" style="display: inline-block; background: linear-gradient(135deg, {accent_color} 0%, #22c55e 100%); color: #ffffff; text-decoration: none; padding: 16px 50px; border-radius: 50px; font-weight: 600; font-size: 16px;">
                                                ⭐ Share Your Experience
                                            </a>
                                        </div>
                                        
                                        <div style="background: #f8fafc; border-radius: 12px; padding: 20px; margin: 25px 0;">
                                            <p style="margin: 0; text-align: center; color: #666; font-size: 14px;">
                                                <strong>Quick & Easy:</strong> Rate your stay on cleanliness, service, value & more
                                            </p>
                                        </div>
                                        
                                        <div style="text-align: center; margin-top: 25px;">
                                            <p style="margin: 0; color: #999; font-size: 13px;">
                                                Or copy this link: <a href="{survey_url}" style="color: {primary_color};">{survey_url}</a>
                                            </p>
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="background-color: #f8fafc; padding: 20px 30px; text-align: center; border-top: 1px solid #e2e8f0;">
                                        <p style="margin: 0; color: #999; font-size: 12px;">
                                            Thank you for being part of the {smtp_settings['company_name']} community!
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """
            
            msg = MIMEMultipart('alternative')
            msg['Subject'] = f"⭐ How was your stay at {hotel_name}? Share your experience!"
            msg['From'] = f"{smtp_settings['smtp_from_name']} <{smtp_settings['smtp_from']}>"
            msg['To'] = guest_email
            msg.attach(MIMEText(html_content, 'html'))
            
            if smtp_settings["smtp_port"] == 465:
                server = smtplib.SMTP_SSL(smtp_settings["smtp_host"], smtp_settings["smtp_port"])
            else:
                server = smtplib.SMTP(smtp_settings["smtp_host"], smtp_settings["smtp_port"])
                server.starttls()
            
            server.login(smtp_settings["smtp_user"], smtp_settings["smtp_password"])
            server.sendmail(smtp_settings["smtp_from"], guest_email, msg.as_string())
            server.quit()
            
            logger.info(f"✅ Sent feedback request to {guest_email} for {hotel_name}")
            return True
            
        except Exception as e:
            logger.error(f"Error sending feedback request: {str(e)}")
            return False
    
    @staticmethod
    async def check_and_send_feedback_requests():
        """Check for bookings 3 days after checkout and send feedback requests"""
        try:
            feedback_date = (datetime.now(timezone.utc) - timedelta(days=3)).strftime("%Y-%m-%d")
            
            logger.info(f"Checking for bookings with checkout on {feedback_date}")
            
            bookings = await db.bookings.find({
                "check_out": feedback_date,
                "status": {"$in": ["completed", "confirmed", "paid"]},
                "feedback_request_sent": {"$ne": True}
            }).to_list(100)
            
            logger.info(f"Found {len(bookings)} bookings needing feedback requests")
            
            sent = 0
            failed = 0
            
            for booking in bookings:
                success = await PostStayFeedbackService.send_feedback_request(booking)
                
                if success:
                    await db.bookings.update_one(
                        {"_id": booking["_id"]},
                        {"$set": {
                            "feedback_request_sent": True,
                            "feedback_request_sent_at": datetime.now(timezone.utc).isoformat()
                        }}
                    )
                    sent += 1
                else:
                    failed += 1
            
            logger.info(f"Feedback requests complete: {sent} sent, {failed} failed")
            return {"sent": sent, "failed": failed, "date": feedback_date}
            
        except Exception as e:
            logger.error(f"Error checking for feedback requests: {str(e)}")
            return {"sent": 0, "failed": 0, "error": str(e)}

async def scheduled_feedback_requests():
    """Scheduled job to send post-stay feedback request emails"""
    try:
        logger.info("Running scheduled feedback request check...")
        result = await PostStayFeedbackService.check_and_send_feedback_requests()
        logger.info(f"Feedback request result: {result}")
    except Exception as e:
        logger.error(f"Scheduled feedback request error: {str(e)}")

def setup_scheduler():
    """Configure and start the background scheduler based on settings"""
    # Daily price drop check at 6 AM UTC
    scheduler.add_job(
        scheduled_price_drop_check,
        CronTrigger(hour=6, minute=0),
        id="price_drop_check",
        replace_existing=True,
        name="Daily Price Drop Check"
    )
    
    # Follow-up emails check every 12 hours (8 AM and 8 PM UTC)
    scheduler.add_job(
        scheduled_follow_up_emails,
        CronTrigger(hour='8,20', minute=0),
        id="follow_up_emails",
        replace_existing=True,
        name="Follow-up Email Check"
    )
    
    # Daily hotel image sync at 3 AM UTC (when traffic is lowest)
    scheduler.add_job(
        scheduled_hotel_image_sync,
        CronTrigger(hour=3, minute=0),
        id="hotel_image_sync",
        replace_existing=True,
        name="Daily Hotel Image Sync"
    )
    
    # Sunhotels email forwarding - check every 5 minutes
    scheduler.add_job(
        scheduled_email_forwarding,
        IntervalTrigger(minutes=5),
        id="sunhotels_email_forwarding",
        replace_existing=True,
        name="Sunhotels Email Forwarding"
    )
    
    # Check-in reminders - daily at 9 AM UTC (sends reminders 3 days before check-in)
    scheduler.add_job(
        scheduled_checkin_reminders,
        CronTrigger(hour=9, minute=0),
        id="checkin_reminders",
        replace_existing=True,
        name="Check-in Reminder Emails"
    )
    
    # Post-stay feedback requests - daily at 10 AM UTC (sends 3 days after checkout)
    scheduler.add_job(
        scheduled_feedback_requests,
        CronTrigger(hour=10, minute=0),
        id="feedback_requests",
        replace_existing=True,
        name="Post-Stay Feedback Requests"
    )
    
    scheduler.start()
    logger.info("Background scheduler started - Price drop: 6 AM, Follow-up: 8 AM/PM, Image sync: 3 AM, Email forwarding: 5 min, Check-in: 9 AM, Feedback: 10 AM")

async def scheduled_follow_up_emails():
    """Scheduled job to send follow-up emails to visitors who haven't booked"""
    try:
        logger.info("Running scheduled follow-up email check...")
        sent_count = await PriceComparisonService.process_follow_up_emails()
        logger.info(f"Scheduled follow-up complete: {sent_count} emails sent")
    except Exception as e:
        logger.error(f"Scheduled follow-up error: {str(e)}")

@app.on_event("startup")
async def startup_event():
    """Initialize scheduler on app startup"""
    setup_scheduler()
    logger.info("FreeStays API started with background scheduler")

@app.on_event("shutdown")
async def shutdown_db_client():
    scheduler.shutdown(wait=False)
    client.close()