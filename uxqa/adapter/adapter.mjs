// ArdaNova session adapter: guard | seed <personas.json> <out.json> | link <handle>
// Contract + refusal rules: ux-qa-research reference/session-adapters.md; see uxqa/study.md.
import fs from 'node:fs';

const BASE = 'https://ardanova-client-production.up.railway.app';
const GUARD_FIELD = 'deploymentKey';
const GUARD_VALUE = 'qa-ardanova-a876ca12';
const HANDLE_RULE = /^qa-[a-z0-9-]+$/;

async function guard() {
	let body;
	try {
		const res = await fetch(BASE + '/api/health', { signal: AbortSignal.timeout(20_000) });
		body = await res.json();
	} catch (e) {
		console.log(`REFUSED health unreadable (${String(e.message || e).slice(0, 60)})`);
		process.exit(2);
	}
	if (body?.[GUARD_FIELD] !== GUARD_VALUE) {
		console.log(`REFUSED ${GUARD_FIELD}=${JSON.stringify(body?.[GUARD_FIELD] ?? null)} (expected the QA marker; is the post-azoa-sdk client deployed?)`);
		process.exit(2);
	}
	console.log(`OK ${GUARD_FIELD}=${GUARD_VALUE} commit=${body.commit ?? 'unknown'}`);
}

// seed: railway ssh into Ardanova-Client, push an in-target script over stdin that
// (1) re-checks the guard from inside, (2) upserts qa-* users with Prisma inside the
// seed-owned @local.ardanova.test namespace... NO - released env has no DEV bypass:
// it must mint real Auth.js v5 session JWEs with the in-container AUTH_SECRET and
// print one "SESSIONS [...]" line. TODO(finish after the client deploy): port
// templates/adapters/railway-ssh-prisma with Auth.js `encode` from the standalone bundle.
async function seed(personasPath, _outPath) {
	const personas = JSON.parse(fs.readFileSync(personasPath, 'utf8'));
	const bad = personas.filter((p) => !HANDLE_RULE.test(p.handle));
	if (bad.length) {
		console.log(`REFUSED non-persona handles: ${bad.map((p) => p.handle).join(', ')}`);
		process.exit(2);
	}
	console.log('REFUSED seed not implemented yet: finish the railway-ssh Auth.js mint (uxqa/RUNBOOK.md setup block)');
	process.exit(2);
}

function link(handle) {
	if (!HANDLE_RULE.test(handle ?? '')) {
		console.log('REFUSED bad handle');
		process.exit(2);
	}
	console.log('REFUSED link not implemented yet');
	process.exit(2);
}

const [, , cmd, a, b] = process.argv;
if (cmd === 'guard') await guard();
else if (cmd === 'seed') await seed(a, b);
else if (cmd === 'link') link(a);
else {
	console.log('REFUSED unknown command');
	process.exit(2);
}
