#!/usr/bin/env node

/**
 * Script to generate a secure AUTH_SECRET for NextAuth
 * Run this with: node scripts/generate-secret.js
 */

import crypto from 'crypto';

// Generate a random 32-byte hex string
const secret = crypto.randomBytes(32).toString('hex');

console.log('🔐 Generated AUTH_SECRET for your .env file:');
console.log('');
console.log(`AUTH_SECRET=${secret}`);
console.log('');
console.log('📝 Copy this line to your .env file');
console.log('⚠️  Keep this secret secure and never commit it to version control');
