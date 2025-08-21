# DoSo Authentication Setup Guide

## Overview
This guide will help you set up authentication for the DoSo platform using Google, GitHub, and Discord OAuth providers.

## Prerequisites
- Node.js and npm installed
- A Google Cloud Console account
- A GitHub account (for GitHub OAuth)
- A Discord account (for Discord OAuth)
- A MySQL database (already configured in your project)

## Step 1: Generate AUTH_SECRET

First, generate a secure secret for NextAuth:

```bash
node scripts/generate-secret.js
```

Copy the generated `AUTH_SECRET` to your `.env` file.

## Step 2: Set Up Google OAuth

### 2.1 Create Google Cloud Project
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Enable the Google+ API (or Google Identity API)

### 2.2 Create OAuth 2.0 Credentials
1. Go to "APIs & Services" → "Credentials"
2. Click "Create Credentials" → "OAuth 2.0 Client IDs"
3. Set Application Type to "Web application"
4. Add Authorized Redirect URIs:
   - `http://localhost:3000/api/auth/callback/google` (development)
   - `https://yourdomain.com/api/auth/callback/google` (production)
5. Copy the Client ID and Client Secret

### 2.3 Add to Environment Variables
Add these to your `.env` file:
```env
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
```

## Step 3: Set Up GitHub OAuth

### 3.1 Create GitHub OAuth App
1. Go to [GitHub Settings → Developer settings → OAuth Apps](https://github.com/settings/developers)
2. Click "New OAuth App"
3. Fill in the details:
   - Application name: "DoSo"
   - Homepage URL: `http://localhost:3000` (development)
   - Authorization callback URL: `http://localhost:3000/api/auth/callback/github`
4. Copy the Client ID and Client Secret

### 3.2 Add to Environment Variables
Add these to your `.env` file:
```env
GITHUB_CLIENT_ID=your-github-client-id
GITHUB_CLIENT_SECRET=your-github-client-secret
```

## Step 4: Set Up Discord OAuth

### 4.1 Create Discord Application
1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Click "New Application"
3. Go to "OAuth2" → "General"
4. Copy the Client ID and Client Secret
5. Add redirect URI: `http://localhost:3000/api/auth/callback/discord`

### 4.2 Add to Environment Variables
Add these to your `.env` file:
```env
AUTH_DISCORD_ID=your-discord-client-id
AUTH_DISCORD_SECRET=your-discord-client-secret
```

## Step 5: Complete Environment Setup

Your `.env` file should look like this:

```env
# Authentication
AUTH_SECRET=your-generated-secret-here

# Google OAuth
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret

# GitHub OAuth
GITHUB_CLIENT_ID=your-github-client-id
GITHUB_CLIENT_SECRET=your-github-client-secret

# Discord OAuth
AUTH_DISCORD_ID=your-discord-client-id
AUTH_DISCORD_SECRET=your-discord-client-secret

# Database
DATABASE_URL=your-mysql-database-url

# Environment
NODE_ENV=development
```

## Step 6: Test Authentication

1. Start your development server:
   ```bash
   npm run dev
   ```

2. Navigate to `http://localhost:3000/auth/signin`

3. Test each authentication provider:
   - Click "Continue with Google" to test Google OAuth
   - Click "Continue with GitHub" to test GitHub OAuth
   - Click "Continue with Discord" to test Discord OAuth

## Troubleshooting

### Common Issues

1. **"Invalid redirect URI" error**
   - Ensure the redirect URI in your OAuth app matches exactly
   - Check for trailing slashes or protocol mismatches

2. **"Client ID not found" error**
   - Verify your environment variables are set correctly
   - Restart your development server after adding environment variables

3. **Database connection issues**
   - Ensure your MySQL database is running
   - Check your DATABASE_URL format
   - Run `npm run db:push` to sync your database schema

### Environment Variable Validation

The app will validate your environment variables on startup. If any required variables are missing, you'll see clear error messages in the console.

## Security Notes

- Never commit your `.env` file to version control
- Keep your OAuth client secrets secure
- Use different OAuth apps for development and production
- Regularly rotate your AUTH_SECRET in production

## Next Steps

Once authentication is working:
1. Test user registration and login flow
2. Verify user data is being stored in your database
3. Test the dashboard access after authentication
4. Implement user profile management
5. Add role-based access control

## Support

If you encounter issues:
1. Check the browser console for errors
2. Check the server console for authentication errors
3. Verify all environment variables are set correctly
4. Ensure your OAuth apps are configured properly
