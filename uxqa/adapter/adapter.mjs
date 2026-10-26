// ArdaNova session adapter: guard | seed <personas.json> <out.json> | link <handle>
// Contract + refusal rules: ux-qa-research reference/session-adapters.md; see uxqa/study.md.
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';

const BASE = 'https://ardanova-client-production.up.railway.app';
const GUARD_FIELD = 'deploymentKey';
const GUARD_VALUE = 'qa-ardanova-a876ca12';
const HANDLE_RULE = /^qa-[a-z0-9-]+$/;

// Railway coordinates (read-only CLI usage only: `railway ssh` into the one
// service this study is allowed to touch). See uxqa/RUNBOOK.md for provenance.
const RAILWAY_PROJECT = 'a876ca12-bbf8-4c84-bce7-2c4268f3296c';
const RAILWAY_ENVIRONMENT = '3443bd75-e5e6-4bf9-9795-a7f2ae809b14';
const RAILWAY_SERVICE = '9e73d11b-07aa-41e9-bc3a-38b92c480d94';
// The Railway environment is literally named "production" (the study target is
// the released env used for QA, per uxqa/study.md) - this is the in-container
// half of the two-guard check; DEPLOYMENT_KEY is the other half.
const EXPECTED_RAILWAY_ENV_NAME = 'production';

// Auth.js v5 config (ardanova-client/src/server/auth/config.ts): session.strategy
// "jwt", cookie name below. The `salt` Auth.js uses for key derivation is the
// cookie name itself (v5 requirement - see @auth/core/jwt.js `getDerivedEncryptionKey`).
const SESSION_COOKIE_NAME = '__Secure-authjs.session-token';
const SESSION_COOKIE_DOMAIN = 'ardanova-client-production.up.railway.app';
const SESSION_MAX_AGE_SECONDS = 7 * 24 * 60 * 60;

async function checkGuard() {
	let body;
	try {
		const res = await fetch(BASE + '/api/health', { signal: AbortSignal.timeout(20_000) });
		body = await res.json();
	} catch (e) {
		return { ok: false, reason: `health unreadable (${String(e.message || e).slice(0, 60)})` };
	}
	if (body?.[GUARD_FIELD] !== GUARD_VALUE) {
		return { ok: false, reason: `${GUARD_FIELD}=${JSON.stringify(body?.[GUARD_FIELD] ?? null)} (expected the QA marker; is the post-azoa-sdk client deployed?)` };
	}
	return { ok: true, body };
}

async function guard() {
	const result = await checkGuard();
	if (!result.ok) {
		console.log(`REFUSED ${result.reason}`);
		process.exitCode = 2;
		return;
	}
	console.log(`OK ${GUARD_FIELD}=${GUARD_VALUE} commit=${result.body.commit ?? 'unknown'}`);
}

// Pushes `scriptText` over stdin to a /tmp file inside the Ardanova-Client
// container and runs it with node, then removes it. Secrets never touch argv;
// only this process's in-memory stdout buffer sees the result (never echoed).
function runInContainer(scriptText, remoteName, timeoutMs) {
	const remoteCmd = `cat > /tmp/${remoteName} && node /tmp/${remoteName}; rc=$?; rm -f /tmp/${remoteName}; exit $rc`;
	const cmdLine = `railway ssh --project "${RAILWAY_PROJECT}" --environment "${RAILWAY_ENVIRONMENT}" --service "${RAILWAY_SERVICE}" -- sh -c "${remoteCmd.replace(/"/g, '\\"')}"`;
	return spawnSync(cmdLine, {
		shell: true,
		input: scriptText,
		encoding: 'utf8',
		timeout: timeoutMs,
		maxBuffer: 10 * 1024 * 1024,
	});
}

// Builds the in-container CommonJS script: re-checks the guard from inside,
// upserts each qa-* User by id (idempotent), and mints a real Auth.js v5
// session JWE per persona using pure node:crypto (no jose/@auth/core needed -
// the standalone build's node_modules does not carry them as externals; this
// re-implements @auth/core/jwt.js `encode()` exactly - verified by round-trip
// through the real `decode()` before shipping, see report).
function buildSeedScript(personas) {
	return `'use strict';
const { PrismaClient } = require('/app/node_modules/@prisma/client');
const crypto = require('node:crypto');

const GUARD_VALUE = ${JSON.stringify(GUARD_VALUE)};
const EXPECTED_RAILWAY_ENV = ${JSON.stringify(EXPECTED_RAILWAY_ENV_NAME)};
const COOKIE_NAME = ${JSON.stringify(SESSION_COOKIE_NAME)};
const MAX_AGE_SECONDS = ${SESSION_MAX_AGE_SECONDS};
const HANDLE_RULE = /^qa-[a-z0-9-]+$/;
const PERSONAS = ${JSON.stringify(personas)};

// Thrown by refuse() and caught in the IIFE below so Prisma always gets a
// clean $disconnect() and the process exits via exitCode (not process.exit())
// - forcing immediate exit here has crashed libuv on Node+Windows when any
// async handle (fetch/timers/engine sockets) is still open; letting the event
// loop drain naturally is the reliable path (verified locally, see report).
class Refusal extends Error {}
function refuse(msg) {
  console.log('REFUSED ' + msg);
  throw new Refusal(msg);
}

function deriveEncryptionKey(secret, salt) {
  const info = 'Auth.js Generated Encryption Key (' + salt + ')';
  return Buffer.from(crypto.hkdfSync('sha256', Buffer.from(secret, 'utf8'), Buffer.from(salt, 'utf8'), Buffer.from(info, 'utf8'), 64));
}

// Mirrors @auth/core/jwt.js encode(): JWE alg "dir", enc "A256CBC-HS512"
// (RFC 7518 Sec 5.2.2.1 AES_256_CBC_HMAC_SHA512). Key K (64 bytes) splits into
// MAC_KEY = K[0:32], ENC_KEY = K[32:64]; tag = HMAC-SHA512(MAC_KEY, AAD||IV||CT||AL)[0:32].
function encodeAuthJwt(claims, secret, salt, maxAgeSeconds) {
  const key = deriveEncryptionKey(secret, salt);
  const macKey = key.subarray(0, 32);
  const encKey = key.subarray(32, 64);
  const now = Math.floor(Date.now() / 1000);
  const payload = { ...claims, iat: now, exp: now + maxAgeSeconds, jti: crypto.randomUUID() };
  const header = { alg: 'dir', enc: 'A256CBC-HS512' };
  const headerB64 = Buffer.from(JSON.stringify(header), 'utf8').toString('base64url');
  const aad = Buffer.from(headerB64, 'ascii');
  const iv = crypto.randomBytes(16);
  const cipher = crypto.createCipheriv('aes-256-cbc', encKey, iv);
  const ciphertext = Buffer.concat([cipher.update(Buffer.from(JSON.stringify(payload), 'utf8')), cipher.final()]);
  const alBuf = Buffer.alloc(8);
  alBuf.writeBigUInt64BE(BigInt(aad.length * 8));
  const tag = crypto.createHmac('sha512', macKey).update(Buffer.concat([aad, iv, ciphertext, alBuf])).digest().subarray(0, 32);
  return [headerB64, '', iv.toString('base64url'), ciphertext.toString('base64url'), tag.toString('base64url')].join('.');
}

const prisma = new PrismaClient();

(async () => {
  try {
    // In-container guard: the public /api/health check already ran locally;
    // this re-checks the container's own identity before any write (two
    // failure modes - local public identity, in-container private identity).
    if (process.env.RAILWAY_ENVIRONMENT_NAME !== EXPECTED_RAILWAY_ENV || process.env.DEPLOYMENT_KEY !== GUARD_VALUE) {
      refuse('in-container guard mismatch (RAILWAY_ENVIRONMENT_NAME=' + process.env.RAILWAY_ENVIRONMENT_NAME + ')');
    }
    if (!process.env.AUTH_SECRET) refuse('AUTH_SECRET missing in container');

    const out = [];
    for (const persona of PERSONAS) {
      if (!HANDLE_RULE.test(persona.handle)) refuse('bad handle reached container: ' + persona.handle);
      const email = persona.handle + '@qa.ardanova.test';
      const name = persona.name ?? persona.handle;
      const userType = persona.seed?.userType ?? 'SUPPORTER';
      const isVerified = persona.seed?.isVerified ?? false;
      const verificationLevel = persona.seed?.verificationLevel ?? 'ANONYMOUS';
      const data = { email, name, role: 'INDIVIDUAL', userType, isVerified, verificationLevel };
      const user = await prisma.user.upsert({
        where: { id: persona.handle },
        update: data,
        create: { id: persona.handle, ...data },
      });
      const value = encodeAuthJwt(
        { id: user.id, email: user.email, name: user.name, role: user.role, userType: user.userType, isVerified: user.isVerified, verificationLevel: user.verificationLevel },
        process.env.AUTH_SECRET,
        COOKIE_NAME,
        MAX_AGE_SECONDS,
      );
      out.push({ handle: persona.handle, value });
    }
    console.log('SESSIONS ' + JSON.stringify(out));
  } catch (e) {
    if (e instanceof Refusal) {
      process.exitCode = 2;
    } else {
      console.log('ERROR ' + String(e?.message ?? e).slice(0, 300));
      process.exitCode = 1;
    }
  } finally {
    await prisma.$disconnect();
  }
})();
`;
}

function extractLine(stdout, prefix) {
	if (!stdout) return null;
	for (const line of stdout.split('\n')) {
		if (line.startsWith(prefix)) return line.slice(prefix.length);
	}
	return null;
}

async function seed(personasPath, outPath) {
	const guardResult = await checkGuard();
	if (!guardResult.ok) {
		console.log(`REFUSED ${guardResult.reason}`);
		process.exitCode = 2;
		return;
	}

	const personas = JSON.parse(fs.readFileSync(personasPath, 'utf8'));
	const bad = personas.filter((p) => !HANDLE_RULE.test(p.handle));
	if (bad.length) {
		console.log(`REFUSED non-persona handles: ${bad.map((p) => p.handle).join(', ')}`);
		process.exitCode = 2;
		return;
	}
	if (personas.length === 0) {
		console.log('REFUSED empty personas file');
		process.exitCode = 2;
		return;
	}

	const remoteName = `uxqa-seed-${process.pid}-${Date.now()}.cjs`;
	const script = buildSeedScript(personas);
	const timeoutMs = Math.max(60_000, personas.length * 5_000);
	const res = runInContainer(script, remoteName, timeoutMs);

	if (res.error) {
		console.log(`REFUSED railway ssh failed to run (${res.error.code ?? res.error.message})`);
		process.exitCode = 1;
		return;
	}

	const refused = extractLine(res.stdout, 'REFUSED ');
	if (refused !== null) {
		console.log(`REFUSED ${refused}`);
		process.exitCode = 2;
		return;
	}
	const errored = extractLine(res.stdout, 'ERROR ');
	if (errored !== null) {
		console.log(`REFUSED in-container seed error: ${errored}`);
		process.exitCode = 1;
		return;
	}

	const sessionsLine = extractLine(res.stdout, 'SESSIONS ');
	if (sessionsLine === null || res.status !== 0) {
		console.log('REFUSED in-container seed produced no SESSIONS line (unexpected failure)');
		process.exitCode = 1;
		return;
	}

	let minted;
	try {
		minted = JSON.parse(sessionsLine);
	} catch {
		console.log('REFUSED in-container seed produced an unparsable SESSIONS line');
		process.exitCode = 1;
		return;
	}

	const sessionsOut = minted.map(({ handle, value }) => ({
		handle,
		cookies: [
			{
				name: SESSION_COOKIE_NAME,
				value,
				domain: SESSION_COOKIE_DOMAIN,
				path: '/',
				secure: true,
				httpOnly: true,
			},
		],
	}));

	fs.mkdirSync(path.dirname(outPath), { recursive: true });
	fs.writeFileSync(outPath, JSON.stringify(sessionsOut, null, 2));
	console.log(`SEEDED ${sessionsOut.length}`);
}

async function link(handle) {
	if (!HANDLE_RULE.test(handle ?? '')) {
		console.log('REFUSED bad handle');
		process.exitCode = 2;
		return;
	}
	// Auth.js v5 here is JWT-strategy + cookie-only; there is no app route that
	// accepts an externally-minted token/code via URL and sets the session
	// cookie for a human browser (checked: no impersonation/magic-link route in
	// ardanova-client/src). Minting a cookie value is not a usable "link" - a
	// human cannot attach an HttpOnly cookie by visiting a URL. Per the
	// adapter's refusal contract, this needs new app support we're not allowed
	// to add (uxqa/** only; no ardanova-client/src changes).
	console.log('REFUSED link requires app support');
	process.exitCode = 2;
}

async function main() {
	const [, , cmd, a, b] = process.argv;
	if (cmd === 'guard') await guard();
	else if (cmd === 'seed') await seed(a, b);
	else if (cmd === 'link') await link(a);
	else {
		console.log('REFUSED unknown command');
		process.exitCode = 2;
	}
}

await main();
