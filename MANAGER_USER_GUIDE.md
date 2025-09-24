# SkillSnap Manager User Guide

## 🎉 Manager User Successfully Added!

A new seed user with Manager role has been created and can now access the Performance Metrics dashboard.

## 📋 Manager User Credentials

**Email**: `manager@skillsnap.com`  
**Password**: `Manager123!`  
**Role**: Manager  
**Name**: Performance Manager  

## 🚀 Testing the Manager User

### 1. Access the Application
- Frontend: http://localhost:5008
- Backend API: http://localhost:5282

### 2. Login as Manager
1. Navigate to the login page
2. Enter the Manager credentials:
   - Email: `manager@skillsnap.com`
   - Password: `Manager123!`
3. Click "Login"

### 3. Access Performance Metrics
1. After logging in as Manager, you'll see the **Metrics** menu item in the navigation (with speedometer icon)
2. Click on **Metrics** to access the Performance Dashboard
3. The dashboard includes:
   - **Real-time Performance Statistics**
   - **Cache Performance Metrics** (hit/miss ratios)
   - **Database Query Performance**
   - **Circuit Breaker Status**
   - **Load Testing Tools**

## 🔒 Role-Based Security Testing

### Manager Access ✅
- **Manager users** can access `/metrics`
- Full dashboard with all performance monitoring tools
- Real-time metrics and analytics

### Non-Manager Access ❌
- **Regular users** (like demo@skillsnap.com) cannot access `/metrics`
- Professional "Access Denied" page with login options
- Secure role-based authorization working correctly

## 👥 All Seed Users

| User | Email | Password | Role | Purpose |
|------|-------|----------|------|---------|
| Admin | admin@skillsnap.com | Admin123! | Admin | System administration |
| Manager | manager@skillsnap.com | Manager123! | Manager | Performance metrics access |
| Demo | demo@skillsnap.com | Demo123! | User | Regular user testing |

## 🧪 Testing Scenarios

### 1. **Manager Dashboard Access**
- Login with manager@skillsnap.com
- Navigate to Metrics
- Verify real-time dashboard loads
- Test auto-refresh functionality

### 2. **Role-Based Authorization**
- Login with demo@skillsnap.com (User role)
- Try to access /metrics directly
- Verify "Access Denied" page appears
- Test navigation back to dashboard

### 3. **Performance Monitoring**
- Use the "Simulate Load" feature
- Watch cache hit/miss ratios
- Monitor database query performance
- Test circuit breaker functionality

## 🎯 Key Features Implemented

- ✅ **Manager Role Seeding**: Automatic creation of manager user
- ✅ **Role-Based Authorization**: [Authorize(Roles = "Manager")] attribute
- ✅ **Performance Dashboard**: Real-time metrics monitoring  
- ✅ **Professional UI**: Unauthorized access handling
- ✅ **Enterprise Security**: Secure role-based access control

## 🔧 Technical Details

### Database Seeding
- Manager user created automatically on first run
- Integrated with existing DbInitializer.cs
- Portfolio and user data created together

### Authorization Implementation
- AuthorizeView component with Manager role filter
- Professional unauthorized access page
- Secure backend API endpoints with [Authorize(Roles = "Manager")]

### Performance Metrics
- Real-time cache performance tracking
- Database query optimization monitoring
- Circuit breaker pattern implementation
- Load testing and simulation tools

### UI Enhancements
- Bootstrap Icons CDN added for proper icon display
- Speedometer icon (bi-speedometer2) for Performance Metrics
- Professional styling throughout the application

---
**Ready for Production**: The Manager user seeding and role-based metrics dashboard is fully operational! 🚀