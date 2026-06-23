# ClassMate AI - Frontend

ClassMate AI is an intelligent assignment assistant that creates a seamless flow between Google Classroom and AI-driven drafting. The frontend is built using Next.js, integrating closely with Google Classroom through OAuth.

## Architecture and Flow

- **Framework**: Next.js (App Router).
- **Authentication**: Uses \
ext-auth\ to perform a Google OAuth dance yielding Google id/access/refresh tokens, which are then passed to our backend for JWT exchange.
- **Styling**: Tailwind CSS v4.

### Frontend - Backend Interaction
1. Start at \/login\, click "Continue with Google".
2. \
ext-auth\ handles the Google handshake, requesting the necessary Classroom/Drive scopes.
3. Once authenticated locally, the Next.js frontend forwards the tokens (idToken, accessToken, refreshToken) back to the Python backend via \/api/auth/google/\.
4. The backend verifies the exchange, persists tokens securely, and issues its own JWT to the UI.
5. The frontend handles API calls leveraging the custom JWT.

## Environment Variable Setup

Copy \.env.example\ to \.env.local\ to run tests and fill in your keys.

\\\ash
cp .env.example .env.local
\\\

## Installation and Running Instructions

1. Install dependencies:
\\\ash
npm install
\\\

2. Run the development server:
\\\ash
npm run dev
\\\

3. Build for production:
\\\ash
npm run build
npm start
\\\

## Important Implementation Details and Dependencies

- **Tailwind CSS v4**: Styling relies on \@import "tailwindcss";\ in \globals.css\.
- **Classroom Scopes**: Modifying Classroom or Drive scopes requires re-auth.
- Ensure the Backend is running when attempting to test authentication.

