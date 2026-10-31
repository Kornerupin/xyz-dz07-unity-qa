// Local contract tests: the GitHub client is a test double; no network calls.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const createIssue = require('../tools/create_ai_issue.cjs');

async function fixture(overrides, fn) {
  const base = path.resolve(__dirname, '../artifacts/issue-tests');
  const dir = path.join(base, crypto.randomUUID());
  fs.mkdirSync(dir, { recursive: true });
  const status = { status: 'ai_success', eligible: true, failed_test_count: 1,
    provenance: { commit: 'unit-test-sha', run_id: '42', run_attempt: '1' }, ...overrides };
  fs.writeFileSync(path.join(dir, 'status.json'), JSON.stringify(status));
  fs.writeFileSync(path.join(dir, 'ai_bug_report.md'), '# Synthetic unit-test report\n');
  const requests = [];
  const outputs = {};
  const ctx = {
    evidenceDir: dir, runAttempt: '1',
    context: { runId: 42, runAttempt: 1, runNumber: 7, sha: 'unit-test-sha', repo: { owner: 'test', repo: 'unit' } },
    github: { rest: { issues: { create: async request => {
      requests.push(request); return { data: { html_url: 'https://example.invalid/unit-test/42' } };
    } } } },
    core: { setOutput: (k, v) => { outputs[k] = v; }, summary: { addLink() { return this; }, async write() {} } },
  };
  try { await fn(ctx, requests, outputs); }
  finally {
    if (path.dirname(dir) !== base) throw new Error('Unexpected test cleanup path');
    fs.rmSync(dir, { recursive: true });
  }
}

test('successful current analysis creates one Issue and saves its URL', async () => {
  await fixture({}, async (ctx, requests, outputs) => {
    const result = await createIssue(ctx);
    assert.equal(requests.length, 1);
    assert.equal(requests[0].body, '# Synthetic unit-test report\n');
    assert.equal(outputs.url, result.url);
    assert.deepEqual(JSON.parse(fs.readFileSync(path.join(ctx.evidenceDir, 'issue.json'))), result);
  });
});
for (const [name, overrides] of [
  ['blocked API', { status: 'blocked', eligible: false }],
  ['no test failure', { failed_test_count: 0 }],
  ['stale commit', { provenance: { commit: 'old', run_id: '42', run_attempt: '1' } }],
  ['stale run', { provenance: { commit: 'unit-test-sha', run_id: '41', run_attempt: '1' } }],
  ['stale retry', { provenance: { commit: 'unit-test-sha', run_id: '42', run_attempt: '0' } }],
]) {
  test(name + ' does not create an Issue', async () => {
    await fixture(overrides, async (ctx, requests) => {
      await assert.rejects(createIssue(ctx), /not eligible/);
      assert.equal(requests.length, 0);
    });
  });
}
test('empty report does not create an Issue', async () => {
  await fixture({}, async (ctx, requests) => {
    fs.writeFileSync(path.join(ctx.evidenceDir, 'ai_bug_report.md'), '  ');
    await assert.rejects(createIssue(ctx), /Empty/);
    assert.equal(requests.length, 0);
  });
});
