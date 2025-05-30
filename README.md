# TravelWise - Travel Agency Management System

TravelWise is a comprehensive travel agency management system that connects travelers with travel agencies, facilitating seamless booking and payment processes. The platform enables agencies to showcase their travel deals while providing users with a secure and efficient way to book and pay for their travel experiences.

## Features

### For Travelers

- Browse and search travel deals
- Filter deals by location, price, duration, and more
- Book travel packages
- Secure payment processing with Stripe
- Real-time chat with travel agencies
- Manage bookings and payments
- Review and rate travel experiences
- Create wishlists of favorite deals

### For Travel Agencies

- Create and manage travel deals
- Handle booking requests
- Process payments securely
- Real-time chat with customers
- Track booking statistics
- Manage agency profile
- View payment history
- Handle refunds

## Tech Stack

### Frontend

- Angular 19
- TypeScript
- HTML5/CSS3
- Stripe.js for payment processing
- Angular Material for UI components

### Backend

- .NET 9
- C#
- SQL Server
- Entity Framework Core
- Stripe API integration
- JWT Authentication

## Prerequisites

- Node.js (v18 or higher)
- .NET 8 SDK
- SQL Server
- Stripe Account
- Visual Studio 2022 or VS Code

## Installation

1. Clone the repository:

```bash
git clone https://github.com/sAayush/TravelWise.git
cd TravelWise
```

2. Backend Setup:

```bash
cd backend/Backend
dotnet restore
dotnet build
```

3. Frontend Setup:

```bash
cd frontend
npm install
```

4. Configure Environment Variables:

   - Create `appsettings.json` in the backend project
   - Create `environment.ts` in the frontend project
   - Add necessary configuration (database connection, Stripe keys, etc.)

5. Database Setup:
   - Run the database migrations
   - Seed initial data if required

## Running the Application

1. Start the Backend:

```bash
cd backend/Backend
dotnet run
```

2. Start the Frontend:

```bash
cd frontend
ng serve
```

3. Access the application at `http://localhost:4200`

## Payment Integration

The application uses Stripe for payment processing:

1. Stripe Connect for agency payouts
2. Stripe Elements for secure payment collection
3. Webhook handling for payment status updates
4. Support for multiple currencies
5. Automated commission calculations

## Security Features

- JWT-based authentication
- Role-based access control
- Secure password hashing
- HTTPS enforcement
- SQL injection prevention
- XSS protection
- CSRF protection

## API Documentation

The API documentation is available at `8080/swagger/index.html` when running the backend server.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support, email aayushsoni1239@gmail.com or create an issue in the repository.

## Acknowledgments

- Stripe for payment processing
- Angular team for the frontend framework
- .NET team for the backend framework
