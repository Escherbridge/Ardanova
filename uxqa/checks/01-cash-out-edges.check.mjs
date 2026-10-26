// Regression checks for the pass 01-cash-out-edges fixes (F3 destination copy, mobile nav disclosure, F9 axe). See uxqa/passes/01-cash-out-edges/synthesis.md §10.
// Run: UXQA_CHECK_BASE=http://localhost:3000 node --test uxqa/checks/01-cash-out-edges.check.mjs
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { createRequire } from 'node:module';
import path from 'node:path';
import { after, before, test } from 'node:test';
import { fileURLToPath, pathToFileURL } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const baseUrl = (process.env.UXQA_CHECK_BASE || 'http://localhost:3000').replace(/\/$/, '');
const axePath = process.env.UXQA_AXE_PATH || path.join(repoRoot, 'ardanova-client', 'node_modules', 'axe-core', 'axe.min.js');

/** Playwright from UXQA_PLAYWRIGHT, else the local QA harness install, else normal resolution. */
async function importPlaywright() {
	if (process.env.UXQA_PLAYWRIGHT) return import(pathToFileURL(process.env.UXQA_PLAYWRIGHT).href);
	try {
		const requireFromHarness = createRequire(path.join(repoRoot, '.omc', 'qa-harness', 'package.json'));
		return import(pathToFileURL(requireFromHarness.resolve('playwright')).href);
	} catch {
		return import('playwright');
	}
}

let browser;
before(async () => {
	const playwright = await importPlaywright();
	const { chromium } = playwright.chromium ? playwright : playwright.default;
	browser = await chromium.launch();
});
after(async () => browser?.close());

async function openPage(pathname, viewport = { width: 1280, height: 900 }) {
	const context = await browser.newContext({ viewport });
	const page = await context.newPage();
	const response = await page.goto(baseUrl + pathname, { waitUntil: 'load' });
	assert.equal(response?.status(), 200, `GET ${pathname} returned ${response?.status()}`);
	return page;
}

test('sign-in names the money destination the visitor came from', async () => {
	const page = await openPage('/auth/signin?callbackUrl=%2Fportfolio%2Fwithdraw');
	const destination = page.getByTestId('signin-return-destination');
	await destination.waitFor({ timeout: 15000 });
	assert.match(await destination.innerText(), /withdraw money/);
	await page.context().close();
});

test('sign-in names no destination for an off-site callbackUrl', async () => {
	const page = await openPage('/auth/signin?callbackUrl=https%3A%2F%2Fevil.example%2Fx');
	// The normal sign-in screen rendered, so the missing line is not an error page.
	await page.getByRole('button', { name: 'Continue with Google' }).waitFor({ timeout: 15000 });
	assert.equal(await page.getByTestId('signin-return-destination').count(), 0);
	assert.doesNotMatch(await page.locator('main').innerText(), /evil\.example/);
	await page.context().close();
});

test('home menu below 768px opens, lists the four destinations, and Escape returns focus to the toggle', async () => {
	const page = await openPage('/', { width: 390, height: 844 });
	const toggle = page.getByRole('button', { name: 'Menu' });
	await toggle.waitFor({ timeout: 15000 });
	assert.equal(await toggle.getAttribute('aria-expanded'), 'false');
	const panel = page.locator(`[id="${await toggle.getAttribute('aria-controls')}"]`);
	assert.equal(await panel.isVisible(), false);

	await toggle.click();
	assert.equal(await toggle.getAttribute('aria-expanded'), 'true');
	assert.equal(await panel.isVisible(), true);
	const visibleLinks = await panel.getByRole('link').allInnerTexts();
	assert.deepEqual(visibleLinks.map((text) => text.trim()), ['Method', 'Rights', 'Nova', 'Projects']);

	// Focus must leave the toggle first, or the focus-return assertion below passes trivially.
	await panel.getByRole('link', { name: 'Rights' }).focus();
	assert.equal(await toggle.evaluate((element) => element === document.activeElement), false);
	await page.keyboard.press('Escape');
	assert.equal(await toggle.getAttribute('aria-expanded'), 'false');
	assert.equal(await panel.isVisible(), false);
	assert.equal(await toggle.evaluate((element) => element === document.activeElement), true);
	await page.context().close();
});

test('axe scan of / reports no aria-prohibited-attr (F9)', async () => {
	assert.ok(fs.existsSync(axePath), `axe-core not found at ${axePath}`);
	const page = await openPage('/');
	await page.addScriptTag({ content: fs.readFileSync(axePath, 'utf8') });
	const violations = await page.evaluate(async () =>
		(await window.axe.run(document, { runOnly: ['wcag2a', 'wcag2aa', 'wcag21aa'] })).violations.map((violation) => ({
			id: violation.id,
			targets: violation.nodes.map((node) => node.target.join(' ')),
		})),
	);
	const prohibited = violations.filter((violation) => violation.id === 'aria-prohibited-attr');
	assert.deepEqual(prohibited, [], `aria-prohibited-attr on /: ${JSON.stringify(prohibited)}`);
	await page.context().close();
});
