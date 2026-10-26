// waitDeploy hook: exit 0 once the released client serves the local HEAD commit.
import { execSync } from 'node:child_process';

const BASE = 'https://ardanova-client-production.up.railway.app';
const head = execSync('git rev-parse HEAD', { encoding: 'utf8' }).trim();
const deadline = Date.now() + 15 * 60_000;

while (Date.now() < deadline) {
	try {
		const body = await (await fetch(BASE + '/api/health', { signal: AbortSignal.timeout(15_000) })).json();
		if (body?.commit === head) {
			console.log(`DEPLOYED ${head.slice(0, 10)}`);
			process.exit(0);
		}
		console.log(`waiting: serving ${String(body?.commit ?? 'unknown').slice(0, 10)}, want ${head.slice(0, 10)}`);
	} catch {
		console.log('waiting: health unreadable');
	}
	await new Promise((r) => setTimeout(r, 20_000));
}
console.log('TIMEOUT: deploy never served HEAD');
process.exit(1);
