# SecureBookingSystem

A secure ASP.NET Core MVC Booking Application with RBAC, Audit Logging, and OWASP security practices.

## How to Run

1. **Navigate to the project directory**:
   ```powershell
   cd C:\Users\IanQal\.gemini\antigravity\scratch\SecureBookingSystem
   ```

2. **Run the application**:
   ```powershell
   dotnet run
   ```

3. **Access the application**:
   Open your browser and navigate to `https://localhost:5001`.

## Default Credentials

The application seeds a default Administrator account on startup:

- **Email**: `admin@securebooking.com`
- **Password**: `StrongPassword123!`

## Features

- **Role-Based Access Control (RBAC)**: Admin vs User roles.
- **Secure Bookings**: Users can only see their own bookings. Admins can manage all.
- **Audit Logs**: Admins can view logs of critical actions.
- **Security**: HTTPS, Security Headers, Input Validation, CSRF Protection.
