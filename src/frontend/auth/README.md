# NIE Template - Auth Application

The authentication frontend for the NIE Template project. A lightweight Vue 3 application handling user login and session management.

## Overview

This is a dedicated authentication application that provides:

- **Login Page**: User authentication interface
- **Portal SSO Entry**: One-click handoff from the portal into the same session flow
- **Session Management**: Cookie-based session handling
- **Redirect Flow**: Seamless redirect to main application after login

## Tech Stack

- **Vue 3** with Composition API
- **TypeScript** for type safety
- **Vite** for fast development and building
- **Tailwind CSS** for styling

## Project Structure

```
src/
├── assets/       # Static assets
├── components/   # Vue components (LoginForm, etc.)
└── main.ts       # Application entry point
```

## Why Separate Auth App?

The authentication application is intentionally separate to:

1. **Security Isolation**: Keep auth logic separate from main application
2. **Independent Deployment**: Deploy auth changes without affecting main app
3. **Simplified Session Flow**: Clear separation of concerns
4. **Multi-App Support**: Single auth service for multiple applications

## For New Projects

**Keep this application largely unchanged.** You may want to:

- Update branding/styling to match your project
- Modify the login form layout if needed
- Update environment variables for your domain

## Scripts

```bash
# Development server
pnpm dev

# Build for staging
pnpm build:staging

# Build for production
pnpm build:production
```

## Environment Variables

Create `.env.development` and `.env.production` files:

```env
VITE_AUTH_API_URL=http://localhost:5001
VITE_DASHBOARD_URL=http://localhost:8001/
VITE_PORTAL_SSO_ENABLED=false
VITE_COOKIE_DOMAIN=localhost
VITE_COOKIE_SESSION_KEY=NieTemplate-SessionToken
VITE_COOKIE_USER_KEY=NieTemplateUserId
```

## Default Ports

- Auth Frontend: `http://localhost:8002`
- Auth API: `http://localhost:5001`
