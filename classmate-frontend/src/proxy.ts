import { withAuth } from "next-auth/middleware"

export default withAuth({
  callbacks: {
    authorized: ({ token }) => !!token,
  },
})

/**
 * Routes listed here are protected — unauthenticated visitors are
 * automatically redirected to /login by NextAuth middleware.
 * No need for useEffect redirect checks in page components.
 */
export const config = {
  matcher: [
    '/dashboard/:path*',
    '/settings/:path*',
  ],
};